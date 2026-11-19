#Requires -Version 7
param(
    [Parameter(Mandatory)][string]$Workspace,
    [ValidateSet('net8.0', 'net9.0', 'net10.0', 'net11.0')][string]$Framework = 'net10.0',
    [string]$Feed = (Join-Path (Resolve-Path "$PSScriptRoot/../..").Path 'artifacts/packages')
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'The native DevTools probes require Windows.' }
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$Feed = [IO.Path]::GetFullPath($Feed)
$version = (Select-Xml -Path "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
$Workspace = [IO.Path]::GetFullPath($Workspace)
if (Test-Path $Workspace) { throw "Choose a fresh workspace: $Workspace" }
New-Item -ItemType Directory "$Workspace/Probe", "$Workspace/feed" | Out-Null
if ($Framework -eq 'net11.0') {
    '{ "sdk": { "version": "11.0.100", "rollForward": "latestPatch", "allowPrerelease": false } }' | Set-Content "$Workspace/global.json"
}
foreach ($file in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') {
    '<Project />' | Set-Content "$Workspace/$file"
}
@"
<configuration>
  <config><add key="globalPackagesFolder" value="$Workspace/.packages" /></config>
  <packageSources><clear /><add key="nazor" value="$Feed" /><add key="local" value="$Workspace/feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><clear />
    <packageSource key="nazor"><package pattern="Nazor.*" /></packageSource>
    <packageSource key="local"><package pattern="DevToolsLeak" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
  <fallbackPackageFolders><clear /><add key="user" value="$(Join-Path $env:USERPROFILE '.nuget/packages')" /></fallbackPackageFolders>
</configuration>
"@ | Set-Content "$Workspace/nuget.config"
$project = "$Workspace/Probe/Probe.csproj"
$script:commandNumber = 0
function Invoke-Probe([string[]]$Arguments, [string]$ExpectedError = '') {
    $script:commandNumber++
    $output = & dotnet @Arguments 2>&1 | Out-String
    $code = $LASTEXITCODE
    $output | Set-Content "$Workspace/$('{0:D2}' -f $script:commandNumber).log"
    if ($ExpectedError) {
        if ($code -eq 0 -or $output -notmatch $ExpectedError) { throw "Expected $ExpectedError from dotnet $Arguments : $output" }
        Write-Host "PASS: $ExpectedError rejects $($Arguments -join ' ')"
    }
    elseif ($code -ne 0) { throw "dotnet $Arguments failed: $output" }
    return $output
}
function Write-Probe([string]$Items = '') {
    @"
<Project Sdk="Nazor.Sdk/$version">
  <PropertyGroup><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId><TargetFramework>$Framework</TargetFramework></PropertyGroup>
  <ItemGroup>$Items</ItemGroup>
</Project>
"@ | Set-Content $project
}
function Run-Probe([string]$Directory, [string]$Mode) {
    $managed = Test-Path "$Directory/Probe.dll"
    $start = [Diagnostics.ProcessStartInfo]::new($(if ($managed) { 'dotnet' } else { "$Directory/Probe.exe" }))
    if ($managed) { $start.ArgumentList.Add("$Directory/Probe.dll") }
    $start.WorkingDirectory = $Directory
    $start.UseShellExecute = $false
    $start.WindowStyle = 'Hidden'
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Environment['NazorEnableDevTools'] = 'true'
    $start.Environment['NAZOR_DEVTOOLS'] = '1'
    $start.Environment['NAZOR_DEVCHANNEL'] = '1'
    $start.Environment['NAZOR_DEV_DIRECTORY'] = "$Workspace/discovery"
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    try {
        if (-not $process.WaitForExit(20000)) { throw 'The diagnostics probe did not exit.' }
        $output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        $output | Set-Content "$Workspace/runtime-$Mode-$(Split-Path $Directory -Leaf).log"
        if ($process.ExitCode -ne 0 -or $output -notmatch "PASS: $Mode") { throw "The $Mode runtime probe failed: $output" }
        if (Test-Path "$Workspace/discovery/$($process.Id).json") { throw 'The explicit DevChannel=false setting was ignored.' }
        Write-Host "PASS: $Framework $Mode runtime attachment policy"
    }
    finally {
        if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
        $process.Dispose()
    }
}
@'
#if NAZOR_DEVTOOLS
using Nazor.DevTools;
#endif

var builder = NazorApplication.CreateBuilder();
builder.Diagnostics.DevChannel = false;
builder.Application.CreateMainWindow<Probe>(window => window.Activate = false);
#if NAZOR_DEVTOOLS
builder.UseDevTools();
#endif
using var app = builder.Build();
app.Run();
return Probe.Passed ? 0 : 1;

public sealed class Probe : ComponentBase
{
    [Inject] public IWindow Window { get; set; } = null!;
    public static bool Passed;

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        var window = (NazorWindow)Window;
        window.Dispatcher.Post(() =>
        {
            try
            {
#if NAZOR_DEVTOOLS
                var agent = window.GetDevTools() ?? throw new InvalidOperationException("Program configuration did not attach DevTools.");
                if (window.EnableDevTools() != agent || !System.IO.File.Exists(agent.DiscoveryFile))
                    throw new InvalidOperationException("Configured DevTools attachment did not publish discovery.");
                var session = window.GetDiagnostics()!;
                if (window.GetDiagnostics() != session || window.EnableDiagnostics() != session) throw new InvalidOperationException("Attachment is not stable.");
                session.SetInspectMode(true);
                var exporter = new TreeSnapshotExporter(session);
                var target = new Nazor.DevTools.Protocol.TargetState(new(1, "process", "runtime", "app", "window", session.SessionId), 1, "running", null);
                var tree = exporter.Capture(target);
                if (tree.Snapshot.Components.Length == 0 || tree.Snapshot.ComponentRoots.Length != 1 || tree.Target.SessionId != session.SessionId)
                    throw new InvalidOperationException("The packaged tree capture lost its component/window context.");
                TreeSnapshotExporter.SerializeAsync(tree).GetAwaiter().GetResult();
                window.DisableDiagnostics();
                if (window.GetDiagnostics() is not null || window.Instrumentation is not null || session.IsInspecting || !agent.IsDisposed || System.IO.File.Exists(agent.DiscoveryFile))
                    throw new InvalidOperationException("Detachment did not release the session.");
                Console.WriteLine("PASS: development");
#else
                if (window.Instrumentation is not null) throw new InvalidOperationException("Attachment was automatic.");
                foreach (var name in new[] { "Nazor.DevTools.Agent", "Nazor.Diagnostics", "Nazor.DevTools.Protocol", "Nazor.DevTools" })
                {
                    try { System.Reflection.Assembly.Load(name); throw new InvalidOperationException(name + " is available."); }
                    catch (System.IO.FileNotFoundException) { }
                }
                Console.WriteLine("PASS: production");
#endif
                Passed = true;
            }
            finally { window.Close(); }
        });
    }
}
'@ | Set-Content "$Workspace/Probe/Program.cs"
Push-Location $Workspace
try {
    Write-Probe
    $properties = Invoke-Probe @('msbuild', $project, '-nologo', '-getProperty:NazorEnableDevTools,DefineConstants') | ConvertFrom-Json
    if ($properties.Properties.NazorEnableDevTools -ne 'false' -or $properties.Properties.DefineConstants -match 'NAZOR_DEVTOOLS') { throw 'DevTools must default to false.' }
    Invoke-Probe @('publish', $project, '-c', 'Release', '-o', "$Workspace/production", '-nodeReuse:false') | Out-Null
    if (Select-String "$Workspace/Probe/obj/project.assets.json" -Pattern '"Nazor\.(Diagnostics|DevTools[^"/]*)/' -Quiet) { throw 'Disabled graph contains DevTools.' }
    if (Get-ChildItem "$Workspace/production" -Recurse | Where-Object Name -Match '^Nazor\.(Diagnostics|DevTools)') { throw 'Disabled output contains DevTools.' }
    Run-Probe "$Workspace/production" 'production'
    Invoke-Probe @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishProfile=NazorProduction', '-o', "$Workspace/self-contained", '-nodeReuse:false') | Out-Null
    Run-Probe "$Workspace/self-contained" 'production'
    Invoke-Probe @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:PublishTrimmed=false', '-p:PublishProfile=NazorProduction', '-o', "$Workspace/single") | Out-Null
    Run-Probe "$Workspace/single" 'production'
    foreach ($variant in 'trim', 'trim-single') {
        $options = @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishAot=false', '-p:PublishTrimmed=true', '-o', "$Workspace/$variant")
        if ($variant -eq 'trim-single') { $options += '-p:PublishSingleFile=true' }
        Invoke-Probe $options | Out-Null
        Run-Probe "$Workspace/$variant" 'production'
    }

    Invoke-Probe @('publish', $project, '-c', 'Release', '-p:NazorEnableDevTools=true', '-p:NazorEnablePublishValidation=false', '-o', "$Workspace/development", '-nodeReuse:false') | Out-Null
    if (-not (Test-Path "$Workspace/development/Nazor.DevTools.Agent.dll")) { throw 'Opt-in did not include the Agent.' }
    if (-not (Test-Path "$Workspace/development/Nazor.DevTools.Protocol.dll")) { throw 'The Agent did not bring the Protocol dependency.' }
    if (-not (Test-Path "$Workspace/development/Nazor.DevTools/Nazor.DevTools.dll")) { throw 'The opted-in app has no bundled inspector.' }
    $bundle = Get-Content "$Workspace/development/Nazor.DevTools/Nazor.DevTools.bundle.json" -Raw | ConvertFrom-Json
    if ($bundle.runtimeIdentifier -ne 'win-x64' -or $bundle.framework -ne 'net10.0' -or $bundle.version -ne $version) { throw 'The tool bundle platform/runtime/version is incorrect.' }
    if (Select-String "$Workspace/development/Probe.deps.json" -Pattern '"Nazor.DevTools/' -Quiet) { throw 'The inspector entered the application dependency graph.' }
    Run-Probe "$Workspace/development" 'development'
    Invoke-Probe @('publish', $project, '-c', 'Release', '-p:NazorEnableDevTools=true', '-o', "$Workspace/rejected-enabled") 'NZB005' | Out-Null
    Invoke-Probe @('publish', $project, '-c', 'Release', '--no-build', '--no-restore', '-p:NazorEnablePublishValidation=true', '-o', "$Workspace/rejected-stale") 'NZB006' | Out-Null

    Write-Probe "<PackageReference Include=""Nazor.DevTools.Agent"" Version=""$version"" />"
    Invoke-Probe @('build', $project, '-c', 'Release') 'NZB005' | Out-Null
    $explicit = Invoke-Probe @('build', $project, '-c', 'Release', '-p:NazorEnableDevTools=true')
    if ($explicit -match 'NU1504') { throw 'The implicit Agent reference duplicates an explicit reference.' }

    New-Item -ItemType Directory "$Workspace/Leak" | Out-Null
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>$Framework</TargetFramework><PackageId>DevToolsLeak</PackageId><Version>1.0.0</Version></PropertyGroup>
  <ItemGroup><PackageReference Include="Nazor.Diagnostics" Version="$version" /></ItemGroup>
</Project>
"@ | Set-Content "$Workspace/Leak/Leak.csproj"
    'public sealed class Leak { }' | Set-Content "$Workspace/Leak/Leak.cs"
    Invoke-Probe @('pack', "$Workspace/Leak/Leak.csproj", '-c', 'Release', '-o', "$Workspace/feed") | Out-Null
    Write-Probe '<PackageReference Include="DevToolsLeak" Version="1.0.0" />'
    Invoke-Probe @('publish', $project, '-c', 'Release', '-p:PublishProfile=NazorProduction', '-o', "$Workspace/rejected-transitive") 'NZB006' | Out-Null
    Write-Probe "<PackageReference Include=""Nazor.Diagnostics"" Version=""$version"" PrivateAssets=""all"" />"
    Invoke-Probe @('publish', $project, '-c', 'Release', '-p:NazorEnablePublishValidation=true', '-o', "$Workspace/rejected-direct") 'NZB006' | Out-Null
    Write-Probe "<PackageReference Include=""Nazor.DevTools.Protocol"" Version=""$version"" PrivateAssets=""all"" />"
    Invoke-Probe @('publish', $project, '-c', 'Release', '-p:NazorEnablePublishValidation=true', '-o', "$Workspace/rejected-protocol") 'NZB006' | Out-Null

    'test payload' | Set-Content "$Workspace/Probe/payload.txt"
    Write-Probe '<Content Include="payload.txt" Link="Nazor.DevTools/bundle.js" CopyToPublishDirectory="Always" />'
    Invoke-Probe @('publish', $project, '-c', 'Release', '-p:NazorEnablePublishValidation=true', '-o', "$Workspace/rejected-content") 'NZB006' | Out-Null
    Invoke-Probe @('publish', $project, '-c', 'Release', '-r', 'win-x64', '-p:PublishSingleFile=true', '-p:NazorEnablePublishValidation=true', '-o', "$Workspace/rejected-bundled-content") 'NZB006' | Out-Null
    $nativeRejection = Invoke-Probe @('publish', $project, '-c', 'Release', '-r', 'win-x64', '-p:PublishAot=true', '-p:NazorEnablePublishValidation=true', '-o', "$Workspace/rejected-native-content") 'NZB006'
    if ($nativeRejection -match 'Generating native code') { throw 'Production payload validation ran after native compilation.' }
    $trimRejection = Invoke-Probe @('publish', $project, '-c', 'Release', '-r', 'win-x64', '-p:PublishAot=false', '-p:PublishTrimmed=true', '-o', "$Workspace/rejected-trim-content") 'NZB006'
    if ($trimRejection -match 'Optimizing assemblies') { throw 'Production payload validation ran after IL trimming.' }
    Write-Probe
    New-Item -ItemType Directory "$Workspace/stale-output" | Out-Null
    'stale payload' | Set-Content "$Workspace/stale-output/Nazor.DevTools.UI.js"
    Invoke-Probe @('publish', $project, '-c', 'Release', '-p:NazorEnablePublishValidation=true', '-o', "$Workspace/stale-output") 'NZB006' | Out-Null
    Invoke-Probe @('msbuild', $project, '-t:NazorValidateProductionSettings', '-p:NazorEnablePublishValidation=true', '-p:PublishTrimmed=true', '-p:PublishAot=false') | Out-Null
    Invoke-Probe @('msbuild', $project, '-t:NazorValidateDevToolsSettings', '-p:PublishAot=true', '-p:NazorEnableDevTools=true') 'NZB005' | Out-Null
    Invoke-Probe @('msbuild', $project, '-t:NazorValidateDevToolsSettings', '-p:NazorEnableDevTools=invalid') 'NZB008' | Out-Null
    Invoke-Probe @('msbuild', $project, '-t:NazorValidateDevToolsSettings', '-p:NazorEnablePublishValidation=invalid') 'NZB008' | Out-Null
    $program = Get-Content "$Workspace/Probe/Program.cs" -Raw
    $program = $program.Replace('var builder = NazorApplication.CreateBuilder();', "VerifyTrimWarning();`nvar builder = NazorApplication.CreateBuilder();")
    $program.Replace('public sealed class Probe', "[System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(`"application trim warning`" )]`nstatic void VerifyTrimWarning() { }`n`npublic sealed class Probe") | Set-Content "$Workspace/Probe/Program.cs"
    Invoke-Probe @('publish', $project, '-c', 'Release', '-r', 'win-x64', '-p:PublishAot=false', '-p:PublishTrimmed=true', '-p:TreatWarningsAsErrors=true', '-o', "$Workspace/rejected-app-trim-warning") 'IL2026' | Out-Null
}
finally { Pop-Location }
Write-Host "PASS: DT-03 packaged $Framework development and production matrix. Logs: $Workspace"
