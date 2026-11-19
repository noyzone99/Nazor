using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Newtonsoft.Json.Linq;

namespace Nazor.VisualStudio
{
    internal sealed class NazorCompletionImages
    {
        internal static readonly Guid Catalog = new Guid("a2d49ac2-4bad-4f94-8a22-12622e85c9bb");
        internal static readonly Guid CssCatalog = new Guid("aba0adf0-a9f1-4328-a36a-9372c0081c59");
        private readonly Dictionary<int, IImageHandle> registered = new Dictionary<int, IImageHandle>();

        internal JObject CreateMap(ImageLibrary library, string manifestPath)
        {
            var images = new JObject();
            XNamespace schema = "http://schemas.microsoft.com/VisualStudio/ImageManifestSchema/2014";
            foreach (var image in XDocument.Load(manifestPath).Descendants(schema + "Image"))
            {
                var id = (int)image.Attribute("ID");
                var moniker = Resolve(library, id);
                images[id.ToString(System.Globalization.CultureInfo.InvariantCulture)] = new JObject { ["guid"] = moniker.Guid.ToString(), ["id"] = moniker.Id };
            }
            return images;
        }

        internal ImageMoniker Resolve(ImageLibrary library, int id)
        {
            var css = new ImageMoniker { Guid = CssCatalog, Id = id };
            if (id != 0x1000000 && library.GetImageMonikerType(css) != 0) return css;
            var own = new ImageMoniker { Guid = Catalog, Id = id };
            if (library.GetImageMonikerType(own) != 0) return own;
            if (!registered.TryGetValue(id, out var handle))
            {
                var image = (Image)Application.LoadComponent(new Uri("/Nazor.VisualStudio;Component/CompletionColors/" + id + ".xaml", UriKind.Relative));
                image.Source.Freeze();
                handle = library.AddCustomImage(image.Source, canTheme: false);
                registered.Add(id, handle);
            }
            return handle.Moniker;
        }
    }
}
