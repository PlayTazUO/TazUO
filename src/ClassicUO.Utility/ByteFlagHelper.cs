namespace ClassicUO.Utility;

public static class ByteFlagHelper
{
    public static byte AddFlag(byte origin, byte flag)
    {
        return origin |= flag;
    }

    public static bool HasFlag(byte origin, byte flag)
    {
        return (origin & flag) == flag;
    }

    public static byte RemoveFlag(byte origin, byte flag)
    {
        return origin &= (byte)~flag;
    }
}
