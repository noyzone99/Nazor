#Requires -Version 7
param(
    [string]$Output = (Join-Path (Resolve-Path "$PSScriptRoot/../..").Path 'artifacts/packages'),
    [switch]$Symbols
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
Remove-Item (Join-Path $Output '*.nupkg') -ErrorAction Ignore
Remove-Item (Join-Path $Output '*.snupkg') -ErrorAction Ignore
$symbolOptions = @()
if ($Symbols) {
    $symbolOptions = @('-p:IncludeSymbols=true', '-p:SymbolPackageFormat=snupkg')
}
& dotnet pack (Join-Path $root 'Nazor.slnx') -c Release -o $Output -m:1 -nodeReuse:false -p:UseSharedCompilation=false -p:ContinuousIntegrationBuild=true -p:NazorEnableDevTools=false -p:NazorSkipNativeBuild=true -p:EnableSourceControlManagerQueries=false @symbolOptions
if ($LASTEXITCODE -ne 0) { throw 'Pack failed.' }
Get-ChildItem $Output -File | Where-Object Extension -in '.nupkg', '.snupkg' | ForEach-Object { $_.Name }
