#Requires -Version 7
param(
    [ValidateSet('Fluent', 'NazorLeaf')][string]$Theme = 'Fluent',
    [ValidateSet('net8.0', 'net9.0', 'net10.0', 'net11.0')][string[]]$Frameworks = @('net8.0', 'net9.0', 'net10.0'),
    [string]$Feed = (Join-Path $PSScriptRoot '../../artifacts/packages'),
    [string]$Workspace = (Join-Path $PSScriptRoot '../../artifacts/font-symbol-consumers')
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$Feed = [IO.Path]::GetFullPath($Feed)
$version = (Select-Xml -Path "$repositoryRoot/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
$themeVersion = dotnet msbuild "$repositoryRoot/Directory.Build.props" -nologo -getProperty:NazorThemeVersion
if ($LASTEXITCODE -ne 0) { throw 'Could not read the theme package version.' }
$consumerRoot = Join-Path ([IO.Path]::GetFullPath($Workspace)) ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $consumerRoot -Force | Out-Null

$settings = if ($Theme -eq 'Fluent') {
    @{ '__REGISTER__' = 'AddFluentFonts'; '__PRIMARY__' = 'FluentSymbol.Add20Regular'; '__SECONDARY__' = 'FluentSymbol.Star20Filled'; '__THIRD__' = 'FluentSymbol.AlbumSubtract20Regular'; '__FAMILY__' = 'FluentSystemIcons-Regular'; '__PREFIX__' = 'FluentSystemIcons-'; '__GLYPH__' = 'F109' }
} else {
    @{ '__REGISTER__' = 'AddNazorLeafFonts'; '__PRIMARY__' = 'LeafSymbol.Plus'; '__SECONDARY__' = 'LeafSymbol.Leaf'; '__THIRD__' = 'LeafSymbol.Search'; '__FAMILY__' = 'NazorLeaf Icons'; '__PREFIX__' = 'NazorLeaf Icons'; '__GLYPH__' = 'E01F' }
}
$settings['__THEME__'] = $Theme
$themeRoot = Join-Path $repositoryRoot "Sources/Themes/Nazor.Themes.$Theme"
$themeAssets = @(Get-ChildItem -LiteralPath "$themeRoot/Fonts" -File | Sort-Object Name)
$settings['__ASSETS__'] = (($themeAssets | ForEach-Object { '"' + $_.Name + '"' }) -join ', ')

function Expand-Source([string]$Source) {
    foreach ($entry in $settings.GetEnumerator()) { $Source = $Source.Replace($entry.Key, $entry.Value) }
    return $Source
}

function Invoke-Dotnet([string[]]$Arguments, [string]$Log) {
    Push-Location $consumer
    try { & dotnet @Arguments *> $Log }
    finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw [IO.File]::ReadAllText($Log) }
}

foreach ($framework in $Frameworks) {
    $consumer = Join-Path $consumerRoot "$Theme-$framework"
    New-Item -ItemType Directory -Path $consumer -Force | Out-Null
    if ($framework -eq 'net11.0') {
        '{ "sdk": { "version": "11.0.100", "rollForward": "latestPatch", "allowPrerelease": false } }' | Set-Content "$consumer/global.json"
    }
    foreach ($file in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props')) {
        '<Project />' | Set-Content -LiteralPath (Join-Path $consumer $file)
    }
    @"
<configuration>
  <config><add key="globalPackagesFolder" value="$consumer/.packages" /></config>
  <packageSources><clear /><add key="nazor" value="$Feed" /><add key="cache" value="$([IO.Path]::Combine([Environment]::GetFolderPath('UserProfile'), '.nuget/packages'))" /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nazor"><package pattern="Nazor.*" /></packageSource><packageSource key="cache"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath "$consumer/nuget.config"
    @"
<Project Sdk="Nazor.Sdk/$version">
  <PropertyGroup>
    <TargetFramework>$framework</TargetFramework>
    <OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>
    <RootNamespace>FontConsumer</RootNamespace>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup><PackageReference Include="Nazor.Themes.$Theme" Version="$themeVersion" /></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath "$consumer/FontConsumer.csproj"
    @'
@using Nazor.Themes.__THEME__
<Icon Symbol="@__PRIMARY__" />
<Icon Symbol="@__SECONDARY__" />
<Icon Symbol="@__THIRD__" />
<Icon Symbol="@(new CompanySymbol())" />
<Icon Glyph="&#x__GLYPH__;" Style="font-family: '__FAMILY__'" />
'@ | ForEach-Object { Expand-Source $_ } | Set-Content -LiteralPath "$consumer/IconProbe.nazor"
    @'
using Nazor;

namespace FontConsumer;

public readonly struct CompanySymbol : IFontSymbol
{
    public string FontFamily => "__FAMILY__";
    public string Glyph => "\u__GLYPH__";
}
'@ | ForEach-Object { Expand-Source $_ } | Set-Content -LiteralPath "$consumer/CompanySymbol.cs"
    @'
using Nazor;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Themes.__THEME__;
using FontConsumer;

var options = new FontOptions { UseSystemFonts = false };
options.__REGISTER__();
using var graphics = new SkiaRenderer(options);
using var renderer = new Renderer(new UIDispatcher());
renderer.AddRootComponent<IconProbe>();
using var scene = new Scene(renderer, graphics.Fonts, graphics.Shaper, images: graphics.Images);
scene.Styles.Theme = Nazor.Themes.Themes.Basic;
scene.Viewport = new Size(200, 200);
scene.Update();
var commands = scene.BuildDisplayList().Commands.OfType<DrawText>().ToArray();
if (commands.Length != 5 || commands.SelectMany(command => command.Layout.Shaped.Runs)
    .Any(run => !run.Face.FamilyName.StartsWith("__PREFIX__", StringComparison.Ordinal) || run.Glyphs.Any(glyph => glyph == 0)))
    throw new InvalidOperationException("Packaged symbols must shape with their private font.");
foreach (var file in new[] { __ASSETS__ })
    if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "Themes", "__THEME__", file)))
        throw new InvalidOperationException("Missing theme asset: " + file);
