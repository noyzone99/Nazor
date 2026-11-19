#Requires -Version 7
param(
    [string]$Feed,
    [switch]$NativeAot
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$workspace = Join-Path $root ('artifacts/application-metadata/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force "$workspace/project/Platforms/Linux" | Out-Null
foreach ($file in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') {
    '<Project />' | Set-Content "$workspace/$file"
}
$project = "$workspace/project/IdentityProbe.csproj"
$id = 'org.example.identityprobe'
$desktop = "Platforms/Linux/$id.desktop"
$sdk = "$root/Sources/Tools/Nazor.Sdk/Sdk"
$version = (Select-Xml -Path "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
if ($Feed) {
    $Feed = [IO.Path]::GetFullPath($Feed)
    $escapedFeed = [Security.SecurityElement]::Escape($Feed)
    $fallback = [Security.SecurityElement]::Escape("$env:USERPROFILE/.nuget/packages")
    @"
<configuration>
  <config><add key="globalPackagesFolder" value="$workspace/.packages" /></config>
  <packageSources><clear /><add key="nazor" value="$escapedFeed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nazor"><package pattern="Nazor.*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
  <fallbackPackageFolders><clear /><add key="user" value="$fallback" /></fallbackPackageFolders>
</configuration>
"@ | Set-Content "$workspace/nuget.config"
}
function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw "Check failed: $message" }
    Write-Host "    ok: $message"
}
function Invoke-Probe([string[]]$arguments, [string]$expectedError = '') {
    if ($arguments[0] -in 'build', 'publish', 'msbuild') { $arguments += '-p:NuGetAudit=false', '-m:1', '-p:UseSharedCompilation=false', '-nodeReuse:false' }
    $output = & dotnet @arguments 2>&1 | Out-String
    if ($expectedError) {
        if ($LASTEXITCODE -eq 0 -or -not $output.Contains($expectedError)) { throw $output }
        Write-Host "    ok: rejects $expectedError"
    } elseif ($LASTEXITCODE -ne 0) { throw $output }
    return $output
}
function Write-Project([string]$properties = '', [string]$items = '') {
    $start = "<Project><Import Project=`"$sdk/Sdk.props`" />"
    $end = "<Import Project=`"$sdk/Sdk.targets`" /></Project>"
    if ($Feed) { $start = "<Project Sdk=`"Nazor.Sdk/$version`">"; $end = '</Project>' }
    @"
$start
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType>
    <NazorApplicationId>$id</NazorApplicationId><NazorImplicitRuntimeReference>false</NazorImplicitRuntimeReference>
    <NazorEnablePublishValidation>false</NazorEnablePublishValidation><NazorLinuxNativeLibraries>None</NazorLinuxNativeLibraries>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>$properties
  </PropertyGroup>
  <ItemGroup><Compile Include="$root/Sources/Platforms/Nazor.Platform/ApplicationIdentity.cs" Link="ApplicationIdentity.cs" />$items</ItemGroup>
$end
"@ | Set-Content $project
}
@'
using System.Reflection;
using System.Runtime.InteropServices;
if (Nazor.Platform.ApplicationIdentity.Id != "org.example.identityprobe") return 1;
if (Assembly.GetEntryAssembly()!.GetName().Name != "IdentityProbe") return 2;
if (OperatingSystem.IsWindows() && args.Length == 2 && args[0] == "--verify-dpi"
    && !Native.AreDpiAwarenessContextsEqual(Native.GetThreadDpiAwarenessContext(), nint.Parse(args[1]))) return 3;
Console.WriteLine("metadata-ok");
return 0;

internal static class Native
{
    [DllImport("user32.dll")] internal static extern nint GetThreadDpiAwarenessContext();
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AreDpiAwarenessContextsEqual(nint first, nint second);
}
'@ | Set-Content "$workspace/project/Program.cs"
$text = "[Desktop Entry]`nType=Application`nName=Identity probe`nExec=dotnet /opt/probe/IdentityProbe.dll`nX-Custom=preserve me`n"
[IO.File]::WriteAllText("$workspace/project/$desktop", $text, [Text.UTF8Encoding]::new($false))
Copy-Item "$sdk/Nazor.Windows.manifest" "$workspace/project/custom.manifest"
(Get-Content "$workspace/project/custom.manifest" -Raw).Replace('>PerMonitorV2<', '>system<') | Set-Content "$workspace/project/custom.manifest"
Copy-Item "$sdk/Nazor.Windows.manifest" "$workspace/project/other.manifest"
New-Item -ItemType Directory -Force "$workspace/project/Properties/PublishProfiles" | Out-Null
'<Project><PropertyGroup><NazorWindowsAppManifest>custom.manifest</NazorWindowsAppManifest></PropertyGroup></Project>' | Set-Content "$workspace/project/Properties/PublishProfiles/Metadata.pubxml"
Write-Project
$manifestTargets = Join-Path (Split-Path ((Invoke-Probe @('msbuild', $project, '-getProperty:ApplicationManifest', '-nologo')).Trim())) 'Nazor.WindowsManifest.targets'
Assert ((Invoke-Probe @('msbuild', $project, '-getProperty:ApplicationManifest', '-p:PublishProfile=Metadata', '-nologo')).Trim() -eq 'custom.manifest') 'publish profiles can select the Nazor Windows manifest before defaults are applied'
if ($IsWindows) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    $engines = if (Test-Path $vswhere) { @(& $vswhere -all -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/Current/Bin/MSBuild.exe') } else { @() }
    $contract = "$workspace/project/ManifestContract.proj"
    @"
<Project>
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup>
  <Import Project="$manifestTargets" />
</Project>
"@ | Set-Content $contract
    foreach ($engine in $engines) {
        foreach ($settings in @(
            @{ Arguments = @(); Error = '' },
            @{ Arguments = @('-p:NazorWindowsAppManifest=custom.manifest'); Error = '' },
            @{ Arguments = @('-p:NazorWindowsAppManifest=custom.manifest', '-p:ApplicationManifest=./custom.manifest'); Error = '' },
            @{ Arguments = @("-p:NazorWindowsAppManifest=$workspace/project/custom.manifest", '-p:ApplicationManifest=./CUSTOM.manifest'); Error = '' },
            @{ Arguments = @('-p:NazorWindowsAppManifest=custom.manifest', '-p:ApplicationManifest=other.manifest'); Error = 'NZB015' }
        )) {
            $output = & $engine $contract -t:NazorValidateWindowsManifest -nologo -v:q -nodeReuse:false @($settings.Arguments) 2>&1 | Out-String
            if ($settings.Error) {
                Assert ($LASTEXITCODE -ne 0 -and $output.Contains($settings.Error)) "Visual Studio MSBuild rejects manifest conflicts: $engine"
            } elseif ($LASTEXITCODE -ne 0) { throw $output }
        }
        Assert $true "Visual Studio MSBuild resolves default, custom, relative and absolute manifests: $engine"
    }
    if ($engines.Count -eq 0) { Write-Host 'Visual Studio MSBuild is not installed; full-framework manifest checks skipped.' }
}
foreach ($invalid in '', 'My.App.Id', 'org.example.my_app', 'org.example.my-app', 'org.1example.app', 'org..app', 'org.app', 'org.example.app ', ('org.example.' + ('a' * 117))) {
    Invoke-Probe @('build', $project, "-p:NazorApplicationId=$invalid", '-v:q', '-nodeReuse:false') 'NZB012' | Out-Null
}
Invoke-Probe @('msbuild', $project, '-t:NazorValidateApplicationIdentity', ('-p:NazorApplicationId=org.example.' + ('a' * 116)), '-v:q') | Out-Null
Invoke-Probe @('publish', $project, '-c', 'Release', '--no-build', '-o', "$workspace/invalid", '-p:NazorApplicationId=invalid', '-v:q') 'NZB012' | Out-Null
foreach ($framework in 'net8.0', 'net9.0', 'net10.0') {
    Write-Project "<TargetFramework>$framework</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo><NazorLinuxDesktopEntry>$desktop</NazorLinuxDesktopEntry>"
    Invoke-Probe @('build', $project, '-c', 'Release', '-v:q', '-nodeReuse:false') | Out-Null
    $output = "$workspace/project/bin/Release/$framework"
    Assert ((Get-Content "$output/$id.desktop" -Raw) -ceq $text) "$framework copies desktop content unchanged"
    Assert ((Invoke-Probe @("$output/IdentityProbe.dll")).Contains('metadata-ok')) "$framework preserves identity without renaming or GenerateAssemblyInfo"
}
Write-Project "<NazorLinuxDesktopEntry>$desktop</NazorLinuxDesktopEntry>"
Invoke-Probe @('build', $project, '-c', 'Release', '-v:q', '-nodeReuse:false') | Out-Null
Invoke-Probe @('publish', $project, '-c', 'Release', '--no-build', '-o', "$workspace/portable", '-v:q') | Out-Null
Assert (Test-Path "$workspace/portable/$id.desktop") 'portable publish --no-build includes the desktop entry'
Copy-Item "$workspace/project/$desktop" "$workspace/project/Platforms/Linux/MyApp.desktop"
Write-Project '<NazorLinuxDesktopEntry>Platforms/Linux/MyApp.desktop</NazorLinuxDesktopEntry>'
$warning = Invoke-Probe @('build', $project, '-c', 'Release', '-v:q')
Assert ($warning.Contains('warning NZB014') -and (Test-Path "$workspace/project/bin/Release/net10.0/MyApp.desktop")) 'a mismatching filename only warns and keeps its name'
New-Item -ItemType Directory "$workspace/project/Platforms/Linux/Case" | Out-Null
Copy-Item "$workspace/project/$desktop" "$workspace/project/Platforms/Linux/Case/ORG.example.identityprobe.desktop"
Write-Project '<NazorLinuxDesktopEntry>Platforms/Linux/Case/ORG.example.identityprobe.desktop</NazorLinuxDesktopEntry>'
Assert ((Invoke-Probe @('msbuild', $project, '-t:NazorValidateLinuxDesktopEntry', '-v:q')).Contains('warning NZB014')) 'desktop filename comparison is case-sensitive'
Write-Project '<NazorLinuxDesktopEntry>missing.desktop</NazorLinuxDesktopEntry>'
Invoke-Probe @('build', $project, '-v:q') 'NZB013' | Out-Null
Invoke-Probe @('msbuild', $project, '-t:NazorValidateLinuxDesktopEntry', '-p:RuntimeIdentifier=win-x64', '-v:q') | Out-Null
Assert $true 'Windows ignores a Linux desktop entry, including missing files'
Write-Project '<OutputType>Library</OutputType><NazorApplicationId /><NazorLinuxDesktopEntry>missing.desktop</NazorLinuxDesktopEntry>'
Invoke-Probe @('msbuild', $project, '-t:NazorValidateApplicationIdentity,NazorValidateLinuxDesktopEntry', '-v:q') | Out-Null
Write-Project '<NazorWindowsAppManifest>custom.manifest</NazorWindowsAppManifest>'
$selected = Invoke-Probe @('msbuild', $project, '-getProperty:ApplicationManifest', '-nologo')
Assert ($selected.Trim() -eq 'custom.manifest') 'the Nazor manifest property selects the standard .NET manifest'
Invoke-Probe @('build', $project, '-c', 'Release', '-v:q') | Out-Null
if ($IsWindows) {
    $output = & "$workspace/project/bin/Release/net10.0/IdentityProbe.exe" --verify-dpi -2 2>&1 | Out-String
    Assert ($LASTEXITCODE -eq 0 -and $output.Contains('metadata-ok')) 'the custom Nazor Windows manifest controls native process DPI awareness'
}
Write-Project '<NazorWindowsAppManifest>custom.manifest</NazorWindowsAppManifest><ApplicationManifest>./custom.manifest</ApplicationManifest>'
Invoke-Probe @('msbuild', $project, '-t:NazorValidateWindowsManifest', '-v:q') | Out-Null
Write-Project '<NazorWindowsAppManifest>custom.manifest</NazorWindowsAppManifest><ApplicationManifest>other.manifest</ApplicationManifest>'
Invoke-Probe @('build', $project, '-v:q') 'NZB015' | Out-Null
foreach ($conflict in '<NoWin32Manifest>true</NoWin32Manifest>', '<Win32Resource>custom.res</Win32Resource>') {
    Write-Project "<NazorWindowsAppManifest>custom.manifest</NazorWindowsAppManifest>$conflict"
    Invoke-Probe @('build', $project, '-v:q') 'NZB015' | Out-Null
}
Write-Project '<Win32Resource>custom.res</Win32Resource>'
$selected = Invoke-Probe @('msbuild', $project, '-getProperty:ApplicationManifest', '-nologo')
Assert ([string]::IsNullOrWhiteSpace($selected)) 'custom Win32 resources suppress the default manifest'
Write-Project
Invoke-Probe @('publish', $project, '-c', 'Release', '-o', "$workspace/no-desktop", '-v:q') | Out-Null
Assert (@(Get-ChildItem "$workspace/no-desktop" -Filter '*.desktop').Count -eq 0) 'no desktop entry is supplied by default'
Write-Project "<NazorLinuxDesktopEntry>$desktop</NazorLinuxDesktopEntry>"
Invoke-Probe @('publish', $project, '-c', 'Release', '-r', 'linux-x64', '--self-contained', 'false', '-p:UseAppHost=false', '-o', "$workspace/linux", '-v:q') | Out-Null
Assert (Test-Path "$workspace/linux/$id.desktop") 'Linux framework-dependent publication copies the entry'
Invoke-Probe @('publish', $project, '-c', 'Release', '-r', 'linux-x64', '--self-contained', 'false', '-p:PublishSingleFile=true', '-p:IncludeAllContentForSelfExtract=true', '-o', "$workspace/linux-single", '-v:q') | Out-Null
Assert (Test-Path "$workspace/linux-single/$id.desktop") 'Linux single-file publication leaves the desktop entry external even with complete extraction'
Invoke-Probe @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', "$workspace/windows", '-v:q') | Out-Null
Assert (-not (Test-Path "$workspace/windows/$id.desktop")) 'Windows publication excludes the entry'
$rid = if ($IsWindows) { 'win-x64' } else { 'linux-x64' }
foreach ($mode in @('single', 'trim') + $(if ($NativeAot) { @('aot') } else { @() })) {
    $properties = switch ($mode) {
        'single' { @('-p:PublishSingleFile=true', '-p:IncludeAllContentForSelfExtract=true') }
        'trim' { @('-p:PublishTrimmed=true') }
        'aot' { @('-p:PublishAot=true') }
    }
    Invoke-Probe (@('publish', $project, '-c', 'Release', '-r', $rid, '--self-contained', 'true', '-o', "$workspace/$mode", '-v:q', '-nodeReuse:false') + $properties) | Out-Null
    $exe = if ($IsWindows) { "$workspace/$mode/IdentityProbe.exe" } else { "$workspace/$mode/IdentityProbe" }
    $output = & $exe --verify-dpi -4 2>&1 | Out-String
    Assert ($LASTEXITCODE -eq 0 -and $output.Contains('metadata-ok')) "$mode preserves runtime identity"
    if (-not $IsWindows) { Assert (Test-Path "$workspace/$mode/$id.desktop") "$mode leaves the desktop entry external" }
}
Write-Host "Application metadata verification passed: $workspace" -ForegroundColor Green
