using System.Globalization;
using System.Resources;

namespace Nazor;

internal static class Strings
{
    private static readonly ResourceManager Resources = new("Nazor.Application.Resources.Strings", typeof(Strings).Assembly);

    public static string Ok => Get(nameof(Ok));

    public static string Cancel => Get(nameof(Cancel));

    public static string Yes => Get(nameof(Yes));

    public static string No => Get(nameof(No));

    internal static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
