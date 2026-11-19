using System.Reflection;
using System.Collections;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Nazor.Compiler;

internal static class RazorSdkAdapter
{
    private static readonly MethodInfo CSharpDocumentMethod = typeof(RazorCodeDocument).GetMethod("GetCSharpDocument", InstanceMembers)!;

    public static RazorCSharpDocument GetCSharp(RazorCodeDocument document) =>
        (RazorCSharpDocument)CSharpDocumentMethod.Invoke(document, [false])!;

    private static readonly PropertyInfo SourceMappingsProperty = typeof(RazorCSharpDocument).GetProperty("SourceMappingsSortedByOriginal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new NazorCompilationException("Unsupported Razor SDK: source mapping API changed.");

    public static IEnumerable<(string? FilePath, int OriginalStart, int OriginalLength, int GeneratedStart, int GeneratedLength)> GetSourceMappings(RazorCSharpDocument document)
    {
        foreach (var mapping in (IEnumerable)SourceMappingsProperty.GetValue(document)!)
        {
            var original = mapping.GetType().GetProperty("OriginalSpan", InstanceMembers)!.GetValue(mapping)!;
            var generated = mapping.GetType().GetProperty("GeneratedSpan", InstanceMembers)!.GetValue(mapping)!;
            yield return ((string?)Span(original, "FilePath"), (int)Span(original, "AbsoluteIndex")!, (int)Span(original, "Length")!,
                (int)Span(generated, "AbsoluteIndex")!, (int)Span(generated, "Length")!);
        }

        static object? Span(object span, string property) => span.GetType().GetProperty(property, InstanceMembers)!.GetValue(span);
    }

    private static readonly RazorParserOptions StyleParserOptions = CreateStyleParserOptions();
    private const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly Type SyntaxNodeType = typeof(RazorSyntaxTree).Assembly.GetType("Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode", throwOnError: true)!;
    private static readonly PropertyInfo RootProperty = typeof(RazorSyntaxTree).GetProperty("Root", InstanceMembers)!;
    private static readonly PropertyInfo PositionProperty = SyntaxNodeType.GetProperty("Position", InstanceMembers)!;
    private static readonly PropertyInfo EndProperty = SyntaxNodeType.GetProperty("EndPosition", InstanceMembers)!;
    private static readonly MethodInfo ChildrenMethod = SyntaxNodeType.GetMethod("ChildNodes", InstanceMembers)!;

    internal static IReadOnlyList<(string Name, SourceSpan Span)> AttributeNames(RazorCodeDocument document)
    {
        var tree = (RazorSyntaxTree)typeof(RazorCodeDocument).GetMethod("GetSyntaxTree", InstanceMembers)!.Invoke(document, null)!;
        var result = new List<(string, SourceSpan)>();
        Visit(RootProperty.GetValue(tree)!);
        return result;

        void Visit(object node)
        {
            if (node.GetType().Name is "MarkupAttributeBlockSyntax" or "MarkupMinimizedAttributeBlockSyntax" or
                "MarkupTagHelperAttributeSyntax" or "MarkupMinimizedTagHelperAttributeSyntax")
            {
                var name = node.GetType().GetProperty("Name", InstanceMembers)!.GetValue(node)!;
                var start = (int)PositionProperty.GetValue(name)!;
                var end = (int)EndProperty.GetValue(name)!;
                var position = document.Source.Text.Lines.GetLinePosition(start);
                result.Add((document.Source.Text.ToString(new Microsoft.CodeAnalysis.Text.TextSpan(start, end - start)),
                    new SourceSpan(document.Source.FilePath, start, position.Line, position.Character, end - start)));
            }
            foreach (var child in (IEnumerable)ChildrenMethod.Invoke(node, null)!) Visit(child);
        }
    }

    internal static IReadOnlyDictionary<int, int> MarkupTagSpans(string text, string path)
    {
        var tree = RazorSyntaxTree.Parse(RazorSourceDocument.Create(text, path), StyleParserOptions, default);
        var result = new Dictionary<int, int>();
        Visit(RootProperty.GetValue(tree)!);
        return result;

        void Visit(object node)
        {
            if (node.GetType().Name is "MarkupStartTagSyntax" or "MarkupEndTagSyntax" or "MarkupTagHelperStartTagSyntax" or "MarkupTagHelperEndTagSyntax")
                result[(int)PositionProperty.GetValue(node)!] = (int)EndProperty.GetValue(node)!;
            foreach (var child in (IEnumerable)ChildrenMethod.Invoke(node, null)!) Visit(child);
        }
    }

    private static RazorParserOptions CreateStyleParserOptions()
    {
        var engine = RazorProjectEngine.Create(RazorConfiguration.Default, RazorProjectFileSystem.Create("/"));
        var defaults = engine.CreateCodeDocument(RazorSourceDocument.Create("", "probe.nazor"), RazorFileKind.Component).ParserOptions;
        return RazorParserOptions.Create(defaults.LanguageVersion, RazorFileKind.Component, builder =>
        {
            builder.Directives = defaults.Directives.Add(DirectiveDescriptor.CreateDirective("style", DirectiveKind.SingleLine))
                .Add(DirectiveDescriptor.CreateDirective("window", DirectiveKind.SingleLine));
            builder.UseRoslynTokenizer = true;
        });
    }

    public static StyleDirectiveContext ClassifyStyle(string prefix, string rest, string path, int offset)
        => ClassifyDirective(prefix, rest, path, offset);

    public static StyleDirectiveContext ClassifyDirective(string prefix, string rest, string path, int offset)
    {
        var tree = RazorSyntaxTree.Parse(RazorSourceDocument.Create(prefix + "\n" + rest, path), StyleParserOptions, default);
        var node = RootProperty.GetValue(tree)!;
        var ancestors = new List<string>();
        while (true)
        {
            var kind = node.GetType().Name;
            if (kind == "RazorDirectiveSyntax" && (int)PositionProperty.GetValue(node)! == offset)
            {
                if (ancestors.Any(a => a.Contains("Comment", StringComparison.Ordinal) || a.Contains("Attribute", StringComparison.Ordinal) ||
                    a.EndsWith("TagSyntax", StringComparison.Ordinal))) return StyleDirectiveContext.None;
                return ancestors.SequenceEqual(new[] { "RazorDocumentSyntax", "MarkupBlockSyntax", "CSharpCodeBlockSyntax" })
                    ? StyleDirectiveContext.TopLevel : StyleDirectiveContext.Nested;
            }
            ancestors.Add(kind);
            var children = ((IEnumerable)ChildrenMethod.Invoke(node, null)!).Cast<object>();
            var next = children.FirstOrDefault(child => (int)PositionProperty.GetValue(child)! <= offset && (int)EndProperty.GetValue(child)! > offset);
            if (next is null) return StyleDirectiveContext.None;
            node = next;
        }
    }
}

internal enum StyleDirectiveContext { None, TopLevel, Nested }
