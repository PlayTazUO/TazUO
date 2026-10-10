using ClassicUO.Network;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Network;

/// <summary>
/// The 0xB9 length decides where every following packet starts, so a wrong value desyncs the whole stream.
/// </summary>
public class PacketsTableTests
{
    private const ClientVersion Modern7 = (ClientVersion)0x07007501; // 7.0.117.1
    private const ClientVersion Legacy6 = (ClientVersion)0x06000E01; // 6.0.14.1

    [Fact]
    public void ModernClientReadsFiveByteFeaturesPacket()
    {
        new PacketsTable(Modern7).GetPacketLength(0xB9).Should().Be(5);
    }

    [Fact]
    public void LegacyServerFormatReadsThreeByteFeaturesPacket()
    {
        var table = new PacketsTable(Modern7);

        table.ApplyServerFormat(true);

        table.GetPacketLength(0xB9).Should().Be(3);
        table.LegacyLoginFormat.Should().BeTrue();
    }

    [Fact]
    public void TurningLegacyFormatOffRestoresModernLength()
    {
        var table = new PacketsTable(Modern7);
        table.ApplyServerFormat(true);

        table.ApplyServerFormat(false);

        table.GetPacketLength(0xB9).Should().Be(5);
        table.LegacyLoginFormat.Should().BeFalse();
    }

    [Fact]
    public void OlderClientAlwaysReadsThreeByteFeaturesPacket()
    {
        new PacketsTable(Legacy6).GetPacketLength(0xB9).Should().Be(3);
    }
}
