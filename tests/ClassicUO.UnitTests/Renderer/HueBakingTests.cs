using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Renderer;

/// <summary>
///     Covers the CPU hue bake against the rules the pixel shader follows, since the two drawing paths
///     disagreeing is invisible until a sprite is compared side by side.
/// </summary>
public class HueBakingTests
{
    /// <summary>Opaque alpha in the RGBA8888 layout the loaders produce.</summary>
    private const uint OPAQUE = 0xFF00_0000;

    /// <summary>A hue whose ramp the synthetic tables fill; any 1-based index inside <see cref="HUES_COUNT"/>.</summary>
    private const ushort TEST_HUE = 9;

    /// <summary>Hues the synthetic tables claim to hold, chosen to span more than one 8-hue group.</summary>
    private const int HUES_COUNT = 24;

    [Fact]
    public void ApplyHue_LeavesFullyTransparentTexelCleared()
    {
        uint[] ramp = Ramp();

        HueBaking.ApplyHue(0x0012_3456, partialHue: false, ramp).Should().Be(0);
    }

    [Fact]
    public void ApplyHue_TakesRgbFromRampAndKeepsSourceAlpha()
    {
        uint[] ramp = Ramp();
        const uint alpha = 0x8000_0000;

        uint baked = HueBaking.ApplyHue(alpha | Gray(80), partialHue: false, ramp);

        baked.Should().Be((ramp[80 >> 3] & 0x00FF_FFFF) | alpha);
    }

    /// <summary>
    ///     The red channel is the shade index, rescaled from 0-255 onto the ramp's 0-31, so the two ends
    ///     and the first step are what a wrong shift would move.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, 0)]
    [InlineData(8, 1)]
    [InlineData(255, 31)]
    public void ApplyHue_PicksShadeByRedChannel(int level, int expectedShade)
    {
        uint[] ramp = Ramp();

        uint baked = HueBaking.ApplyHue(OPAQUE | Gray((byte)level), partialHue: false, ramp);

        baked.Should().Be((ramp[expectedShade] & 0x00FF_FFFF) | OPAQUE);
    }

    [Fact]
    public void ApplyHue_WithoutPartialHue_RecolorsColoredTexelsToo()
    {
        uint[] ramp = Ramp();
        uint colored = OPAQUE | Rgb(90, 20, 20);

        uint baked = HueBaking.ApplyHue(colored, partialHue: false, ramp);

        baked.Should().Be((ramp[90 >> 3] & 0x00FF_FFFF) | OPAQUE);
    }

    [Fact]
    public void ApplyHue_WithPartialHue_PassesColoredTexelsThrough()
    {
        uint[] ramp = Ramp();
        uint colored = OPAQUE | Rgb(90, 20, 20);

        HueBaking.ApplyHue(colored, partialHue: true, ramp).Should().Be(colored);
    }

    [Fact]
    public void ApplyHue_WithPartialHue_StillRecolorsTrueGray()
    {
        uint[] ramp = Ramp();

        uint baked = HueBaking.ApplyHue(OPAQUE | Gray(64), partialHue: true, ramp);

        baked.Should().Be((ramp[64 >> 3] & 0x00FF_FFFF) | OPAQUE);
    }

    /// <summary>Hue 0 means "no recoloring", and is the one case that must not touch the tables at all.</summary>
    [Fact]
    public void Bake_WithHueZero_CopiesSourceThroughWithoutReadingTables()
    {
        uint[] source = [1, 2, 3, 4];

        uint[] baked = HueBaking.Bake(source, 2, new Rectangle(0, 0, 2, 2), hue: 0, partialHue: false, hues: null);

        baked.Should().Equal(source);
    }

    /// <summary>
    ///     The region walk reads by stride rather than by row, which is the half of <c>Bake</c> that a
    ///     trimmed sprite's bounds exercise and a flat copy would silently get wrong.
    /// </summary>
    [Fact]
    public void Bake_ExtractsTheRequestedSubRectangle()
    {
        uint[] source =
        [
            00, 01, 02, 03,
            10, 11, 12, 13,
            20, 21, 22, 23
        ];

        uint[] baked = HueBaking.Bake(source, 4, new Rectangle(1, 1, 2, 2), hue: 0, partialHue: false, hues: null);

        baked.Should().Equal(11u, 12u, 21u, 22u);
    }

    [Fact]
    public void Bake_RecolorsEveryTexelThroughTheHuesRamp()
    {
        HuesLoader hues = SyntheticHues();
        uint[] source = [OPAQUE | Gray(0), OPAQUE | Gray(255)];

        uint[] baked = HueBaking.Bake(source, 2, new Rectangle(0, 0, 2, 1), TEST_HUE, partialHue: false, hues);

        baked.Should().Equal(
            (Shade(TEST_HUE, 0) & 0x00FF_FFFF) | OPAQUE,
            (Shade(TEST_HUE, 31) & 0x00FF_FFFF) | OPAQUE
        );
    }

    [Fact]
    public void TryFillHueRamp_RejectsHueZero()
    {
        Span<uint> ramp = new uint[HueBaking.HUE_RAMP_LENGTH];

        HueBaking.TryFillHueRamp(0, ramp, SyntheticHues()).Should().BeFalse();
    }

