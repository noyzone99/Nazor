using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Nazor.VisualStudio.CodeLens.Tests;

internal static class EditingSequenceChecks
{
    internal static void Check(Assembly extension, ITextBuffer buffer, Assembly editor)
    {
        var elapsed = Stopwatch.StartNew();
        var harness = new Keyboard(extension, buffer, editor);
        harness.BraceCompletion = false;
        harness.CSharpBraceCompletion = true;
        harness.Load("@code {\n    void Save() |\n}");
        harness.Type("{");
        harness.Return();
        harness.Expect("@code {\n    void Save()\n    {\n        |\n    }\n}");
        harness.BraceCompletion = true;
        harness.CSharpBraceCompletion = false;
        harness.Load("@code {\n    void Save() |\n}");
        harness.Type("{");
        harness.Expect("@code {\n    void Save() {|\n}");
        harness.CSharpBraceCompletion = null;
        var client = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(extension.GetType("Nazor.VisualStudio.NazorLanguageClient", true));
        var initialization = client.GetType().GetProperty("InitializationOptions").GetValue(client);
        if (!(bool)initialization.GetType().GetProperty("nativeEditing").GetValue(initialization)) throw new Exception("Native editing must disable competing server auto-insertion");
        harness.Load("|");
        harness.Type("@code ");
        harness.Type("{");
        harness.Expect("@code {|}");
        harness.Return();
        harness.Expect("@code {\n    |\n}");
        harness.Type("private void Save");
        harness.Type("(");
        harness.Expect("@code {\n    private void Save(|)\n}");
        harness.Type(") ");
        harness.Type("{");
        harness.Return();
        harness.Expect("@code {\n    private void Save()\n    {\n        |\n    }\n}");
        harness.Type("Run();");
        harness.Return();
        harness.Expect("@code {\n    private void Save()\n    {\n        Run();\n        |\n    }\n}");
        harness.Undo();
        harness.Expect("@code {\n    private void Save()\n    {\n        Run();|\n    }\n}");
        harness.Redo();
        harness.Expect("@code {\n    private void Save()\n    {\n        Run();\n        |\n    }\n}");

        harness.Load("@code {\n    private void Run|}");
        harness.Type("(");
        harness.Expect("@code {\n    private void Run(|)}");
        harness.Backspace();
        harness.Expect("@code {\n    private void Run|}");
        harness.Undo();
        harness.Expect("@code {\n    private void Run(|)}");
        harness.Redo();
        harness.Expect("@code {\n    private void Run|}");

        harness.Load("<Container|");
        harness.Type(">");
        harness.Expect("<Container>|</Container>");
        harness.Return();
        harness.Expect("<Container>\n    |\n</Container>");
        harness.Type("<Button Text=");
        harness.Expect("<Container>\n    <Button Text=\"|\"\n</Container>");
        harness.Type("Save\" />");
        harness.Expect("<Container>\n    <Button Text=\"Save\" />|\n</Container>");

        harness.BraceCompletion = false;
        harness.CSharpBraceCompletion = false;
        harness.Load("@style |");
        harness.Type("{");
        harness.Expect("@style {|}");
        harness.Return();
        harness.Type(".primary ");
        harness.Type("{");
        harness.Return();
        harness.Expect("@style {\n    .primary {\n        |\n    }\n}");
        harness.Type("color: rgb");
        harness.Type("(");
        harness.Type("1, 2, 3)");
        harness.Expect("@style {\n    .primary {\n        color: rgb(1, 2, 3)|\n    }\n}");
        harness.Load("@style {\n    Button|\n}");
        harness.Type("[");
        harness.Expect("@style {\n    Button[|]\n}");
        harness.Backspace();
        harness.Expect("@style {\n    Button|\n}");
        harness.Undo();
        harness.Expect("@style {\n    Button[|]\n}");
        harness.Redo();
        harness.Expect("@style {\n    Button|\n}");
        harness.Load("@style {\n    .primary { content: \"text |\"; }\n}");
        harness.Type("{");
        harness.Expect("@style {\n    .primary { content: \"text {|\"; }\n}");
        harness.Load("@style {\n    /* | */\n}");
        harness.Type("{");
        harness.Expect("@style {\n    /* {| */\n}");
        harness.BraceCompletion = true;
        harness.CSharpBraceCompletion = null;

        harness.Load("<Button @onclick=\"() => |\" />");
        harness.Type("{");
        if (!harness.Display.Contains("}")) throw new Exception("An embedded C# lambda did not get its closer");
        harness.Load("@window|");
        harness.Type("(");
        harness.Expect("@window(|)");
        harness.Load("@(Run|)");
        harness.Type("(");
        harness.Expect("@(Run(|))");

        foreach (var declaration in new[] { "private void Save()", "public Widget()", "public int Value", "get", "set", "init", "void Local()", "if (ready)", "foreach (var item in items)", "Action action = () =>" })
        {
            var member = declaration.StartsWith("public", StringComparison.Ordinal) || declaration.StartsWith("private", StringComparison.Ordinal);
            var accessor = declaration == "get" || declaration == "set" || declaration == "init";
            var root = member ? "@code {\n    " : accessor ? "@code {\n    public int Value\n    {\n        " : "@code {\n    void Run()\n    {\n        ";
            var tail = member ? "\n}" : "\n    }\n}";
            var indent = member ? "    " : "        ";
            harness.Load(root + "|" + tail);
            harness.Type(declaration + " {");
            var beforeReturn = harness.Display;
            harness.Return();
            harness.Expect(root + declaration + "\n" + indent + "{\n" + indent + "    |\n" + indent + "}" + tail);
            harness.Undo();
            harness.Expect(beforeReturn);
            harness.Redo();
            harness.Expect(root + declaration + "\n" + indent + "{\n" + indent + "    |\n" + indent + "}" + tail);
        }

        harness.Profile(new Dictionary<string, string> { ["csharp_new_line_before_open_brace"] = "none" });
        harness.Load("@code {\n    |\n}");
        harness.Type("void Save() {");
        harness.Return();
        harness.Expect("@code {\n    void Save() {\n        |\n    }\n}");
        harness.Profile(new Dictionary<string, string> { ["indent_size"] = "2", ["tab_width"] = "4", ["indent_style"] = "space" });
        harness.Load("@code {\n  |\n}");
        harness.Type("void Save() {");
        harness.Return();
        harness.Expect("@code {\n  void Save()\n  {\n    |\n  }\n}");
        harness.Type("Run();");
        harness.Expect("@code {\n  void Save()\n  {\n    Run();|\n  }\n}");
        harness.Profile(new Dictionary<string, string> { ["indent_size"] = "4", ["tab_width"] = "4", ["indent_style"] = "tab" });
        harness.Newline = "\r\n";
        harness.Load("@code {\r\n\t|\r\n}");
        harness.Type("void Save() {");
        harness.Return();
        harness.Expect("@code {\r\n\tvoid Save()\r\n\t{\r\n        |\r\n\t}\r\n}");
        harness.Type("Run();");
        harness.Expect("@code {\r\n\tvoid Save()\r\n\t{\r\n\t\tRun();|\r\n\t}\r\n}");
        harness.Newline = "\n";
        harness.Profile(new Dictionary<string, string>());
        var fixture = Path.Combine(Path.GetTempPath(), "NazorEditing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);
        var config = Path.Combine(fixture, ".editorconfig");
        try
        {
            File.WriteAllText(config, "root=true\n[*.nazor.cs]\nindent_size=2\nindent_style=space\ncsharp_new_line_before_open_brace=none\n");
            var configured = new Keyboard(extension, buffer, editor, Path.Combine(fixture, "Example.nazor"));
            configured.Load("@code {\n  |\n}");
            configured.Type("void Save() {");
            configured.Return();
            configured.Expect("@code {\n  void Save() {\n    |\n  }\n}");
            harness.Assertions += configured.Assertions;
        }
        finally { File.Delete(config); Directory.Delete(fixture); }

        harness.Load("@code {\n    void Run() {|}\n}");
        harness.Return();
        harness.Expect("@code {\n    void Run()\n    {\n        |\n    }\n}");
        harness.Load("@code {\n    void Run()\n    {\n        int value=1|\n    }\n}");
        harness.Type(";");
        harness.Expect("@code {\n    void Run()\n    {\n        int value = 1;|\n    }\n}");
        harness.Undo();
        harness.Expect("@code {\n    void Run()\n    {\n        int value=1|\n    }\n}");
        harness.Profile(new Dictionary<string, string> { ["csharp_format_on_semicolon"] = "false" });
        harness.Type(";");
        harness.Expect("@code {\n    void Run()\n    {\n        int value=1;|\n    }\n}");
        harness.Profile(new Dictionary<string, string>());
        harness.Load("@code {\n    |\n}");
        harness.Type("void Run() {");
        harness.Return();
        harness.Type("Run();");
        harness.Return();
        var beforeClose = harness.Display;
        harness.Type("}");
        harness.Expect("@code {\n    void Run()\n    {\n        Run();\n    }|\n}");
        harness.Undo();
        harness.Expect(beforeClose);
        harness.Redo();
        harness.Expect("@code {\n    void Run()\n    {\n        Run();\n    }|\n}");
        harness.Load("@code {|}");
        harness.Return();
        harness.Expect("@code {\n    |\n}");
        harness.Load("<Container>|</Container>");
        harness.Return();
        harness.Expect("<Container>\n    |\n</Container>");
        harness.Load("<Container>\n    @code {\n        |\n    }\n</Container>");
        harness.Type("void Run() {");
        harness.Return();
        harness.Expect("<Container>\n    @code {\n        void Run()\n        {\n            |\n        }\n    }\n</Container>");
        harness.Load("@code {\n    string value = |;\n}");
        harness.Type("$\"Hello {name}\"");
        harness.Expect("@code {\n    string value = $\"Hello {name}\"|;\n}");
        harness.Load("@code {\n    string value = \"\"\"raw |\"\"\";\n}");
        harness.Type("([{\"");
        harness.Expect("@code {\n    string value = \"\"\"raw ([{\"|\"\"\";\n}");
        harness.Load("@code {\n    void Run()\n    {\n        var value = new Person |\n    }\n}");
        harness.Type("{");
        harness.Return();
        harness.Expect("@code {\n    void Run()\n    {\n        var value = new Person\n        {\n            |\n        }\n    }\n}");

        harness.Load("@code {\n    void Run() { var value = | }\n}");
        harness.Type("items[");
        harness.Expect("@code {\n    void Run() { var value = items[|] }\n}");
        harness.Tab();
        harness.Expect("@code {\n    void Run() { var value = items[]| }\n}");
        harness.Undo();
        harness.Expect("@code {\n    void Run() { var value = items[|] }\n}");
        harness.Redo();
        harness.Expect("@code {\n    void Run() { var value = items[]| }\n}");
        harness.Load("@code {\n    string value = |;\n}");
        harness.Type("\"hello ( [ {\"");
        harness.Expect("@code {\n    string value = \"hello ( [ {\"|;\n}");
        harness.Load("@code {\n    // |\n}");
        harness.Type("([{\"");
        harness.Expect("@code {\n    // ([{\"|\n}");
        harness.Load("@code {\n    void Run() { if (a | b) {} }\n}");
        harness.Type("<");
        harness.Expect("@code {\n    void Run() { if (a <| b) {} }\n}");
        harness.Load("@code {\n    List| values;\n}");
        harness.Type("<int>");
        harness.Expect("@code {\n    List<int>| values;\n}");
        harness.BraceCompletion = false;
        harness.Load("@code {\n    void Run|\n}");
        harness.Type("(");
        harness.Expect("@code {\n    void Run(|\n}");
        harness.BraceCompletion = true;
        harness.Load("@functions|");
        harness.Type("{");
        harness.Expect("@functions{|}");
        harness.Load("@if (ready) |");
        harness.Type("{");
        harness.Return();
        harness.Expect("@if (ready)\n{\n    |\n}");
        harness.Load("<Button Text=\"@(Run|)\" />");
        harness.Type("(");
        harness.Expect("<Button Text=\"@(Run(|))\" />");
        harness.Load("<Container|</Container>");
        harness.Type(">");
        harness.Expect("<Container>|</Container>");
        harness.Load("<Container />\n<Container|");
        harness.Type(">");
        harness.Expect("<Container />\n<Container>|</Container>");
        harness.Load("<Container Text=\"> </Container>\"|");
        harness.Type(">");
        harness.Expect("<Container Text=\"> </Container>\">|</Container>");
        harness.Load("<!-- </Container> -->\n<Container|");
        harness.Type(">");
        harness.Expect("<!-- </Container> -->\n<Container>|</Container>");
        foreach (var comment in new[] { "@*", "<!--" })
        {
            harness.Load("|");
            harness.Type(comment);
            harness.Expect(comment + "|" + (comment == "@*" ? "*@" : "-->"));
            harness.Type("(");
            harness.Expect(comment + "(|" + (comment == "@*" ? "*@" : "-->"));
        }
        var assertions = harness.Assertions;
        object Create(string name) => Activator.CreateInstance(editor.GetType(name, true), true);
        var content = editor.GetType("Microsoft.VisualStudio.Text.Implementation.StringRebuilder", true).GetMethod("Create", new[] { typeof(string) }).Invoke(null, new object[] { "" });
        var styles = (ITextBuffer)editor.GetType("Microsoft.VisualStudio.Text.Implementation.TextBuffer", true).GetConstructors().Single(c => c.GetParameters().Length == 5).Invoke(
            new object[] { new StyleContentType(), content, null, Create("Microsoft.VisualStudio.Text.Utilities.GuardedOperations"), Create("Microsoft.VisualStudio.Text.Implementation.BufferFactoryService") });
        harness = new Keyboard(extension, styles, editor);
        harness.BraceCompletion = false;
        harness.CSharpBraceCompletion = false;
        harness.Load("|");
        harness.Type(".primary {");
        harness.Expect(".primary {|}");
        harness.Return();
        harness.Expect(".primary {\n    |\n}");
        harness.Type("color: rgb(1, 2, 3);");
        harness.Return();
        harness.Expect(".primary {\n    color: rgb(1, 2, 3);\n    |\n}");
        harness.Load("|");
        harness.Type("/*");
        harness.Expect("/*|*/");
        harness.Backspace();
        harness.Expect("|");
        harness.Load("[Kind|");
        harness.Type("=");
        harness.Type("\"Button\"");
        harness.Expect("[Kind=\"Button\"|");
        harness.Load(".primary {|}");
        harness.Return();
        harness.Expect(".primary {\n    |\n}");
        harness.Load("Button|");
        harness.Type("[");
        harness.Expect("Button[|]");
        harness.Tab();
        harness.Expect("Button[]|");
        harness.Load(".primary { /* | */ }");
        harness.Type("{");
        harness.Expect(".primary { /* {| */ }");
        harness.Load(".primary { content: \"  |  \"; }");
        harness.Type("(");
        harness.Expect(".primary { content: \"  (|  \"; }");
        buffer.Delete(new Span(0, buffer.CurrentSnapshot.Length));
        Console.WriteLine("PASS: " + (assertions + harness.Assertions) + " native command-sequence assertions cover C#/NSS/markup typing, profiles, Return, Tab, overtype, Backspace and native buffer undo/redo in " + elapsed.ElapsedMilliseconds + " ms.");
    }

    private sealed class StyleContentType : IContentType
    {
        public string TypeName => "NazorStyleSheet";
        public string DisplayName => TypeName;
        public IEnumerable<IContentType> BaseTypes => Array.Empty<IContentType>();
        public bool IsOfType(string type) => type == TypeName;
    }

    private sealed class Keyboard
    {
        private readonly ITextBuffer buffer;
        private readonly object commands;
        private readonly ITextView view;
        private readonly History history;
        private int caret;
        private int virtualSpaces;
        private PropertyCollection properties;
        private (int Caret, int Virtual) before;
        private readonly Assembly extension;
        internal string Newline = "\n";
        internal int Assertions;
        internal bool BraceCompletion = true;
        internal bool? CSharpBraceCompletion;
        internal void Profile(IReadOnlyDictionary<string, string> values) => extension.GetType("Nazor.VisualStudio.NazorCSharpOptions", true)
            .GetField("Current", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, values);

        internal Keyboard(Assembly extension, ITextBuffer buffer, Assembly editor, string file = null)
        {
            this.buffer = buffer;
            this.extension = extension;
            history = new History(() => (caret, virtualSpaces), state => { caret = state.Item1; virtualSpaces = state.Item2; });
            var registry = Proxy<ITextUndoHistoryRegistry>((method, args) => method.Name == "RegisterHistory" ? history.Value : Default(method.ReturnType));
            var manager = (ITextBufferUndoManager)Activator.CreateInstance(editor.GetType("Microsoft.VisualStudio.Text.BufferUndoManager.Implementation.TextBufferUndoManager", true), buffer, registry);
            var managers = Proxy<ITextBufferUndoManagerProvider>((method, args) => manager);
            var mapping = Proxy<IMappingPoint>((method, args) => new SnapshotPoint(buffer.CurrentSnapshot, caret));
            var cursor = Proxy<ITextCaret>((method, args) =>
            {
                if (method.Name == "get_Position") return new CaretPosition(new VirtualSnapshotPoint(new SnapshotPoint(buffer.CurrentSnapshot, caret), virtualSpaces), mapping, PositionAffinity.Successor);
                if (method.Name == "MoveTo")
                {
                    if (args[0] is VirtualSnapshotPoint point) { caret = point.Position.Position; virtualSpaces = point.VirtualSpaces; }
                    else if (args[0] is SnapshotPoint simple) { caret = simple.Position; virtualSpaces = 0; }
                }
                return Default(method.ReturnType);
            });
            var options = Proxy<IEditorOptions>((method, args) =>
            {
                if (method.Name != "GetOptionValue") return Default(method.ReturnType);
                var type = method.GetGenericArguments()[0];
                if (type == typeof(int)) return 4;
                if (type == typeof(string)) return Newline;
                return args[0].Equals(DefaultTextViewOptions.BraceCompletionEnabledOptionId) ? BraceCompletion : true;
            });
            var graph = Proxy<IBufferGraph>((method, args) => method.Name == "MapUpToBuffer" ? args[0] : Default(method.ReturnType));
            var selection = Proxy<ITextSelection>((method, args) => method.Name == "get_IsEmpty" ? true : Default(method.ReturnType));
            view = Proxy<ITextView>((method, args) => method.Name switch
            {
                "get_Caret" => cursor, "get_Options" => options, "get_BufferGraph" => graph, "get_Selection" => selection,
                "get_Properties" => properties, "get_TextBuffer" => buffer, "get_TextSnapshot" => buffer.CurrentSnapshot, _ => Default(method.ReturnType),
            });
            var ops = Proxy<IEditorOperations>((method, args) =>
            {
                if (method.Name == "AddBeforeTextBufferChangePrimitive") before = (caret, virtualSpaces);
                if (method.Name == "AddAfterTextBufferChangePrimitive") history.AddCaret(before);
                return Default(method.ReturnType);
            });
            var operations = Proxy<IEditorOperationsFactoryService>((method, args) => ops);
            var document = Proxy<ITextDocument>((method, args) => method.Name == "get_FilePath" ? file : Default(method.ReturnType));
            var documents = Proxy<ITextDocumentFactoryService>((method, args) =>
            {
                if (method.Name == "TryGetTextDocument") { args[1] = document; return file != null; }
                return Default(method.ReturnType);
            });
            var textManager = Proxy<IVsTextManager4>((method, args) =>
            {
                if (method.Name != "GetUserPreferences4") throw new Exception("Unexpected native preference call");
                var preferences = (LANGPREFERENCES3[])args[1];
                if (preferences[0].guidLang != new Guid("694dd9b6-b865-4c5b-ad85-86356e9c88dc")) throw new Exception("C# language preferences required");
                preferences[0].fBraceCompletion = CSharpBraceCompletion == true ? 1u : 0u;
                return VSConstants.S_OK;
            });
            var services = Proxy<IServiceProvider>((method, args) => CSharpBraceCompletion.HasValue ? textManager : null);
            commands = Activator.CreateInstance(extension.GetType("Nazor.VisualStudio.NazorEditingCommands", true), managers, operations, documents, null, services);
        }

        internal string Display
        {
            get
            {
                var text = buffer.CurrentSnapshot.GetText();
                return text.Insert(caret, new string(' ', virtualSpaces) + "|");
            }
        }
        internal void Load(string marked)
        {
            history.Reset();
            buffer.Replace(new Span(0, buffer.CurrentSnapshot.Length), marked.Replace("|", ""));
            caret = marked.IndexOf('|');
            virtualSpaces = 0;
            properties = new PropertyCollection();
            history.Reset();
        }
        internal void Type(string text)
        {
            foreach (var character in text)
                ((IChainedCommandHandler<TypeCharCommandArgs>)commands).ExecuteCommand(new TypeCharCommandArgs(view, buffer, character), () => Fallback(character.ToString()), null);
        }
        internal void Return() => ((IChainedCommandHandler<ReturnKeyCommandArgs>)commands).ExecuteCommand(new ReturnKeyCommandArgs(view, buffer), () => Fallback(Newline), null);
        internal void Tab() => ((IChainedCommandHandler<TabKeyCommandArgs>)commands).ExecuteCommand(new TabKeyCommandArgs(view, buffer), () => throw new Exception("Unexpected Tab fallback"), null);
        internal void Backspace() => ((IChainedCommandHandler<BackspaceKeyCommandArgs>)commands).ExecuteCommand(new BackspaceKeyCommandArgs(view, buffer), () => throw new Exception("Unexpected Backspace fallback"), null);
        internal void Undo() => history.Undo();
        internal void Redo() => history.Redo();
        internal void Expect(string expected)
        {
            Assertions++;
            if (Display != expected) throw new Exception("Editing sequence mismatch.\nExpected: " + expected.Replace("\n", "\\n") + "\nActual: " + Display.Replace("\n", "\\n"));
        }
        private void Fallback(string text)
        {
            using (var transaction = history.Value.CreateTransaction("Keyboard insertion"))
            {
                var start = (caret, virtualSpaces);
                buffer.Insert(caret, new string(' ', virtualSpaces) + text);
                caret += virtualSpaces + text.Length;
                virtualSpaces = 0;
                history.AddCaret(start);
                transaction.Complete();
            }
        }
    }

    private sealed class History
    {
        private readonly Func<(int Caret, int Virtual)> read;
        private readonly Action<(int Caret, int Virtual)> move;
        private readonly Stack<List<ITextUndoPrimitive>> undo = new Stack<List<ITextUndoPrimitive>>();
        private readonly Stack<List<ITextUndoPrimitive>> redo = new Stack<List<ITextUndoPrimitive>>();
        private List<ITextUndoPrimitive> pending;
        private ITextUndoTransaction transaction;
        private TextUndoHistoryState state;
        private readonly PropertyCollection properties = new PropertyCollection();
        internal ITextUndoHistory Value { get; }
        internal History(Func<(int, int)> read, Action<(int, int)> move)
        {
            this.read = read;
            this.move = move;
            Value = Proxy<ITextUndoHistory>((method, args) =>
            {
                if (method.Name == "get_Properties") return properties;
                if (method.Name == "get_State") return state;
                if (method.Name == "get_CurrentTransaction") return transaction;
                if (method.Name != "CreateTransaction") return Default(method.ReturnType);
                if (transaction != null) throw new Exception("Unexpected nested test transaction");
                pending = new List<ITextUndoPrimitive>();
                transaction = Proxy<ITextUndoTransaction>((action, values) =>
                {
                    if (action.Name == "AddUndo") { var primitive = (ITextUndoPrimitive)values[0]; primitive.Parent = transaction; pending.Add(primitive); }
                    if (action.Name == "Complete") { undo.Push(pending); redo.Clear(); pending = null; transaction = null; }
                    return Default(action.ReturnType);
                });
                return transaction;
            });
        }
        internal void AddCaret((int Caret, int Virtual) before) => transaction.AddUndo(new CaretChange(before, read(), move));
        internal void Reset() { undo.Clear(); redo.Clear(); }
        internal void Undo()
        {
            var changes = undo.Pop();
            state = TextUndoHistoryState.Undoing;
            foreach (var change in changes.AsEnumerable().Reverse()) change.Undo();
            state = TextUndoHistoryState.Idle;
            redo.Push(changes);
        }
        internal void Redo()
        {
            var changes = redo.Pop();
            state = TextUndoHistoryState.Redoing;
            foreach (var change in changes) change.Do();
            state = TextUndoHistoryState.Idle;
            undo.Push(changes);
        }
    }

    private sealed class CaretChange((int, int) before, (int, int) after, Action<(int, int)> move) : ITextUndoPrimitive
    {
        public bool CanRedo => true;
        public bool CanUndo => true;
        public ITextUndoTransaction Parent { get; set; }
        public void Do() => move(after);
        public void Undo() => move(before);
        public bool CanMerge(ITextUndoPrimitive older) => false;
        public ITextUndoPrimitive Merge(ITextUndoPrimitive older) => throw new NotSupportedException();
    }

    private static object Default(Type type) => type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
    private static T Proxy<T>(Func<MethodInfo, object[], object> call) where T : class => (T)new Stub(typeof(T), call).GetTransparentProxy();
    private sealed class Stub : RealProxy
    {
        private readonly Type type;
        private readonly Func<MethodInfo, object[], object> call;
        internal Stub(Type type, Func<MethodInfo, object[], object> call) : base(type) { this.type = type; this.call = call; }
        public override IMessage Invoke(IMessage message)
        {
            var method = (IMethodCallMessage)message;
            if (method.MethodName == "GetType") return new ReturnMessage(type, null, 0, method.LogicalCallContext, method);
            try
            {
                var arguments = method.Args;
                var result = call((MethodInfo)method.MethodBase, arguments);
                return new ReturnMessage(result, arguments, arguments.Length, method.LogicalCallContext, method);
            }
            catch (Exception exception) { return new ReturnMessage(exception, method); }
        }
    }
}
