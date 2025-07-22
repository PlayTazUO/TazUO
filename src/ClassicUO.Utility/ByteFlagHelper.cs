namespace ClassicUO.Utility;

public static class ByteFlagHelper
{
    public static byte AddFlag(byte origin, byte flag)
    {
        return (byte)(origin | flag);
    }

    public static bool HasFlag(byte origin, byte flag)
    {
        return (origin & flag) == flag;
    }

    public static byte RemoveFlag(byte origin, byte flag)
    {
        return (byte)(origin & ~flag);
    }

    public static ulong AddFlag(ulong origin, ulong flag)
    {
        return (ulong)(origin | flag);
    }

    public static bool HasFlag(ulong origin, ulong flag)
    {
        return (origin & flag) == flag;
    }

    public static ulong RemoveFlag(ulong origin, ulong flag)
    {
        return (ulong)(origin & ~flag);
    }
}
