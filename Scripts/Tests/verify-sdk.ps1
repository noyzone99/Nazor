#Requires -Version 7
param(
    [string]$Workspace = (Join-Path ([IO.Path]::GetTempPath()) 'nazor-sdk-e2e'),
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

if (-not $SkipPack) {
    Step "Packing Nazor $version"
    & "$root/Scripts/Packaging/pack.ps1"
}
Assert (Test-Path "$feed/Nazor.Sdk.$version.nupkg") "Nazor.Sdk $version is in artifacts/packages"

Step 'Official package icons'
$officialIconHash = (Get-FileHash "$root/Docs/Assets/nazor.png" -Algorithm SHA256).Hash
foreach ($package in Get-ChildItem $feed -Filter '*.nupkg') {
    $archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $reader = [IO.StreamReader]::new(($archive.Entries | Where-Object FullName -Like '*.nuspec' | Select-Object -First 1).Open())
        try { [xml]$metadata = $reader.ReadToEnd() } finally { $reader.Dispose() }
        Assert ($metadata.package.metadata.icon -eq 'nazor.png') "$($package.Name) declares the official icon"
        $entry = $archive.GetEntry('nazor.png')
        Assert ($null -ne $entry) "$($package.Name) embeds its icon"
        $stream = $entry.Open()
        try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) } finally { $stream.Dispose() }
        Assert ($hash -eq $officialIconHash) "$($package.Name) contains the current NZ artwork"
    }
    finally { $archive.Dispose() }
}

Step "Workspace $Workspace"
if (Test-Path $Workspace) { Remove-Item $Workspace -Recurse -Force }
$cache = Join-Path $Workspace '.packages'
$hive = Join-Path $Workspace '.templates'
$workspaceFeed = Join-Path $Workspace '.feed'
New-Item -ItemType Directory -Force $workspaceFeed | Out-Null
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
    <add key="workspace" value="$workspaceFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="nazor"><package pattern="Nazor.*" /></packageSource>
    <packageSource key="workspace"><package pattern="DeviceRegistry.Components" /></packageSource>
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
Invoke-Dotnet @('new', 'nazor-app', '-n', 'HelloNazor', '-o', $app, '--debug:custom-hive', $hive) | Out-Null
Assert (Select-String -Path "$app/HelloNazor.csproj" -Pattern "Nazor.Sdk/$version" -Quiet) 'the project uses Nazor.Sdk with the packed version'
Assert (Select-String -Path "$app/HelloNazor.csproj" -Pattern '<TargetFramework>net10.0</TargetFramework>' -Quiet) 'the template uses the neutral desktop framework'
Assert (-not (Test-Path "$app/app.manifest") -and -not (Select-String -Path "$app/HelloNazor.csproj" -Pattern 'ApplicationManifest' -Quiet)) 'the template relies on the SDK manifest without a local copy or project property'
Assert (Select-String -Path "$app/HelloNazor.csproj" -Pattern '<NazorApplicationId>org.example.myapp</NazorApplicationId>' -Quiet) 'the template declares a valid application ID'
Assert ((Test-Path "$app/_Imports.nazor") -and (Test-Path "$app/Services/Greeter.cs") -and -not (Test-Path "$app/Pages")) 'the default template creates the single-window app with shared imports and a service folder'
Assert (-not (Select-Xml -LiteralPath "$app/HelloNazor.csproj" -XPath '//PackageReference')) 'the Basic theme uses the built-in styles'
$customIdApp = Join-Path $Workspace 'CustomIdentity'
Invoke-Dotnet @('new', 'nazor-app', '-n', 'CustomIdentity', '-o', $customIdApp, '--ApplicationId', 'org.example.customidentity', '--debug:custom-hive', $hive) | Out-Null
Assert (Select-String -Path "$customIdApp/CustomIdentity.csproj" -Pattern '<NazorApplicationId>org.example.customidentity</NazorApplicationId>' -Quiet) 'the template accepts an explicit application ID without changing the project name'

