namespace MyNazorApp.Models;

public sealed class SampleTask(string title)
{
    public string Title { get; } = title;
    public bool IsCompleted { get; set; }
}
