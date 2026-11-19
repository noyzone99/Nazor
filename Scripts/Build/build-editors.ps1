#Requires -Version 7
param([string]$Output = (Join-Path (Resolve-Path "$PSScriptRoot/../..").Path 'artifacts/editors'))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$Output = [IO.Path]::GetFullPath($Output)
if (-not $Output.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Editor build output must be inside the repository.' }
$grammars = Join-Path $root 'Sources/Tools/Nazor.Grammars'

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw "Check failed: $message" }
    Write-Host "    ok: $message"
}

if (Test-Path $Output) { Remove-Item $Output -Recurse -Force }
New-Item -ItemType Directory $Output | Out-Null
foreach ($file in Get-ChildItem $grammars -Filter *.json) {
    $null = Get-Content $file.FullName -Raw | ConvertFrom-Json
}

Write-Host '==> Language server'
$server = Join-Path $Output 'server'
& dotnet publish (Join-Path $root 'Sources/Tools/Nazor.LanguageServer') -c Release -o $server -p:UseAppHost=false -p:UseSharedCompilation=false -m:1 -nodeReuse:false -nologo -v:q
if ($LASTEXITCODE -ne 0) { throw 'The language server could not be published.' }
foreach ($required in 'Nazor.LanguageServer.dll', 'Nazor.LanguageServer.runtimeconfig.json', 'Nazor.Compiler.dll', 'Microsoft.CodeAnalysis.Razor.Compiler.dll', 'Nazor.Controls.dll') {
    Assert (Test-Path (Join-Path $server $required)) "the server contains $required"
}
$serverRuntime = Get-Content (Join-Path $server 'Nazor.LanguageServer.runtimeconfig.json') -Raw | ConvertFrom-Json
Assert ($serverRuntime.runtimeOptions.tfm -eq 'net10.0') 'both IDE extensions run their bundled language server on .NET 10 LTS'

Write-Host '==> Published language server regression'
$serverTests = Join-Path $root 'Tests/Nazor.LanguageServer.Tests'
& dotnet build $serverTests -c Release -p:UseSharedCompilation=false -m:1 -nodeReuse:false -nologo -v:q
if ($LASTEXITCODE -ne 0) { throw 'The language server tests could not be built.' }
$previousServer = $env:NAZOR_TEST_SERVER
try {
    $env:NAZOR_TEST_SERVER = Join-Path $server 'Nazor.LanguageServer.dll'
    & dotnet (Join-Path $serverTests 'bin/Release/net10.0/Nazor.LanguageServer.Tests.dll') -noLogo -method '*The_server_process_speaks_LSP_on_stdio'
    if ($LASTEXITCODE -ne 0) { throw 'The published server failed component/C# hover, coloring or refresh checks.' }
}
finally { $env:NAZOR_TEST_SERVER = $previousServer }