$themePackages = @{ basic = ''; fluent = "Nazor.Themes.Fluent/$version"; nazorleaf = "Nazor.Themes.NazorLeaf/$version" }
foreach ($theme in 'basic', 'fluent', 'nazorleaf') {
    $sampleName = "Sample-$theme"
    $sampleApp = Join-Path $Workspace $sampleName
    $sampleProject = "$sampleApp/$sampleName.csproj"
    Invoke-Dotnet @('new', 'nazor-app', '-n', $sampleName, '-o', $sampleApp, '--Theme', $theme,
        '--IncludeSamplePages', '--debug:custom-hive', $hive) | Out-Null
    $references = Select-Xml -LiteralPath $sampleProject -XPath '//PackageReference' |
        ForEach-Object { "$($_.Node.Include)/$($_.Node.Version)" }
    Assert (($references -join ',') -ceq $themePackages[$theme]) "the $theme template references only the selected theme package"
    foreach ($file in 'App.nazor', '_Imports.nazor', 'Views/DesktopView.nazor', 'Pages/HomePage.nazor', 'Pages/CounterPage.nazor',
        'Pages/TasksPage.nazor', 'Components/PageIntro.nazor', 'Models/SampleTask.cs', 'Services/SampleTaskStore.cs') {
        Assert (Test-Path -LiteralPath "$sampleApp/$file") "the $theme sample includes $file"
    }
    Assert (-not (Test-Path "$sampleApp/SamplePages") -and -not (Test-Path "$sampleApp/App.nazor.cs")) 'the sample layout replaces the single-window example'
    Invoke-Dotnet @('build', $sampleProject, '-c', 'Release', '-warnaserror') | Out-Null
}

@'
namespace HelloNazor;

public sealed class SdkImports
{
    public ComponentBase? Component { get; set; }
    public Button? Control { get; set; }
    public KeyboardEventArgs? Input { get; set; }
    public StyleSheet? Style { get; set; }
    public Point Position { get; set; }
    public SemanticRole Role { get; set; }
    public StyleSheet Theme => Themes.Default;
    public Color Accent => Color.FromRgb(0, 120, 215);
    public Nazor.Graphics.Color Pixel => Nazor.Graphics.Color.Black;
    public IConfiguration? Configuration { get; set; }
    public IHost? Host { get; set; }
    public ILogger<SdkImports>? Logger { get; set; }
    public IOptions<SdkImports>? Options { get; set; }
    public IStringLocalizer<SdkImports>? Localizer { get; set; }
    public CultureInfo Culture => CultureInfo.InvariantCulture;
    [JsonIgnore] public string Json => JsonSerializer.Serialize(Accent);
    public Task<SdkImports?> ReadAsync(HttpClient client) => client.GetFromJsonAsync<SdkImports>("settings.json");
    public void Register(IServiceCollection services) => services.AddSingleton(this);
}
'@ | Set-Content "$app/SdkImports.cs"

New-Item -ItemType Directory -Force "$app/images" | Out-Null
[System.IO.File]::WriteAllBytes("$app/images/logo.png", [byte[]](137, 80, 78, 71))
[System.IO.File]::WriteAllBytes("$app/images/logo@2x.png", [byte[]](137, 80, 78, 71))

New-Item -ItemType Directory -Force "$app/Fonts" | Out-Null
Copy-Item "$root/Tests/Assets/Fonts/FiraSans-Regular.ttf" "$app/Fonts/Regular.ttf"
$themeStartup = "app.Appearance.Theme = Nazor.Themes.Themes.Basic;"
(Get-Content "$app/Program.cs" -Raw).Replace('builder.Application.UseMainWindow<App>();', "builder.Fonts.Add(""Fonts/Regular.ttf"");`nbuilder.Application.UseMainWindow<App>();").Replace('app.Appearance.StyleSheet = HelloNazor.StyleSheets.Theme;', "app.Appearance.StyleSheet = HelloNazor.StyleSheets.Theme;`n$themeStartup") | Set-Content "$app/Program.cs"

Step 'Clean build'
$generated = Join-Path $app 'obj/Release/net10.0/Nazor'
Invoke-Dotnet @('build', $app, '-c', 'Release', '-nodeReuse:false') | Out-Null
Assert (Test-Path "$app/bin/Release/net10.0/HelloNazor.dll") 'the application assembly is built'
Assert (Test-Path "$generated/App.nazor.g.cs") 'App.nazor was generated'
Assert (Test-Path "$generated/NazorStyleSheets.g.cs") 'Theme.nss was compiled'
Assert (Test-Path "$app/bin/Release/net10.0/Nazor.Application.dll") 'the runtime comes from the packages'
Assert (Test-Path "$app/bin/Release/net10.0/Nazor.Controls.dll") 'the control library comes with the SDK'
Assert (-not (Get-ChildItem "$app/bin/Release/net10.0" -Filter 'Nazor.Diagnostics*') -and
    -not (Get-ChildItem "$app/bin/Release/net10.0" -Filter 'Nazor.DevTools*')) 'ordinary application output omits optional diagnostics and DevTools assemblies'
