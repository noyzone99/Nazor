'use strict';

const vscode = require('vscode');
const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');
const crypto = require('node:crypto');
const net = require('node:net');
const { TextDecoder } = require('node:util');
const { spawn } = require('node:child_process');
const limit = 1024 * 1024;
const directory = process.platform === 'win32' ? path.join(process.env.LOCALAPPDATA, 'Nazor', 'DevTools') :
    path.join(os.tmpdir(), 'nazor-dt-' + crypto.createHash('sha256').update(os.userInfo().username).digest('hex').slice(0, 12).toUpperCase());
const sessions = new Map();

function readString(variable) {
    if (!variable || typeof variable.value !== 'string') throw new Error('The debugger field is unavailable.');
    try { return JSON.parse(variable.value); } catch { throw new Error('The debugger did not return a complete raw string.'); }
}

class DebugSession {
    constructor(session) {
        this.session = session;
        this.stop = 0;
        this.state = 'running';
        this.safe = false;
        this.clients = new Set();
    }
    sent(message) {
        if (message.type !== 'request') return;
        if (message.command === 'launch' || message.command === 'attach') {
            const options = message.arguments?.expressionEvaluationOptions;
            this.safe = options?.allowImplicitFuncEval === false && options?.allowToString === false &&
                options?.allowFastEvaluate === false && options?.showRawValues === true;
        }
        if (['continue', 'next', 'stepIn', 'stepOut', 'restart', 'disconnect'].includes(message.command)) this.transition('running');
    }
    received(message) {
        if (message.type !== 'event') return;
        if (message.event === 'process') {
            this.pid = message.body.systemProcessId;
            this.discovery = setInterval(() => {
                if (this.scanning || this.descriptor) return;
                this.scanning = true;
                (async () => {
                    try {
                        for (const file of fs.readdirSync(directory).filter(name => /^[a-f0-9]{32}\.json$/.test(name)).slice(0, 256)) {
                            const full = path.join(directory, file), info = fs.lstatSync(full);
                            if (!info.isFile() || info.isSymbolicLink() || info.size > 16384) continue;
                            const target = JSON.parse(fs.readFileSync(full, 'utf8'));
                            if (target.version === 1 && target.identity?.processId === this.pid) { await this.listen(target); break; }
                        }
                    } catch {} finally { this.scanning = false; }
                })();
            }, 1000);
            this.discovery.unref();
        }
        if (message.event === 'stopped') { this.thread = message.body.threadId; this.transition('paused'); }
        if (message.event === 'continued') this.transition('running');
        if (message.event === 'exited' || message.event === 'terminated') { this.transition('exited'); this.dispose(); }
    }
    transition(state) { this.state = state; this.stop++; }
    status(pid) {
        if (pid !== this.pid) throw new Error('The debugger target changed.');
        return { processId: pid, adapter: 'VS Code / C# (coreclr)', state: this.state,
            stopId: this.state === 'paused' ? String(this.stop) : null, canReadFields: this.safe,
            message: this.safe ? 'Raw fields only; render/layout may be incomplete at this stop.' :
                'Current-stop reads require nazorDevTools: true in the coreclr launch/attach configuration. Restart debugging after changing it.' };
    }
    async read(method, args) {
        const status = this.status(args.processId);
        if (method === 'state') return status;
        if (!this.safe || this.state !== 'paused' || args.stopId !== String(this.stop)) throw new Error('No validated current debugger stop.');
        const stop = this.stop;
        const deadline = Date.now() + 2500;
        const request = async (command, parameters) => {
            if (this.stop !== stop || this.state !== 'paused' || Date.now() >= deadline) throw new Error('The debugger stop expired.');
            if (this.pending) throw new Error('The debugger is still processing the preceding read.');
            const work = this.pending = this.session.customRequest(command, parameters);
            work.then(() => { if (this.pending === work) this.pending = undefined; }, () => { if (this.pending === work) this.pending = undefined; });
            let timer;
            let result;
            try {
                result = await Promise.race([work, new Promise((_, reject) => {
                    timer = setTimeout(() => reject(new Error('Debugger read timed out.')), Math.max(1, deadline - Date.now())); timer.unref();
                })]);
            } finally { clearTimeout(timer); }
            if (this.stop !== stop || this.state !== 'paused') throw new Error('The debugger stop expired.');
            return result;
        };
        const children = async value => {
            if (!value?.variablesReference) throw new Error('The raw debugger reference is unavailable.');
            const result = await request('variables', { variablesReference: value.variablesReference, start: 0, count: 4096 });
            if (!Array.isArray(result.variables) || result.variables.length > 4096) throw new Error('Debugger expansion exceeds the field budget.');
            return result.variables.map(variable => ({ ...variable, name: variable.name.replace(/ \[.*\]$/, '') }));
        };
        const threads = (await request('threads', {})).threads.slice(0, 16);
        threads.sort((a, b) => (b.id === this.thread ? 1 : 0) - (a.id === this.thread ? 1 : 0));
        let root;
        for (const thread of threads) {
            const frames = (await request('stackTrace', { threadId: thread.id, startFrame: 0, levels: 16 })).stackFrames;
            for (const frame of frames) {
                try {
                    root = await request('evaluate', { expression: 'Nazor.DevTools.DebuggerRoots.Windows', frameId: frame.id, context: 'watch' });
                    if (root.variablesReference) break;
                } catch (error) { if (Date.now() >= deadline || this.stop !== stop) throw error; }
            }
            if (root?.variablesReference) break;
        }
        for (const window of (await children(root)).filter(v => /^\[\d+\]$/.test(v.name)).slice(0, 64)) {
            const fields = await children(window);
            if (readString(fields.find(v => v.name === 'SessionId')) !== args.sessionId) continue;
            if (method === 'tree') {
                const chunks = await children(fields.find(v => v.name === 'TreeChunks'));
                const json = chunks.filter(v => /^\[\d+\]$/.test(v.name)).map(readString).join('');
                if (Buffer.byteLength(json) > limit) throw new Error('The retained tree exceeds the debugger transfer budget.');
                return JSON.parse(json);
            }
            if (method !== 'fields') throw new Error('Unknown debugger operation.');
            const components = await children(fields.find(v => v.name === 'Components'));
            for (const component of components.filter(v => /^\[\d+\]$/.test(v.name))) {
                const values = await children(component);
                if (readString(values.find(v => v.name === 'Id')) !== args.componentId) continue;
                const raw = await children(values.find(v => v.name === 'Instance'));
                const result = raw.slice(0, 128).map(v => ({ name: v.name, type: v.type || '', value: String(v.value).slice(0, 4096),
                    available: !v.presentationHint?.attributes?.some(attribute => ['failedEvaluation', 'hasSideEffects'].includes(attribute)) &&
                        !/^(Implicit function evaluation is turned off by the user|Function evaluation is disabled)$/i.test(v.value) && v.presentationHint?.kind !== 'property' }));
                return { capture: { sessionId: args.sessionId, generation: args.generation, source: 'debugger', stopId: args.stopId, capturedAtUtc: new Date().toISOString() },
                    componentId: args.componentId, fields: result, message: 'Raw fields at the current stop; getters and ToString are disabled.' };
            }
            throw new Error('This component is absent from the materialized debugger registry.');
        }
        throw new Error('This window is absent from the materialized debugger registry.');
    }
    async listen(target) {
        if (!this.listening) this.listening = this.startListening(target).catch(error => { this.listening = undefined; this.server?.close(); throw error; });
        return this.listening;
    }
    async startListening(target) {
        if (this.descriptor) return this.descriptor;
        if (this.pid !== target.identity.processId) throw new Error('Select the VS Code debug session for this application.');
        const info = fs.lstatSync(directory);
        if (!info.isDirectory() || info.isSymbolicLink() || (process.platform !== 'win32' && (info.mode & 0o077))) throw new Error('DevTools discovery storage must be private.');
        const id = crypto.randomBytes(16).toString('hex');
        const token = crypto.randomBytes(32).toString('hex');
        const pipeName = process.platform === 'win32' ? 'nazor-ide-' + id : path.join(directory, id + '.ide.sock');
        this.server = net.createServer(socket => {
            if (this.clients.size >= 8) { socket.destroy(); return; }
            this.clients.add(socket);
            let buffer = Buffer.alloc(0), busy = false;
            const deadline = setTimeout(() => socket.destroy(), 5000);
            deadline.unref();
            socket.setTimeout(10000, () => socket.destroy());
            socket.on('error', () => {});
            socket.on('close', () => { clearTimeout(deadline); this.clients.delete(socket); });
            socket.on('data', data => {
                buffer = Buffer.concat([buffer, data]);
                if (buffer.length > 16384 + 128 || busy) { socket.destroy(); return; }
                const end = buffer.indexOf('\r\n\r\n');
                if (end < 0) { if (buffer.length > 128) socket.destroy(); return; }
                if (end > 128 || buffer.subarray(0, end).some(byte => byte > 127)) { socket.destroy(); return; }
                const header = buffer.subarray(0, end).toString('ascii');
                const match = /^Content-Length: ([1-9][0-9]{0,6})$/.exec(header);
                if (!match || Number(match[1]) > 16384) { socket.destroy(); return; }
                const length = Number(match[1]);
                if (buffer.length < end + 4 + length) return;
                if (buffer.length !== end + 4 + length) { socket.destroy(); return; }
                busy = true;
                (async () => {
                    let response;
                    try {
                        const message = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(buffer.subarray(end + 4)));
                        if (message.version !== 1 || message.token !== token || !['state', 'tree', 'fields'].includes(message.method)) throw new Error('Invalid debugger bridge request.');
                        response = { result: await this.read(message.method, message.params) };
                    } catch (error) { response = { error: error.message }; }
                    const body = Buffer.from(JSON.stringify(response));
                    if (body.length > limit) { socket.destroy(); return; }
                    socket.end(Buffer.concat([Buffer.from(`Content-Length: ${body.length}\r\n\r\n`), body]));
                })().catch(() => socket.destroy());
            });
        });
        await new Promise((resolve, reject) => { this.server.once('error', reject); this.server.listen(process.platform === 'win32' ? '\\\\.\\pipe\\' + pipeName : pipeName, resolve); });
        if (process.platform !== 'win32') fs.chmodSync(pipeName, 0o600);
        if (this.state === 'exited') { this.server.close(); throw new Error('The debug session ended.'); }
        this.descriptor = path.join(directory, id + '.ide');
        fs.writeFileSync(this.descriptor, JSON.stringify({ version: 1, processId: this.pid, processStartToken: target.processStartToken,
            hostProcessId: process.pid, pipeName, token }), { flag: 'wx', mode: 0o600 });
        return this.descriptor;
    }
    dispose() {
        clearInterval(this.discovery);
        for (const client of this.clients) client.destroy();
        this.server?.close();
        if (this.descriptor) { try { fs.unlinkSync(this.descriptor); } catch {} }
    }
}

