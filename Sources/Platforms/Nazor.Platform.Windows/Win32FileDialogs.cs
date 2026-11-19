using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Platform.Windows;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
internal partial interface IFileDialog
{
    [PreserveSig]
    int Show(nint owner);

    void SetFileTypes(uint count, nint filterSpecs);
    void SetFileTypeIndex(uint index);
    uint GetFileTypeIndex();
    uint Advise(nint events);
    void Unadvise(uint cookie);
    void SetOptions(uint options);
    uint GetOptions();
    void SetDefaultFolder(nint item);
    void SetFolder(nint item);
    nint GetFolder();
    nint GetCurrentSelection();
    void SetFileName(string name);
    nint GetFileName();
    void SetTitle(string title);
    void SetOkButtonLabel(string text);
    void SetFileNameLabel(string label);
    nint GetResult();
    void AddPlace(nint item, int place);
    void SetDefaultExtension(string extension);
    void Close(int hresult);
    void SetClientGuid(in Guid guid);
    void ClearClientData();
    void SetFilter(nint filter);
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
internal partial interface IFileOpenDialog : IFileDialog
{
    nint GetResults();
    nint GetSelectedItems();
}

[GeneratedComInterface]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
internal partial interface IShellItem
{
    nint BindToHandler(nint bindContext, in Guid handler, in Guid iid);
    nint GetParent();
    nint GetDisplayName(uint sigdn);
    uint GetAttributes(uint mask);
    int Compare(nint other, uint hint);
}

[GeneratedComInterface]
[Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
internal partial interface IShellItemArray
{
    nint BindToHandler(nint bindContext, in Guid handler, in Guid iid);
    nint GetPropertyStore(int flags, in Guid iid);
    nint GetPropertyDescriptionList(nint keyType, in Guid iid);
    uint GetAttributes(int flags, uint mask);
    uint GetCount();
    nint GetItemAt(uint index);
    nint EnumItems();
}

internal static class Win32FileDialogs
{
    private static readonly Guid FileOpenDialog = new("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
    private static readonly Guid FileSaveDialog = new("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");
    private static readonly StrategyBasedComWrappers Wrappers = new();

    private const uint FOS_OVERWRITEPROMPT = 0x2, FOS_NOCHANGEDIR = 0x8, FOS_PICKFOLDERS = 0x20, FOS_FORCEFILESYSTEM = 0x40,
        FOS_ALLOWMULTISELECT = 0x200, FOS_PATHMUSTEXIST = 0x800, FOS_FILEMUSTEXIST = 0x1000;
    private const uint SIGDN_FILESYSPATH = 0x80058000;
    private const int ErrorCancelled = unchecked((int)0x800704C7);

    public static IReadOnlyList<string>? Show(nint owner, FileDialogRequest request)
    {
        var save = request.Kind == FileDialogKind.Save;
        Marshal.ThrowExceptionForHR(CoCreateInstance(save ? FileSaveDialog : FileOpenDialog, 0, CLSCTX_INPROC_SERVER,
            save ? typeof(IFileDialog).GUID : typeof(IFileOpenDialog).GUID, out var pointer));
        var dialog = (IFileDialog)Wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
        Marshal.Release(pointer);
        var strings = new List<nint>();
        nint specs = 0;
        try
        {
            dialog.SetOptions(dialog.GetOptions() | FOS_FORCEFILESYSTEM | FOS_NOCHANGEDIR | FOS_PATHMUSTEXIST | request.Kind switch
            {
                FileDialogKind.Save => FOS_OVERWRITEPROMPT,
                FileDialogKind.Folder => FOS_PICKFOLDERS,
                _ => FOS_FILEMUSTEXIST | (request.Multiple ? FOS_ALLOWMULTISELECT : 0),
            });
            if (request.Title is { } title) dialog.SetTitle(title);
            if (request.FileName is { } name) dialog.SetFileName(name);
            if (request.DefaultExtension is { } extension) dialog.SetDefaultExtension(extension.TrimStart('.'));
            if (request.Filters.Count > 0 && request.Kind != FileDialogKind.Folder)
            {

                specs = Marshal.AllocCoTaskMem(2 * nint.Size * request.Filters.Count);
                for (var i = 0; i < request.Filters.Count; i++)
                {
                    var (label, patterns) = (Marshal.StringToCoTaskMemUni(request.Filters[i].Name), Marshal.StringToCoTaskMemUni(string.Join(';', request.Filters[i].Patterns)));
                    strings.Add(label);
                    strings.Add(patterns);
                    Marshal.WriteIntPtr(specs, 2 * i * nint.Size, label);
                    Marshal.WriteIntPtr(specs, (2 * i + 1) * nint.Size, patterns);
                }
                dialog.SetFileTypes((uint)request.Filters.Count, specs);
            }
            if (request.InitialDirectory is { } directory && SHCreateItemFromParsingName(directory, 0, typeof(IShellItem).GUID, out var folder) >= 0)
            {
                dialog.SetFolder(folder);
                Marshal.Release(folder);
            }
            var result = dialog.Show(owner);
            if (result == ErrorCancelled) return null;
            Marshal.ThrowExceptionForHR(result);
            if (request.Kind != FileDialogKind.Open || !request.Multiple) return [PathOf(dialog.GetResult())];
            var items = (IShellItemArray)Wrappers.GetOrCreateObjectForComInstance(((IFileOpenDialog)dialog).GetResults(), CreateObjectFlags.UniqueInstance);
            try
            {
                return [.. Enumerable.Range(0, (int)items.GetCount()).Select(i => PathOf(items.GetItemAt((uint)i)))];
            }
            finally
            {
                ((ComObject)(object)items).FinalRelease();
            }
        }
        finally
        {
            foreach (var text in strings) Marshal.FreeCoTaskMem(text);
            if (specs != 0) Marshal.FreeCoTaskMem(specs);
            ((ComObject)(object)dialog).FinalRelease();
        }
    }

    private static string PathOf(nint pointer)
    {
        var item = (IShellItem)Wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.UniqueInstance);
        Marshal.Release(pointer);
        try
        {
            var name = item.GetDisplayName(SIGDN_FILESYSPATH);
            try
            {
                return Marshal.PtrToStringUni(name) ?? "";
            }
            finally
            {
                Marshal.FreeCoTaskMem(name);
            }
        }
        finally
        {
            ((ComObject)(object)item).FinalRelease();
        }
    }
}