Assert (-not (Select-String -Path "$app/obj/project.assets.json" -Pattern '"Nazor\.(Diagnostics|DevTools[^"/]*)/' -Quiet)) 'the ordinary resolved dependency graph excludes diagnostics and DevTools packages'
Assert (Test-Path "$app/bin/Release/net10.0/runtimes/win-x64/native/libSkiaSharp.dll") 'the native Skia and HarfBuzz libraries are deployed'
Assert (Test-Path "$app/bin/Release/net10.0/appsettings.json") 'appsettings.json is copied next to the application (host configuration)'
Assert ((Test-Path "$app/bin/Release/net10.0/images/logo.png") -and (Test-Path "$app/bin/Release/net10.0/images/logo@2x.png")) 'the images of the project are copied with their folders (NazorAsset)'
Assert (Select-String -Path "$app/Program.cs" -Pattern 'NazorApplication.CreateBuilder' -Quiet) 'the template uses platform-independent startup'
Assert (Test-Path "$generated/NazorPlatformBootstrap.g.cs") 'the executable has a generated platform bootstrap'

Assert (Test-Path "$app/bin/Release/net10.0/Fonts/Regular.ttf") 'the SDK copies private fonts without explicit asset declarations'

Step 'Publish assets'
Invoke-Dotnet @('publish', $app, '-c', 'Release', '--no-build', '--no-restore', '-o', "$Workspace/publish") | Out-Null
Assert (Test-Path "$Workspace/publish/Fonts/Regular.ttf") 'private fonts are deployed by publish'
Assert (Test-Path "$Workspace/publish/Nazor.Controls.dll") 'application publication deploys the control library'
Assert (-not (Get-ChildItem "$Workspace/publish" -Filter 'Nazor.Diagnostics*') -and
    -not (Get-ChildItem "$Workspace/publish" -Filter 'Nazor.DevTools*')) 'ordinary publish output omits optional diagnostics and DevTools assemblies'

Step 'Incremental build'
$second = Invoke-Dotnet @('build', $app, '-c', 'Release', '-nodeReuse:false', '-v:n')
Assert ($second.Output -match 'Skipping target "NazorGenerate"') 'nothing changed: generation is skipped'

Step 'NSS change'
$before = Get-Content "$generated/NazorStyleSheets.g.cs" -Raw
(Get-Content "$app/Theme.nss" -Raw).Replace('LinkText', '#107c10') | Set-Content "$app/Theme.nss" -NoNewline
Invoke-Dotnet @('build', $app, '-c', 'Release', '-nodeReuse:false') | Out-Null
$after = Get-Content "$generated/NazorStyleSheets.g.cs" -Raw
Assert ($before -ne $after) 'editing Theme.nss regenerates its C#'

Step 'Sources embedded in the PDB'
Invoke-Dotnet @('build', $app, '-c', 'Release', '-nodeReuse:false', '--no-incremental', '-p:EmbedAllSources=true') | Out-Null
Assert (Test-Path "$app/bin/Release/net10.0/HelloNazor.pdb") 'the generated C# can be embedded in the PDB (no #line to missing files)'

Step 'Rebuild'
Invoke-Dotnet @('build', $app, '-c', 'Release', '-nodeReuse:false', '--no-incremental') | Out-Null
Assert (Test-Path "$app/bin/Release/net10.0/HelloNazor.dll") 'a full rebuild succeeds'

Step 'Development tools: launch profile, dotnet watch inputs and project metadata'
Assert (Test-Path "$app/Properties/launchSettings.json") 'the created project has Properties/launchSettings.json'
Assert (Select-String -Path "$app/Properties/launchSettings.json" -Pattern '"NAZOR_DEVCHANNEL": "1"' -Quiet) 'the launch profile turns the development channel on (hot reload of styles)'
$env:DOTNET_WATCH_SUPPRESS_EMOJIS = '1'
$watched = Invoke-Dotnet @('watch', '--list', '--project', "$app/HelloNazor.csproj")
Assert (($watched.Output -match '(?m)App\.nazor\s*$') -and ($watched.Output -match '(?m)Theme\.nss\s*$')) 'dotnet watch lists the .nazor and .nss inputs (Watch items of the SDK)'
Assert (-not (Select-String -Path "$app/obj/Release/net10.0/HelloNazor.AssemblyInfo.cs" -Pattern 'Nazor\.ProjectDirectory' -Quiet)) 'a Release build carries no project directory metadata'
Invoke-Dotnet @('build', $app, '-c', 'Debug', '-nodeReuse:false') | Out-Null
Assert (Select-String -Path "$app/obj/Debug/net10.0/HelloNazor.AssemblyInfo.cs" -Pattern 'AssemblyMetadata\("Nazor\.ProjectDirectory"' -Quiet) 'a Debug build carries AssemblyMetadata("Nazor.ProjectDirectory") for nazorc watch and nazorc dev'

