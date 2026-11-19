using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Nazor.Platform.Windows.Automation;

internal enum ProviderOptions
{
    ServerSideProvider = 0x2,
    ProviderOwnsSetFocus = 0x10,
    UseComThreading = 0x20,
}

internal enum NavigateDirection
{
    Parent = 0,
    NextSibling = 1,
    PreviousSibling = 2,
    FirstChild = 3,
    LastChild = 4,
}

internal enum ToggleState
{
    Off = 0,
    On = 1,
    Indeterminate = 2,
}

internal enum ExpandCollapseState
{
    Collapsed = 0,
    Expanded = 1,
    LeafNode = 3,
}

internal enum ScrollAmount
{
    LargeDecrement = 0,
    SmallDecrement = 1,
    NoAmount = 2,
    LargeIncrement = 3,
    SmallIncrement = 4,
}

internal enum StructureChangeType
{
    ChildAdded = 0,
    ChildRemoved = 1,
    ChildrenInvalidated = 2,
}

[StructLayout(LayoutKind.Sequential)]
internal struct UiaRect
{
    public double Left, Top, Width, Height;
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal struct Variant
{
    public const ushort VT_EMPTY = 0, VT_I4 = 3, VT_R8 = 5, VT_BSTR = 8, VT_BOOL = 11, VT_UNKNOWN = 13;

    [FieldOffset(0)] public ushort Type;
    [FieldOffset(8)] public int Int32;
    [FieldOffset(8)] public short Bool;
    [FieldOffset(8)] public double Double;
    [FieldOffset(8)] public nint Pointer;

    public static Variant FromObject(object? value) => value switch
    {
        int i => new Variant { Type = VT_I4, Int32 = i },
        bool b => new Variant { Type = VT_BOOL, Bool = b ? (short)-1 : (short)0 },
        double d => new Variant { Type = VT_R8, Double = d },
        string s => new Variant { Type = VT_BSTR, Pointer = Marshal.StringToBSTR(s) },

        IRawElementProviderSimple provider => new Variant { Type = VT_UNKNOWN, Pointer = Unknown(provider) },
        _ => default,
    };

    public static unsafe nint Unknown<T>(T value) where T : class
    {
        var pointer = (nint)System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller<T>.ConvertToUnmanaged(value);
        try
        {
            var iid = new Guid("00000000-0000-0000-c000-000000000046");
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(pointer, in iid, out var unknown));
            return unknown;
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    public readonly object? ToObject() => Type switch
    {
        VT_I4 => Int32,
        VT_BOOL => Bool != 0,
        VT_R8 => Double,
        VT_BSTR => Marshal.PtrToStringBSTR(Pointer),
        _ => null,
    };

    public void Clear()
    {
        if (Type == VT_BSTR && Pointer != 0) Marshal.FreeBSTR(Pointer);
        if (Type == VT_UNKNOWN && Pointer != 0) Marshal.Release(Pointer);
        this = default;
    }
}

[GeneratedComInterface]
[Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c")]
internal partial interface IRawElementProviderSimple
{
    ProviderOptions GetProviderOptions();

    [return: MarshalAs(UnmanagedType.Interface)]
    object? GetPatternProvider(int patternId);

    Variant GetPropertyValue(int propertyId);

    nint GetHostRawElementProvider();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("b17d6187-0907-464b-a168-0ef17a1572b1")]
internal partial interface IGridProvider
{
    IRawElementProviderSimple? GetItem(int row, int column);
    int GetRowCount();
    int GetColumnCount();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("d02541f1-fb81-4d64-ae32-f520f8a6dbd1")]
internal partial interface IGridItemProvider
{
    int GetRow();
    int GetColumn();
    int GetRowSpan();
    int GetColumnSpan();
    IRawElementProviderSimple? GetContainingGrid();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("9c860395-97b3-490a-b52a-858cc22af166")]
internal partial interface ITableProvider
{
    nint GetRowHeaders();
    nint GetColumnHeaders();
    int GetRowOrColumnMajor();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("b9734fa6-771f-4d78-9c90-2517999349cd")]
internal partial interface ITableItemProvider
{
    nint GetRowHeaderItems();
    nint GetColumnHeaderItems();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("f7063da8-8359-439c-9297-bbc5299a7d87")]
internal partial interface IRawElementProviderFragment
{
    IRawElementProviderFragment? Navigate(NavigateDirection direction);

    nint GetRuntimeId();

    UiaRect GetBoundingRectangle();

    nint GetEmbeddedFragmentRoots();

    void SetFocus();

    IRawElementProviderFragmentRoot? GetFragmentRoot();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58")]
internal partial interface IRawElementProviderFragmentRoot
{
    IRawElementProviderFragment? ElementProviderFromPoint(double x, double y);

    IRawElementProviderFragment? GetFocus();
}

[StructLayout(LayoutKind.Sequential)]
internal struct UiaPoint
{
    public double X, Y;
}

internal enum TextPatternRangeEndpoint
{
    Start = 0,
    End = 1,
}

internal enum SupportedTextSelection
{
    None = 0,
    Single = 1,
    Multiple = 2,
}

#if NET8_0
[GeneratedComInterface]
#else
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#endif
[Guid("3589c92c-63f3-4367-99bb-ada653b77cf2")]
internal partial interface ITextProvider
{
    nint GetSelection();

    nint GetVisibleRanges();

    ITextRangeProvider? RangeFromChild(IRawElementProviderSimple childElement);

    ITextRangeProvider? RangeFromPoint(UiaPoint point);

    ITextRangeProvider GetDocumentRange();

    SupportedTextSelection GetSupportedTextSelection();
}

#if NET8_0
[GeneratedComInterface]
#else
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#endif
[Guid("0dc5e6ed-3e16-4bf1-8f9a-a979878bc195")]
internal partial interface ITextProvider2 : ITextProvider
{
    ITextRangeProvider? RangeFromAnnotation(IRawElementProviderSimple annotationElement);

    ITextRangeProvider GetCaretRange([MarshalAs(UnmanagedType.Bool)] out bool isActive);
}

[GeneratedComInterface]
[Guid("5347ad7b-c355-46f8-aff5-909033582f63")]
internal partial interface ITextRangeProvider
{
    ITextRangeProvider Clone();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool Compare(ITextRangeProvider range);

    int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);

    void ExpandToEnclosingUnit(Accessibility.TextUnit unit);

    ITextRangeProvider? FindAttribute(int attributeId, Variant value, [MarshalAs(UnmanagedType.Bool)] bool backward);

    ITextRangeProvider? FindText([MarshalAs(UnmanagedType.BStr)] string text, [MarshalAs(UnmanagedType.Bool)] bool backward, [MarshalAs(UnmanagedType.Bool)] bool ignoreCase);

    Variant GetAttributeValue(int attributeId);

    nint GetBoundingRectangles();

    IRawElementProviderSimple GetEnclosingElement();

    [return: MarshalAs(UnmanagedType.BStr)]
    string GetText(int maxLength);

    int Move(Accessibility.TextUnit unit, int count);

    int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, Accessibility.TextUnit unit, int count);

    void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);

    void Select();

    void AddToSelection();

    void RemoveFromSelection();

    void ScrollIntoView([MarshalAs(UnmanagedType.Bool)] bool alignToTop);

    nint GetChildren();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("fb8b03af-3bdf-48d4-bd36-1a65793be168")]
internal partial interface ISelectionProvider
{
    nint GetSelection();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetCanSelectMultiple();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsSelectionRequired();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2")]
internal partial interface IInvokeProvider
{
    void Invoke();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892")]
internal partial interface IToggleProvider
{
    void Toggle();

