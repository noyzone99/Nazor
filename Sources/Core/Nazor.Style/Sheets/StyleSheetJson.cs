using System.Text.Json;
using System.Text.Json.Nodes;
using Nazor.Rendering;
using Nazor.Style.Syntax;

namespace Nazor.Style;

/// <summary>
/// JSON form of a compiled <see cref="StyleSheet" />, for development tools that send sheets to a running application
/// (hot reload of styles). It round-trips everything the C# emitter handles; properties resolve through
/// <see cref="StyleProperties" />, so a sheet compiled by another version of the registry is rejected with
/// <see cref="InvalidDataException" />.
/// </summary>
public static class StyleSheetJson
{
    /// <summary>Serializes the style sheet to its JSON text representation.</summary>
    /// <param name="sheet">The stylesheet to serialize.</param>
    /// <returns>The serialized stylesheet JSON text.</returns>
    public static string Serialize(StyleSheet sheet) => ToJson(sheet).ToJsonString();

    /// <summary>Converts the style sheet into its structured JSON representation.</summary>
    /// <param name="sheet">The stylesheet to serialize.</param>
    /// <returns>The JSON node representing the stylesheet.</returns>
    /// <exception cref="ArgumentNullException">sheet is null.</exception>
    public static JsonNode ToJson(StyleSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var rules = new JsonArray();
        foreach (var rule in sheet.Rules) rules.Add((JsonNode)Rule(rule));
        var json = new JsonObject { ["source"] = sheet.Source, ["rules"] = rules };
        if (sheet.Keyframes.Count > 0) json["keyframes"] = new JsonArray(sheet.Keyframes.Select(k => (JsonNode)Keyframes(k)).ToArray());
        return json;
    }

