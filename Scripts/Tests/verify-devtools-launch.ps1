#Requires -Version 7
param(
    [Parameter(Mandatory)][string]$Workspace,
    [ValidateSet('net8.0', 'net9.0', 'net10.0', 'net11.0')][string]$Framework = 'net10.0',
    [string]$Feed = (Join-Path (Resolve-Path "$PSScriptRoot/../..").Path 'artifacts/packages'),
    [switch]$Production
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$Feed = [IO.Path]::GetFullPath($Feed)
$Workspace = [IO.Path]::GetFullPath($Workspace)
if (Test-Path $Workspace) { throw 'Choose a fresh consumer directory.' }
New-Item -ItemType Directory "$Workspace/Probe" -Force | Out-Null
if ($Framework -eq 'net11.0') {
    '{ "sdk": { "version": "11.0.100", "rollForward": "latestPatch", "allowPrerelease": false } }' | Set-Content "$Workspace/global.json"
}
foreach ($name in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') { '<Project />' | Set-Content "$Workspace/$name" }
$version = (Select-Xml -Path "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
$enabled = if ($Production) { 'false' } else { 'true' }
@"
<Project Sdk="Nazor.Sdk/$version">
  <PropertyGroup><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId><TargetFramework>$Framework</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><RootNamespace>DevToolsLaunch</RootNamespace><NazorEnableDevTools>$enabled</NazorEnableDevTools></PropertyGroup>
</Project>
"@ | Set-Content "$Workspace/Probe/Probe.csproj"
@"
<configuration>
  <config><add key="globalPackagesFolder" value="$Workspace/.packages" /></config>
  <packageSources><clear /><add key="nazor" value="$Feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nazor"><package pattern="Nazor.*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
  <fallbackPackageFolders><clear /></fallbackPackageFolders>
</configuration>
"@ | Set-Content "$Workspace/nuget.config"
Copy-Item "$root/Tests/Fixtures/DevToolsLaunch/Program.cs" "$Workspace/Probe/Program.cs"
$arguments = @('publish', "$Workspace/Probe/Probe.csproj", '-c', 'Release', '-o', "$Workspace/output", '-m:1', '-nodeReuse:false', '-v:q')
if ($Production) { $arguments += '-p:PublishProfile=NazorProduction' }
else { $arguments += '-p:NazorEnablePublishValidation=false' }
Push-Location $Workspace
try {
    & dotnet @arguments *> "$Workspace/build.log"
    if ($LASTEXITCODE -ne 0) { throw "Consumer build failed: $Workspace/build.log" }
    $apphost = Join-Path "$Workspace/output" $(if ($IsWindows) { 'Probe.exe' } else { 'Probe' })
    & node "$root/Scripts/Tests/verify-devtools-launch.mjs" $apphost "$Workspace/runtime/gate" $(if ($Production) { 'production' } else { 'development' })
    if ($LASTEXITCODE -ne 0) { throw "Native launch verification failed: $Workspace/runtime/gate.evidence.json" }
}
finally { Pop-Location }
