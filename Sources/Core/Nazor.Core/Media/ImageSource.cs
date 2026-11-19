using System.Globalization;
using System.Reflection;

namespace Nazor;

/// <summary>The encoded bytes of an image and the display scale they were made for (2 for "logo@2x.png").</summary>
/// <param name="Bytes">The bytes (byte[]).</param>
/// <param name="Scale">The scale (float).</param>
public sealed record ImageData(byte[] Bytes, float Scale);

/// <summary>
/// Where an image comes from: a file, an embedded resource, a stream, bytes or a URI. A string converts to a source,
/// so markup can write <c>Source="images/logo.png"</c>: "http://" and "https://" are URIs, "file://" and absolute
/// paths files, other paths files relative to the application directory. Files and resources have variants by display
/// scale: at 200% "logo@2x.png" is taken instead of "logo.png" when it exists (also @1.25x, @1.5x, @3x and @4x).
/// </summary>
public abstract class ImageSource : IEquatable<ImageSource>
{

    private static readonly float[] Factors = [1, 1.25f, 1.5f, 2, 3, 4];

    private protected ImageSource()
    {
    }

    /// <summary>The identity of the image: sources with the same key give the same image (caches use it).</summary>
    public abstract string Key { get; }

    /// <summary>Whether the source has variants by display scale (files and resources).</summary>
    public virtual bool HasVariants => false;

    /// <summary>
    /// Reads the encoded bytes of the best variant for <paramref name="scale" />; throws when the image cannot be
    /// read.
    /// </summary>
    /// <param name="scale">The number of physical pixels per logical unit.</param>
    /// <param name="cancellationToken">A token used to request cancellation.</param>
    /// <returns>A task whose result is the value produced by the operation.</returns>
    public abstract Task<ImageData> LoadAsync(float scale, CancellationToken cancellationToken = default);

