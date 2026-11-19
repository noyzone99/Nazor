#Requires -Version 7
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$sdk = Join-Path $root 'Sources/Tools/Nazor.Sdk/Sdk'
$workspace = Join-Path $root 'artifacts/local-sdk-tests'
New-Item -ItemType Directory -Force $workspace | Out-Null
# Isolate the probe from repository defaults so the SDK must supply them.
'<Project />' | Set-Content "$workspace/Directory.Build.props"

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw "Check failed: $message" }
    Write-Host "    ok: $message"
}

function Read-Project([string]$project, [string[]]$properties = @(), [string]$probeConfiguration = $Configuration) {
    $configurationArgument = @()
    if ($probeConfiguration) { $configurationArgument += "-p:Configuration=$probeConfiguration" }
    $output = & dotnet msbuild $project -nologo @configurationArgument @properties `
        -getProperty:TargetFramework,TargetFrameworks,ImplicitUsings,Nullable,NazorRepositoryRoot,NazorToolchainSdkVersion,NazorLanguageServicesSdkVersion,NazorRoslynDirectory,NazorToolSupportDirectory,ApplicationManifest,ApplicationIcon,RuntimeIdentifier,NazorEnablePublishValidation,IncludeAllContentForSelfExtract,IncludeNativeLibrariesForSelfExtract `
        -getItem:ProjectReference,PackageReference,Using 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw $output }
    return $output | ConvertFrom-Json
}

function Write-Probe([string]$sdkPath, [string]$properties) {
    @"
<Project>
  <Import Project="$sdkPath/Sdk.props" />
  <PropertyGroup>$properties</PropertyGroup>
  <Import Project="$sdkPath/Sdk.targets" />
</Project>
"@ | Set-Content "$workspace/Probe.csproj"
}

function Check-PublishValidation([string]$sdkPath) {
    Write-Probe $sdkPath '<OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>'
    $probe = Join-Path $workspace 'Probe.csproj'
    foreach ($case in @(
        @{ Configuration = 'Debug'; Properties = @(); Expected = 'false' },
        @{ Configuration = 'Release'; Properties = @(); Expected = 'true' },
        @{ Configuration = 'Custom'; Properties = @(); Expected = 'false' },
        @{ Configuration = 'Release'; Properties = @('-p:NazorEnablePublishValidation=false'); Expected = 'false' },
        @{ Configuration = 'Debug'; Properties = @('-p:NazorEnablePublishValidation=true'); Expected = 'true' },
        @{ Configuration = 'Release'; Properties = @('-p:NazorEnableDevTools=true'); Expected = 'true' },
        @{ Configuration = 'Debug'; Properties = @('-p:PublishProfile=NazorProduction'); Expected = 'true' }
    )) {
        $selection = Read-Project $probe $case.Properties $case.Configuration
        Assert ($selection.Properties.NazorEnablePublishValidation -eq $case.Expected) "publish validation defaults/overrides: $($case.Configuration) $($case.Properties -join ' ') -> $($case.Expected)"
    }
    Write-Probe $sdkPath '<OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId><NazorEnablePublishValidation>false</NazorEnablePublishValidation>'
    $selection = Read-Project $probe @() 'Release'
    Assert ($selection.Properties.NazorEnablePublishValidation -eq 'false') 'a project can explicitly disable Release validation'
    $selection = Read-Project $probe @('-p:NazorEnablePublishValidation=true') 'Release'
    Assert ($selection.Properties.NazorEnablePublishValidation -eq 'true') 'the command line takes precedence over the project setting'
    Write-Probe $sdkPath '<OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>'
    $profileDirectory = Join-Path $workspace 'Properties/PublishProfiles'
    New-Item -ItemType Directory -Force $profileDirectory | Out-Null
    '<Project><PropertyGroup><NazorEnablePublishValidation>false</NazorEnablePublishValidation></PropertyGroup></Project>' | Set-Content "$profileDirectory/Development.pubxml"
    $selection = Read-Project $probe @('-p:PublishProfile=Development') 'Release'
    Assert ($selection.Properties.NazorEnablePublishValidation -eq 'false') 'a development publish profile can override the Release default'
    '<Project><PropertyGroup><Configuration>Release</Configuration></PropertyGroup></Project>' | Set-Content "$profileDirectory/Release.pubxml"
    $selection = Read-Project $probe @('-p:PublishProfile=Release') ''
    Assert ($selection.Properties.NazorEnablePublishValidation -eq 'true') 'a profile selecting Release receives the Release validation default'
}

