#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClassicUO.Game.UI.Gumps;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

/// <summary>
/// Covers the single normalization point every world-map icon name passes through, on both the
/// registration side (icon file names) and the marker side (UOAM, CSV and Ultima Mapper files).
/// </summary>
public class MarkerIconKeyTests
{
    /// <summary>Cultures whose casing rules diverge from the invariant fold for ASCII 'I'.</summary>
    private static readonly string[] DottedlessICultures = ["tr-TR", "tr", "tr-CY", "az-AZ", "az-Latn-AZ"];

    /// <summary>Icon names that actually occur in the reference UOAM files.</summary>
    private static readonly string[] RealIconNames =
    [
        "Island", "Terrain", "Daily Rares", "Body of Water", "Minax's Fortress",
        "point of interest", "teleporter", "treasure", "landmark", "warriors guild"
    ];

    private static void InCulture(string culture, System.Action body)
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            body();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    #region Locale independence

    /// <summary>
    /// The defect this choke point exists for: Turkish and Azeri fold 'I' to a dotless 'i', which
    /// no comparer equates with 'i', so an "Island.png" would stop matching its markers.
    /// </summary>
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("tr")]
    [InlineData("tr-CY")]
    [InlineData("az-AZ")]
    [InlineData("az-Latn-AZ")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    public void CapitalIFoldsToAsciiIInEveryCulture(string culture)
    {
        InCulture(culture, () => MarkerIconKey.Normalize("Island").Should().Be("island"));
    }

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("az-AZ")]
    [InlineData("en-US")]
    public void EveryRealIconNameNormalizesIdenticallyAcrossCultures(string culture)
    {
        InCulture(culture, () =>
        {
            List<string> keys = RealIconNames.Select(MarkerIconKey.Normalize).ToList();

            keys.Should().Equal(
                "island",
                "terrain",
                "daily rares",
                "body of water",
                "minax's fortress",
                "point of interest",
                "teleporter",
                "treasure",
                "landmark",
                "warriors guild"
            );
        });
    }

    /// <summary>
    /// Registration and lookup must agree whichever side ran under which locale - this is the
    /// pairing that silently broke before, since both halves folded culture-sensitively.
    /// </summary>
    [Fact]
    public void RegistrationAndLookupAgreeAcrossDifferingLocales()
    {
        foreach (string registrationCulture in DottedlessICultures.Concat(["en-US"]))
        {
            string registered = string.Empty;

            // What RegisterMarkerIcon feeds in: the icon file's name without its extension.
            InCulture(registrationCulture, () => registered = MarkerIconKey.Normalize(Path.GetFileNameWithoutExtension("Island.png")));

            foreach (string lookupCulture in DottedlessICultures.Concat(["en-US", "ja-JP"]))
            {
                string looked = string.Empty;

                InCulture(lookupCulture, () => looked = MarkerIconKey.Normalize("Island"));

                looked.Should().Be(
                    registered,
                    $"a marker keyed under {lookupCulture} must find an icon registered under {registrationCulture}"
                );
            }
        }
    }

    /// <summary>
    /// A key already stored in the dotless form (written by an older build under tr-TR) stays
    /// dotless, so it will not match. Documents the known migration gap rather than hiding it.
    /// </summary>
    [Fact]
    public void AlreadyDotlessKeyIsLeftAloneAndDoesNotMatchTheAsciiForm()
    {
        MarkerIconKey.Normalize("ısland").Should().Be("ısland");
        MarkerIconKey.Normalize("ısland").Should().NotBe("island");
    }

    #endregion

    #region Shape

    [Theory]
    [InlineData("point of interest", "point of interest")]
    [InlineData("POINT OF INTEREST", "point of interest")]
    [InlineData("Point Of Interest", "point of interest")]
    [InlineData("  point of interest  ", "point of interest")]
    [InlineData("\tteleporter\t", "teleporter")]
    [InlineData("Minax's Fortress", "minax's fortress")]
    public void NormalizesCaseAndOuterPadding(string raw, string expected)
    {
        MarkerIconKey.Normalize(raw).Should().Be(expected);
    }

    [Fact]
    public void SpacingInsideTheNameIsPreserved()
    {
        MarkerIconKey.Normalize("Body  of  Water").Should().Be("body  of  water");
    }

    /// <summary>
    /// A missing Ultima Mapper <c>Icon</c> attribute used to reach <c>.ToLower()</c> as null and
    /// throw out of the gump constructor, taking the whole world map with it.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void AbsentOrBlankNameYieldsAnEmptyKeyRatherThanThrowing(string? raw)
    {
        MarkerIconKey.Normalize(raw).Should().BeEmpty();
    }

    [Fact]
    public void NormalizationIsIdempotent()
    {
        foreach (string raw in RealIconNames)
        {
            string once = MarkerIconKey.Normalize(raw);

            MarkerIconKey.Normalize(once).Should().Be(once);
        }
    }

    #endregion
}
