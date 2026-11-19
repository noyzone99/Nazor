#Requires -Version 7
param(
    [Parameter(Mandatory)][string]$ApplicationDirectory,
    [ValidateSet('linux-x64','linux-arm64','linux-musl-x64','linux-musl-arm64')][string]$Rid = 'linux-x64',
    [switch]$NativeBridge,
    [string]$Output = "$PSScriptRoot/../../artifacts/x11-client"
)
$ErrorActionPreference = 'Stop'
$application = (Resolve-Path $ApplicationDirectory).Path
$output = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force $output | Out-Null
$id = 'nazor-x11-' + [guid]::NewGuid().ToString('N')
$volume = "$id-sockets"
$server = ''
$ready = "$output/$id-display"
function Invoke-Docker([string[]]$Arguments) {
    $result = & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw "docker $($Arguments -join ' ') failed." }
    return $result
}
try {
    Invoke-Docker @('volume','create',$volume) | Out-Null
    $server = Invoke-Docker @('run','--detach','--rm','--network','none','--platform','linux/amd64',
        '--mount',"type=volume,source=$volume,target=/tmp/.X11-unix", '--mount',"type=bind,source=$output,target=/out",
        '--entrypoint','/bin/sh','nazor-x11-test:glibc','/x11-test-server.sh','/bin/sh','-c',
        ('printf "%s" "$DISPLAY" > /out/' + $id + '-display; exec sleep 3600'))
    $display = ''
    for ($attempt = 0; $attempt -lt 100; $attempt++) {
        if (Test-Path $ready) {
            $display = Get-Content $ready -Raw
            if (-not [string]::IsNullOrWhiteSpace($display)) { $display = $display.Trim(); break }
        }
        Start-Sleep -Milliseconds 100
    }
    if ([string]::IsNullOrWhiteSpace($display)) { & docker logs $server; throw 'The isolated X11 server did not start.' }
    $arm = $Rid.EndsWith('arm64')
    if ($arm) { Invoke-Docker @('run','--privileged','--rm','tonistiigi/binfmt','--install','arm64') | Out-Null }
    $libc = if ($Rid.StartsWith('linux-musl-')) { 'musl' } else { 'glibc' }
    $image = "nazor-x11-test:$libc" + $(if ($arm) { '-arm64' } else { '' })
    $platform = if ($arm) { 'linux/arm64' } else { 'linux/amd64' }
    $command = if ($NativeBridge) { @('/app/tests/nazor-x11-tests') } else { @('/app/Deployment','/out/frame.bgra') }
    if (-not $NativeBridge -and (Test-Path "$output/frame.bgra")) { Remove-Item -LiteralPath "$output/frame.bgra" }
    Invoke-Docker (@('run','--rm','--network','none','--platform',$platform,
        '--mount',"type=volume,source=$volume,target=/tmp/.X11-unix,readonly", '--mount',"type=bind,source=$application,target=/app,readonly", '--mount',"type=bind,source=$output,target=/out",
        '--env',"DISPLAY=$display",'--env','LD_LIBRARY_PATH=/app','--env','NAZOR_RENDERER=CPU','--entrypoint','/usr/bin/timeout',$image,'40s') + $command)
    if (-not $NativeBridge -and -not (Test-Path "$output/frame.bgra")) { throw 'The native X11 frame capture is missing.' }
    Write-Host "PASS: X11 client / $Rid / native=$NativeBridge"
}
finally {
    if ($server) { & docker stop --time 3 $server | Out-Null }
    & docker volume rm $volume | Out-Null
}
