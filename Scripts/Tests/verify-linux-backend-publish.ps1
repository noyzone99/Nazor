#Requires -Version 7
param(
    [switch]$ContractsOnly,
    [switch]$SkipPack,
    [string]$Feed = "$PSScriptRoot/../../artifacts/packages",
    [ValidateSet('source', 'package')][string[]]$Modes = @('source', 'package')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$workspace = Join-Path $root ('artifacts/linux-backend-publish/' + [guid]::NewGuid().ToString('N'))
$feed = [IO.Path]::GetFullPath($Feed)
$version = (Select-Xml -Path "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
$rids = @('linux-x64', 'linux-arm64', 'linux-musl-x64', 'linux-musl-arm64')
New-Item -ItemType Directory -Force $workspace | Out-Null
function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Invoke-Dotnet([string[]]$Arguments, [string]$Diagnostic = '') {
    $output = & dotnet @Arguments 2>&1 | Out-String
    if ($Diagnostic) {
        Assert ($LASTEXITCODE -ne 0 -and $output -match $Diagnostic) "Expected $Diagnostic. $output"
    } else {
        Assert ($LASTEXITCODE -eq 0) "dotnet $($Arguments -join ' ') failed. $output"
    }
}
$portableContent = foreach ($rid in $rids) {
    foreach ($module in 'wayland', 'x11') {
        "<ResolvedFileToPublish Include=""$root/artifacts/native/$rid/libnazor-$module.so"" RelativePath=""runtimes/$rid/native/libnazor-$module.so"" Condition=""'`$(OmitNativeAsset)' != '$rid/$module'"" />"
    }
}
@"
<Project>
  <PropertyGroup><OutputType>Exe</OutputType></PropertyGroup>
  <Import Project="$root/Sources/Tools/Nazor.Sdk/build/Nazor.Repository.targets" />
  <Import Project="$root/Sources/Tools/Nazor.Sdk/Sdk/Nazor.Runtime.props" />
  <Import Project="$root/Sources/Directory.Build.targets" />
  <ItemGroup>
    <ResolvedFileToPublish Include="`$(Candidate)" RelativePath="libnazor-wayland.so" />
    <ResolvedFileToPublish Include="`$(CandidateX11)" RelativePath="libnazor-x11.so" />
    <ResolvedFileToPublish Include="/renderer/libSkiaSharp.so" RelativePath="libSkiaSharp.so" />
  </ItemGroup>
  <ItemGroup Condition="'`$(RuntimeIdentifier)' == ''">$($portableContent -join "`n")</ItemGroup>
  <Target Name="Verify" DependsOnTargets="NazorValidateLinuxNativeLibrariesSettings;NazorFilterLinuxNativeLibrariesContent;NazorValidateNativePublication;NazorValidatePortableNativePublication">
    <Error Condition="'`$(NazorLinuxNativeLibraries)' == 'None' and '@(ResolvedFileToPublish->WithMetadataValue('Filename', 'libnazor-wayland'))' != ''" Text="None retained Wayland." />
    <Error Condition="'@(ResolvedFileToPublish->WithMetadataValue('Filename', 'libSkiaSharp'))' == ''" Text="Renderer was removed." />
  </Target>
  <Target Name="IncludeTransitiveProjectReferences">
    <ItemGroup>
      <ProjectReference Include="/transitive/Nazor.Platform.Linux.csproj" AdditionalProperties="Preserved=true" />
      <ProjectReference Include="/transitive/Nazor.Core.csproj" GlobalPropertiesToRemove="Preserved" />
    </ItemGroup>
  </Target>
  <Target Name="VerifyDefaultTransitive" DependsOnTargets="IncludeTransitiveProjectReferences">
    <ItemGroup><NativeReference Include="@(ProjectReference->WithMetadataValue('Filename', 'Nazor.Platform.Linux'))" /></ItemGroup>
    <Error Condition="`$([System.String]::Copy('%(NativeReference.AdditionalProperties)').Contains('_NazorNativeLibrariesToBuild')) or `$([System.String]::Copy('%(NativeReference.AdditionalProperties)').Contains('NazorRuntimeIdentifier'))" Text="Defaults created another native project instance." />
  </Target>
  <Target Name="VerifyNativePropertyIsolation" DependsOnTargets="IncludeTransitiveProjectReferences">
    <ItemGroup><CoreReference Include="@(ProjectReference->WithMetadataValue('Filename', 'Nazor.Core'))" /></ItemGroup>
    <Error Condition="!`$([System.String]::Copy('%(CoreReference.GlobalPropertiesToRemove)').Contains('_NazorNativeLibrariesToBuild')) or !`$([System.String]::Copy('%(CoreReference.GlobalPropertiesToRemove)').Contains('NazorRuntimeIdentifier')) or !`$([System.String]::Copy('%(CoreReference.GlobalPropertiesToRemove)').Contains('Preserved'))" Text="Synthesized core references lost native property isolation or existing metadata." />
  </Target>
  <Target Name="VerifyTransitive" DependsOnTargets="IncludeTransitiveProjectReferences">
    <ItemGroup><NativeReference Include="@(ProjectReference->WithMetadataValue('Filename', 'Nazor.Platform.Linux'))" /></ItemGroup>
    <Error Condition="!`$([System.String]::Copy('%(NativeReference.AdditionalProperties)').Contains('_NazorNativeLibrariesToBuild=None')) or !`$([System.String]::Copy('%(NativeReference.AdditionalProperties)').Contains('Preserved=true'))" Text="Transitive native reference lost its local backend selection or existing properties." />
  </Target>
</Project>
"@ | Set-Content "$workspace/Contract.proj"
$contract = @('msbuild', "$workspace/Contract.proj", '-t:Verify', '-nologo', '-v:q')
foreach ($rid in $rids) {
    foreach ($backend in 'All', 'Wayland', 'X11', 'None', 'none', 'wayland', 'x11') {
        Invoke-Dotnet ($contract + "-p:RuntimeIdentifier=$rid" + "-p:NazorLinuxNativeLibraries=$backend" + "-p:Candidate=$root/artifacts/native/$rid/libnazor-wayland.so" + "-p:CandidateX11=$root/artifacts/native/$rid/libnazor-x11.so")
    }
    Invoke-Dotnet ($contract + "-p:RuntimeIdentifier=$rid" + '-p:NazorLinuxNativeLibraries=None')
    Invoke-Dotnet ($contract + "-p:RuntimeIdentifier=$rid") 'NZB004'
    Invoke-Dotnet ($contract + "-p:RuntimeIdentifier=$rid" + '-p:NazorLinuxNativeLibraries=X11') 'NZB004'
    Invoke-Dotnet ($contract + "-p:RuntimeIdentifier=$rid" + '-p:NazorLinuxNativeLibraries=invalid') 'NZB011'
}
foreach ($rid in '', 'linux-x64') {
    foreach ($value in 'DRM', 'invalid') {
        Invoke-Dotnet ($contract + "-p:RuntimeIdentifier=$rid" + "-p:NazorLinuxNativeLibraries=$value") 'NZB011'
    }
}
foreach ($libraries in 'All', 'Wayland', 'X11', 'None') {
    Invoke-Dotnet ($contract + "-p:NazorLinuxNativeLibraries=$libraries" + "-p:Candidate=$root/artifacts/native/linux-x64/libnazor-wayland.so" + "-p:CandidateX11=$root/artifacts/native/linux-x64/libnazor-x11.so")
}
foreach ($rid in $rids) {
    foreach ($module in 'wayland', 'x11') {
        Invoke-Dotnet ($contract + "-p:OmitNativeAsset=$rid/$module") 'NZB004'
        Invoke-Dotnet ($contract + "-p:OmitNativeAsset=$rid/$module" + "-p:NazorLinuxNativeLibraries=$module") 'NZB004'
        $other = if ($module -eq 'wayland') { 'X11' } else { 'Wayland' }
        Invoke-Dotnet ($contract + "-p:OmitNativeAsset=$rid/$module" + "-p:NazorLinuxNativeLibraries=$other")
    }
}
foreach ($rid in 'win-x64', 'win-arm64', 'osx-x64', 'freebsd-x64') {
    foreach ($libraries in 'All', 'Wayland', 'None', 'X11', 'DRM', 'invalid') {
        Invoke-Dotnet ($contract + "-p:RuntimeIdentifier=$rid" + "-p:NazorLinuxNativeLibraries=$libraries")
    }
    Invoke-Dotnet ($contract + "-p:RuntimeIdentifier=$rid")
}
Write-Host 'Verified native library selection, missing bridges, None filtering and ignored non-Linux values.'
Invoke-Dotnet @('msbuild', "$workspace/Contract.proj", '-t:VerifyTransitive', '-p:RuntimeIdentifier=linux-x64', '-p:NazorLinuxNativeLibraries=None', '-nologo', '-v:q')
Invoke-Dotnet @('msbuild', "$workspace/Contract.proj", '-t:VerifyTransitive', '-p:NazorLinuxNativeLibraries=None', '-nologo', '-v:q')
Invoke-Dotnet @('msbuild', "$workspace/Contract.proj", '-t:VerifyDefaultTransitive;VerifyNativePropertyIsolation', '-nologo', '-v:q')
if ($ContractsOnly) { return }
if (-not $SkipPack -and $Modes -contains 'package') {
    Invoke-Dotnet @('pack', "$root/Nazor.slnx", '-c', 'Release', '-o', $feed, '-m:1', '-nodeReuse:false', '-p:UseSharedCompilation=false', '-p:NuGetAudit=false', '-v:q')
}
foreach ($file in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') { '<Project />' | Set-Content "$workspace/$file" }
@"
<configuration>
  <config><add key="globalPackagesFolder" value="$workspace/.packages" /></config>
  <packageSources><clear /><add key="nazor" value="$feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nazor"><package pattern="Nazor.*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
  <fallbackPackageFolders><clear /><add key="user" value="$env:USERPROFILE/.nuget/packages" /></fallbackPackageFolders>
</configuration>
"@ | Set-Content "$workspace/nuget.config"
foreach ($mode in $Modes) {
    $probe = "$workspace/$mode"
    New-Item -ItemType Directory -Force "$probe/Properties/PublishProfiles" | Out-Null
    $sdk = if ($mode -eq 'source') { 'Microsoft.NET.Sdk' } else { "Nazor.Sdk/$version" }
    $references = if ($mode -eq 'source') {
        "<ItemGroup><ProjectReference Include=""$root/Sources/Hosts/Nazor.Hosting.Desktop/Nazor.Hosting.Desktop.csproj"" /></ItemGroup><Import Project=""$root/Sources/Tools/Nazor.Sdk/build/Nazor.targets"" />"
    } else { '' }
    @"
<Project Sdk="$sdk">
  <PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.deployment</NazorApplicationId><RootNamespace>Deployment</RootNamespace><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><None Update="payload.txt" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" /></ItemGroup>
  $references
</Project>
"@ | Set-Content "$probe/Deployment.csproj"
    foreach ($file in 'Program.cs', 'Probe.nazor', 'payload.txt') { Copy-Item "$root/Tests/Fixtures/Deployment/$file" "$probe/$file" }
    foreach ($rid in @($rids) + @('portable','win-x64')) {
        foreach ($backend in 'default', 'All', 'Wayland', 'X11', 'None') {
            $setting = if ($backend -eq 'default') { '' } else { "<NazorLinuxNativeLibraries>$backend</NazorLinuxNativeLibraries>" }
            $runtimeIdentifier = if ($rid -eq 'portable') { '' } else { $rid }
            $selfContained = if ($rid -eq 'portable') { 'false' } else { 'true' }
            "<Project><PropertyGroup><RuntimeIdentifier>$runtimeIdentifier</RuntimeIdentifier><SelfContained>$selfContained</SelfContained>$setting</PropertyGroup></Project>" | Set-Content "$probe/Properties/PublishProfiles/Test.pubxml"
            $output = "$workspace/$mode-$rid-$backend"
            $nativeOptions = if ($backend -eq 'None' -and $mode -eq 'source') { @('-p:NazorSkipNativeBuild=false', '-p:NazorNativeRid=invalid-rid') } else { @('-p:NazorSkipNativeBuild=true') }
            Invoke-Dotnet (@('publish', "$probe/Deployment.csproj", '-c', 'Release', '-p:PublishProfile=Test', '-p:NuGetAudit=false', '--artifacts-path', "$workspace/build-$mode", '-m:1', '-nodeReuse:false', '-p:UseSharedCompilation=false', '-o', $output, '-v:q') + $nativeOptions)
            $bridges = @(Get-ChildItem $output -Recurse -Filter 'libnazor*.so' -File)
            $expected = switch ($backend) { 'None' { @() } 'Wayland' { @('wayland') } 'X11' { @('x11') } default { @('wayland','x11') } }
            if ($rid.StartsWith('win-')) { $expected = @() }
            $linuxRids = if ($rid -eq 'portable') { $rids } elseif ($rid.StartsWith('linux-')) { @($rid) } else { @() }
            Assert ($bridges.Count -eq (@($expected).Count * @($linuxRids).Count)) "$mode/$rid/$backend has incorrect native assets."
            foreach ($linuxRid in $linuxRids) {
                foreach ($module in $expected) {
                    $published = if ($rid -eq 'portable') { "$output/runtimes/$linuxRid/native/libnazor-$module.so" } else { "$output/libnazor-$module.so" }
                    Assert (Test-Path $published) "$mode/$rid/$backend lost $module for $linuxRid."
                    Assert ((Get-FileHash $published).Hash -eq (Get-FileHash "$root/artifacts/native/$linuxRid/libnazor-$module.so").Hash) "$mode/$rid/$backend selected a different RID's bridge."
                }
            }
            $renderer = if ($rid.StartsWith('win-')) { 'libSkiaSharp.dll' } else { 'libSkiaSharp.so' }
            Assert (@(Get-ChildItem $output -Recurse -Filter $renderer -File).Count -gt 0) "$mode/$rid/$backend lost the renderer."
            Assert ((Test-Path "$output/Nazor.Platform.Linux.dll") -eq (-not $rid.StartsWith('win-'))) "$mode/$rid/$backend has incorrect Linux platform presence."
            if ($rid -eq 'portable') {
                Assert (Test-Path "$output/Nazor.Platform.Windows.dll") 'Portable selection removed Windows hosting.'
                Assert (Test-Path "$output/runtimes/win-x64/native/Microsoft.UI.Windowing.dll") 'Portable selection removed Windows native libraries.'
            }
            Write-Host "Verified $mode / $rid / $backend"
        }
    }
    foreach ($case in @(@('linux-x64', 'DRM', 'NZB011'), @('', 'DRM', 'NZB011'), @('linux-x64', 'invalid', 'NZB011'))) {
        $rid, $backend, $diagnostic = $case
        "<Project><PropertyGroup><RuntimeIdentifier>$rid</RuntimeIdentifier><NazorLinuxNativeLibraries>$backend</NazorLinuxNativeLibraries></PropertyGroup></Project>" | Set-Content "$probe/Properties/PublishProfiles/Test.pubxml"
        Invoke-Dotnet @('publish', "$probe/Deployment.csproj", '-c', 'Release', '-p:PublishProfile=Test', '--artifacts-path', "$workspace/build-$mode", '-p:NuGetAudit=false', '-m:1', '-nodeReuse:false', '-v:q') $diagnostic
    }
    foreach ($backend in 'Wayland', 'X11', 'None') {
        $output = "$workspace/$mode-linux-x64-$backend-single"
        Invoke-Dotnet @('publish', "$probe/Deployment.csproj", '-c', 'Release', '-r', 'linux-x64', '--self-contained', 'true', "-p:NazorLinuxNativeLibraries=$backend", '-p:PublishSingleFile=true', '-p:NuGetAudit=false', '-p:NazorSkipNativeBuild=true', '--artifacts-path', "$workspace/build-$mode", '-m:1', '-nodeReuse:false', '-p:UseSharedCompilation=false', '-o', $output, '-v:q')
        Assert ((Test-Path "$output/libnazor-wayland.so") -eq ($backend -eq 'Wayland')) "$mode/$backend single-file has incorrect Wayland payload."
        Assert ((Test-Path "$output/libnazor-x11.so") -eq ($backend -eq 'X11')) "$mode/$backend single-file has incorrect X11 payload."
        Assert (Test-Path "$output/Deployment") "$mode/$backend single-file executable is missing."
    }
}
Write-Host "Linux native library publication matrix verified: $workspace"
