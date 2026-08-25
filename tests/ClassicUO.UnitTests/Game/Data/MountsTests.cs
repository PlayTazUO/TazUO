using Xunit;

namespace ClassicUO.UnitTests.Game.Data;

public class MountsTests
{
    [Fact]
    public void ResolveAnimationGraphic_KnownMount_PrefersMountMapping()
    {
        ushort result = Mounts.ResolveAnimationGraphic(0x3EB4, 0x0037);

        Assert.Equal((ushort)0x007A, result);
    }

    [Fact]
    public void ResolveAnimationGraphic_UnknownMount_UsesTileDataAnimation()
    {
        ushort result = Mounts.ResolveAnimationGraphic(0x4000, 0x0123);

        Assert.Equal((ushort)0x0123, result);
    }

    [Fact]
    public void ResolveAnimationGraphic_UnknownMountWithoutTileDataAnimation_KeepsItemGraphic()
    {
        ushort result = Mounts.ResolveAnimationGraphic(0x4000, 0);

        Assert.Equal((ushort)0x4000, result);
    }
}
