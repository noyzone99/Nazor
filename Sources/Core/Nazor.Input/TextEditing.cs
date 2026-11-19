using System.Globalization;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.Input;

/// <summary>
/// A selection in a text: from <see cref="Start" /> to <see cref="End" />, the caret at one of them.
/// </summary>
/// <param name="Start">The start offset in the UTF-16 source text.</param>
/// <param name="End">The end offset in the UTF-16 source text.</param>
/// <param name="Caret">The caret (int).</param>
public readonly record struct TextSelection(int Start, int End, int Caret);

/// <summary>
/// Single-line text editing on elements with <c>role="textbox"</c>: the element shows its <c>value</c> attribute,
/// every edit produces a new value delivered through <c>oninput</c>. Caret moves by grapheme (Ctrl: by word),
/// selection follows Shift or a pointer drag, the clipboard and IME composition replace the selection; a <c>maxlength</c>
/// attribute limits the length, and Ctrl+Z and Ctrl+Y (or Ctrl+Shift+Z) undo and redo, with the characters typed in a
/// row as one step. Geometry comes from the scene's text layout.
/// </summary>
public sealed partial class InputManager
{
    private ElementNode? dragSelecting;
    private DateTime lastClickTime;
    private Point lastClickPoint;
    private int clickCount;
    private readonly Dictionary<ElementNode, string> committedText = [];

    private const int UndoLimit = 100;

    private RootComponent? textMenuHost;
    private ElementNode? textMenuBox;

    /// <summary>Clipboard used by text editing; the window host sets the system clipboard.</summary>
    public IClipboard Clipboard { get; set; } = new MemoryClipboard();

    /// <summary>The focused text input, if any.</summary>
    public ElementNode? FocusedTextBox => Focused is { } focused && IsTextBox(focused) ? focused : null;

    private ElementNode? FocusedText => Focused is { } focused && (IsTextBox(focused) || CanSelectText(focused)) ? focused : null;

    private bool CanSelectText(ElementNode element) =>
        element.Attributes.GetValueOrDefault("selectable") is true && scene.GetStyle(element).Get(StyleProperties.UserSelect) != UserSelect.None;

    private string TextOf(ElementNode element) => scene.GetEditableLayout(element)?.Text ??
        string.Concat(element.Children.Select(child => child is TextNode text ? text.Text : child is ElementNode nested ? TextOf(nested) : ""));

    /// <summary>Inserts typed text in the focused text input (characters from the keyboard).</summary>
    /// <param name="text">The text to process.</param>
    public void TextInput(string text)
    {
        if (FocusedTextBox is not { } box || EditState(box).Composition is not null) return;
        var state = EditState(box);
        Replace(box, state.SelectionStart, state.SelectionEnd, text, typing: true);
    }

    /// <summary>
    /// Replaces the whole value of an enabled text input (semantic action, for example the UI Automation Value
    /// pattern); the change is delivered through <c>oninput</c> like typing.
    /// </summary>
    /// <param name="box">The box (ElementNode).</param>
    /// <param name="text">The text to process.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool SetText(ElementNode box, string text)
    {
        if (!CanInteract(box) || !IsTextBox(box) || box.Attributes.GetValueOrDefault("readonly") is true) return false;
        var state = EditState(box);
        state.Composition = null;
        box.SetState(ElementStates.Composing, false);
        Replace(box, 0, state.Value.Length, text);
        return true;
    }

    /// <summary>Shows the IME preedit at the caret; an empty string ends the composition without text.</summary>
    /// <param name="text">The text to process.</param>
    /// <param name="cursor">The cursor (int).</param>
    public void CompositionUpdate(string text, int cursor)
    {
        if (FocusedTextBox is not { } box || box.Attributes.GetValueOrDefault("readonly") is true) return;
        var state = EditState(box);
        if (state.Composition is null && text.Length == 0) return;
        if (state.Composition is null && state.HasSelection) Replace(box, state.SelectionStart, state.SelectionEnd, "");
        state.Composition = text.Length == 0 ? null : text;
        box.SetState(ElementStates.Composing, state.Composition is not null);
        state.CompositionCursor = Math.Clamp(cursor, 0, text.Length);
        RevealCaret(box);
        VisualStateChanged?.Invoke();
    }

