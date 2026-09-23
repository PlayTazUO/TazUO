// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;

namespace ClassicUO.Utility
{
    public static class ZLib
    {
        // thanks ServUO :)

        public enum ZLibError
        {
            VersionError = -6,
            BufferError = -5,
            MemoryError = -4,
            DataError = -3,
            StreamError = -2,
            FileError = -1,

            Ok = 0,

            StreamEnd = 1,
            NeedDictionary = 2
        }

        public static ZLibError Decompress(byte[] source, int offset, byte[] dest, int length)
        {
            try
            {
                return ZLibManaged.Decompress(source, offset, source.Length, offset, dest, length);
            }
            catch (Exception e) when (e is InvalidDataException or IOException)
            {
                return ZLibError.DataError;
            }
        }

        public static ZLibError Decompress(IntPtr source, int sourceLength, int offset, IntPtr dest, int length)
        {
            // Reject malformed inputs before constructing the unmanaged stream: a null source or a
            // non-positive remaining byte count would otherwise wrap a pointer/length pair that
            // cannot be read.
            if (source == IntPtr.Zero || offset < 0 || sourceLength - offset <= 0 || length < 0)
            {
                return ZLibError.DataError;
            }

            try
            {
                return ZLibManaged.Decompress(source, sourceLength, offset, dest, length);
            }
            catch (Exception e) when (e is InvalidDataException or IOException)
            {
                return ZLibError.DataError;
            }
        }

        public static unsafe ZLibError Decompress(ReadOnlySpan<byte> source, Span<byte> dest)
        {
            fixed (byte* srcPtr = source)
            fixed (byte* destPtr = dest)
                return Decompress((IntPtr)srcPtr, source.Length, 0, (IntPtr)destPtr, dest.Length);
        }
    }
}
