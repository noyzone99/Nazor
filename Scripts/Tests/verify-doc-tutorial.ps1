#Requires -Version 7
param(
    [string]$Feed,
    [string]$Workspace
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath "$PSScriptRoot/../..").Path
if (-not $Feed) {
    $Feed = Join-Path $root 'artifacts/packages'
}
$Feed = (Resolve-Path -LiteralPath $Feed).Path
if (-not $Workspace) {
    $Workspace = Join-Path ([IO.Path]::GetTempPath()) ("nazor-doc-tutorial-" + [guid]::NewGuid().ToString('N'))
}
$Workspace = [IO.Path]::GetFullPath($Workspace)
if (Test-Path -LiteralPath $Workspace) {
    throw "Choose a fresh workspace. The path already exists: $Workspace"
}

function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') exited with code $LASTEXITCODE."
    }
}

function Replace-Required([string]$Text, [string]$Pattern, [string]$Replacement) {
    if (-not $Text.Contains($Pattern)) {
        throw "Tutorial edit expected this text: $Pattern"
    }
    return $Text.Replace($Pattern, $Replacement)
}

function Assert-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Expected output file: $Path"
    }
}

$document = Get-Content -Raw -LiteralPath "$root/Docs/Guide/38-First-Desktop-Application.md"
$blocks = [regex]::Matches($document, '(?ms)^<!-- tutorial-example: (?<name>[^ \r\n]+) -->\r?\n^\x60{3}\w+\r?\n(?<code>.*?)^\x60{3}')
$snippets = @{}
foreach ($match in $blocks) {
    $snippets.Add($match.Groups['name'].Value, $match.Groups['code'].Value)
}
$expected = @(
    'ProjectNameEditor.nazor', 'ProjectNameEditor.nazor.nss', 'ProjectSession.cs',
    'register-session', 'OverviewPage.nazor', 'App.nazor', 'DetailsPage.nazor',
    'ProjectWorkspace.nazor', 'Assets/project.svg', 'image', 'drm-startup'
)
if ($blocks.Count -ne $expected.Count -or @($expected | Where-Object { -not $snippets.ContainsKey($_) }).Count) {
    throw 'The tutorial source blocks have changed. Update the verification steps to match.'
}

