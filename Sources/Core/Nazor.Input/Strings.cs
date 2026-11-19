using System.Globalization;
using System.Resources;

namespace Nazor.Input;

internal static class Strings
{
    private static readonly ResourceManager Resources = new("Nazor.Input.Resources.Strings", typeof(Strings).Assembly);

    public static string Undo => Get(nameof(Undo));

    public static string Cut => Get(nameof(Cut));

    public static string Copy => Get(nameof(Copy));

    public static string Paste => Get(nameof(Paste));

    public static string SelectAll => Get(nameof(SelectAll));

    private static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
