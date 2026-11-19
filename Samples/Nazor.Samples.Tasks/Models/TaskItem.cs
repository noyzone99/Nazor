namespace Nazor.Samples.Tasks.Models;

/// <summary>UI-independent session data; Id stays stable when a record is replaced after an edit.</summary>
public sealed record TaskItem(Guid Id, string Title, bool IsCompleted = false);
