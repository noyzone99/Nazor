using Nazor.Controls;
using Nazor.Input;
using Nazor.Samples.Tasks.Models;

namespace Nazor.Samples.Tasks.Views;

/// <summary>Coordinates the task screen while its partial .nazor file describes the UI.</summary>
public partial class TasksView : IDisposable
{
    // The generated markup supplies these references after rendering; shortcut lambdas read them when invoked, not on registration.
    private global::Nazor.Controls.TextField newTaskBox = null!;
    private global::Nazor.Controls.TextField searchBox = null!;
    private string newTitle = "";
    private string search = "";
    private bool onlyOpen;
    // Retain selection by stable identity because the store replaces immutable records when completion changes.
    private Guid? selectedId;
    private string status = "Ready. Tasks remain available until the app closes.";
    private IDisposable? newShortcut;
    private IDisposable? searchShortcut;

    // Derive the filtered view from the current store instead of synchronizing a second collection of task copies.
    private IEnumerable<TaskItem> VisibleTasks => Store.Items.Where(item =>
        (!onlyOpen || !item.IsCompleted) && item.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

    protected override void OnInitialized()
    {
        // The window-scoped registry outlives this view. Keep registration handles so disposal removes callbacks to the old instance.
        newShortcut = Shortcuts.Register("n", KeyModifiers.Control,
            EventCallback.Factory.Create<KeyboardEventArgs>(this, () => newTaskBox.FocusAsync()));
        searchShortcut = Shortcuts.Register("f", KeyModifiers.Control,
            EventCallback.Factory.Create<KeyboardEventArgs>(this, () => searchBox.FocusAsync()));
    }

    private async Task AddTask()
    {
        if (string.IsNullOrWhiteSpace(newTitle)) return;
        // Validate at the UI boundary for feedback; the store validates again so other callers cannot insert an empty title.
        var item = Store.Add(newTitle);
        selectedId = item.Id;
        newTitle = "";
        search = "";
        status = $"Added: {item.Title}";
        // Restore keyboard flow to the next entry. EventCallback completion schedules the component update automatically.
        await newTaskBox.FocusAsync();
    }

    private Task NewTaskKeyDown(KeyboardEventArgs e)
        => e.Key == "Enter" ? AddTask() : Task.CompletedTask;
    private void SelectTask(Guid id) => selectedId = id;

    private void CompleteTask(Guid id, bool completed)
    {
        Store.SetCompleted(id, completed);
        status = completed ? "Task completed." : "Task reopened.";
    }

    private void RemoveTask(Guid id)
    {
        Store.Remove(id);
        if (selectedId == id) selectedId = null;
        status = "Task deleted.";
    }

    // Delete may bubble from a card, but a focused text field owns its editing command.
    // Inspect focus before removing a task so keyboard editing cannot accidentally delete the selection.
    private void WorkspaceKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Delete" && selectedId is { } id &&
            newTaskBox.Element.Element?.States.HasFlag(Nazor.Rendering.ElementStates.Focus) != true &&
            searchBox.Element.Element?.States.HasFlag(Nazor.Rendering.ElementStates.Focus) != true)
            RemoveTask(id);
    }

    // Conditional navigation disposes this view; remove registrations to avoid duplicate shortcuts when it is opened again.
    public void Dispose()
    {
        newShortcut?.Dispose();
        searchShortcut?.Dispose();
    }
}
