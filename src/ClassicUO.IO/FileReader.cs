using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    public abstract class FileReader : IDisposable
    {
        private long _position;
        private readonly FileStream _stream;

        protected FileReader(FileStream stream)
        {
            _stream = stream;
        }

        public string FilePath => _stream.Name;
        public long Length => _stream.Length;
        public long Position => _position;

        public abstract BinaryReader Reader { get; }

        // Stream-backed readers (e.g. UltimaLive's growable files) are expensive to
        // read repeatedly, so Map.GetTileZ caches their blocks. Memory-mapped readers
        // are cheap enough that the cache would only add overhead.
        public virtual bool IsStreamBased => false;

        public virtual void Dispose()
        {
            Reader?.Dispose();
            _stream?.Dispose();
        }

        public virtual void Seek(long index, SeekOrigin origin) => _position = Reader.BaseStream.Seek(index, origin);

        public virtual T ReadAt<T>(long offset) where T : unmanaged
        {
            Seek(offset, SeekOrigin.Begin);
            return Read<T>();
        }

        public virtual void ReadAt(long offset, Span<byte> buffer)
        {
            Seek(offset, SeekOrigin.Begin);
            Read(buffer);
        }
        public sbyte ReadInt8() { _position += sizeof(sbyte); return Reader.ReadSByte(); }
        public byte ReadUInt8() { _position += sizeof(byte); return Reader.ReadByte(); }
        public short ReadInt16() { _position += sizeof(short); return Reader.ReadInt16(); }
        public ushort ReadUInt16() { _position += sizeof(ushort); return Reader.ReadUInt16(); }
        public int ReadInt32() { _position += sizeof(int); return Reader.ReadInt32(); }
        public uint ReadUInt32() { _position += sizeof(uint); return Reader.ReadUInt32(); }
        public long ReadInt64() { _position += sizeof(long); return Reader.ReadInt64(); }
        public ulong ReadUInt64() { _position += sizeof(ulong); return Reader.ReadUInt64(); }
        public virtual int Read(Span<byte> buffer) { _position += buffer.Length; return Reader.Read(buffer); }
        public virtual unsafe T Read<T>() where T : unmanaged
        {
            Unsafe.SkipInit<T>(out T v);
            var p = new Span<byte>(&v, sizeof(T));
            Read(p);
            return v;
        }
    }
}
