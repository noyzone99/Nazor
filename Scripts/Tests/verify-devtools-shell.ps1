#Requires -Version 7
param(
    [string]$Workspace = (Join-Path (Resolve-Path "$PSScriptRoot/../..").Path ('artifacts/devtools-dt07/' + [guid]::NewGuid().ToString('N'))),
    [string]$ArtifactsPath,
    [switch]$Native
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$Workspace = [IO.Path]::GetFullPath($Workspace)
if (Test-Path $Workspace) { throw "Choose a fresh workspace: $Workspace" }
if ($Native -and -not $IsWindows) { throw 'The native shell probe requires Windows.' }
New-Item -ItemType Directory $Workspace | Out-Null
$script:step = 0
function Invoke-Checked([string]$Command, [string[]]$Arguments) {
    $script:step++
    $output = & $Command @Arguments 2>&1 | Out-String
    $code = $LASTEXITCODE
    $output | Set-Content "$Workspace/$('{0:D2}' -f $script:step).log"
    if ($code -ne 0) { throw "$Command $Arguments failed: $output" }
    Write-Host $output
}
$buildOptions = @()
if ($ArtifactsPath) {
    $ArtifactsPath = [IO.Path]::GetFullPath($ArtifactsPath)
    $buildOptions = @('--artifacts-path', $ArtifactsPath)
}
function Binary([string]$Project, [string]$Framework) {
    if ($ArtifactsPath) {
        $name = Split-Path $Project -Leaf
        $flavor = if ($name -eq 'DevToolsAgent') { "release_$Framework" } else { 'release' }
        return "$ArtifactsPath/bin/$name/$flavor/$name.dll"
    }
    return "$root/$Project/bin/Release/$Framework/$(Split-Path $Project -Leaf).dll"
}
Invoke-Checked dotnet (@('build', "$root/Tests/Fixtures/DevToolsAgent", '-c', 'Release', '-nodeReuse:false', '-v:q') + $buildOptions)
Invoke-Checked dotnet (@('build', "$root/Tests/Nazor.DevTools.Tests", '-c', 'Release', '-nodeReuse:false', '-v:q') + $buildOptions)
$previous = $env:NAZOR_DEVTOOLS_FIXTURE
try {
    foreach ($framework in 'net8.0', 'net9.0', 'net10.0') {
        $env:NAZOR_DEVTOOLS_FIXTURE = Binary 'Tests/Fixtures/DevToolsAgent' $framework
        Invoke-Checked dotnet @((Binary 'Tests/Nazor.DevTools.Tests' 'net10.0'))
    }
}
finally { $env:NAZOR_DEVTOOLS_FIXTURE = $previous }
if ($Native) {
    @'
param([long]$Handle, [ValidateSet('appearance','saved-appearance','connect','pick','picked','details','history','retained-details','idle','frozen','live','disconnected')][string]$Action)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
function View { [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$Handle) }
function ById([string]$id) {
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return (View).FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}
function HasText([string]$text) {
    $items = (View).FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($item in $items) { $name = $item.Current.Name; if ($name -and $name.StartsWith($text, [StringComparison]::Ordinal)) { return $true } }
    return $false
}
function Wait([scriptblock]$condition) {
    $end = [DateTime]::UtcNow.AddSeconds(8)
    while ([DateTime]::UtcNow -lt $end) {
        try { if (& $condition) { return } }
        catch [System.Windows.Automation.ElementNotAvailableException] { }
        Start-Sleep -Milliseconds 50
    }
    throw 'Native shell condition timed out.'
}
function Invoke([string]$id) {
    Wait { $button = ById $id; $null -ne $button -and $button.Current.IsEnabled }
    (ById $id).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}
function SelectTab([string]$name) {
    $byName = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $byType = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::TabItem)
    $tab = (View).FindFirst([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.AndCondition]::new($byName, $byType))
    if ($null -eq $tab) { throw "Missing tab: $name" }
    $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
}
if ($Action -eq 'connect') {
    Invoke 'connect-target'; Wait { HasText 'Live capture' }
    Wait { (View).Current.Name -eq 'Nazor DevTools - DevToolsAgent' }
}
function OpenSettings { if ($null -eq (ById 'scheme-system')) { Invoke 'settings' }; Wait { $null -ne (ById 'scheme-system') } }
function CloseSettings { if ($null -ne (ById 'scheme-system')) { Invoke 'settings' }; Wait { $null -eq (ById 'scheme-system') } }
function SchemeSelected([string]$id) { (ById $id).GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected }
if ($Action -eq 'saved-appearance') { OpenSettings; Wait { SchemeSelected 'scheme-system' }; CloseSettings }
if ($Action -eq 'appearance') {
    Add-Type -AssemblyName System.Drawing
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class InspectorWindowProbe {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
}
"@
    Wait { [InspectorWindowProbe]::SendMessage([IntPtr]$Handle, 0x7f, [IntPtr]1, [IntPtr]::Zero) -ne [IntPtr]::Zero }
    foreach ($scheme in 'Dark', 'Light', 'System') {
        $id = 'scheme-' + $scheme.ToLowerInvariant()
        OpenSettings
        (ById $id).GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
        Wait { SchemeSelected $id }
        CloseSettings
        Start-Sleep -Milliseconds 150
        $bounds = (View).Current.BoundingRectangle
        $bitmap = New-Object System.Drawing.Bitmap ([int]$bounds.Width), ([int]$bounds.Height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $dc = $graphics.GetHdc()
        try { if (-not [InspectorWindowProbe]::PrintWindow([IntPtr]$Handle, $dc, 2)) { throw 'Cannot capture the native inspector.' } }
        finally { $graphics.ReleaseHdc($dc); $graphics.Dispose() }
        try { $bitmap.Save((Join-Path $PSScriptRoot ("inspector-" + $scheme + ".png"))) }
        finally { $bitmap.Dispose() }
    }
}
if ($Action -eq 'pick') { Invoke 'pick-control'; Wait { HasText 'Cancel picking' } }
if ($Action -eq 'idle') { Wait { HasText 'Pick control' } }
if ($Action -eq 'picked') {
    SelectTab 'Elements'
    Invoke 'show-producing-component'
    Wait { HasText 'Nazor.DevTools.Fixtures.MultiOutput' }
    $buttons = (View).FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    $output = $null
    foreach ($button in $buttons) { if ($button.Current.AutomationId -and $button.Current.AutomationId.StartsWith('output-', [StringComparison]::Ordinal)) { $output = $button; break } }
    if ($null -eq $output) { throw 'The picked component has no output links.' }
    $output.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Invoke 'highlight-selection'
    SelectTab 'Properties'; Wait { HasText 'Markup ID' }
}
if ($Action -eq 'retained-details') { Wait { HasText 'Disconnected' } }
if ($Action -in 'details', 'retained-details') {
    SelectTab 'Elements'
    SelectTab 'Styles'; Wait { HasText 'theme.nss' }
    SelectTab 'Computed'; Wait { HasText 'Target scale: 1 physical' }
    Wait { HasText 'Border' }
}
if ($Action -in 'history', 'retained-details') {
    SelectTab 'Performance'; Wait { HasText 'Frame 125 ' }
    Wait { HasText 'Total node count' }
    if ($Action -eq 'retained-details') { Wait { HasText 'Retained live capture' } }
}
if ($Action -eq 'frozen') { Invoke 'refresh-capture'; Wait { HasText 'UI unavailable' } }
if ($Action -eq 'live') { Invoke 'refresh-capture'; Wait { HasText 'Live capture' } }
if ($Action -eq 'disconnected') { Wait { HasText 'Disconnected' }; Wait { HasText 'Retained capture' } }
if ($Action -in 'frozen', 'disconnected') {
    $name = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Components')
    $type = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::TabItem)
    $tab = (View).FindFirst([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.AndCondition]::new($name, $type))
    if ($null -eq $tab) { throw 'The Components tab disappeared.' }
    $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::TreeItem)
    $row = (View).FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if ($null -eq $row) { throw 'Retained component rows disappeared.' }
    $row.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    Wait { HasText 'Nazor.DevTools.Fixtures.TreePage' }
}
"PASS: native shell $Action"
'@ | Set-Content "$Workspace/uia.ps1"
    function Start-Target([string[]]$Arguments) {
        $start = [Diagnostics.ProcessStartInfo]::new('dotnet')
        foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
        $start.UseShellExecute = $false; $start.CreateNoWindow = $true; $start.WindowStyle = 'Hidden'
        $start.RedirectStandardInput = $true; $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
        return [Diagnostics.Process]::Start($start)
    }
    function Start-Shell([string]$Directory) {
        $shell = Start-Target @((Binary 'Sources/Tools/Nazor.DevTools' 'net10.0'), '--discovery-directory', $Directory, '--no-activate')
        $end = [DateTime]::UtcNow.AddSeconds(12)
        do {
            if ($shell.HasExited) { throw "The shell exited: $($shell.StandardError.ReadToEnd())" }
            $shell.Refresh()
            if ($shell.MainWindowHandle -ne [IntPtr]::Zero) { return $shell }
            Start-Sleep -Milliseconds 50
        } while ([DateTime]::UtcNow -lt $end)
        $shell.Kill($true); $shell.Dispose(); throw 'The native shell did not create its window.'
    }
    function Probe([Diagnostics.Process]$Shell, [string]$Action) {
        Invoke-Checked powershell.exe @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', "$Workspace/uia.ps1", '-Handle', $Shell.MainWindowHandle.ToInt64(), '-Action', $Action)
    }
    function Command([Diagnostics.Process]$Target, [string]$Command, [string]$Expected) {
        $Target.StandardInput.WriteLine($Command); $Target.StandardInput.Flush()
        $line = $Target.StandardOutput.ReadLineAsync().WaitAsync([TimeSpan]::FromSeconds(5)).GetAwaiter().GetResult()
        if ($line -ne $Expected) { throw "Expected $Expected, got $line" }
    }
    $target = Start-Target @((Binary 'Tests/Fixtures/DevToolsAgent' 'net10.0'), "$Workspace/discovery")
    $shell = $null
    $preference = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Nazor/DevTools.appearance'
    $savedPreference = if (Test-Path $preference) { [IO.File]::ReadAllBytes($preference) } else { $null }
    try {
        $descriptor = $target.StandardOutput.ReadLineAsync().WaitAsync([TimeSpan]::FromSeconds(8)).GetAwaiter().GetResult() | ConvertFrom-Json
        $shell = Start-Shell "$Workspace/discovery"
        Probe $shell 'connect'
        Probe $shell 'pick'
        Command $target 'pick' 'PICKED'
        Probe $shell 'picked'
        Probe $shell 'appearance'
        Probe $shell 'details'
        Command $target 'frames' 'FRAMES'
        Probe $shell 'history'
        Command $target 'frame-count' '125'
        Probe $shell 'live'
        Probe $shell 'picked'
        Probe $shell 'pick'
        Command $target 'escape' 'ESCAPED'
        Probe $shell 'idle'
        Command $target 'freeze' 'FROZEN'
        Probe $shell 'frozen'
        $target.StandardInput.WriteLine('resume'); $target.StandardInput.Flush()
        Command $target 'ping' 'PONG'
        Probe $shell 'live'
        if (-not $shell.CloseMainWindow() -or -not $shell.WaitForExit(5000)) { throw 'The tools did not close independently.' }
        if ($shell.ExitCode -ne 0) { throw 'The tools failed on close.' }
        $shell.StandardError.ReadToEnd() | Set-Content "$Workspace/shell-first.err.log"
        $shell.Dispose(); $shell = $null
        Command $target 'ping' 'PONG'
        $shell = Start-Shell "$Workspace/discovery"
        Probe $shell 'saved-appearance'
        Probe $shell 'connect'
        Probe $shell 'pick'
        Command $target 'pick' 'PICKED'
        Probe $shell 'picked'
        Probe $shell 'details'
        Probe $shell 'history'
        Command $target 'frame-count' '125'
        $target.StandardInput.WriteLine('exit'); $target.StandardInput.Flush()
        if (-not $target.WaitForExit(5000)) { throw 'The target did not exit.' }
        Probe $shell 'retained-details'
        Probe $shell 'disconnected'
        Write-Host 'PASS: closing tools preserves the app; app exit preserves interactive retained data.'
    }
    finally {
        if ($null -ne $shell) {
            if (-not $shell.HasExited -and (-not $shell.CloseMainWindow() -or -not $shell.WaitForExit(5000))) { $shell.Kill($true) }
            $shell.WaitForExit(); $shell.StandardError.ReadToEnd() | Set-Content "$Workspace/shell-last.err.log"; $shell.Dispose()
        }
        if (-not $target.HasExited) {
            $target.StandardInput.WriteLine('resume'); $target.StandardInput.WriteLine('exit'); $target.StandardInput.Flush()
            if (-not $target.WaitForExit(5000)) { $target.Kill($true); $target.WaitForExit() }
        }
        if ($null -ne $savedPreference) { [IO.File]::WriteAllBytes($preference, $savedPreference) }
        elseif (Test-Path $preference) { Remove-Item -LiteralPath $preference }
        $target.StandardError.ReadToEnd() | Set-Content "$Workspace/target.err.log"; $target.Dispose()
    }
}
Write-Host "PASS: external DevTools shell, retained cache and linked navigation. Evidence: $Workspace"
