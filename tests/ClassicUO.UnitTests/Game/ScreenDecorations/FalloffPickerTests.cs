using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// The falloff editor is hand-built, so nothing but a check like this notices a curve added to
/// <see cref="FalloffCurve"/> and never wired into the dropdown - it would simply be unreachable,
/// with the enum and the effect behind it both perfectly correct.
/// </summary>
public class FalloffPickerTests
{
    private static IEnumerable<FalloffCurve> AllCurves() => Enum.GetValues<FalloffCurve>();

    [Fact]
    public void EveryCurveIsOffered()
    {
        FalloffPicker.Offered.Should().BeEquivalentTo(AllCurves());
    }

    [Fact]
    public void NoCurveIsOfferedTwice()
    {
        FalloffPicker.Offered.Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Names are what the dropdown is keyed by, so two curves sharing one would make the second
    /// unselectable rather than merely confusing.
    /// </summary>
    [Fact]
    public void EveryCurveHasItsOwnName()
    {
        IEnumerable<string> names = AllCurves().Select(FalloffPicker.DisplayName);

        names.Should().OnlyHaveUniqueItems();
        names.Should().OnlyContain(name => !string.IsNullOrWhiteSpace(name));
    }

    /// <summary>
    /// The per-option descriptions are the whole reason this editor exists rather than the grid's
    /// enum row, so a curve without one defeats the point.
    /// </summary>
    [Fact]
    public void EveryCurveExplainsItself()
    {
        IEnumerable<string> descriptions = AllCurves().Select(FalloffPicker.Description);

        descriptions.Should().OnlyHaveUniqueItems();
        descriptions.Should().OnlyContain(text => !string.IsNullOrWhiteSpace(text));
    }

    /// <summary>Both halves of every curve's copy have to be in the language file; the fallbacks
    /// make a missing key invisible at runtime.</summary>
    [Fact]
    public void EveryCurveKeyHasAnEntry()
    {
        IReadOnlyDictionary<string, string> entries = LangIniSerializer.ReadEmbedded();

        foreach (FalloffCurve curve in AllCurves())
        {
            string suffix = Suffix(curve);

            entries.Should().ContainKey($"falloff_{suffix}");
            entries.Should().ContainKey($"falloff_{suffix}_tooltip");
        }
    }

    /// <summary>The language-key suffix each curve uses, mirroring <see cref="FalloffPicker"/>.</summary>
    private static string Suffix(FalloffCurve curve) =>
        curve switch
        {
            FalloffCurve.SquareRoot => "sqrt",
            _ => curve.ToString().ToLowerInvariant()
        };
}
