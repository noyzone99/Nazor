using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Nazor.VisualStudio
{
    [Export]
    internal sealed class ReferenceLocations
    {
        private readonly NazorLanguageClient client;
        private ConditionalWeakTable<ITableEntryHandle, Location> locations = new ConditionalWeakTable<ITableEntryHandle, Location>();
        private sealed class Location { public Task<LspLocation> Task; public volatile LspLocation Current; }
        [Import(AllowDefault = true)] private VisualStudioWorkspace Workspace { get; set; }
        public event Action Changed;

        [ImportingConstructor]
        public ReferenceLocations(NazorLanguageClient client)
        {
            this.client = client;
            client.Initialized += () =>
            { locations = new ConditionalWeakTable<ITableEntryHandle, Location>(); Changed?.Invoke(); };
        }

        public Task<LspLocation> GetAsync(ITableEntryHandle entry) => GetLocation(entry)?.Task ?? Task.FromResult<LspLocation>(null);

        public LspLocation GetCurrent(ITableEntryHandle entry) => GetLocation(entry)?.Current;

        private Location GetLocation(ITableEntryHandle entry)
        {
            if (client.Rpc == null || !entry.TryGetValue(StandardTableKeyNames.DocumentName, out string path) ||
                !path.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase)) return null;
            return locations.GetValue(entry, _ =>
            {
                entry.TryGetValue(StandardTableKeyNames.Line, out int line);
                entry.TryGetValue(StandardTableKeyNames.Column, out int character);
                var location = new Location();
                location.Task = Task.Run(async () =>
                {
                    var found = await ResolveAsync(path, line, character);
                    location.Current = found;
                    Changed?.Invoke();
                    return found;
                });
                return location;
            });
        }

        private async Task<LspLocation> ResolveAsync(string path, int line, int character)
        {
            try
            {
                var solution = Workspace?.CurrentSolution;
                var document = solution?.GetDocumentIdsWithFilePath(path).Select(id => solution.GetDocument(id)).FirstOrDefault();
                var built = document == null ? await Task.Run(() => File.ReadAllText(path)) : (await document.GetTextAsync()).ToString();
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    return await client.Rpc.InvokeWithParameterObjectAsync<LspLocation>("nazor/referencePosition", new
                    { path, text = built, position = new { line, character } }, timeout.Token);
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException)) { return null; }
        }
    }

    internal abstract class ReferenceColumn : TableColumnDefinitionBase
    {
        [Import] private ReferenceLocations Locations { get; set; }
        [Import] private ITableColumnDefinitionManager Columns { get; set; }
        public abstract string SourceColumn { get; }
        protected abstract string Content(LspLocation location);
        public override string DisplayName => Columns.GetColumnDefinition(SourceColumn).DisplayName;
        public override bool IsSortable => true;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement content)
        {
            var location = Locations.GetCurrent(entry);
            if (location == null)
                return Columns.GetColumnDefinition(SourceColumn).TryCreateColumnContent(entry, singleColumnView, out content);
            content = new TextBlock { Text = Content(location), TextTrimming = TextTrimming.CharacterEllipsis };
            return true;
        }

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            var location = Locations.GetCurrent(entry);
            if (location == null)
                return Columns.GetColumnDefinition(SourceColumn).TryCreateStringContent(entry, truncatedText, singleColumnView, out content);
            content = Content(location);
            return true;
        }

        public override int CompareContent(ITableEntryHandle left, ITableEntryHandle right)
        {
            TryCreateStringContent(left, false, false, out var a);
            TryCreateStringContent(right, false, false, out var b);
            return StringComparer.CurrentCulture.Compare(a, b);
        }
    }

    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    internal sealed class ReferenceFileColumn : ReferenceColumn
    {
        public const string ColumnName = "Nazor.Reference.File";
        public override string Name => ColumnName;
        public override string SourceColumn => StandardTableColumnDefinitions.DocumentName;
        protected override string Content(LspLocation location) => new Uri(location.uri).LocalPath;
    }

    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    internal sealed class ReferenceLineColumn : ReferenceColumn
    {
        public const string ColumnName = "Nazor.Reference.Line";
        public override string Name => ColumnName;
        public override string SourceColumn => StandardTableColumnDefinitions.Line;
        protected override string Content(LspLocation location) => (location.range.start.line + 1).ToString();

        public override int CompareContent(ITableEntryHandle left, ITableEntryHandle right)
        {
            TryCreateStringContent(left, false, false, out var a);
            TryCreateStringContent(right, false, false, out var b);
            return int.TryParse(a, out var x) && int.TryParse(b, out var y) ? x.CompareTo(y) : base.CompareContent(left, right);
        }
    }

    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    internal sealed class ReferenceTextColumn : ReferenceColumn
    {
        public const string ColumnName = "Nazor.Reference.Text";
        public override string Name => ColumnName;
        public override string SourceColumn => StandardTableColumnDefinitions.Text;
        protected override string Content(LspLocation location) => location.preview;
    }

    [Export(typeof(ITableControlEventProcessorProvider))]
    [Name("Nazor references")]
    [Order(Before = Priority.Default)]
    internal sealed class ReferenceNavigationProvider : ITableControlEventProcessorProvider
    {
        [Import] private ReferenceLocations Locations { get; set; }

        public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl table)
        {
            if (table.Manager.Identifier.IndexOf("References", StringComparison.OrdinalIgnoreCase) < 0 ||
                Environment.GetEnvironmentVariable("NAZOR_REFERENCE_MAPPING") == "0") return null;
            if (table is IWpfTableControl2 columns)
                columns.SetColumnStates(table.ColumnStates.Select(state => new ColumnState(
                    state.Name == StandardTableColumnDefinitions.DocumentName ? ReferenceFileColumn.ColumnName :
                    state.Name == StandardTableColumnDefinitions.Line ? ReferenceLineColumn.ColumnName :
                    state.Name == StandardTableColumnDefinitions.Text ? ReferenceTextColumn.ColumnName : state.Name,
                    state.IsVisible, state.Width, state.SortPriority, state.DescendingSort)));
            return new Processor(table, Locations);
        }

        private sealed class Processor : TableControlEventProcessorBase
        {
            private readonly IWpfTableControl table;
            private readonly ReferenceLocations locations;
            private bool bypass;
            private bool listening;

            public Processor(IWpfTableControl table, ReferenceLocations locations)
            {
                this.table = table;
                this.locations = locations;
                Listen();
                table.Control.Loaded += (sender, e) => Listen();
                table.Control.Unloaded += (sender, e) =>
                { locations.Changed -= Refresh; listening = false; };
            }

            private void Listen()
            {
                if (listening) return;
                locations.Changed += Refresh;
                listening = true;
            }

            private void Refresh() => RefreshAsync().FileAndForget("nazor/references");

            private async Task RefreshAsync()
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (table.Control.IsLoaded) table.RefreshUI();
            }

            public override void PreprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
            {
                if (bypass || !entry.TryGetValue(StandardTableKeyNames.DocumentName, out string path) ||
                    !path.EndsWith(".nazor.g.cs", StringComparison.OrdinalIgnoreCase)) return;
                e.Handled = true;
                var preview = e.IsPreview;
                var activate = e.ShouldActivate;
                NavigateAsync(entry, preview, activate).FileAndForget("nazor/references/navigation");
            }

            private async Task NavigateAsync(ITableEntryHandle entry, bool preview, bool activate)
            {
                var target = await locations.GetAsync(entry);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (target?.uri != null && target.range?.start != null && File.Exists(new Uri(target.uri).LocalPath))
                {
                    VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, new Uri(target.uri).LocalPath, Guid.Empty,
                        out _, out _, out IVsWindowFrame frame, out IVsTextView view);
                    if (preview) frame?.SetProperty((int)__VSFPROPID5.VSFPROPID_IsProvisional, true);
                    view?.SetCaretPos(target.range.start.line, target.range.start.character);
                    view?.CenterLines(target.range.start.line, 1);
                    if (!activate) table.Control.Focus();
                }
                else
                {
                    bypass = true;
                    try
                    {
                        if (entry is ITableEntryHandle2 handle) handle.NavigateTo(preview, activate);
                        else entry.NavigateTo(preview);
                    }
                    finally { bypass = false; }
                }
            }
        }
    }
}
