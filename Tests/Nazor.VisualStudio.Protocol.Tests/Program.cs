using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Nazor.VisualStudio.Protocol.Tests;

internal static class Program
{
    private static void Main(string[] args)
    {
        var ide = Path.GetFullPath(args[0]);
        var host = new AssemblyLoadContext("Installed Visual Studio");
        host.Resolving += (_, name) =>
        {
            if (name.Name!.EndsWith(".resources", StringComparison.Ordinal)) return null;
            foreach (var path in Directory.EnumerateFiles(ide, name.Name + ".dll", SearchOption.AllDirectories)
                .OrderBy(path => AssemblyName.GetAssemblyName(path).FullName == name.FullName ? 0 : 1)
                .ThenBy(path => string.Equals(Path.GetDirectoryName(path), Path.Combine(ide, "PublicAssemblies"), StringComparison.OrdinalIgnoreCase) ? 0 : 1))
            {
                var candidate = AssemblyName.GetAssemblyName(path);
                if (candidate.Name == name.Name && candidate.CultureName == name.CultureName &&
                    candidate.GetPublicKeyToken()!.SequenceEqual(name.GetPublicKeyToken()!) &&
                    candidate.Version!.Major == name.Version!.Major && candidate.Version.Minor == name.Version.Minor &&
                    candidate.Version.Build == name.Version.Build && candidate.Version >= name.Version)
                    return host.LoadFromAssemblyPath(path);
            }
            return null;
        };
        var directory = Path.Combine(ide, "CommonExtensions", "Microsoft", "LanguageServer");
        Assembly Load(string name) => host.LoadFromAssemblyPath(Path.Combine(directory, name + ".dll"));
        var protocol = Load("Microsoft.VisualStudio.LanguageServer.Protocol");
        var internalProtocol = Load("Microsoft.VisualStudio.LanguageServer.Protocol.Internal");
        var implementation = Load("Microsoft.VisualStudio.LanguageServer.Client.Implementation");
        var translate = implementation.GetType("Microsoft.VisualStudio.LanguageServer.Client.SemanticTokensTaggerBase", true)!
            .GetMethod("ClassificationTypeNameForTokenType", BindingFlags.NonPublic | BindingFlags.Static)!;
        if (!Equals("text", translate.Invoke(null, ["Nazor component parameter"])))
            throw new Exception("Recheck whether the installed VS client now supports custom semantic classifications");
        Console.WriteLine("PASS: native VS semantic classification requires the Nazor tagger for dedicated parameter colors.");
        var newtonsoft = host.LoadFromAssemblyName(new AssemblyName("Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed"));
        var tokenType = newtonsoft.GetType("Newtonsoft.Json.Linq.JToken", true)!;
        var serializerType = newtonsoft.GetType("Newtonsoft.Json.JsonSerializer", true)!;
        var extensions = Load("Microsoft.VisualStudio.LanguageServer.Protocol.Extensions");
        var nativeTags = extensions.GetType("Microsoft.VisualStudio.LanguageServer.Protocol.VSDiagnosticTags", true)!;
        foreach (var (name, expected) in new[] { ("HiddenInEditor", int.MaxValue - 6), ("HiddenInErrorList", int.MaxValue - 4),
                     ("SuppressEditorToolTip", int.MaxValue - 7), ("VisibleInErrorList", int.MaxValue - 5), ("IntellisenseError", int.MaxValue - 2) })
            if (Convert.ToInt32(nativeTags.GetField(name)!.GetValue(null)) != expected)
                throw new Exception("The installed VS diagnostic contract changed: " + name);
        var diagnosticJson = tokenType.GetMethod("Parse", [typeof(string)])!.Invoke(null, ["""
            { "range": { "start": { "line": 2, "character": 4 }, "end": { "line": 2, "character": 8 } },
              "severity": 4, "code": "IDE0022", "message": "Use block body for method",
              "tags": [2147483641,2147483643,2147483640,2147483645] }
            """])!;
        var diagnosticType = protocol.GetType("Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic", true)!;
        var nativeDiagnostic = tokenType.GetMethod("ToObject", [typeof(Type)])!.Invoke(diagnosticJson, [diagnosticType])!;
        var diagnosticTags = ((Array)diagnosticType.GetProperty("Tags")!.GetValue(nativeDiagnostic)!).Cast<object>().Select(Convert.ToInt32).ToArray();
        if (!diagnosticTags.SequenceEqual(new[] { int.MaxValue - 6, int.MaxValue - 4, int.MaxValue - 7, int.MaxValue - 2 }))
            throw new Exception("The native VS diagnostic converter lost silent diagnostic presentation tags");
        Console.WriteLine("PASS: installed VS diagnostic contracts preserve hidden editor/error-list/tooltip tags for silent C# diagnostics.");
        var token = tokenType.GetMethod("Parse", [typeof(string)])!.Invoke(null, ["""
            { "isIncomplete": false, "items": [{ "label": "blue", "kind": 16,
              "_vs_icon": { "_vs_type": "ImageElement", "ImageId": {
                "_vs_type": "ImageId", "Guid": "aba0adf0-a9f1-4328-a36a-9372c0081c59", "Id": 255 } } }] }
            """])!;
        var itemType = protocol.GetType("Microsoft.VisualStudio.LanguageServer.Protocol.CompletionItem", true)!;
        var listType = protocol.GetType("Microsoft.VisualStudio.LanguageServer.Protocol.CompletionList", true)!;
        var responseType = protocol.GetType("Microsoft.VisualStudio.LanguageServer.Protocol.SumType`2", true)!
            .MakeGenericType(itemType.MakeArrayType(), listType);
        object Item(object response)
        {
            var list = responseType.GetProperty("Value")!.GetValue(response)!;
            return ((Array)listType.GetProperty("Items")!.GetValue(list)!).GetValue(0)!;
        }
        var plain = tokenType.GetMethod("ToObject", [typeof(Type)])!.Invoke(token, [responseType])!;
        var dropped = Item(plain);
        if (dropped.GetType() != itemType) throw new Exception("Expected the middle-layer path to drop the extended item");
        var serializer = Activator.CreateInstance(serializerType)!;
        internalProtocol.GetType("Microsoft.VisualStudio.LanguageServer.Protocol.VSInternalExtensionUtilities", true)!
            .GetMethod("AddVSInternalExtensionConverters", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [serializer]);
        var direct = tokenType.GetMethod("ToObject", [typeof(Type), serializerType])!.Invoke(token, [responseType, serializer])!;
        var preserved = Item(direct);
        if (preserved.GetType().Name != "VSInternalCompletionItem") throw new Exception("Native VS converters lost the extended item");
        var uxType = implementation.GetType("Microsoft.VisualStudio.LanguageServer.Client.CompletionUxService", true)!;
        var ux = uxType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single().Invoke([null]);
        var getImage = uxType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(method =>
            method.Name == "GetCompletionImage" && method.GetParameters() is [{ ParameterType.Name: "CompletionItem" }]);
        object ImageId(object item)
        {
            var nativeType = getImage.GetParameters()[0].ParameterType;
            if (!nativeType.IsInstanceOfType(item))
            {
                var converted = Activator.CreateInstance(nativeType)!;
                var kind = nativeType.GetProperty("Kind")!;
                kind.SetValue(converted, Enum.ToObject(Nullable.GetUnderlyingType(kind.PropertyType) ?? kind.PropertyType,
                    Convert.ToInt32(itemType.GetProperty("Kind")!.GetValue(item))));
                nativeType.GetProperty("Icon")!.SetValue(converted, item.GetType().GetProperty("Icon")?.GetValue(item));
                item = converted;
            }
            var image = getImage.Invoke(ux, [item])!;
            return image.GetType().GetProperty("ImageId")!.GetValue(image)!;
        }
        var lostId = ImageId(dropped);
        var actualId = ImageId(preserved);
        var guid = actualId.GetType().GetField("Guid")!;
        var id = actualId.GetType().GetField("Id")!;
        if ((Guid)guid.GetValue(actualId)! != Guid.Parse("aba0adf0-a9f1-4328-a36a-9372c0081c59") || (int)id.GetValue(actualId)! != 255)
            throw new Exception("The native completion list did not select the CSS blue swatch");
        if ((Guid)guid.GetValue(lostId)! != Guid.Parse("ae27a6b0-e345-4288-96df-5eaf394ee369") || (int)id.GetValue(lostId)! != 539)
            throw new Exception("Expected the generic color palette glyph after middleware conversion");
        Console.WriteLine("PASS: installed VS reproduces the generic color glyph after middleware; its native converters and CompletionUxService preserve the CSS blue swatch.");
    }
}
