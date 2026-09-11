// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.IO.Compression;

namespace ClassicUO.Utility
{
    public static class ZLibManaged
    {
        public static void Decompress
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
                    ReadAll(ds, dest, length);
                }
            }
        }

        public static unsafe void Decompress(IntPtr source, int sourceLength, int offset, IntPtr dest, int length)
        {
            // UnmanagedMemoryStream wraps the caller's pinned buffers, so decompression writes
            // straight into the destination: no staging arrays and no extra copy of either buffer.
            using (var stream = new UnmanagedMemoryStream((byte*) source, sourceLength - offset))
            {
                using (var ds = new ZLibStream(stream, CompressionMode.Decompress))
                {
                    ReadAll(ds, new Span<byte>((void*) dest, length));
                }
            }
        }

        private static void ReadAll(ZLibStream stream, byte[] dest, int length)
        {
            int totalRead = 0;

            while (totalRead < length)
            {
                int bytesRead = stream.Read(dest, totalRead, length - totalRead);

                if (bytesRead <= 0)
                    break;

                totalRead += bytesRead;
            }
        }

        private static void ReadAll(ZLibStream stream, Span<byte> dest)
        {
            int totalRead = 0;

            while (totalRead < dest.Length)
            {
                int bytesRead = stream.Read(dest.Slice(totalRead));

                if (bytesRead <= 0)
                    break;

                totalRead += bytesRead;
            }
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
