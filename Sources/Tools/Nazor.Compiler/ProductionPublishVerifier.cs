using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace Nazor.Compiler;

internal static class ProductionPublishVerifier
{
    internal static int Run(string[] arguments)
    {
        if (arguments.Length is < 2 or > 3)
        {
            Console.Error.WriteLine("error NZB006: Expected assets file, publish inputs and optional output directory.");
            return 1;
        }
        try
        {
            CheckDependencyFile(arguments[0]);
            var inputs = File.ReadAllLines(arguments[1]);
            if (inputs.Length == 0) throw new InvalidDataException("The production publish input list is empty.");
            foreach (var line in inputs)
            {
                var separator = line.IndexOf('|');
                if (separator <= 0 || separator == line.Length - 1) throw new InvalidDataException("Invalid production publish input.");
                CheckName(line[(separator + 1)..]);
                CheckFile(line[..separator]);
            }
            if (arguments.Length == 3)
            {
                if (!Directory.Exists(arguments[2])) throw new DirectoryNotFoundException("The production publish directory does not exist.");
                foreach (var path in Directory.EnumerateFiles(arguments[2], "*", SearchOption.AllDirectories))
                {
                    CheckName(Path.GetRelativePath(arguments[2], path));
                    CheckFile(path);
                }
            }
            Console.WriteLine("nazorc: production dependency and payload checks passed.");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or BadImageFormatException)
        {
            Console.Error.WriteLine($"error NZB006: Production publication rejected: {exception.Message}");
            return 1;
        }
    }

    private static bool Forbidden(string name) => name.Equals("Nazor.Diagnostics", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("Nazor.Diagnostics.", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Nazor.DevTools", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("Nazor.DevTools.", StringComparison.OrdinalIgnoreCase);

    private static void CheckName(string name)
    {
        if (name.Split('/', '\\').Any(Forbidden)) throw new InvalidDataException($"Forbidden development payload '{name}'.");
    }

    private static void CheckDependencyFile(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("libraries", out var libraries) || libraries.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Missing dependency libraries in '{path}'.");
        foreach (var library in libraries.EnumerateObject())
            if (Forbidden(library.Name.Split('/')[0])) throw new InvalidDataException($"Forbidden resolved dependency '{library.Name}' in '{path}'.");
    }

    private static void CheckFile(string path)
    {
        CheckName(Path.GetFileName(path));
        if (!File.Exists(path)) throw new FileNotFoundException("A production payload is missing.", path);
        if (path.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)) CheckDependencyFile(path);
        var extension = Path.GetExtension(path);
        if (!extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)) return;
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        if (!pe.HasMetadata) return;
        var metadata = pe.GetMetadataReader();
        if (metadata.IsAssembly && Forbidden(metadata.GetString(metadata.GetAssemblyDefinition().Name)))
            throw new InvalidDataException($"Forbidden assembly identity in '{path}'.");
        foreach (var reference in metadata.AssemblyReferences)
        {
            var name = metadata.GetString(metadata.GetAssemblyReference(reference).Name);
            if (Forbidden(name)) throw new InvalidDataException($"Forbidden assembly reference '{name}' in '{path}'. Rebuild with DevTools disabled.");
        }
    }
}
