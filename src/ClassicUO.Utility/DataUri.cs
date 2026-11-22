#nullable enable
using System;
using System.IO;
using System.Text;

namespace ClassicUO.Utility
{
    /// <summary>
    /// Decodes RFC 2397 data URIs (<c>data:[&lt;mediatype&gt;][;base64],&lt;data&gt;</c>), letting an image or
    /// other payload travel inline instead of pointing at a remote resource.
    /// </summary>
    public static class DataUri
    {
        private const string Prefix = "data:";

        /// <summary>
        /// Checks whether <paramref name="source"/> is a data URI rather than a URL or file path.
        /// </summary>
        /// <param name="source">String to inspect.</param>
        /// <returns><see langword="true"/> when <paramref name="source"/> starts with the data URI scheme.</returns>
        public static bool IsDataUri(string source) =>
            !string.IsNullOrEmpty(source) && source.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Decodes the payload of a data URI into its raw bytes. Both base64 and percent-encoded payloads
        /// are supported; the media type in the header is not interpreted.
        /// </summary>
        /// <param name="source">Full data URI, including the <c>data:</c> scheme.</param>
        /// <param name="data">Decoded payload on success, otherwise an empty array.</param>
        /// <returns><see langword="false"/> when <paramref name="source"/> is not a data URI or is malformed.</returns>
        public static bool TryDecode(string source, out byte[] data)
        {
            data = Array.Empty<byte>();

            if (!IsDataUri(source))
                return false;

            int comma = source.IndexOf(',', Prefix.Length);

            if (comma < 0)
                return false;

            ReadOnlySpan<char> header = source.AsSpan(Prefix.Length, comma - Prefix.Length);
            ReadOnlySpan<char> payload = source.AsSpan(comma + 1);

            if (header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    data = Convert.FromBase64String(payload.ToString());

                    return true;
                }
                catch (FormatException)
                {
                    // Whitespace or stray characters in the payload: treat as an unreadable URI.
                    return false;
                }
            }

            data = PercentDecode(payload);

            return true;
        }

        /// <summary>
        /// Resolves URL escapes (<c>%XX</c>) in a non-base64 payload. Unlike query strings, <c>+</c> is a
        /// literal character here, so it is copied through untouched.
        /// </summary>
        private static byte[] PercentDecode(ReadOnlySpan<char> payload)
        {
            using var stream = new MemoryStream(payload.Length);
            Span<byte> utf8 = stackalloc byte[4];

            for (int i = 0; i < payload.Length; i++)
            {
                char current = payload[i];

                if (current == '%' && i + 2 < payload.Length
                    && Uri.IsHexDigit(payload[i + 1]) && Uri.IsHexDigit(payload[i + 2]))
                {
                    stream.WriteByte((byte)((HexValue(payload[i + 1]) << 4) | HexValue(payload[i + 2])));
                    i += 2;

                    continue;
                }

                if (current < 0x80)
                {
                    stream.WriteByte((byte)current);

                    continue;
                }

                ReadOnlySpan<char> character;

                if (char.IsHighSurrogate(current) && i + 1 < payload.Length && char.IsLowSurrogate(payload[i + 1]))
                {
                    character = payload.Slice(i, 2);
                    i++;
                }
                else
                {
                    character = payload.Slice(i, 1);
                }

                int written = Encoding.UTF8.GetBytes(character, utf8);
                stream.Write(utf8[..written]);
            }

            return stream.ToArray();
        }

        private static int HexValue(char hex) =>
            hex <= '9' ? hex - '0' : (hex | 0x20) - 'a' + 10;
    }
}
