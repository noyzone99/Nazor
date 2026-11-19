'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const moduleScope = { exports: {} };
vm.runInNewContext(fs.readFileSync(process.argv[2], 'utf8'), { module: moduleScope, process, Buffer, setTimeout, clearTimeout, setInterval, clearInterval,
    require(name) { return name === 'vscode' ? {} : require(name); } });
const { DebugSession, readString } = moduleScope.exports;
const variable = (name, value, variablesReference = 0) => ({ name, value, type: 'int', variablesReference });
(async () => {
    let calls = 0;
    const values = { 1: [variable('[0]', '{}', 2)], 2: [variable('SessionId [string]', '"session"'), variable('Components [DebuggerComponent[]]', '{}', 3)],
        3: [variable('[0]', '{}', 4)], 4: [variable('Id [string]', '"1"'), variable('Instance', '{}', 5)],
        5: [variable('Value [int]', '11'), { ...variable('Dangerous', 'Implicit function evaluation is turned off by the user'), presentationHint: { kind: 'data' } }] };
    const session = { customRequest: async (command, args) => {
        calls++;
        if (command === 'threads') return { threads: [{ id: 7 }] };
        if (command === 'stackTrace') return { stackFrames: [{ id: 42 }] };
        if (command === 'evaluate') { assert.equal(args.expression, 'Nazor.DevTools.DebuggerRoots.Windows'); assert.equal(args.frameId, 42); return { variablesReference: 1 }; }
        if (command === 'variables') return { variables: values[args.variablesReference] };
        throw new Error(command);
    } };
    const state = new DebugSession(session);
    state.received({ type: 'event', event: 'process', body: { systemProcessId: 17 } });
    state.received({ type: 'event', event: 'stopped', body: { threadId: 7 } });
    const args = { processId: 17, sessionId: 'session', generation: 1, componentId: '1', stopId: '1' };
    await assert.rejects(state.read('fields', args), /validated/);
    assert.equal(calls, 0);
    state.sent({ type: 'request', command: 'launch', arguments: { expressionEvaluationOptions: { allowImplicitFuncEval: false, allowToString: false, allowFastEvaluate: false, showRawValues: true } } });
    const result = await state.read('fields', args);
    assert.equal(result.fields[0].name, 'Value');
    assert.equal(result.fields[0].value, '11');
    assert.equal(result.fields[1].available, false);
    state.sent({ type: 'request', command: 'next' });
    await assert.rejects(state.read('fields', args), /validated/);
    state.received({ type: 'event', event: 'stopped', body: { threadId: 7 } });
    await assert.rejects(state.read('fields', args), /validated/);
    const count = calls;
    await assert.rejects(state.read('fields', { ...args, processId: 18 }), /changed/);
    assert.equal(calls, count);
    assert.throws(() => readString(variable('x', '"truncated...')), /complete/);
    console.log('PASS: raw DAP navigation, safe configuration, getter unavailability, target/stop invalidation, complete strings.');
})().catch(error => { console.error(error); process.exitCode = 1; });