function Check-SingleFileDefaults([string]$sdkPath) {
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>' | Set-Content "$workspace/Standard.csproj"
    $standard = Read-Project "$workspace/Standard.csproj" @('-p:PublishSingleFile=true')
    Write-Probe $sdkPath '<OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>'
    $probe = Join-Path $workspace 'Probe.csproj'
    $selection = Read-Project $probe @('-p:PublishSingleFile=true')
    foreach ($name in 'IncludeAllContentForSelfExtract', 'IncludeNativeLibrariesForSelfExtract') {
        Assert ($selection.Properties.$name -eq $standard.Properties.$name) "single-file $name keeps the standard .NET default"
    }
    $selection = Read-Project $probe @('-p:PublishSingleFile=true', '-p:IncludeAllContentForSelfExtract=true', '-p:IncludeNativeLibrariesForSelfExtract=true')
    Assert ($selection.Properties.IncludeAllContentForSelfExtract -eq 'true' -and $selection.Properties.IncludeNativeLibrariesForSelfExtract -eq 'true') 'complete extraction remains an explicit application opt-in'
}

$projects = @(Get-ChildItem "$root/Samples" -Recurse -Filter '*.csproj') + @(Get-Item "$root/Workspace/Nazor.Workspace/Nazor.Workspace.csproj")
foreach ($project in $projects) {
    $selection = Read-Project $project.FullName
    Assert ($selection.Properties.NazorRepositoryRoot -eq "$root$([IO.Path]::DirectorySeparatorChar)") "$($project.BaseName) uses the local SDK"
    Assert ($selection.Items.ProjectReference.Filename -contains 'Nazor.Application' -and
        $selection.Items.ProjectReference.Filename -contains 'Nazor.Hosting.Desktop') "$($project.BaseName) receives the source runtime and desktop host automatically"
    Assert (-not ($selection.Items.PackageReference.Identity -like 'Nazor.*')) "$($project.BaseName) needs no Nazor packages"
    Assert ($selection.Items.Using.Identity -contains 'Nazor.Controls' -and
        @($selection.Items.Using | Where-Object { $_.Identity -eq 'Nazor.Style.Color' -and $_.Alias -eq 'Color' }).Count -eq 1) "$($project.BaseName) receives SDK usings and the Color alias"
}

$compiler = Read-Project "$root/Sources/Tools/Nazor.Compiler/Nazor.Compiler.csproj"
$sdkVersion = (Get-Content "$root/global.json" -Raw | ConvertFrom-Json).sdk.version
Assert ($compiler.Properties.NazorToolchainSdkVersion -eq $sdkVersion -and $compiler.Properties.TargetFramework -eq 'net10.0') 'the source SDK pin and LTS compiler assets agree'
Assert ($compiler.Properties.NazorLanguageServicesSdkVersion -eq '11.0.100' -and $compiler.Properties.NazorRoslynDirectory -match '11\.0\.100') 'C# 15 language services have an independent fixed SDK source'
Assert ($compiler.Properties.NazorToolSupportDirectory -match '10\.0\.404') 'language-service support dependencies remain compatible with the LTS runtime'

Check-PublishValidation $sdk
Check-SingleFileDefaults $sdk
Write-Probe $sdk '<OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>'
$probe = Join-Path $workspace 'Probe.csproj'
$selection = Read-Project $probe
Assert ($selection.Properties.TargetFramework -eq 'net10.0' -and $selection.Properties.ImplicitUsings -eq 'enable' -and
    $selection.Properties.Nullable -eq 'enable') 'the local SDK supplies defaults without Directory.Build.props'
foreach ($variant in @(
    @{ Properties = @(); Host = 'Nazor.Hosting.Desktop' },
    @{ Properties = @('-p:RuntimeIdentifier=win-x64'); Host = 'Nazor.Hosting.Desktop.Windows' },
    @{ Properties = @('-p:RuntimeIdentifier=linux-x64'); Host = 'Nazor.Hosting.Desktop.Linux' }
)) {
    $selection = Read-Project $probe $variant.Properties
    $hosts = @($selection.Items.ProjectReference | Where-Object Filename -Like 'Nazor.Hosting.*')
    Assert ($hosts.Count -eq 1 -and $hosts[0].Filename -eq $variant.Host) "RID selection uses only $($variant.Host)"
}
$profileDirectory = Join-Path $workspace 'Properties/PublishProfiles'
New-Item -ItemType Directory -Force $profileDirectory | Out-Null
'<Project><PropertyGroup><RuntimeIdentifier>linux-x64</RuntimeIdentifier></PropertyGroup></Project>' | Set-Content "$profileDirectory/Linux.pubxml"
$selection = Read-Project $probe @('-p:PublishProfile=Linux')
Assert ($selection.Items.ProjectReference.Filename -contains 'Nazor.Hosting.Desktop.Linux') 'a publish profile selects the source backend before restore'
$selection = Read-Project $probe @('-p:NazorEnableDevTools=true')
Assert ($selection.Items.ProjectReference.Filename -contains 'Nazor.DevTools.Agent' -and
    -not ($selection.Items.PackageReference.Identity -like 'Nazor.*')) 'DevTools also comes from source'
