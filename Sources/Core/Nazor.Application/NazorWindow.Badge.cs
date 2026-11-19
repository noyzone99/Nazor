using Nazor.Graphics;
using Nazor.Platform;
using Nazor.Presentation;
using Nazor.Style;
using Nss = Nazor.Style;
using BadgeFit = Nazor.Platform.WindowsBadgeImageFit;
using P = Nazor.Style.StyleProperties;

namespace Nazor;

public sealed partial class NazorWindow
{
    private PositionValue? badgeImagePosition;
    private Func<Size, Point>? badgePositionResolver;
    private (object?[] Key, DisplayList List)? badgeBackgroundLayers, badgeMaskLayers;
    private readonly HashSet<Task<LoadedImage>> reportedBadgeImages = [];

    private void ApplyBadgeAppearance(ComputedStyle style)
    {
        float Length(StyleProperty<Nss.Length> property, float basis = 16) => (float)style.Get(property).Resolve(basis);
        Graphics.Color Color(StyleProperty<Nss.Color> property) => Convert(style.Get(property));
        static Graphics.Color Convert(Nss.Color value) => new(value.R, value.G, value.B, value.A);
        Graphics.Color? Native(StyleProperty<NativeColor> property) => style.Get(property).Value is { } value ? Convert(value) : null;
        var size = Length(P.WindowsBadgeSize);
        float Dimension(StyleProperty<SizeValue> property) => Math.Clamp(style.Get(property) is { Kind: SizeKind.Auto } ? size : (float)style.Get(property).Length.Resolve(16), 0, 16);
        var width = Dimension(P.WindowsBadgeWidth);
        var height = Dimension(P.WindowsBadgeHeight);
        Radius Radius(StyleProperty<RadiusValue> property)
        {
            var value = style.Get(property);
            return new((float)value.X.Resolve(width), (float)value.Y.Resolve(height));
        }
        var position = style.Get(P.WindowsBadgeImagePosition);
        if (badgeImagePosition != position)
        {
            badgeImagePosition = position;
            badgePositionResolver = free => new((float)position.X.Resolve(free.Width), (float)position.Y.Resolve(free.Height));
        }
        static float Absolute(Nss.Length value) => (float)value.Resolve(0);
        static float Fraction(Nss.Length value) => (float)(value.Resolve(1) - value.Resolve(0));
        IReadOnlyList<DrawShadow> Shadows(StyleProperty<ValueList<BoxShadow>> property) => style.Get(property).Items.Select(value =>
            new DrawShadow(default, default, new((float)value.X.Resolve(16), (float)value.Y.Resolve(16)),
                (float)value.Blur.Resolve(16), (float)value.Spread.Resolve(16), Convert(value.Color), value.Inset)).ToArray();
        float BorderWidth(StyleProperty<Nss.Length> property, StyleProperty<Nss.BorderStyle> line, float basis) =>
            style.Get(line) == Nss.BorderStyle.None ? 0 : Math.Clamp(Length(property), 0, basis / 2);
        var borders = new Thickness(BorderWidth(P.WindowsBadgeBorderLeftWidth, P.WindowsBadgeBorderLeftStyle, width),
            BorderWidth(P.WindowsBadgeBorderTopWidth, P.WindowsBadgeBorderTopStyle, height),
            BorderWidth(P.WindowsBadgeBorderRightWidth, P.WindowsBadgeBorderRightStyle, width),
            BorderWidth(P.WindowsBadgeBorderBottomWidth, P.WindowsBadgeBorderBottomStyle, height));
        BorderSide Side(StyleProperty<Nss.Color> color, StyleProperty<Nss.BorderStyle> line) => new(Color(color),
            style.Get(line) switch { Nss.BorderStyle.Dashed => LineStyle.Dashed, Nss.BorderStyle.Dotted => LineStyle.Dotted, _ => LineStyle.Solid });
        var radius = new CornerRadius(Radius(P.WindowsBadgeBorderTopLeftRadius), Radius(P.WindowsBadgeBorderTopRightRadius),
            Radius(P.WindowsBadgeBorderBottomRightRadius), Radius(P.WindowsBadgeBorderBottomLeftRadius));
        var bounds = new Rect((16 - width) / 2 + Length(P.WindowsBadgeOffsetX), (16 - height) / 2 + Length(P.WindowsBadgeOffsetY), width, height);
        var origin = style.Get(P.WindowsBadgeTransformOrigin);
        var x = bounds.X + (float)origin.X.Resolve(width);
        var y = bounds.Y + (float)origin.Y.Resolve(height);
        var transform = Matrix.Translation(-x, -y).Then(Nazor.Presentation.Scene.Compose(style.Get(P.WindowsBadgeTransform), bounds)).Then(Matrix.Translation(x, y));
        platformWindow!.SetWindowsBadgeAppearance(new()
        {
            Fonts = Scene.Fonts, FontVersion = Scene.Fonts.Version,
            Background = BadgeLayers(style, bounds, radius, borders, false),
            Mask = BadgeLayers(style, bounds, radius, borders, true),
            ClipBackgroundToPadding = style.Get(P.WindowsBadgeBackgroundClip), Transform = transform,
            BackgroundColor = Native(P.WindowsBadgeBackgroundColor), Color = Color(P.WindowsBadgeColor), Width = width, Height = height,
            Offset = new(Length(P.WindowsBadgeOffsetX), Length(P.WindowsBadgeOffsetY)), Opacity = (float)style.Get(P.WindowsBadgeOpacity),
            Padding = new(Length(P.WindowsBadgePaddingLeft, width), Length(P.WindowsBadgePaddingTop, height), Length(P.WindowsBadgePaddingRight, width), Length(P.WindowsBadgePaddingBottom, height)),
            BorderWidths = borders,
            BorderTop = Side(P.WindowsBadgeBorderTopColor, P.WindowsBadgeBorderTopStyle),
            BorderRight = Side(P.WindowsBadgeBorderRightColor, P.WindowsBadgeBorderRightStyle),
            BorderBottom = Side(P.WindowsBadgeBorderBottomColor, P.WindowsBadgeBorderBottomStyle),
            BorderLeft = Side(P.WindowsBadgeBorderLeftColor, P.WindowsBadgeBorderLeftStyle), Radius = radius,
            FontFamilies = style.Get(P.WindowsBadgeFontFamily).Items.ToArray(), FontSize = Length(P.WindowsBadgeFontSize, height),
            FontWeight = style.Get(P.WindowsBadgeFontWeight), Italic = style.Get(P.WindowsBadgeFontStyle) == Nss.FontStyle.Italic,
            LetterSpacing = Length(P.WindowsBadgeLetterSpacing),
            TextAlign = style.Get(P.WindowsBadgeTextAlign) switch { Nss.TextAlign.Left or Nss.TextAlign.Start => 0, Nss.TextAlign.Right or Nss.TextAlign.End => 1, _ => 0.5f },
            VerticalAlign = (float)style.Get(P.WindowsBadgeVerticalAlign), TextFit = style.Get(P.WindowsBadgeTextFit),
            TextStrokeWidth = Length(P.WindowsBadgeTextStrokeWidth), TextStrokeColor = Color(P.WindowsBadgeTextStrokeColor),
            ImageBackgroundColor = Native(P.WindowsBadgeImageBackgroundColor),
            ImageFit = style.Get(P.WindowsBadgeImageFit) switch { Nss.ObjectFit.Fill => BadgeFit.Fill, Nss.ObjectFit.Cover => BadgeFit.Cover,
                Nss.ObjectFit.None => BadgeFit.None, Nss.ObjectFit.ScaleDown => BadgeFit.ScaleDown, _ => BadgeFit.Contain },
            ImagePosition = new(Fraction(position.X), Fraction(position.Y)), ImageOffset = new(Absolute(position.X), Absolute(position.Y)),
            ImagePositionResolver = badgePositionResolver,
            ImageOpacity = (float)style.Get(P.WindowsBadgeImageOpacity), ClipImage = style.Get(P.WindowsBadgeImageClip),
            Shadows = Shadows(P.WindowsBadgeBoxShadow), TextShadows = Shadows(P.WindowsBadgeTextShadow),
        });
    }