$version = (Select-Xml -LiteralPath "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
$template = Join-Path $Feed "Nazor.Templates.$version.nupkg"
Assert-File $template
Assert-File (Join-Path $Feed "Nazor.Sdk.$version.nupkg")
New-Item -ItemType Directory -Path $Workspace | Out-Null
Write-Host "Tutorial workspace $Workspace"

# Keep the application isolated from build settings in any surrounding checkout.
foreach ($file in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') {
    Set-Content -Encoding utf8 -LiteralPath (Join-Path $Workspace $file) -Value '<Project />'
}
Copy-Item -LiteralPath "$root/global.json" -Destination (Join-Path $Workspace 'global.json')

$packageCache = $env:NUGET_PACKAGES
$dependencyCache = $packageCache
if (-not $dependencyCache) {
    $dependencyCache = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages'
}
$feedXml = [Security.SecurityElement]::Escape($Feed)
$cacheSource = ''
$cacheMapping = ''
if (Test-Path -LiteralPath $dependencyCache -PathType Container) {
    $cacheXml = [Security.SecurityElement]::Escape([IO.Path]::GetFullPath($dependencyCache))
    $cacheSource = '<add key="dependency-cache" value="' + $cacheXml + '" />'
    $cacheMapping = '<packageSource key="dependency-cache"><package pattern="*" /></packageSource>'
}

@"
<configuration>
  <packageSources>
    <clear />
    <add key="Nazor" value="$feedXml" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    $cacheSource
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="Nazor"><package pattern="Nazor.*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
    $cacheMapping
  </packageSourceMapping>
</configuration>
"@ | Set-Content -Encoding utf8 -LiteralPath (Join-Path $Workspace 'nuget.config')

Push-Location -LiteralPath $Workspace
try {
    $env:NUGET_PACKAGES = Join-Path $Workspace '.packages'
    $hive = Join-Path $Workspace '.templates'
    Invoke-Dotnet @('new', 'install', $template, '--debug:custom-hive', $hive)
    Invoke-Dotnet @('new', 'nazor-app', '-n', 'HelloNazor', '--debug:custom-hive', $hive)
    $build = @('build', 'HelloNazor', '-warnaserror', '--verbosity', 'quiet')
    Invoke-Dotnet $build
    Write-Host 'Verified the generated template.'

    foreach ($file in 'ProjectNameEditor.nazor', 'ProjectNameEditor.nazor.nss', 'ProjectSession.cs', 'OverviewPage.nazor', 'App.nazor') {
        Set-Content -Encoding utf8 -LiteralPath "HelloNazor/$file" -Value $snippets[$file]
    }
    $programPath = 'HelloNazor/Program.cs'
    $program = Get-Content -Raw -LiteralPath $programPath
    $registration = 'builder.Services.AddSingleton<Greeter>();'
    $program = Replace-Required $program $registration ($registration + [Environment]::NewLine + $snippets['register-session'].Trim())
    Set-Content -Encoding utf8 -LiteralPath $programPath -Value $program
    Invoke-Dotnet $build
    Write-Host 'Verified the editor and session stages.'

    Set-Content -Encoding utf8 -LiteralPath 'HelloNazor/OverviewPage.nazor' -Value ("@page ""/""" + [Environment]::NewLine + $snippets['OverviewPage.nazor'])
    foreach ($file in 'DetailsPage.nazor', 'ProjectWorkspace.nazor') {
        Set-Content -Encoding utf8 -LiteralPath "HelloNazor/$file" -Value $snippets[$file]
    }
    $app = [regex]::Replace($snippets['App.nazor'], '(?ms)^<Container Class="workspace">.*?^</Container>', '<ProjectWorkspace />')
    $app = [regex]::Replace($app, '(?ms)^@style \{.*?^\}\s*$', '')
    if (-not $app.Contains('<ProjectWorkspace />') -or $app.Contains('@style')) {
        throw 'The window-shell edit no longer matches the tutorial.'
    }
    Set-Content -Encoding utf8 -LiteralPath 'HelloNazor/App.nazor' -Value $app
    Invoke-Dotnet $build
    Write-Host 'Verified the routed application.'

    New-Item -ItemType Directory -Path 'HelloNazor/Assets' | Out-Null
    Set-Content -Encoding utf8 -LiteralPath 'HelloNazor/Assets/project.svg' -Value $snippets['Assets/project.svg']
    $heading = '<Heading Level="1">Project details</Heading>'
    $details = Replace-Required $snippets['DetailsPage.nazor'] $heading ($heading + [Environment]::NewLine + '    ' + $snippets['image'].Trim())
    Set-Content -Encoding utf8 -LiteralPath 'HelloNazor/DetailsPage.nazor' -Value $details
    Invoke-Dotnet $build
    Assert-File 'HelloNazor/bin/Debug/net10.0/Assets/project.svg'

    $publish = @('publish', 'HelloNazor', '-c', 'Release', '--self-contained', 'false', '-warnaserror', '--verbosity', 'quiet')
    Invoke-Dotnet ($publish + @('-r', 'win-x64', '-o', 'publish/win-x64'))
    Assert-File 'publish/win-x64/HelloNazor.exe'
    Assert-File 'publish/win-x64/Assets/project.svg'
    Write-Host 'Verified the Windows publication and asset.'

    Invoke-Dotnet ($publish + @('-r', 'linux-x64', '-p:NazorLinuxNativeLibraries=All', '-o', 'publish/linux-x64'))
    foreach ($file in 'HelloNazor', 'Assets/project.svg', 'libnazor-wayland.so', 'libnazor-x11.so') {
        Assert-File "publish/linux-x64/$file"
    }
    Write-Host 'Verified the Linux desktop publication with both bridges.'

    $drmProgram = Replace-Required $program 'builder.Application.UseMainWindow<App>();' $snippets['drm-startup'].Trim()
    Set-Content -Encoding utf8 -LiteralPath $programPath -Value ('using Nazor.Platform.Linux;' + [Environment]::NewLine + $drmProgram)
    Invoke-Dotnet ($publish + @('-r', 'linux-x64', '-p:NazorLinuxNativeLibraries=None', '-o', 'publish/linux-drm-x64'))
    Assert-File 'publish/linux-drm-x64/HelloNazor'
    Assert-File 'publish/linux-drm-x64/Assets/project.svg'
    foreach ($file in 'libnazor-wayland.so', 'libnazor-x11.so') {
        if (Test-Path -LiteralPath "publish/linux-drm-x64/$file") {
            throw "Unexpected Linux desktop bridge in the DRM output: $file"
        }
    }
    Write-Host 'Verified the Linux DRM publication with the desktop bridges omitted.'
}
finally {
    Pop-Location
    $env:NUGET_PACKAGES = $packageCache
}
