using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClassicUO.Game.UI.MyraWindows.Theme;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

/// <summary>
/// The palette is read during content load, before anything is on screen. A slot that comes back
/// null there is a startup crash rather than a missing tint, and static initialisers are exactly
/// where that goes wrong: a property initialised from a sibling declared below it reads that
/// sibling's backing store before it has been filled.
/// </summary>
public class MyraThemeTests
{
    [Fact]
    public void ThereIsAlwaysAPaletteInForce()
    {
        MyraTheme.Current.Should().NotBeNull();
        MyraTheme.Dark.Should().NotBeNull();
    }

    /// <summary>
    /// Every slot, by reflection rather than by name: a palette gains slots as the UI does, and a
    /// list written out here would only ever check the ones that already existed.
    /// </summary>
    [Fact]
    public void EverySlotOfTheShippedPaletteIsFilled()
    {
        IEnumerable<string> empty = typeof(MyraPalette)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetValue(MyraTheme.Dark) == null)
            .Select(property => property.Name);

        empty.Should().BeEmpty();
    }

    /// <summary>The nesting ramps are indexed by depth, so an empty one would leave every bracket
    /// transparent and unbordered.</summary>
    [Fact]
    public void TheNestingRampsHaveSomethingToRamp()
    {
        MyraTheme.Dark.NestingFills.Should().NotBeEmpty();
        MyraTheme.Dark.NestingBorders.Should().NotBeEmpty();
    }

    /// <summary>
    /// Myra draws under <c>BlendState.AlphaBlend</c>, which in FNA is premultiplied. A colour
    /// whose channels exceed its alpha cannot be the product of premultiplication, and renders as
    /// its full-strength self over most of what is behind it - a pale tint at low alpha comes out
    /// near-solid rather than subtle. Black scrims survive the mistake, which is exactly why it
    /// goes unnoticed until someone reaches for a light one.
    /// </summary>
    [Fact]
    public void EveryTranslucentColourIsPremultiplied()
    {
        IEnumerable<string> raw = PaletteColors(MyraTheme.Dark)
            .Where(entry => entry.Color.A < byte.MaxValue)
            .Where(entry => entry.Color.R > entry.Color.A || entry.Color.G > entry.Color.A || entry.Color.B > entry.Color.A)
            .Select(entry => $"{entry.Name} = {entry.Color}");

        raw.Should().BeEmpty();
    }

    /// <summary>Every colour the palette holds, flattened out of the single slots and the
    /// ramps alike.</summary>
    private static IEnumerable<(string Name, Color Color)> PaletteColors(MyraPalette palette)
    {
        foreach (PropertyInfo property in typeof(MyraPalette).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            object value = property.GetValue(palette);

            if (value is Color color)
                yield return (property.Name, color);

            if (value is not IReadOnlyList<Color> ramp)
                continue;

            for (int i = 0; i < ramp.Count; i++)
                yield return ($"{property.Name}[{i}]", ramp[i]);
        }
    }

    /// <summary>Depth is unbounded and the ramps are not, so lookup wraps rather than
    /// throwing.</summary>
    [Fact]
    public void ADepthPastTheEndOfARampWrapsRatherThanThrowing()
    {
        IReadOnlyList<Color> fills = MyraTheme.Dark.NestingFills;

        MyraPalette.AtDepth(fills, fills.Count).Should().Be(fills[0]);
        MyraPalette.AtDepth(fills, fills.Count * 3 + 1).Should().Be(fills[1]);
        MyraPalette.AtDepth([], 2).Should().Be(Color.Transparent);
    }
}