    ToggleState GetToggleState();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("c7935180-6fb3-4201-b174-7df73adbf64a")]
internal partial interface IValueProvider
{
    void SetValue([MarshalAs(UnmanagedType.LPWStr)] string? value);

    [return: MarshalAs(UnmanagedType.BStr)]
    string? GetValue();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsReadOnly();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("d847d3a5-cab0-4a98-8c32-ecb45c59ad24")]
internal partial interface IExpandCollapseProvider
{
    void Expand();

    void Collapse();

    ExpandCollapseState GetExpandCollapseState();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("36dc7aef-33e6-4691-afe1-2be7274b3d33")]
internal partial interface IRangeValueProvider
{
    void SetValue(double value);

    double GetValue();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsReadOnly();

    double GetMaximum();

    double GetMinimum();

    double GetLargeChange();

    double GetSmallChange();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("2acad808-b2d4-452d-a407-91ff1ad167b2")]
internal partial interface ISelectionItemProvider
{
    void Select();

    void AddToSelection();

    void RemoveFromSelection();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsSelected();

    IRawElementProviderSimple? GetSelectionContainer();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("b38b8077-1fc3-42a5-8cae-d40c2215055a")]
internal partial interface IScrollProvider
{
    void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);

    void SetScrollPercent(double horizontalPercent, double verticalPercent);

