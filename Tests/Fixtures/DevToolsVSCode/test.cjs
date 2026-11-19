'use strict';
const vscode = require('vscode');
const fs = require('node:fs');
const path = require('node:path');
const net = require('node:net');
const assert = require('node:assert/strict');
const os = require('node:os');
const crypto = require('node:crypto');
const { execFile } = require('node:child_process');
const { promisify } = require('node:util');
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
exports.run = async () => {
    const settings = JSON.parse(fs.readFileSync(process.env.NAZOR_DT11_CONFIG, 'utf8'));
    const events = [];
    const evidence = { platform: process.platform, code: vscode.version, stages: [] };
    let inspector;
    const record = () => fs.writeFileSync(settings.output + '.evidence.json', JSON.stringify(evidence, null, 2));
    async function probeUi(value, stop, retained = false) {
        const script = path.resolve(__dirname, '../../../Scripts/Tests/verify-devtools-ide-shell.ps1');
        const result = await promisify(execFile)('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', script,
            '-ProcessId', String(inspector.processId), '-Value', value, '-Stop', stop, ...(retained ? ['-Retained'] : [])], { timeout: 30000, windowsHide: true });
        return result.stdout;
    }
    const tracker = vscode.debug.registerDebugAdapterTrackerFactory('coreclr', { createDebugAdapterTracker() {
        return { onDidSendMessage(message) {
            if (message.type === 'event') events.push(message);
            if (message.type === 'response' && ['variables', 'evaluate'].includes(message.command)) fs.appendFileSync(settings.output + '.dap.jsonl', JSON.stringify(message) + '\n');
        } };
    } });
    async function until(test) {
        const end = Date.now() + 90000;
        while (!test()) { if (Date.now() >= end) throw new Error('Timeout waiting for native debugger acceptance. Events: ' + JSON.stringify(events)); await wait(100); }
    }
    async function rpc(method, params, attempt = 0) {
        const descriptor = JSON.parse(fs.readFileSync(inspector.descriptor, 'utf8'));
        return new Promise((resolve, reject) => {
            const socket = net.connect(process.platform === 'win32' ? '\\\\.\\pipe\\' + descriptor.pipeName : descriptor.pipeName);
            let bytes = Buffer.alloc(0);
            socket.setTimeout(5000, () => socket.destroy(new Error('IDE bridge timeout')));
            socket.on('error', reject);
            socket.on('connect', () => {
                const body = Buffer.from(JSON.stringify({ version: 1, token: descriptor.token, method, params }));
                socket.write(Buffer.concat([Buffer.from(`Content-Length: ${body.length}\r\n\r\n`), body]));
            });
            socket.on('data', data => { bytes = Buffer.concat([bytes, data]); });
            socket.on('end', () => {
                try { const response = JSON.parse(bytes.subarray(bytes.indexOf('\r\n\r\n') + 4)); if (response.error) reject(new Error(response.error)); else resolve(response.result); }
                catch (error) { reject(error); }
            });
        }).catch(async error => {
            if (attempt >= 10 || !error.message.includes('preceding read')) throw error;
            await wait(100);
            return rpc(method, params, attempt + 1);
        });
    }
    try {
        await vscode.extensions.getExtension('nazor.nazor').activate();
        await vscode.extensions.getExtension('ms-dotnettools.csharp').activate();
        await wait(15000);
        assert(await vscode.debug.startDebugging(undefined, { type: 'coreclr', request: 'launch', name: 'Nazor DT-11 acceptance',
            program: settings.program, args: [settings.output], cwd: path.dirname(settings.program), stopAtEntry: false,
            console: 'internalConsole', justMyCode: true, nazorDevTools: true,
            env: settings.native ? { NAZOR_DEVTOOLS_NATIVE_PROBE: '1', NAZOR_DEVTOOLS_NATIVE_F12: settings.f12 ? '1' : '0' } : {} }));
        await until(() => fs.existsSync(settings.output + '.ready'));
        const target = JSON.parse(fs.readFileSync(settings.output + '.descriptor', 'utf8'));
        const identity = target.Identity || target.identity;
        const pid = identity.ProcessId || identity.processId;
        const sessionId = identity.SessionId || identity.sessionId;
        if (settings.f12) {
            const directory = process.platform === 'win32' ? path.join(process.env.LOCALAPPDATA, 'Nazor', 'DevTools') :
                path.join(os.tmpdir(), 'nazor-dt-' + crypto.createHash('sha256').update(os.userInfo().username).digest('hex').slice(0, 12).toUpperCase());
            let bridge;
            await until(() => {
                bridge = fs.readdirSync(directory).filter(f => f.endsWith('.ide')).map(f => path.join(directory, f)).find(file => {
                    try { return JSON.parse(fs.readFileSync(file, 'utf8')).processId === pid; } catch { return false; }
                });
                return bridge;
            });
            fs.writeFileSync(settings.output, 'go');
            let ui;
            await until(() => { try { ui = JSON.parse(fs.readFileSync(path.join(directory, sessionId + '.ui'), 'utf8')); return ui.processId > 0; } catch { return false; } });
            inspector = { processId: ui.processId, descriptor: bridge };
            await until(() => events.some(e => e.event === 'stopped'));
            const status = await rpc('state', { processId: pid });
            if (process.platform === 'win32') {
                evidence.nativeUi = await probeUi('11', status.stopId);
            }
            const launched = await vscode.commands.executeCommand('nazor.openDevTools');
            assert.equal(launched.processId, ui.processId, 'The IDE command must reuse the inspector opened through F12.');
            inspector = launched;
            evidence.f12AndIdeReused = ui.processId;
        } else inspector = await vscode.commands.executeCommand('nazor.openDevTools');
        assert(inspector?.descriptor, 'The production extension must launch the external inspector and publish its bridge.');
        await wait(2000);
        process.kill(inspector.processId, 0);
        if (!settings.f12) fs.writeFileSync(settings.output, 'go');
        let stops = 0;
        for (let index = 0; index < 3; index++) {
            await until(() => events.filter(e => e.event === 'stopped').length > stops);
            stops++;
            const status = await rpc('state', { processId: pid });
            assert.equal(status.state, 'paused');
            assert.equal(status.canReadFields, true);
            const tree = await rpc('tree', { processId: pid, sessionId, stopId: status.stopId });
            const component = tree.components.find(c => c.typeName.includes('ParentComponent'));
            assert(component, JSON.stringify(tree));
            const fields = await rpc('fields', { processId: pid, sessionId, generation: 1, stopId: status.stopId, componentId: component.id });
            const value = name => fields.fields.find(f => f.name === name);
            assert.equal(value('Value').value, index === 0 ? '11' : '31');
            assert.equal(value('GetterCalls').value, '0');
            assert.equal(value('ToStringCalls').value, '0');
            if (settings.f12 && process.platform === 'win32') {
                evidence.nativeStages ??= [];
                evidence.nativeStages.push(await probeUi(index === 0 ? '11' : '31', status.stopId));
            }
            evidence.stages.push({ status, fields, component: component.id }); record();
            const threadId = events.filter(e => e.event === 'stopped').at(-1).body.threadId;
            if (index === 0) {
                await vscode.debug.activeDebugSession.customRequest('next', { threadId });
                await until(() => events.filter(e => e.event === 'stopped').length > stops);
                stops++;
                const stepped = await rpc('state', { processId: pid });
                assert.notEqual(stepped.stopId, status.stopId);
                await assert.rejects(rpc('fields', { processId: pid, sessionId, generation: 1, stopId: status.stopId, componentId: component.id }), /validated/);
                const current = await rpc('fields', { processId: pid, sessionId, generation: 1, stopId: stepped.stopId, componentId: component.id });
                assert.equal(current.fields.find(f => f.name === 'GetterCalls').value, '0');
                evidence.step = { status: stepped, fields: current }; record();
            }
            await vscode.debug.activeDebugSession.customRequest('continue', { threadId });
        }
        await until(() => fs.existsSync(settings.output + '.result') && fs.readFileSync(settings.output + '.result', 'utf8') === '31');
        assert.equal(fs.readFileSync(settings.output + '.result', 'utf8'), '31');
        if (settings.f12 && process.platform === 'win32') {
            await until(() => events.some(event => event.event === 'terminated' || event.event === 'exited'));
            evidence.retainedUi = await probeUi('31', evidence.stages.at(-1).status.stopId, true);
        }
        evidence.passed = true; record();
    } catch (error) { evidence.error = error.stack; record(); throw error; }
    finally { tracker.dispose(); await vscode.debug.stopDebugging(); if (inspector?.processId) { try { process.kill(inspector.processId); } catch {} } }
};
