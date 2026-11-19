using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Nazor.VisualStudio
{
    [Export]
    [Export(typeof(ICodeLensCallbackListener))]
    [ContentType(NazorContentTypes.Component)]
    [ContentType(NazorContentTypes.StyleSheet)]
    internal sealed class NssReferenceCallbacks : ICodeLensCallbackListener
    {
        internal readonly ConcurrentDictionary<string, NssCodeLensData> Results = new ConcurrentDictionary<string, NssCodeLensData>();
        public Task<NssCodeLensData> NazorNssReferencesAsync(string key) => Task.FromResult(Results.TryGetValue(key, out var data) ? data : null);
        public Task<long> NazorNssReferencesVersionAsync(string key) => Task.FromResult(Results.TryGetValue(key, out var data) ? data.Version : 0);
    }

    [Export(typeof(IViewTaggerProvider))]
    [ContentType(NazorContentTypes.Component)]
    [ContentType(NazorContentTypes.StyleSheet)]
    [TagType(typeof(ICodeLensTag))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class NssReferenceTaggerProvider : IViewTaggerProvider
    {
        [Import] private NazorLanguageClient Client { get; set; }
        [Import] private ITextDocumentFactoryService Documents { get; set; }
        [Import] private NssReferenceCallbacks Callbacks { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView view, ITextBuffer buffer) where T : ITag =>
            view.TextBuffer == buffer ? new Tagger(view, buffer, Client, Documents, Callbacks) as ITagger<T> : null;

        private sealed class Tagger : ITagger<ICodeLensTag>
        {
            private readonly ITextView view;
            private readonly ITextBuffer buffer;
            private readonly NazorLanguageClient client;
            private readonly ITextDocumentFactoryService documents;
            private readonly NssReferenceCallbacks callbacks;
            private readonly CancellationTokenSource closed = new CancellationTokenSource();
            private CancellationTokenSource pending;
            private readonly JoinableTaskCollection jobs;
            private readonly JoinableTaskFactory tasks;
            private TagSpan<ICodeLensTag>[] tags = Array.Empty<TagSpan<ICodeLensTag>>();
            private ITextSnapshot seeded;

            public Tagger(ITextView view, ITextBuffer buffer, NazorLanguageClient client, ITextDocumentFactoryService documents, NssReferenceCallbacks callbacks)
            {
                this.view = view; this.buffer = buffer; this.client = client; this.documents = documents; this.callbacks = callbacks;
                jobs = ThreadHelper.JoinableTaskContext.CreateCollection();
                tasks = ThreadHelper.JoinableTaskContext.CreateFactory(jobs);
                buffer.Changed += Changed;
                client.Initialized += Refresh;
                client.ParametersChanged += Refresh;
                view.Closed += Close;
                Seed();
                Refresh();
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
            public IEnumerable<ITagSpan<ICodeLensTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                Seed();
                if (spans.Count == 0) return Array.Empty<ITagSpan<ICodeLensTag>>();
                var snapshot = spans[0].Snapshot;
                return tags.Select(t => new TagSpan<ICodeLensTag>(t.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive), t.Tag))
                    .Where(t => t.Span.Length > 0 && spans.Any(s => s.IntersectsWith(t.Span)));
            }

            private void Changed(object sender, TextContentChangedEventArgs e) { Seed(); Refresh(); }
            private void Seed()
            {
                if (documents == null || buffer == null || seeded == buffer.CurrentSnapshot || !documents.TryGetTextDocument(buffer, out var document)) return;
                Seed(buffer.CurrentSnapshot, document.FilePath);
                seeded = buffer.CurrentSnapshot;
            }

            private void Seed(ITextSnapshot snapshot, string path)
            {
                var text = snapshot.GetText();
                var component = path.EndsWith(".nazor", StringComparison.OrdinalIgnoreCase);
                var fresh = tags.ToList();
                var blocked = new Stack<bool>();
                var start = 0;
                for (var i = 0; i < text.Length; i++)
                {
                    if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '*')
                    {
                        var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                        if (end < 0) break;
                        if (string.IsNullOrWhiteSpace(text.Substring(start, i - start))) start = end + 2;
                        i = end + 1;
                    }
                    else if (text[i] == '"' || text[i] == '\'')
                    {
                        var quote = text[i];
                        for (i++; i < text.Length && text[i] != quote; i++) if (text[i] == '\\') i++;
                    }
                    else if (text[i] == '{')
                    {
                        var inStyle = !component || NazorRegions.At(text, i) == NazorRegion.Style;
                        var header = text.Substring(start, i - start).Trim();
                        var skip = blocked.Count > 0 && blocked.Peek();
                        if (inStyle && header.StartsWith("@", StringComparison.Ordinal) &&
                            !System.Text.RegularExpressions.Regex.IsMatch(header, @"^@(media|supports|layer)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) skip = true;
                        blocked.Push(skip);
                        if (inStyle && !skip && header.Length > 0 && !header.StartsWith("@", StringComparison.Ordinal))
                        {
                            var at = start;
                            while (at < i && char.IsWhiteSpace(text[at])) at++;
                            if (!fresh.Any(t => t.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive).Start.Position == at &&
                                t.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive).Length > 0))
                            {
                                var span = new SnapshotSpan(snapshot, at, header.Length);
                                var data = new NssCodeLensData { Selector = string.Join(" ", header.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)),
                                    Global = !component && !path.EndsWith(".nazor.nss", StringComparison.OrdinalIgnoreCase), References = Array.Empty<NativeCodeLensReference>() };
                                fresh.Add(new TagSpan<ICodeLensTag>(span, new Tag(path, span, data, callbacks)));
                            }
                        }
                        start = i + 1;
                    }
                    else if (text[i] == '}' || text[i] == ';')
                    {
                        if (text[i] == '}' && blocked.Count > 0) blocked.Pop();
                        start = i + 1;
                    }
                }
                if (fresh.Count == tags.Length) return;
                tags = fresh.OrderBy(t => t.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive).Start).ToArray();
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
            }
            private void Refresh()
            {
                if (closed.IsCancellationRequested) return;
                tasks.RunAsync(async () =>
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(closed.Token);
                        pending?.Cancel();
                        using (var request = CancellationTokenSource.CreateLinkedTokenSource(closed.Token))
                        {
                            pending = request;
                            try
                            {
                                await Task.Delay(200, request.Token);
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(request.Token);
                                if (!client.IsReady || client.Rpc == null || !documents.TryGetTextDocument(buffer, out var document)) return;
                                var snapshot = buffer.CurrentSnapshot;
                                request.CancelAfter(TimeSpan.FromSeconds(15));
                                var rules = await client.Rpc.InvokeWithParameterObjectAsync<Rule[]>("nazor/nssReferences", new
                                { textDocument = new { uri = new Uri(document.FilePath).AbsoluteUri }, text = snapshot.GetText() }, request.Token);
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(request.Token);
                                if (snapshot != buffer.CurrentSnapshot || rules == null) return;
                                Apply(snapshot, document.FilePath, rules);
                            }
                            finally { if (pending == request) pending = null; }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception exception) when (!(exception is OutOfMemoryException))
                    { System.Diagnostics.Trace.WriteLine("Nazor NSS references: " + exception.Message); }
                }).FileAndForget("Nazor/NssReferences");
            }

            private void Apply(ITextSnapshot snapshot, string path, Rule[] rules)
            {
                var remaining = tags.ToList();
                var fresh = new List<TagSpan<ICodeLensTag>>();
                var changed = false;
                foreach (var rule in rules.Where(r => r.start >= 0 && r.length > 0 && r.start <= snapshot.Length - r.length))
                {
                    var span = new SnapshotSpan(snapshot, rule.start, rule.length);
                    var previous = remaining.Where(t => t.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive).Start == span.Start)
                        .OrderByDescending(t => t.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive).Length).FirstOrDefault();
                    Tag tag;
                    if (previous != null)
                    {
                        remaining.Remove(previous);
                        tag = (Tag)previous.Tag;
                        changed |= previous.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive) != span;
                        tag.Update(span, Data(rule));
                    }
                    else
                    {
                        tag = new Tag(path, span, Data(rule), callbacks);
                        changed = true;
                    }
                    fresh.Add(new TagSpan<ICodeLensTag>(span, tag));
                }
                foreach (var previous in remaining)
                {
                    var span = previous.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive);
                    var header = snapshot.GetText(span.Start, snapshot.Length - span.Start);
                    var brace = header.IndexOfAny(new[] { '{', '}', ';' });
                    if (span.Length > 0 && brace > 0 && header[brace] == '{' && !header.TrimStart().StartsWith("@", StringComparison.Ordinal) &&
                        header.Substring(0, brace).Trim() != previous.Span.GetText().Trim() &&
                        !fresh.Any(t => t.Span.Start >= span.Start && t.Span.Start < span.Start + brace))
                        fresh.Add(new TagSpan<ICodeLensTag>(span, previous.Tag));
                    else { ((Tag)previous.Tag).Disconnect(); changed = true; }
                }
                tags = fresh.OrderBy(t => t.Span.Start).ToArray();
                if (changed) TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
            }

            private static NssCodeLensData Data(Rule rule) => new NssCodeLensData
            {
                Selector = rule.selector, Partial = rule.partial, Global = rule.global,
                References = (rule.references ?? Array.Empty<LspLocation>()).Select(location => new NativeCodeLensReference
                {
                    FilePath = new Uri(location.uri).LocalPath, LineNumber = location.range.start.line, ColumnNumber = location.range.start.character,
                    ReferenceLineText = location.preview, ReferenceStart = location.range.start.character,
                    ReferenceLength = location.range.end.character - location.range.start.character,
                    LongDescription = rule.selector, Language = "Nazor",
                }).ToArray(),
            };

            private void Disconnect() { foreach (var tag in tags) ((Tag)tag.Tag).Disconnect(); tags = Array.Empty<TagSpan<ICodeLensTag>>(); }
            private void Close(object sender, EventArgs e)
            {
                buffer.Changed -= Changed; client.Initialized -= Refresh; client.ParametersChanged -= Refresh; view.Closed -= Close;
                closed.Cancel(); Disconnect();
                ThreadHelper.JoinableTaskFactory.Run(async () => await jobs.JoinTillEmptyAsync());
                closed.Dispose();
            }
        }

        private sealed class Tag : ICodeLensTag3, ICodeLensDescriptor, ICodeLensDescriptorContextProvider
        {
            private readonly NssReferenceCallbacks callbacks;
            private ITrackingSpan span;
            private string content;
            private long version;
            public Tag(string path, SnapshotSpan span, NssCodeLensData data, NssReferenceCallbacks callbacks)
            {
                FilePath = path; this.callbacks = callbacks;
                ElementDescription = "nazor-nss:" + Guid.NewGuid().ToString("N");
                Update(span, data);
            }
            public void Update(SnapshotSpan current, NssCodeLensData data)
            {
                span = current.Snapshot.CreateTrackingSpan(current.Span, SpanTrackingMode.EdgeExclusive);
                data.Version = 0;
                var next = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                if (next == content) return;
                content = next;
                data.Version = ++version;
                callbacks.Results[ElementDescription] = data;
            }
            public ICodeLensDescriptor Descriptor => this;
            public ICodeLensDescriptorContextProvider DescriptorContextProvider => this;
            public CodeLensTagProperties Properties { get; } = new CodeLensTagProperties(true);
            public string FilePath { get; }
            public Span? ApplicableSpan => span.GetSpan(span.TextBuffer.CurrentSnapshot).Span;
            public string ElementDescription { get; }
            public Guid ProjectGuid => Guid.Empty;
            public CodeElementKinds Kind => CodeElementKinds.Class;
            public event EventHandler Disconnected;
            public Task<CodeLensDescriptorContext> GetCurrentContextAsync() => Task.FromResult(new CodeLensDescriptorContext(ApplicableSpan, new Dictionary<object, object>()));
            public void Disconnect() { callbacks.Results.TryRemove(ElementDescription, out _); Disconnected?.Invoke(this, EventArgs.Empty); }
        }

#pragma warning disable IDE1006
        private sealed class Rule
        {
            public int start { get; set; }
            public int length { get; set; }
            public string selector { get; set; }
            public LspLocation[] references { get; set; }
            public bool partial { get; set; }
            public bool global { get; set; }
        }
#pragma warning restore IDE1006
    }
}
