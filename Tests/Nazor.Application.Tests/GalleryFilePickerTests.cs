using Microsoft.Extensions.DependencyInjection;
using Nazor.Samples.Gallery.Examples.Fields;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class GalleryFilePickerTests
{
    [Fact]
    public async Task Gallery_invokes_window_file_services_with_selection_modes_filters_and_cancel_preservation()
    {
        var picker = new Picker();
        using var services = new ServiceCollection().AddNazorServices().AddSingleton<IFileDialogService>(picker).BuildServiceProvider();
        using var scope = services.CreateScope();
        using var host = new TestHost(scope.ServiceProvider);
        var root = host.Render<FileTokenSample>();

        async Task Browse(string id)
        {
            var field = root.Elements("FileField").Single(element => element.Id == id);
            await host.ClickAsync(field.Elements().Single(element => element.Name == "FileFieldBrowse"));
            host.RunPending();
        }

        await Browse("single-file");
        Assert.Equal("single", picker.Calls.Last());
        Assert.Equal("*.pdf", Assert.Single(Assert.Single(picker.Options!.Filters).Patterns));
        Assert.Contains("first.pdf", root.Elements("FileField").Single(element => element.Id == "single-file").Text());

        await host.ClickAsync(root.Elements("Select").Single());
        host.RunPending();
        await host.ClickAsync(root.Elements("SelectOption").Single(element => element.Text() == "Images"));
        host.RunPending();
        await Browse("multiple-files");
        Assert.Equal("multiple", picker.Calls.Last());
        Assert.Equal(new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" }, Assert.Single(picker.Options!.Filters).Patterns);
        Assert.Contains("one.png", root.Elements("FileField").Single(element => element.Id == "multiple-files").Text());
        Assert.Contains("two.png", root.Elements("FileField").Single(element => element.Id == "multiple-files").Text());

        await Browse("folder");
        Assert.Equal("folder", picker.Calls.Last());
        Assert.Contains("selected-folder", root.Elements("FileField").Single(element => element.Id == "folder").Text());

        picker.Canceled = true;
        await Browse("single-file");
        Assert.Contains("first.pdf", root.Elements("FileField").Single(element => element.Id == "single-file").Text());
        Assert.Equal("Selection canceled.", root.Elements().Single(element => element.Id == "picker-result").Text());

        picker.Unavailable = true;
        await Browse("folder");
        Assert.Contains("selected-folder", root.Elements("FileField").Single(element => element.Id == "folder").Text());
        Assert.Contains("does not provide", root.Elements().Single(element => element.Id == "picker-result").Text());
    }

    private sealed class Picker : IFileDialogService
    {
        public List<string> Calls { get; } = [];
        public OpenFileOptions? Options { get; private set; }
        public bool Canceled { get; set; }
        public bool Unavailable { get; set; }

        public Task<string?> OpenFileAsync(OpenFileOptions? options = null)
        {
            Check("single");
            Options = options;
            return Task.FromResult(Canceled ? null : "first.pdf");
        }

        public Task<IReadOnlyList<string>> OpenFilesAsync(OpenFileOptions? options = null)
        {
            Check("multiple");
            Options = options;
            return Task.FromResult<IReadOnlyList<string>>(Canceled ? [] : ["one.png", "two.png"]);
        }

        public Task<string?> PickFolderAsync(FolderPickerOptions? options = null)
        {
            Check("folder");
            return Task.FromResult(Canceled ? null : Path.Combine(Path.GetTempPath(), "selected-folder"));
        }

        public Task<string?> SaveFileAsync(SaveFileOptions? options = null) => throw new NotSupportedException();

        private void Check(string mode)
        {
            Calls.Add(mode);
            if (Unavailable) throw new PlatformNotSupportedException();
        }
    }
}
