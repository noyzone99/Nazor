#Requires -Version 7
[CmdletBinding()]
param(
    [ValidateRange(10,10000)][int]$Samples=120,
    [ValidateRange(5,1000)][int]$Warmup=30,
    [ValidateRange(1,20)][int]$Repetitions=4,
    [string]$OutputDirectory,
    [switch]$SkipBuild
)

$ErrorActionPreference='Stop'
if (-not $IsWindows) { throw 'The matched native renderer comparison currently targets Windows.' }
$root=(Resolve-Path "$PSScriptRoot/../..").Path
if (-not $OutputDirectory) { $OutputDirectory=Join-Path $root ('artifacts/renderer-comparison/'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8)) }
$output=[System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "Refusing to overwrite an existing result directory: $output" }
New-Item -ItemType Directory -Path $output | Out-Null

function Assert([bool]$Condition,[string]$Message) { if (-not $Condition) { throw $Message } }
function Percentile([double[]]$Values,[double]$Fraction) {
    Assert ($Values.Count -gt 0) 'No samples were recorded.'
    [Array]::Sort($Values)
    $Values[[Math]::Max(0,[int][Math]::Ceiling($Values.Count*$Fraction)-1)]
}
function Check-Image($Report,[string]$File) {
    Assert ((Test-Path -LiteralPath ($File+'.png')) -and (Test-Path -LiteralPath ($File+'.final.png'))) "Missing validation snapshot: $File"
    $initial=[System.Drawing.Bitmap]::new($File+'.png')
    $final=[System.Drawing.Bitmap]::new($File+'.final.png')
    try {
        $scale=[double]$Report.environment.scale
        Assert ($initial.Width -eq [int][Math]::Round(900*$scale) -and $initial.Height -eq [int][Math]::Round(600*$scale)) "Incorrect snapshot size: $File"
        Assert ($initial.Size -eq $final.Size) "Snapshot size changed: $File"
        for ($index=0;$index -lt 80;$index++) {
            $x=12+($index%8)*108; $y=12+[Math]::Floor($index/8)*48
            $before=$initial.GetPixel([int][Math]::Floor(($x+88)*$scale),[int][Math]::Floor(($y+34)*$scale))
            Assert ($before.A -eq 255 -and $before.R -eq 220 -and $before.G -eq 232 -and $before.B -eq 242) "Missing initial card $index in $File"
            $after=$final.GetPixel([int][Math]::Floor(($x+88)*$scale),[int][Math]::Floor(($y+34)*$scale))
            $expected=if ($Report.scenario -eq 'paint') { @(240,220,186) } else { @(220,232,242) }
            Assert ($after.A -eq 255 -and $after.R -eq $expected[0] -and $after.G -eq $expected[1] -and $after.B -eq $expected[2]) "Incorrect updated card $index in $File"
            if ($Report.scenario -eq 'layout') {
                $beforeEdge=$initial.GetPixel([int][Math]::Floor(($x+102)*$scale),[int][Math]::Floor(($y+25)*$scale))
                $afterEdge=$final.GetPixel([int][Math]::Floor(($x+102)*$scale),[int][Math]::Floor(($y+25)*$scale))
                Assert ($beforeEdge.R -eq 255 -and $beforeEdge.G -eq 255 -and $beforeEdge.B -eq 255) "Initial card width is wrong: $File"
                Assert ($afterEdge.R -eq 220 -and $afterEdge.G -eq 232 -and $afterEdge.B -eq 242) "Updated card width is wrong: $File"
            }
        }
        if ($Report.scenario -eq 'text') {
            Assert ((Get-FileHash ($File+'.png')).Hash -ne (Get-FileHash ($File+'.final.png')).Hash) "Text did not change in the rendered output: $File"
        }
    }
    finally { $initial.Dispose(); $final.Dispose() }
}