    /// <summary>Inserts the text committed by the IME, replacing the preedit.</summary>
    /// <param name="text">The text to process.</param>
    public void CompositionCommit(string text)
    {
        if (FocusedTextBox is not { } box || box.Attributes.GetValueOrDefault("readonly") is true) return;
        var state = EditState(box);
        state.Composition = null;
        box.SetState(ElementStates.Composing, false);
        Replace(box, state.SelectionStart, state.SelectionEnd, text);
    }

    /// <summary>The caret of the focused text input in window coordinates (IME candidate window), or null.</summary>
    /// <returns>The resulting Rect? value.</returns>
    public Rect? GetTextCaretRect()
    {
        if (FocusedTextBox is not { } box || scene.GetEditableLayout(box) is not { } layout) return null;
        var state = EditState(box);
        var content = scene.GetContentBox(box);
        var caret = layout.GetCaretRect(Math.Min(state.DisplayCaret, layout.Text.Length));
        caret = caret.Offset(scene.GetEditableLineOffset(box, layout.LineAt(Math.Min(state.DisplayCaret, layout.Text.Length))), 0);
        return scene.GetTransform(box).TransformBounds(caret.Offset(content.X - state.ScrollX, content.Y - state.ScrollY) with { Width = 1 });
    }

    /// <summary>
    /// The selection of a text box with its caret (the caret alone when nothing is selected), as assistive
    /// technologies read it.
    /// </summary>
    /// <param name="box">The box (ElementNode).</param>
    /// <returns>The resulting TextSelection value.</returns>
    public TextSelection GetTextSelection(ElementNode box)
    {
        var state = EditState(box);
        var length = state.Value.Length;
        return new TextSelection(Math.Min(state.SelectionStart, length), Math.Min(state.SelectionEnd, length), Math.Min(state.CaretIndex, length));
    }

    /// <summary>
    /// Selects [start, end) of a text box with the caret at the end, as Shift+arrows do; false when it is not one.
    /// </summary>
    /// <param name="box">The box (ElementNode).</param>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="end">The end offset in the UTF-16 source text.</param>
    /// <returns>true when the operation succeeds or the condition is satisfied; otherwise, false.</returns>
    public bool SetTextSelection(ElementNode box, int start, int end)
    {
        if (box.IsRemoved || !IsTextBox(box)) return false;
        var state = EditState(box);
        state.SelectionAnchor = Math.Clamp(start, 0, state.Value.Length);
        state.DesiredX = null;
        Move(box, state, end, extend: true);
        return true;
    }

    /// <summary>
    /// The rectangles of [start, end) of a text box in window coordinates, one per line it covers; the caret for an
    /// empty range.
    /// </summary>
    /// <param name="box">The box (ElementNode).</param>
    /// <param name="start">The start offset in the UTF-16 source text.</param>
    /// <param name="end">The end offset in the UTF-16 source text.</param>
    /// <returns>The resulting IReadOnlyList&lt;Rect&gt; value.</returns>
    public IReadOnlyList<Rect> GetTextRangeBounds(ElementNode box, int start, int end)
    {
        scene.Update();
        if (scene.GetEditableLayout(box) is not { } layout) return [];
        var state = EditState(box);
        var content = scene.GetContentBox(box);
        var transform = scene.GetTransform(box);
        (start, end) = (Math.Clamp(start, 0, layout.Text.Length), Math.Clamp(end, 0, layout.Text.Length));
        var rects = start == end ? [layout.GetCaretRect(start)] : layout.GetSelectionRects(start, end);
        return [.. rects.Select(r => transform.TransformBounds(r.Offset(content.X - state.ScrollX +
            scene.GetEditableLineOffset(box, layout.Lines.First(line => r.Y >= line.Top && r.Y < line.Top + line.Height)), content.Y - state.ScrollY)))];
    }