Write-Host '==> Visual Studio extension (VSIX)'
$completionImages = Join-Path $Output 'completion-colors'
& dotnet (Join-Path $server 'Nazor.LanguageServer.dll') --completion-images $completionImages
if ($LASTEXITCODE -ne 0) { throw 'Completion color images could not be generated.' }
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$msbuild = if (Test-Path $vswhere) {
    & $vswhere -latest -prerelease -requires Microsoft.VisualStudio.Workload.VisualStudioExtension -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
}
if (-not $msbuild) {
    Write-Host '    skipped: Visual Studio with the extension development workload was not found' -ForegroundColor Yellow
}
else {
    $project = Join-Path $root 'Sources/Tools/Nazor.VisualStudio/Nazor.VisualStudio.csproj'
    $log = & $msbuild $project -restore -nologo -nodeReuse:false -p:Configuration=Release "-p:NazorServerDirectory=$server" "-p:NazorCompletionImagesDirectory=$completionImages" -v:m 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host $log; throw 'The VSIX build failed.' }
    $tests = Join-Path $root 'Tests/Nazor.VisualStudio.CodeLens.Tests/Nazor.VisualStudio.CodeLens.Tests.csproj'
    $log = & $msbuild $tests -restore -nologo -nodeReuse:false -p:Configuration=Release -v:m 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { Write-Host $log; throw 'The CodeLens SDK test build failed.' }
    $vsix = Join-Path $root 'Sources/Tools/Nazor.VisualStudio/bin/Release/Nazor.VisualStudio.vsix'
    New-Item -ItemType Directory "$Output/visualstudio" | Out-Null
    Copy-Item $vsix "$Output/visualstudio/"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $version = (Select-Xml -LiteralPath "$root/Directory.Build.props" -XPath '//NazorVersion').Node.InnerText
    $projectTemplatePackage = "ProjectTemplates/Nazor.Templates.$version.nupkg"
    $zip = [IO.Compression.ZipFile]::OpenRead("$Output/visualstudio/Nazor.VisualStudio.vsix")
    try {
        $entries = $zip.Entries.FullName
        foreach ($required in 'extension.vsixmanifest', 'manifest.json', 'catalog.json', 'nazor.png', 'LICENSE.txt', 'Nazor.pkgdef', 'Nazor.VisualStudio.pkgdef', 'Nazor.VisualStudio.dll', 'Nazor.VisualStudio.CodeLens.dll', 'NazorCompletionColors.imagemanifest', 'NazorIcons.imagemanifest',
            'Grammars/nazor.tmLanguage.json', 'Grammars/nss.tmLanguage.json',
            'Grammars/nazor.language-configuration.json', 'Grammars/nss.language-configuration.json',
            'Grammars/nazor.tmLanguage.tmTheme', 'Grammars/nss.tmLanguage.tmTheme',
            'Grammars/csharp.tmLanguage.json', 'Grammars/csharp.tmLanguage.LICENSE.txt',
            'ItemTemplates/CSharp/Nazor/NazorComponent/Component.vstemplate', 'ItemTemplates/CSharp/Nazor/NazorComponent/Component.nazor', 'ItemTemplates/CSharp/Nazor/NazorComponent/Component.png',
            'ItemTemplates/CSharp/Nazor/NazorStyleSheet/StyleSheet.vstemplate', 'ItemTemplates/CSharp/Nazor/NazorStyleSheet/StyleSheet.nss', 'ItemTemplates/CSharp/Nazor/NazorStyleSheet/StyleSheet.png',
            $projectTemplatePackage,
            'Server/Nazor.LanguageServer.dll', 'Server/Nazor.LanguageServer.runtimeconfig.json', 'Server/Microsoft.CodeAnalysis.Razor.Compiler.dll', 'Server/Nazor.Controls.dll') {
            Assert ($entries -contains $required) "the VSIX contains $required"
        }
        $shipped = $entries | Where-Object { $_ -notlike 'Server/*' -and $_ -like '*.dll' }
        Assert (-not ($shipped | Where-Object { $_ -notin 'Nazor.VisualStudio.dll', 'Nazor.VisualStudio.CodeLens.dll' })) 'the VSIX ships no Visual Studio assembly'
        $pkgdef = [IO.StreamReader]::new($zip.GetEntry('Nazor.pkgdef').Open()).ReadToEnd()
        $hotReloadPkgdef = [IO.StreamReader]::new($zip.GetEntry('Nazor.VisualStudio.pkgdef').Open()).ReadToEnd()
        Assert ($hotReloadPkgdef -match 'NazorHotReloadPackage' -and $hotReloadPkgdef -match 'AutoLoadPackages') 'the VSIX starts Hot Reload integration when a solution opens'
        Assert ($hotReloadPkgdef -match '"CodeBase"=.*Nazor\.VisualStudio\.dll') 'the Hot Reload package resolves its assembly from the extension folder'
        Assert ($hotReloadPkgdef -match '\\BindingPaths\\\{44c56c55-b3eb-48ef-b102-f37fd96e434d\}\][\r\n]+"\$PackageFolder\$"=""') 'the image service can resolve the icon assembly before the package loads'
        Assert ($pkgdef -match '\\TextMate\\Repositories\]' -and $pkgdef -match '"text.nazor"' -and $pkgdef -match '"source.nss"') 'the pkgdef registers the grammars and their language configurations'
        Assert ($pkgdef -match '\\Editors\\\{8B382828-6202-11D1-8870-0000F87579D2\}\\Extensions\]' -and $pkgdef -notmatch '3B902123') 'the pkgdef opens .nazor and .nss in the code editor'
        Assert ($pkgdef -match 'ShellFileAssociations\\\.nazor\][\r\n]+"DefaultIconMoniker"="df813fe5-43b7-40ab-9ba5-4ac6892fdbb6:2"' -and
            $pkgdef -match 'ShellFileAssociations\\\.nss\][\r\n]+"DefaultIconMoniker"="df813fe5-43b7-40ab-9ba5-4ac6892fdbb6:3"') 'component and stylesheet file associations use the packaged Nazor monikers'
        $manifest = [IO.StreamReader]::new($zip.GetEntry('extension.vsixmanifest').Open()).ReadToEnd()
        $metadata = ([xml]$manifest).PackageManifest.Metadata
        Assert ($metadata.Description.InnerText.Contains($version) -and
            [Uri]::IsWellFormedUriString($metadata.MoreInfo, [UriKind]::Absolute) -and
            $entries -contains $metadata.ReleaseNotes -and $entries -contains $metadata.PreviewImage) 'the VSIX advertises the current release and includes its preview image and release notes'
        Assert ($manifest -match 'Microsoft\.VisualStudio\.ItemTemplate') 'the VSIX registers the Nazor component item template'
        Assert ($manifest -match 'Type="Microsoft\.VisualStudio\.VsPackage"[^>]*Path="Nazor\.pkgdef"' -and
            $pkgdef -match '\\TemplateEngine\\Templates\\Nazor\.Templates\][\r\n]+"InstalledPath"="\$PackageFolder\$\\ProjectTemplates"') 'the VSIX registers the bundled .NET project template'
        $projectTemplates = [IO.Compression.ZipArchive]::new($zip.GetEntry($projectTemplatePackage).Open())
        try {
            $templateRoot = 'content/MyNazorApp'
            $template = [IO.StreamReader]::new($projectTemplates.GetEntry("$templateRoot/.template.config/template.json").Open()).ReadToEnd() | ConvertFrom-Json
            $hostOptions = [IO.StreamReader]::new($projectTemplates.GetEntry("$templateRoot/.template.config/ide.host.json").Open()).ReadToEnd() | ConvertFrom-Json
            [xml]$templateProject = [IO.StreamReader]::new($projectTemplates.GetEntry("$templateRoot/MyNazorApp.csproj").Open()).ReadToEnd()
            Assert ($template.shortName -eq 'nazor-app' -and $templateProject.Project.Sdk -eq "Nazor.Sdk/$version") 'the bundled template uses the CLI template and current Nazor SDK'
            Assert ($template.defaultName -eq 'NazorApp1' -and $template.symbols.Framework.defaultValue -eq 'net10.0') 'the project template suggests NazorApp1 and defaults to .NET 10'
            Assert ($hostOptions.symbolInfo.id -notcontains 'Framework') 'Visual Studio uses its own framework selector'
            $visible = @($hostOptions.symbolInfo | Where-Object isVisible | ForEach-Object id)
            Assert ($visible -contains 'Theme' -and $visible -contains 'ApplicationId' -and $visible -contains 'IncludeSamplePages') 'Visual Studio exposes the template theme, identity and sample-page options'
            Assert ($visible -contains 'IncludeDevTools' -and $template.symbols.IncludeDevTools.defaultValue -ceq 'false') 'Visual Studio exposes the opt-in DevTools option'
            Assert ($hostOptions.icon -eq 'icon.png' -and $projectTemplates.GetEntry("$templateRoot/.template.config/icon.png")) 'the project template includes its Nazor icon'
            Assert ($null -ne $projectTemplates.GetEntry("$templateRoot/SamplePages/Pages/CounterPage.nazor")) 'the bundled template includes the optional routed pages'
        }
        finally { $projectTemplates.Dispose() }
        Assert ($manifest -match 'Microsoft\.VisualStudio\.ImageManifest') 'the VSIX registers the completion color catalog'
        Assert ($manifest -match 'Type="Microsoft.VisualStudio.ImageManifest"[^>]*Path="NazorIcons.imagemanifest"') 'the VSIX registers the Nazor icon catalog'
        [xml]$componentTemplate = [IO.StreamReader]::new($zip.GetEntry('ItemTemplates/CSharp/Nazor/NazorComponent/Component.vstemplate').Open()).ReadToEnd()
        Assert ($componentTemplate.VSTemplate.TemplateData.Icon -eq 'Component.png') 'the component template uses its Nazor icon'
        [xml]$styleTemplate = [IO.StreamReader]::new($zip.GetEntry('ItemTemplates/CSharp/Nazor/NazorStyleSheet/StyleSheet.vstemplate').Open()).ReadToEnd()
        Assert ($styleTemplate.VSTemplate.TemplateData.Name -eq 'Nazor Style Sheet' -and $styleTemplate.VSTemplate.TemplateData.Icon -eq 'StyleSheet.png' -and
            $styleTemplate.VSTemplate.TemplateData.DefaultName -eq 'StyleSheet.nss') 'the NSS template has its name, extension and distinct icon'
        $styleText = [IO.StreamReader]::new($zip.GetEntry('ItemTemplates/CSharp/Nazor/NazorStyleSheet/StyleSheet.nss').Open()).ReadToEnd()
        Assert ($styleText -match '^:root\s*\{\s*/\*[^*]+\*/\s*\}\s*$') 'the NSS template starts on its first line with a root rule and a short guidance comment'
        [xml]$colorCatalog = Get-Content (Join-Path $completionImages 'NazorCompletionColors.imagemanifest') -Raw
        Assert ($colorCatalog.ImageManifest.Images.Image.Count -gt 100) 'the named-color images are embedded in the VSIX assembly and rendered by ImageLibrary'
        Assert ($manifest -match 'Microsoft\.VisualStudio\.MefComponent' -and $manifest -match 'Nazor\.VisualStudio\.dll') 'the manifest declares the language client as a MEF component'
        Assert ($manifest -match 'Microsoft\.VisualStudio\.CodeLensComponent' -and $manifest -match 'Nazor\.VisualStudio\.CodeLens\.dll') 'the manifest declares the out-of-process CodeLens provider'
        $packagedImages = Join-Path $completionImages 'Nazor.VisualStudio.dll'
        [IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry('Nazor.VisualStudio.dll'), $packagedImages, $true)
        [IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry('NazorCompletionColors.imagemanifest'), (Join-Path $completionImages 'NazorCompletionColors.imagemanifest'), $true)
        [IO.Compression.ZipFileExtensions]::ExtractToFile($zip.GetEntry('NazorIcons.imagemanifest'), (Join-Path $completionImages 'NazorIcons.imagemanifest'), $true)
    }
    finally { $zip.Dispose() }
    $ideDirectory = & $vswhere -latest -prerelease -property installationPath
    $editorAssembly = Join-Path $ideDirectory 'Common7/IDE/CommonExtensions/Microsoft/Editor/Microsoft.VisualStudio.Platform.VSEditor.dll'
    & (Join-Path $root 'Tests/Nazor.VisualStudio.CodeLens.Tests/bin/Release/Nazor.VisualStudio.CodeLens.Tests.exe') --icon-startup $completionImages
    if ($LASTEXITCODE -ne 0) { throw 'The icon assembly startup check failed.' }
    & (Join-Path $root 'Tests/Nazor.VisualStudio.CodeLens.Tests/bin/Release/Nazor.VisualStudio.CodeLens.Tests.exe') --roslyn-startup $packagedImages (Join-Path $ideDirectory 'Common7/IDE')
    if ($LASTEXITCODE -ne 0) { throw 'The native Roslyn cold-start check failed.' }
    & (Join-Path $root 'Tests/Nazor.VisualStudio.CodeLens.Tests/bin/Release/Nazor.VisualStudio.CodeLens.Tests.exe') $completionImages $packagedImages $editorAssembly
    if ($LASTEXITCODE -ne 0) { throw 'The CodeLens or completion image SDK tests failed.' }
    & dotnet run --project (Join-Path $root 'Tests/Nazor.VisualStudio.Protocol.Tests') -c Release -p:UseSharedCompilation=false -- (Join-Path $ideDirectory 'Common7/IDE')
    if ($LASTEXITCODE -ne 0) { throw 'The native completion serialization test failed.' }
}

