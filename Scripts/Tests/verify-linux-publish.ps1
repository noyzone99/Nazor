param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$bridge = Join-Path $repositoryRoot 'artifacts/native/linux-x64/libnazor-wayland.so'
if (-not (Test-Path -LiteralPath $bridge)) {
    throw 'Build the Wayland bridge in Linux/WSL with Scripts/Build/build-native.sh before verifying Linux publication.'
}
$publishDirectory = Join-Path $repositoryRoot ('artifacts/linux-publish/' + [guid]::NewGuid().ToString('N'))
$project = Join-Path $repositoryRoot 'Workspace/Nazor.Workspace/Nazor.Workspace.csproj'
& dotnet publish $project -c $Configuration -r linux-x64 --self-contained true -o $publishDirectory -m:1 -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw "Linux publication failed with exit code $LASTEXITCODE." }

foreach ($name in @('Nazor.Workspace', 'libnazor-wayland.so', 'libSkiaSharp.so', 'libHarfBuzzSharp.so', 'libhostpolicy.so', 'libcoreclr.so')) {
    $path = Join-Path $publishDirectory $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing Linux publication asset: $name" }
    $stream = [System.IO.File]::OpenRead($path)
    try {
        $signature = [byte[]]::new(4)
        if ($stream.Read($signature, 0, 4) -ne 4 -or [BitConverter]::ToString($signature) -ne '7F-45-4C-46') {
            throw "The publication asset is not an ELF binary: $name"
        }
    }
    finally { $stream.Dispose() }
}
$dependencies = Get-Content -LiteralPath (Join-Path $publishDirectory 'Nazor.Workspace.deps.json') -Raw | ConvertFrom-Json
if (-not $dependencies.runtimeTarget.name.EndsWith('/linux-x64', [StringComparison]::Ordinal)) {
    throw 'The published dependency manifest does not target linux-x64.'
}
foreach ($name in @('WinRT.Runtime.dll', 'Microsoft.Windows.SDK.NET.dll', 'Microsoft.InteractiveExperiences.Projection.dll', 'Nazor.Hosting.Windows.dll', 'Nazor.Platform.Windows.dll')) {
    if (Test-Path -LiteralPath (Join-Path $publishDirectory $name)) { throw "Unexpected Windows dependency in Linux publication: $name" }
}
if ($dependencies.libraries.PSObject.Properties.Name -match '^(Nazor\.(Hosting|Platform)\.Windows|WinRT\.Runtime)/') {
    throw 'The Linux dependency manifest still includes Windows dependencies.'
}
Write-Host "Linux x64 self-contained publication verified: $publishDirectory"