    /// <summary>The position in the text of a text box closest to a point of the window.</summary>
    /// <param name="box">The box (ElementNode).</param>
    /// <param name="point">The position in logical viewport coordinates.</param>
    /// <returns>The resulting int value.</returns>
    public int GetTextIndexAt(ElementNode box, Point point) => HitIndex(box, point);

    /// <summary>The lines of a text box as shown (broken at its width): where each starts and ends.</summary>
    /// <param name="box">The box (ElementNode).</param>
    /// <returns>The resulting IReadOnlyList&lt;(int Start, int End)&gt; value.</returns>
    public IReadOnlyList<(int Start, int End)> GetTextLines(ElementNode box)
    {
        scene.Update();
        var value = EditState(box).Value;
        return scene.GetEditableLayout(box) is { Lines.Count: > 0 } layout && layout.Text == value
            ? [.. layout.Lines.Select(line => (line.Start, line.End))]
            : [(0, value.Length)];
    }

    private static bool IsTextBox(ElementNode element) => Rendering.TextEditorRoles.IsEditor(element);

    private static bool IsMultiline(ElementNode element) => element.Attributes.GetValueOrDefault("multiline") is true;

    private static string ValueOf(ElementNode element) => element.Attributes.GetValueOrDefault("value") as string ?? "";

    private TextEditState EditState(ElementNode element)
    {
        if (element.GetFeature<TextEditState>() is { } state)
        {

            if (!IsTextBox(element) && TextOf(element) is var text && text != state.Value)
            {
                state.Value = text;
                state.CaretIndex = Math.Min(state.CaretIndex, text.Length);
                state.SelectionAnchor = Math.Min(state.SelectionAnchor, text.Length);
            }
            return state;
        }
        var value = IsTextBox(element) ? ValueOf(element) : TextOf(element);
        state = new TextEditState { Value = value, CaretIndex = value.Length, SelectionAnchor = value.Length };
        element.SetFeature(state);
        return state;
    }

    private void OnTextBoxFocused(ElementNode box, bool keyboard)
    {

        if (box == textMenuBox)
        {
            textMenuBox = null;
            return;
        }
        var state = EditState(box);
        committedText[box] = state.Value;
        if (keyboard)
        {
            state.SelectionAnchor = 0;
            state.CaretIndex = state.Value.Length;
        }
        RevealCaret(box);
    }

    private void OnTextBoxBlurred(ElementNode box)
    {
        if (box == textMenuBox) return;
        var state = EditState(box);

        if (state.Composition is { } pending)
        {
            state.Composition = null;
            box.SetState(ElementStates.Composing, false);
            Replace(box, state.SelectionStart, state.SelectionEnd, pending);
        }
        CommitTextChange(box);
    }

    private void CommitTextChange(ElementNode box)
    {
        var value = EditState(box).Value;
        if (!committedText.TryGetValue(box, out var previous) || value == previous) return;
        committedText[box] = value;
        Dispatch(box, "onchange", new ChangeEventArgs { Value = value });
    }

    private void SyncTextBox(ElementNode box)
    {
        if (box.GetFeature<TextEditState>() is not { } state) return;
        var value = ValueOf(box);
        if (value == state.Value) return;
        state.Value = value;
        if (committedText.ContainsKey(box)) committedText[box] = value;
        state.CaretIndex = state.SelectionAnchor = value.Length;
        state.Composition = null;
        box.SetState(ElementStates.Composing, false);

        state.UndoStates.Clear();
        state.RedoStates.Clear();
        state.TypingEnd = null;
    }