function activate(context) {
    context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider('coreclr', {
        resolveDebugConfiguration(_folder, configuration) {
            if (configuration.nazorDevTools === true) configuration.expressionEvaluationOptions = {
                ...configuration.expressionEvaluationOptions, allowImplicitFuncEval: false, allowToString: false, allowFastEvaluate: false, showRawValues: true };
            return configuration;
        },
    }));
    context.subscriptions.push(vscode.debug.registerDebugAdapterTrackerFactory('coreclr', {
        createDebugAdapterTracker(session) {
            const state = new DebugSession(session);
            sessions.set(session.id, state);
            return { onWillReceiveMessage: message => state.sent(message), onDidSendMessage: message => state.received(message),
                onExit: () => { state.dispose(); sessions.delete(session.id); } };
        },
    }));
    context.subscriptions.push({ dispose() { for (const state of sessions.values()) state.dispose(); sessions.clear(); } });
    context.subscriptions.push(vscode.commands.registerCommand('nazor.openDevTools', async () => {
        try {
            const targets = fs.readdirSync(directory).filter(f => /^[a-f0-9]{32}\.json$/.test(f)).slice(0, 256).flatMap(file => {
                try {
                    const full = path.join(directory, file), info = fs.lstatSync(full);
                    if (!info.isFile() || info.isSymbolicLink() || info.size > 16384 || (process.platform !== 'win32' && (info.mode & 0o077))) return [];
                    const target = JSON.parse(fs.readFileSync(full, 'utf8'));
                    if (target.version !== 1 || !Number.isSafeInteger(target.identity?.processId) || target.identity.processId <= 0 ||
                        !/^[a-f0-9]{32}$/.test(target.identity.sessionId)) return [];
                    process.kill(target.identity.processId, 0);
                    return [target];
                } catch { return []; }
            });
            const state = sessions.get(vscode.debug.activeDebugSession?.id);
            const choices = targets.filter(t => !state || t.identity.processId === state.pid).map(target => ({
                label: 'Nazor window ' + target.identity.windowId.slice(0, 8), description: `PID ${target.identity.processId} / ${target.identity.sessionId}`, target }));
            const chosen = choices.length === 1 ? choices[0] : await vscode.window.showQuickPick(choices, { placeHolder: 'Select a running Nazor window' });
            if (!chosen) { if (!choices.length) throw new Error('No opted-in Nazor application was discovered. Call UseDevTools() in the application.'); return; }
            const target = chosen.target;
            if (!target.inspectorPath || !path.isAbsolute(target.inspectorPath) || !fs.existsSync(target.inspectorPath)) throw new Error('The inspector assets are missing. Rebuild the app with NazorEnableDevTools=true.');
            const args = ['--connect-session', target.identity.sessionId];
            if (target.inspectorVersion) args.push('--expected-version', target.inspectorVersion);
            if (state) args.push('--ide-descriptor', await state.listen(target));
            const executable = target.inspectorPath.endsWith('.dll') ? vscode.workspace.getConfiguration('nazor').get('dotnetPath') || 'dotnet' : target.inspectorPath;
            if (target.inspectorPath.endsWith('.dll')) args.unshift(target.inspectorPath);
            const child = spawn(executable, args, { cwd: path.dirname(target.inspectorPath), detached: true, stdio: ['ignore', 'ignore', 'pipe'], windowsHide: true });
            let diagnostic = '';
            child.stderr.on('data', bytes => { if (diagnostic.length < 8192) diagnostic += bytes.toString('utf8'); });
            child.on('exit', code => { if (code) vscode.window.showErrorMessage(`Nazor DevTools exited (${code}). Install the .NET 10 runtime and restore matching Nazor packages. ${diagnostic}`); });
            child.on('error', error => vscode.window.showErrorMessage('Nazor DevTools: ' + error.message));
            child.unref();
            const uiFile = path.join(directory, target.identity.sessionId + '.ui');
            let inspectorProcessId = child.pid;
            for (let attempt = 0; attempt < 60; attempt++) {
                try {
                    const ui = JSON.parse(fs.readFileSync(uiFile, 'utf8'));
                    process.kill(ui.processId, 0); inspectorProcessId = ui.processId; break;
                } catch {}
                if (child.exitCode && child.exitCode !== 0) throw new Error(diagnostic || 'The inspector could not start.');
                await new Promise(resolve => setTimeout(resolve, 50));
            }
            return { processId: inspectorProcessId, descriptor: state?.descriptor, sessionId: target.identity.sessionId };
        } catch (error) { vscode.window.showErrorMessage('Nazor DevTools: ' + error.message); }
    }));
}

module.exports = { activate, DebugSession, readString };
