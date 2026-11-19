using Nazor.Graphics;
using Nazor.Style;
using GraphicsSize = Nazor.Graphics.Size;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Presentation;

internal static class BackgroundPainter
{
    internal static void Paint(IReadOnlyList<ImageValue> images, IReadOnlyList<PositionValue> positions,
        IReadOnlyList<BackgroundSize> sizes, IReadOnlyList<BackgroundRepeat> repeats, StyleColor color,
        Func<string, LoadedImage?> load, Rect area, CornerRadius radius, DisplayListBuilder builder, IReadOnlyList<Nazor.Style.BlendMode>? blends = null)
    {
        if (images.Count == 0 || area.IsEmpty) return;
        for (var i = images.Count - 1; i >= 0; i--)
        {
            var blend = blends is { Count: > 0 } ? blends[i % blends.Count] : Nazor.Style.BlendMode.Normal;
            if (blend != Nazor.Style.BlendMode.Normal) builder.Add(new PushBlend(BlendModes.Resolve(blend),area));
            PaintLayer(i);
            if (blend != Nazor.Style.BlendMode.Normal) builder.Add(Pop.Instance);
        }

        void PaintLayer(int i)
        {
            var loaded = images[i] is UrlImage url ? load(url.Url) : null;
            if (images[i] is UrlImage && loaded is null) return;
            var tile = TileSize(sizes[i % sizes.Count], area, loaded?.NaturalSize);
            if (tile.Width <= 0 || tile.Height <= 0) return;
            var position = positions[i % positions.Count];
            var placed = new Rect(area.X + (float)position.X.Resolve(area.Width - tile.Width), area.Y + (float)position.Y.Resolve(area.Height - tile.Height), tile.Width, tile.Height);
            var repeat = repeats[i % repeats.Count];
            var (repeatX, repeatY) = (repeat is BackgroundRepeat.Repeat or BackgroundRepeat.RepeatX, repeat is BackgroundRepeat.Repeat or BackgroundRepeat.RepeatY);
            if (loaded is not null)
            {
                builder.Add(new FillImage(area, radius, loaded.Image, placed, repeatX, repeatY));
                return;
            }
            if (images[i] is not Gradient gradient) return;

            if (placed == area)
            {
                if (GradientGeometry.Resolve(gradient, area, color) is { } brush) builder.Add(new FillGradient(area, radius, brush));
                return;
            }
            builder.Add(new PushClip(area, radius));
            foreach (var cell in Tiles(area, placed, repeatX, repeatY))
                if (GradientGeometry.Resolve(gradient, cell, color) is { } brush) builder.Add(new FillGradient(cell, CornerRadius.Zero, brush));
            builder.Add(Pop.Instance);
        }
    }

    private static GraphicsSize TileSize(BackgroundSize size, Rect area, GraphicsSize? natural)
    {
        if (natural is not { Width: > 0, Height: > 0 } image) image = area.Size;
        switch (size.Kind)
        {
            case BackgroundSizeKind.Cover or BackgroundSizeKind.Contain:
                var scale = size.Kind == BackgroundSizeKind.Cover
                    ? Math.Max(area.Width / image.Width, area.Height / image.Height)
                    : Math.Min(area.Width / image.Width, area.Height / image.Height);
                return new GraphicsSize(image.Width * scale, image.Height * scale);
        }
        float? width = size.Width.IsFixed ? (float)size.Width.Length.Resolve(area.Width) : null;
        float? height = size.Height.IsFixed ? (float)size.Height.Length.Resolve(area.Height) : null;
        return (width, height) switch
        {
            ({ } w, { } h) => new GraphicsSize(w, h),
            ({ } w, null) => new GraphicsSize(w, natural is not null ? w * image.Height / image.Width : area.Height),
            (null, { } h) => new GraphicsSize(natural is not null ? h * image.Width / image.Height : area.Width, h),
            _ => image,
        };
    }

    private static IEnumerable<Rect> Tiles(Rect area, Rect tile, bool repeatX, bool repeatY)
    {
        var startX = repeatX ? tile.X - MathF.Ceiling((tile.X - area.X) / tile.Width) * tile.Width : tile.X;
        var startY = repeatY ? tile.Y - MathF.Ceiling((tile.Y - area.Y) / tile.Height) * tile.Height : tile.Y;
        var columns = repeatX ? (int)MathF.Ceiling((area.Right - startX) / tile.Width) : 1;
        var rows = repeatY ? (int)MathF.Ceiling((area.Bottom - startY) / tile.Height) : 1;
        if ((long)columns * rows > 4096) return [tile];
        var cells = new List<Rect>();
        for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
                cells.Add(new Rect(startX + column * tile.Width, startY + row * tile.Height, tile.Width, tile.Height));
        return cells;
    }

}
