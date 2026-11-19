#Requires -Version 7
param(
    [ValidateSet('net8.0', 'net9.0', 'net10.0', 'net11.0')][string]$Framework = 'net10.0',
    [string]$Feed = (Join-Path $PSScriptRoot '../../artifacts/packages'),
    [string]$Workspace = (Join-Path $PSScriptRoot '../../artifacts/controls-consumers'),
    [ValidateSet('trim', 'aot')][string[]]$DeploymentModes = @()
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$Feed = [IO.Path]::GetFullPath($Feed)
$version = (Select-Xml -Path "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
$consumer = Join-Path ([IO.Path]::GetFullPath($Workspace)) "$Framework-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $consumer -Force | Out-Null

function Invoke-Dotnet([string[]]$Arguments, [switch]$AllowFailure) {
    Push-Location $consumer
    try { $output = & dotnet @Arguments 2>&1 | Out-String }
    finally { Pop-Location }
    $result = [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    if (-not $AllowFailure -and $result.ExitCode -ne 0) { throw $output }
    return $result
}

function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Check failed: $Message" }
    Write-Host "ok: $Message"
}

function Assert-PackedAssembly([string]$Package) {
    $archive = [IO.Compression.ZipFile]::OpenRead("$Feed/$Package.$version.nupkg")
    try {
        $selected = $assets.targets[$Framework]["$Package/$version"].compile.Keys | Where-Object { $_ -like "lib/*/$Package.dll" } | Select-Object -First 1
        $entry = $archive.GetEntry($selected)
        if (-not $entry) { throw "Missing packed assembly: $Package $selected" }
        $stream = $entry.Open()
        try { $expected = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) }
        finally { $stream.Dispose() }
        $actual = (Get-FileHash -LiteralPath "$consumer/bin/Release/$Framework/$Package.dll" -Algorithm SHA256).Hash
        Assert ($actual -eq $expected) "$Package consumer assembly matches the freshly packed binary"
    }
    finally { $archive.Dispose() }
}

foreach ($package in 'Nazor.Sdk', 'Nazor.Controls') {
    Assert (Test-Path "$Feed/$package.$version.nupkg") "$package $version is packed"
}
'<Project />' | Set-Content "$consumer/Directory.Build.props"
'<Project />' | Set-Content "$consumer/Directory.Build.targets"
'<Project />' | Set-Content "$consumer/Directory.Packages.props"
if ($Framework -eq 'net11.0') {
    '{ "sdk": { "version": "11.0.100", "rollForward": "latestPatch", "allowPrerelease": false } }' | Set-Content "$consumer/global.json"
}
@"
<configuration>
  <config><add key="globalPackagesFolder" value="$consumer/.packages" /></config>
  <packageSources><clear /><add key="nazor" value="$Feed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /><add key="cache" value="$([IO.Path]::Combine([Environment]::GetFolderPath('UserProfile'), '.nuget/packages'))" /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nazor"><package pattern="Nazor.*" /></packageSource><packageSource key="nuget.org"><package pattern="*" /></packageSource><packageSource key="cache"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
"@ | Set-Content "$consumer/nuget.config"
@"
<Project Sdk="Nazor.Sdk/$version">
  <PropertyGroup>
    <TargetFramework>$Framework</TargetFramework>
    <OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>
    <RootNamespace>ControlsConsumer</RootNamespace>
  </PropertyGroup>
</Project>
"@ | Set-Content "$consumer/ControlsConsumer.csproj"
@'
@using Nazor.Controls.Primitives

<Field LabelText="Account"><TextField Value="Example" /></Field>
<NumberField Value="12.5" Format="N2" />
<DatePicker />
<Select Items="values" Value="@(values[0])" />
<ProgressBar Value="25" />
<Badge Icon="@(badgeIcon)" AccessibleName="Attention"><Button>Messages</Button></Badge>
<Badge AccessibleName="Synchronization needs attention">
    <IndicatorContent><TextBlock>!</TextBlock></IndicatorContent>
    <ChildContent><Button>Synchronization</Button></ChildContent>
</Badge>
<Tabs><Tab Label="Preview"><TextBlock>Controls</TextBlock></Tab></Tabs>
<ColorPicker Color="@(color)" IsAlphaEnabled="@(true)" />
<NavigationView TItem="string" Items="@(values)" SelectedItem="@(values[0])" ItemKey="@((string item) => item)" PaneDisplayMode="@(NavigationViewPaneDisplayMode.Top)">
    <TextBlock>Navigation content</TextBlock>
</NavigationView>
<BreadcrumbBar TItem="string" Items="@(values)" ItemKey="@((string item) => item)" />
<PipsPager Count="@(values.Length)" SelectedIndex="@(1)" />
<FlipView TItem="string" Items="@(values)" SelectedIndex="@(1)">
    <ItemTemplate Context="entry">
        <Container><TextBlock>@(entry)</TextBlock><Button>Custom content</Button></Container>
    </ItemTemplate>
</FlipView>
<SegmentedDatePicker Value="@(date)" />
<SegmentedTimePicker Value="@(time)" Use24Hour="@(true)" MinuteIncrement="@(15)" />
<Select TValue="string" Items="@(values)" Value="@(values[0])" DropDownPlacement="@(DropDownPlacement.Center)" />
<Nazor.Controls.Primitives.Combobox TItem="string" Items="@(values)" IsItemSelected="@((string entry) => entry == values[0])" DropDownPlacement="@(DropDownPlacement.Center)" />
<NumberPresenter Value="@(0.42)">
    <ChildContent Context="sample"><TextBlock>@(sample.Value)</TextBlock></ChildContent>
</NumberPresenter>
@code {
    private readonly string[] values = ["one", "two"];
    private readonly Nazor.Style.Color color = new(51, 102, 153, 160);
    private readonly DateOnly date = new(2026, 11, 13);
    private readonly TimeOnly time = new(9, 30);
    private readonly Nazor.IFontSymbol badgeIcon = new Nazor.FontSymbol("sans-serif", "!");
}
'@ | Set-Content "$consumer/ControlsProbe.nazor"
if ($Framework -eq 'net11.0') {
    @'
<TextBlock>@Count</TextBlock>
@code {
#if NET11_0
    private readonly List<int> values = [with(capacity: 8), 1, 2, 3];
    public int Count => values.Count;
#else
    public int Count => -1;
#endif
}
'@ | Set-Content "$consumer/ModernProbe.nazor"
    Add-Content "$consumer/ControlsProbe.nazor" '<ModernProbe />'
}
@'
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Rendering;
using ControlsConsumer;

using var renderer = new Renderer(new UIDispatcher());
renderer.AddRootComponent<ControlsProbe>();
if (typeof(TextField).Assembly != typeof(DatePicker).Assembly)
    throw new InvalidOperationException("Controls must share one assembly.");
foreach (var type in new[] { typeof(ColorPicker), typeof(NavigationView<string>), typeof(BreadcrumbBar<string>), typeof(PipsPager),
    typeof(FlipView<string>), typeof(SegmentedDatePicker), typeof(SegmentedTimePicker), typeof(NumberPresenter), typeof(Badge) })
    if (type.Assembly != typeof(TextField).Assembly)
        throw new InvalidOperationException($"{type.FullName} must share the unified controls assembly.");
Console.WriteLine("Controls rendered.");
'@ | Set-Content "$consumer/Program.cs"
if ($Framework -eq 'net11.0') {
    Add-Content "$consumer/Program.cs" 'if (new ModernProbe().Count != 3) throw new InvalidOperationException("C# 15 and target symbols must reach component generation.");'
}
@'
TextBlock { color: #123456; }
.engine-conic { background: conic-gradient(from 90deg at center, red 0 25%, blue 25% 100%); }
.engine-typography { font-feature-settings:"kern" off,"liga" on,"ss01" 2; font-synthesis:weight style; }
.engine-blend { mix-blend-mode:soft-light; isolation:isolate; background-blend-mode:screen,multiply; }
.engine-filters { filter: blur(.1em) contrast(.8) invert(.2); backdrop-filter: brightness(.8) brightness(1.2); }
.engine-stripes { background: repeating-linear-gradient(to right, red 0 4px, blue 4px 8px); }
.engine-rings { background: repeating-radial-gradient(circle, red 5px 10px, blue 10px 15px); }
.engine-sectors { border-image-source: repeating-conic-gradient(red 0 45deg, blue 45deg 90deg); }
'@ | Set-Content "$consumer/Probe.nss"
Invoke-Dotnet @('build', $consumer, '-c', 'Release', '-nodeReuse:false') | Out-Null
if ($Framework -eq 'net11.0') {
    $olderLanguage = Invoke-Dotnet @('build', $consumer, '-c', 'Release', '-nodeReuse:false', '-p:LangVersion=14.0') -AllowFailure
    Assert ($olderLanguage.ExitCode -ne 0 -and $olderLanguage.Output -match '15\.0') 'an explicit C# 14 language version rejects C# 15 component syntax'
    Invoke-Dotnet @('build', $consumer, '-c', 'Release', '-nodeReuse:false', '-p:LangVersion=15.0') | Out-Null
}
$compilerPath = (Invoke-Dotnet @('msbuild', "$consumer/ControlsConsumer.csproj", '-nologo', '-getProperty:NazorCompilerPath')).Output.Trim()
$compilerRuntime = Get-Content (Join-Path (Split-Path $compilerPath) 'Nazor.Compiler.runtimeconfig.json') -Raw | ConvertFrom-Json
Assert ($compilerRuntime.runtimeOptions.tfm -eq 'net10.0') "the $Framework consumer uses the .NET 10 compiler"
$appRuntime = Get-Content "$consumer/bin/Release/$Framework/ControlsConsumer.runtimeconfig.json" -Raw | ConvertFrom-Json
Assert ($appRuntime.runtimeOptions.tfm -eq $Framework) 'the compiler runtime does not change the application target'
Assert (Test-Path "$consumer/obj/Release/$Framework/Nazor/NazorStyleSheets.g.cs") 'the .NET 10 tool compiles NSS for the application target'
$engineStyles = Get-Content "$consumer/obj/Release/$Framework/Nazor/NazorStyleSheets.g.cs" -Raw
Assert ($engineStyles.Contains('BlendMode.SoftLight') -and $engineStyles.Contains('BackgroundBlendMode')) 'blend and isolation settings compile into the packaged application'
Assert ($engineStyles.Contains('FontFeatureSetting') -and $engineStyles.Contains('FontSynthesis')) 'typography settings compile into the packaged application'
Assert ($engineStyles.Contains('FilterValue') -and $engineStyles.Contains('FilterKind.Contrast')) 'ordered filter values compile into the packaged application'
Assert ($engineStyles.Contains('ConicGradient') -and $engineStyles.Contains('Repeating = true')) 'extended gradient values compile into the packaged application'
Assert (-not (Test-Path "$consumer/bin/Release/$Framework/Nazor.Nss.Compiler.dll")) 'the NSS compiler is not an application dependency'
$assets = Get-Content "$consumer/obj/project.assets.json" -Raw | ConvertFrom-Json -AsHashtable
if ($Framework -eq 'net11.0') {
    Assert ($assets.targets[$Framework]["Nazor.Controls/$version"].compile.ContainsKey('lib/net10.0/Nazor.Controls.dll')) '.NET 11 selects the LTS runtime asset'
}
$legacy = @($assets.libraries.Keys | Where-Object { $_ -match '^Nazor\.(Controls\.Advanced|Themes\.Fluent)/' })
Assert ($legacy.Count -eq 0) 'the SDK restores the single controls package'
Assert-PackedAssembly 'Nazor.Controls'
Assert (Test-Path "$consumer/bin/Release/$Framework/it/Nazor.Controls.resources.dll") 'Italian control resources are deployed'
$run = Invoke-Dotnet @('run', '--project', $consumer, '-c', 'Release', '--no-build')
Assert ($run.Output -match 'Controls rendered') 'the packaged controls render'

foreach ($mode in $DeploymentModes) {
    $output = "$consumer/publish/$mode"
    $option = if ($mode -eq 'aot') { '-p:PublishAot=true' } else { '-p:PublishTrimmed=true' }
    $published = Invoke-Dotnet @('publish', $consumer, '-c', 'Release', '-r', [Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier,
        '--self-contained', 'true', '-o', $output, '-p:NazorEnableDevTools=false', '-p:NazorEnablePublishValidation=true', '-nodeReuse:false', $option)
    $published.Output | Set-Content "$consumer/$mode-build.log"
    $executable = if ($IsWindows) { "$output/ControlsConsumer.exe" } else { "$output/ControlsConsumer" }
    $result = & $executable 2>&1 | Out-String
    $result | Set-Content "$consumer/$mode-run.log"
    Assert ($LASTEXITCODE -eq 0 -and $result -match 'Controls rendered') "$mode retains the packaged controls"
}
Write-Host "Controls package verification passed for $Framework. Consumer: $consumer"
