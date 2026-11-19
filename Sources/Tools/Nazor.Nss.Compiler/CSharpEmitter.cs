using System.Globalization;
using System.Text;
using Nazor.Rendering;
using Nazor.Style;
using Nazor.Style.Syntax;

namespace Nazor.Nss;

public static class CSharpEmitter
{
    private const string Ns = "global::Nazor.Style.";
    private const string NoInline = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]";
    private const int DeclarationBatchSize = 16;

    public static string Emit(StyleSheet sheet) => Emit(sheet, null);

    public static string EmitFactory(StyleSheet sheet, string name)
    {
        var index = 0;
        var builder = new StringBuilder($"private static {Ns}StyleSheet {name}()\n{{\n    return ");
        builder.Append(Emit(sheet, _ => $"__Rule{index++}()")).AppendLine(";");
        for (index = 0; index < sheet.Rules.Count; index++)
        {
            var rule = sheet.Rules[index];
            builder.AppendLine(NoInline);
            builder.Append($"    static {Ns}StyleRule __Rule{index}()");
            if (rule.Declarations.Count <= DeclarationBatchSize)
            {
                builder.Append(" => ").Append(Rule(rule)).AppendLine(";");
                continue;
            }
            builder.AppendLine("\n    {");
            builder.AppendLine($"        var declarations = new {Ns}StyleDeclaration[{rule.Declarations.Count}];");
            for (var start = 0; start < rule.Declarations.Count; start += DeclarationBatchSize)
                builder.AppendLine($"        __Fill{start}(declarations);");
            builder.Append("        return ").Append(Rule(rule, "declarations")).AppendLine(";");
            for (var start = 0; start < rule.Declarations.Count; start += DeclarationBatchSize)
            {
                builder.AppendLine(NoInline);
                builder.AppendLine($"        static void __Fill{start}({Ns}StyleDeclaration[] declarations)\n        {{");
                for (var declaration = start; declaration < Math.Min(start + DeclarationBatchSize, rule.Declarations.Count); declaration++)
                    builder.Append($"            declarations[{declaration}] = ").Append(Declaration(rule.Declarations[declaration])).AppendLine(";");
                builder.AppendLine("        }");
            }
            builder.AppendLine("    }");
        }
        return builder.AppendLine("}").ToString();
    }

    private static string Emit(StyleSheet sheet, Func<StyleRule, string>? ruleFactory)
    {
        var builder = new StringBuilder();
        builder.Append($"new {Ns}StyleSheet({Literal(sheet.Source)}, new {Ns}StyleRule[]\n{{\n");
        foreach (var rule in sheet.Rules)
        {
            builder.Append("    ").Append(ruleFactory?.Invoke(rule) ?? Rule(rule)).Append(",\n");
        }
        builder.Append('}');
        if (sheet.Keyframes.Count > 0)
        {
            builder.Append($", new {Ns}KeyframesRule[]\n{{\n");
            foreach (var keyframes in sheet.Keyframes)
            {
                builder.Append($"    new {Ns}KeyframesRule({Literal(keyframes.Name)}, new {Ns}Keyframe[] {{ ");
                builder.Append(string.Join(", ", keyframes.Keyframes.Select(frame =>
                    $"new {Ns}Keyframe({Number(frame.Offset)}, new {Ns}StyleDeclaration[] {{ {string.Join(", ", frame.Declarations.Select(Declaration))} }}, " +
                    $"{(frame.Easing is { } easing ? Easing(easing) : "null")})")));
                builder.Append(" }),\n");
            }
            builder.Append('}');
        }
        builder.Append(')');
        return builder.ToString();
    }

    private static string Rule(StyleRule rule, string? declarations = null)
    {
        declarations ??= $"new {Ns}StyleDeclaration[] {{ {string.Join(", ", rule.Declarations.Select(Declaration))} }}";
        return $"new {Ns}StyleRule({Selector(rule.Selector)}, {(rule.Media is null ? "null" : Media(rule.Media))}, {declarations}, {rule.Order})";
    }

    private static string Easing(EasingFunction easing) =>
        $"new {Ns}EasingFunction({Number(easing.X1)}, {Number(easing.Y1)}, {Number(easing.X2)}, {Number(easing.Y2)})";

    private static string Selector(Selector selector) =>
        $"new {Ns}Selector(new {Ns}CompoundSelector[] {{ {string.Join(", ", selector.Compounds.Select(Compound))} }}, " +
        $"new {Ns}Combinator[] {{ {string.Join(", ", selector.Combinators.Select(c => Ns + "Combinator." + c))} }})";

    private static string Compound(CompoundSelector compound)
    {
        var basic = $"{Nullable(compound.Type)}, {Nullable(compound.Id)}, new string[] {{ {string.Join(", ", compound.Classes.Select(Literal))} }}, {States(compound.States)}";
        if (compound.Attributes.Count == 0 && compound.Pseudos.Count == 0 && compound.PseudoElement == PseudoElement.None) return $"new {Ns}CompoundSelector({basic})";
        var attributes = string.Join(", ", compound.Attributes.Select(a =>
            $"new {Ns}AttributeSelector({Literal(a.Name)}, {Ns}AttributeMatch.{a.Match}, {Nullable(a.Value)}, {(a.IgnoreCase ? "true" : "false")})"));
        var target = compound.PseudoElement == PseudoElement.None ? "" : $", {Ns}PseudoElement.{compound.PseudoElement}";
        return $"new {Ns}CompoundSelector({basic}, new {Ns}AttributeSelector[] {{ {attributes} }}, new {Ns}PseudoSelector[] {{ {string.Join(", ", compound.Pseudos.Select(Pseudo))} }}{target})";
    }

    private static string Pseudo(PseudoSelector pseudo) => pseudo switch
    {
        NthSelector n => $"new {Ns}NthSelector({n.A}, {n.B}, {(n.FromEnd ? "true" : "false")}, {(n.OfType ? "true" : "false")}, {Literal(n.Name)})" +
            (n.Filter is null ? "" : $" {{ Filter = new {Ns}ValueList<{Ns}Selector>(new {Ns}Selector[] {{ {string.Join(", ", n.Filter.Items.Select(Selector))} }}) }}"),
        OnlyChildSelector o => $"new {Ns}OnlyChildSelector({(o.OfType ? "true" : "false")})",
        EmptySelector => $"new {Ns}EmptySelector()",
        RootSelector => $"new {Ns}RootSelector()",
        ControlStateSelector c => $"new {Ns}ControlStateSelector({Literal(c.Name)})",
        NamedStateSelector s => $"new {Ns}NamedStateSelector({Literal(s.Name)})",
        PartSelector p => $"new {Ns}PartSelector({Literal(p.Component)}, {Literal(p.Name)})",
        ItemPositionSelector i => $"new {Ns}ItemPositionSelector({i.A}, {i.B}, {(i.FromEnd ? "true" : "false")}, {Literal(i.Name)})",
        ComponentRootSelector => $"new {Ns}ComponentRootSelector()",
        HasSelector h => $"new {Ns}HasSelector(new {Ns}ValueList<{Ns}RelativeSelector>(new {Ns}RelativeSelector[] {{ {string.Join(", ", h.Selectors.Items.Select(r => $"new {Ns}RelativeSelector({Ns}Combinator.{r.Relation}, {Selector(r.Selector)})"))} }}))",
        LogicalSelector l => $"new {Ns}LogicalSelector({Ns}LogicalPseudo.{l.Kind}, new {Ns}ValueList<{Ns}Selector>(new {Ns}Selector[] {{ {string.Join(", ", l.Selectors.Items.Select(Selector))} }}))",
        _ => throw new NotSupportedException(pseudo.GetType().Name),
    };

    private static string States(ElementStates states) => states == ElementStates.None
        ? "global::Nazor.Rendering.ElementStates.None"
        : string.Join(" | ", Enum.GetValues<ElementStates>().Where(s => s != ElementStates.None && states.HasFlag(s)).Select(s => "global::Nazor.Rendering.ElementStates." + s));

    private static string Media(MediaQuery media) =>
        $"new {Ns}MediaQuery(new {Ns}MediaCondition[] {{ {string.Join(", ", media.Conditions.Select(c =>
            $"new {Ns}MediaCondition({Number(c.MinWidth)}, {Number(c.MaxWidth)}, {Number(c.MinHeight)}, {Number(c.MaxHeight)}, " +
            $"{(c.PrefersColorScheme is { } scheme ? $"{Ns}ColorScheme.{scheme}" : "null")}, {Flag(c.PrefersReducedMotion)}, {Flag(c.ForcedColors)}, {Flag(c.PrefersMoreContrast)}, {(c.Platform is { } platform ? $"global::Nazor.PlatformKind.{platform}" : "null")}, {(c.Density is { } density ? $"global::Nazor.ControlDensity.{density}" : "null")})"))} }})";

    private static string Flag(bool? value) => value is { } flag ? (flag ? "true" : "false") : "null";

    private static string Declaration(StyleDeclaration declaration) => declaration switch
    {
        ValueDeclaration value => $"new {Ns}ValueDeclaration({Property(value.Property)}, {Value(value.Value)})",
        KeywordDeclaration keyword => $"new {Ns}KeywordDeclaration({Property(keyword.Property)}, {Ns}GlobalKeyword.{keyword.Keyword})",
        PendingDeclaration pending => $"new {Ns}PendingDeclaration({Literal(pending.PropertyName)}, {Tokens(pending.Tokens)})",
        CustomPropertyDeclaration custom => $"new {Ns}CustomPropertyDeclaration({Literal(custom.Name)}, {Tokens(custom.Tokens)})",
        _ => throw new NotSupportedException(declaration.GetType().Name),
    };

    private static string Property(StyleProperty property) =>
        Ns + "StyleProperties." + string.Concat(property.Name.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));

    private static string Value(object value) => value switch
    {
        Length { Expression: { } expression } => $"{Ns}Length.Calc(new {Ns}CalcExpression({Calc(expression.Root)}, {(expression.NonNegative ? "true" : "false")}))",
        Length length => $"new {Ns}Length({Number(length.Value)}, {Ns}LengthUnit.{length.Unit})",
        SizeValue size => $"new {Ns}SizeValue({Ns}SizeKind.{size.Kind}, {Value(size.Length)})",
        Color color => $"new {Ns}Color({color.R}, {color.G}, {color.B}, {color.A}, {(color.IsCurrentColor ? "true" : "false")})",
        NativeColor native => native.Value is { } color ? $"new {Ns}NativeColor({Value(color)})" : $"{Ns}NativeColor.Auto",
        LineHeight line => $"new {Ns}LineHeight({Ns}LineHeightKind.{line.Kind}, {Number(line.Number)}, {Value(line.Length)})",
        ScrollWheelStep step => $"new {Ns}ScrollWheelStep({Ns}ScrollWheelStepKind.{step.Kind}, {Value(step.Length)}, {Number(step.Lines)})",
        AspectRatio ratio => $"new {Ns}AspectRatio({Number(ratio.Ratio)})",
        RadiusValue radius => $"new {Ns}RadiusValue({Value(radius.X)}, {Value(radius.Y)})",
        ClipPathValue clip => $"new {Ns}ClipPathValue({Value(clip.Top)}, {Value(clip.Right)}, {Value(clip.Bottom)}, {Value(clip.Left)}, {(clip.IsNone ? "true" : "false")})" +
            (clip.TopLeftRadius == RadiusValue.Zero && clip.TopRightRadius == RadiusValue.Zero && clip.BottomRightRadius == RadiusValue.Zero && clip.BottomLeftRadius == RadiusValue.Zero ? "" :
                $" {{ TopLeftRadius = {Value(clip.TopLeftRadius)}, TopRightRadius = {Value(clip.TopRightRadius)}, BottomRightRadius = {Value(clip.BottomRightRadius)}, BottomLeftRadius = {Value(clip.BottomLeftRadius)} }}"),
        ValueList<ImageValue> images => $"new {Ns}ValueList<{Ns}ImageValue>(new {Ns}ImageValue[] {{ {string.Join(", ", images.Items.Select(Image))} }})",
        ValueList<PositionValue> positions => $"new {Ns}ValueList<{Ns}PositionValue>(new {Ns}PositionValue[] {{ {string.Join(", ", positions.Items.Select(p => Value(p)))} }})",
        ValueList<BackgroundSize> sizes => $"new {Ns}ValueList<{Ns}BackgroundSize>(new {Ns}BackgroundSize[] {{ {string.Join(", ", sizes.Items.Select(b =>
            $"new {Ns}BackgroundSize({Ns}BackgroundSizeKind.{b.Kind}, {Value(b.Width)}, {Value(b.Height)})"))} }})",
        ValueList<BlendMode> modes => $"new {Ns}ValueList<{Ns}BlendMode>(new {Ns}BlendMode[] {{ {string.Join(", ", modes.Items.Select(mode => $"{Ns}BlendMode.{mode}"))} }})",
        ValueList<BackgroundRepeat> repeats => $"new {Ns}ValueList<{Ns}BackgroundRepeat>(new {Ns}BackgroundRepeat[] {{ {string.Join(", ", repeats.Items.Select(r => $"{Ns}BackgroundRepeat.{r}"))} }})",
        WindowFrameShadows frame => $"new {Ns}WindowFrameShadows({(frame.Shadows is { } explicitShadows ? Value(explicitShadows) : "null")})",
        ValueList<BoxShadow> shadows => $"new {Ns}ValueList<{Ns}BoxShadow>(new {Ns}BoxShadow[] {{ {string.Join(", ", shadows.Items.Select(s =>
            $"new {Ns}BoxShadow({(s.Inset ? "true" : "false")}, {Value(s.X)}, {Value(s.Y)}, {Value(s.Blur)}, {Value(s.Spread)}, {Value(s.Color)})"))} }})",
        ZIndex z => $"new {Ns}ZIndex({(z.IsAuto ? "true" : "false")}, {z.Value.ToString(CultureInfo.InvariantCulture)})",
        GridLine line => $"new {Ns}GridLine({Ns}GridLineKind.{line.Kind}, {line.Value.ToString(CultureInfo.InvariantCulture)}, {Nullable(line.Name)})",
        GridTrackList list => $"new {Ns}GridTrackList({Tracks(list.Tracks)}, {Ns}GridAutoRepeat.{list.Repeat}, {list.RepeatIndex.ToString(CultureInfo.InvariantCulture)}, {Tracks(list.RepeatTracks)})",
        ValueList<TrackSize> tracks => Tracks(tracks),
        FilterValue filters => $"new {Ns}FilterValue(new {Ns}ValueList<{Ns}FilterFunction>(new {Ns}FilterFunction[] {{ " +
            string.Join(", ", filters.Functions.Items.Select(function => $"new {Ns}FilterFunction({Ns}FilterKind.{function.Kind}, {Number(function.Amount)}, {Value(function.Radius)})")) + " }))",
        ValueList<TransformFunction> transforms =>
            $"new {Ns}ValueList<{Ns}TransformFunction>(new {Ns}TransformFunction[] {{ {string.Join(", ", transforms.Items.Select(TransformFunction))} }})",
        TransformOrigin origin => $"new {Ns}TransformOrigin({Value(origin.X)}, {Value(origin.Y)})",
        PositionValue position => $"new {Ns}PositionValue({Value(position.X)}, {Value(position.Y)})",
        GridAreas areas => $"new {Ns}GridAreas(new string[] {{ {string.Join(", ", areas.Rows.Items.Select(Literal))} }})",
        double number => Number(number),
        TimeSpan time => $"global::System.TimeSpan.FromTicks({time.Ticks})",
        EasingFunction easing => Easing(easing),
        bool flag => flag ? "true" : "false",
        int integer => integer.ToString(CultureInfo.InvariantCulture),
        Enum enumeration when !Enum.IsDefined(enumeration.GetType(), enumeration) =>
            $"({Ns}{enumeration.GetType().Name}){Convert.ToInt64(enumeration, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)}",
        Enum enumeration => $"global::{enumeration.GetType().FullName!.Replace('+', '.')}.{enumeration}",
        ValueList<string> strings => $"new {Ns}ValueList<string>(new string[] {{ {string.Join(", ", strings.Items.Select(Literal))} }})",
        ValueList<FontFeatureSetting> features => $"new {Ns}ValueList<{Ns}FontFeatureSetting>(new {Ns}FontFeatureSetting[] {{ {string.Join(", ", features.Items.Select(feature => $"new {Ns}FontFeatureSetting({Literal(feature.Tag)}, {feature.Value.ToString(CultureInfo.InvariantCulture)}u)"))} }})",
        ValueList<FontVariationSetting> axes => $"new {Ns}ValueList<{Ns}FontVariationSetting>(new {Ns}FontVariationSetting[] {{ {string.Join(", ", axes.Items.Select(axis =>
            $"new {Ns}FontVariationSetting({Literal(axis.Tag)}, {Number(axis.Value)})"))} }})",
        ValueList<FontSymbol> symbols => $"new {Ns}ValueList<global::Nazor.FontSymbol>(new global::Nazor.FontSymbol[] {{ {string.Join(", ", symbols.Items.Select(symbol =>
            $"new global::Nazor.FontSymbol({Literal(symbol.FontFamily)}, {Literal(symbol.Glyph)})"))} }})",
        ValueList<AnimationDefinition> animations =>
            $"new {Ns}ValueList<{Ns}AnimationDefinition>(new {Ns}AnimationDefinition[] {{ {string.Join(", ", animations.Items.Select(Animation))} }})",
        ValueList<TransitionDefinition> transitions =>
            $"new {Ns}ValueList<{Ns}TransitionDefinition>(new {Ns}TransitionDefinition[] {{ {string.Join(", ", transitions.Items.Select(Transition))} }})",
        _ => throw new NotSupportedException($"Cannot emit a value of type {value.GetType().Name}."),
    };

    private static string TransformFunction(TransformFunction function) => function switch
    {
        TranslateFunction t => $"new {Ns}TranslateFunction({Value(t.X)}, {Value(t.Y)})",
        ScaleFunction s => $"new {Ns}ScaleFunction({Number(s.X)}, {Number(s.Y)})",
        RotateFunction r => $"new {Ns}RotateFunction({Number(r.Angle)})",
        SkewFunction k => $"new {Ns}SkewFunction({Number(k.X)}, {Number(k.Y)})",
        MatrixFunction m => $"new {Ns}MatrixFunction({Number(m.A)}, {Number(m.B)}, {Number(m.C)}, {Number(m.D)}, {Number(m.E)}, {Number(m.F)})",
        _ => throw new NotSupportedException($"Cannot emit a transform function of type {function.GetType().Name}."),
    };

    private static string Calc(CalcNode node) => node switch
    {
        CalcNumber number => $"new {Ns}CalcNumber({Number(number.Value)})",
        CalcLength length => $"new {Ns}CalcLength({Value(length.Length)})",
        CalcOperation operation => $"new {Ns}CalcOperation({Ns}CalcOperator.{operation.Operator}, {Calc(operation.Left)}, {Calc(operation.Right)})",
        CalcFunction function => $"new {Ns}CalcFunction({Ns}CalcFunctionKind.{function.Kind}, new {Ns}ValueList<{Ns}CalcNode>(new {Ns}CalcNode[] {{ " +
            $"{string.Join(", ", function.Arguments.Items.Select(Calc))} }}))",
        _ => throw new NotSupportedException($"Cannot emit a calculation node of type {node.GetType().Name}."),
    };

    private static string Tracks(ValueList<TrackSize> tracks) =>
        $"new {Ns}ValueList<{Ns}TrackSize>(new {Ns}TrackSize[] {{ {string.Join(", ", tracks.Items.Select(Track))} }})";

    private static string Track(TrackSize track) =>
        $"new {Ns}TrackSize({Breadth(track.Min)}, {Breadth(track.Max)}, {(track.FitContent ? "true" : "false")})";

    private static string Breadth(TrackBreadth breadth) =>
        $"new {Ns}TrackBreadth({Ns}TrackBreadthKind.{breadth.Kind}, {Value(breadth.Length)}, {Number(breadth.Flex)})";

    private static string Image(ImageValue image) => (image switch
    {
        UrlImage u => $"new {Ns}UrlImage({Literal(u.Url)})",
        LinearGradient l => $"new {Ns}LinearGradient({Number(l.Angle)}, {l.ToX}, {l.ToY}, {Stops(l.Stops)})",
        RadialGradient r => $"new {Ns}RadialGradient({Ns}RadialShape.{r.Shape}, {Ns}RadialExtent.{r.Extent}, {Value(r.SizeX)}, {Value(r.SizeY)}, " +
            $"{Value(r.CenterX)}, {Value(r.CenterY)}, {Stops(r.Stops)})",
        ConicGradient c => $"new {Ns}ConicGradient({Number(c.Angle)}, {Value(c.CenterX)}, {Value(c.CenterY)}, {Stops(c.Stops)})",
        _ => throw new NotSupportedException($"Cannot emit an image of type {image.GetType().Name}."),
    }) + (image is Gradient { Repeating: true } ? " { Repeating = true }" : "");

    private static string Stops(ValueList<ColorStop> stops) =>
        $"new {Ns}ValueList<{Ns}ColorStop>(new {Ns}ColorStop[] {{ {string.Join(", ", stops.Items.Select(s =>
            $"new {Ns}ColorStop({Value(s.Color)}, {(s.Position is { } position ? Value(position) : "null")})"))} }})";

    private static string Animation(AnimationDefinition a) =>
        $"new {Ns}AnimationDefinition({(a.Name is null ? "null" : Literal(a.Name))}, global::System.TimeSpan.FromTicks({a.Duration.Ticks}), {Easing(a.Easing)}, " +
        $"global::System.TimeSpan.FromTicks({a.Delay.Ticks}), {(double.IsPositiveInfinity(a.IterationCount) ? "double.PositiveInfinity" : Number(a.IterationCount))}, " +
        $"{Ns}AnimationDirection.{a.Direction}, {Ns}AnimationFillMode.{a.FillMode}, {(a.Paused ? "true" : "false")})";

    private static string Transition(TransitionDefinition t) =>
        $"new {Ns}TransitionDefinition({Literal(t.Property)}, global::System.TimeSpan.FromTicks({t.Duration.Ticks}), " +
        $"new {Ns}EasingFunction({Number(t.Easing.X1)}, {Number(t.Easing.Y1)}, {Number(t.Easing.X2)}, {Number(t.Easing.Y2)}), " +
        $"global::System.TimeSpan.FromTicks({t.Delay.Ticks}))";

    private static string Tokens(IReadOnlyList<StyleToken> tokens) =>
        $"new {Ns}Syntax.StyleToken[] {{ {string.Join(", ", tokens.Select(t =>
            $"new {Ns}Syntax.StyleToken({Ns}Syntax.StyleTokenKind.{t.Kind}, {Literal(t.Value)}, {Number(t.Number)}, 0, 0)"))} }}";

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture) + "d";

    private static string Number(double? value) => value is { } v ? Number(v) : "null";

    private static string Nullable(string? value) => value is null ? "null" : Literal(value);

    public static string Literal(string value)
    {
        var builder = new StringBuilder("\"");
        foreach (var c in value)
        {
            builder.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                < ' ' => $"\\u{(int)c:x4}",
                _ => c.ToString(),
            });
        }
        return builder.Append('"').ToString();
    }
}
