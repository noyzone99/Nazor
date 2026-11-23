using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nazor.Nss;
using Nazor.Style;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Documentation <repository-root> <package-feed>");
    return 2;
}

var root = Path.GetFullPath(args[0]);
var feed = Path.GetFullPath(args[1]);
var version = XDocument.Load(Path.Combine(root, "Directory.Build.props")).Descendants("NazorVersion").Single().Value;
var themeVersion = Regex.IsMatch(version, @"^[^-+]+-alpha(?:[.+]|$)") ? version : version.Split('-', '+')[0] + "-alpha";
var errors = new List<string>();
string Read(string relativePath) => File.ReadAllText(Path.Combine(root, relativePath));
void Check(bool condition, string message)
{
    if (!condition) errors.Add(message);
}

var index = Read("Docs/API.md");
var pages = Regex.Matches(index, @"(?m)^\| `(?<id>Nazor\.[\w.]+)` \| .*?\((?<file>ApiReferences/[^)]+)\)")
    .ToDictionary(match => match.Groups["id"].Value, match => match.Groups["file"].Value);
var binaries = new List<(string Package, string Framework, string Name, PortableExecutableReference Reference)>();
var packageIds = new HashSet<string>(StringComparer.Ordinal);
foreach (var file in Directory.GetFiles(feed, "Nazor.*.nupkg"))
{
    using var archive = ZipFile.OpenRead(file);
    using var manifestStream = archive.Entries.Single(entry => entry.Name.EndsWith(".nuspec", StringComparison.Ordinal)).Open();
    var manifest = XDocument.Load(manifestStream).Descendants().Single(element => element.Name.LocalName == "metadata");
    string PackageValue(string name) => manifest.Elements().Single(element => element.Name.LocalName == name).Value;
    var package = PackageValue("id");
    Check(packageIds.Add(package), $"{package}: more than one archive in the release feed.");
    var packageVersion = package.StartsWith("Nazor.Themes.", StringComparison.Ordinal) ? themeVersion : version;
    Check(PackageValue("version") == packageVersion, $"{file}: version must match {packageVersion}.");
    Check(PackageValue("license") == "LICENSE" && archive.GetEntry("LICENSE") is not null, $"{package}: missing project license.");
    Check(PackageValue("readme") == "NuGet.md" && archive.GetEntry("NuGet.md") is not null, $"{package}: missing NuGet readme.");
    Check(!archive.Entries.Any(entry => entry.Name is "Microsoft.InteractiveExperiences.Projection.dll" or "Microsoft.Windows.SDK.NET.dll" or "WinRT.Runtime.dll" or "Microsoft.UI.Windowing.dll" or "CoreMessagingXP.dll"),
        $"{package}: Microsoft Windows dependencies must come from the original feed at application restore.");
    if (package == "Nazor.Sdk")
    {
        using var sdkVersion = archive.GetEntry("Sdk/Nazor.Sdk.Version.props")!.Open();
        Check(XDocument.Load(sdkVersion).Descendants("NazorSdkVersion").Single().Value == version, "Nazor.Sdk: inconsistent SDK version metadata.");
    }
    if (package == "Nazor.Templates")
    {
        using var template = archive.Entries.Single(entry => entry.Name == "MyNazorApp.csproj").Open();
        var project = XDocument.Load(template);
        Check((string?)project.Root?.Attribute("Sdk") == $"Nazor.Sdk/{version}", "Nazor.Templates: SDK version does not match the release.");
        foreach (var themeReference in project.Descendants("PackageReference").Where(element =>
            ((string?)element.Attribute("Include"))?.StartsWith("Nazor.Themes.", StringComparison.Ordinal) is true))
            Check((string?)themeReference.Attribute("Version") == themeVersion, $"Nazor.Templates: {themeReference.Attribute("Include")?.Value} must use {themeVersion}.");
    }
    foreach (var entry in archive.Entries.Where(entry => Regex.IsMatch(entry.FullName, @"^lib/[^/]+/[^/]+\.dll$")))
    {
        using var source = entry.Open();
        using var image = new MemoryStream();
        source.CopyTo(image);
        binaries.Add((package, entry.FullName.Split('/')[1], entry.Name, MetadataReference.CreateFromImage(image.ToArray())));
    }
}
Check(packageIds.Contains("Nazor.Sdk") && packageIds.Contains("Nazor.Templates"), "The release feed must contain the SDK and template packages.");

var mainAssemblies = binaries.Where(binary => binary.Name == binary.Package + ".dll" ||
    binary.Name == "Nazor.Hosting.Desktop.dll" && binary.Package.StartsWith("Nazor.Hosting.Desktop.", StringComparison.Ordinal)).ToArray();
foreach (var package in mainAssemblies.Select(binary => binary.Package).Distinct())
    Check(pages.ContainsKey(package), $"{package}: missing from Docs/API.md.");
var runtimeFrameworks = XDocument.Load(Path.Combine(root, "Sources/Directory.Build.props"))
    .Descendants("TargetFrameworks").Single().Value.Split(';');
foreach (var package in pages.Keys)
{
    var expected = package == "Nazor.DevTools.Protocol" ? runtimeFrameworks.Append("netstandard2.0") : runtimeFrameworks;
    foreach (var framework in expected)
        Check(mainAssemblies.Any(binary => binary.Package == package && binary.Framework == framework),
            $"{package}: no {framework} assembly in the release feed.");
}

var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Where(path => !Path.GetFileName(path).StartsWith("Nazor.", StringComparison.Ordinal))
    .Select(path => MetadataReference.CreateFromFile(path)).ToArray();
