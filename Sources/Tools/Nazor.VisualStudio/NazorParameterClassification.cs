using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;

namespace Nazor.VisualStudio
{
    internal sealed class NazorParameterUpdates
    {
        internal event Action Changed;
        [JsonRpcMethod("nazor/componentParametersChanged")]
        public void Invalidate() => Changed?.Invoke();
    }

    internal static class NazorParameterClassifications
    {
        public const string Known = "Nazor component parameter";
        public const string Unknown = "Nazor unknown component parameter";
#pragma warning disable CS0649
        [Export(typeof(ClassificationTypeDefinition)), Name(Known), BaseDefinition("property name")]
        internal static ClassificationTypeDefinition KnownDefinition;
        [Export(typeof(ClassificationTypeDefinition)), Name(Unknown), BaseDefinition("excluded code")]
        internal static ClassificationTypeDefinition UnknownDefinition;
#pragma warning restore CS0649
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = NazorParameterClassifications.Known)]
    [Name(NazorParameterClassifications.Known), UserVisible(true), Order(After = Priority.High)]
    internal sealed class NazorParameterFormat : ClassificationFormatDefinition
    {
        public NazorParameterFormat()
        {
            DisplayName = "Nazor - Component parameter";
            ForegroundColor = Color.FromRgb(0xD7, 0xBA, 0x7D);
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = NazorParameterClassifications.Unknown)]
    [Name(NazorParameterClassifications.Unknown), UserVisible(true), Order(After = Priority.High)]
    internal sealed class NazorUnknownParameterFormat : ClassificationFormatDefinition
    {
        public NazorUnknownParameterFormat()
        {
            DisplayName = "Nazor - Unknown component parameter";
            ForegroundColor = Color.FromRgb(0xF4, 0x87, 0x71);
            IsItalic = true;
        }
    }

    [Export(typeof(IViewTaggerProvider))]
    [ContentType(NazorContentTypes.Component), TagType(typeof(ClassificationTag))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class NazorParameterTaggerProvider : IViewTaggerProvider
    {
        [Import] private NazorLanguageClient Client { get; set; }
        [Import] private ITextDocumentFactoryService Documents { get; set; }
        [Import] private IClassificationTypeRegistryService Classifications { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag =>
            textView.TextBuffer == buffer ? new Tagger(textView, buffer, Client, Documents, Classifications) as ITagger<T> : null;

        private sealed class Tagger : ITagger<ClassificationTag>
        {
            private readonly ITextView view;
            private readonly ITextBuffer buffer;
            private readonly NazorLanguageClient client;
            private readonly ITextDocumentFactoryService documents;
            private readonly IClassificationTypeRegistryService classifications;
            private readonly CancellationTokenSource closed = new CancellationTokenSource();
            private readonly CancellationToken lifetime;
            private CancellationTokenSource pending;
            private readonly JoinableTaskCollection jobs;
            private readonly JoinableTaskFactory tasks;
            private ITagSpan<ClassificationTag>[] tags = Array.Empty<ITagSpan<ClassificationTag>>();

            public Tagger(ITextView view, ITextBuffer buffer, NazorLanguageClient client, ITextDocumentFactoryService documents, IClassificationTypeRegistryService classifications)
            {
                this.view = view; this.buffer = buffer; this.client = client; this.documents = documents; this.classifications = classifications;
                lifetime = closed.Token;
                jobs = ThreadHelper.JoinableTaskContext.CreateCollection();
                tasks = ThreadHelper.JoinableTaskContext.CreateFactory(jobs);
                buffer.Changed += Changed;
                client.Initialized += Refresh;
                client.ParametersChanged += Refresh;
                view.Closed += OnClosed;
                Refresh();
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

            public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                if (spans.Count == 0) return Array.Empty<ITagSpan<ClassificationTag>>();
                var snapshot = spans[0].Snapshot;
                return tags.Select(t => (ITagSpan<ClassificationTag>)new TagSpan<ClassificationTag>(
                    t.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive), t.Tag))
                    .Where(t => t.Span.Length > 0 && spans.Any(s => s.IntersectsWith(t.Span)));
            }

            private void Changed(object sender, TextContentChangedEventArgs e) => Refresh();

            private void Refresh()
            {
                if (lifetime.IsCancellationRequested) return;
                tasks.RunAsync(async () =>
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(lifetime);
                        pending?.Cancel();
                        using (var request = CancellationTokenSource.CreateLinkedTokenSource(lifetime))
                        {
                            pending = request;
                            try
                            {
                                await Task.Delay(100, request.Token);
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(request.Token);
                                if (client.Rpc == null || !documents.TryGetTextDocument(buffer, out var document)) return;
                                var snapshot = buffer.CurrentSnapshot;
                                request.CancelAfter(TimeSpan.FromSeconds(5));
                                var parameters = await client.Rpc.InvokeWithParameterObjectAsync<ParameterSpan[]>("nazor/componentParameters", new
                                {
                                    textDocument = new { uri = new Uri(document.FilePath).AbsoluteUri }, text = snapshot.GetText(),
                                }, request.Token);
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(request.Token);
                                if (buffer.CurrentSnapshot != snapshot) return;
                                tags = (parameters ?? Array.Empty<ParameterSpan>()).Where(p => p.start >= 0 && p.length > 0 && p.start <= snapshot.Length - p.length)
                                    .Select(p => (ITagSpan<ClassificationTag>)new TagSpan<ClassificationTag>(new SnapshotSpan(snapshot, p.start, p.length),
                                        new ClassificationTag(classifications.GetClassificationType(p.known ? NazorParameterClassifications.Known : NazorParameterClassifications.Unknown)))).ToArray();
                                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
                            }
                            finally { if (pending == request) pending = null; }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception exception) when (!(exception is OutOfMemoryException))
                    {
                        System.Diagnostics.Trace.WriteLine("Nazor parameter classification: " + exception.Message);
                    }
                }).FileAndForget("Nazor/ParameterClassification");
            }

            private void OnClosed(object sender, EventArgs e)
            {
                buffer.Changed -= Changed;
                client.Initialized -= Refresh;
                client.ParametersChanged -= Refresh;
                view.Closed -= OnClosed;
                closed.Cancel();
                ThreadHelper.JoinableTaskFactory.Run(async () => await jobs.JoinTillEmptyAsync());
                closed.Dispose();
            }
        }

#pragma warning disable IDE1006
        private sealed class ParameterSpan
        {
            public int start { get; set; }
            public int length { get; set; }
            public bool known { get; set; }
        }
#pragma warning restore IDE1006
    }
}
