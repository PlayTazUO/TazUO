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
            using (var stream = new MemoryStream(source, sourceStart, sourceLength - offset, true))
            {
                using (var ds = new ZLibStream(stream, CompressionMode.Decompress))
                {
                    int totalRead = 0;

                    while (totalRead < length)
                    {
                        // Read directly into destination buffer in chunks
                        int toRead = Math.Min(4096, length - totalRead);
                        int bytesRead = ds.Read(dest, totalRead, toRead);
                        if (bytesRead <= 0)
                            break;
                        totalRead += bytesRead;
                    }
                }
            }
        }

        public static unsafe void Decompress(IntPtr source, int sourceLength, int offset, IntPtr dest, int length)
        {
            // Use a temporary buffer to leverage the optimized byte array version
            byte[] tempDest = new byte[length];
            byte[] tempSource = new byte[sourceLength - offset];

            // Copy from unmanaged to managed
            fixed (byte* tempSourcePtr = tempSource)
            {
                Buffer.MemoryCopy((byte*)source.ToPointer(), tempSourcePtr, tempSource.Length, tempSource.Length);
            }

            // Decompress using the byte array version
            Decompress(tempSource, 0, sourceLength, offset, tempDest, length);

            // Copy result back to unmanaged
            fixed (byte* tempDestPtr = tempDest)
            {
                Buffer.MemoryCopy(tempDestPtr, (byte*)dest.ToPointer(), length, length);
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