    /// <summary>
    /// Parses the JSON of <see cref="Serialize" />; malformed input raises <see cref="InvalidDataException" /> naming
    /// the offending node.
    /// </summary>
    /// <param name="json">The serialized stylesheet JSON text.</param>
    /// <returns>The resulting StyleSheet value.</returns>
    /// <exception cref="ArgumentNullException">json is null.</exception>
    public static StyleSheet Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The stylesheet JSON is malformed: {exception.Message}", exception);
        }
        return FromJson(node ?? throw new InvalidDataException("The stylesheet JSON is empty."));
    }

    /// <summary>Reconstructs a style sheet from its serialized JSON node.</summary>
    /// <param name="node">The JSON node or retained tree node to inspect.</param>
    /// <returns>The stylesheet reconstructed from the JSON node.</returns>
    /// <exception cref="ArgumentNullException">node is null.</exception>
    public static StyleSheet FromJson(JsonNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var sheet = Object(node);
        var rules = new List<StyleRule>();
        foreach (var item in Array(sheet, "rules")) rules.Add(ReadRule(Object(item)));

        var keyframes = sheet["keyframes"] is JsonArray list ? list.Select(item => ReadKeyframes(Object(item))).ToList() : [];
        return new StyleSheet(String(sheet, "source"), rules, keyframes, track: false);
    }

    private static JsonObject Keyframes(KeyframesRule rule) => new()
    {
        ["name"] = rule.Name,
        ["frames"] = new JsonArray(rule.Keyframes.Select(frame =>
        {
            var json = new JsonObject { ["offset"] = frame.Offset, ["declarations"] = new JsonArray(frame.Declarations.Select(d => (JsonNode)Declaration(d)).ToArray()) };
            if (frame.Easing is { } easing) json["easing"] = Easing(easing);
            return (JsonNode)json;
        }).ToArray()),
    };

    private static JsonObject Easing(EasingFunction easing) => new() { ["x1"] = easing.X1, ["y1"] = easing.Y1, ["x2"] = easing.X2, ["y2"] = easing.Y2 };

    private static KeyframesRule ReadKeyframes(JsonObject rule) => new(String(rule, "name"), Array(rule, "frames").Select(item =>
    {
        var frame = Object(item);
        return new Keyframe(Value<double>(frame, "offset"), Array(frame, "declarations").Select(d => ReadDeclaration(Object(d))).ToArray(),
            frame["easing"] is { } easing ? ReadEasing(Object(easing)) : null);
    }).ToArray());

    private static EasingFunction ReadEasing(JsonObject easing) =>
        new(Value<double>(easing, "x1"), Value<double>(easing, "y1"), Value<double>(easing, "x2"), Value<double>(easing, "y2"));

    private static JsonObject Rule(StyleRule rule)
    {
        var declarations = new JsonArray();
        foreach (var declaration in rule.Declarations) declarations.Add((JsonNode)Declaration(declaration));
        var json = new JsonObject { ["order"] = rule.Order, ["selector"] = Selector(rule.Selector) };
        if (rule.Media is not null) json["media"] = Media(rule.Media);
        json["declarations"] = declarations;
        return json;
    }

    private static JsonObject Selector(Selector selector)
    {
        var compounds = new JsonArray();
        foreach (var compound in selector.Compounds)
        {
            var json = new JsonObject();
            if (compound.Type is not null) json["type"] = compound.Type;
            if (compound.Id is not null) json["id"] = compound.Id;
            if (compound.PseudoElement != PseudoElement.None) json["pseudoElement"] = compound.PseudoElement.ToString();
            json["classes"] = new JsonArray(compound.Classes.Select(c => (JsonNode)c).ToArray());
            json["states"] = new JsonArray(Enum.GetValues<ElementStates>()
                .Where(s => s != ElementStates.None && compound.States.HasFlag(s)).Select(s => (JsonNode)s.ToString()).ToArray());
            if (compound.Attributes.Count > 0)
                json["attributes"] = new JsonArray(compound.Attributes.Select(a =>
                {
                    var attribute = new JsonObject { ["name"] = a.Name, ["match"] = a.Match.ToString(), ["ignoreCase"] = a.IgnoreCase };
                    if (a.Value is not null) attribute["value"] = a.Value;
                    return (JsonNode)attribute;
                }).ToArray());
            if (compound.Pseudos.Count > 0) json["pseudos"] = new JsonArray(compound.Pseudos.Select(p => (JsonNode)Pseudo(p)).ToArray());
            compounds.Add((JsonNode)json);
        }
        return new JsonObject
        {
            ["compounds"] = compounds,
            ["combinators"] = new JsonArray(selector.Combinators.Select(c => (JsonNode)c.ToString()).ToArray()),
        };
    }

    private static JsonObject Pseudo(PseudoSelector pseudo) => pseudo switch
    {
        NthSelector n => new JsonObject { ["kind"] = "nth", ["a"] = n.A, ["b"] = n.B, ["fromEnd"] = n.FromEnd, ["ofType"] = n.OfType, ["name"] = n.Name,
            ["filter"] = n.Filter is null ? null : new JsonArray(n.Filter.Items.Select(s => (JsonNode)Selector(s)).ToArray()) },
        OnlyChildSelector o => new JsonObject { ["kind"] = "only", ["ofType"] = o.OfType },
        EmptySelector => new JsonObject { ["kind"] = "empty" },
        RootSelector => new JsonObject { ["kind"] = "root" },
        ControlStateSelector c => new JsonObject { ["kind"] = "control", ["name"] = c.Name },
        NamedStateSelector s => new JsonObject { ["kind"] = "state", ["name"] = s.Name },
        PartSelector p => new JsonObject { ["kind"] = "part", ["component"] = p.Component, ["name"] = p.Name },
        ItemPositionSelector i => new JsonObject { ["kind"] = "item", ["a"] = i.A, ["b"] = i.B, ["fromEnd"] = i.FromEnd, ["name"] = i.Name },
        ComponentRootSelector => new JsonObject { ["kind"] = "component-root" },
        HasSelector h => new JsonObject { ["kind"] = "has", ["selectors"] = new JsonArray(h.Selectors.Items.Select(r => (JsonNode)new JsonObject { ["relation"] = r.Relation.ToString(), ["selector"] = Selector(r.Selector) }).ToArray()) },
        LogicalSelector l => new JsonObject
        {
            ["kind"] = "logical", ["logical"] = l.Kind.ToString(), ["selectors"] = new JsonArray(l.Selectors.Items.Select(s => (JsonNode)Selector(s)).ToArray()),
        },
        _ => throw new NotSupportedException(pseudo.GetType().Name),
    };

    private static JsonObject Media(MediaQuery media)
    {
        var conditions = new JsonArray();
        foreach (var condition in media.Conditions)
        {
            var json = new JsonObject();
            if (condition.MinWidth is { } minWidth) json["minWidth"] = minWidth;
            if (condition.MaxWidth is { } maxWidth) json["maxWidth"] = maxWidth;
            if (condition.MinHeight is { } minHeight) json["minHeight"] = minHeight;
            if (condition.MaxHeight is { } maxHeight) json["maxHeight"] = maxHeight;
            if (condition.PrefersColorScheme is { } scheme) json["prefersColorScheme"] = scheme.ToString();
            if (condition.PrefersReducedMotion is { } reduced) json["prefersReducedMotion"] = reduced;
            if (condition.ForcedColors is { } forced) json["forcedColors"] = forced;
            if (condition.PrefersMoreContrast is { } more) json["prefersMoreContrast"] = more;
            if (condition.Platform is { } platform) json["platform"] = platform.ToString();
            if (condition.Density is { } density) json["density"] = density.ToString();
            conditions.Add((JsonNode)json);
        }
        return new JsonObject { ["conditions"] = conditions };
    }

    private static JsonObject Declaration(StyleDeclaration declaration) => declaration switch
    {
        ValueDeclaration value => new JsonObject { ["kind"] = "value", ["property"] = value.Property.Name, ["value"] = Value(value.Value) },
        KeywordDeclaration keyword => new JsonObject { ["kind"] = "keyword", ["property"] = keyword.Property.Name, ["keyword"] = keyword.Keyword.ToString() },
        PendingDeclaration pending => new JsonObject { ["kind"] = "pending", ["property"] = pending.PropertyName, ["tokens"] = Tokens(pending.Tokens) },
        CustomPropertyDeclaration custom => new JsonObject { ["kind"] = "custom", ["name"] = custom.Name, ["tokens"] = Tokens(custom.Tokens) },
        _ => throw new NotSupportedException(declaration.GetType().Name),
    };

    private static JsonObject Value(object value) => value switch
    {
        NativeColor native => new JsonObject { ["type"] = "nativeColor", ["value"] = native.Value is { } color ? Value(color) : null },
        Length length => Length(length),
        SizeValue size => new JsonObject { ["type"] = "size", ["kind"] = size.Kind.ToString(), ["length"] = Length(size.Length) },
        Color color => new JsonObject { ["type"] = "color", ["r"] = color.R, ["g"] = color.G, ["b"] = color.B, ["a"] = color.A, ["currentColor"] = color.IsCurrentColor },
        LineHeight line => new JsonObject { ["type"] = "lineHeight", ["kind"] = line.Kind.ToString(), ["number"] = line.Number, ["length"] = Length(line.Length) },
        ScrollWheelStep step => new JsonObject { ["type"] = "scrollWheelStep", ["kind"] = step.Kind.ToString(), ["lines"] = step.Lines, ["length"] = Length(step.Length) },
        ZIndex z => new JsonObject { ["type"] = "zIndex", ["auto"] = z.IsAuto, ["value"] = z.Value },
        AspectRatio ratio => new JsonObject { ["type"] = "aspectRatio", ["ratio"] = ratio.Ratio },
        RadiusValue radius => new JsonObject { ["type"] = "radius", ["x"] = Length(radius.X), ["y"] = Length(radius.Y) },
        ClipPathValue clip => new JsonObject { ["type"] = "clipPath", ["none"] = clip.IsNone, ["top"] = Length(clip.Top),
            ["right"] = Length(clip.Right), ["bottom"] = Length(clip.Bottom), ["left"] = Length(clip.Left),
            ["topLeftRadius"] = Value(clip.TopLeftRadius), ["topRightRadius"] = Value(clip.TopRightRadius),
            ["bottomRightRadius"] = Value(clip.BottomRightRadius), ["bottomLeftRadius"] = Value(clip.BottomLeftRadius) },
        FilterValue filters => new JsonObject
        {
            ["type"] = "filters", ["functions"] = new JsonArray(filters.Functions.Items.Select(function => (JsonNode)new JsonObject
            { ["kind"] = function.Kind.ToString(), ["amount"] = function.Amount, ["radius"] = Length(function.Radius) }).ToArray()),
        },
        ValueList<TransformFunction> transforms => new JsonObject
        {
            ["type"] = "transforms", ["items"] = new JsonArray(transforms.Items.Select(f => (JsonNode)TransformFunction(f)).ToArray()),
        },
        TransformOrigin origin => new JsonObject { ["type"] = "transformOrigin", ["x"] = Length(origin.X), ["y"] = Length(origin.Y) },
        PositionValue position => new JsonObject { ["type"] = "position", ["x"] = Length(position.X), ["y"] = Length(position.Y) },
        ValueList<ImageValue> images => new JsonObject { ["type"] = "images", ["items"] = new JsonArray(images.Items.Select(i => (JsonNode)Image(i)).ToArray()) },
        ValueList<PositionValue> positions => new JsonObject
        {
            ["type"] = "positions", ["items"] = new JsonArray(positions.Items.Select(p => (JsonNode)new JsonObject { ["x"] = Length(p.X), ["y"] = Length(p.Y) }).ToArray()),
        },
        ValueList<BackgroundSize> sizes => new JsonObject
        {
            ["type"] = "backgroundSizes",
            ["items"] = new JsonArray(sizes.Items.Select(b => (JsonNode)new JsonObject
            {
                ["kind"] = b.Kind.ToString(),
                ["width"] = new JsonObject { ["kind"] = b.Width.Kind.ToString(), ["length"] = Length(b.Width.Length) },
                ["height"] = new JsonObject { ["kind"] = b.Height.Kind.ToString(), ["length"] = Length(b.Height.Length) },
            }).ToArray()),
        },
        ValueList<BlendMode> modes => new JsonObject { ["type"] = "blendModes", ["items"] = new JsonArray(modes.Items.Select(mode => (JsonNode)mode.ToString()).ToArray()) },
        ValueList<BackgroundRepeat> repeats => new JsonObject
        {
            ["type"] = "repeats", ["items"] = new JsonArray(repeats.Items.Select(r => (JsonNode)r.ToString()).ToArray()),
        },
        WindowFrameShadows frame => new JsonObject { ["type"] = "frameShadows", ["value"] = frame.Shadows is { } explicitShadows ? Value(explicitShadows) : null },
        ValueList<BoxShadow> shadows => new JsonObject
        {
            ["type"] = "shadows",
            ["items"] = new JsonArray(shadows.Items.Select(s => (JsonNode)new JsonObject
            {
                ["inset"] = s.Inset, ["x"] = Length(s.X), ["y"] = Length(s.Y), ["blur"] = Length(s.Blur), ["spread"] = Length(s.Spread), ["color"] = Value(s.Color),
            }).ToArray()),
        },
        GridLine line => line.Name is null
            ? new JsonObject { ["type"] = "gridLine", ["kind"] = line.Kind.ToString(), ["value"] = line.Value }
            : new JsonObject { ["type"] = "gridLine", ["kind"] = line.Kind.ToString(), ["value"] = line.Value, ["name"] = line.Name },
        GridTrackList list => new JsonObject
        {
            ["type"] = "trackList", ["tracks"] = Tracks(list.Tracks), ["repeat"] = list.Repeat.ToString(),
            ["repeatIndex"] = list.RepeatIndex, ["repeatTracks"] = Tracks(list.RepeatTracks),
        },
        ValueList<TrackSize> tracks => new JsonObject { ["type"] = "trackSizes", ["items"] = Tracks(tracks) },
        GridAreas areas => new JsonObject { ["type"] = "gridAreas", ["rows"] = new JsonArray(areas.Rows.Items.Select(r => (JsonNode)r).ToArray()) },
        double number => new JsonObject { ["type"] = "number", ["value"] = number },
        TimeSpan time => new JsonObject { ["type"] = "time", ["ticks"] = time.Ticks },
        EasingFunction easing => new JsonObject { ["type"] = "easing", ["value"] = Easing(easing) },
        bool flag => new JsonObject { ["type"] = "boolean", ["value"] = flag },
        int integer => new JsonObject { ["type"] = "integer", ["value"] = integer },
        Enum enumeration => new JsonObject { ["type"] = "enum", ["name"] = enumeration.ToString() },
        ValueList<string> strings => new JsonObject { ["type"] = "strings", ["items"] = new JsonArray(strings.Items.Select(s => (JsonNode)s).ToArray()) },
        ValueList<FontFeatureSetting> features => new JsonObject { ["type"] = "fontFeatures", ["items"] = new JsonArray(features.Items.Select(feature => (JsonNode)new JsonObject { ["tag"] = feature.Tag, ["value"] = feature.Value }).ToArray()) },
        ValueList<FontVariationSetting> axes => new JsonObject
        {
            ["type"] = "fontVariations", ["items"] = new JsonArray(axes.Items.Select(axis => (JsonNode)new JsonObject
            {
                ["tag"] = axis.Tag, ["value"] = axis.Value,
            }).ToArray()),
        },
        ValueList<FontSymbol> symbols => new JsonObject
        {
            ["type"] = "fontSymbols", ["items"] = new JsonArray(symbols.Items.Select(symbol => (JsonNode)new JsonObject
            {
                ["family"] = symbol.FontFamily, ["glyph"] = symbol.Glyph,
            }).ToArray()),
        },
        ValueList<TransitionDefinition> transitions => new JsonObject
        {
            ["type"] = "transitions",
            ["items"] = new JsonArray(transitions.Items.Select(t => (JsonNode)new JsonObject
            {
                ["property"] = t.Property,
                ["durationTicks"] = t.Duration.Ticks,
                ["easing"] = new JsonObject { ["x1"] = t.Easing.X1, ["y1"] = t.Easing.Y1, ["x2"] = t.Easing.X2, ["y2"] = t.Easing.Y2 },
                ["delayTicks"] = t.Delay.Ticks,
            }).ToArray()),
        },
        ValueList<AnimationDefinition> animations => new JsonObject
        {
            ["type"] = "animations",
            ["items"] = new JsonArray(animations.Items.Select(a =>
            {
                var json = new JsonObject
                {
                    ["durationTicks"] = a.Duration.Ticks, ["easing"] = Easing(a.Easing), ["delayTicks"] = a.Delay.Ticks,
                    ["iterations"] = double.IsPositiveInfinity(a.IterationCount) ? "infinite" : a.IterationCount,
                    ["direction"] = a.Direction.ToString(), ["fill"] = a.FillMode.ToString(), ["paused"] = a.Paused,
                };
                if (a.Name is not null) json["name"] = a.Name;
                return (JsonNode)json;
            }).ToArray()),
        },
        _ => throw new NotSupportedException($"Cannot serialize a value of type {value.GetType().Name}."),
    };

    private static JsonObject Image(ImageValue value)
    {
        if (value is UrlImage url) return new JsonObject { ["kind"] = "url", ["url"] = url.Url };
        var image = (Gradient)value;
        var stops = new JsonArray(image.Stops.Items.Select(s =>
        {
            var stop = new JsonObject { ["color"] = Value(s.Color) };
            if (s.Position is { } position) stop["position"] = Length(position);
            return (JsonNode)stop;
        }).ToArray());
        var result = image switch
        {
            LinearGradient l => l.Angle is { } angle
                ? new JsonObject { ["kind"] = "linear", ["angle"] = angle, ["toX"] = l.ToX, ["toY"] = l.ToY, ["stops"] = stops }
                : new JsonObject { ["kind"] = "linear", ["toX"] = l.ToX, ["toY"] = l.ToY, ["stops"] = stops },
            RadialGradient r => new JsonObject
            {
                ["kind"] = "radial", ["shape"] = r.Shape.ToString(), ["extent"] = r.Extent.ToString(), ["sizeX"] = Length(r.SizeX), ["sizeY"] = Length(r.SizeY),
                ["centerX"] = Length(r.CenterX), ["centerY"] = Length(r.CenterY), ["stops"] = stops,
            },
            ConicGradient c => new JsonObject { ["kind"] = "conic", ["angle"] = c.Angle,
                ["centerX"] = Length(c.CenterX), ["centerY"] = Length(c.CenterY), ["stops"] = stops },
            _ => throw new NotSupportedException($"Cannot serialize an image of type {image.GetType().Name}."),
        };
        if (image.Repeating) result["repeating"] = true;
        return result;
    }

    private static JsonArray Tracks(ValueList<TrackSize> tracks) => new(tracks.Items.Select(t => (JsonNode)new JsonObject
    {
        ["min"] = Breadth(t.Min), ["max"] = Breadth(t.Max), ["fitContent"] = t.FitContent,
    }).ToArray());

    private static JsonObject Breadth(TrackBreadth breadth) => new() { ["kind"] = breadth.Kind.ToString(), ["length"] = Length(breadth.Length), ["flex"] = breadth.Flex };

    private static JsonObject Length(Length length) => length.Expression is { } expression
        ? new() { ["type"] = "length", ["unit"] = length.Unit.ToString(), ["calc"] = Calc(expression.Root), ["nonNegative"] = expression.NonNegative }
        : new() { ["type"] = "length", ["value"] = length.Value, ["unit"] = length.Unit.ToString() };

    private static JsonObject TransformFunction(TransformFunction function) => function switch
    {
        TranslateFunction t => new() { ["kind"] = "translate", ["x"] = Length(t.X), ["y"] = Length(t.Y) },
        ScaleFunction s => new() { ["kind"] = "scale", ["x"] = s.X, ["y"] = s.Y },
        RotateFunction r => new() { ["kind"] = "rotate", ["angle"] = r.Angle },
        SkewFunction k => new() { ["kind"] = "skew", ["x"] = k.X, ["y"] = k.Y },
        MatrixFunction m => new() { ["kind"] = "matrix", ["values"] = new JsonArray(m.A, m.B, m.C, m.D, m.E, m.F) },
        _ => throw new NotSupportedException($"Cannot serialize a transform function of type {function.GetType().Name}."),
    };

    private static TransformFunction ReadTransformFunction(JsonObject function)
    {
        var kind = String(function, "kind");
        switch (kind)
        {
            case "translate": return new TranslateFunction(ReadLength(Object(function, "x")), ReadLength(Object(function, "y")));
            case "scale": return new ScaleFunction(Value<double>(function, "x"), Value<double>(function, "y"));
            case "rotate": return new RotateFunction(Value<double>(function, "angle"));
            case "skew": return new SkewFunction(Value<double>(function, "x"), Value<double>(function, "y"));
            case "matrix":
                var values = Array(function, "values").Select(Value<double>).ToArray();
                if (values.Length != 6) throw Invalid(function, "a matrix has six values");
                return new MatrixFunction(values[0], values[1], values[2], values[3], values[4], values[5]);
            default:
                throw Invalid(function, $"'{kind}' is not a transform function");
        }
    }

    private static JsonObject Calc(CalcNode node) => node switch
    {
        CalcNumber number => new() { ["kind"] = "number", ["value"] = number.Value },
        CalcLength length => new() { ["kind"] = "length", ["length"] = Length(length.Length) },
        CalcOperation operation => new() { ["kind"] = operation.Operator.ToString(), ["left"] = Calc(operation.Left), ["right"] = Calc(operation.Right) },
        CalcFunction function => new() { ["kind"] = function.Kind.ToString(), ["arguments"] = new JsonArray(function.Arguments.Items.Select(a => (JsonNode)Calc(a)).ToArray()) },
        _ => throw new NotSupportedException($"Cannot serialize a calculation node of type {node.GetType().Name}."),
    };

    private static JsonArray Tokens(IReadOnlyList<StyleToken> tokens) =>
        new(tokens.Select(t => (JsonNode)new JsonObject { ["kind"] = t.Kind.ToString(), ["value"] = t.Value, ["number"] = t.Number }).ToArray());

    private static StyleRule ReadRule(JsonObject rule)
    {
        var order = Value<int>(rule, "order");
        var selector = ReadSelector(Object(rule, "selector"));
        var media = rule["media"] is { } node ? ReadMedia(Object(node)) : null;
        var declarations = new List<StyleDeclaration>();
        foreach (var item in Array(rule, "declarations")) declarations.Add(ReadDeclaration(Object(item)));
        return new StyleRule(selector, media, declarations, order);
    }

    private static Selector ReadSelector(JsonObject selector)
    {
        var compounds = new List<CompoundSelector>();
        foreach (var item in Array(selector, "compounds"))
        {
            var compound = Object(item);
            var states = ElementStates.None;
            foreach (var state in Array(compound, "states")) states |= ReadEnum<ElementStates>(state);
            var attributes = compound["attributes"] is { } list
                ? Array(compound, "attributes").Select(item =>
                {
                    var attribute = Object(item);
                    return new AttributeSelector(String(attribute, "name"), ReadEnum<AttributeMatch>(Required(attribute, "match")), OptionalString(attribute, "value"),
                        Value<bool>(attribute, "ignoreCase"));
                }).ToArray()
                : [];
            var pseudos = compound["pseudos"] is not null ? Array(compound, "pseudos").Select(item => ReadPseudo(Object(item))).ToArray() : [];
            compounds.Add(new CompoundSelector(OptionalString(compound, "type"), OptionalString(compound, "id"), Strings(compound, "classes"), states, attributes, pseudos,
                compound["pseudoElement"] is { } target ? ReadEnum<PseudoElement>(target) : PseudoElement.None));
        }
        var combinators = Array(selector, "combinators").Select(ReadEnum<Combinator>).ToArray();
        if (combinators.Length != compounds.Count - 1) throw Invalid(selector, "a selector needs one combinator between each pair of compounds");
        return new Selector(compounds, combinators);
    }

    private static PseudoSelector ReadPseudo(JsonObject pseudo)
    {
        var kind = String(pseudo, "kind");
        return kind switch
        {
            "nth" => new NthSelector(Value<int>(pseudo, "a"), Value<int>(pseudo, "b"), Value<bool>(pseudo, "fromEnd"), Value<bool>(pseudo, "ofType"), String(pseudo, "name"))
            {
                Filter = pseudo["filter"] is null ? null : new ValueList<Selector>(Array(pseudo, "filter").Select(item => ReadSelector(Object(item)))),
            },
            "only" => new OnlyChildSelector(Value<bool>(pseudo, "ofType")),
            "empty" => new EmptySelector(),
            "root" => new RootSelector(),
            "has" => ReadHas(pseudo),
            "component-root" => new ComponentRootSelector(),
            "item" => new ItemPositionSelector(Value<int>(pseudo, "a"), Value<int>(pseudo, "b"), Value<bool>(pseudo, "fromEnd"), String(pseudo, "name")),
            "part" => CustomStateSet.IsValidName(String(pseudo, "component")) && CustomStateSet.IsValidName(String(pseudo, "name"))
                ? new PartSelector(String(pseudo, "component"), String(pseudo, "name")) : throw Invalid(pseudo, "invalid public part name"),
            "state" => CustomStateSet.IsValidName(String(pseudo, "name")) ? new NamedStateSelector(String(pseudo, "name")) : throw Invalid(pseudo, "invalid custom state name"),
            "control" => ControlStateSelector.Names.Contains(String(pseudo, "name"), StringComparer.OrdinalIgnoreCase)
                ? new ControlStateSelector(String(pseudo, "name")) : throw Invalid(pseudo, "unknown control-state pseudo-class"),
            "logical" => new LogicalSelector(ReadEnum<LogicalPseudo>(Required(pseudo, "logical")),
                new ValueList<Selector>(Array(pseudo, "selectors").Select(item => ReadSelector(Object(item))).ToArray())),
            _ => throw Invalid(pseudo["kind"], $"unknown pseudo-class kind '{kind}'"),
        };
    }

    private static HasSelector ReadHas(JsonObject pseudo)
    {
        var relatives = Array(pseudo, "selectors").Select(item =>
        {
            var relative = Object(item);
            var relation = ReadEnum<Combinator>(Required(relative, "relation"));
            var selector = ReadSelector(Object(relative, "selector"));
            if (relation == Combinator.Deep || !Allowed(selector)) throw Invalid(relative, "unsupported has argument");
            return new RelativeSelector(relation, selector);
        }).ToArray();
        if (relatives.Length == 0) throw Invalid(pseudo, "has requires relative selectors");
        return new HasSelector(new ValueList<RelativeSelector>(relatives));

        static bool Allowed(Selector selector) => !selector.HasDeep && !selector.ContainsHas && selector.Compounds.All(c =>
            c.PseudoElement == PseudoElement.None && c.Pseudos.All(p => p switch
            {
                LogicalSelector l => l.Selectors.Items.All(Allowed),
                NthSelector { Filter: { } f } => f.Items.All(Allowed),
                PartSelector => false,
                _ => true,
            }));
    }

    private static MediaQuery ReadMedia(JsonObject media)
    {
        var conditions = new List<MediaCondition>();
        foreach (var item in Array(media, "conditions"))
        {
            var condition = Object(item);
            conditions.Add(new MediaCondition(OptionalDouble(condition, "minWidth"), OptionalDouble(condition, "maxWidth"),
                OptionalDouble(condition, "minHeight"), OptionalDouble(condition, "maxHeight"),
                condition["prefersColorScheme"] is { } scheme ? ReadEnum<ColorScheme>(scheme) : null,
                condition["prefersReducedMotion"] is { } reduced ? Value<bool>(reduced) : null,
                condition["forcedColors"] is { } forced ? Value<bool>(forced) : null,
                condition["prefersMoreContrast"] is { } more ? Value<bool>(more) : null,
                condition["platform"] is { } platform ? ReadEnum<PlatformKind>(platform) : null,
                condition["density"] is { } density ? ReadEnum<ControlDensity>(density) : null));
        }
        return new MediaQuery(conditions);
    }

    private static StyleDeclaration ReadDeclaration(JsonObject declaration)
    {
        var kind = String(declaration, "kind");
        switch (kind)
        {
            case "value":
            {
                var property = Property(declaration);
                var value = ReadValue(Object(declaration, "value"), property);
                if (!property.ValueType.IsInstanceOfType(value))
                    throw Invalid(declaration["value"], $"a value of type {value.GetType().Name} is not valid for '{property.Name}'");
                return new ValueDeclaration(property, value);
            }
            case "keyword":
                return new KeywordDeclaration(Property(declaration), ReadEnum<GlobalKeyword>(Required(declaration, "keyword")));
            case "pending":
                return new PendingDeclaration(String(declaration, "property"), ReadTokens(declaration));
            case "custom":
                return new CustomPropertyDeclaration(String(declaration, "name"), ReadTokens(declaration));
            default:
                throw Invalid(declaration["kind"], $"unknown declaration kind '{kind}'");
        }
    }

    private static StyleProperty Property(JsonObject declaration)
    {
        var name = String(declaration, "property");
        return StyleProperties.TryGet(name, out var property) ? property : throw Invalid(declaration["property"], $"unknown property '{name}'");
    }

    private static object ReadValue(JsonObject value, StyleProperty property)
    {
        var type = String(value, "type");
        switch (type)
        {
            case "time":
            {
                var ticks = Value<long>(value, "ticks");
                if (ticks < 0) throw Invalid(value, "expected a non-negative time");
                return TimeSpan.FromTicks(ticks);
            }
            case "easing":
                return ReadEasing(Object(value, "value"));
            case "length":
                return ReadLength(value);
            case "size":
                return new SizeValue(ReadEnum<SizeKind>(Required(value, "kind")), ReadLength(Object(value, "length")));
            case "nativeColor":
                return value["value"] is JsonObject nativeValue ? new NativeColor((Color)ReadValue(nativeValue, property)) : NativeColor.Auto;
            case "color":
                return new Color(Value<byte>(value, "r"), Value<byte>(value, "g"), Value<byte>(value, "b"), Value<byte>(value, "a"), Value<bool>(value, "currentColor"));
            case "lineHeight":
                return new LineHeight(ReadEnum<LineHeightKind>(Required(value, "kind")), Value<double>(value, "number"), ReadLength(Object(value, "length")));
            case "scrollWheelStep":
            {
                var kind = ReadEnum<ScrollWheelStepKind>(Required(value, "kind"));
                var lines = Value<double>(value, "lines");
                var length = ReadLength(Object(value, "length"));
                if (!double.IsFinite(lines) || lines < 0 || length.IsPercent || !double.IsFinite(length.Value) || length.Value < 0)
                    throw Invalid(value, "expected a non-negative wheel distance without percentages");
                return new ScrollWheelStep(kind, length, lines);
            }
            case "frameShadows":
            {
                if (value["value"] is null) return default(WindowFrameShadows);
                var frameShadows = Object(value, "value");
                if (String(frameShadows, "type") != "shadows") throw Invalid(frameShadows, "expected a list of shadows");
                return new WindowFrameShadows((ValueList<BoxShadow>)ReadValue(frameShadows, StyleProperties.BoxShadow));
            }
            case "shadows":
                return new ValueList<BoxShadow>(Array(value, "items").Select(item =>
                {
                    var shadow = Object(item);
                    var color = Object(shadow, "color");
                    return new BoxShadow(Value<bool>(shadow, "inset"), ReadLength(Object(shadow, "x")), ReadLength(Object(shadow, "y")),
                        ReadLength(Object(shadow, "blur")), ReadLength(Object(shadow, "spread")),
                        new Color(Value<byte>(color, "r"), Value<byte>(color, "g"), Value<byte>(color, "b"), Value<byte>(color, "a"), Value<bool>(color, "currentColor")));
                }).ToArray());
            case "images":
                return new ValueList<ImageValue>(Array(value, "items").Select(item => ReadImage(Object(item))).ToArray());
            case "positions":
                return new ValueList<PositionValue>(Array(value, "items").Select(item =>
                    new PositionValue(ReadLength(Object(Object(item), "x")), ReadLength(Object(Object(item), "y")))).ToArray());
            case "backgroundSizes":
                return new ValueList<BackgroundSize>(Array(value, "items").Select(item =>
                {
                    var size = Object(item);
                    SizeValue Side(string name) => new(ReadEnum<SizeKind>(Required(Object(size, name), "kind")), ReadLength(Object(Object(size, name), "length")));
                    return new BackgroundSize(ReadEnum<BackgroundSizeKind>(Required(size, "kind")), Side("width"), Side("height"));
                }).ToArray());
            case "repeats":
                return new ValueList<BackgroundRepeat>(Array(value, "items").Select(item => ReadEnum<BackgroundRepeat>(item)).ToArray());
            case "radius":
                return new RadiusValue(ReadLength(Object(value, "x")), ReadLength(Object(value, "y")));
            case "clipPath":
            {
                RadiusValue Radius(string name) => value[name] is { } radius ? (RadiusValue)ReadValue(Object(radius), property) : RadiusValue.Zero;
                return new ClipPathValue(ReadLength(Object(value, "top")), ReadLength(Object(value, "right")),
                    ReadLength(Object(value, "bottom")), ReadLength(Object(value, "left")), Value<bool>(value, "none"))
                {
                    TopLeftRadius = Radius("topLeftRadius"), TopRightRadius = Radius("topRightRadius"),
                    BottomRightRadius = Radius("bottomRightRadius"), BottomLeftRadius = Radius("bottomLeftRadius"),
                };
            }
            case "blendModes":
                return new ValueList<BlendMode>(Array(value,"items").Select(ReadEnum<BlendMode>));
            case "filters":
                return new FilterValue(new ValueList<FilterFunction>(Array(value, "functions").Select(item =>
                {
                    var function = Object(item);
                    return new FilterFunction(ReadEnum<FilterKind>(Required(function, "kind")), Value<double>(function, "amount"), ReadLength(Object(function, "radius")));
                })));
            case "transforms":
                return new ValueList<TransformFunction>(Array(value, "items").Select(item => ReadTransformFunction(Object(item))).ToArray());
            case "transformOrigin":
                return new TransformOrigin(ReadLength(Object(value, "x")), ReadLength(Object(value, "y")));
            case "position":
                return new PositionValue(ReadLength(Object(value, "x")), ReadLength(Object(value, "y")));
            case "aspectRatio":
                return new AspectRatio(Value<double>(value, "ratio"));
            case "zIndex":
                return new ZIndex(Value<bool>(value, "auto"), Value<int>(value, "value"));
            case "gridLine":
                return new GridLine(ReadEnum<GridLineKind>(Required(value, "kind")), Value<int>(value, "value"), OptionalString(value, "name"));
            case "trackList":
                return new GridTrackList(ReadTracks(value, "tracks"), ReadEnum<GridAutoRepeat>(Required(value, "repeat")), Value<int>(value, "repeatIndex"), ReadTracks(value, "repeatTracks"));
            case "trackSizes":
                return ReadTracks(value, "items");
            case "gridAreas":
                try
                {
                    return new GridAreas(Strings(value, "rows"));
                }
                catch (ArgumentException exception)
                {
                    throw Invalid(value["rows"], exception.Message.TrimEnd('.'));
                }
            case "number":
                return Value<double>(value, "value");
            case "boolean":
                return Value<bool>(value, "value");
            case "integer":
                return Value<int>(value, "value");
            case "enum":
            {
                var name = String(value, "name");
                if (!property.ValueType.IsEnum || !System.Enum.TryParse(property.ValueType, name, ignoreCase: true, out var result) || result is null || !IsDefined(property.ValueType, result))
                    throw Invalid(value["name"], $"'{name}' is not a value of '{property.Name}'");
                return result;
            }
            case "strings":
                return new ValueList<string>(Strings(value, "items"));
            case "fontFeatures":
                return FontFeatureSetting.Canonicalize(Array(value,"items").Select(item =>
                {
                    var feature = Object(item);
                    var tag = String(feature,"tag");
                    if (!FontVariationSetting.IsValidTag(tag)) throw Invalid(feature,"Invalid font feature tag");
                    return new FontFeatureSetting(tag,Value<uint>(feature,"value"));
                }));
            case "fontVariations":
            {
                var axes = new List<FontVariationSetting>();
                foreach (var item in Array(value, "items"))
                {
                    var axis = Object(item);
                    var tag = String(axis, "tag");
                    var coordinate = Value<double>(axis, "value");
                    if (!FontVariationSetting.IsValidTag(tag) || !double.IsFinite(coordinate)) throw Invalid(axis, "Invalid font variation coordinate");
                    axes.Add(new(tag, coordinate));
                }
                return FontVariationSetting.Canonicalize(axes);
            }
            case "fontSymbols":
            {
                var symbols = Array(value, "items").Select(item =>
                {
                    var symbol = Object(item);
                    return new FontSymbol(String(symbol, "family"), String(symbol, "glyph"));
                }).ToArray();
                if (symbols.Length > FontSymbols.MaximumCandidates || symbols.Any(symbol => string.IsNullOrWhiteSpace(symbol.FontFamily) ||
                    symbol.FontFamily.Any(char.IsControl) || string.IsNullOrWhiteSpace(symbol.Glyph) || symbol.Glyph.Any(char.IsControl)))
                    throw Invalid(value, "Invalid font-symbol candidates");
                return new ValueList<FontSymbol>(symbols);
            }
            case "animations":
                return new ValueList<AnimationDefinition>(Array(value, "items").Select(item =>
                {
                    var animation = Object(item);
                    var iterations = Required(animation, "iterations");
                    return new AnimationDefinition(OptionalString(animation, "name"), TimeSpan.FromTicks(Value<long>(animation, "durationTicks")),
                        ReadEasing(Object(animation, "easing")), TimeSpan.FromTicks(Value<long>(animation, "delayTicks")),
                        iterations is JsonValue text && text.TryGetValue<string>(out var word) && word == "infinite" ? double.PositiveInfinity : Value<double>(iterations),
                        ReadEnum<AnimationDirection>(Required(animation, "direction")), ReadEnum<AnimationFillMode>(Required(animation, "fill")), Value<bool>(animation, "paused"));
                }).ToArray());
            case "transitions":
            {
                var items = new List<TransitionDefinition>();
                foreach (var item in Array(value, "items"))
                {
                    var transition = Object(item);
                    var easing = Object(transition, "easing");
                    items.Add(new TransitionDefinition(String(transition, "property"), TimeSpan.FromTicks(Value<long>(transition, "durationTicks")),
                        new EasingFunction(Value<double>(easing, "x1"), Value<double>(easing, "y1"), Value<double>(easing, "x2"), Value<double>(easing, "y2")),
                        TimeSpan.FromTicks(Value<long>(transition, "delayTicks"))));
                }
                return new ValueList<TransitionDefinition>(items);
            }
            default:
                throw Invalid(value["type"], $"unknown value type '{type}'");
        }
    }

    private static ImageValue ReadImage(JsonObject image)
    {
        if (image["kind"] is JsonValue kindValue && kindValue.TryGetValue<string>(out var imageKind) && imageKind == "url") return new UrlImage(String(image, "url"));
        var stops = new ValueList<ColorStop>(Array(image, "stops").Select(item =>
        {
            var stop = Object(item);
            var color = Object(stop, "color");
            return new ColorStop(new Color(Value<byte>(color, "r"), Value<byte>(color, "g"), Value<byte>(color, "b"), Value<byte>(color, "a"), Value<bool>(color, "currentColor")),
                stop["position"] is { } position ? ReadLength(Object(position)) : null);
        }).ToArray());
        var kind = String(image, "kind");
        Gradient result = kind switch
        {
            "linear" => new LinearGradient(OptionalDouble(image, "angle"), Value<int>(image, "toX"), Value<int>(image, "toY"), stops),
            "radial" => new RadialGradient(ReadEnum<RadialShape>(Required(image, "shape")), ReadEnum<RadialExtent>(Required(image, "extent")),
                ReadLength(Object(image, "sizeX")), ReadLength(Object(image, "sizeY")), ReadLength(Object(image, "centerX")), ReadLength(Object(image, "centerY")), stops),
            "conic" => new ConicGradient(Value<double>(image, "angle"), ReadLength(Object(image, "centerX")), ReadLength(Object(image, "centerY")), stops),
            _ => throw Invalid(image["kind"], $"unknown image kind '{kind}'"),
        };
        return result with { Repeating = image["repeating"] is not null && Value<bool>(image, "repeating") };
    }

    private static ValueList<TrackSize> ReadTracks(JsonObject parent, string name) => new(Array(parent, name).Select(item =>
    {
        var track = Object(item);
        return new TrackSize(ReadBreadth(Object(track, "min")), ReadBreadth(Object(track, "max")), Value<bool>(track, "fitContent"));
    }).ToArray());

    private static TrackBreadth ReadBreadth(JsonObject breadth) =>
        new(ReadEnum<TrackBreadthKind>(Required(breadth, "kind")), ReadLength(Object(breadth, "length")), Value<double>(breadth, "flex"));

    private static Length ReadLength(JsonObject length) => ReadEnum<LengthUnit>(Required(length, "unit")) is var unit && unit == LengthUnit.Calc
        ? Style.Length.Calc(new CalcExpression(ReadCalc(Object(length, "calc")), Value<bool>(length, "nonNegative")))
        : new(Value<double>(length, "value"), unit);

    private static CalcNode ReadCalc(JsonObject node)
    {
        var kind = String(node, "kind");
        switch (kind)
        {
            case "number":
                return new CalcNumber(Value<double>(node, "value"));
            case "length":
                var length = ReadLength(Object(node, "length"));
                if (length.Unit == LengthUnit.Calc) throw Invalid(node, "a calculation leaf cannot be a calculation");
                return new CalcLength(length);
        }
        if (Enum.TryParse<CalcOperator>(kind, out var op) && Enum.IsDefined(op))
            return new CalcOperation(op, ReadCalc(Object(node, "left")), ReadCalc(Object(node, "right")));
        if (Enum.TryParse<CalcFunctionKind>(kind, out var function) && Enum.IsDefined(function))
        {
            var arguments = Array(node, "arguments").Select(a => ReadCalc(Object(a))).ToArray();
            if (arguments.Length == 0 || (function == CalcFunctionKind.Clamp && arguments.Length != 3)) throw Invalid(node, $"wrong number of arguments for {kind}");
            return new CalcFunction(function, new ValueList<CalcNode>(arguments));
        }
        throw Invalid(node, $"'{kind}' is not a calculation node");
    }

    private static StyleToken[] ReadTokens(JsonObject declaration) => Array(declaration, "tokens").Select(item =>
    {
        var token = Object(item);
        return new StyleToken(ReadEnum<StyleTokenKind>(Required(token, "kind")), String(token, "value"), Value<double>(token, "number"), 0, 0);
    }).ToArray();

    private static InvalidDataException Invalid(JsonNode? node, string message) => new($"{node?.GetPath() ?? "$"}: {message}.");

    private static JsonObject Object(JsonNode? node) => node as JsonObject ?? throw Invalid(node, "expected an object");

    private static JsonObject Object(JsonObject parent, string name) => Object(Required(parent, name));

    private static JsonNode Required(JsonObject parent, string name) => parent[name] ?? throw Invalid(parent, $"missing '{name}'");

    private static JsonArray Array(JsonObject parent, string name) => Required(parent, name) as JsonArray ?? throw Invalid(parent[name], "expected an array");

    private static string String(JsonObject parent, string name) => Value<string>(parent, name);

    private static string? OptionalString(JsonObject parent, string name) => parent[name] is { } node ? Value<string>(node) : null;

    private static double? OptionalDouble(JsonObject parent, string name) => parent[name] is { } node ? Value<double>(node) : null;

    private static string[] Strings(JsonObject parent, string name) => Array(parent, name).Select(Value<string>).ToArray();

    private static T Value<T>(JsonObject parent, string name) => Value<T>(Required(parent, name));

    private static T Value<T>(JsonNode? node)
    {
        if (node is JsonValue value)
        {
            if (value.TryGetValue<T>(out var result)) return result;

            if (typeof(T) != typeof(string) && value.TryGetValue<object>(out var boxed) && boxed is IConvertible)
            {
                try
                {
                    return (T)Convert.ChangeType(boxed, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
                {
                }
            }
        }
        throw Invalid(node, $"expected a {typeof(T).Name}");
    }

    private static bool IsDefined(Type type, object value)
    {
        if (System.Enum.IsDefined(type, value)) return true;
        if (!type.IsDefined(typeof(FlagsAttribute), inherit: false)) return false;
        var all = System.Enum.GetValuesAsUnderlyingType(type).Cast<object>().Aggregate(0L, (bits, flag) => bits | Convert.ToInt64(flag, System.Globalization.CultureInfo.InvariantCulture));
        return (Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture) & ~all) == 0;
    }

    private static T ReadEnum<T>(JsonNode? node) where T : struct, Enum
    {
        var name = Value<string>(node);
        return System.Enum.TryParse<T>(name, ignoreCase: true, out var result) && System.Enum.IsDefined(result) ? result : throw Invalid(node, $"'{name}' is not a {typeof(T).Name}");
    }
}
