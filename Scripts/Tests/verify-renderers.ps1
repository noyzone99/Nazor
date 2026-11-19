#Requires -Version 7
param([ValidateRange(1,10000)][int]$Samples=30,[switch]$SkipBuild)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$previousRenderer=$env:NAZOR_RENDERER
Push-Location $root
try {
    if (-not $SkipBuild) {
        & dotnet build Tests/Nazor.Benchmarks -c Release -m:1 -nodeReuse:false -p:UseSharedCompilation=false
        if ($LASTEXITCODE -ne 0) { throw 'Renderer benchmark build failed.' }
    }
    foreach ($mode in @('CPU','GPU')) {
        $env:NAZOR_RENDERER=$mode
        & dotnet run --project Tests/Nazor.Benchmarks -c Release --no-build -- --native --samples $Samples --out "artifacts/renderers/$mode.json"
        if ($LASTEXITCODE -ne 0) { throw "Native $mode verification failed. GPU mode requires a hardware context." }
    }
}
finally {
    $env:NAZOR_RENDERER=$previousRenderer
    Pop-Location
}