Step 'Diagnostics at the original position'
$markup = Get-Content "$app/App.nazor" -Raw
$markup.Replace('font-weight: bold;', 'font-weight: bold; colr: red;') | Set-Content "$app/App.nazor" -NoNewline
$failed = Invoke-Dotnet @('build', $app, '-c', 'Release', '-nodeReuse:false') -AllowFailure
Assert ($failed.ExitCode -ne 0) 'an invalid NSS declaration fails the build'
Assert ($failed.Output -match 'App\.nazor\(\d+,\d+\).*NZS\d+') 'the error points into App.nazor with line and column'
$markup | Set-Content "$app/App.nazor" -NoNewline

Step 'Visual Studio: MSBuild, design-time build and fast up-to-date check'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$vsMSBuild = if (Test-Path $vswhere) { & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1 }
if (-not $vsMSBuild) {
    Write-Host '    skipped: Visual Studio with MSBuild was not found' -ForegroundColor Yellow
}
else {
    function Invoke-VSMSBuild([string[]]$Arguments, [switch]$AllowFailure) {
        $output = & $vsMSBuild @Arguments -nologo -nodeReuse:false 2>&1 | Out-String
        if (-not $AllowFailure -and $LASTEXITCODE -ne 0) { Write-Host $output; throw "MSBuild.exe $($Arguments -join ' ') failed with exit code $LASTEXITCODE" }
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    }
    $designTime = @("$app/HelloNazor.csproj", '-p:Configuration=Debug', '-p:DesignTimeBuild=true', '-p:SkipCompilerExecution=true',
        '-p:ProvideCommandLineArgs=true', '-p:BuildingInsideVisualStudio=true',
        '-t:CollectUpToDateCheckInputDesignTime;CollectUpToDateCheckOutputDesignTime;CompileDesignTime',
        '-getTargetResult:CollectUpToDateCheckInputDesignTime', '-getTargetResult:CollectUpToDateCheckOutputDesignTime',
        '-getItem:CscCommandLineArgs', '-getItem:Compile')

    Remove-Item "$app/bin/Debug" -Recurse -Force -ErrorAction Ignore
    Invoke-VSMSBuild @("$app/HelloNazor.csproj", '-restore', '-p:Configuration=Debug', '-v:m') | Out-Null
    Assert (Test-Path "$app/bin/Debug/net10.0/HelloNazor.dll") "Visual Studio's MSBuild builds the project ($vsMSBuild)"

    $result = (Invoke-VSMSBuild $designTime).Output | ConvertFrom-Json
    $inputs = $result.TargetResults.CollectUpToDateCheckInputDesignTime.Items | Where-Object { $_.Set -eq 'Nazor' } | ForEach-Object { Split-Path $_.Identity -Leaf }
    $outputs = $result.TargetResults.CollectUpToDateCheckOutputDesignTime.Items | Where-Object { $_.Set -eq 'Nazor' } | ForEach-Object { Split-Path $_.Identity -Leaf }
    Assert (($inputs -contains 'App.nazor') -and ($inputs -contains 'Theme.nss')) 'the fast up-to-date check watches .nazor and .nss files'
    Assert ($outputs -contains 'nazor.stamp') 'the fast up-to-date check compares them with the generation stamp'
    $sources = $result.Items.CscCommandLineArgs | ForEach-Object { $_.Identity } | Where-Object { $_ -like '*.cs' }
    Assert ([bool]($sources | Where-Object { $_ -like '*App.nazor.g.cs' })) 'a design-time build gives the generated C# to IntelliSense'
    Assert ([bool]($sources | Where-Object { $_ -like '*NazorPlatformBootstrap.g.cs' })) 'a design-time build includes the platform bootstrap'
    $codeBehind = $result.Items.Compile | Where-Object { $_.Identity -eq 'App.nazor.cs' }
    Assert ($codeBehind.DependentUpon -eq 'App.nazor') 'App.nazor.cs is nested under App.nazor'

    $markup = Get-Content "$app/App.nazor" -Raw
    $markup.Replace('<Container Class="page">', '<Container Class="page"><div></div>') | Set-Content "$app/App.nazor" -NoNewline
    $broken = Invoke-VSMSBuild ($designTime + '-getProperty:_NazorExitCode') -AllowFailure
    $markup | Set-Content "$app/App.nazor" -NoNewline
    $jsonStart = [regex]::Match($broken.Output, '(?m)^\{').Index
    $brokenResult = $broken.Output.Substring($jsonStart) | ConvertFrom-Json
    Assert ($brokenResult.Properties._NazorExitCode -ne '0' -and $broken.Output -match 'NZC010') 'the broken App.nazor really fails generation (NZC010)'
    Assert ($broken.ExitCode -eq 0) 'a design-time build does not fail on an error in App.nazor'
    $brokenSources = $brokenResult.Items.CscCommandLineArgs | ForEach-Object { $_.Identity }
    Assert ([bool]($brokenSources | Where-Object { $_ -like '*App.nazor.g.cs' })) 'meanwhile IntelliSense keeps the last generated C#'
}

