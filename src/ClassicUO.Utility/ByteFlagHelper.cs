using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ClassicUO.Utility;

public static class ByteFlagHelper
{
    public static byte AddFlag(byte origin, byte flag) => (byte)(origin | flag);

    public static bool HasFlag(byte origin, byte flag) => (origin & flag) == flag;

    public static byte RemoveFlag(byte origin, byte flag) => (byte)(origin & ~flag);

    public static ulong AddFlag(ulong origin, ulong flag) => origin | flag;

    public static bool HasFlag(ulong origin, ulong flag) => (origin & flag) == flag;

    public static ulong RemoveFlag(ulong origin, ulong flag) => origin & ~flag;


    public static TEnum AllBits<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Aggregate(default(TEnum), AddFlag);

    public static bool HasFlag<TEnum>(TEnum value, TEnum flag) where TEnum : struct, Enum
    {
        ulong valueBits = ToBits(value);
        ulong flagBits = ToBits(flag);

        return (valueBits & flagBits) == flagBits;
    }

    public static TEnum AddFlag<TEnum>(TEnum value, TEnum flag) where TEnum : struct, Enum
    {
        ulong valueBits = ToBits(value);
        ulong flagBits = ToBits(flag);

        return FromBits<TEnum>(valueBits | flagBits);
    }

    public static TEnum RemoveFlag<TEnum>(TEnum value, TEnum flag) where TEnum : struct, Enum
    {
        ulong valueBits = ToBits(value);
        ulong flagBits = ToBits(flag);

        return FromBits<TEnum>(valueBits & ~flagBits);
    }

    // Uses unsafe interpretation to allow catering for both signed and unsigned enums
    private static ulong ToBits<TEnum>(TEnum value) where TEnum : struct, Enum =>
        Unsafe.SizeOf<TEnum>() switch
        {
            1 => Unsafe.As<TEnum, byte>(ref value),
            2 => Unsafe.As<TEnum, ushort>(ref value),
            4 => Unsafe.As<TEnum, uint>(ref value),
            8 => Unsafe.As<TEnum, ulong>(ref value),
            _ => throw new ArgumentException($"Unsupported enum size for {typeof(TEnum)}")
        };

    private static TEnum FromBits<TEnum>(ulong bits) where TEnum : struct, Enum
    {
        switch (Unsafe.SizeOf<TEnum>())
        {
            case 1:
                byte b = (byte)bits;
                return Unsafe.As<byte, TEnum>(ref b);
            case 2:
                ushort s = (ushort)bits;
                return Unsafe.As<ushort, TEnum>(ref s);
            case 4:
                uint i = (uint)bits;
                return Unsafe.As<uint, TEnum>(ref i);
            case 8:
                return Unsafe.As<ulong, TEnum>(ref bits);
            default: throw new ArgumentException($"Unsupported enum size for {typeof(TEnum)}");
        }
    }
}
