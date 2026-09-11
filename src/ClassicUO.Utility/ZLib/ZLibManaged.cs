// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.IO.Compression;

namespace ClassicUO.Utility
{
    public static class ZLibManaged
    {
        public static ZLib.ZLibError Decompress
        (
            byte[] source,
            int sourceStart,
            int sourceLength,
            int offset,
            byte[] dest,
            int length
        )
        {
            using (var stream = new MemoryStream(source, sourceStart, sourceLength - offset, false))
            {
                using (var ds = new ZLibStream(stream, CompressionMode.Decompress))
                {
                    return ReadAll(ds, dest, length);
                }
            }
        }

        public static unsafe ZLib.ZLibError Decompress(IntPtr source, int sourceLength, int offset, IntPtr dest, int length)
        {
            // UnmanagedMemoryStream wraps the caller's pinned buffers, so decompression writes
            // straight into the destination: no staging arrays and no extra copy of either buffer.
            using (var stream = new UnmanagedMemoryStream((byte*) source + offset, sourceLength - offset))
            {
                using (var ds = new ZLibStream(stream, CompressionMode.Decompress))
                {
                    return ReadAll(ds, new Span<byte>((void*) dest, length));
                }
            }
        }

        private static ZLib.ZLibError ReadAll(ZLibStream stream, byte[] dest, int length)
        {
            int totalRead = 0;

            while (totalRead < length)
            {
                int bytesRead = stream.Read(dest, totalRead, length - totalRead);

                // The destination is the exact uncompressed size, so ending early means the
                // compressed input was truncated.
                if (bytesRead <= 0)
                    return ZLib.ZLibError.DataError;

                totalRead += bytesRead;
            }

            return HasMoreOutput(stream) ? ZLib.ZLibError.BufferError : ZLib.ZLibError.Ok;
        }

        private static ZLib.ZLibError ReadAll(ZLibStream stream, Span<byte> dest)
        {
            int totalRead = 0;

            while (totalRead < dest.Length)
            {
                int bytesRead = stream.Read(dest.Slice(totalRead));

                // The destination is the exact uncompressed size, so ending early means the
                // compressed input was truncated.
                if (bytesRead <= 0)
                    return ZLib.ZLibError.DataError;

                totalRead += bytesRead;
            }

            return HasMoreOutput(stream) ? ZLib.ZLibError.BufferError : ZLib.ZLibError.Ok;
        }

        /// <summary>
        ///     Probes for a single decompressed byte past the destination buffer. A byte means the
        ///     uncompressed data is larger than the caller-provided buffer; end of stream means the
        ///     buffer held all of it.
        /// </summary>
        private static bool HasMoreOutput(ZLibStream stream)
        {
            Span<byte> probe = stackalloc byte[1];

            return stream.Read(probe) > 0;
        }

        public static void Compress(byte[] dest, ref int destLength, byte[] source)
        {
            using (var stream = new MemoryStream(dest, true))
            {
                using (var ds = new ZLibStream(stream, CompressionMode.Compress, true))
                {
                    ds.Write(source, 0, source.Length);
                    ds.Flush();
                }

                destLength = (int) stream.Position;
            }
        }
    }
}
