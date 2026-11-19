#Requires -Version 7
param([string]$Workspace = (Join-Path (Resolve-Path "$PSScriptRoot/../..").Path ('artifacts/devtools-dt04/' + [guid]::NewGuid().ToString('N'))))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$Workspace = [IO.Path]::GetFullPath($Workspace)
if (Test-Path $Workspace) { throw "Choose a fresh workspace: $Workspace" }
New-Item -ItemType Directory $Workspace | Out-Null
$script:step = 0
function Invoke-Checked([string]$Command, [string[]]$Arguments) {
    $script:step++
    $output = & $Command @Arguments 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    $output | Set-Content "$Workspace/$('{0:D2}' -f $script:step).log"
    if ($exitCode -ne 0) { throw "$Command $Arguments failed: $output" }
    Write-Host $output
}
foreach ($framework in 'net8.0', 'net9.0', 'net10.0') {
    Invoke-Checked dotnet @('run', '--project', "$root/Tests/Nazor.DevTools.Protocol.Tests", '-c', 'Release', '-f', $framework)
}
Invoke-Checked dotnet @('build', "$root/Tests/Fixtures/DevToolsProtocol", '-c', 'Release', '-nodeReuse:false', '-v:q')
$fixture = "$root/Tests/Fixtures/DevToolsProtocol/bin/Release/net10.0/DevToolsProtocol.dll"
foreach ($framework in 'net8.0', 'net9.0', 'net10.0') {
    Invoke-Checked node @("$PSScriptRoot/verify-devtools-protocol.mjs", "$root/Tests/Fixtures/DevToolsProtocol/bin/Release/$framework/DevToolsProtocol.dll")
}
if ($IsWindows) {
    Invoke-Checked dotnet @('build', "$root/Tests/Fixtures/DevToolsProtocol.NetFramework", '-c', 'Release', '-nodeReuse:false', '-v:q')
    Invoke-Checked dotnet @($fixture, '--emit', "$Workspace/snapshot.json")
    Invoke-Checked "$root/Tests/Fixtures/DevToolsProtocol.NetFramework/bin/Release/net472/DevToolsProtocol.NetFramework.exe" @("$Workspace/snapshot.json", "$Workspace/netframework.json")
    Invoke-Checked dotnet @($fixture, '--check', "$Workspace/netframework.json")
}
else { Write-Host 'SKIP: the .NET Framework 4.7.2 fixture requires Windows.' }
Write-Host "PASS: DT-04 protocol interoperability. Evidence: $Workspace"
