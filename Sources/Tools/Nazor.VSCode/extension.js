'use strict';

const path = require('path');
const fs = require('fs');
const vscode = require('vscode');
const { LanguageClient, TransportKind } = require('vscode-languageclient/node');

let client;

async function activate(context) {
    require('./devtools').activate(context);
    context.subscriptions.push(vscode.commands.registerCommand('nazor.showNssReferences', (uri, position, references) => {
        const at = p => new vscode.Position(p.line, p.character);
        const locations = references.map(reference => new vscode.Location(vscode.Uri.parse(reference.uri),
            new vscode.Range(at(reference.range.start), at(reference.range.end))));
        return vscode.commands.executeCommand('editor.action.showReferences', vscode.Uri.parse(uri), at(position), locations);
    }));
    const server = context.asAbsolutePath(path.join('server', 'Nazor.LanguageServer.dll'));
    if (!fs.existsSync(server)) {
        vscode.window.showErrorMessage(`Nazor: the language server was not found (${server}).`);
        return;
    }
    const configured = vscode.workspace.getConfiguration('nazor').get('dotnetPath');
    const dotnet = configured || process.env.DOTNET_HOST_PATH || 'dotnet';
    const run = { command: dotnet, args: [server], transport: TransportKind.stdio };
    const configurationWatcher = vscode.workspace.createFileSystemWatcher('**/{.editorconfig,*.globalconfig,nazorc.rsp,nazorc.roslyn.rsp,nazor.stamp,*.dll,*.nazor,*.nss}');
    context.subscriptions.push(configurationWatcher);
    client = new LanguageClient('nazor', 'Nazor', { run, debug: run }, {
        synchronize: { fileEvents: configurationWatcher },
        documentSelector: [
            { scheme: 'file', language: 'nazor' },
            { scheme: 'file', language: 'nss' },
        ],
        middleware: {
            async provideCodeActions(document, range, context, token, next) {
                await synchronizeCSharp(document.uri.fsPath);
                return next(document, range, context, token);
            },
        },
    });
    try {
        await client.start();
        context.subscriptions.push(vscode.workspace.onDidChangeTextDocument(event => {
            if (event.document.uri.scheme === 'file' && event.document.uri.fsPath.endsWith('.cs'))
                synchronizeCSharp(event.document.uri.fsPath).catch(error => client.error('C# synchronization failed', error));
        }));
        context.subscriptions.push(vscode.workspace.onDidCloseTextDocument(document => {
            if (document.uri.scheme === 'file' && document.uri.fsPath.endsWith('.cs'))
                synchronizeCSharp(document.uri.fsPath).catch(error => client.error('C# synchronization failed', error));
        }));
    } catch (error) {
        vscode.window.showErrorMessage(`Nazor: the language server did not start with '${dotnet}' (${error.message}). Set nazor.dotnetPath to a dotnet installation with the .NET 10 runtime.`);
    }
}

async function synchronizeCSharp(file) {
    let directory = path.dirname(file);
    while (!fs.readdirSync(directory).some(name => name.endsWith('.csproj'))) {
        const parent = path.dirname(directory);
        if (parent === directory) return;
        directory = parent;
    }
    const documents = vscode.workspace.textDocuments
        .filter(document => document.uri.scheme === 'file' && document.uri.fsPath.endsWith('.cs') &&
            !path.relative(directory, document.uri.fsPath).startsWith('..') &&
            !path.relative(directory, document.uri.fsPath).split(path.sep).some(part => part === 'obj' || part === 'bin'))
        .map(document => ({ path: document.uri.fsPath, text: document.getText() }));
    await client.sendNotification('nazor/csharpDocuments', { projectDirectory: directory, documents });
}

function deactivate() {
    return client ? client.stop() : undefined;
}

module.exports = { activate, deactivate };
