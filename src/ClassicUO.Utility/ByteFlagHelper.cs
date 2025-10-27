namespace ClassicUO.Utility;

public static class ByteFlagHelper
{
    public static byte AddFlag(byte origin, byte flag) => (byte)(origin | flag);

    public static bool HasFlag(byte origin, byte flag) => (origin & flag) == flag;

    public static byte RemoveFlag(byte origin, byte flag) => (byte)(origin & ~flag);

    public static ulong AddFlag(ulong origin, ulong flag) => (ulong)(origin | flag);

    public static bool HasFlag(ulong origin, ulong flag) => (origin & flag) == flag;

    public static ulong RemoveFlag(ulong origin, ulong flag) => (ulong)(origin & ~flag);
}
