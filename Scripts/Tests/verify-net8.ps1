#Requires -Version 7
param(
    [string]$Workspace,
    [ValidateSet('net8.0', 'net9.0')][string]$Framework = 'net8.0',
    [switch]$SkipPack
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$feed = Join-Path $root 'artifacts/packages'
$applicationName = if ($Framework -eq 'net9.0') { 'HelloNazor9' } else { 'HelloNazor8' }
if (-not $Workspace) { $Workspace = Join-Path $root ("artifacts/" + $Framework.Replace('.0', '') + '-vs2022') }
$version = (Select-Xml -Path "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
if (-not $IsWindows) { throw 'This verification requires Windows and Visual Studio 2022 17.14.' }
$Workspace = [IO.Path]::GetFullPath($Workspace)
if (Test-Path $Workspace) { throw "Choose a new workspace; existing files are preserved: $Workspace" }
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$msbuild = if (Test-Path $vswhere) {
    & $vswhere -latest -version '[17.14,18.0)' -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
}
if (-not $msbuild) { throw 'Visual Studio 2022 17.14 with MSBuild was not found.' }
if (-not $SkipPack) {
    & "$root/Scripts/Packaging/pack.ps1"
}
if (-not (Test-Path "$feed/Nazor.Sdk.$version.nupkg")) { throw 'Pack Nazor before running with -SkipPack.' }
New-Item -ItemType Directory -Path $Workspace | Out-Null
foreach ($file in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') {
    '<Project />' | Set-Content (Join-Path $Workspace $file)
}
@'
{
  "sdk": { "version": "9.0.100", "rollForward": "latestFeature", "allowPrerelease": false }
}
'@ | Set-Content "$Workspace/global.json"
@"
<configuration>
  <config><add key="globalPackagesFolder" value="$Workspace/.packages" /></config>
  <packageSources>
    <clear />
    <add key="nazor" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="nazor"><package pattern="Nazor.*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
  <fallbackPackageFolders>
    <clear />
    <add key="user" value="$(Join-Path $env:USERPROFILE '.nuget/packages')" />
  </fallbackPackageFolders>
</configuration>
"@ | Set-Content "$Workspace/nuget.config"

function Invoke-Checked([string]$Command, [string[]]$Arguments, [string]$Log) {
    $output = & $Command @Arguments 2>&1 | Out-String
    $output | Set-Content (Join-Path $Workspace $Log)
    if ($LASTEXITCODE -ne 0) { throw "$Command failed: $output" }
    return $output
}

Push-Location $Workspace
try {
    Invoke-Checked dotnet @('new', 'install', "$feed/Nazor.Templates.$version.nupkg", '--debug:custom-hive', "$Workspace/.templates") 'template-install.log' | Out-Null
    Invoke-Checked dotnet @('new', 'nazor-app', '-n', $applicationName, '-o', $applicationName, '--Framework', $Framework, '--debug:custom-hive', "$Workspace/.templates") 'template-create.log' | Out-Null
    Invoke-Checked dotnet @('new', 'sln', '-n', $applicationName) 'solution-create.log' | Out-Null
    Invoke-Checked dotnet @('sln', "$applicationName.sln", 'add', "$applicationName/$applicationName.csproj") 'solution-add.log' | Out-Null
    $project = Join-Path $Workspace "$applicationName/$applicationName.csproj"
    Invoke-Checked $msbuild @($project, '-restore', '-p:Configuration=Debug', '-nologo', '-nodeReuse:false', '-v:m') 'build.log' | Out-Null
    $app = Join-Path $Workspace "$applicationName/bin/Debug/$Framework"
    if ((Get-ChildItem $app -Filter 'Nazor.Diagnostics*') -or (Get-ChildItem $app -Filter 'Nazor.DevTools*') -or
        (Select-String -Path "$Workspace/$applicationName/obj/project.assets.json" -Pattern '"Nazor\.(Diagnostics|DevTools[^"/]*)/' -Quiet)) {
        throw 'An ordinary application must not depend on Diagnostics or DevTools.'
    }
    $runtime = Get-Content "$app/$applicationName.runtimeconfig.json" -Raw | ConvertFrom-Json
    if ($runtime.runtimeOptions.tfm -ne $Framework) { throw 'The application target does not match the requested framework.' }
    $designTime = Invoke-Checked $msbuild @($project, '-nologo', '-nodeReuse:false', '-p:Configuration=Debug', '-p:DesignTimeBuild=true',
        '-p:SkipCompilerExecution=true', '-p:ProvideCommandLineArgs=true', '-p:BuildingInsideVisualStudio=true',
        '-t:CompileDesignTime', '-getItem:Compile') 'design-time.log' | ConvertFrom-Json
    if (-not ($designTime.Items.Compile.Identity | Where-Object { $_ -like '*App.nazor.g.cs' })) {
        throw 'The VS2022 design-time build did not include the generated component.'
    }
    Write-Host 'PASS: the application template builds with VS2022 and supplies generated C# to IntelliSense.'
    $discovery = Join-Path $Workspace 'dev'
    New-Item -ItemType Directory $discovery | Out-Null
    $start = [Diagnostics.ProcessStartInfo]::new((Join-Path $app "$applicationName.exe"))
    $start.WorkingDirectory = $app
    $start.UseShellExecute = $false
    $start.WindowStyle = 'Hidden'
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Environment['NAZOR_DEVCHANNEL'] = '1'
    $start.Environment['NAZOR_DEV_DIRECTORY'] = $discovery
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $previousDiscovery = $env:NAZOR_DEV_DIRECTORY
    try {
        $env:NAZOR_DEV_DIRECTORY = $discovery
        $compiler = Join-Path $Workspace ".packages/nazor.sdk/$version/tools/net10.0/Nazor.Compiler.dll"
        $deadline = [DateTime]::UtcNow.AddSeconds(20)
        $info = $null
        do {
            if ($process.HasExited) { throw "The application application exited during startup: $($process.ExitCode)." }
            if (Test-Path "$discovery/$($process.Id).json") {
                $reply = & dotnet $compiler dev $process.Id info 2>&1 | Out-String
                if ($LASTEXITCODE -eq 0) {
                    $info = $reply | ConvertFrom-Json
                    if ($info.pid -eq $process.Id -and $info.windows.Count -gt 0) { break }
                }
            }
            Start-Sleep -Milliseconds 200
        } while ([DateTime]::UtcNow -lt $deadline)
        if ($null -eq $info -or $info.windows.Count -eq 0) { throw 'The application application did not start its native UI loop.' }
        $info | ConvertTo-Json -Depth 10 | Set-Content "$Workspace/app-info.json"
        $process.Refresh()
        @'
param([int]$ApplicationPid)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class Net8Window
{
    private delegate bool Callback(IntPtr window, IntPtr state);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Callback callback, IntPtr state);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    public static IntPtr Find(int processId)
    {
        IntPtr result = IntPtr.Zero;
        EnumWindows((window, state) => {
            uint owner;
            GetWindowThreadProcessId(window, out owner);
            if (owner != processId) return true;
            result = window;
            return false;
        }, IntPtr.Zero);
        return result;
    }
}
"@
$root = [System.Windows.Automation.AutomationElement]::FromHandle([Net8Window]::Find($ApplicationPid))
$buttonCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$button = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $buttonCondition)
if ($null -eq $button -or $button.Current.Name -ne 'Clicked 0 times') { throw 'The application button is missing from the accessibility tree.' }
$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
$deadline = [DateTime]::UtcNow.AddSeconds(5)
while ($button.Current.Name -ne 'Clicked 1 times' -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
if ($button.Current.Name -ne 'Clicked 1 times') { throw 'The application button did not update through UI Automation.' }
$editCondition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
$edit = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editCondition)
$pattern = $edit.GetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern)
$null = $pattern.DocumentRange.GetText(-1)
'PASS: Application UI Automation invokes the button and reads the textbox document.'
'@ | Set-Content "$Workspace/check-uia.ps1"
        Invoke-Checked powershell.exe @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', "$Workspace/check-uia.ps1", '-ApplicationPid', $process.Id) 'uia.log' | Write-Host
        Write-Host 'PASS: the native application responds to the net10 development tools.'
    }
    finally {
        $env:NAZOR_DEV_DIRECTORY = $previousDiscovery
        if (-not $process.HasExited -and (-not $process.CloseMainWindow() -or -not $process.WaitForExit(5000))) { $process.Kill($true) }
        $process.WaitForExit()
        $stdout.GetAwaiter().GetResult() | Set-Content "$Workspace/app.out.log"
        $stderr.GetAwaiter().GetResult() | Set-Content "$Workspace/app.err.log"
        $process.Dispose()
    }
}
finally { Pop-Location }
Write-Host "Open $Workspace/$applicationName.sln in Visual Studio 2022 and press F5."
& "$PSScriptRoot/verify-devtools-publish.ps1" -Workspace "$Workspace/DevToolsPolicy" -Framework $Framework -Feed $feed
