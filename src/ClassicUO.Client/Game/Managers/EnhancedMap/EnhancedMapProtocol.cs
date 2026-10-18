using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Managers.EnhancedMap;

/// <summary>
/// Wire constants and helpers for the EnhancedMap protocol. EnhancedMap is unrelated to the UO
/// protocol: it is a little-endian frame protocol with a <c>[id][lenLow][lenHigh]</c> header, where
/// the length is the total frame size including those three bytes.
/// </summary>
internal static class EnhancedMapProtocol
{
    public const byte ProtocolRequest = 0x2B;
    public const byte Login = 0x1F;
    public const byte PlayerData = 0x25;
    public const byte UserData = 0x25;
    public const byte UserConnection = 0x29;
    public const byte ChatMessage = 0x23;
    public const byte ChatMessageResponse = 0x2D;
    public const byte AlertAdvice = 0x27;
    public const byte ServerMessage = 0x2F;
    public const byte SharedLabel = 0x31;
    public const byte ServerResponse = 0x21;

    public const int FrameHeaderSize = 3;

    public static readonly Encoding Wire = Encoding.ASCII;

    public const byte FlagNormal = 0;
    public const byte FlagPoisoned = 1;
    public const byte FlagYellowHits = 2;
    public const byte FlagParalyzed = 3;
    public const byte FlagDead = 4;
    public const byte FlagHidden = 5;

    /// <summary>Reassembles the total frame length from the little-endian low/high bytes.</summary>
    /// <param name="lenLow">Low byte of the length.</param>
    /// <param name="lenHigh">High byte of the length.</param>
    /// <returns>The total frame size including the three-byte header.</returns>
    public static int GetFrameLength(byte lenLow, byte lenHigh) => lenLow | (lenHigh << 8);
}

/// <summary>Little-endian writer that emits an EnhancedMap frame with its three-byte header.</summary>
internal sealed class EmPacketWriter
{
    private readonly List<byte> _buffer = new(32);

    public EmPacketWriter(byte id)
    {
        _buffer.Add(id);
        _buffer.Add(0);
        _buffer.Add(0);
    }

    public int Count => _buffer.Count;

    public EmPacketWriter U8(byte value)
    {
        _buffer.Add(value);
        return this;
    }

    public EmPacketWriter U16(ushort value)
    {
        _buffer.Add((byte)value);
        _buffer.Add((byte)(value >> 8));
        return this;
    }

    public EmPacketWriter U32(uint value)
    {
        _buffer.Add((byte)value);
        _buffer.Add((byte)(value >> 8));
        _buffer.Add((byte)(value >> 16));
        _buffer.Add((byte)(value >> 24));
        return this;
    }

    public EmPacketWriter F32(float value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
        return this;
    }

    public EmPacketWriter Ascii(string value)
    {
        _buffer.AddRange(EnhancedMapProtocol.Wire.GetBytes(value ?? string.Empty));
        return this;
    }

    /// <summary>Fills in the little-endian total length and returns the owned frame bytes.</summary>
    public byte[] Build()
    {
        int length = _buffer.Count;
        _buffer[1] = (byte)(length & 0xFF);
        _buffer[2] = (byte)((length >> 8) & 0xFF);
        return _buffer.ToArray();
    }
}

/// <summary>
/// Bounds-checked little-endian reader over a single EnhancedMap payload (header already stripped).
/// </summary>
/// <remarks>
/// Malformed input latches <see cref="Failed"/> and yields default values instead of throwing, so a
/// truncated frame received from the network can never bring the client down. Callers should check
/// <see cref="Failed"/> before trusting a fully-parsed packet.
/// </remarks>
internal ref struct EmPacketReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    public EmPacketReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
        Failed = false;
    }

    public bool Failed { get; private set; }

    public int Remaining => _data.Length - _position;

    public byte U8()
    {
        if (Remaining < 1)
        {
            Failed = true;
            return 0;
        }

        return _data[_position++];
    }

    public ushort U16()
    {
        if (Remaining < 2)
        {
            Failed = true;
            return 0;
        }

        ushort value = (ushort)(_data[_position] | (_data[_position + 1] << 8));
        _position += 2;
        return value;
    }

    public uint U32()
    {
        if (Remaining < 4)
        {
            Failed = true;
            return 0;
        }

        uint value = (uint)(_data[_position] | (_data[_position + 1] << 8) | (_data[_position + 2] << 16) | (_data[_position + 3] << 24));
        _position += 4;
        return value;
    }

    /// <summary>Reads <paramref name="count"/> raw ASCII bytes as a string.</summary>
    /// <param name="count">The number of bytes to consume.</param>
    /// <returns>The decoded string, or an empty string when <paramref name="count"/> is not positive.</returns>
    public string Ascii(int count)
    {
        if (count <= 0)
            return string.Empty;

        if (count > Remaining)
        {
            Failed = true;
            count = Remaining;
        }

        string value = EnhancedMapProtocol.Wire.GetString(_data.Slice(_position, count));
        _position += count;
        return value;
    }

    /// <summary>Advances the read cursor by <paramref name="count"/> bytes, latching failure if it would overrun.</summary>
    /// <param name="count">The number of bytes to skip.</param>
    public void Skip(int count)
    {
        if (count < 0 || count > Remaining)
        {
            Failed = true;
            _position = _data.Length;
            return;
        }

        _position += count;
    }
}
