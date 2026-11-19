import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';

const [fixture] = process.argv.slice(2);
if (!fixture) throw new Error('Pass the built DevToolsProtocol.dll fixture.');

async function connect(major = 1) {
    const process = spawn('dotnet', [fixture], { windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'] });
    const pending = new Map();
    let buffer = Buffer.alloc(0);
    let id = 0;
    let stderr = '';
    process.stderr.on('data', bytes => { stderr = (stderr + bytes).slice(-8192); });
    process.stdout.on('data', bytes => {
        buffer = Buffer.concat([buffer, bytes]);
        while (true) {
            const separator = buffer.indexOf('\r\n\r\n');
            if (separator < 0) { assert.ok(buffer.length <= 8192); return; }
            const match = /^Content-Length: ([0-9]+)$/i.exec(buffer.subarray(0, separator).toString('ascii'));
            assert.ok(match, 'valid response framing');
            const length = Number(match[1]);
            assert.ok(length > 0 && length <= 1048576);
            if (buffer.length < separator + 4 + length) return;
            const message = JSON.parse(buffer.subarray(separator + 4, separator + 4 + length).toString('utf8'));
            buffer = buffer.subarray(separator + 4 + length);
            assert.equal(message.jsonrpc, '2.0');
            const request = pending.get(message.id);
            assert.ok(request, 'correlated response');
            pending.delete(message.id);
            clearTimeout(request.timer);
            request.resolve(message);
        }
    });
    const exited = new Promise(resolve => process.on('exit', resolve));
    process.on('error', error => { for (const request of pending.values()) request.reject(error); });
    process.on('exit', () => {
        for (const request of pending.values()) { clearTimeout(request.timer); request.reject(new Error(`Fixture exited: ${stderr}`)); }
        pending.clear();
    });
    function send(message) {
        const body = Buffer.from(JSON.stringify({ jsonrpc: '2.0', ...message }), 'utf8');
        process.stdin.write(Buffer.concat([Buffer.from(`Content-Length: ${body.length}\r\n\r\n`, 'ascii'), body]));
    }
    function request(method, params) {
        const requestId = String(++id);
        const result = new Promise((resolve, reject) => {
            const timer = setTimeout(() => { process.kill(); reject(new Error(`Timeout: ${method}`)); }, 5000);
            pending.set(requestId, { resolve, reject, timer });
        });
        send({ id: requestId, method, params });
        return { id: requestId, result };
    }
    const hello = await request('initialize', { version: { major, minor: 99 }, clientName: 'JavaScript fixture 🙂', capabilities: ['tree', 'selection', 'future.capability'] }).result;
    return { process, hello, request, send, exited, async close() { process.stdin.end(); await exited; } };
}

const peer = await connect();
try {
    assert.deepEqual(peer.hello.result.version, { major: 1, minor: 3 });
    assert.deepEqual(peer.hello.result.capabilities, ['tree', 'selection']);
    const context = { sessionId: 'session-instance', generation: 1, stopId: null };
    const params = extra => ({ context, deadlineUtc: new Date(Date.now() + 4000).toISOString(), ...extra });
    const first = peer.request('tree/get', params({ delayMs: 100, futureText: 'Caffè 漢字 🙂' }));
    const second = peer.request('tree/get', params({ delayMs: 0 }));
    const order = [];
    first.result.then(() => order.push('first'));
    second.result.then(() => order.push('second'));
    const snapshot = (await second.result).result;
    await first.result;
    assert.deepEqual(order, ['second', 'first']);
    assert.equal(snapshot.elements[0].text, 'Caffè 漢字 🙂');
    assert.equal(snapshot.components[0].id, 'c1');
    assert.equal(snapshot.capture.source, 'live');
    const cancelled = peer.request('selection/set', params({ kind: 'element', nodeId: 'e1' }));
    peer.send({ method: '$/cancelRequest', params: { id: cancelled.id } });
    assert.equal((await cancelled.result).error.code, -32016);
    assert.equal((await peer.request('tree/get', params()).result).result.revision, 1);
    assert.equal((await peer.request('selection/set', params({ kind: 'element', nodeId: 'e1' })).result).result.nodeId, 'e1');
    assert.equal((await peer.request('tree/get', params()).result).result.revision, 2);
    assert.equal((await peer.request('unknown/method', params()).result).error.code, -32601);
    assert.equal((await peer.request('tree/get', params({ context: { ...context, generation: 2 } })).result).error.code, -32012);
    assert.equal((await peer.request('tree/get', { context: { sessionId: 'missing' } }).result).error.code, -32602);
    console.log('PASS: independent JavaScript client, UTF-8 framing, negotiation, ordering, cancellation and errors.');
} finally { await peer.close(); }
const mismatch = await connect(2);
assert.equal(mismatch.hello.error.code, -32014);
await mismatch.exited;
console.log('PASS: independent major-version mismatch closes the session.');
