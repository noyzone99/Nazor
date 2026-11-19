using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nazor.DevTools.Protocol
{
    internal static class IdeWire
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        internal static async Task<string?> ReadAsync(Stream stream, CancellationToken token)
        {
            var header = new StringBuilder();
            var one = new byte[1];
            while (!header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
            {
                if (header.Length >= 128) throw new InvalidDataException("Invalid IDE bridge header.");
                if (await stream.ReadAsync(one, 0, 1, token).ConfigureAwait(false) == 0)
                {
                    if (header.Length == 0) return null;
                    throw new EndOfStreamException();
                }
                if (one[0] > 127) throw new InvalidDataException("Invalid IDE bridge header.");
                header.Append((char)one[0]);
            }
            var text = header.ToString();
            const string prefix = "Content-Length: ";
            int length;
            if (!text.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(text.Substring(prefix.Length, text.Length - prefix.Length - 4),
                System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out length) || length <= 0 || length > 1024 * 1024)
                throw new InvalidDataException("Invalid IDE bridge length.");
            var bytes = new byte[length];
            for (var offset = 0; offset < length;)
            {
                var read = await stream.ReadAsync(bytes, offset, length - offset, token).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException();
                offset += read;
            }
            return Utf8.GetString(bytes);
        }

        internal static async Task WriteAsync(Stream stream, string json, CancellationToken token)
        {
            var bytes = Utf8.GetBytes(json);
            if (bytes.Length == 0 || bytes.Length > 1024 * 1024) throw new InvalidDataException("Oversized IDE bridge message.");
            var header = Encoding.ASCII.GetBytes("Content-Length: " + bytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\r\n\r\n");
            await stream.WriteAsync(header, 0, header.Length, token).ConfigureAwait(false);
            await stream.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
            await stream.FlushAsync(token).ConfigureAwait(false);
        }
    }
}