    double GetHorizontalScrollPercent();

    double GetVerticalScrollPercent();

    double GetHorizontalViewSize();

    double GetVerticalViewSize();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetHorizontallyScrollable();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetVerticallyScrollable();
}

internal static unsafe partial class Uia
{
    public const int UiaRootObjectId = -25, UiaAppendRuntimeId = 3;

    public const int TextPattern = 10014, TextPattern2 = 10024, InvokePattern = 10000, SelectionPattern = 10001, ValuePattern = 10002, RangeValuePattern = 10003, ScrollPattern = 10004, ExpandCollapsePattern = 10005, SelectionItemPattern = 10010, TogglePattern = 10015;

    public const int IsScrollPatternAvailableProperty = 30034, HorizontalScrollPercentProperty = 30053, HorizontalViewSizeProperty = 30054,
        VerticalScrollPercentProperty = 30055, VerticalViewSizeProperty = 30056, HorizontallyScrollableProperty = 30057, VerticallyScrollableProperty = 30058;

    public const int ControlTypeProperty = 30003, LocalizedControlTypeProperty = 30004, NameProperty = 30005, HasKeyboardFocusProperty = 30008,
        HelpTextProperty = 30013, LabeledByProperty = 30018, PositionInSetProperty = 30152, SizeOfSetProperty = 30153, FullDescriptionProperty = 30159,
        IsKeyboardFocusableProperty = 30009, IsEnabledProperty = 30010, AutomationIdProperty = 30011,
        ClassNameProperty = 30012, IsPasswordProperty = 30019, IsOffscreenProperty = 30022, FrameworkIdProperty = 30024,
        ValueValueProperty = 30045, RangeValueValueProperty = 30047, ExpandCollapseStateProperty = 30070, LevelProperty = 30154, SelectionItemIsSelectedProperty = 30079, ToggleStateProperty = 30086,
        LiveSettingProperty = 30135, SelectionSelectionProperty = 30059, SelectionCanSelectMultipleProperty = 30060, SelectionIsSelectionRequiredProperty = 30061;

    public const int TableControlType = 50036, DataItemControlType = 50029, HeaderItemControlType = 50035, SeparatorControlType = 50038;

    public const int ButtonControlType = 50000, CheckBoxControlType = 50002, ComboBoxControlType = 50003, ListItemControlType = 50007, ListControlType = 50008, TreeControlType = 50023, TreeItemControlType = 50024, TabControlType = 50018, TabItemControlType = 50019, ThumbControlType = 50027, HyperlinkControlType = 50005, SplitButtonControlType = 50031, MenuBarControlType = 50010, CalendarControlType = 50001, TitleBarControlType = 50037, ToolBarControlType = 50021, StatusBarControlType = 50017, EditControlType = 50004, ImageControlType = 50006, MenuControlType = 50009, ProgressBarControlType = 50012, RadioButtonControlType = 50013, SliderControlType = 50015, MenuItemControlType = 50011, ToolTipControlType = 50022,
        TextControlType = 50020, GroupControlType = 50026, WindowControlType = 50032, PaneControlType = 50033;

    public const int StructureChangedEvent = 20002, AutomationFocusChangedEvent = 20005, InvokeInvokedEvent = 20009,
        SelectionItemElementAddedToSelectionEvent = 20010, SelectionItemElementRemovedFromSelectionEvent = 20011,
        SelectionItemElementSelectedEvent = 20012, LiveRegionChangedEvent = 20024, NotificationEvent = 20035,
        TextSelectionChangedEvent = 20014, TextChangedEvent = 20015;

    public const int IsReadOnlyAttribute = 40015;

    public const int NotificationKindOther = 4, NotificationProcessingImportantAll = 0, NotificationProcessingAll = 2;

    public const int E_ELEMENTNOTENABLED = unchecked((int)0x80040200), E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201),
        E_INVALIDOPERATION = unchecked((int)0x80131509);

    public const double NoScroll = -1;

    [LibraryImport("UIAutomationCore.dll")]
    public static partial nint UiaReturnRawElementProvider(nint hwnd, nint wParam, nint lParam, IRawElementProviderSimple? element);

