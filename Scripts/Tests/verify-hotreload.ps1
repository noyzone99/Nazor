#Requires -Version 7
param(
    [string]$Workspace = (Join-Path ([IO.Path]::GetTempPath()) 'nazor-hotreload-e2e'),
    [switch]$SkipPack
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$Workspace = [IO.Path]::GetFullPath($Workspace)
$feed = Join-Path $root 'artifacts/packages'
$version = (Select-Xml -Path "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
$started = Get-Date

function Step([string]$text) { Write-Host "==> $text" -ForegroundColor Cyan }

function Invoke-Dotnet {
    param([string[]]$Arguments, [switch]$AllowFailure)
    $output = & dotnet @Arguments 2>&1 | Out-String
    if (-not $AllowFailure -and $LASTEXITCODE -ne 0) {
        Write-Host $output
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw "Check failed: $message" }
    Write-Host "    ok: $message"
}

function Read-Log([string]$path) {
    if (-not (Test-Path $path)) { return '' }
    $stream = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    try {
        $reader = New-Object IO.StreamReader($stream)
        return $reader.ReadToEnd()
    } finally { $stream.Dispose() }
}

function Wait-Until([scriptblock]$condition, [int]$timeoutSeconds, [int]$pollMilliseconds = 250) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ($true) {
        if (& $condition) { return $true }
        if ((Get-Date) -ge $deadline) { return $false }
        Start-Sleep -Milliseconds $pollMilliseconds
    }
}

function Stop-Tree($process, [string]$name) {
    if ($null -eq $process) { return }
    try {
        if (-not $process.HasExited) { & taskkill /PID $process.Id /T /F 2>&1 | Out-Null }
    } catch { Write-Host "    could not stop $name (pid $($process.Id)): $_" -ForegroundColor Yellow }
}

function Show-Tail([string]$title, [string]$path, [int]$lines = 40) {
    Write-Host "--- $title ($path) ---" -ForegroundColor Yellow
    $text = Read-Log $path
    if ($text) { ($text -split "`r?`n" | Select-Object -Last $lines) -join "`n" | Write-Host }
}

if (-not $SkipPack) {
    Step "Packing Nazor $version"
    Remove-Item "$feed/*.nupkg" -ErrorAction Ignore
    Invoke-Dotnet @('pack', "$root/Nazor.slnx", '-c', 'Release', '-o', $feed, '-m:1', '-nodeReuse:false',
        '-p:UseSharedCompilation=false', '-p:ContinuousIntegrationBuild=true', '-v', 'q') | Out-Null
}
Assert (Test-Path "$feed/Nazor.Sdk.$version.nupkg") "Nazor.Sdk $version is in artifacts/packages"
Assert (Test-Path "$feed/Nazor.Templates.$version.nupkg") "Nazor.Templates $version is in artifacts/packages"

Step "Workspace $Workspace"
if (Test-Path $Workspace) { Remove-Item $Workspace -Recurse -Force }
$cache = Join-Path $Workspace '.packages'
$hive = Join-Path $Workspace '.templates'
$devDirectory = Join-Path $Workspace 'dev'
New-Item -ItemType Directory -Force $Workspace | Out-Null
'<Project />' | Set-Content "$Workspace/Directory.Build.props"
'<Project />' | Set-Content "$Workspace/Directory.Build.targets"
'<Project />' | Set-Content "$Workspace/Directory.Packages.props"
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="$cache" />
  </config>
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
    <add key="user" value="$(Join-Path $env:USERPROFILE '.nuget/packages')" />
  </fallbackPackageFolders>
</configuration>
"@ | Set-Content "$Workspace/nuget.config"

Step 'Template: dotnet new nazor-app'
Invoke-Dotnet @('new', 'install', "$feed/Nazor.Templates.$version.nupkg", '--debug:custom-hive', $hive) | Out-Null
$app = Join-Path $Workspace 'HelloNazor'
$csproj = Join-Path $app 'HelloNazor.csproj'
Invoke-Dotnet @('new', 'nazor-app', '-n', 'HelloNazor', '-o', $app, '--debug:custom-hive', $hive) | Out-Null
Assert (Test-Path "$app/Properties/launchSettings.json") 'the project has the launch profile with NAZOR_DEVCHANNEL=1'
$program = Get-Content "$app/Program.cs" -Raw
$patched = $program.Replace('builder.Application.UseMainWindow<App>();', 'builder.Application.UseMainWindow<App>(new() { Activate = false });')
Assert ($patched -ne $program) 'Program.cs of the template creates the builder (patched with Activate = false)'
$patched | Set-Content "$app/Program.cs" -NoNewline

$savedEnvironment = @{}
foreach ($name in 'NAZOR_DEV_DIRECTORY', 'DOTNET_WATCH_SUPPRESS_EMOJIS', 'DOTNET_CLI_TELEMETRY_OPTOUT', 'DOTNET_CLI_UI_LANGUAGE', 'MSBUILDNODEREUSE') {
    $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
}
$env:NAZOR_DEV_DIRECTORY = $devDirectory
$env:DOTNET_WATCH_SUPPRESS_EMOJIS = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$env:MSBUILDNODEREUSE = '0'

function Invoke-Nazorc([string[]]$Arguments) { return Invoke-Dotnet (@($nazorc) + $Arguments) -AllowFailure }

function Get-Info {
    $result = Invoke-Nazorc @('dev', $app, 'info')
    if ($result.ExitCode -ne 0) { return $null }
    try { return $result.Output | ConvertFrom-Json } catch { return $null }
}

function Wait-Dump([string[]]$Arguments, [string]$text, [int]$timeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ($true) {
        $result = Invoke-Nazorc (@('dev', $app, 'dump') + $Arguments)
        if ($result.ExitCode -eq 0 -and $result.Output.Contains($text)) { return $true }
        if ((Get-Date) -ge $deadline) { Write-Host $result.Output; return $false }
        Start-Sleep -Milliseconds 250
    }
}

$watchOut = Join-Path $Workspace 'dotnet-watch.out.log'
$watchErr = Join-Path $Workspace 'dotnet-watch.err.log'
$nazorcOut = Join-Path $Workspace 'nazorc-watch.out.log'
$nazorcErr = Join-Path $Workspace 'nazorc-watch.err.log'
$watch = $null
$watcher = $null
$failed = $true
try {
    Step 'Debug build'
    Invoke-Dotnet @('build', $csproj, '-c', 'Debug', '-nodeReuse:false') | Out-Null
    Assert (Test-Path "$app/bin/Debug/net10.0/HelloNazor.dll") 'the application builds in Debug'
    $nazorc = Join-Path $cache "nazor.sdk/$version/tools/net10.0/Nazor.Compiler.dll"
    if (-not (Test-Path $nazorc)) { $nazorc = (Get-ChildItem $cache -Recurse -Filter 'Nazor.Compiler.dll' | Select-Object -First 1).FullName }
    Assert ([bool]$nazorc -and (Test-Path $nazorc)) "nazorc of the packed SDK is in the package cache ($nazorc)"

    Step 'dotnet watch run (hot reload mode, launch profile with NAZOR_DEVCHANNEL=1)'
    $watch = Start-Process -FilePath 'dotnet' -ArgumentList @('watch', 'run', '--non-interactive', '--project', "`"$csproj`"") `
        -WorkingDirectory $app -RedirectStandardOutput $watchOut -RedirectStandardError $watchErr -PassThru -NoNewWindow
    function Read-Watch { return (Read-Log $watchOut) + "`n" + (Read-Log $watchErr) }
    $ready = Wait-Until { ((Read-Watch) -match 'Waiting for a file to change|Waiting for changes') -or ((Test-Path $devDirectory) -and (Get-ChildItem $devDirectory -Filter '*.json' -ErrorAction Ignore)) } 180
    Assert $ready 'dotnet watch built and started the application'
    $published = Wait-Until { (Test-Path $devDirectory) -and (Get-ChildItem $devDirectory -Filter '*.json' -ErrorAction Ignore) } 60 500
    Assert $published 'the application published its development channel descriptor (NAZOR_DEVCHANNEL from launchSettings.json)'
    $info = $null
    Assert (Wait-Until { ($script:info = Get-Info) -ne $null } 30 500) 'nazorc dev info answers'
    $pid0 = [int]$info.pid
    Assert ($pid0 -gt 0 -and $info.hotReloadSupported -eq $true) "the application (pid $pid0) runs with .NET Hot Reload support"
    Assert (-not $watch.HasExited) 'dotnet watch is still running'

    Step 'dotnet build -t:NazorWatch -tl:off (nazorc watch)'
    $watcher = Start-Process -FilePath 'dotnet' -ArgumentList @('build', "`"$csproj`"", '-t:NazorWatch', '-tl:off', '-c', 'Debug', '-nodeReuse:false') `
        -WorkingDirectory $app -RedirectStandardOutput $nazorcOut -RedirectStandardError $nazorcErr -PassThru -NoNewWindow
    Assert (Wait-Until { (Read-Log $nazorcOut) -match 'nazorc watch: watching' } 120 500) 'nazorc watch started through the NazorWatch target'
    Assert (Test-Path "$app/obj/Debug/net10.0/Nazor/nazorc.watch.rsp") 'the target wrote nazorc.watch.rsp next to nazorc.rsp'
    Assert (Wait-Until { (Read-Log $nazorcOut) -match "connected to application $pid0" } 30 250) "nazorc watch connected to application $pid0"
    Assert (-not $watcher.HasExited) 'the watcher is still running'

    function Read-After([string]$path, [int]$mark) { $text = Read-Log $path; if ($text.Length -le $mark) { return '' } else { return $text.Substring($mark) } }
    function Get-WatchMarks { return @{ Out = (Read-Log $watchOut).Length; Err = (Read-Log $watchErr).Length } }
    function Read-WatchAfter($marks) { return (Read-After $watchOut $marks.Out) + "`n" + (Read-After $watchErr $marks.Err) }

    Step 'Styles live: Theme.nss'
    $theme = Get-Content "$app/Theme.nss" -Raw
    Assert ($theme.Contains('background: Canvas;')) 'Theme.nss has the window background of the template'
    $nazorcMark = (Read-Log $nazorcOut).Length
    $edited = Get-Date
    $theme.Replace('background: Canvas;', 'background: #1e3a5f;') | Set-Content "$app/Theme.nss" -NoNewline
    Assert (Wait-Dump @('--styles') 'background-color: #1e3a5f' 10) 'the running window shows the new background within 10 s'
    $stylesLatency = ((Get-Date) - $edited).TotalMilliseconds
    $applied = Wait-Until { (Read-After $nazorcOut $nazorcMark) -match 'styles of Theme\.nss applied to 1 application' } 5
    Assert $applied 'nazorc watch reported the styles applied to 1 application (after the edit)'
    $reported = [regex]::Match((Read-After $nazorcOut $nazorcMark), 'styles of Theme\.nss applied to 1 application\(s\) \((\d+) ms\)')
    $info = Get-Info
    Assert ($null -ne $info -and [int]$info.pid -eq $pid0) "the application was not restarted (pid $pid0)"
    Write-Host ("    styles visible after {0:N0} ms (nazorc watch measured {1} ms)" -f $stylesLatency, $(if ($reported.Success) { $reported.Groups[1].Value } else { '?' }))

    Step 'Markup through .NET Hot Reload: App.nazor'
    $markup = Get-Content "$app/App.nazor" -Raw
    $anchor = '<TextBlock Class="title">@Greeter.Greet(Name)</TextBlock>'
    Assert ($markup.Contains($anchor)) 'App.nazor has the title of the template'
    Assert (Wait-Until { (Read-Watch) -match 'changes applied in \d+ms' } 20) 'dotnet watch applied the rewritten NazorStyleSheets.g.cs as a delta (the standalone sheet was already live)'
    Assert (Wait-Until { ($script:info = Get-Info) -ne $null -and [int]$script:info.windows[0].hotReloadCount -ge 1 } 10 500) 'the window re-rendered on that delta'
    $count0 = [int]$info.windows[0].hotReloadCount
    $nazorcMark = (Read-Log $nazorcOut).Length
    $watchMarks = Get-WatchMarks
    $edited = Get-Date
    $markup.Replace($anchor, "$anchor`r`n        <TextBlock>Hot reloaded</TextBlock>") | Set-Content "$app/App.nazor" -NoNewline
    Assert (Wait-Dump @() 'Hot reloaded' 30) 'the running window shows the new TextBlock within 30 s'
    $markupLatency = ((Get-Date) - $edited).TotalMilliseconds
    Assert ((Read-After $nazorcOut $nazorcMark) -match 'App\.nazor\.g\.cs regenerated: dotnet watch or Visual Studio will apply the code change') 'nazorc watch regenerated App.nazor.g.cs for dotnet watch'
    $info = Get-Info
    Assert ($null -ne $info -and [int]$info.pid -eq $pid0) "the application was not restarted (pid $pid0)"
    Assert ([int]$info.windows[0].hotReloadCount -gt $count0) "the window re-rendered on the delta (hotReloadCount $count0 -> $($info.windows[0].hotReloadCount))"
    Assert ((Read-After $nazorcOut $nazorcMark) -match 'hot reload applied') 'the application reported the delta to nazorc watch (app/log)'
    Assert (Wait-Until { (Read-WatchAfter $watchMarks) -match 'changes applied in \d+ms' } 10) 'dotnet watch applied the rewritten App.nazor.g.cs as a code change'
    Write-Host ("    markup visible after {0:N0} ms" -f $markupLatency)

    Step 'Nothing restarted'
    Assert (-not $watch.HasExited) 'dotnet watch is still running'
    Assert (-not $watcher.HasExited) 'nazorc watch is still running'
    Assert ((Read-Watch) -cnotmatch 'Restarting|rude edit|Unable to apply') 'dotnet watch never restarted the application'
    $failed = $false
    Write-Host ("Hot reload verification passed in {0:N0} s (styles {1:N0} ms, markup {2:N0} ms)." -f ((Get-Date) - $started).TotalSeconds, $stylesLatency, $markupLatency) -ForegroundColor Green
}
finally {
    Stop-Tree $watcher 'nazorc watch'
    Stop-Tree $watch 'dotnet watch'
    & dotnet build-server shutdown 2>&1 | Out-Null
    foreach ($name in $savedEnvironment.Keys) {
        $value = if ($null -eq $savedEnvironment[$name]) { [NullString]::Value } else { $savedEnvironment[$name] }
        [Environment]::SetEnvironmentVariable($name, $value)
    }
    if ($failed) {
        Show-Tail 'dotnet watch stdout' $watchOut
        Show-Tail 'dotnet watch stderr' $watchErr
        Show-Tail 'nazorc watch stdout' $nazorcOut
        Show-Tail 'nazorc watch stderr' $nazorcErr
    }
}
