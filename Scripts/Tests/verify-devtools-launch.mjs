import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import crypto from 'node:crypto';
import { spawn } from 'node:child_process';
import assert from 'node:assert/strict';

const [program, gate, mode = 'development'] = process.argv.slice(2);
if (!program || !gate) throw new Error('Usage: node verify-devtools-launch.mjs <absolute apphost> <fresh absolute gate> [development|production|custom|menu]');
fs.mkdirSync(path.dirname(gate), { recursive: true });
const directory = process.platform === 'win32' ? path.join(process.env.LOCALAPPDATA, 'Nazor', 'DevTools') :
    path.join(os.tmpdir(), 'nazor-dt-' + crypto.createHash('sha256').update(os.userInfo().username).digest('hex').slice(0, 12).toUpperCase());
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
let counter = 0;
const children = new Set();
const sessions = new Set();
const evidence = { platform: process.platform, mode, stages: [] };
const target = spawn(program, [gate, ...(mode === 'custom' ? ['--custom-shortcut'] : mode === 'menu' ? ['--menu-only'] : [])], { cwd: path.dirname(program), stdio: ['ignore', 'pipe', 'pipe'], windowsHide: true });
target.on('error', error => { evidence.spawnError = error.message; });
let log = '';
target.stdout.on('data', bytes => { log += bytes; }); target.stderr.on('data', bytes => { log += bytes; });
async function until(read) {
    const end = Date.now() + 45000;
    while (true) {
        try { const value = read(); if (value) return value; } catch (error) { if (!['ENOENT', 'EBUSY', 'ESRCH'].includes(error.code) && !(error instanceof SyntaxError)) throw error; }
        if (Date.now() >= end) throw new Error('Native launch acceptance timed out: ' + log);
        await wait(50);
    }
}
const status = number => JSON.parse(fs.readFileSync(gate + '.' + number + '.status', 'utf8'));
async function command(number, name) {
    const value = name + ' ' + ++counter;
    await until(() => { fs.writeFileSync(gate + '.' + number + '.command', value); return true; });
    if (name === 'close') return;
    if (name === 'exit') { await until(() => target.exitCode !== null); return; }
    return until(() => { const result = status(number); return result.command === value && result; });
}
function instance(session) {
    const value = JSON.parse(fs.readFileSync(path.join(directory, session + '.ui'), 'utf8'));
    process.kill(value.processId, 0); children.add(value.processId); return value;
}
try {
    const first = await until(() => status(1));
    await wait(250);
    if (mode === 'production') {
        assert.equal(first.descriptor, null);
        await command(1, 'f12');
        assert(!fs.existsSync(path.join(path.dirname(program), 'Nazor.DevTools')));
        evidence.stages.push('No Agent, bundle or F12 launch in the production application.');
    } else {
        const activation = mode === 'custom' ? 'shortcut' : mode === 'menu' ? 'menu' : 'f12';
        const identity = first.descriptor.Identity;
        const session = identity.SessionId;
        sessions.add(session);
        const payload = first.descriptor.InspectorPath;
        assert(payload.startsWith(path.join(path.dirname(program), 'Nazor.DevTools')));
        if (mode === 'custom' || mode === 'menu') {
            const applicationKey = await command(1, 'f12');
            assert.equal(applicationKey.f12Calls, 1, 'F12 must reach the application when the DevTools shortcut is changed or disabled.');
        }
        await command(1, 'modified'); await command(1, 'repeat'); await wait(400);
        assert(!fs.existsSync(path.join(directory, session + '.ui')));
        await command(1, activation);
        const ui = await until(() => instance(session));
        await until(() => { fs.writeFileSync(gate + '.1.command', 'status ' + ++counter); return status(1).connected; });
        if (process.platform === 'win32') assert.equal((await command(1, 'owner ' + ui.processId)).ownerVerified, true, 'The inspector must be owned by its launching window.');
        await command(1, activation); await wait(1500);
        if (process.platform === 'win32') { await command(1, 'menu'); await wait(1000); }
        assert.equal(instance(session).processId, ui.processId);
        evidence.stages.push({ session, inspectorProcessId: ui.processId, activation, repeatedActivationReused: true, liveAgentConnection: true });
        await command(1, 'second');
        const second = await until(() => status(2));
        sessions.add(second.descriptor.Identity.SessionId);
        await command(2, activation);
        const other = await until(() => instance(second.descriptor.Identity.SessionId));
        assert.notEqual(other.processId, ui.processId);
        evidence.stages.push({ secondSession: second.descriptor.Identity.SessionId, secondInspectorProcessId: other.processId });
        process.kill(ui.processId); await wait(1000);
        assert.equal(target.exitCode, null);
        await command(1, activation);
        const reopened = await until(() => { const current = instance(session); return current.processId !== ui.processId && current; });
        evidence.stages.push({ independentCloseAndReopen: reopened.processId });
        const manifest = path.join(path.dirname(payload), 'Nazor.DevTools.bundle.json');
        const original = fs.readFileSync(manifest, 'utf8');
        try {
            const invalid = JSON.parse(original); invalid.version = '0.0.0-invalid'; fs.writeFileSync(manifest, JSON.stringify(invalid));
            assert.match((await command(1, 'open')).error, /mismatch/);
            fs.renameSync(manifest, manifest + '.saved');
            assert((await command(1, 'open')).error);
            fs.renameSync(manifest + '.saved', manifest);
        } finally { if (fs.existsSync(manifest + '.saved')) fs.renameSync(manifest + '.saved', manifest); fs.writeFileSync(manifest, original); }
        evidence.stages.push('Missing/mismatched assets report errors without closing the target.');
        await command(2, 'close');
        await until(() => !fs.existsSync(path.join(directory, second.descriptor.Identity.SessionId + '.ui')));
        process.kill(reopened.processId, 0);
        assert.equal(target.exitCode, null);
        evidence.stages.push('Closing the second owner closes only its inspector; the first window and inspector remain alive.');
        await command(1, 'exit');
        await until(() => target.exitCode !== null);
        await until(() => !fs.existsSync(path.join(directory, session + '.ui')));
        evidence.stages.push('The inspector closes when its owner exits.');
    }
    evidence.passed = true;
} catch (error) { evidence.error = error.stack; process.exitCode = 1; }
finally {
    target.kill();
    target.stdout.destroy(); target.stderr.destroy();
    for (const session of sessions) { try { children.add(JSON.parse(fs.readFileSync(path.join(directory, session + '.ui'), 'utf8')).processId); } catch {} }
    for (const pid of children) { try { process.kill(pid); } catch {} }
    fs.writeFileSync(gate + '.evidence.json', JSON.stringify(evidence, null, 2));
    fs.writeFileSync(gate + '.log', log);
    console.log(JSON.stringify(evidence, null, 2));
}
