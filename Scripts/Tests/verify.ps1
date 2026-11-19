param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$SkipSdk,
    [switch]$RealInput,
    [switch]$HotReload
)
$ErrorActionPreference = 'Stop'
$previousRealInput = $env:NAZOR_SENDINPUT_TESTS
if ($RealInput) { $env:NAZOR_SENDINPUT_TESTS = '1' }
$root = (Resolve-Path "$PSScriptRoot/../..").Path
Push-Location $root
try {
    if (-not $IsWindows -or [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture -ne 'X64') {
        throw 'The verification baseline requires PowerShell 7 on Windows x64.'
    }
    & dotnet build Nazor.slnx --configuration $Configuration -m:1 -p:UseSharedCompilation=false -nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    & dotnet build Workspace/Nazor.Workspace --configuration $Configuration -m:1 -p:UseSharedCompilation=false -nodeReuse:false
    if ($LASTEXITCODE -ne 0) { throw 'Workspace build failed.' }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    $vsMSBuild = if (Test-Path $vswhere) { & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1 }
    if ($vsMSBuild) {
        $other = if ($Configuration -eq 'Release') { 'Debug' } else { 'Release' }
        & $vsMSBuild Nazor.slnx -restore "-p:Configuration=$other" -m -nologo -nodeReuse:false -v:m
        if ($LASTEXITCODE -ne 0) { throw "Build with Visual Studio MSBuild failed ($vsMSBuild)." }
    }
    else { Write-Host 'Visual Studio MSBuild was not found: skipping the Visual Studio solution build.' -ForegroundColor Yellow }
    foreach ($tests in 'Tests/Nazor.Core.Tests', 'Tests/Nazor.Style.Tests', 'Tests/Nazor.Layout.Tests', 'Tests/Nazor.Compiler.Tests', 'Tests/Nazor.Controls.Tests', 'Tests/Nazor.Rendering.Tests', 'Tests/Nazor.Diagnostics.Tests', 'Tests/Nazor.DevTools.Agent.Tests', 'Tests/Nazor.DevTools.Tests', 'Tests/Nazor.Application.Tests', 'Tests/Nazor.Linux.Tests', 'Tests/Nazor.Tooling.Tests', 'Tests/Nazor.LanguageServer.Tests') {
        & dotnet run --project $tests --configuration $Configuration --no-build
        if ($LASTEXITCODE -ne 0) { throw "Tests failed: $tests" }
    }
    foreach ($framework in 'net8.0', 'net9.0', 'net10.0') {
        & dotnet run --project Tests/Nazor.DevTools.Protocol.Tests --configuration $Configuration --framework $framework --no-build
        if ($LASTEXITCODE -ne 0) { throw "Protocol tests failed: $framework" }
    }
    $benchmarkArguments = @('--check', 'Scripts/Tests/benchmarks.json', '--out', 'artifacts/benchmarks.json')
    if ($Configuration -eq 'Debug') { $benchmarkArguments += '--counters-only' }
    & dotnet run --project Tests/Nazor.Benchmarks --configuration $Configuration --no-build -- @benchmarkArguments
    if ($LASTEXITCODE -ne 0) { throw 'Benchmark thresholds exceeded.' }
    & dotnet run --project Samples/Nazor.Samples.Counter --configuration $Configuration --no-build --no-launch-profile -- --headless --png artifacts/counter
    if ($LASTEXITCODE -ne 0) { throw 'Counter failed.' }
    & "$PSScriptRoot/verify-samples.ps1" -Configuration $Configuration -SkipBuild
    & "$root/Scripts/Build/build-editors.ps1"
    if (-not $SkipSdk -or $HotReload) { & "$PSScriptRoot/verify-sdk.ps1" }
    if ($HotReload) { & "$PSScriptRoot/verify-hotreload.ps1" -SkipPack }
} finally {
    $env:NAZOR_SENDINPUT_TESTS = $previousRealInput
    Pop-Location
}
