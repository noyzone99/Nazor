#Requires -Version 7
param([string]$Output = (Join-Path (Resolve-Path "$PSScriptRoot/../..").Path 'artifacts/packages'))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
Remove-Item (Join-Path $Output '*.nupkg') -ErrorAction Ignore
& dotnet pack (Join-Path $root 'Nazor.slnx') -c Release -o $Output -m:1 -nodeReuse:false -p:UseSharedCompilation=false -p:ContinuousIntegrationBuild=true -p:NazorEnableDevTools=false -p:NazorSkipNativeBuild=true -p:EnableSourceControlManagerQueries=false
if ($LASTEXITCODE -ne 0) { throw 'Pack failed.' }
Get-ChildItem $Output -Filter *.nupkg | ForEach-Object { $_.Name }