var surface = new Dictionary<(string Package, string Framework), HashSet<string>>();
foreach (var binary in mainAssemblies)
{
    if (!pages.TryGetValue(binary.Package, out var page)) continue;
    var dependencies = binaries.Where(item => item.Framework == binary.Framework && item.Name != binary.Name)
        .DistinctBy(item => item.Name).Select(item => item.Reference);
    var compilation = CSharpCompilation.Create("DocumentationCheck", references: platform.Concat(dependencies).Append(binary.Reference),
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, metadataImportOptions: MetadataImportOptions.Public));
    var assembly = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(binary.Reference)!;
    var documented = ApiSignatures.Read(Read("Docs/" + page));
    var signatures = new HashSet<string>(StringComparer.Ordinal);
    foreach (var (type, signature) in ApiSignatures.From(assembly.GlobalNamespace))
    {
        var normalized = ApiSignatures.Normalize(signature);
        signatures.Add(type + " " + normalized);
        Check(documented.TryGetValue(type, out var members) && members.Contains(normalized),
            $"{page} ({binary.Framework}): missing `{signature}` under {type}.");
    }
    surface.Add((binary.Package, binary.Framework), signatures);
}

foreach (var ((package, framework), signatures) in surface.Where(item => item.Key.Package.StartsWith("Nazor.Hosting.Desktop.", StringComparison.Ordinal)))
    Check(surface.TryGetValue(("Nazor.Hosting.Desktop", framework), out var shared) && signatures.SetEquals(shared),
        $"{package} ({framework}): shared desktop reference no longer describes this variant.");

var reference = Read("Docs/Guide/19-NSS-Property-Reference.md");
var propertyRows = reference.Split('\n').Where(line => Regex.IsMatch(line, @"^\| `[-\w]+` \|"))
    .Select(line => Regex.Split(line.Trim(), @"(?<!\\)\|").Select(cell => cell.Trim()).ToArray())
    .ToLookup(cells => cells[1].Trim('`'));
foreach (var property in StyleProperties.All)
{
    var rows = propertyRows[property.Name].ToArray();
    Check(rows.Length == 1, $"NSS: expected one row for {property.Name}.");
    if (rows.Length != 1) continue;
    var cells = rows[0];
    Check(cells.Length == 7 && cells[4] == (property.Inherited ? "Yes" : "No") &&
        (property.Animatable ? cells[5] is "Yes" or "Insets only" : cells[5] == "No"),
        $"NSS: wrong inheritance or animation flag for {property.Name}.");
    if (cells.Length != 7) continue;
    var initial = Regex.Match(cells[3], "`([^`]+)`");
    if (initial.Success && initial.Groups[1].Value != "initial")
    {
        var result = NssCompiler.Compile("Default", $".example {{ {property.Name}: {initial.Groups[1].Value}; }}");
        var value = result.StyleSheet.Rules.SelectMany(rule => rule.Declarations).OfType<ValueDeclaration>().SingleOrDefault()?.Value;
        Check(result.Success && value is not null && (Equals(value, property.InitialValue) || value.ToString() == property.InitialValue.ToString()),
            $"NSS: initial value {initial.Value} differs from the registry for {property.Name}.");
    }
    if (!StyleProperties.AppliesTo(property, "Button", false))
    {
        var scope = NssCompiler.Compile("Scope", $"Button {{ {property.Name}: initial; }}");
        Check(scope.Diagnostics.Any(diagnostic => diagnostic.Id == "NZS112"), $"NSS: {property.Name} must warn outside the window root.");
    }
}
foreach (var shorthand in StyleProperties.AllShorthands)
{
    var rows = propertyRows[shorthand.Name].ToArray();
    Check(rows.Length == 1, $"NSS: expected one row for {shorthand.Name}.");
    if (rows.Length != 1) continue;
    var names = Regex.Matches(rows[0][3], "`([-\\w]+)`").Select(match => match.Groups[1].Value);
    Check(names.SequenceEqual(shorthand.Longhands.Select(property => property.Name)), $"NSS: wrong expansion for {shorthand.Name}.");
}
Check(propertyRows.Count == StyleProperties.All.Count + StyleProperties.AllShorthands.Count, "NSS: remove unregistered property rows.");

var files = Directory.GetFiles(Path.Combine(root, "Docs"), "*.md", SearchOption.AllDirectories)
    .Prepend(Path.Combine(root, "README.md")).ToArray();
foreach (var file in files)
{
    var text = File.ReadAllText(file);
    foreach (Match match in Regex.Matches(text, @"(?:Nazor\.Sdk/|Nazor\.Templates[.@]|`)(?<version>\d+\.\d+\.\d+-(?:alpha|beta|preview|rc)(?:\.\d+)?)"))
        Check(match.Groups["version"].Value == version, $"{file}: Nazor version must match {version}.");
    foreach (Match block in Regex.Matches(text, @"(?ms)^```css\s*\r?\n(.*?)^```"))
    {
        var result = NssCompiler.Compile("Documentation", block.Groups[1].Value, file);
        foreach (var diagnostic in result.Diagnostics.Where(diagnostic => diagnostic.IsError)) errors.Add(diagnostic.ToString());
    }
}

MarkdownLinks.Check(root, files, errors);
foreach (var error in errors) Console.Error.WriteLine(error);
Console.WriteLine($"Checked {surface.Count} package/TFM APIs, {propertyRows.Count} NSS property rows, and links in {files.Length} Markdown files.");
return errors.Count == 0 ? 0 : 1;