    [LibraryImport("UIAutomationCore.dll")]
    public static partial int UiaHostProviderFromHwnd(nint hwnd, out nint provider);

    [LibraryImport("UIAutomationCore.dll")]
    public static partial int UiaRaiseAutomationEvent(IRawElementProviderSimple provider, int eventId);

    [LibraryImport("UIAutomationCore.dll")]
    public static partial int UiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple provider, int propertyId, Variant oldValue, Variant newValue);

    [LibraryImport("UIAutomationCore.dll")]
    public static partial int UiaRaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangeType type, int* runtimeId, int runtimeIdLength);

    [LibraryImport("UIAutomationCore.dll")]
    public static partial int UiaDisconnectProvider(IRawElementProviderSimple provider);

    [LibraryImport("UIAutomationCore.dll")]
    public static partial int UiaRaiseNotificationEvent(IRawElementProviderSimple provider, int kind, int processing,
        [MarshalAs(UnmanagedType.BStr)] string displayString, [MarshalAs(UnmanagedType.BStr)] string activityId);

    [LibraryImport("UIAutomationCore.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UiaClientsAreListening();

    [LibraryImport("oleaut32.dll")]
    public static partial nint SafeArrayCreateVector(ushort type, int lowerBound, uint count);

    [LibraryImport("oleaut32.dll")]
    public static partial int SafeArrayAccessData(nint array, void** data);

    [LibraryImport("oleaut32.dll")]
    public static partial int SafeArrayUnaccessData(nint array);

    [LibraryImport("oleaut32.dll")]
    public static partial int SafeArrayPutElement(nint array, int* index, nint value);

    [LibraryImport("oleaut32.dll")]
    public static partial int SafeArrayPutElement(nint array, int* index, double* value);

    [LibraryImport("UIAutomationCore.dll")]
    public static partial int UiaGetReservedNotSupportedValue(out nint notSupportedValue);

    public static nint CreateUnknownArray<T>(IReadOnlyList<T> items) where T : class
    {
        var array = SafeArrayCreateVector(Variant.VT_UNKNOWN, 0, (uint)items.Count);
        if (array == 0) throw new OutOfMemoryException();
        for (var i = 0; i < items.Count; i++)
        {
            var unknown = Variant.Unknown(items[i]);
            try
            {
                Marshal.ThrowExceptionForHR(SafeArrayPutElement(array, &i, unknown));
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }
        return array;
    }

    public static nint CreateRectangleArray(IReadOnlyList<UiaRect> rectangles)
    {
        var array = SafeArrayCreateVector(Variant.VT_R8, 0, (uint)rectangles.Count * 4);
        if (array == 0) throw new OutOfMemoryException();
        var values = stackalloc double[4];
        for (var i = 0; i < rectangles.Count; i++)
        {
            var r = rectangles[i];
            (values[0], values[1], values[2], values[3]) = (r.Left, r.Top, r.Width, r.Height);
            for (var j = 0; j < 4; j++)
            {
                var index = i * 4 + j;
                Marshal.ThrowExceptionForHR(SafeArrayPutElement(array, &index, values + j));
            }
        }
        return array;
    }

    public static nint CreateProviderArray(IReadOnlyList<IRawElementProviderSimple> providers) => CreateUnknownArray(providers);

    public static nint CreateRuntimeId(int id)
    {
        var array = SafeArrayCreateVector(Variant.VT_I4, 0, 2);
        if (array == 0) throw new OutOfMemoryException();
        int* data;
        Marshal.ThrowExceptionForHR(SafeArrayAccessData(array, (void**)&data));
        data[0] = UiaAppendRuntimeId;
        data[1] = id;
        SafeArrayUnaccessData(array);
        return array;
    }
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("987df77b-db06-4d77-8f8a-86a9c3bb90b9")]
internal partial interface IWindowProvider
{
    void SetVisualState(int state);
    void Close();
    [return: MarshalAs(UnmanagedType.Bool)] bool WaitForInputIdle(int milliseconds);
    [return: MarshalAs(UnmanagedType.Bool)] bool GetCanMaximize();
    [return: MarshalAs(UnmanagedType.Bool)] bool GetCanMinimize();
    [return: MarshalAs(UnmanagedType.Bool)] bool GetIsModal();
    int GetWindowVisualState();
    int GetWindowInteractionState();
    [return: MarshalAs(UnmanagedType.Bool)] bool GetIsTopmost();
}