Step 'Component library as a package'
$library = Join-Path $Workspace 'DeviceRegistry.Components'
New-Item -ItemType Directory $library | Out-Null
Copy-Item "$root/Tests/Fixtures/Projects/DeviceRegistry/Components/*.nazor*" $library
@"
<Project Sdk="Nazor.Sdk/$version">
  <PropertyGroup>
    <RootNamespace>DeviceRegistry.Components</RootNamespace>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
"@ | Set-Content "$library/DeviceRegistry.Components.csproj"
Invoke-Dotnet @('pack', $library, '-c', 'Release', '-o', $workspaceFeed, '-nodeReuse:false') | Out-Null
Assert (Test-Path "$workspaceFeed/DeviceRegistry.Components.1.0.0.nupkg") 'the library is packed'
$libraryAssets = Get-Content "$library/obj/project.assets.json" -Raw | ConvertFrom-Json -AsHashtable
Assert ($libraryAssets.project.frameworks.ContainsKey('net10.0')) 'a library defaults to the platform-neutral framework'
Assert (-not [bool]($libraryAssets.libraries.Keys | Where-Object { $_ -like 'Nazor.Hosting.Desktop/*' -or $_ -like 'Nazor.Hosting.Windows/*' -or $_ -like 'Nazor.Platform.Windows/*' })) 'a shared component library does not reference the Windows host or backend'

Step 'Desktop runtime hosting, shared libraries and opt-outs'
$probe = Join-Path $Workspace 'SdkProbe'
New-Item -ItemType Directory $probe | Out-Null
$probeProject = Join-Path $probe 'SdkProbe.csproj'
function Write-Probe([string]$properties) {
    "<Project Sdk=""Nazor.Sdk/$version""><PropertyGroup>$properties</PropertyGroup></Project>" | Set-Content $probeProject
}
function Read-Probe([string[]]$properties = @()) {
    $result = Invoke-Dotnet (@('msbuild', $probeProject, '-nologo', '-getProperty:TargetFramework,TargetFrameworks,ApplicationManifest,NoWin32Manifest,ApplicationIcon', '-getItem:PackageReference,Using') + $properties)
    return $result.Output | ConvertFrom-Json
}
Write-Probe '<OutputType>WinExe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>'
$selection = Read-Probe
Assert ($selection.Properties.TargetFramework -eq 'net10.0') 'an application defaults to the neutral desktop framework'
Assert ($selection.Items.PackageReference.Identity -contains 'Nazor.Hosting.Desktop') 'an executable automatically references the desktop host'
$defaultManifest = $selection.Properties.ApplicationManifest
Assert ((Test-Path $defaultManifest) -and (Split-Path $defaultManifest -Leaf) -eq 'Nazor.Windows.manifest') 'the executable defaults to the manifest shipped inside the SDK package'
$defaultIcon = $selection.Properties.ApplicationIcon
Assert ((Test-Path $defaultIcon) -and (Split-Path $defaultIcon -Leaf) -eq 'nazor.ico') 'the executable defaults to the Nazor application icon shipped inside the SDK package'
$selection = Read-Probe @('-p:ApplicationIcon=custom icon.ico')
Assert ($selection.Properties.ApplicationIcon -eq 'custom icon.ico') 'a custom application icon takes precedence'
$selection = Read-Probe @('-p:NazorUseDefaultApplicationIcon=false')
Assert ($selection.Properties.ApplicationIcon -eq '') 'the application icon default can be disabled'
$selection = Read-Probe @('-p:Win32Resource=custom.res')
Assert ($selection.Properties.ApplicationIcon -eq '') 'custom Win32 resources suppress the default icon'
@"
public static class Program
{
    public static int Main(string[] args)
    {
        var expectedDpiContext = args.Length == 0 ? -4 : int.Parse(args[0], global::System.Globalization.CultureInfo.InvariantCulture);
        if (!AreDpiAwarenessContextsEqual(GetThreadDpiAwarenessContext(), expectedDpiContext)) return 3;
        if (LoadImageW(GetModuleHandleW(null), 32512, 1, 16, 16, 0x8000) == 0) return 4;
        var builder = NazorApplication.CreateBuilder();
        if (builder.Platform.Host is not DesktopPlatformHost) return 1;
        builder.Application.CreateMainWindow<Button>();
        using var app = builder.Build();
        var legacy = WindowsApplication.CreateBuilder();
        if (legacy.Platform.Host is not global::Nazor.Platform.Windows.WindowsPlatformHost) return 2;
        legacy.Application.CreateMainWindow<Button>();
        using var legacyApp = legacy.Build();
        return 0;
    }

