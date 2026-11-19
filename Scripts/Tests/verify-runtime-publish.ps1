#Requires -Version 7
param(
    [switch]$SkipPack,
    [string]$Feed = "$PSScriptRoot/../../artifacts/packages",
    [ValidateSet('source', 'package')][string[]]$Modes = @('source', 'package')
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$workspace = Join-Path $root ('artifacts/runtime-publish/' + [guid]::NewGuid().ToString('N'))
$feed = [IO.Path]::GetFullPath($Feed)
$version = (Select-Xml -Path "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
New-Item -ItemType Directory -Force $workspace | Out-Null

function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments -join ' ') failed." }
}
function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

@"
<Project>
  <PropertyGroup><OutputType>Exe</OutputType><NazorLinuxNativeLibraries>Wayland</NazorLinuxNativeLibraries></PropertyGroup>
  <Import Project="$root/Sources/Tools/Nazor.Sdk/Sdk/Nazor.Runtime.props" />
  <ItemGroup><ResolvedFileToPublish Include="`$(Candidate)" RelativePath="libnazor-wayland.so" /></ItemGroup>
</Project>
"@ | Set-Content "$workspace/NativeContract.proj"
foreach ($pair in @(@('linux-x64', 'linux-arm64'), @('linux-arm64', 'linux-x64'), @('linux-musl-x64', 'linux-x64'), @('linux-musl-arm64', 'linux-arm64'))) {
    $requested, $wrong = $pair
    Invoke-Dotnet @('msbuild', "$workspace/NativeContract.proj", '-t:NazorValidateNativePublication', "-p:RuntimeIdentifier=$requested", "-p:Candidate=$root/artifacts/native/$requested/libnazor-wayland.so", '-nologo', '-v:q')
    $rejected = & dotnet msbuild "$workspace/NativeContract.proj" -t:NazorValidateNativePublication "-p:RuntimeIdentifier=$requested" "-p:Candidate=$root/artifacts/native/$wrong/libnazor-wayland.so" -nologo -v:q 2>&1 | Out-String
    Assert ($LASTEXITCODE -ne 0 -and $rejected -match 'NZB004') "The native validation accepted $wrong for $requested. $rejected"
}
Write-Host 'Verified exact native RID validation: architecture and libc mismatches rejected.'

if (-not $SkipPack) {
    Invoke-Dotnet @('pack', "$root/Nazor.slnx", '-c', 'Release', '-o', $feed, '-m:1', '-nodeReuse:false', '-p:UseSharedCompilation=false', '-p:NuGetAudit=false', '-v:q')
}
$windowingVersion = (Select-Xml -Path "$root/Sources/Tools/Nazor.Sdk/Sdk/Nazor.WindowsDependencies.props" -XPath '//NazorWindowingVersion').Node.InnerText
$windowing = "$env:USERPROFILE/.nuget/packages/microsoft.windowsappsdk.interactiveexperiences/$windowingVersion"
Assert (Test-Path "$windowing/runtimes-framework/win-x64/native/Microsoft.UI.Windowing.dll") 'Restore the Windows platform project before running the matrix.'

# Use a separate primary restore cache for each matrix run.
'<Project />' | Set-Content "$workspace/Directory.Build.props"
'<Project />' | Set-Content "$workspace/Directory.Build.targets"
'<Project />' | Set-Content "$workspace/Directory.Packages.props"
@"
<configuration>
  <config><add key="globalPackagesFolder" value="$workspace/.packages" /></config>
  <packageSources><clear /><add key="nazor" value="$feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nazor"><package pattern="Nazor.*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
  <fallbackPackageFolders><add key="user" value="$env:USERPROFILE/.nuget/packages" /></fallbackPackageFolders>
</configuration>
"@ | Set-Content "$workspace/nuget.config"