Write-Host '==> TextMate bundle for %userprofile%\.vs\Extensions'
$bundle = Join-Path $Output 'visualstudio-folder/Nazor/Syntaxes'
New-Item -ItemType Directory $bundle | Out-Null
Copy-Item "$grammars/*.tmLanguage.json", "$grammars/*.tmTheme", "$grammars/*.txt" $bundle
Copy-Item "$root/LICENSE" (Split-Path -Parent $bundle)
Assert ((Get-ChildItem $bundle -Filter *.tmLanguage.json).Count -eq 3) 'the bundle contains the Nazor, NSS and C# grammars and the Visual Studio themes'

Write-Host '==> Visual Studio Code extension'
$extension = Join-Path $Output 'vscode/nazor'
$sources = Join-Path $root 'Sources/Tools/Nazor.VSCode'
New-Item -ItemType Directory "$extension/syntaxes" | Out-Null
Copy-Item "$sources/package.json", "$sources/package-lock.json", "$sources/extension.js", "$sources/devtools.js", "$sources/README.md" $extension
Copy-Item "$root/LICENSE" $extension
Copy-Item (Join-Path $root 'Docs/Assets/nazor.png') $extension
Copy-Item "$grammars/nazor.*.json", "$grammars/nss.*.json" "$extension/syntaxes"
Copy-Item "$sources/icons" "$extension/icons" -Recurse
Copy-Item $server "$extension/server" -Recurse
$manifest = Get-Content "$extension/package.json" -Raw | ConvertFrom-Json
$fileIcons = foreach ($language in $manifest.contributes.languages) { $language.icon.light; $language.icon.dark }
Assert (@($fileIcons | Where-Object { $_ } | Sort-Object -Unique).Count -eq 4) 'both languages declare distinct light and dark icons'
foreach ($path in @($manifest.contributes.grammars.path) + @($manifest.contributes.languages.configuration) + $fileIcons + @($manifest.main) + @($manifest.icon)) {
    Assert (Test-Path (Join-Path $extension $path)) "package.json points to an existing file ($path)"
}
Assert (Test-Path "$extension/server/Nazor.LanguageServer.dll") 'the extension contains the language server'
Assert (Test-Path "$extension/server/Nazor.Controls.dll") 'the extension contains the controls used for authoring metadata'
if (Get-Command npm -ErrorAction SilentlyContinue) {
    Push-Location $extension
    try {
        & npm ci --omit=dev --no-audit --no-fund --loglevel=error
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed for the VS Code extension.' }
    }
    finally { Pop-Location }
    Assert (Test-Path "$extension/node_modules/vscode-languageclient/package.json") 'the extension contains vscode-languageclient'
    & node --check "$extension/extension.js"
    Assert ($LASTEXITCODE -eq 0) 'extension.js is valid JavaScript'
    & node (Join-Path $root 'Tests/Nazor.Tooling.Tests/NssReferencesCommand.cjs') "$extension/extension.js"
    Assert ($LASTEXITCODE -eq 0) 'VS Code converts NSS source references for native navigation'
    & node (Join-Path $root 'Tests/Nazor.Tooling.Tests/DevToolsDebugger.cjs') "$extension/devtools.js"
    Assert ($LASTEXITCODE -eq 0) 'VS Code DevTools validates raw reads and debugger stop lifetime'
}
else {
    Write-Host '    skipped: npm was not found, the extension has no language client (run npm ci in it)' -ForegroundColor Yellow
}

Write-Host "Editor support built in $Output." -ForegroundColor Green