    [global::System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint GetThreadDpiAwarenessContext();

    [global::System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = global::System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? name);

    [global::System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint LoadImageW(nint instance, nint name, uint type, int width, int height, uint flags);

    [global::System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: global::System.Runtime.InteropServices.MarshalAs(global::System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool AreDpiAwarenessContextsEqual(nint first, nint second);
}
"@ | Set-Content "$probe/SharedComponent.cs"
Invoke-Dotnet @('build', $probeProject, '-c', 'Release', '-nodeReuse:false') | Out-Null
Assert (Test-Path "$probe/bin/Release/net10.0/SdkProbe.dll") 'a neutral executable builds with automatic desktop hosting'
$startup = Invoke-Dotnet @('run', '--project', $probeProject, '-c', 'Release', '--no-build', '--no-restore')
Assert ($startup.ExitCode -eq 0) 'the executable starts with PerMonitorV2 DPI awareness, desktop hosting and WindowsApplication compatibility'
foreach ($runtime in 'win-x64', 'win-arm64', 'linux-x64', 'linux-arm64') {
    $selection = Read-Probe @("-p:RuntimeIdentifier=$runtime")
    $backend = if ($runtime.StartsWith('win-')) { 'Windows' } else { 'Linux' }
    Assert ($selection.Items.PackageReference.Identity -contains "Nazor.Hosting.Desktop.$backend") "RID $runtime selects only the $backend desktop package"
    Assert ($selection.Items.PackageReference.Identity -notcontains 'Nazor.Hosting.Desktop') "RID $runtime omits the portable desktop package"
}
$unsupported = Invoke-Dotnet @('msbuild', $probeProject, '-t:NazorValidateTargetPlatform', '-p:RuntimeIdentifier=osx-arm64', '-nologo') -AllowFailure
Assert ($unsupported.ExitCode -ne 0 -and $unsupported.Output -match 'NZB003') 'an unsupported desktop RID fails with an actionable error'
$selection = Read-Probe @('-p:TargetFramework=net10.0-windows')
Assert ($selection.Items.PackageReference.Identity -contains 'Nazor.Hosting.Desktop') 'an optional Windows API framework still uses runtime desktop selection'

Step 'Windows manifests: publish, custom files, incremental changes and opt-out'
$manifestPublish = Join-Path $Workspace 'manifest-publish'
Invoke-Dotnet @('publish', $probeProject, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', $manifestPublish, '-nodeReuse:false') | Out-Null
& "$manifestPublish/SdkProbe.exe"
Assert ($LASTEXITCODE -eq 0) 'the published Windows apphost retains PerMonitorV2 DPI awareness and its Nazor application icon'

New-Item -ItemType Directory -Force "$probe/Platforms/Windows" | Out-Null
$customManifest = Join-Path $probe 'Platforms/Windows/custom app.manifest'
(Get-Content $defaultManifest -Raw).Replace('PerMonitorV2', 'system') | Set-Content $customManifest
Write-Probe '<OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId><NazorWindowsAppManifest>Platforms/Windows/custom app.manifest</NazorWindowsAppManifest>'
$selection = Read-Probe
Assert ($selection.Properties.ApplicationManifest -eq 'Platforms/Windows/custom app.manifest') 'a custom manifest path with spaces takes precedence over the SDK default'
Invoke-Dotnet @('build', $probeProject, '-c', 'Release', '-nodeReuse:false') | Out-Null
$customStartup = Invoke-Dotnet @('run', '--project', $probeProject, '-c', 'Release', '--no-build', '--no-restore', '--', '-2')
Assert ($customStartup.ExitCode -eq 0) 'an Exe uses the custom system-aware manifest without merging the default DPI settings'
Invoke-Dotnet @('publish', $probeProject, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', $manifestPublish, '-nodeReuse:false') | Out-Null
& "$manifestPublish/SdkProbe.exe" -2
Assert ($LASTEXITCODE -eq 0) 'publish embeds the custom manifest in the Windows apphost'

(Get-Content $customManifest -Raw).Replace('>system<', '>PerMonitorV2<') | Set-Content $customManifest
Invoke-Dotnet @('build', $probeProject, '-c', 'Release', '-nodeReuse:false') | Out-Null
$customStartup = Invoke-Dotnet @('run', '--project', $probeProject, '-c', 'Release', '--no-build', '--no-restore')
Assert ($customStartup.ExitCode -eq 0) 'editing a custom manifest updates DPI awareness through an incremental build'

Write-Probe '<OutputType>WinExe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId><ApplicationManifest>missing.manifest</ApplicationManifest>'
$missingManifest = Invoke-Dotnet @('build', $probeProject, '-c', 'Release', '-nodeReuse:false') -AllowFailure
Assert ($missingManifest.ExitCode -ne 0 -and $missingManifest.Output -match 'missing\.manifest') 'a missing custom manifest fails instead of silently falling back to the SDK default'

Write-Probe '<OutputType>WinExe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId><NoWin32Manifest>true</NoWin32Manifest>'
$selection = Read-Probe
Assert ($selection.Properties.ApplicationManifest -eq '') 'NoWin32Manifest disables the SDK default'
Invoke-Dotnet @('build', $probeProject, '-c', 'Release', '-nodeReuse:false') | Out-Null
$disabledStartup = Invoke-Dotnet @('run', '--project', $probeProject, '-c', 'Release', '--no-build', '--no-restore', '--', '-1')
Assert ($disabledStartup.ExitCode -eq 0) 'the opt-out builds and starts without a DPI-aware manifest'

Write-Probe '<TargetFrameworks>net10.0;net10.0-windows</TargetFrameworks>'
$selection = Read-Probe
Assert ($selection.Properties.TargetFramework -eq '' -and $selection.Items.PackageReference.Count -eq 0) 'an explicit TargetFrameworks remains intact without references in the outer build'
foreach ($framework in 'net10.0', 'net10.0-windows') {
    $selection = Read-Probe @("-p:TargetFramework=$framework")
    Assert ($selection.Properties.ApplicationManifest -eq '') "the $framework component library has no SDK application manifest"
    Assert ($selection.Properties.ApplicationIcon -eq '') "the $framework component library has no application icon"
}
@"
public sealed class SharedComponent : ComponentBase
{
    public static NazorApplicationBuilder CreateBuilder() => NazorApplication.CreateBuilder();
    public Button? Control { get; set; }
    public KeyboardEventArgs? Input { get; set; }
    public StyleSheet? Style { get; set; }
    public Point Position { get; set; }
}
"@ | Set-Content "$probe/SharedComponent.cs"
Invoke-Dotnet @('build', $probeProject, '-c', 'Release', '-nodeReuse:false') | Out-Null
Assert ((Test-Path "$probe/bin/Release/net10.0/SdkProbe.dll") -and (Test-Path "$probe/bin/Release/net10.0-windows/SdkProbe.dll")) 'optional library multitargeting builds with base packages and global usings'
$probeAssets = Get-Content "$probe/obj/project.assets.json" -Raw | ConvertFrom-Json -AsHashtable
Assert (-not [bool]($probeAssets.libraries.Keys | Where-Object { $_ -like 'Nazor.Hosting.*/*' -or $_ -like 'Nazor.Platform.Windows/*' })) 'libraries have no implicit desktop or Windows host, regardless of their target framework'

$libraryConsumer = Join-Path $Workspace 'LibraryConsumer'
New-Item -ItemType Directory $libraryConsumer | Out-Null
@"
<Project Sdk="Nazor.Sdk/$version">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>
    <NazorImplicitRuntimeReference>false</NazorImplicitRuntimeReference>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../SdkProbe/SdkProbe.csproj" />
  </ItemGroup>
</Project>
"@ | Set-Content "$libraryConsumer/LibraryConsumer.csproj"
@"
var builder = SharedComponent.CreateBuilder();
builder.Application.CreateMainWindow<global::Nazor.Controls.Button>();
using var app = builder.Build();
return builder.Platform.Host is null ? 0 : 1;
"@ | Set-Content "$libraryConsumer/Program.cs"
$consumer = Invoke-Dotnet @('run', '--project', $libraryConsumer, '-c', 'Release')
Assert ($consumer.ExitCode -eq 0) 'a shared library does not register a host and the executable runtime opt-out suppresses bootstrap'

Write-Probe '<TargetFramework>net10.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings>'
$disabled = Invoke-Dotnet @('build', $probeProject, '-c', 'Release', '-nodeReuse:false') -AllowFailure
Assert ($disabled.ExitCode -ne 0 -and $disabled.Output -match 'CS0246') 'disabling implicit usings requires explicit namespace imports'
$sharedSource = Get-Content "$probe/SharedComponent.cs" -Raw
"using Nazor;`nusing Nazor.Controls;`nusing Nazor.Input;`nusing Nazor.Style;`nusing Nazor.Graphics;`n$sharedSource" | Set-Content "$probe/SharedComponent.cs"
Invoke-Dotnet @('build', $probeProject, '-c', 'Release', '-nodeReuse:false') | Out-Null

Write-Probe '<TargetFramework>net10.0</TargetFramework><NazorImplicitRuntimeReference>false</NazorImplicitRuntimeReference>'
$selection = Read-Probe
Assert ($selection.Items.PackageReference.Count -eq 0 -and -not ($selection.Items.Using.Identity -contains 'Nazor')) 'the runtime opt-out removes automatic packages and Nazor usings'
'public sealed class PlainLibrary { }' | Set-Content "$probe/SharedComponent.cs"
Invoke-Dotnet @('build', $probeProject, '-c', 'Release', '-nodeReuse:false') | Out-Null

Write-Probe '<TargetFramework>net10.0-android</TargetFramework><OutputType>Exe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>'
$invalid = Invoke-Dotnet @('msbuild', $probeProject, '-t:NazorValidateTargetPlatform') -AllowFailure
Assert ($invalid.ExitCode -ne 0 -and $invalid.Output -match 'NZB002') 'a mobile target reports that a dedicated host is required'

Step 'Acceptance app against the library package'
$registry = Join-Path $Workspace 'DeviceRegistry'
New-Item -ItemType Directory $registry | Out-Null
Copy-Item "$root/Tests/Fixtures/Projects/DeviceRegistry/*" $registry -Include '*.nazor', '*.cs', '*.nss', '*.json', 'app.manifest'
Copy-Item -LiteralPath "$root/Tests/Fixtures/Projects/DeviceRegistry/Styles" -Destination "$registry/Styles" -Recurse
@"
<Project Sdk="Nazor.Sdk/$version">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>WinExe</OutputType><NazorApplicationId>org.nazor.testapp</NazorApplicationId>
    <RootNamespace>DeviceRegistry</RootNamespace>
    <NazorWindowsAppManifest>app.manifest</NazorWindowsAppManifest>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="DeviceRegistry.Components" Version="1.0.0" />
  </ItemGroup>
</Project>
"@ | Set-Content "$registry/DeviceRegistry.csproj"
Invoke-Dotnet @('build', $registry, '-c', 'Release', '-nodeReuse:false') | Out-Null
Assert (Test-Path "$registry/bin/Release/net10.0/DeviceRegistry.dll") 'the acceptance app builds with the packaged SDK'
Assert (Select-String -Path "$registry/obj/Release/net10.0/Nazor/App.nazor.g.cs" -Pattern 'global::DeviceRegistry.Components.DeviceCard' -Quiet) 'DeviceCard is resolved from the compiled library'

foreach ($framework in 'net8.0', 'net9.0', 'net10.0', 'net11.0') {
    & "$PSScriptRoot/verify-controls-packages.ps1" -Framework $framework -Feed $feed -Workspace "$Workspace/Controls"
}

Step '.NET 11 template consumer'
$net11 = Join-Path $Workspace 'HelloNazor11'
New-Item -ItemType Directory -Force $net11 | Out-Null
'{ "sdk": { "version": "11.0.100", "rollForward": "latestPatch", "allowPrerelease": false } }' | Set-Content "$net11/global.json"
Invoke-Dotnet @('new', 'nazor-app', '-n', 'HelloNazor11', '-o', $net11, '--framework', 'net11.0', '--debug:custom-hive', $hive) | Out-Null
Assert (Select-String -Path "$net11/HelloNazor11.csproj" -Pattern '<TargetFramework>net11.0</TargetFramework>' -Quiet) 'the template accepts .NET 11'
Push-Location $net11
try {
Invoke-Dotnet @('build', $net11, '-c', 'Release', '-m:1', '-nodeReuse:false') | Out-Null
Assert (Test-Path "$net11/obj/Release/net11.0/Nazor/App.nazor.g.cs") 'Razor generates .NET 11 application components'
Invoke-Dotnet @('publish', $net11, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-m:1', '-nodeReuse:false', '-o', "$net11/publish") | Out-Null
Assert (Test-Path "$net11/publish/HelloNazor11.exe") 'the .NET 11 template publishes a native application'
}
finally { Pop-Location }

& "$PSScriptRoot/verify-devtools-publish.ps1" -Workspace "$Workspace/DevToolsPolicy" -Framework net10.0 -Feed $feed

& "$PSScriptRoot/verify-application-metadata.ps1" -Feed $feed

Write-Host ("SDK verification passed in {0:N0} s." -f ((Get-Date) - $started).TotalSeconds) -ForegroundColor Green
