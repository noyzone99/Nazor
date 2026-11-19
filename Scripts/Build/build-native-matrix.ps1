#Requires -Version 7
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [ValidateSet('linux-x64', 'linux-arm64', 'linux-musl-x64', 'linux-musl-arm64')]
    [string[]]$Rids = @('linux-x64', 'linux-arm64', 'linux-musl-x64', 'linux-musl-arm64')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
foreach ($rid in $Rids) {
    $platform = if ($rid.EndsWith('arm64')) { 'linux/arm64' } else { 'linux/amd64' }
    $libc = if ($rid.StartsWith('linux-musl-')) { 'musl' } else { 'glibc' }
    $build = "$root/artifacts/native-build/$rid/$Configuration"
    New-Item -ItemType Directory -Force $build | Out-Null
    & docker buildx build --platform $platform --build-arg "LIBC=$libc" --build-arg "CONFIGURATION=$Configuration" `
        --file "$PSScriptRoot/Native.Dockerfile" --target artifact --metadata-file "$build/build-metadata.json" `
        --output "type=local,dest=$root/artifacts/native" "$root/Sources/Platforms/Nazor.Platform.Linux/Native"
    if ($LASTEXITCODE -ne 0) { throw "Native build failed: $rid" }
    if (-not (Test-Path "$root/artifacts/native/$rid/libnazor-wayland.so")) { throw "Missing native artifact: $rid" }
    if (-not (Test-Path "$root/artifacts/native/$rid/libnazor-x11.so")) { throw "Missing X11 native artifact: $rid" }
    Write-Host "Built ${rid}: $root/artifacts/native/$rid/libnazor-wayland.so"
}
