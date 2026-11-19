using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Nazor.Platform.Windows;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
internal partial interface ITaskbarList3
{
    void HrInit();
    void AddTab(nint window);
    void DeleteTab(nint window);
    void ActivateTab(nint window);
    void SetActiveAlt(nint window);
    void MarkFullscreenWindow(nint window, int fullscreen);
    void SetProgressValue(nint window, ulong completed, ulong total);
    void SetProgressState(nint window, int state);
    void RegisterTab(nint tab, nint owner);
    void UnregisterTab(nint tab);
    void SetTabOrder(nint tab, nint before);
    void SetTabActive(nint tab, nint owner, uint reserved);
    void ThumbBarAddButtons(nint window, uint count, nint buttons);
    void ThumbBarUpdateButtons(nint window, uint count, nint buttons);
    void ThumbBarSetImageList(nint window, nint images);
    [PreserveSig] int SetOverlayIcon(nint window, nint icon, string description);
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("6332debf-87b5-4670-90c0-5e57b408a49e")]
internal partial interface ICustomDestinationList
{
    void SetAppID(string appId);
    void BeginList(out uint slots, in Guid iid, out nint removed);
    void AppendCategory(string category, nint items);
    void AppendKnownCategory(int category);
    void AddUserTasks(nint tasks);
    void CommitList();
    void GetRemovedDestinations(in Guid iid, out nint removed);
    void DeleteList(string appId);
    void AbortList();
}

[GeneratedComInterface]
[Guid("92ca9dcd-5622-4bba-a805-5e9f541bd8c9")]
internal partial interface IObjectArray
{
    uint GetCount();
    nint GetAt(uint index, in Guid iid);
}

[GeneratedComInterface]
[Guid("5632b1a4-e38a-400a-928a-d4cd63230295")]
internal partial interface IObjectCollection : IObjectArray
{
    void AddObject(nint value);
    void AddFromArray(nint values);
    void RemoveObjectAt(uint index);
    void Clear();
}

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("000214f9-0000-0000-c000-000000000046")]
internal partial interface IShellLinkW
{
    void GetPath(nint path, int count, nint data, uint flags);
    nint GetIDList();
    void SetIDList(nint id);
    void GetDescription(nint value, int count);
    void SetDescription(string value);
    void GetWorkingDirectory(nint value, int count);
    void SetWorkingDirectory(string value);
    void GetArguments(nint value, int count);
    void SetArguments(string value);
    ushort GetHotkey();
    void SetHotkey(ushort value);
    int GetShowCmd();
    void SetShowCmd(int value);
    void GetIconLocation(nint path, int count, out int index);
    void SetIconLocation(string path, int index);
    void SetRelativePath(string path, uint reserved);
    void Resolve(nint window, uint flags);
    void SetPath(string path);
}

[StructLayout(LayoutKind.Sequential)]
internal struct LauncherPropertyKey
{
    internal Guid Format;
    internal uint Id;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LauncherPropertyValue
{
    internal ushort Type, Reserved1, Reserved2, Reserved3;
    internal nint Text, Padding;
}

[GeneratedComInterface]
[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
internal partial interface ILauncherPropertyStore
{
    uint GetCount();
    void GetAt(uint index, out LauncherPropertyKey key);
    void GetValue(in LauncherPropertyKey key, out LauncherPropertyValue value);
    void SetValue(in LauncherPropertyKey key, in LauncherPropertyValue value);
    void Commit();
}
