#Requires -Version 7
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$logs = Join-Path $root "artifacts/sample-startup/$Configuration"
$discovery = Join-Path $logs 'dev'
New-Item -ItemType Directory -Force $discovery | Out-Null
if (-not $IsWindows) { throw 'Native sample verification requires Windows.' }
if (-not $SkipBuild) {
    & dotnet build "$root/Nazor.slnx" -c $Configuration -m:1 -nodeReuse:false -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }
    & dotnet build "$root/Workspace/Nazor.Workspace/Nazor.Workspace.csproj" -c $Configuration -m:1 -nodeReuse:false -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Workspace build failed.' }
}
$compiler = Join-Path $root "Sources/Tools/Nazor.Compiler/bin/$Configuration/net10.0/Nazor.Compiler.dll"
$projects = @(Get-ChildItem "$root/Samples" -Recurse -Filter '*.csproj') + @(Get-Item "$root/Workspace/Nazor.Workspace/Nazor.Workspace.csproj")
$defaultManifest = (Resolve-Path "$root/Sources/Tools/Nazor.Sdk/Sdk/Nazor.Windows.manifest").Path
foreach ($project in $projects) {
    $manifest = & dotnet msbuild $project.FullName -nologo "-p:Configuration=$Configuration" -getProperty:ApplicationManifest
    if ($LASTEXITCODE -ne 0) { throw "Manifest evaluation failed for $($project.BaseName)." }
    if ((Resolve-Path -LiteralPath $manifest).Path -ne $defaultManifest) { throw "$($project.BaseName) does not use the shared SDK manifest." }
    $targetPath = & dotnet msbuild $project.FullName -nologo "-p:Configuration=$Configuration" -getProperty:TargetPath
    if ($LASTEXITCODE -ne 0) { throw "Target path evaluation failed for $($project.BaseName)." }
    $executable = [IO.Path]::ChangeExtension($targetPath.Trim(), '.exe')
    if (-not (Test-Path $executable)) { throw "Build output missing: $executable" }
    Write-Host "==> Starting $($project.BaseName)" -ForegroundColor Cyan
    Write-Host '    ok: the project uses the shared SDK manifest'
    $start = [Diagnostics.ProcessStartInfo]::new($executable)
    $start.WorkingDirectory = $project.DirectoryName
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = 'Hidden'
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Environment['NAZOR_DEVCHANNEL'] = '1'
    $start.Environment['NAZOR_DEV_DIRECTORY'] = $discovery
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $info = $null
    try {
        $deadline = (Get-Date).AddSeconds(20)
        do {
            if ($process.HasExited) { throw "The application exited during startup (code $($process.ExitCode))." }
            if (Test-Path (Join-Path $discovery "$($process.Id).json")) {
                $previousDiscovery = $env:NAZOR_DEV_DIRECTORY
                try {
                    $env:NAZOR_DEV_DIRECTORY = $discovery
                    $reply = & dotnet $compiler dev $process.Id info 2>&1 | Out-String
                }
                finally { $env:NAZOR_DEV_DIRECTORY = $previousDiscovery }
                if ($LASTEXITCODE -eq 0) {
                    $info = $reply | ConvertFrom-Json
                    if ($info.pid -ne $process.Id) { throw 'The development channel answered for a different process.' }
                    if ($info.windows.Count -gt 0) { break }
                }
            }
            Start-Sleep -Milliseconds 200
        } while ((Get-Date) -lt $deadline)
        if ($null -eq $info -or $info.windows.Count -eq 0) {
            throw "The application did not start its UI loop and expected window within 20 seconds. $reply"
        }
        Write-Host "    ok: native UI loop responds, $($info.windows.Count) window(s)"
    }
    finally {
        if (-not $process.HasExited) {
            if (-not $process.CloseMainWindow() -or -not $process.WaitForExit(5000)) { $process.Kill($true) }
        }
        $process.WaitForExit()
        $output = $stdout.GetAwaiter().GetResult()
        $errors = $stderr.GetAwaiter().GetResult()
        $output | Set-Content (Join-Path $logs "$($project.BaseName).out.log")
        $errors | Set-Content (Join-Path $logs "$($project.BaseName).err.log")
        $process.Dispose()
        if ($null -eq $info) {
            Write-Host $output
            Write-Host $errors
        }
        if ($output -match '(?m)^fail:' -or $errors -match 'Unhandled exception') {
            Write-Host $output
            Write-Host $errors
            throw "Startup verification failed for $($project.BaseName); see $logs."
        }
    }
}
Write-Host "Native startup verified for $($projects.Count) applications." -ForegroundColor Green
