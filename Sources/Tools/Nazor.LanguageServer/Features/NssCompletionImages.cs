using System.Globalization;
using System.Xml.Linq;
using Nazor.Style;

namespace Nazor.LanguageServer.Features;

internal static class NssCompletionImages
{
    public const string Catalog = "a2d49ac2-4bad-4f94-8a22-12622e85c9bb";
    public const string ManifestNamespace = "http://schemas.microsoft.com/VisualStudio/ImageManifestSchema/2014";

    public static int Id(Color color) => color.A == 0 ? 0x1000000 : color.R << 16 | color.G << 8 | color.B;

    public static void Write(string directory)
    {
        Directory.CreateDirectory(directory);
        var assets = Directory.CreateDirectory(Path.Combine(directory, "CompletionColors")).FullName;
        XNamespace schema = ManifestNamespace;
        var images = new XElement(schema + "Images");
        foreach (var color in StyleProperties.ColorNames.Select(NssColors.Parse).OfType<Color>().DistinctBy(Id))
        {
            var id = Id(color).ToString(CultureInfo.InvariantCulture);
            XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            XElement Rect(string fill, string bounds) => new(xaml + "GeometryDrawing", new XAttribute("Brush", fill),
                new XElement(xaml + "GeometryDrawing.Geometry", new XElement(xaml + "RectangleGeometry", new XAttribute("Rect", bounds))));
            var drawing = new XElement(xaml + "DrawingGroup", Rect("White", "0,0,16,16"));
            for (var y = 0; y < 16; y += 4)
                for (var x = 0; x < 16; x += 4)
                    if ((x + y) % 8 == 0) drawing.Add(Rect("LightGray", $"{x},{y},4,4"));
            var swatch = Rect($"#{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}", "0.5,0.5,15,15");
            swatch.Add(new XElement(xaml + "GeometryDrawing.Pen", new XElement(xaml + "Pen", new XAttribute("Brush", "Gray"), new XAttribute("Thickness", "1"))));
            drawing.Add(swatch);
            new XDocument(new XElement(xaml + "Image", new XAttribute("Width", "16"), new XAttribute("Height", "16"),
                new XElement(xaml + "Image.Source", new XElement(xaml + "DrawingImage", new XElement(xaml + "DrawingImage.Drawing", drawing)))))
                .Save(Path.Combine(assets, id + ".xaml"));
            images.Add(new XElement(schema + "Image", new XAttribute("Guid", Catalog), new XAttribute("ID", id), new XAttribute("AllowColorInversion", "false"),
                new XElement(schema + "Source", new XAttribute("Uri", "/Nazor.VisualStudio;Component/CompletionColors/" + id + ".xaml"))));
        }
        new XDocument(new XElement(schema + "ImageManifest", images)).Save(Path.Combine(directory, "NazorCompletionColors.imagemanifest"));
    }
}
