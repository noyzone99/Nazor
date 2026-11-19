using Nazor.Samples.Tasks.Models;

namespace Nazor.Samples.Tasks.Services;

/// <summary>Separates task data operations from components; another implementation can provide persistence.</summary>
public interface ITaskStore
{
    // Readers can inspect the current items, but changes go through operations that enforce the store contract.
    IReadOnlyList<TaskItem> Items { get; }
    TaskItem Add(string title);
    void SetCompleted(Guid id, bool completed);
    void Remove(Guid id);
}
