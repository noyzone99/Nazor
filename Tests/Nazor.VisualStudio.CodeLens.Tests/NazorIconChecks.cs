using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Threading;

namespace Nazor.VisualStudio.CodeLens.Tests;

internal static class NazorIconChecks
{
    internal static void CheckStartup(string directory)
    {
        if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Nazor.VisualStudio"))
            throw new Exception("Startup checks require an unloaded extension assembly");
        var resolved = false;
        ResolveEventHandler resolve = (_, args) =>
        {
            if (new AssemblyName(args.Name).Name != "Nazor.VisualStudio") return null;
            resolved = true;
            return Assembly.LoadFrom(Path.Combine(directory, "Nazor.VisualStudio.dll"));
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolve;
        try
        {
#pragma warning disable VSSDK005
            using var context = new JoinableTaskContext();
#pragma warning restore VSSDK005
            Check(context.Factory, directory, null);
            if (!resolved) throw new Exception("Icons must resolve their assembly through the registered binding path");
        }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= resolve; }
        Console.WriteLine("PASS: icon resources load before the extension through its binding path.");
    }

    internal static void Check(JoinableTaskFactory factory, string directory, Assembly extension)
    {
        var path = Path.Combine(directory, "NazorIcons.imagemanifest");
        var manifest = XDocument.Load(path);
        XNamespace schema = "http://schemas.microsoft.com/VisualStudio/ImageManifestSchema/2014";
        var images = manifest.Descendants(schema + "Image").ToArray();
        var catalog = Guid.Parse("df813fe5-43b7-40ab-9ba5-4ac6892fdbb6");
        if (!images.Select(i => (int)i.Attribute("ID")).SequenceEqual(new[] { 1, 2, 3 }) ||
            images.Any(i => Guid.Parse((string)i.Attribute("Guid")) != catalog))
            throw new Exception("Nazor project, component and stylesheet monikers must be stable and distinct");

        using var library = ImageLibrary.Load(factory, path);
        if (library.GetImageCount() != 3) throw new Exception("Nazor icon catalog did not load");
        var previews = Directory.CreateDirectory(Path.Combine(directory, "icon-previews")).FullName;
        foreach (var image in images)
        {
            if (!image.Elements(schema + "Source").Select(s => (string)s.Attribute("Background")).OrderBy(s => s)
                .SequenceEqual(new[] { "Dark", "HighContrastDark", "HighContrastLight", "Light" }))
                throw new Exception("Each icon must supply normal and high-contrast light/dark vectors");
            var id = (int)image.Attribute("ID");
            foreach (var highContrast in new[] { false, true })
            foreach (var dark in new[] { false, true })
            foreach (var dpi in new[] { 96, 144, 192, 384 })
            {
                var attributes = new ImageAttributes
                {
                    StructSize = Marshal.SizeOf(typeof(ImageAttributes)),
                    ImageType = (uint)_UIImageType.IT_Bitmap,
                    Format = (uint)_UIDataFormat.DF_WPF,
                    LogicalWidth = 16, LogicalHeight = 16, Dpi = dpi,
                    Background = dark ? 0xff202020u : 0xffffffffu,
                    HighContrast = highContrast ? 1 : 0,
                    Flags = unchecked((uint)(_ImageAttributesFlags.IAF_RequiredFlags | _ImageAttributesFlags.IAF_Background |
                        (highContrast ? _ImageAttributesFlags.IAF_HighContrast : 0))),
                };
                var bitmap = library.GetImage(new ImageMoniker { Guid = catalog, Id = id }, attributes) as BitmapSource;
                var size = dpi / 6;
                if (bitmap == null || bitmap.PixelWidth != size || bitmap.PixelHeight != size)
                    throw new Exception("Nazor vector icon did not render at requested DPI");
                var pixels = new byte[size * size * 4];
                new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0).CopyPixels(pixels, size * 4, 0);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var output = File.Create(Path.Combine(previews, $"{id}-{(highContrast ? "hc-" : "")}{(dark ? "dark" : "light")}-{size}.png")))
                    encoder.Save(output);
                var outline = highContrast ? (dark ? Colors.White : Colors.Black) :
                    (dark ? (id == 1 ? Color.FromRgb(216, 216, 220) : Color.FromRgb(200, 200, 200)) : Color.FromRgb(66, 66, 66));
                if (!HasColor(pixels, outline)) throw new Exception($"Incorrect theme source for icon {id}, dark={dark}, highContrast={highContrast}, dpi={dpi}");
                if (!highContrast && !HasColor(pixels, dark ? Color.FromRgb(168, 199, 250) : Color.FromRgb(11, 87, 208)))
                    throw new Exception($"Nazor blue was lost or inverted for icon {id}");
                if (pixels[3] != 0) throw new Exception("Nazor icons must keep a transparent background");
                if (id == 3 && pixels[((size * 10 / 16) * size + size * 10 / 16) * 4 + 3] != 0)
                    throw new Exception("Stylesheet brace joins must not create long miter spikes beyond the document");
            }
        }
        if (extension != null) CheckProjectProvider(extension, catalog);
        Console.WriteLine("PASS: packaged Nazor icons render in light/dark and high contrast at 100/150/200/400% DPI.");
    }

    private static bool HasColor(byte[] pixels, Color color)
    {
        for (var i = 0; i < pixels.Length; i += 4)
            if (pixels[i + 3] >= 200 && Math.Abs(pixels[i] - color.B) <= 1 && Math.Abs(pixels[i + 1] - color.G) <= 1 && Math.Abs(pixels[i + 2] - color.R) <= 1) return true;
        return false;
    }

    private static void CheckProjectProvider(Assembly extension, Guid catalog)
    {
        var type = extension.GetType("Nazor.VisualStudio.NazorProjectIcons", true);
        if (type.GetCustomAttribute<AppliesToAttribute>()?.AppliesTo != "Nazor & CSharp" ||
            !type.GetCustomAttributes<ExportAttribute>().Any(a => a.ContractType == typeof(IProjectTreePropertiesProvider)) ||
            (type.GetCustomAttribute<OrderAttribute>()?.OrderPrecedence ?? 0) < 1000)
            throw new Exception("Project icon provider must be scoped to Nazor C# and override the base root icon");
        var provider = (IProjectTreePropertiesProvider)Activator.CreateInstance(type, true);
        var original = new ProjectImageMoniker(Guid.NewGuid(), 100);
        foreach (var root in new[] { false, true })
        {
            var flags = root ? ProjectTreeFlags.Create(ProjectTreeFlags.Common.ProjectRoot) : ProjectTreeFlags.Create();
            var values = new Values { Flags = flags, Icon = original, ExpandedIcon = original };
            provider.CalculatePropertyValues(null, values);
            var expected = root ? new ProjectImageMoniker(catalog, 1) : original;
            if (values.Icon != expected || values.ExpandedIcon != expected || values.Flags != flags)
                throw new Exception("Only root icons may change; expanded icons and flags must be preserved correctly");
        }
    }

    private sealed class Values : IProjectTreeCustomizablePropertyValues
    {
        public ProjectTreeFlags Flags { get; set; }
        public ProjectImageMoniker Icon { get; set; }
        public ProjectImageMoniker ExpandedIcon { get; set; }
    }
}
