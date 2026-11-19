#Requires -Version 7
param(
    [Parameter(Mandatory)][string]$Workspace,
    [Parameter(Mandatory)][string]$Feed,
    [ValidateSet('net8.0', 'net9.0', 'net10.0', 'net11.0')][string]$Framework = 'net10.0',
    [ValidateSet('single', 'trim', 'trim-single', 'aot')][string[]]$Modes = @('single', 'trim', 'trim-single', 'aot')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$Workspace = [IO.Path]::GetFullPath($Workspace)
$Feed = [IO.Path]::GetFullPath($Feed)
if (Test-Path $Workspace) { throw 'Choose a fresh workspace for package cache isolation.' }
New-Item -ItemType Directory "$Workspace/project" | Out-Null
if ($Framework -eq 'net11.0') {
    '{ "sdk": { "version": "11.0.100", "rollForward": "latestPatch", "allowPrerelease": false } }' | Set-Content "$Workspace/global.json"
}
foreach ($file in 'Directory.Build.props','Directory.Build.targets','Directory.Packages.props') { '<Project />' | Set-Content "$Workspace/$file" }
$version = (Select-Xml -Path "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
@'
<Project Sdk="Nazor.Sdk/VERSION">
  <PropertyGroup><TargetFramework>$(NazorDeploymentFramework)</TargetFramework><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.deployment</NazorApplicationId><RootNamespace>Deployment</RootNamespace><AssemblyName>Deployment</AssemblyName><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup>
  <ItemGroup><None Update="payload.txt" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" /></ItemGroup>
  <ItemGroup><PackageReference Include="Nazor.Themes.NazorLeaf" Version="VERSION" /></ItemGroup>
</Project>
'@.Replace('VERSION', $version) | Set-Content "$Workspace/project/Deployment.csproj"
foreach ($file in 'Program.cs','Probe.nazor','payload.txt') { Copy-Item "$root/Tests/Fixtures/Deployment/$file" "$Workspace/project/$file" }
$escapedFeed = [Security.SecurityElement]::Escape($Feed)
$escapedCache = [Security.SecurityElement]::Escape("$Workspace/.packages")
$fallback = [Security.SecurityElement]::Escape("$env:USERPROFILE/.nuget/packages")
@"
<configuration>
  <config><add key="globalPackagesFolder" value="$escapedCache" /></config>
  <packageSources><clear /><add key="nazor" value="$escapedFeed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nazor"><package pattern="Nazor.*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
  <fallbackPackageFolders><clear /><add key="user" value="$fallback" /></fallbackPackageFolders>
</configuration>
"@ | Set-Content "$Workspace/nuget.config"
Push-Location $Workspace
try {
foreach ($mode in $Modes) {
    & "$PSScriptRoot/verify-deployment.ps1" -Mode $mode -Framework $Framework -Project "$Workspace/project/Deployment.csproj" -Output "$Workspace/output"
}
}
finally { Pop-Location }
