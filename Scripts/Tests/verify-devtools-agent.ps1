#Requires -Version 7
param([string]$Workspace = (Join-Path (Resolve-Path "$PSScriptRoot/../..").Path ('artifacts/devtools-dt06/' + [guid]::NewGuid().ToString('N'))))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$Workspace = [IO.Path]::GetFullPath($Workspace)
if (Test-Path $Workspace) { throw "Choose a fresh workspace: $Workspace" }
New-Item -ItemType Directory $Workspace | Out-Null
$script:step = 0
function Invoke-Checked([string[]]$Arguments) {
    $script:step++
    $output = & dotnet @Arguments 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    $output | Set-Content "$Workspace/$('{0:D2}' -f $script:step).log"
    if ($exitCode -ne 0) { throw "dotnet $Arguments failed: $output" }
    Write-Host $output
}
Invoke-Checked @('build', "$root/Tests/Fixtures/DevToolsAgent", '-c', 'Release', '-nodeReuse:false', '-v:q')
Invoke-Checked @('build', "$root/Tests/Nazor.DevTools.Agent.Tests", '-c', 'Release', '-nodeReuse:false', '-v:q')
$previous = $env:NAZOR_DEVTOOLS_FIXTURE
try {
    foreach ($framework in 'net8.0', 'net9.0', 'net10.0') {
        $env:NAZOR_DEVTOOLS_FIXTURE = "$root/Tests/Fixtures/DevToolsAgent/bin/Release/$framework/DevToolsAgent.dll"
        Invoke-Checked @('run', '--project', "$root/Tests/Nazor.DevTools.Agent.Tests", '-c', 'Release', '--no-build')
    }
}
finally { $env:NAZOR_DEVTOOLS_FIXTURE = $previous }
Write-Host "PASS: DT-06 separate-process Agent matrix. Evidence: $Workspace"
