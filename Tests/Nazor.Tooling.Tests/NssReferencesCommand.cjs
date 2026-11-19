'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

const handlers = new Map();
let executed;
class Uri { constructor(value) { this.value = value; } static parse(value) { return new Uri(value); } }
class Position { constructor(line, character) { this.line = line; this.character = character; } }
class Range { constructor(start, end) { this.start = start; this.end = end; } }
class Location { constructor(uri, range) { this.uri = uri; this.range = range; } }
const vscode = {
    Uri, Position, Range, Location,
    commands: {
        registerCommand(name, handler) { handlers.set(name, handler); return {}; },
        executeCommand(...args) { executed = args; return Promise.resolve(); },
    },
    workspace: {
        getConfiguration() { return { get() {} }; },
        createFileSystemWatcher() { return {}; },
        onDidChangeTextDocument() { return {}; },
        onDidCloseTextDocument() { return {}; },
    },
    window: { showErrorMessage(message) { throw new Error(message); } },
};
const moduleScope = { exports: {} };
vm.runInNewContext(fs.readFileSync(process.argv[2], 'utf8'), {
    module: moduleScope, process: { env: {} },
    require(name) {
        if (name === 'vscode') return vscode;
        if (name === './devtools') return { activate() {} };
        if (name === 'vscode-languageclient/node') return { TransportKind: { stdio: 0 }, LanguageClient: class { async start() {} } };
        if (name === 'fs') return { existsSync() { return true; } };
        if (name === 'path') return path;
        throw new Error('Unexpected dependency: ' + name);
    },
});
(async () => {
    await moduleScope.exports.activate({ asAbsolutePath: value => value, subscriptions: [] });
    const reference = { uri: 'file:///C:/project/Page.nazor', range: { start: { line: 4, character: 3 }, end: { line: 4, character: 9 } } };
    await handlers.get('nazor.showNssReferences')('file:///C:/project/Page.nazor.nss', { line: 2, character: 0 }, [reference]);
    assert.equal(executed[0], 'editor.action.showReferences');
    assert(executed[1] instanceof Uri);
    assert.equal(executed[1].value, 'file:///C:/project/Page.nazor.nss');
    assert(executed[2] instanceof Position);
    assert.equal(executed[2].line, 2);
    assert(executed[3][0] instanceof Location);
    assert.equal(executed[3][0].uri.value, reference.uri);
    assert.equal(executed[3][0].range.start.line, 4);
    assert.equal(executed[3][0].range.start.character, 3);
    assert.equal(executed[3][0].range.end.character, 9);
    console.log('PASS: VS Code opens native NSS reference navigation with converted URI, position and source ranges.');
})().catch(error => { console.error(error); process.exitCode = 1; });
