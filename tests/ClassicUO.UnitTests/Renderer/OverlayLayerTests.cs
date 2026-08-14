using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Microsoft.Xna.Framework.Graphics;
using Xunit;

namespace ClassicUO.UnitTests.Renderer;

public class OverlayLayerTests
{
    [Fact]
    public void DefaultLayer_UsesAlphaBlend()
    {
        OverlayLayer layer = default;

        layer.Blend.Should().Be(OverlayBlend.Alpha);
    }

    [Fact]
    public void Alpha_MapsToNonPremultiplied()
    {
        OverlayBlend.Alpha.ToBlendState().Should().BeSameAs(BlendState.NonPremultiplied);
    }

    [Fact]
    public void Additive_MapsToAdditive()
    {
        OverlayBlend.Additive.ToBlendState().Should().BeSameAs(BlendState.Additive);
    }

    /// <summary>
    /// The shader emits straight alpha, so both blend states must scale the source by its own
    /// alpha. Getting this wrong makes an additive layer ignore its opacity entirely.
    /// </summary>
    [Theory]
    [InlineData(OverlayBlend.Alpha)]
    [InlineData(OverlayBlend.Additive)]
    public void BothBlendStates_TakeSourceAlphaAsSourceFactor(OverlayBlend blend)
    {
        BlendState state = blend.ToBlendState();

        state.ColorSourceBlend.Should().Be(Blend.SourceAlpha);
    }

    [Fact]
    public void AdditiveBlend_AccumulatesIntoDestination()
    {
        BlendState state = OverlayBlend.Additive.ToBlendState();

        state.ColorDestinationBlend.Should().Be(Blend.One);
    }
}