    private bool TextBoxKeyDown(ElementNode box, string key, KeyModifiers modifiers)
    {
        var state = EditState(box);
        if (state.Composition is not null) return false;
        var value = state.Value;
        var shift = (modifiers & KeyModifiers.Shift) != 0;
        var control = TextPolicy.IsCommand(modifiers);
        var word = (modifiers & TextPolicy.WordModifier) != 0;
        var line = TextPolicy.LineModifier != KeyModifiers.None && (modifiers & TextPolicy.LineModifier) != 0;
        var document = control || TextPolicy.HomeEndToDocument;
        var multiline = IsMultiline(box);
        if (key is not ("ArrowUp" or "ArrowDown")) state.DesiredX = null;
        switch (key)
        {
            case "Enter" when multiline:
                Replace(box, state.SelectionStart, state.SelectionEnd, "\n");
                return true;
            case "ArrowUp" when multiline && line:
                Move(box, state, 0, shift);
                return true;
            case "ArrowDown" when multiline && line:
                Move(box, state, value.Length, shift);
                return true;
            case "ArrowUp" or "ArrowDown" when multiline:
                MoveVertically(box, state, key == "ArrowUp" ? -1 : 1, shift);
                return true;
            case "Home" when multiline && !document:
                Move(box, state, LineOf(box, state.CaretIndex)?.Start ?? 0, shift);
                return true;
            case "End" when multiline && !document:
                Move(box, state, LineOf(box, state.CaretIndex)?.ContentEnd ?? value.Length, shift);
                return true;
            case "ArrowLeft":
                Move(box, state, line ? LineOf(box, state.CaretIndex)?.Start ?? 0 : word ? PreviousWord(value, state.CaretIndex) : !shift && state.HasSelection ? state.SelectionStart : PreviousGrapheme(value, state.CaretIndex), shift);
                return true;
            case "ArrowRight":
                Move(box, state, line ? LineOf(box, state.CaretIndex)?.ContentEnd ?? value.Length : word ? NextWord(value, state.CaretIndex) : !shift && state.HasSelection ? state.SelectionEnd : NextGrapheme(value, state.CaretIndex), shift);
                return true;
            case "Home":
                Move(box, state, 0, shift);
                return true;
            case "End":
                Move(box, state, value.Length, shift);
                return true;
            case "Backspace":
                if (state.HasSelection) Replace(box, state.SelectionStart, state.SelectionEnd, "");
                else if (state.CaretIndex > 0) Replace(box, line ? LineOf(box, state.CaretIndex)?.Start ?? 0 : word ? PreviousWord(value, state.CaretIndex) : PreviousGrapheme(value, state.CaretIndex), state.CaretIndex, "");
                return true;
            case "Delete":
                if (state.HasSelection) Replace(box, state.SelectionStart, state.SelectionEnd, "");
                else if (state.CaretIndex < value.Length) Replace(box, state.CaretIndex, line ? LineOf(box, state.CaretIndex)?.ContentEnd ?? value.Length : word ? NextWord(value, state.CaretIndex) : NextGrapheme(value, state.CaretIndex), "");
                return true;
        }
        if (!control) return false;
        switch (key.ToLowerInvariant())
        {
            case "a":
                SelectAll(box);
                return true;
            case "c" when state.HasSelection:
                Copy(box);
                return true;
            case "x" when state.HasSelection:
                Cut(box);
                return true;
            case "v":
                Paste(box);
                return true;
            case "z" when shift:
            case "y" when TextPolicy.RedoWithY:
                Restore(box, state.RedoStates, state.UndoStates);
                return true;
            case "z":
                Restore(box, state.UndoStates, state.RedoStates);
                return true;
        }
        return false;
    }

    private static bool IsPassword(ElementNode box) => box.Attributes.GetValueOrDefault("password") is true;

    private void SelectAll(ElementNode box)
    {
        var state = EditState(box);
        state.SelectionAnchor = 0;
        state.CaretIndex = state.Value.Length;
        state.TypingEnd = null;
        RevealCaret(box);
        VisualStateChanged?.Invoke();
    }

    private void Copy(ElementNode box)
    {
        var state = EditState(box);
        if (state.HasSelection && !IsPassword(box)) Clipboard.SetText(state.Value[state.SelectionStart..state.SelectionEnd]);
    }

