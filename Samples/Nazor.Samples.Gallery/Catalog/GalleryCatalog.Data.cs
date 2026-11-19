using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the data grid family, listed with the other collections.
    private static IEnumerable<GalleryEntry> DataEntries() =>
    [
        new("data-grid", "DataGrid", "Collections", "Sort a data grid by selecting a column header.", typeof(DataGrid<Examples.Data.DataGridSample.City>), typeof(Examples.Data.DataGridSample), typeof(DataGridColumn<Examples.Data.DataGridSample.City>)),
        new("property-grid", "PropertyGrid", "Collections", "Inspect and edit the properties of a .NET object.", typeof(PropertyGrid), typeof(Examples.Data.PropertyGridSample)),
    ];
}