Console.WriteLine("Packaged __THEME__ symbols shaped without system fonts.");
'@ | ForEach-Object { Expand-Source $_ } | Set-Content -LiteralPath "$consumer/Program.cs"
    Invoke-Dotnet @('build', $consumer, '-c', 'Release', '-m:1', '-nodeReuse:false', '-p:UseSharedCompilation=false') "$consumer/build.log"
    Invoke-Dotnet @('msbuild', "$consumer/FontConsumer.csproj", '-getItem:None,Content', '-p:DesignTimeBuild=true') "$consumer/project-items.json"
    $projectItems = (Get-Content -LiteralPath "$consumer/project-items.json" -Raw | ConvertFrom-Json).Items
    if ((@($projectItems.None) + @($projectItems.Content)) | Where-Object { $_.FullPath -like "*nazor.themes.$Theme*" }) {
        throw "Packaged $Theme assets appear as design-time project items."
    }
    Invoke-Dotnet @("$consumer/bin/Release/$framework/FontConsumer.dll") "$consumer/run.log"
    $published = Join-Path $consumer 'published'
    Invoke-Dotnet @('publish', $consumer, '-c', 'Release', '--no-restore', '--self-contained', 'false', '-o', $published, '-m:1', '-nodeReuse:false', '-p:UseSharedCompilation=false') "$consumer/publish.log"
    Invoke-Dotnet @("$published/FontConsumer.dll") "$consumer/published-run.log"
    foreach ($file in $themeAssets) {
        $expected = (Get-FileHash -LiteralPath $file.FullName).Hash
        foreach ($directory in @("$consumer/bin/Release/$framework", $published)) {
            if ((Get-FileHash -LiteralPath "$directory/Themes/$Theme/$($file.Name)").Hash -ne $expected) {
                throw "Theme asset differs from source: $directory/Themes/$Theme/$($file.Name)"
            }
        }
    }
    Write-Output "$Theme ${framework}: project items, private-font shaping and build/publish asset hashes verified."
}
Write-Output "Font symbol package consumers: $consumerRoot"
