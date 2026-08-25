using Xunit;

namespace ClassicUO.UnitTests.Game.Data;

public class MountsTests
{
    /// <summary>Verifies that stale tile data cannot override a known mount mapping.</summary>
    [Fact]
    public void ResolveAnimationGraphic_KnownMount_PrefersMountMapping()
    {
        ushort result = Mounts.ResolveAnimationGraphic(0x3EB4, 0x0037);

        Assert.Equal((ushort)0x007A, result);
    }

    /// <summary>Verifies that unknown custom mounts retain the tile data fallback.</summary>
    [Fact]
    public void ResolveAnimationGraphic_UnknownMount_UsesTileDataAnimation()
    {
        ushort result = Mounts.ResolveAnimationGraphic(0x4000, 0x0123);

        Assert.Equal((ushort)0x0123, result);
    }

    /// <summary>Verifies that an unknown mount without a fallback retains its item graphic.</summary>
    [Fact]
    public void ResolveAnimationGraphic_UnknownMountWithoutTileDataAnimation_KeepsItemGraphic()
    {
        ushort result = Mounts.ResolveAnimationGraphic(0x4000, 0);

        Assert.Equal((ushort)0x4000, result);
    }
}
