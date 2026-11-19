using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Nazor.VisualStudio
{
    [Export(typeof(ICommandHandler))]
    [ContentType(NazorContentTypes.Component)]
    [ContentType(NazorContentTypes.StyleSheet)]
    [Name("Nazor comments")]
    [Order(Before = "Comment Selection Command Handler")]
    [Order(Before = "Uncomment Selection Command Handler")]
    [Order(Before = "Toggle Line Comment Command Handler")]
    [Order(Before = "Toggle Block Comment Command Handler")]
    internal sealed class CommentCommandHandler :
        ICommandHandler<CommentSelectionCommandArgs>, ICommandHandler<UncommentSelectionCommandArgs>,
        ICommandHandler<ToggleLineCommentCommandArgs>, ICommandHandler<ToggleBlockCommentCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry undoHistories;
        private readonly IEditorOperationsFactoryService operations;

        [ImportingConstructor]
        public CommentCommandHandler(ITextUndoHistoryRegistry undoHistories, IEditorOperationsFactoryService operations)
        {
            this.undoHistories = undoHistories;
            this.operations = operations;
        }

        public string DisplayName => "Nazor comments";

        public CommandState GetCommandState(CommentSelectionCommandArgs args) => CommandState.Available;

        public CommandState GetCommandState(UncommentSelectionCommandArgs args) => CommandState.Available;

        public CommandState GetCommandState(ToggleLineCommentCommandArgs args) => CommandState.Available;

        public CommandState GetCommandState(ToggleBlockCommentCommandArgs args) => CommandState.Available;

        public bool ExecuteCommand(CommentSelectionCommandArgs args, CommandExecutionContext executionContext) =>
            Run(args.TextView, args.SubjectBuffer, CommentCommand.Comment, "Comment Selection");

        public bool ExecuteCommand(UncommentSelectionCommandArgs args, CommandExecutionContext executionContext) =>
            Run(args.TextView, args.SubjectBuffer, CommentCommand.Uncomment, "Uncomment Selection");

        public bool ExecuteCommand(ToggleLineCommentCommandArgs args, CommandExecutionContext executionContext) =>
            Run(args.TextView, args.SubjectBuffer, CommentCommand.ToggleLine, "Toggle Line Comment");

        public bool ExecuteCommand(ToggleBlockCommentCommandArgs args, CommandExecutionContext executionContext) =>
            Run(args.TextView, args.SubjectBuffer, CommentCommand.ToggleBlock, "Toggle Block Comment");

        private bool Run(ITextView view, ITextBuffer buffer, CommentCommand command, string name)
        {
            var snapshot = buffer.CurrentSnapshot;
            int start, end;
            var selected = view.Selection.IsEmpty ? null : view.BufferGraph.MapDownToBuffer(view.Selection.StreamSelectionSpan.SnapshotSpan, SpanTrackingMode.EdgeInclusive, buffer);
            if (selected != null && selected.Count > 0)
            {
                start = selected[0].Start.Position;
                end = selected[selected.Count - 1].End.Position;
            }
            else if (view.Caret.Position.Point.GetPoint(buffer, PositionAffinity.Successor) is SnapshotPoint caret)
            {
                start = end = caret.Position;
            }
            else return false;

            var changes = NazorComments.Edit(snapshot.GetText(), start, end, buffer.ContentType.IsOfType(NazorContentTypes.Component), command);
            if (changes.Count == 0) return true;
            var editorOperations = operations.GetEditorOperations(view);
            using (var transaction = undoHistories.RegisterHistory(buffer).CreateTransaction(name))
            {
                editorOperations.AddBeforeTextBufferChangePrimitive();
                using (var edit = buffer.CreateEdit())
                {
                    foreach (var change in changes) edit.Replace(change.Start, change.Length, change.Text);
                    edit.Apply();
                }
                editorOperations.AddAfterTextBufferChangePrimitive();
                transaction.Complete();
            }
            return true;
        }
    }
}
