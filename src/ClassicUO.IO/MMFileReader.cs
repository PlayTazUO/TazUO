using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace ClassicUO.IO
{
    public class MMFileReader : FileReader
    {
        private readonly MemoryMappedViewAccessor _accessor;
        private readonly MemoryMappedFile _mmf;
        private readonly BinaryReader _file;
        private unsafe byte* _ptr;

        public MMFileReader(FileStream stream) : base(stream)
        {
            if (Length <= 0)
                return;

            _mmf = MemoryMappedFile.CreateFromFile
            (
                stream,
                null,
                0,
                MemoryMappedFileAccess.Read,
                HandleInheritability.None,
                false
            );

            _accessor = _mmf.CreateViewAccessor(0, Length, MemoryMappedFileAccess.Read);

            try
            {
                unsafe
                {
                    _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
                    _file = new BinaryReader(new UnmanagedMemoryStream(_ptr, Length));
                }
            }
            catch (Exception ex)
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();

                throw new InvalidOperationException("Failed to acquire memory-mapped file pointer.", ex);
            }
        }

        public override BinaryReader Reader => _file;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe T ReadAt<T>(long offset) => Unsafe.ReadUnaligned<T>(_ptr + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe void ReadAt(long offset, Span<byte> buffer) => new ReadOnlySpan<byte>(_ptr + offset, buffer.Length).CopyTo(buffer);

        public override void Dispose()
        {
            _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor?.Dispose();
            _mmf?.Dispose();

            base.Dispose();
        }
    }
}