    /// <summary>
    ///     The bound is inclusive: the parameter is the 1-based wire hue, where <c>HuesCount</c> is the
    ///     last valid one rather than one past the end, unlike the loader's own 0-based accessors.
    /// </summary>
    [Fact]
    public void TryFillHueRamp_AcceptsTheLastHueAndRejectsTheOneAfterIt()
    {
        HuesLoader hues = SyntheticHues();
        Span<uint> ramp = new uint[HueBaking.HUE_RAMP_LENGTH];

        HueBaking.TryFillHueRamp((ushort)HUES_COUNT, ramp, hues).Should().BeTrue();
        HueBaking.TryFillHueRamp((ushort)(HUES_COUNT + 1), ramp, hues).Should().BeFalse();
    }

    /// <summary>
    ///     Hues are 1-based on the wire but stored eight to a group, so an off-by-one here silently
    ///     returns a neighbouring hue's colors.
    /// </summary>
    [Fact]
    public void TryFillHueRamp_ResolvesTheGroupAndEntryTheHueNames()
    {
        HuesLoader hues = SyntheticHues();
        Span<uint> ramp = new uint[HueBaking.HUE_RAMP_LENGTH];

        HueBaking.TryFillHueRamp(TEST_HUE, ramp, hues).Should().BeTrue();

        for (int shade = 0; shade < HueBaking.HUE_RAMP_LENGTH; shade++)
            ramp[shade].Should().Be(Shade(TEST_HUE, shade), $"shade {shade} of hue {TEST_HUE}");
    }

    /// <summary>A ramp with a distinguishable color per shade, so a wrong index shows as a wrong value.</summary>
    private static uint[] Ramp()
    {
        uint[] ramp = new uint[HueBaking.HUE_RAMP_LENGTH];

        for (int shade = 0; shade < ramp.Length; shade++)
            ramp[shade] = HuesHelper.Color16To32(Color16(TEST_HUE, shade));

        return ramp;
    }

    /// <summary>The color the synthetic tables hold for one shade of one hue, as a baked texel.</summary>
    /// <param name="hue">UO hue, 1-based.</param>
    /// <param name="shade">Index into the hue's ramp.</param>
    private static uint Shade(ushort hue, int shade) => HuesHelper.Color16To32(Color16(hue, shade));

    /// <summary>
    ///     A 15-bit color unique to each hue and shade pair, so a lookup landing on the wrong one is
    ///     caught rather than coincidentally matching.
    /// </summary>
    /// <param name="hue">UO hue, 1-based.</param>
    /// <param name="shade">Index into the hue's ramp.</param>
    private static ushort Color16(ushort hue, int shade) => (ushort)(0x8000 | ((hue * 32 + shade) & 0x7FFF));

    /// <summary>
    ///     Builds hue tables in memory, filled so that every hue and shade carries its own colour.
    /// </summary>
    /// <remarks>
    ///     The loader reads <c>hues.mul</c> and exposes its tables through private setters, so it is
    ///     raised without running a constructor and populated through its backing fields - the only way
    ///     to exercise the ramp lookup without shipping a data file alongside the tests.
    /// </remarks>
    /// <returns>A loader holding <see cref="HUES_COUNT"/> hues.</returns>
    private static HuesLoader SyntheticHues()
    {
        var groups = new HuesGroup[HUES_COUNT / 8];

        for (int group = 0; group < groups.Length; group++)
            for (int entry = 0; entry < 8; entry++)
            {
                // The hue these coordinates answer to, back in 1-based wire terms.
                ushort hue = (ushort)(group * 8 + entry + 1);

                for (int shade = 0; shade < HueBaking.HUE_RAMP_LENGTH; shade++)
                    groups[group].Entries[entry].ColorTable[shade] = Color16(hue, shade);
            }

        var hues = (HuesLoader)RuntimeHelpers.GetUninitializedObject(typeof(HuesLoader));

        SetBackingField(hues, nameof(HuesLoader.HuesRange), groups);
        SetBackingField(hues, nameof(HuesLoader.HuesCount), HUES_COUNT);

        return hues;
    }

    /// <summary>Writes an auto-property's backing field directly, the property being private to set.</summary>
    /// <param name="target">The instance to write to.</param>
    /// <param name="propertyName">Name of the auto-property.</param>
    /// <param name="value">The value to store.</param>
    private static void SetBackingField(object target, string propertyName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        field.Should().NotBeNull($"{propertyName} should be an auto-property on {target.GetType().Name}");
        field!.SetValue(target, value);
    }

    /// <summary>A true-gray texel at one level, which is what the hue ramp is keyed on.</summary>
    /// <param name="level">The gray level, used for all three channels.</param>
    private static uint Gray(byte level) => Rgb(level, level, level);

    /// <summary>Packs a texel in the loaders' RGBA8888 layout, where the low byte is red.</summary>
    private static uint Rgb(byte red, byte green, byte blue) => (uint)(red | (green << 8) | (blue << 16));
}
