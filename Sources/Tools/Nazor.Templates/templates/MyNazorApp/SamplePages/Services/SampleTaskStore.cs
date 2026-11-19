using MyNazorApp.Models;

namespace MyNazorApp.Services;

public sealed class SampleTaskStore
{
    public IReadOnlyList<SampleTask> Items { get; } =
    [
        new("Run the app"),
        new("Try the counter"),
        new("Open a page source file")
    ];
}
