param([long]$Handle, [int]$ProcessId, [string]$Value, [string]$Stop, [switch]$Retained)
$ErrorActionPreference = 'Stop'
if ($ProcessId -gt 0) {
    $end = [DateTime]::UtcNow.AddSeconds(15)
    while ($Handle -eq 0 -and [DateTime]::UtcNow -lt $end) {
        $Handle = [Diagnostics.Process]::GetProcessById($ProcessId).MainWindowHandle.ToInt64()
        if ($Handle -eq 0) { Start-Sleep -Milliseconds 50 }
    }
}
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
function View { [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$Handle) }
function Wait([scriptblock]$Condition) {
    $end = [DateTime]::UtcNow.AddSeconds(15)
    while ([DateTime]::UtcNow -lt $end) {
        try { if (& $Condition) { return } } catch [System.Windows.Automation.ElementNotAvailableException] { }
        Start-Sleep -Milliseconds 50
    }
    $names = (View).FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition) | ForEach-Object { $_.Current.Name } | Where-Object { $_ } | Select-Object -First 80
    throw ("The actual IDE inspector did not expose the expected current-stop data. UI: " + ($names -join ' | '))
}
function HasText([string]$text, [string]$suffix = '') {
    foreach ($item in (View).FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($item.Current.Name -and $item.Current.Name.StartsWith($text, [StringComparison]::Ordinal) -and $item.Current.Name.EndsWith($suffix, [StringComparison]::Ordinal)) { return $true }
    }
    return $false
}
if ($Retained) { Wait { HasText 'Disconnected' } } else { Wait { HasText 'Paused' } }
Wait {
    $type = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::TreeItem)
    foreach ($item in (View).FindAll([System.Windows.Automation.TreeScope]::Descendants, $type)) {
        if ($item.Current.Name -and $item.Current.Name.Contains('ParentComponent')) {
            $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); return $true
        }
    }
    return $false
}
if ($Retained) { Wait { HasText 'Retained debugger fields' (' ' + $Stop) } }
else { Wait { HasText 'Current-stop fields' (' ' + $Stop) } }
Wait { HasText "Value: $Value " }
Wait { HasText 'Dangerous: <unavailable>' }
"PASS: native IDE inspector stop $Stop, current Value=$Value and unavailable getter."
