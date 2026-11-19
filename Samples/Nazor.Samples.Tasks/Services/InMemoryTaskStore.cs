using Nazor.Samples.Tasks.Models;

namespace Nazor.Samples.Tasks.Services;

/// <summary>Session data survives view changes because Program registers this service as a singleton.</summary>
public sealed class InMemoryTaskStore : ITaskStore
{
    private readonly List<TaskItem> items =
    [
        new(Guid.NewGuid(), "Prepare the project meeting"),
        new(Guid.NewGuid(), "Review this week's notes"),
        new(Guid.NewGuid(), "Organize the workspace", true),
    ];

    public IReadOnlyList<TaskItem> Items => items.AsReadOnly();

    public TaskItem Add(string title)
    {
        // Validate here as well as in the UI because the service may also be called by another view or a test.
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var item = new TaskItem(Guid.NewGuid(), title.Trim());
        items.Add(item);
        return item;
    }

    public void SetCompleted(Guid id, bool completed)
    {
        var index = items.FindIndex(item => item.Id == id);
        // Replace the immutable value while retaining Id; keyed rows keep their UI identity across this replacement.
        if (index >= 0) items[index] = items[index] with { IsCompleted = completed };
    }

    public void Remove(Guid id) => items.RemoveAll(item => item.Id == id);
}