    /// <summary>Creates an image source backed by a file path.</summary>
    /// <param name="path">The file path used by this operation.</param>
    /// <returns>An image source pointing to the file.</returns>
    public static ImageSource FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return new FileImageSource(Path.GetFullPath(path, AppContext.BaseDirectory));
    }

    /// <summary>
    /// An embedded resource by its manifest name, for example "MyApp.Images.logo.png" or a NazorAsset path.
    /// </summary>
    /// <param name="assembly">The assembly (Assembly).</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <returns>The resulting ImageSource value.</returns>
    /// <exception cref="ArgumentNullException">assembly is null.</exception>
    public static ImageSource FromResource(Assembly assembly, string name)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrEmpty(name);
        return new ResourceImageSource(assembly, name);
    }

    /// <summary>A stream opened for every load; <paramref name="key" /> names the image for caches.</summary>
    /// <param name="open">The open (Func&lt;CancellationToken, Task&lt;Stream&gt;&gt;).</param>
    /// <param name="key">The key (string).</param>
    /// <returns>The resulting ImageSource value.</returns>
    /// <exception cref="ArgumentNullException">open is null.</exception>
    public static ImageSource FromStream(Func<CancellationToken, Task<Stream>> open, string key)
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentException.ThrowIfNullOrEmpty(key);
        return new StreamImageSource(open, key);
    }

    /// <summary>Bytes in memory; every call is a different image for caches.</summary>
    /// <param name="bytes">The bytes (byte[]).</param>
    /// <returns>The resulting ImageSource value.</returns>
    /// <exception cref="ArgumentNullException">bytes is null.</exception>
    public static ImageSource FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return new BytesImageSource(bytes);
    }

    /// <summary>Creates an image source backed by the supplied URI.</summary>
    /// <param name="uri">The target address or image URI.</param>
    /// <returns>An image source pointing to the URI.</returns>
    /// <exception cref="ArgumentNullException">uri is null.</exception>
    public static ImageSource FromUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (uri.IsFile) return FromFile(uri.LocalPath);
        if (uri.Scheme is not ("http" or "https")) throw new ArgumentException($"Only file, http and https URIs are supported, not '{uri.Scheme}'.", nameof(uri));
        return new UriImageSource(uri);
    }

    /// <summary>Parses from the current token position.</summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting ImageSource value.</returns>
    public static ImageSource Parse(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "file") return FromUri(uri);
        return FromFile(value);
    }

    /// <summary>Converts a string value to ImageSource.</summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting ImageSource value.</returns>
    public static implicit operator ImageSource(string value) => Parse(value);

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public bool Equals(ImageSource? other) => other is not null && other.Key == Key;

    /// <summary>Determines whether the supplied value is equal to this instance.</summary>
    /// <param name="obj">The value to compare with this instance.</param>
    /// <returns>true if the supplied value satisfies the comparison; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is ImageSource other && Equals(other);

    /// <summary>Returns a hash code consistent with equality for this value.</summary>
    /// <returns>The hash code for this value.</returns>
    public override int GetHashCode() => Key.GetHashCode(StringComparison.Ordinal);

    /// <summary>Returns a textual representation of this value.</summary>
    /// <returns>The textual representation of this value.</returns>
    public override string ToString() => Key;

    internal static IEnumerable<float> Candidates(float scale) =>
        Factors.Where(f => f >= scale - 0.01f).Concat(Factors.Where(f => f < scale - 0.01f).Reverse());

    internal static string VariantName(string name, float factor)
    {
        if (factor == 1) return name;
        var extension = Path.GetExtension(name);
        return string.Create(CultureInfo.InvariantCulture, $"{name[..^extension.Length]}@{factor:0.##}x{extension}");
    }

    private sealed class FileImageSource(string path) : ImageSource
    {
        public override string Key => "file:" + path;

        public override bool HasVariants => true;

        public override async Task<ImageData> LoadAsync(float scale, CancellationToken cancellationToken)
        {
            foreach (var factor in Candidates(scale))
            {
                var variant = VariantName(path, factor);
                if (File.Exists(variant)) return new ImageData(await File.ReadAllBytesAsync(variant, cancellationToken).ConfigureAwait(false), factor);
            }
            throw new FileNotFoundException($"Image not found: {path}", path);
        }
    }

    private sealed class ResourceImageSource(Assembly assembly, string name) : ImageSource
    {
        public override string Key => $"resource:{assembly.GetName().Name}/{name}";

        public override bool HasVariants => true;

        public override async Task<ImageData> LoadAsync(float scale, CancellationToken cancellationToken)
        {
            foreach (var factor in Candidates(scale))
            {
                await using var stream = assembly.GetManifestResourceStream(VariantName(name, factor));
                if (stream is not null) return new ImageData(await ReadAllAsync(stream, cancellationToken).ConfigureAwait(false), factor);
            }
            throw new FileNotFoundException($"Image resource '{name}' not found in {assembly.GetName().Name}.", name);
        }
    }

    private sealed class StreamImageSource(Func<CancellationToken, Task<Stream>> open, string key) : ImageSource
    {
        public override string Key => "stream:" + key;

        public override async Task<ImageData> LoadAsync(float scale, CancellationToken cancellationToken)
        {
            await using var stream = await open(cancellationToken).ConfigureAwait(false);
            return new ImageData(await ReadAllAsync(stream, cancellationToken).ConfigureAwait(false), 1);
        }
    }

    private sealed class BytesImageSource(byte[] bytes) : ImageSource
    {
        private static long next;
        private readonly string key = "bytes:" + Interlocked.Increment(ref next).ToString(CultureInfo.InvariantCulture);

        public override string Key => key;

        public override Task<ImageData> LoadAsync(float scale, CancellationToken cancellationToken) => Task.FromResult(new ImageData(bytes, 1));
    }

    private sealed class UriImageSource(Uri uri) : ImageSource
    {
        private static readonly Lazy<HttpClient> Client = new(() => new HttpClient());

        public override string Key => uri.AbsoluteUri;

        public override async Task<ImageData> LoadAsync(float scale, CancellationToken cancellationToken) =>
            new(await Client.Value.GetByteArrayAsync(uri, cancellationToken).ConfigureAwait(false), 1);
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return memory.ToArray();
    }
}
