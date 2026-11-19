using System.Globalization;
using System.Resources;

namespace Nazor.Controls;

internal static class ControlStrings
{
    private static readonly ResourceManager Resources = new("Nazor.Controls.Resources.Strings", typeof(ControlStrings).Assembly);

    internal static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    internal static string Format(string name, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(name), arguments);
}
