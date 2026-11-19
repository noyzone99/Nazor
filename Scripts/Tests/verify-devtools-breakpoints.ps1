#Requires -Version 7
param(
    [ValidateSet('2022', '2026')][string]$VisualStudio = '2026',
    [ValidateSet('net8.0', 'net10.0')][string]$FixtureFramework = 'net10.0',
    [ValidatePattern('^DevToolsDT01[A-Za-z0-9]*$')][string]$RootSuffix = 'DevToolsDT01',
    [string]$Output,
    [switch]$Native
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $IsWindows) { throw 'The Visual Studio debugger spike requires Windows.' }
if ($VisualStudio -eq '2022' -and $FixtureFramework -ne 'net8.0') { throw 'Use net8.0 for the Visual Studio 2022 fixture.' }
if (-not $Output) { $Output = Join-Path $root "artifacts/devtools-dt01/vs$VisualStudio-$FixtureFramework" }
$Output = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Path $Output -Force | Out-Null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$version = if ($VisualStudio -eq '2022') { '[17.0,18.0)' } else { '[18.0,19.0)' }
$installation = & $vswhere -latest -version $version -requires Microsoft.VisualStudio.Workload.VisualStudioExtension -property installationPath
if (-not $installation) { throw "Visual Studio $VisualStudio with extension development tools was not found." }
$ide = Join-Path $installation 'Common7/IDE'
$msbuild = Join-Path $installation 'MSBuild/Current/Bin/MSBuild.exe'
$fixture = Join-Path $root 'Tests/Fixtures/DevToolsBreakpoint/DevToolsBreakpoint.csproj'
$client = Join-Path $root 'Tests/Nazor.VisualStudio.Debugger.Tests/Nazor.VisualStudio.Debugger.Tests.csproj'

$fixtureArtifacts = Join-Path $Output 'fixture-managed'
$clientArtifacts = Join-Path $Output 'client-managed'
& dotnet build $fixture -c Debug "-p:NazorBreakpointTargetFramework=$FixtureFramework" --artifacts-path $fixtureArtifacts --nologo *> (Join-Path $Output 'fixture-build.log')
if ($LASTEXITCODE -ne 0) { throw "The breakpoint fixture build failed; see $Output/fixture-build.log." }
& dotnet build $client -c Release "-p:VisualStudioIde=$ide" --artifacts-path $clientArtifacts --nologo *> (Join-Path $Output 'client-build.log')
if ($LASTEXITCODE -ne 0) { throw "The external debugger client build failed; see $Output/client-build.log." }
$extension = Join-Path $root 'Sources/Tools/Nazor.VisualStudio/Nazor.VisualStudio.csproj'
$restore = @('/restore')
if ($VisualStudio -eq '2022') {
    & dotnet restore $extension -p:VisualStudioVersion=17.0 -r win *> (Join-Path $Output 'extension-restore.log')
    if ($LASTEXITCODE -ne 0) { throw "The Visual Studio 2022 extension restore failed; see $Output/extension-restore.log." }
    $restore = @()
}
& $msbuild $extension @restore /p:Configuration=Release /p:DeployExtension=true "/p:VSSDKTargetPlatformRegRootSuffix=$RootSuffix" /v:minimal /nologo /nodeReuse:false *> (Join-Path $Output 'extension-build.log')
if ($LASTEXITCODE -ne 0) { throw "The experimental Visual Studio extension deployment failed; see $Output/extension-build.log." }

if ($Native) {
    & dotnet build (Join-Path $root 'Sources/Tools/Nazor.DevTools') -c Release --artifacts-path (Join-Path $root 'artifacts/devtools-host') -m:1 -nodeReuse:false -v:q *> (Join-Path $Output 'inspector-build.log')
    if ($LASTEXITCODE -ne 0) { throw 'The actual inspector build failed.' }
}
$oldNative = $env:NAZOR_DEVTOOLS_NATIVE_PROBE
$oldUi = $env:NAZOR_DEVTOOLS_UI_PROBE
$oldF12 = $env:NAZOR_DEVTOOLS_NATIVE_F12
try {
    if ($Native) { $env:NAZOR_DEVTOOLS_NATIVE_PROBE = '1'; $env:NAZOR_DEVTOOLS_UI_PROBE = '1'; $env:NAZOR_DEVTOOLS_NATIVE_F12 = '1' }
    $fixtureExe = Get-ChildItem (Join-Path $fixtureArtifacts 'bin/DevToolsBreakpoint') -Recurse -Filter DevToolsBreakpoint.exe | Select-Object -First 1 -ExpandProperty FullName
    $clientExe = Get-ChildItem (Join-Path $clientArtifacts 'bin/Nazor.VisualStudio.Debugger.Tests') -Recurse -Filter Nazor.VisualStudio.Debugger.Tests.exe | Select-Object -First 1 -ExpandProperty FullName
    & $clientExe $fixtureExe $Output (Join-Path $ide 'devenv.exe') $RootSuffix
}
finally { $env:NAZOR_DEVTOOLS_NATIVE_PROBE = $oldNative; $env:NAZOR_DEVTOOLS_UI_PROBE = $oldUi; $env:NAZOR_DEVTOOLS_NATIVE_F12 = $oldF12 }
if ($LASTEXITCODE -ne 0) { throw "Breakpoint verification failed; see $Output/evidence.json." }
Write-Host "PASS: Visual Studio $VisualStudio / $FixtureFramework. Evidence: $Output/evidence.json"