$selection = Read-Project $probe @('-p:ImplicitUsings=disable')
Assert (-not ($selection.Items.Using.Identity -contains 'Nazor')) 'ImplicitUsings=disable removes SDK namespace imports'
$selection = Read-Project $probe @('-p:NazorImplicitRuntimeReference=false')
Assert (-not ($selection.Items.ProjectReference.Filename -contains 'Nazor.Application') -and
    -not ($selection.Items.ProjectReference.Filename -like 'Nazor.Hosting.*') -and
    -not ($selection.Items.Using.Identity -contains 'Nazor')) 'the runtime opt-out removes automatic runtime, host and usings'

# A package contains Sdk/ without the neighboring source .csproj: it must keep using packages.
$packagedSdk = Join-Path $workspace 'package/Sdk'
New-Item -ItemType Directory -Force $packagedSdk | Out-Null
Copy-Item "$sdk/*" $packagedSdk -Recurse -Force
'<Project><PropertyGroup><NazorSdkVersion>0.1.0-preview.1</NazorSdkVersion></PropertyGroup></Project>' | Set-Content "$packagedSdk/Nazor.Sdk.Version.props"
Check-PublishValidation $packagedSdk
Check-SingleFileDefaults $packagedSdk
Write-Probe $packagedSdk '<OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>'
$selection = Read-Project $probe
Assert ($selection.Properties.NazorRepositoryRoot -eq '' -and $selection.Items.ProjectReference.Count -eq 0 -and
    $selection.Items.PackageReference.Identity -contains 'Nazor.Application' -and
    $selection.Items.PackageReference.Identity -contains 'Nazor.Hosting.Desktop') 'the packaged SDK keeps its NuGet references'

Write-Probe $sdk '<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>'
$selection = Read-Project $probe
Assert ($selection.Properties.TargetFramework -eq '' -and
    -not ($selection.Items.ProjectReference.Filename -contains 'Nazor.Application')) 'multitargeting does not add a runtime in the outer build'
foreach ($framework in 'net8.0', 'net9.0', 'net10.0') {
    $selection = Read-Project $probe @("-p:TargetFramework=$framework")
    Assert ($selection.Items.ProjectReference.Filename -contains 'Nazor.Application' -and
        -not ($selection.Items.ProjectReference.Filename -like 'Nazor.Hosting.*') -and
        $selection.Properties.ApplicationManifest -eq '' -and $selection.Properties.ApplicationIcon -eq '') "$framework libraries receive runtime and usings without desktop hosting"
}
'public sealed class SharedComponent : ComponentBase { public Button? Control { get; set; } public Color Accent => Color.FromRgb(0, 120, 215); }' | Set-Content "$workspace/Program.cs"
& dotnet build $probe -c $Configuration -m:1 -nodeReuse:false -p:UseSharedCompilation=false -v:q
if ($LASTEXITCODE -ne 0) { throw 'Local SDK multitargeted library build failed.' }
'return NazorApplication.CreateBuilder().Platform.Host is null ? 1 : 0;' | Set-Content "$workspace/Program.cs"
foreach ($framework in 'net8.0', 'net9.0', 'net10.0') {
    Write-Probe $sdk "<TargetFramework>$framework</TargetFramework><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>"
    & dotnet build $probe -c $Configuration -m:1 -nodeReuse:false -p:UseSharedCompilation=false -v:q
    if ($LASTEXITCODE -ne 0) { throw "Local SDK $framework application build failed." }
    & dotnet "$workspace/bin/$Configuration/$framework/Probe.dll"
    Assert ($LASTEXITCODE -eq 0) "the .NET 10 compiler builds a $framework application with its desktop host"
}
& "$PSScriptRoot/verify-application-metadata.ps1"
Write-Host 'Local SDK verification passed.' -ForegroundColor Green
