using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Nazor.Compiler;

public enum NazorDiagnosticSeverity
{
    Warning,
    Error,
}

public sealed record NazorDiagnostic(string Id, NazorDiagnosticSeverity Severity, string Path, int Line, int Column, string Message, int Length = 0)
{
    public bool IsError => Severity == NazorDiagnosticSeverity.Error;

    public override string ToString()
    {
        var severity = IsError ? "error" : "warning";
        return string.IsNullOrEmpty(Path) ? $"{severity} {Id}: {Message}" : $"{Path}({Line},{Column}): {severity} {Id}: {Message}";
    }

    internal static NazorDiagnostic FromRazor(RazorDiagnostic diagnostic, string fallbackPath)
    {
        var span = diagnostic.Span;
        var path = string.IsNullOrEmpty(span.FilePath) ? fallbackPath : span.FilePath;
        return new(diagnostic.Id, diagnostic.Severity == RazorDiagnosticSeverity.Error ? NazorDiagnosticSeverity.Error : NazorDiagnosticSeverity.Warning,
            path, span.LineIndex + 1, span.CharacterIndex + 1, diagnostic.GetMessage(), Math.Max(span.Length, 0));
    }

    internal static NazorDiagnostic FromRoslyn(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.GetMappedLineSpan();
        return new(diagnostic.Id, diagnostic.Severity == DiagnosticSeverity.Error ? NazorDiagnosticSeverity.Error : NazorDiagnosticSeverity.Warning,
            span.Path ?? "", span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, diagnostic.GetMessage(),
            span.StartLinePosition.Line == span.EndLinePosition.Line ? span.EndLinePosition.Character - span.StartLinePosition.Character : 0);
    }

    internal static NazorDiagnostic Error(string id, string message) => new(id, NazorDiagnosticSeverity.Error, "", 0, 0, message);
}

public sealed class NazorCompilationException(IReadOnlyList<NazorDiagnostic> diagnostics)
    : Exception(string.Join(Environment.NewLine, diagnostics))
{
    public NazorCompilationException(string message) : this([NazorDiagnostic.Error("NZC000", message)])
    {
    }

    public IReadOnlyList<NazorDiagnostic> Diagnostics { get; } = diagnostics;
}
