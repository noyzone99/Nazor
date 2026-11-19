using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Nazor.Platform.Windows.Automation;

namespace Nazor.Application.Tests;


[StructLayout(LayoutKind.Explicit, Size = 24)]
internal struct ClientVariant
{
    [FieldOffset(0)] public ushort Type;
    [FieldOffset(8)] public int Int32;
    [FieldOffset(8)] public short Bool;
    [FieldOffset(8)] public nint Pointer;

    public readonly object? ToObject() => Type switch
    {
        Variant.VT_I4 => Int32,
        Variant.VT_BOOL => Bool != 0,
        Variant.VT_BSTR => Marshal.PtrToStringBSTR(Pointer),
        Variant.VT_UNKNOWN => Element(Pointer),
        _ => null,
    };

    private static unsafe IUIAutomationElement? Element(nint pointer)
    {
        if (pointer == 0) return null;
        var element = ComInterfaceMarshaller<IUIAutomationElement>.ConvertToManaged((void*)pointer);
        Marshal.Release(pointer);
        return element;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct ClientRect
{
    public int Left, Top, Right, Bottom;
}

internal enum TreeScope
{
    Element = 1,
    Children = 2,
    Descendants = 4,
    Subtree = 7,
}

[GeneratedComInterface]
[Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee")]
internal partial interface IUIAutomation
{
    void _CompareElements();
    void _CompareRuntimeIds();
    void _GetRootElement();
    IUIAutomationElement ElementFromHandle(nint hwnd);
    void _ElementFromPoint();
    void _GetFocusedElement();
    void _s9(); void _s10(); void _s11(); void _s12(); void _s13(); void _s14();
    void _s15(); void _s16(); void _s17(); void _s18(); void _s19(); void _s20();

    [return: MarshalAs(UnmanagedType.Interface)]
    object CreateTrueCondition();
    void _s22(); void _s23(); void _s24(); void _s25(); void _s26(); void _s27(); void _s28(); void _s29(); void _s30(); void _s31();
    void AddAutomationEventHandler(int eventId, IUIAutomationElement element, TreeScope scope, nint cacheRequest, IUIAutomationEventHandler handler);
    void _s33(); void _s34(); void _s35(); void _s36(); void _s37(); void _s38(); void _s39(); void _s40();
    void RemoveAllEventHandlers();
}

[GeneratedComInterface]
[Guid("25f700c8-d816-4057-a9dc-3cbdee77e256")]
internal partial interface IUIAutomation5 : IUIAutomation
{
    void _s42(); void _s43(); void _s44(); void _s45(); void _s46(); void _s47(); void _s48(); void _s49(); void _s50(); void _s51();
    void _s52(); void _s53(); void _s54(); void _s55(); void _s56(); void _s57(); void _s58(); void _s59(); void _s60(); void _s61();
    void _s62(); void _s63(); void _s64(); void _s65(); void _s66(); void _s67();
    void AddNotificationEventHandler(IUIAutomationElement element, TreeScope scope, nint cacheRequest, IUIAutomationNotificationEventHandler handler);
}

[GeneratedComInterface]
[Guid("146c3c17-f12e-4e22-8c27-f894b9b79c69")]
internal partial interface IUIAutomationEventHandler
{
    void HandleAutomationEvent(IUIAutomationElement sender, int eventId);
}

[GeneratedComInterface]
[Guid("c7cb2637-e6c2-4d0c-85de-4948c02175c7")]
internal partial interface IUIAutomationNotificationEventHandler
{
    void HandleNotificationEvent(IUIAutomationElement sender, int kind, int processing,
        [MarshalAs(UnmanagedType.BStr)] string displayString, [MarshalAs(UnmanagedType.BStr)] string activityId);
}

[GeneratedComClass]
internal sealed partial class UiaEventLog : IUIAutomationEventHandler, IUIAutomationNotificationEventHandler
{
    public System.Collections.Concurrent.BlockingCollection<string> Events { get; } = [];

    public void HandleAutomationEvent(IUIAutomationElement sender, int eventId) =>
        Events.Add($"{eventId} {sender.GetCurrentAutomationId()}: {sender.GetCurrentName()}");

    public void HandleNotificationEvent(IUIAutomationElement sender, int kind, int processing, string displayString, string activityId) =>
        Events.Add($"notification {displayString} ({processing}) {activityId}");

    public string Next(Func<string, bool> match)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (Events.TryTake(out var item, Math.Max(0, (int)(deadline - DateTime.UtcNow).TotalMilliseconds)))
            if (match(item)) return item;
        throw new TimeoutException("No such UI Automation event.");
    }
}

[GeneratedComInterface]
[Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e")]
internal partial interface IUIAutomationElement
{
    void SetFocus();
    void _GetRuntimeId();
    void _FindFirst();
    IUIAutomationElementArray FindAll(TreeScope scope, [MarshalAs(UnmanagedType.Interface)] object condition);
    void _s7(); void _s8(); void _s9();
    ClientVariant GetCurrentPropertyValue(int propertyId);
    void _s11(); void _s12(); void _s13(); void _s14(); void _s15();

    [return: MarshalAs(UnmanagedType.Interface)]
    object? GetCurrentPattern(int patternId);
    void _s17(); void _s18(); void _s19(); void _s20();
    int GetCurrentControlType();
    void _s22();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetCurrentName();
    void _s24(); void _s25();
    int GetCurrentHasKeyboardFocus();
    int GetCurrentIsKeyboardFocusable();
    int GetCurrentIsEnabled();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetCurrentAutomationId();
    void _s30(); void _s31(); void _s32(); void _s33(); void _s34(); void _s35(); void _s36(); void _s37();
    int GetCurrentIsOffscreen();
    void _s39();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetCurrentFrameworkId();
    void _s41(); void _s42();
    ClientRect GetCurrentBoundingRectangle();
}

[GeneratedComInterface]
[Guid("32eba289-3583-42c9-9c59-3b6d9a1e9b6a")]
internal partial interface IUIAutomationTextPattern
{
    IUIAutomationTextRange RangeFromPoint(ClientPoint point);
    void _RangeFromChild();
    IUIAutomationTextRangeArray GetSelection();
    IUIAutomationTextRangeArray GetVisibleRanges();
    IUIAutomationTextRange GetDocumentRange();
    int GetSupportedTextSelection();
}

[GeneratedComInterface]
[Guid("506a921a-fcc9-409f-b23b-37eb74106872")]
internal partial interface IUIAutomationTextPattern2 : IUIAutomationTextPattern
{
    void _RangeFromAnnotation();
    IUIAutomationTextRange GetCaretRange(out int isActive);
}

[GeneratedComInterface]
[Guid("ce4ae76a-e717-4c98-81ea-47371d028eb6")]
internal partial interface IUIAutomationTextRangeArray
{
    int GetLength();
    IUIAutomationTextRange GetElement(int index);
}

[GeneratedComInterface]
[Guid("a543cc6a-f4ae-494b-8239-c814481187a8")]
internal partial interface IUIAutomationTextRange
{
    IUIAutomationTextRange Clone();
    int Compare(IUIAutomationTextRange range);
    int CompareEndpoints(int endpoint, IUIAutomationTextRange targetRange, int targetEndpoint);
    void ExpandToEnclosingUnit(int unit);
    void _FindAttribute();
    IUIAutomationTextRange? FindText([MarshalAs(UnmanagedType.BStr)] string text, int backward, int ignoreCase);
    ClientVariant GetAttributeValue(int attributeId);
    void _GetBoundingRectangles();
    IUIAutomationElement GetEnclosingElement();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetText(int maxLength);
    int Move(int unit, int count);
    int MoveEndpointByUnit(int endpoint, int unit, int count);
    void MoveEndpointByRange(int endpoint, IUIAutomationTextRange targetRange, int targetEndpoint);
    void Select();
}

[StructLayout(LayoutKind.Sequential)]
internal struct ClientPoint
{
    public int X, Y;
}

[GeneratedComInterface]
[Guid("5ed5202e-b2ac-47a6-b638-4b0bf140d78e")]
internal partial interface IUIAutomationSelectionPattern
{
    IUIAutomationElementArray GetCurrentSelection();
    int GetCurrentCanSelectMultiple();
    int GetCurrentIsSelectionRequired();
}

[GeneratedComInterface]
[Guid("14314595-b4bc-4055-95f2-58f2e42c9855")]
internal partial interface IUIAutomationElementArray
{
    int GetLength();
    IUIAutomationElement GetElement(int index);
}

[GeneratedComInterface]
[Guid("fb377fbe-8ea6-46d5-9c73-6499642d3059")]
internal partial interface IUIAutomationInvokePattern
{
    void Invoke();
}

[GeneratedComInterface]
[Guid("94cf8058-9b8d-4ab9-8bfd-4cd0a33c8c70")]
internal partial interface IUIAutomationTogglePattern
{
    void Toggle();
    int GetCurrentToggleState();
}

[GeneratedComInterface]
[Guid("a94cd8b1-0844-4cd6-9d2d-640537ab39e9")]
internal partial interface IUIAutomationValuePattern
{
    void SetValue([MarshalAs(UnmanagedType.BStr)] string value);

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetCurrentValue();

    int GetCurrentIsReadOnly();
}

[GeneratedComInterface]
[Guid("a8efa66a-0fda-421a-9194-38021f3578ea")]
internal partial interface IUIAutomationSelectionItemPattern
{
    void Select();
    void AddToSelection();
    void RemoveFromSelection();
    int GetCurrentIsSelected();
    IUIAutomationElement? GetCurrentSelectionContainer();
}

[GeneratedComInterface]
[Guid("619be086-1f4e-4ee4-bafa-210128738730")]
internal partial interface IUIAutomationExpandCollapsePattern
{
    void Expand();
    void Collapse();
    int GetCurrentExpandCollapseState();
}

[GeneratedComInterface]
[Guid("59213f4f-7346-49e5-b120-80555987a148")]
internal partial interface IUIAutomationRangeValuePattern
{
    void SetValue(double value);
    double GetCurrentValue();
    int GetCurrentIsReadOnly();
    double GetCurrentMaximum();
    double GetCurrentMinimum();
    double GetCurrentLargeChange();
    double GetCurrentSmallChange();
}

[GeneratedComInterface]
[Guid("88f4d42a-e881-459d-a77c-73bbbb7e02dc")]
internal partial interface IUIAutomationScrollPattern
{
    void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);
    void SetScrollPercent(double horizontalPercent, double verticalPercent);
    double GetCurrentHorizontalScrollPercent();
    double GetCurrentVerticalScrollPercent();
    double GetCurrentHorizontalViewSize();
    double GetCurrentVerticalViewSize();
    int GetCurrentHorizontallyScrollable();
    int GetCurrentVerticallyScrollable();
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static partial class UiaClient
{
    public const int TitleBarControlType = 50037;

    public static T Run<T>(Func<IUIAutomation, T> body, bool automation8 = false)
    {
        T result = default!;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            CoInitializeEx(0, 0  );
            try
            {
                var clsid = automation8 ? new Guid("e22ad333-b25f-460c-83d0-0581107395c9") : new Guid("ff48dba4-60ef-4201-aa87-54103eef594e");
                var iid = new Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee");
                Marshal.ThrowExceptionForHR(CoCreateInstance(clsid, 0, 1  , iid, out var pointer));
                var uia = (IUIAutomation)new StrategyBasedComWrappers().GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.None);
                Marshal.Release(pointer);
                result = body(uia);
            }
            catch (Exception exception)
            {
                error = exception;
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                CoUninitialize();
            }
        })
        { IsBackground = true, Name = "UIA client (test)" };
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(30))) throw new TimeoutException("The UI Automation client did not finish.");
        if (error is not null) ExceptionDispatchInfo.Throw(error);
        return result;
    }

    public static void Run(Action<IUIAutomation> body) => Run(uia =>
    {
        body(uia);
        return true;
    });

    public static List<IUIAutomationElement> FindAll(this IUIAutomation uia, IUIAutomationElement root, TreeScope scope)
    {
        var array = root.FindAll(scope, uia.CreateTrueCondition());
        return Enumerable.Range(0, array.GetLength()).Select(array.GetElement).ToList();
    }

    public static IUIAutomationElement Find(this IUIAutomation uia, nint hwnd, string automationId) =>
        uia.FindAll(uia.ElementFromHandle(hwnd), TreeScope.Descendants).Single(e => e.GetCurrentAutomationId() == automationId);

    public static T Pattern<T>(this IUIAutomationElement element, int patternId) where T : class =>
        element.GetCurrentPattern(patternId) as T ?? throw new InvalidOperationException($"Pattern {patternId} not supported.");

    [LibraryImport("oleaut32.dll", EntryPoint = "SafeArrayDestroy")]
    public static partial int DestroySafeArray(nint array);

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(nint reserved, uint flags);

    [LibraryImport("ole32.dll")]
    private static partial void CoUninitialize();

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint instance);
}