    private void Cut(ElementNode box)
    {
        var state = EditState(box);
        if (state.HasSelection && !IsPassword(box) && Clipboard.SetText(state.Value[state.SelectionStart..state.SelectionEnd]))
            Replace(box, state.SelectionStart, state.SelectionEnd, "");
    }

    private void Paste(ElementNode box)
    {
        var state = EditState(box);
        if (Clipboard.GetText() is { Length: > 0 } pasted) Replace(box, state.SelectionStart, state.SelectionEnd, pasted);
    }

    private bool SelectableTextKeyDown(ElementNode text, string key, KeyModifiers modifiers)
    {
        if (!TextPolicy.IsCommand(modifiers)) return false;
        switch (key.ToLowerInvariant())
        {
            case "a":
                SelectAll(text);
                return true;
            case "c":
                Copy(text);
                return true;
        }
        return false;
    }

    private void OnSelectableTextBlurred(ElementNode text)
    {
        var state = EditState(text);
        if (!state.HasSelection || text == textMenuBox) return;
        state.SelectionAnchor = state.CaretIndex;
        VisualStateChanged?.Invoke();
    }

    private void ShowTextMenu(ElementNode box, Point at, bool clicked)
    {
        if (IsDisabled(box)) return;
        var editable = IsTextBox(box) && box.Attributes.GetValueOrDefault("readonly") is not true;
        if (Focused != box) Focus(box, visible: false);
        var state = EditState(box);
        if (clicked)
        {
            var index = HitIndex(box, at);
            if (!state.HasSelection || index < state.SelectionStart || index > state.SelectionEnd) Move(box, state, index, extend: false);
        }
        var secret = IsPassword(box);
        TextMenuItem[] items = editable
            ?
            [
                new(Strings.Undo, state.UndoStates.Count > 0, () => Restore(box, state.UndoStates, state.RedoStates)),
                new(Strings.Cut, state.HasSelection && !secret, () => Cut(box)),
                new(Strings.Copy, state.HasSelection && !secret, () => Copy(box)),
                new(Strings.Paste, Clipboard.GetText() is { Length: > 0 }, () => Paste(box)),
                new(Strings.SelectAll, state.Value.Length > 0, () => SelectAll(box)),
            ]
            :
            [
                new(Strings.Copy, state.HasSelection, () => Copy(box)),
                new(Strings.SelectAll, state.Value.Length > 0, () => SelectAll(box)),
            ];
        textMenuBox = box;
        textMenuHost ??= renderer.AddRootComponent<TextMenuHost>();
        ((TextMenuHost)textMenuHost.Component).Show(at.X, at.Y, items);
    }

    private ElementNode? TextBoxForMenu(ElementNode? target) =>
        Ancestors(target).Any(e => e.Attributes.GetValueOrDefault("oncontextmenu") is IEventCallback { HasDelegate: true })
            ? null
            : Ancestors(target).FirstOrDefault(e => IsTextBox(e) || CanSelectText(e));

    private void Restore(ElementNode box, List<TextSnapshot> from, List<TextSnapshot> to)
    {
        if (box.IsDisabled || box.Attributes.GetValueOrDefault("readonly") is true) return;
        var state = EditState(box);
        state.TypingEnd = null;
        if (from.Count == 0) return;
        var snapshot = from[^1];
        from.RemoveAt(from.Count - 1);
        to.Add(new TextSnapshot(state.Value, state.CaretIndex, state.SelectionAnchor));
        state.Value = snapshot.Value;
        state.CaretIndex = Math.Min(snapshot.CaretIndex, snapshot.Value.Length);
        state.SelectionAnchor = Math.Min(snapshot.SelectionAnchor, snapshot.Value.Length);
        Dispatch(box, "oninput", new ChangeEventArgs { Value = state.Value });
        RevealCaret(box);
        VisualStateChanged?.Invoke();
    }