Push-Location $root
try {
    if (-not $SkipBuild) {
        foreach ($project in @('Nazor.Benchmarks','Nazor.Avalonia.Benchmarks')) {
            & dotnet build "Tests/$project" -c Release -m:1 -nodeReuse:false -p:UseSharedCompilation=false
            Assert ($LASTEXITCODE -eq 0) "$project build failed."
        }
    }
    $cases=@(
        @{Framework='Nazor';Backend='CPU'},@{Framework='Nazor';Backend='GPU'},
        @{Framework='Avalonia';Backend='CPU'},@{Framework='Avalonia';Backend='GPU'}
    )
    $results=[System.Collections.Generic.List[object]]::new()
    $execution=[System.Collections.Generic.List[object]]::new()
    $baseline=$null
    $gpuDevice=$null
    for ($repeat=0;$repeat -lt $Repetitions;$repeat++) {
        foreach ($scenario in @('text','paint','layout')) {
            for ($offset=0;$offset -lt $cases.Count;$offset++) {
                $case=$cases[($offset+$repeat)%$cases.Count]
                $framework=$case.Framework; $backend=$case.Backend
                $file=Join-Path $output "$scenario-$framework-$backend-$($repeat+1).json"
                $project=if ($framework -eq 'Nazor') { 'Nazor.Benchmarks' } else { 'Nazor.Avalonia.Benchmarks' }
                $binary=Join-Path $root "Tests/$project/bin/Release/net10.0/$project.exe"
                Assert (Test-Path -LiteralPath $binary) "Missing benchmark executable: $binary"
                $arguments=@('--backend',$backend,'--scenario',$scenario,'--samples',"$Samples",'--warmup',"$Warmup",'--out',$file)
                if ($framework -eq 'Nazor') { $arguments=@('--compare')+$arguments }
                Write-Host "Run $($repeat+1)/${Repetitions}: $scenario $framework $backend"
                & $binary @arguments > ($file+'.log') 2>&1
                Assert ($LASTEXITCODE -eq 0) "Benchmark failed; inspect $file.log"
                $report=Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
                Assert ($report.schema -eq 1 -and $report.framework -eq $framework -and $report.requestedBackend -eq $backend -and $report.scenario -eq $scenario) "Unexpected report identity: $file"
                Assert ($report.samples -eq $Samples -and $report.warmup -eq $Warmup -and $report.completionSamplesMs.Count -eq $Samples) "Incomplete sample set: $file"
                foreach ($number in @($report.cpuMsPerUpdate,$report.allocatedKBPerUpdate)+@($report.completionSamplesMs)) {
                    Assert ([double]::IsFinite([double]$number) -and [double]$number -ge 0) "Invalid measurement: $file"
                }
                $signature=@($report.environment.machine,$report.environment.runtime,$report.environment.os,$report.environment.architecture,$report.environment.scale,$report.workload.fontSha256,$report.workload.width,$report.workload.height,$report.workload.cards) -join '|'
                if ($null -eq $baseline) { $baseline=$signature }
                Assert ($signature -eq $baseline) "Machine, runtime, DPI or workload changed during the comparison: $file"
                if ($backend -eq 'GPU') {
                    Assert (-not [string]::IsNullOrWhiteSpace($report.device)) "GPU hardware was not identified: $file"
                    $device=([regex]::Replace($report.device,'\s+',' ')).Trim().ToUpperInvariant()
                    if ($null -eq $gpuDevice) { $gpuDevice=$device }
                    Assert ($device -eq $gpuDevice) "GPU device identities differ; verify adapter selection before comparing: $file"
                }
                Check-Image $report $file
                $results.Add([pscustomobject]@{Repetition=$repeat+1;Report=$report;File=[System.IO.Path]::GetFileName($file)})
                $execution.Add([pscustomobject]@{Repetition=$repeat+1;Scenario=$scenario;Framework=$framework;Backend=$backend})
            }
        }
    }
    $summary=@(foreach ($group in ($results | Group-Object { "$($_.Report.scenario)|$($_.Report.framework)|$($_.Report.requestedBackend)" })) {
        $rows=@($group.Group.Report)
        $first=$rows[0]
        $wall=@($rows | ForEach-Object { $_.completionSamplesMs })
        [pscustomobject]@{
            Scenario=$first.scenario; Framework=$first.framework; Backend=$first.requestedBackend; NativeBackend=$first.actualBackend; Device=$first.device
            CompletionMedianMs=Percentile $wall 0.5; CompletionP95Ms=Percentile $wall 0.95
            CpuMsPerUpdate=Percentile @($rows.cpuMsPerUpdate) 0.5
            AllocatedKBPerUpdate=Percentile @($rows.allocatedKBPerUpdate) 0.5
            WorkingSetMB=Percentile @($rows.workingSetMB) 0.5
            RunMedianMinMs=($rows.completionMs.median | Measure-Object -Minimum).Minimum
            RunMedianMaxMs=($rows.completionMs.median | Measure-Object -Maximum).Maximum
        }
    })
    $commit=& git rev-parse HEAD
    $manifest=[ordered]@{
        Schema=1;Commit=$commit;Samples=$Samples;Warmup=$Warmup;Repetitions=$Repetitions
        Environment=$results[0].Report.environment
        GpuDrivers=@(Get-CimInstance Win32_VideoController -ErrorAction SilentlyContinue | Select-Object Name,DriverVersion,DriverDate)
        ExecutionOrder=@($execution);Results=@($results);Summary=$summary
        Interpretation='Completion includes pacing and UI notification. CPU means total process CPU per update; allocations include all threads. Pool completion samples; take medians of per-process CPU/allocation/memory values. One update in flight; throughput is not a continuous-animation FPS benchmark.'
    }
    $manifest | ConvertTo-Json -Depth 14 | Set-Content (Join-Path $output 'summary.json') -Encoding utf8
    $lines=[System.Collections.Generic.List[string]]::new()
    $lines.Add('# Nazor / Avalonia native renderer comparison')
    $lines.Add('')
    $lines.Add("Commit: $commit. $Repetitions fresh processes per case; $Samples measured updates and $Warmup warmup updates per process.")
    $lines.Add('')
    $lines.Add('| Scenario | Framework | Renderer | Completion median / p95 ms | CPU ms/update | Allocated KB/update | Working set MB |')
    $lines.Add('| --- | --- | --- | ---: | ---: | ---: | ---: |')
    foreach ($row in ($summary | Sort-Object Scenario,Framework,Backend)) {
        $lines.Add([string]::Format([System.Globalization.CultureInfo]::InvariantCulture,
            '| {0} | {1} | {2} | {3:F3} / {4:F3} | {5:F3} | {6:F1} | {7:F1} |',
            $row.Scenario,$row.Framework,$row.Backend,$row.CompletionMedianMs,$row.CompletionP95Ms,$row.CpuMsPerUpdate,$row.AllocatedKBPerUpdate,$row.WorkingSetMB))
    }
    $lines.Add('')
    $lines.Add('Completion includes backend pacing/waits and UI notification, not physical scanout or GPU execution. Nazor GPU uses WGL; Avalonia GPU uses ANGLE/Direct3D 11 and its normal composition settings. These are different presentation policies. Compare CPU work and allocations as well as completion; do not infer a general framework ranking from completion ratios. Default text antialiasing may differ.')
    $lines.Add('')
    $lines.Add('All 80 card bounds/content and before/after image probes passed. Snapshots and final validation are outside the measurement window. Every JSON retains individual completion samples, framework/runtime/font/device metadata and the completion signal used. CPU/memory values are medians across processes; completion percentiles pool all process samples. summary.json also records execution order and the range of per-process completion medians.')
    $lines | Set-Content (Join-Path $output 'report.md') -Encoding utf8
    Write-Host "Comparison completed: $output/report.md"
}
finally { Pop-Location }
