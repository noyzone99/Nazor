#Requires -Version 7
param(
    [ValidateSet('single', 'trim', 'trim-single', 'aot')][string]$Mode = 'single',
    [ValidateSet('net8.0', 'net9.0', 'net10.0', 'net11.0')][string]$Framework = 'net10.0',
    [string]$Project = "$PSScriptRoot/../../Tests/Fixtures/Deployment/Deployment.csproj",
    [ValidateSet('CPU', 'GPU')][string]$Backend = 'CPU',
    [ValidateSet('Default', 'All')][string]$SingleFileExtraction = 'Default',
    [switch]$SkipPublish,
    [string]$Output = "$PSScriptRoot/../../artifacts/deployment/windows"
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Use verify-deployment.sh on Linux.' }
$Output = [IO.Path]::GetFullPath($Output)
$project = [IO.Path]::GetFullPath($Project)
New-Item -ItemType Directory $Output -Force | Out-Null
foreach ($variant in @('directory', $Mode)) {
    $directory = "$Output/$variant"
    $options = @('-p:PublishAot=false', '-p:PublishSingleFile=false', '-p:PublishTrimmed=false')
    if ($variant -eq 'trim') { $options = @('-p:PublishAot=false', '-p:PublishSingleFile=false', '-p:PublishTrimmed=true') }
    if ($variant -in 'single', 'trim-single') {
        $trimmed = ($variant -eq 'trim-single').ToString().ToLowerInvariant()
        $options = @('-p:PublishAot=false', '-p:PublishSingleFile=true', "-p:PublishTrimmed=$trimmed")
        if ($SingleFileExtraction -eq 'All') { $options += '-p:IncludeAllContentForSelfExtract=true', '-p:IncludeNativeLibrariesForSelfExtract=true' }
    }
    if ($variant -eq 'aot') { $options = @('-p:PublishAot=true', '-p:PublishSingleFile=false') }
    if (-not $SkipPublish) {
        & dotnet publish $project -c Release "-p:NazorDeploymentFramework=$Framework" -r win-x64 --self-contained true --artifacts-path "$Output/build" -p:NazorEnablePublishValidation=true -p:NazorEnableDevTools=false -p:NuGetAudit=false -m:1 -nodeReuse:false -o $directory @options *> "$Output/$variant-build.log"
        if ($LASTEXITCODE -ne 0) { throw "Publish failed: $Output/$variant-build.log" }
    }
    if ($variant -in 'trim', 'trim-single') {
        foreach ($assembly in 'Microsoft.Windows.SDK.NET.dll', 'WinRT.Runtime.dll') {
            $original = "$Output/directory/$assembly"
            if (-not (Test-Path $original)) { continue }
            $linked = @(Get-ChildItem "$Output/build/obj/Deployment" -Recurse -Filter $assembly | Where-Object { $_.Directory.Name -eq 'linked' })
            if ($linked.Count -ne 1 -or (Get-FileHash $linked[0].FullName).Hash -ne (Get-FileHash $original).Hash) { throw "JIT WinRT preservation changed: $assembly" }
        }
    }
    if ($variant -in 'single', 'trim-single' -and $SingleFileExtraction -eq 'All') {
        $directory = "$Output/isolated"
        New-Item -ItemType Directory $directory -Force | Out-Null
        if (Get-ChildItem $directory -File | Where-Object Name -NE 'Deployment.exe') { throw 'The isolated directory must contain only the executable.' }
        Copy-Item "$Output/$variant/Deployment.exe" $directory -Force
    }
    $start = [Diagnostics.ProcessStartInfo]::new("$directory/Deployment.exe")
    $start.ArgumentList.Add("$Output/$variant-$Backend.bgra")
    $start.UseShellExecute = $false
    $start.WindowStyle = 'Hidden'
    $start.WorkingDirectory = $directory
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Environment['NAZOR_RENDERER'] = $Backend
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    try {
        if (-not $process.WaitForExit(30000)) { throw 'Native deployment probe timed out.' }
        $log = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        $log | Set-Content "$Output/$variant-$Backend-run.log"
        if ($process.ExitCode -ne 0 -or $log -notmatch 'PASS: native window') { throw "Native probe failed: $log" }
    }
    finally {
        if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
        $process.Dispose()
    }
}
if ((Get-FileHash "$Output/directory-$Backend.bgra").Hash -ne (Get-FileHash "$Output/$Mode-$Backend.bgra").Hash) { throw 'Published pixels differ from directory publication.' }
Write-Host "PASS: Windows $Mode $Backend native behavior and identical pixels. Evidence: $Output"