foreach ($mode in $Modes) {
    $probe = Join-Path $workspace $mode
    New-Item -ItemType Directory -Force "$probe/Properties/PublishProfiles" | Out-Null
    $sdk = if ($mode -eq 'source') { 'Microsoft.NET.Sdk' } else { "Nazor.Sdk/$version" }
    $references = if ($mode -eq 'source') {
        "<ItemGroup><ProjectReference Include=""$root/Sources/Hosts/Nazor.Hosting.Desktop/Nazor.Hosting.Desktop.csproj"" /></ItemGroup><Import Project=""$root/Sources/Tools/Nazor.Sdk/build/Nazor.targets"" />"
    } else { '' }
    @"
<Project Sdk="$sdk">
  <PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  $references
</Project>
"@ | Set-Content "$probe/RuntimeProbe.csproj"
    @'
using System.Reflection;
using Nazor;
using Nazor.Platform;

if (NazorApplication.CreateBuilder().Platform.Host is not DesktopPlatformHost) return 1;
var select = typeof(DesktopPlatformHost).GetMethod("CreatePlatformHost", BindingFlags.Static | BindingFlags.NonPublic)!;
foreach (var platform in new[] { PlatformKind.Windows, PlatformKind.Linux })
{
    var included = args[0] == "portable" || (args[0].StartsWith("win-") ? platform == PlatformKind.Windows : platform == PlatformKind.Linux);
    try
    {
        var host = select.Invoke(null, new object[] { platform });
        if (!included || host!.GetType().Name != (platform == PlatformKind.Windows ? "WindowsPlatformHost" : "LinuxPlatformHost")) return 2;
        if (platform == PlatformKind.Linux)
        {
            var drmHost = host.GetType().Assembly.GetType("Nazor.Platform.Linux.DrmPlatformHost", throwOnError: true)!;
            if (Activator.CreateInstance(drmHost, new object?[] { null }) is not IPlatformHost) return 3;
        }
    }
    catch (TargetInvocationException error) when (!included && error.InnerException is PlatformNotSupportedException) { }
}
if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var windowing = Assembly.Load("Nazor.Platform.Windows").GetType("Nazor.Platform.Windows.WindowsWindowing", true)!;
            using var native = (IDisposable?)windowing.GetMethod("TryInitialize")!.Invoke(null, null);
            if (native is null) throw new InvalidOperationException("Windows windowing silently fell back to Win32.");
        }
        catch (Exception error) { failure = error; }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (failure is not null) throw failure;
}
return 0;
'@ | Set-Content "$probe/Program.cs"

    foreach ($framework in 'net8.0', 'net9.0', 'net10.0') {
        $projectText = Get-Content "$probe/RuntimeProbe.csproj" -Raw
        [regex]::Replace($projectText, '<TargetFramework>[^<]+</TargetFramework>', "<TargetFramework>$framework</TargetFramework>") | Set-Content "$probe/RuntimeProbe.csproj"
        foreach ($runtime in 'portable', 'win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'linux-arm64', 'linux-musl-x64', 'linux-musl-arm64') {
            $output = Join-Path $workspace "$mode-$framework-$runtime"
            # A profile exercises the Visual Studio path, where RuntimeIdentifier is not a command-line global property.
            $ridProperty = if ($runtime -eq 'portable') { '' } else { "<RuntimeIdentifier>$runtime</RuntimeIdentifier>" }
            "<Project><PropertyGroup>$ridProperty<SelfContained>false</SelfContained></PropertyGroup></Project>" | Set-Content "$probe/Properties/PublishProfiles/Test.pubxml"
            Invoke-Dotnet @('publish', "$probe/RuntimeProbe.csproj", '-c', 'Release', '-p:PublishProfile=Test', '--self-contained', 'false', '--artifacts-path', "$workspace/build-$mode", '-o', $output, '-m:1', '-nodeReuse:false', '-p:UseSharedCompilation=false', '-p:NuGetAudit=false', '-v:q')
            $files = @(Get-ChildItem $output -Recurse -File | Select-Object -ExpandProperty Name)
            $deps = Get-Content "$output/RuntimeProbe.deps.json" -Raw | ConvertFrom-Json
            $libraries = @($deps.libraries.PSObject.Properties.Name)
            $windows = -not $runtime.StartsWith('linux-')
            $linux = -not $runtime.StartsWith('win-')
            foreach ($file in 'Nazor.Hosting.Windows.dll', 'Nazor.Platform.Windows.dll', 'WinRT.Runtime.dll', 'Microsoft.Windows.SDK.NET.dll', 'Microsoft.InteractiveExperiences.Projection.dll') {
                Assert (($files -contains $file) -eq $windows) "$mode/$framework/${runtime}: incorrect presence of $file"
            }
            Assert ($files -notcontains 'Tmds.DBus.dll') "$mode/$framework/${runtime}: legacy D-Bus assembly was published"
            Assert (-not ($libraries -match '^Tmds\.DBus/')) "$mode/$framework/${runtime}: legacy D-Bus package was restored"
            foreach ($file in 'Nazor.Platform.Linux.dll', 'Tmds.DBus.Protocol.dll', 'libnazor-wayland.so', 'libnazor-x11.so') {
                Assert (($files -contains $file) -eq $linux) "$mode/$framework/${runtime}: incorrect presence of $file"
            }
            if ($runtime -eq 'portable') {
                Assert (-not (Test-Path "$output/libnazor-wayland.so")) 'Portable publication must not put a RID-specific Wayland bridge in the root.'
                foreach ($bridge in Get-ChildItem "$root/artifacts/native/linux-*/libnazor-*.so" -File) {
                    $published = "$output/runtimes/$($bridge.Directory.Name)/native/$($bridge.Name)"
                    Assert (Test-Path $published) "Missing portable bridge: $published"
                    Assert ((Get-FileHash $published).Hash -eq (Get-FileHash $bridge.FullName).Hash) "Incorrect portable bridge: $published"
                }
            } elseif ($runtime.StartsWith('linux-')) {
                Assert (Test-Path "$output/libnazor-wayland.so") 'Linux RID publication should retain the standard flat native layout.'
                Assert ((Get-FileHash "$output/libnazor-wayland.so").Hash -eq (Get-FileHash "$root/artifacts/native/$runtime/libnazor-wayland.so").Hash) "The publication selected a different RID's libnazor-wayland.so: $runtime"
            }
            Assert ($files -notcontains 'libnazor.so') 'The obsolete native library name must not be published.'
            foreach ($notice in 'WindowsAppSDK.LICENSE.txt', 'ThirdPartyNotices.md') {
                Assert ((Test-Path "$output/$notice") -eq $windows) "$mode/$framework/${runtime}: incorrect root notice $notice"
                Assert (@($files | Where-Object { $_ -eq $notice }).Count -eq [int]$windows) "Duplicated notice: $notice"
            }
            Assert (-not (Test-Path "$output/ThirdParty")) 'Windows notices must not create a ThirdParty directory.'
            if ($windows) {
                $rids = if ($runtime -eq 'portable') { @('win-x64', 'win-x86', 'win-arm64') } else { @($runtime) }
                foreach ($rid in $rids) {
                    $native = if ($runtime -eq 'portable') { "$output/runtimes/$rid/native" } else { $output }
                    Assert (Test-Path "$native/Windowing.manifest") "Missing $rid windowing manifest."
                    foreach ($original in Get-ChildItem "$windowing/runtimes-framework/$rid/native" -File) {
                        $published = Join-Path $native $original.Name
                        Assert (Test-Path $published) "Missing native asset: $published"
                        Assert ((Get-FileHash $published).Hash -eq (Get-FileHash $original.FullName).Hash) "Modified native asset: $published"
                    }
                }
                if ($runtime -ne 'portable') { Assert (-not (Test-Path "$output/runtimes")) 'Windows RID publication must use a flat native layout.' }
            }
            Assert ((@($libraries -match '^Nazor\.Platform\.Windows/').Count -gt 0) -eq $windows) 'Incorrect Windows dependency graph.'
            Assert ((@($libraries -match '^Nazor\.Platform\.Linux/').Count -gt 0) -eq $linux) 'Incorrect Linux dependency graph.'
            if ($runtime -eq 'portable') {
                Assert (($files -contains 'libSkiaSharp.dll') -and ($files -contains 'libSkiaSharp.so')) 'Portable publication must retain Windows and Linux renderer assets.'
            } else {
                Assert ($deps.runtimeTarget.name.EndsWith("/$runtime")) 'Incorrect published RID.'
                $unwantedNative = if ($windows) { 'libSkiaSharp.so' } else { 'libSkiaSharp.dll' }
                Assert ($files -notcontains $unwantedNative) "Unexpected native renderer asset: $unwantedNative"
                $foreignRuntimes = @(Get-ChildItem "$output/runtimes" -Directory -ErrorAction SilentlyContinue | Where-Object Name -NE $runtime)
                Assert ($foreignRuntimes.Count -eq 0) 'Publication contains native assets for a different RID.'
            }
            if ($runtime -eq 'portable' -or ($IsWindows -and $runtime -eq 'win-x64') -or ($IsLinux -and $runtime -eq 'linux-x64')) {
                Invoke-Dotnet @("$output/RuntimeProbe.dll", $runtime)
            }
            Write-Host "Verified $mode / $framework / $runtime"
        }
    }
    foreach ($missingRuntime in 'linux-arm', 'linux-musl-arm') {
        if (Test-Path "$root/artifacts/native/$missingRuntime/libnazor-wayland.so") { continue }
        # NuGet can fall back from musl to glibc; a bridge built for that different ABI must still be rejected.
        $missingBridge = & dotnet publish "$probe/RuntimeProbe.csproj" -c Release -r $missingRuntime --self-contained false --artifacts-path "$workspace/build-$mode" -p:UseAppHost=false -p:NuGetAudit=false -m:1 -nodeReuse:false -p:UseSharedCompilation=false -o "$workspace/$mode-missing-$missingRuntime" -v:q 2>&1 | Out-String
        Assert ($LASTEXITCODE -ne 0 -and $missingBridge -match 'NZB004') "$mode publication must reject a missing $missingRuntime Wayland bridge. $missingBridge"
        Write-Host "Verified $mode / missing $missingRuntime bridge"
    }
}
Write-Host "Runtime publication matrix verified: $workspace"
