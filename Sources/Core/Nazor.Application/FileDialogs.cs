using Nazor.Platform;

namespace Nazor;

/// <summary>A choice of file types: <c>new FileFilter("CSV files", "*.csv")</c>.</summary>
/// <param name="Name">The name used to identify the parameter, property, resource, or registration.</param>
/// <param name="Patterns">The patterns (string[]).</param>
public sealed record FileFilter(string Name, params string[] Patterns);

/// <summary>How <see cref="IFileDialogService" /> opens files.</summary>
public sealed record OpenFileOptions
{
    /// <summary>Gets or sets the title displayed by this window, dialog, or message.</summary>
    public string? Title { get; init; }

    /// <summary>Gets or sets the directory initially displayed by the native picker.</summary>
    public string? InitialDirectory { get; init; }

    /// <summary>The file selected when the dialog opens.</summary>
    public string? FileName { get; init; }

    /// <summary>Gets or sets the file-type filters offered by the native picker.</summary>
    public IReadOnlyList<FileFilter> Filters { get; init; } = [];
}

/// <summary>How <see cref="IFileDialogService" /> saves a file.</summary>
public sealed record SaveFileOptions
{
    /// <summary>Gets or sets the title displayed by this window, dialog, or message.</summary>
    public string? Title { get; init; }

    /// <summary>Gets or sets the directory initially displayed by the native picker.</summary>
    public string? InitialDirectory { get; init; }

    /// <summary>The name proposed.</summary>
    public string? FileName { get; init; }

    /// <summary>The extension added to a name without one (<c>csv</c>).</summary>
    public string? DefaultExtension { get; init; }

    /// <summary>Gets or sets the file-type filters offered by the native picker.</summary>
    public IReadOnlyList<FileFilter> Filters { get; init; } = [];
}

/// <summary>How <see cref="IFileDialogService" /> picks a folder.</summary>
public sealed record FolderPickerOptions
{
    /// <summary>Gets or sets the title displayed by this window, dialog, or message.</summary>
    public string? Title { get; init; }

    /// <summary>Gets or sets the directory initially displayed by the native picker.</summary>
    public string? InitialDirectory { get; init; }
}

/// <summary>
/// Provides native open, save, and folder dialogs that are modal to the window owning the scoped service. Canceling
/// returns null for a single path or an empty list for multiple paths.
/// </summary>
public interface IFileDialogService
{
    /// <summary>Opens a native file picker for a single file, modal to the current window.</summary>
    /// <param name="options">The options used to configure the operation.</param>
    /// <returns>A task returning the selected file's path, or null if the user cancels.</returns>
    Task<string?> OpenFileAsync(OpenFileOptions? options = null);

    /// <summary>Opens a native file picker that allows multiple files to be selected.</summary>
    /// <param name="options">The options used to configure the operation.</param>
    /// <returns>A task returning the selected file paths, or an empty list if the user cancels.</returns>
    Task<IReadOnlyList<string>> OpenFilesAsync(OpenFileOptions? options = null);

    /// <summary>Asks before replacing an existing file.</summary>
    /// <param name="options">The options used to configure the operation.</param>
    /// <returns>A task returning the selected destination path, or null if the user cancels.</returns>
    Task<string?> SaveFileAsync(SaveFileOptions? options = null);

    /// <summary>Opens a native folder picker, modal to the current window.</summary>
    /// <param name="options">The options used to configure the operation.</param>
    /// <returns>A task returning the selected folder path, or null if the user cancels.</returns>
    Task<string?> PickFolderAsync(FolderPickerOptions? options = null);
}

internal sealed class FileDialogService(WindowManager manager, WindowContext context) : IFileDialogService
{
    public async Task<string?> OpenFileAsync(OpenFileOptions? options = null) =>
        (await Show(Open(options, multiple: false)).ConfigureAwait(true))?.FirstOrDefault();

    public async Task<IReadOnlyList<string>> OpenFilesAsync(OpenFileOptions? options = null) =>
        await Show(Open(options, multiple: true)).ConfigureAwait(true) ?? [];

    public async Task<string?> SaveFileAsync(SaveFileOptions? options = null) =>
        (await Show(new FileDialogRequest(FileDialogKind.Save)
        {
            Title = options?.Title,
            InitialDirectory = options?.InitialDirectory,
            FileName = options?.FileName,
            DefaultExtension = options?.DefaultExtension,
            Filters = Filters(options?.Filters),
        }).ConfigureAwait(true))?.FirstOrDefault();

    public async Task<string?> PickFolderAsync(FolderPickerOptions? options = null) =>
        (await Show(new FileDialogRequest(FileDialogKind.Folder) { Title = options?.Title, InitialDirectory = options?.InitialDirectory })
            .ConfigureAwait(true))?.FirstOrDefault();

    private static FileDialogRequest Open(OpenFileOptions? options, bool multiple) => new(FileDialogKind.Open)
    {
        Title = options?.Title,
        InitialDirectory = options?.InitialDirectory,
        FileName = options?.FileName,
        Filters = Filters(options?.Filters),
        Multiple = multiple,
    };

    private static IReadOnlyList<FileDialogFilter> Filters(IReadOnlyList<FileFilter>? filters) =>
        filters is null ? [] : [.. filters.Select(filter => new FileDialogFilter(filter.Name, filter.Patterns))];

    private Task<IReadOnlyList<string>?> Show(FileDialogRequest request) => manager.ShowFileDialog(context.Window, request);
}