    private void TextBoxPointerPressed(ElementNode box, Point point, KeyModifiers modifiers)
    {
        var state = EditState(box);
        var now = DateTime.UtcNow;
        var near = Math.Abs(point.X - lastClickPoint.X) <= 4 && Math.Abs(point.Y - lastClickPoint.Y) <= 4;
        clickCount = near && now - lastClickTime < TimeSpan.FromMilliseconds(500) ? clickCount + 1 : 1;
        lastClickTime = now;
        lastClickPoint = point;
        var index = HitIndex(box, point);

        var whole = !IsTextBox(box) && scene.GetStyle(box).Get(StyleProperties.UserSelect) == UserSelect.All;
        switch (whole ? 3 : clickCount)
        {
            case 2:
                state.SelectionAnchor = WordStart(state.Value, index);
                state.CaretIndex = WordEnd(state.Value, index);
                break;
            case >= 3:
                state.SelectionAnchor = 0;
                state.CaretIndex = state.Value.Length;
                break;
            default:
                state.CaretIndex = index;
                if ((modifiers & KeyModifiers.Shift) == 0) state.SelectionAnchor = index;
                dragSelecting = box;
                break;
        }
        RevealCaret(box);
        VisualStateChanged?.Invoke();
    }

    private void TextBoxPointerMoved(Point point)
    {
        if (dragSelecting is not { IsRemoved: false } box) return;
        var state = EditState(box);
        var index = HitIndex(box, point);
        if (index == state.CaretIndex) return;
        state.CaretIndex = index;
        RevealCaret(box);
        VisualStateChanged?.Invoke();
    }

    private TextLine? LineOf(ElementNode box, int index)
    {
        scene.Update();
        return scene.GetEditableLayout(box) is { } layout ? layout.LineAt(index) : null;
    }

    private void MoveVertically(ElementNode box, TextEditState state, int direction, bool extend)
    {
        scene.Update();
        if (scene.GetEditableLayout(box) is not { } layout) return;
        var caret = layout.GetCaretRect(Math.Min(state.CaretIndex, layout.Text.Length));
        caret = caret.Offset(scene.GetEditableLineOffset(box, layout.LineAt(Math.Min(state.CaretIndex, layout.Text.Length))), 0);
        var x = state.DesiredX ?? caret.X;
        var line = layout.LineAt(state.CaretIndex);
        var row = layout.Lines.ToList().IndexOf(line) + direction;
        var outside = row < 0 || row >= layout.Lines.Count;
        var index = row < 0 ? 0 : outside ? state.Value.Length : layout.HitTest(new Point(
            x - scene.GetEditableLineOffset(box, layout.Lines[row]), layout.Lines[row].Top + layout.Lines[row].Height / 2));
        Move(box, state, index, extend);

        state.DesiredX = outside ? null : x;
    }

    private int HitIndex(ElementNode box, Point point)
    {
        scene.Update();
        if (scene.GetEditableLayout(box) is not { } layout || !scene.GetTransform(box).TryInvert(out var toBox)) return 0;

        point = toBox.Transform(point);
        var content = scene.GetContentBox(box);
        var state = EditState(box);
        var local = new Point(point.X - content.X + state.ScrollX, point.Y - content.Y + state.ScrollY);
        var line = layout.Lines.FirstOrDefault(line => local.Y < line.Top + line.Height) ?? layout.Lines[^1];
        return Math.Min(layout.HitTest(local with { X = local.X - scene.GetEditableLineOffset(box, line) }), state.Value.Length);
    }

    private void Move(ElementNode box, TextEditState state, int index, bool extend)
    {
        state.TypingEnd = null;
        state.CaretIndex = Math.Clamp(index, 0, state.Value.Length);
        if (!extend) state.SelectionAnchor = state.CaretIndex;
        RevealCaret(box);
        VisualStateChanged?.Invoke();
    }