    private DisplayList? BadgeLayers(ComputedStyle style, Rect bounds, CornerRadius radius, Thickness borders, bool mask)
    {
        var images = style.Get(mask ? P.WindowsBadgeMaskImage : P.WindowsBadgeBackgroundImage);
        if (images.Items.Count == 0) return null;
        var positions = mask ? new ValueList<PositionValue>([PositionValue.Center]) : style.Get(P.WindowsBadgeBackgroundPosition);
        var sizes = mask ? new ValueList<BackgroundSize>([new(BackgroundSizeKind.Explicit, SizeValue.Fixed(Nss.Length.Percent(100)), SizeValue.Fixed(Nss.Length.Percent(100)))])
            : style.Get(P.WindowsBadgeBackgroundSize);
        var repeats = mask ? new ValueList<BackgroundRepeat>([BackgroundRepeat.NoRepeat]) : style.Get(P.WindowsBadgeBackgroundRepeat);
        var inside = !mask && style.Get(P.WindowsBadgeBackgroundClip);
        var area = inside ? bounds.Deflate(borders) : bounds;
        var corners = mask ? default : inside ? radius.Deflate(borders) : radius;
        var loaded = new Dictionary<string, LoadedImage?>();
        foreach (var url in images.Items.OfType<UrlImage>())
        {
            try
            {
                var task = Scene.LoadImage(ImageSource.Parse(url.Url), Scene.Scale);
                loaded[url.Url] = task.IsCompletedSuccessfully ? task.Result : null;
                if (task.IsFaulted && reportedBadgeImages.Add(task))
                    Console.Error.WriteLine($"Nazor: could not load badge image '{url.Url}': {task.Exception!.GetBaseException().Message}");
            }
            catch (ArgumentException error)
            {
                Console.Error.WriteLine($"Nazor: invalid badge image '{url.Url}': {error.Message}");
            }
        }
        object?[] key = [images, positions, sizes, repeats, style.Color, area, corners, .. loaded.Values];
        ref var cache = ref (mask ? ref badgeMaskLayers : ref badgeBackgroundLayers);
        if (cache is { } previous && previous.Key.SequenceEqual(key)) return previous.List;
        var builder = new DisplayListBuilder();
        BackgroundPainter.Paint(images.Items, positions.Items, sizes.Items, repeats.Items, style.Color,
            url => loaded.GetValueOrDefault(url), area, corners, builder);
        var list = builder.Build(new(16, 16));
        cache = (key, list);
        return list;
    }
}
