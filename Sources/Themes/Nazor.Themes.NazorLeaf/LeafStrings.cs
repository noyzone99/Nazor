using System.Globalization;
using System.Resources;

namespace Nazor.Themes.NazorLeaf;

internal static class LeafStrings
{
    private static readonly ResourceManager Resources = new("Nazor.Themes.NazorLeaf.Resources.Strings", typeof(LeafStrings).Assembly);

    internal static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
