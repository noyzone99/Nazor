using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Nazor.VisualStudio.CodeLens.Tests;

internal static class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--roslyn-startup")
        {
            RoslynStartupChecks.Check(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]));
            return;
        }
        if (args.Length == 2 && args[0] == "--icon-startup")
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { NazorIconChecks.CheckStartup(args[1]); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) throw failure;
            return;
        }
        await RunAsync();
        if (args.Length >= 2)
        {
            var extension = Assembly.LoadFrom(args[1]);
            await NssCodeLensChecks.CheckAsync(extension);
            await CheckCustomMessageTargetAsync(extension);
            CheckParameterFormats(extension);
            CheckParameterTracking(extension, args[2]);
            await CheckSidecarCommandAsync(extension.GetType("Nazor.VisualStudio.NazorCompletion", true));
            CheckColorImages(args[0], extension);
        }
    }

    internal static string FindChannelsAssembly(string ide)
    {
        // Prefer the IDE's in-process assembly when an out-of-process component has an exact version match.
        XNamespace binding = "urn:schemas-microsoft-com:asm.v1";
        var channels = XDocument.Load(Path.Combine(ide, "devenv.exe.config")).Descendants(binding + "dependentAssembly")
            .FirstOrDefault(dependency => (string)dependency.Element(binding + "assemblyIdentity")?.Attribute("name") == "System.Threading.Channels");
        var href = (string)channels?.Element(binding + "codeBase")?.Attribute("href");
        return href == null ? null : Path.Combine(ide, href);
    }

    static void CheckParameterTracking(Assembly extension, string editorAssembly)
    {
        var ide = new DirectoryInfo(Path.GetDirectoryName(editorAssembly)).Parent.Parent.Parent.FullName;
        var channelsPath = FindChannelsAssembly(ide);
        ResolveEventHandler resolve = (_, args) =>
        {
            var name = new AssemblyName(args.Name);
            if (name.Name.EndsWith(".resources", StringComparison.Ordinal)) return null;
            var shared = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name.Name);
            if (shared != null) return shared;
            if (name.Name == "System.Threading.Channels" && channelsPath != null)
                return Assembly.LoadFrom(channelsPath);
            return Directory.EnumerateFiles(ide, name.Name + ".dll", SearchOption.AllDirectories)
                .Where(path => path.IndexOf("\\SemanticSearchRefs\\", StringComparison.OrdinalIgnoreCase) < 0 && path.IndexOf("\\LanguageServices\\Core\\", StringComparison.OrdinalIgnoreCase) < 0)
                .Where(path => AssemblyName.GetAssemblyName(path).FullName == name.FullName)
                .Select(Assembly.LoadFrom).FirstOrDefault();
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolve;
        try { CheckParameterTrackingCore(extension, editorAssembly); }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= resolve; }
    }

    static void CheckParameterTrackingCore(Assembly extension, string editorAssembly)
    {
        var editor = Assembly.LoadFrom(editorAssembly);
        object Create(string name) => Activator.CreateInstance(editor.GetType(name, true), true);
        const string text = "<Clock InitialDate=\"@DateTime.Now\" param1=\"\" />";
        var content = editor.GetType("Microsoft.VisualStudio.Text.Implementation.StringRebuilder", true)
            .GetMethod("Create", new[] { typeof(string) }).Invoke(null, new object[] { text });
        var buffer = (ITextBuffer)editor.GetType("Microsoft.VisualStudio.Text.Implementation.TextBuffer", true).GetConstructors().Single(c => c.GetParameters().Length == 5).Invoke(
            new object[] { new TestContentType(), content, null, Create("Microsoft.VisualStudio.Text.Utilities.GuardedOperations"), Create("Microsoft.VisualStudio.Text.Implementation.BufferFactoryService") });
        var taggerType = extension.GetType("Nazor.VisualStudio.NazorParameterTaggerProvider+Tagger", true);
        var tagger = (ITagger<ClassificationTag>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(taggerType);
        var known = new ClassificationTag(new TestClassification("known"));
        var unknown = new ClassificationTag(new TestClassification("unknown"));
        taggerType.GetField("tags", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tagger, new ITagSpan<ClassificationTag>[] {
            new TagSpan<ClassificationTag>(new SnapshotSpan(buffer.CurrentSnapshot, text.IndexOf("InitialDate"), "InitialDate".Length), known),
            new TagSpan<ClassificationTag>(new SnapshotSpan(buffer.CurrentSnapshot, text.IndexOf("param1"), "param1".Length), unknown),
        });
        void Check(params string[] expected)
        {
            var snapshot = buffer.CurrentSnapshot;
            var actual = tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(snapshot, 0, snapshot.Length))).ToArray();
            if (!actual.Select(t => t.Span.GetText()).SequenceEqual(expected) || actual.Any(t => t.Span.Snapshot != snapshot))
                throw new Exception("Parameter colors must survive buffer edits while waiting for the server");
            if (actual.Length > 0 && !ReferenceEquals(actual[0].Tag, known)) throw new Exception("Known parameter format changed");
            if (actual.Length > 1 && !ReferenceEquals(actual[1].Tag, unknown)) throw new Exception("Unknown parameter format changed");
        }
        Check("InitialDate", "param1");
        buffer.Insert(0, "\n");
        Check("InitialDate", "param1");
        foreach (var letter in "abc")
        {
            buffer.Insert(buffer.CurrentSnapshot.GetText().IndexOf("@DateTime"), letter.ToString());
            Check("InitialDate", "param1");
        }
        buffer.Insert(buffer.CurrentSnapshot.GetText().IndexOf("InitialDate"), " ");
        Check("InitialDate", "param1");
        buffer.Insert(buffer.CurrentSnapshot.GetText().IndexOf("InitialDate") + "InitialDate".Length, " ");
        Check("InitialDate", "param1");
        buffer.Delete(new Span(buffer.CurrentSnapshot.GetText().IndexOf("param1"), "param1".Length));
        Check("InitialDate");
        buffer.Delete(new Span(0, buffer.CurrentSnapshot.Length));
        Check();
        if (tagger.GetTags(new NormalizedSnapshotSpanCollection()).Any()) throw new Exception("Empty requests must have no tags");
        Console.WriteLine("PASS: parameter colors track real editor snapshots across typing, shifts, boundary edits and deletion without an RPC refresh.");
        NssCodeLensChecks.CheckTracking(extension, buffer);
        QuickAddChecks.Check(extension, new DirectoryInfo(Path.GetDirectoryName(editorAssembly)).Parent.Parent.Parent.FullName);
        CSharpOptionsChecks.Check(extension, new DirectoryInfo(Path.GetDirectoryName(editorAssembly)).Parent.Parent.Parent.FullName);
        var languageServices = Path.Combine(new DirectoryInfo(Path.GetDirectoryName(editorAssembly)).Parent.Parent.Parent.FullName, "CommonExtensions", "Microsoft", "VBCSharp", "LanguageServices", "Microsoft.VisualStudio.LanguageServices.dll");
        Assembly.LoadFrom(languageServices);
        EditingSequenceChecks.Check(extension, buffer, editor);
    }

    private sealed class TestContentType : IContentType
    {
        public string TypeName => "Nazor";
        public string DisplayName => TypeName;
        public IEnumerable<IContentType> BaseTypes => Array.Empty<IContentType>();
        public bool IsOfType(string type) => type == TypeName;
    }

    private sealed class TestClassification(string name) : IClassificationType
    {
        public string Classification => name;
        public IEnumerable<IClassificationType> BaseTypes => Array.Empty<IClassificationType>();
        public bool IsOfType(string type) => type == Classification;
    }

    static async Task CheckCustomMessageTargetAsync(Assembly extension)
    {
        var client = Activator.CreateInstance(extension.GetType("Nazor.VisualStudio.NazorLanguageClient", true), true);
        var target = client.GetType().GetProperty("CustomMessageTarget").GetValue(client);
        var changed = new TaskCompletionSource<bool>();
        client.GetType().GetEvent("ParametersChanged").AddEventHandler(client, (Action)(() => changed.TrySetResult(true)));
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"method\":\"nazor/componentParametersChanged\",\"params\":{}}");
        var header = System.Text.Encoding.ASCII.GetBytes("Content-Length: " + payload.Length + "\r\n\r\n");
        using var input = new MemoryStream(header.Concat(payload).ToArray());
        using var rpc = new JsonRpc(Stream.Null, input);
        rpc.AddLocalRpcTarget(target);
        rpc.StartListening();
        if (await Task.WhenAny(changed.Task, Task.Delay(5000)) != changed.Task)
            throw new Exception("Parameter classification notification did not reach the language client");
        var failure = await (Task<InitializationFailureContext>)client.GetType().GetMethod("OnServerInitializeFailedAsync")
            .Invoke(client, new object[] { new FailedInitialization() });
        if (!failure.FailureMessage.Contains("Configuring targets for custom messages") ||
            !failure.FailureMessage.Contains("Unsupported event handler") || failure.FailureMessage.Contains("install the .NET"))
            throw new Exception("Initialization errors must report the actual exception without guessing a missing runtime");
        Console.WriteLine("PASS: the packaged custom message target registers and receives parameter notifications.");
        Console.WriteLine("PASS: initialization failure reports the stage and root cause without a misleading runtime diagnosis.");
    }

    private sealed class FailedInitialization : ILanguageClientInitializationInfo
    {
        public InitializationStatus Status => default;
        public Exception InitializationException => new InvalidOperationException("Initialization failed", new NotSupportedException("Unsupported event handler"));
        public bool IsInitialized => false;
        public string StatusMessage => "Configuring targets for custom messages";
    }

    static void CheckParameterFormats(Assembly extension)
    {
        var known = Activator.CreateInstance(extension.GetType("Nazor.VisualStudio.NazorParameterFormat", true), true);
        var unknown = Activator.CreateInstance(extension.GetType("Nazor.VisualStudio.NazorUnknownParameterFormat", true), true);
        var format = typeof(Microsoft.VisualStudio.Text.Classification.EditorFormatDefinition);
        var color = format.GetProperty("ForegroundColor");
        if (color.GetValue(known) == null || color.GetValue(unknown) == null || Equals(color.GetValue(known), color.GetValue(unknown)))
            throw new Exception("Known and unknown component parameters need distinct explicit colors");
        var classification = typeof(Microsoft.VisualStudio.Text.Classification.ClassificationFormatDefinition);
        if (!(bool)classification.GetProperty("IsItalic").GetValue(unknown)) throw new Exception("Unknown component parameters need their own format");
        Console.WriteLine("PASS: packaged parameter formats use distinct explicit colors; unknown parameters are italic.");
    }

    static void CheckColorImages(string directory, Assembly extension)
    {
        Exception failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var manifest = XDocument.Load(Path.Combine(directory, "NazorCompletionColors.imagemanifest"));
                XNamespace schema = "http://schemas.microsoft.com/VisualStudio/ImageManifestSchema/2014";
                var images = manifest.Descendants(schema + "Image").ToArray();
                if (images.Length < 100) throw new Exception("Named color catalog incomplete");
#pragma warning disable VSSDK005
                using var context = new JoinableTaskContext();
#pragma warning restore VSSDK005
                NazorIconChecks.Check(context.Factory, directory, extension);
                var trace = new Microsoft.VisualStudio.Utilities.Tracer("Nazor.CompletionImages");
                trace.Level = System.Diagnostics.SourceLevels.Error;
                trace.Source.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Error));
                using var library = ImageLibrary.Load(context.Factory, Path.Combine(directory, "NazorCompletionColors.imagemanifest"), tracer: trace);
                if (library.GetImageCount() != images.Length) throw new Exception("Visual Studio ImageLibrary did not register the color catalog");
                using var uncatalogued = new ImageLibrary(context.Factory);
                var resolverType = extension.GetType("Nazor.VisualStudio.NazorCompletionImages", true);
                var resolver = Activator.CreateInstance(resolverType, true);
                var resolve = resolverType.GetMethod("Resolve", BindingFlags.Instance | BindingFlags.NonPublic);
                var map = (JObject)resolverType.GetMethod("CreateMap", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(resolver, new object[] { uncatalogued, Path.Combine(directory, "NazorCompletionColors.imagemanifest") });
                if (map.Count != images.Length) throw new Exception("Startup color map incomplete");
                var middlewareType = extension.GetType("Nazor.VisualStudio.NazorCompletion", true);
                var middleware = middlewareType.GetConstructors().Single().Invoke(new object[] { null, null });
                var canHandle = middlewareType.GetMethod("CanHandle");
                if ((bool)canHandle.Invoke(middleware, new object[] { "textDocument/completion" }))
                    throw new Exception("Completion middleware would drop VS icon converters");
                if (!(bool)canHandle.Invoke(middleware, new object[] { "textDocument/codeAction" }))
                    throw new Exception("C# buffer synchronization middleware lost");
                CheckSidecarDeletion(extension);
                foreach (var entry in images)
                {
                    if ((string)entry.Attribute("AllowColorInversion") != "false") throw new Exception("Color swatches may be theme-inverted");
                    var id = (int)entry.Attribute("ID");
                    foreach (var background in new uint[] { 0xffffffff, 0xff202020 })
                    foreach (var dpi in new[] { 96, 192 })
                    {
                        var attributes = new Microsoft.VisualStudio.Imaging.Interop.ImageAttributes
                        {
                            StructSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Microsoft.VisualStudio.Imaging.Interop.ImageAttributes)),
                            ImageType = (uint)_UIImageType.IT_Bitmap,
                            Format = (uint)_UIDataFormat.DF_WPF,
                            LogicalWidth = 16, LogicalHeight = 16, Dpi = dpi, Background = background,
                            Flags = unchecked((uint)(_ImageAttributesFlags.IAF_RequiredFlags | _ImageAttributesFlags.IAF_Background)),
                        };
                        var moniker = new ImageMoniker { Guid = Guid.Parse((string)entry.Attribute("Guid")), Id = id };
                        var registered = (ImageMoniker)resolve.Invoke(resolver, new object[] { uncatalogued, id });
                        if ((string)map[id.ToString()]["guid"] != registered.Guid.ToString() || (int)map[id.ToString()]["id"] != registered.Id)
                            throw new Exception("Startup map did not preserve the host moniker");
                        foreach (var pair in new[] { (Library: library, Moniker: moniker), (Library: uncatalogued, Moniker: registered) })
                        {
                            var bitmap = pair.Library.GetImage(pair.Moniker, attributes) as BitmapSource;
                            if (bitmap == null) throw new Exception("ImageLibrary cannot render moniker " + id);
                            var pixels = new byte[4];
                            new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0).CopyPixels(
                                new System.Windows.Int32Rect(bitmap.PixelWidth / 2, bitmap.PixelHeight / 2, 1, 1), pixels, 4, 0);
                            var expected = id == 0x1000000 ? Colors.LightGray : Color.FromRgb((byte)(id >> 16), (byte)(id >> 8), (byte)id);
                            if (pixels[3] != 255 || pixels[2] != expected.R || pixels[1] != expected.G || pixels[0] != expected.B)
                                throw new Exception("ImageLibrary changed swatch " + id + " at DPI " + dpi);
                        }
                    }
                }
                Console.WriteLine("PASS: ImageLibrary renders all " + images.Length + " colors with and without a registered Nazor manifest, on light/dark backgrounds at 96/192 DPI.");
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null) throw failure;
    }

    static void CheckSidecarDeletion(Assembly extension)
    {
        var component = Path.GetFullPath("Counter.nazor");
        var files = new Dictionary<string, string> { [component] = "@code { int count; }", [component + ".cs"] = "", [component + ".nss"] = " \n" };
        var arguments = JObject.FromObject(new { component, changes = new object[] {
            new { path = component, text = files[component], delete = false }, new { path = component + ".cs", text = "", delete = true }, new { path = component + ".nss", text = "", delete = true },
        } });
        var deletion = extension.GetType("Nazor.VisualStudio.NazorSidecars", true).GetMethod("Deletable", BindingFlags.Static | BindingFlags.NonPublic);
        string[] Check() => (string[])deletion.Invoke(null, new object[] { arguments, (Func<string, string>)(p => files.TryGetValue(p, out var text) ? text : null) });
        if (Check().Length != 2) throw new Exception("Empty sidecars were not scheduled for deletion");
        void Refuses()
        {
            try { Check(); }
            catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { return; }
            throw new Exception("Unsafe sidecar deletion was accepted");
        }
        files[component] = "<Button />";
        Refuses();
        files[component] = "@code { int count; }";
        files[component + ".cs"] = "int unsaved;";
        Refuses();
        files[component + ".cs"] = "";
        arguments["changes"][1]["path"] = Path.GetFullPath("Unrelated.cs");
        Refuses();
        Console.WriteLine("PASS: deletion requires applied destination edits and empty sidecar buffers, and rejects unrelated paths.");
    }

    static async Task CheckSidecarCommandAsync(Type middlewareType)
    {
        JToken received = null;
        var middleware = Activator.CreateInstance(middlewareType, new object[] { null, (Func<JToken, Task>)(a => { received = a; return Task.CompletedTask; }) });
        var request = middlewareType.GetMethod("HandleRequestAsync");
        var args = new JObject { ["component"] = "Counter.nazor" };
        var parameters = new JObject { ["command"] = "nazor.deleteEmptySidecars", ["arguments"] = new JArray(args) };
        var forwarded = false;
        Func<JToken, Task<JToken>> forward = p => { forwarded = true; return Task.FromResult<JToken>(new JValue("forwarded")); };
        await (Task<JToken>)request.Invoke(middleware, new object[] { "workspace/executeCommand", parameters, forward });
        if (received != args || forwarded) throw new Exception("Sidecar command did not run in the VS adapter");
        parameters["command"] = "another.command";
        await (Task<JToken>)request.Invoke(middleware, new object[] { "workspace/executeCommand", parameters, forward });
        if (!forwarded) throw new Exception("Other server commands were intercepted");
        Console.WriteLine("PASS: the VS middleware executes sidecar cleanup locally and forwards other commands.");
    }
    static async Task RunAsync()
    {
        var assembly = Assembly.Load("Nazor.VisualStudio.CodeLens");
        var type = assembly.GetType("Nazor.VisualStudio.CodeLens.NazorCodeLensProvider", true);
        var provider = (IAsyncCodeLensDataPointProvider)Activator.CreateInstance(type, true);
        var callback = new Callback();
        type.GetProperty("Callback", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(provider, callback);
        var descriptor = new CodeLensDescriptor { FilePath = "Page.cs", Kind = CodeElementKinds.Method, ProjectGuid = Guid.NewGuid() };
        var context = new CodeLensDescriptorContext(new Span(0, 1), new Dictionary<object, object>());
        if (!await provider.CanCreateDataPointAsync(descriptor, context, CancellationToken.None)) throw new Exception("Provider not created");
        var point = await provider.CreateDataPointAsync(descriptor, context, CancellationToken.None);
        try
        {
            var count = await point.GetDataAsync(context, CancellationToken.None);
            if (count.Description != "2 references" || callback.Uses != 1) throw new Exception("Native count/selection not forwarded");
            var details = await point.GetDetailsAsync(context, CancellationToken.None);
            var files = details.Entries.Select(e => e.Fields.First().Text).ToArray();
            if (!files.SequenceEqual(new[] { "ComponentBase.cs", "Page.nazor" })) throw new Exception("Source mapping not forwarded");
            if (details.Entries.First().Fields.ElementAt(7).ImageId.Value.Id == 0) throw new Exception("Native glyph lost");
            var again = await point.GetDataAsync(context, CancellationToken.None);
            if (again.IntValue != 2 || !callback.Cached) throw new Exception("Calculated native count not forwarded");
            Console.WriteLine("PASS: SDK provider invokes native callbacks, preserves base reference/glyph, maps Nazor, and forwards cached count.");
        }
        finally { ((IDisposable)point).Dispose(); }
    }

    sealed class Callback : ICodeLensCallbackService
    {
        public int Uses;
        public bool Cached;
        public JsonRpc GetCallbackJsonRpc(IAsyncCodeLensDataPointProvider provider) => null;
        public JsonRpc GetCallbackJsonRpc(IAsyncCodeLensDataPoint point) => null;
        public Task InvokeAsync(IAsyncCodeLensDataPointProvider provider, string method, IReadOnlyList<object> args, CancellationToken token) => Task.CompletedTask;
        public Task InvokeAsync(IAsyncCodeLensDataPoint point, string method, IReadOnlyList<object> args, CancellationToken token) => Task.CompletedTask;
        public Task<T> InvokeAsync<T>(IAsyncCodeLensDataPoint point, string method, IReadOnlyList<object> args, CancellationToken token) => InvokeAsync<T>((IAsyncCodeLensDataPointProvider)null, method, args, token);
        public Task<T> InvokeAsync<T>(IAsyncCodeLensDataPointProvider provider, string method, IReadOnlyList<object> args, CancellationToken token)
        {
            object value;
            if (method == "NazorCodeLensCanCreateAsync") value = true;
            else if (method == "NazorCodeLensUseNativeAsync") { Uses++; value = null; }
            else if (method == "GetReferenceCountAsync") { Cached = args[2] != null; value = new { Count = 2, IsCapped = false, Version = "v1" }; }
            else if (method == "GetProjectVersionsAsync") value = new Dictionary<Guid, string> { [((Guid[])args[0])[0]] = "v1" };
            else if (method == "FindReferenceLocationsAsync") value = new { Item1 = "v1", Item2 = new[] {
                new { FilePath = "ComponentBase.cs", LineNumber = 156, ColumnNumber = 8, SpanLength = 13, ReferenceLineText = "        OnAfterRender(firstRender);", ReferenceStart = 8, ReferenceLength = 13, Glyph = 50 },
                new { FilePath = "Page.nazor.g.cs", LineNumber = 90, ColumnNumber = 5, SpanLength = 5, ReferenceLineText = "__builder.AddContent(1, Title);", ReferenceStart = 5, ReferenceLength = 5, Glyph = 51 } } };
            else if (method == "NazorCodeLensMapReferencesAsync") { var rows = JArray.FromObject(args[0]); rows[1]["FilePath"] = "Page.nazor"; value = rows; }
            else throw new Exception("Unexpected callback: " + method);
            return Task.FromResult(value == null ? default(T) : JToken.FromObject(value).ToObject<T>());
        }
    }
}
