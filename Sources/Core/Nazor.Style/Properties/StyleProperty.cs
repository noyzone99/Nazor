using System.Text.RegularExpressions;
using Nazor.Style.Syntax;

namespace Nazor.Style;

/// <summary>What has to be redone when a computed value changes.</summary>
[Flags]
public enum StyleEffects
{
    /// <summary>Requires no layout, paint, or compositing invalidation.</summary>
    None = 0,

    /// <summary>Geometry of the element and possibly of its neighbours.</summary>
    Layout = 1,

    /// <summary>Redraw of the element without geometry changes.</summary>
    Paint = 2,

    /// <summary>Visual update that a renderer may composite without repainting (opacity).</summary>
    Composite = 4,
}

/// <summary>A longhand property: name, value grammar, initial value, inheritance and effects.</summary>
public abstract class StyleProperty
{
    private IReadOnlyList<string>? keywords;
    private IReadOnlyList<string>? valueTypes;

    private protected StyleProperty(int id, string name, bool inherited, StyleEffects effects, bool animatable, string syntax)
    {
        Id = id;
        Name = name;
        Inherited = inherited;
        Effects = effects;
        Animatable = animatable;
        Syntax = syntax;
    }

    /// <summary>Index in <see cref="StyleProperties.All" />.</summary>
    public int Id { get; }

    /// <summary>Gets the canonical NSS name of the longhand property.</summary>
    public string Name { get; }

    /// <summary>Inherited properties take the parent's computed value when not specified.</summary>
    public bool Inherited { get; }

    /// <summary>Gets the layout, paint, or compositing work invalidated when the value changes.</summary>
    public StyleEffects Effects { get; }

    /// <summary>Values can be interpolated by transitions.</summary>
    public bool Animatable { get; }

    /// <summary>
    /// The accepted values in CSS value definition syntax, for example <c>auto | &lt;length-percentage&gt;</c>.
    /// </summary>
    public string Syntax { get; }

    /// <summary>
    /// The keywords of <see cref="Syntax" /> (<c>auto</c>), besides <c>inherit</c>, <c>initial</c> and <c>unset</c>.
    /// </summary>
    public IReadOnlyList<string> Keywords => ValueSyntax.Keywords(Syntax, ref keywords);

    /// <summary>
    /// The value types of <see cref="Syntax" /> without brackets (<c>length-percentage</c>, <c>color</c>...).
    /// </summary>
    public IReadOnlyList<string> ValueTypes => ValueSyntax.Types(Syntax, ref valueTypes);

    /// <summary>Gets the property's initial value before cascading and inheritance.</summary>
    public abstract object InitialValue { get; }

    /// <summary>Gets the CLR type used to represent values of this property.</summary>
    public abstract Type ValueType { get; }

    /// <summary>Parses the complete value; throws <see cref="StyleValueException" /> on invalid input.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <returns>The resulting object value.</returns>
    public abstract object Parse(TokenReader reader);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Name;
}

/// <summary>Describes a typed NSS longhand property and its parser.</summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class StyleProperty<T> : StyleProperty where T : notnull
{
    private readonly Func<TokenReader, T> parser;

    internal StyleProperty(int id, string name, T initial, bool inherited, StyleEffects effects, bool animatable, string syntax, Func<TokenReader, T> parser)
        : base(id, name, inherited, effects, animatable, syntax)
    {
        Initial = initial;
        InitialValue = initial;
        this.parser = parser;
    }

    /// <summary>Gets the property's strongly typed initial value.</summary>
    public T Initial { get; }

    /// <summary>Gets the property's initial value before cascading and inheritance.</summary>
    public override object InitialValue { get; }

    /// <summary>Gets the CLR type used to represent values of this property.</summary>
    public override Type ValueType => typeof(T);

    /// <summary>Parses a value according to this property's grammar.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <returns>The resulting object value.</returns>
    public override object Parse(TokenReader reader)
    {
        var value = parser(reader);
        reader.ExpectEnd();
        return value;
    }
}

/// <summary>A shorthand expanded into longhands, at compile time or after <c>var()</c> substitution.</summary>
public sealed class ShorthandProperty
{
    private readonly Func<TokenReader, IReadOnlyList<(StyleProperty Property, object Value)>> expand;
    private IReadOnlyList<string>? keywords;
    private IReadOnlyList<string>? valueTypes;

    internal ShorthandProperty(string name, IReadOnlyList<StyleProperty> longhands, string syntax, Func<TokenReader, IReadOnlyList<(StyleProperty, object)>> expand)
    {
        Name = name;
        Longhands = longhands;
        Syntax = syntax;
        this.expand = expand;
    }

    /// <summary>Gets the canonical NSS name of the shorthand property.</summary>
    public string Name { get; }

    /// <inheritdoc cref="StyleProperty.Syntax" />
    /// <summary>Gets the NSS value-definition grammar accepted by this property.</summary>
    public string Syntax { get; }

    /// <inheritdoc cref="StyleProperty.Keywords" />
    /// <summary>Gets the literal keywords accepted by the property's grammar.</summary>
    public IReadOnlyList<string> Keywords => ValueSyntax.Keywords(Syntax, ref keywords);

    /// <inheritdoc cref="StyleProperty.ValueTypes" />
    /// <summary>Gets the value-type names referenced by the property's grammar.</summary>
    public IReadOnlyList<string> ValueTypes => ValueSyntax.Types(Syntax, ref valueTypes);

    /// <summary>Gets the longhand properties assigned by this shorthand.</summary>
    public IReadOnlyList<StyleProperty> Longhands { get; }

    /// <summary>Parses a shorthand and expands it into its constituent longhand property values.</summary>
    /// <param name="reader">The token reader positioned at the start of the value.</param>
    /// <returns>The longhand property and parsed value pairs assigned by the shorthand.</returns>
    public IReadOnlyList<(StyleProperty Property, object Value)> Expand(TokenReader reader)
    {
        var values = expand(reader);
        reader.ExpectEnd();
        return values;
    }

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Name;
}

internal static partial class ValueSyntax
{
    public static IReadOnlyList<string> Keywords(string syntax, ref IReadOnlyList<string>? cache)
    {
        if (Volatile.Read(ref cache) is { } cached) return cached;
        IReadOnlyList<string> parsed = Keyword().Matches(Type().Replace(syntax, " ")).Select(m => m.Value).Distinct(StringComparer.Ordinal).ToArray();
        return Interlocked.CompareExchange(ref cache, parsed, null) ?? parsed;
    }

    public static IReadOnlyList<string> Types(string syntax, ref IReadOnlyList<string>? cache)
    {
        if (Volatile.Read(ref cache) is { } cached) return cached;
        IReadOnlyList<string> parsed = Type().Matches(syntax).Select(m => m.Groups["name"].Value).Distinct(StringComparer.Ordinal).ToArray();
        return Interlocked.CompareExchange(ref cache, parsed, null) ?? parsed;
    }

    [GeneratedRegex("<(?<name>[a-z-]+)>")]
    private static partial Regex Type();

    [GeneratedRegex("(?<![\\w-])[a-z][a-z-]*(?![\\w-])")]
    private static partial Regex Keyword();
}