    private void Replace(ElementNode box, int start, int end, string text, bool typing = false)
    {
        if (box.IsDisabled || box.Attributes.GetValueOrDefault("readonly") is true) return;
        var state = EditState(box);

        text = IsMultiline(box) ? text.Replace("\r\n", "\n").Replace('\r', '\n') : text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        start = Math.Clamp(start, 0, state.Value.Length);
        end = Math.Clamp(end, start, state.Value.Length);

        if (box.Attributes.GetValueOrDefault("maxlength") is int max and > 0)
            text = Truncate(text, Math.Max(0, max - (state.Value.Length - (end - start))));
        var value = string.Concat(state.Value.AsSpan(0, start), text, state.Value.AsSpan(end));
        if (value == state.Value) return;

        if (!(typing && end == start && state.TypingEnd == start))
        {
            state.UndoStates.Add(new TextSnapshot(state.Value, state.CaretIndex, state.SelectionAnchor));
            if (state.UndoStates.Count > UndoLimit) state.UndoStates.RemoveAt(0);
        }
        state.RedoStates.Clear();
        state.Value = value;
        state.CaretIndex = state.SelectionAnchor = start + text.Length;
        state.TypingEnd = typing ? state.CaretIndex : null;
        Dispatch(box, "oninput", new ChangeEventArgs { Value = value });
        RevealCaret(box);
        VisualStateChanged?.Invoke();
    }

    private void RevealCaret(ElementNode box)
    {
        scene.Update();
        if (scene.GetEditableLayout(box) is not { } layout) return;
        var state = EditState(box);
        var content = scene.GetContentBox(box);
        var caret = layout.GetCaretRect(Math.Min(state.DisplayCaret, layout.Text.Length));
        caret = caret.Offset(scene.GetEditableLineOffset(box, layout.LineAt(Math.Min(state.DisplayCaret, layout.Text.Length))), 0);
        if (caret.X - state.ScrollX > content.Width - 1) state.ScrollX = caret.X - content.Width + 1;
        if (caret.X - state.ScrollX < 0) state.ScrollX = caret.X;
        state.ScrollX = Math.Clamp(state.ScrollX, 0, Math.Max(0, layout.Width - content.Width + 1));
        if (caret.Bottom - state.ScrollY > content.Height) state.ScrollY = caret.Bottom - content.Height;
        if (caret.Y - state.ScrollY < 0) state.ScrollY = caret.Y;
        state.ScrollY = Math.Clamp(state.ScrollY, 0, Math.Max(0, layout.Height - content.Height));
    }

    private static string Truncate(string text, int length)
    {
        if (text.Length <= length) return text;
        var cut = 0;
        foreach (var boundary in StringInfo.ParseCombiningCharacters(text))
        {
            if (boundary > length) break;
            cut = boundary;
        }
        return text[..cut];
    }

    private static int NextGrapheme(string text, int index) =>
        index >= text.Length ? text.Length : index + Math.Max(1, StringInfo.GetNextTextElementLength(text, index));

    private static int PreviousGrapheme(string text, int index)
    {
        var previous = 0;
        for (var i = 0; i < index && i < text.Length; i = NextGrapheme(text, i)) previous = i;
        return Math.Min(previous, index);
    }

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static int NextWord(string text, int index)
    {
        var i = index;
        while (i < text.Length && !IsWordCharacter(text[i])) i++;
        while (i < text.Length && IsWordCharacter(text[i])) i++;
        return i;
    }

    private static int PreviousWord(string text, int index)
    {
        var i = index;
        while (i > 0 && !IsWordCharacter(text[i - 1])) i--;
        while (i > 0 && IsWordCharacter(text[i - 1])) i--;
        return i;
    }

    private static int WordStart(string text, int index)
    {
        var i = Math.Min(index, text.Length);
        while (i > 0 && IsWordCharacter(text[i - 1])) i--;
        return i;
    }

    private static int WordEnd(string text, int index)
    {
        var i = Math.Min(index, text.Length);
        while (i < text.Length && IsWordCharacter(text[i])) i++;
        return i;
    }
}
