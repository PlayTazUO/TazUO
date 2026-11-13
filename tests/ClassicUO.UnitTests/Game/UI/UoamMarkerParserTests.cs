#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClassicUO.Game.UI.Gumps;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

/// <summary>
/// Covers UOAM <c>.map</c> marker parsing. Fixture lines are copied verbatim from real UOAM
/// 2003-era files (Atlas/Common/Dungeons/Rares/Treasure) so the expectations track the actual
/// format rather than an assumed one.
/// </summary>
public class UoamMarkerParserTests
{
    #region Icon names are kept whole

    /// <summary>
    /// Cutting the icon name at the first space made multi-word names unmatchable against the icon
    /// files named after them. 271 of 1467 markers in the reference files are multi-word.
    /// </summary>
    [Theory]
    [InlineData("+Body of Water: 1496 472 0 Arrow's Point Bay", "body of water")]
    [InlineData("-point of interest: 3728 1360 0 Chessboard", "point of interest")]
    [InlineData("+Daily Rares: 3778 1251 0 Basket", "daily rares")]
    [InlineData("-armourers guild: 3587 2601 2 Armoury", "armourers guild")]
    [InlineData("-rogues guild: 1442 1662 0 Artists' Guild", "rogues guild")]
    [InlineData("-blacksmiths guild: 1348 1778 0 Britain's Blacksmith Guild", "blacksmiths guild")]
    [InlineData("-warriors guild: 1939 2739 0 The Brotherhood of Trinsic", "warriors guild")]
    [InlineData("+marble patio: 3599 2491 2 Companion Hall", "marble patio")]
    [InlineData("-fishermans guild: 2963 813 0 The Fisherman's Guild", "fishermans guild")]
    [InlineData("+Minax's Fortress: 5778 3376 1 Minax's Fortress", "minax's fortress")]
    [InlineData("-illusionists guild: 4626 1207 0 Masters of Illusion", "illusionists guild")]
    public void MultiWordIconNamesSurviveWhole(string line, string expectedIconName)
    {
        UoamMarkerParser.ParseLine(line, out WorldMapGump.WMapMarker? marker, out _)
            .Should().Be(UoamLineResult.Parsed);

        marker!.MarkerIconName.Should().Be(expectedIconName);
    }

    [Theory]
    [InlineData("+teleporter: 791 1548 3 ", "teleporter")]
    [InlineData("+Terrain: 1496 472 0 Hill", "terrain")]
    [InlineData("-treasure: 1 2 0 Chest", "treasure")]
    public void SingleWordIconNamesAreLowercased(string line, string expectedIconName)
    {
        UoamMarkerParser.ParseLine(line, out WorldMapGump.WMapMarker? marker, out _)
            .Should().Be(UoamLineResult.Parsed);

        marker!.MarkerIconName.Should().Be(expectedIconName);
    }

    /// <summary>
    /// Icon names are ordinal dictionary keys and are persisted, so casing must not depend on the
    /// machine's locale: Turkish and Azeri fold 'I' to a dotless 'i', which would orphan the icon.
    /// </summary>
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("az-AZ")]
    [InlineData("en-US")]
    public void IconNameCasingIsCultureIndependent(string culture)
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            UoamMarkerParser.ParseLine("+Island: 1496 472 0 Dagger Isle", out WorldMapGump.WMapMarker? marker, out _)
                .Should().Be(UoamLineResult.Parsed);

            marker!.MarkerIconName.Should().Be("island");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void IconNameIsNeverTruncatedAtItsFirstSpace()
    {
        UoamMarkerParser.ParseLine(
            "-point of interest: 237 48 3 alchemy workshop + rifts",
            out WorldMapGump.WMapMarker? marker,
            out _
        );

        marker!.MarkerIconName.Should().NotBe("point");
        marker.MarkerIconName.Should().Be("point of interest");
    }

    #endregion

    #region Marker name field

    [Fact]
    public void NameKeepsPunctuationAndEmbeddedSigns()
    {
        UoamMarkerParser.ParseLine(
            "-point of interest: 237 48 3 alchemy workshop + rifts",
            out WorldMapGump.WMapMarker? marker,
            out _
        );

        marker!.Name.Should().Be("alchemy workshop + rifts");
    }

    /// <summary>One reference marker carries a trailing space after a quoted name.</summary>
    [Fact]
    public void TrailingWhitespaceIsTrimmedFromName()
    {
        UoamMarkerParser.ParseLine(
            "+champion: 1645 1108 3 Champ \"Lord Oaks and the Forest Queen\" ",
            out WorldMapGump.WMapMarker? marker,
            out _
        );

        marker!.Name.Should().Be("Champ \"Lord Oaks and the Forest Queen\"");
    }

    [Fact]
    public void SpacingInsideNameIsPreserved()
    {
        UoamMarkerParser.ParseLine("+inn: 1 2 0 Double  Spaced  Name", out WorldMapGump.WMapMarker? marker, out _);

        marker!.Name.Should().Be("Double  Spaced  Name");
    }

    /// <summary>
    /// Unlabeled markers are valid UOAM data, not malformed input: 18 reference markers (mostly
    /// teleporters) carry coordinates and no name.
    /// </summary>
    [Theory]
    [InlineData("+teleporter: 791 1548 3 ")]
    [InlineData("+teleporter: 787 1537 3")]
    public void UnlabeledMarkersParseWithAnEmptyName(string line)
    {
        UoamMarkerParser.ParseLine(line, out WorldMapGump.WMapMarker? marker, out _)
            .Should().Be(UoamLineResult.Parsed);

        marker!.Name.Should().BeEmpty();
        marker.X.Should().BePositive();
    }

    #endregion

    #region Coordinates

    [Fact]
    public void ParsesCoordinatesAndMapIdFromReferenceLine()
    {
        UoamMarkerParser.ParseLine(
                "+Minax's Fortress: 5778 3376 1 Minax's Fortress",
                out WorldMapGump.WMapMarker? marker,
                out _
            )
            .Should().Be(UoamLineResult.Parsed);

        marker!.X.Should().Be(5778);
        marker.Y.Should().Be(3376);
        marker.MapId.Should().Be(1);
        marker.Name.Should().Be("Minax's Fortress");
        marker.ZoomIndex.Should().Be(3);
    }

    /// <summary>
    /// Skipping a fixed two characters past the separator assumed exactly one following space, so a
    /// file written without it lost the first digit of X and parsed to wrong coordinates silently.
    /// </summary>
    [Theory]
    [InlineData("+rune:100 200 0 No space after separator")]
    [InlineData("+rune: 100 200 0 One space")]
    [InlineData("+rune:   100 200 0 Several spaces")]
    [InlineData("+rune:\t100 200 0 Tab")]
    public void SeparatorPaddingDoesNotShiftCoordinates(string line)
    {
        UoamMarkerParser.ParseLine(line, out WorldMapGump.WMapMarker? marker, out _)
            .Should().Be(UoamLineResult.Parsed);

        marker!.X.Should().Be(100);
        marker.Y.Should().Be(200);
        marker.MapId.Should().Be(0);
    }

    [Fact]
    public void IconNameIsTrimmedOfSeparatorPadding()
    {
        UoamMarkerParser.ParseLine("+ point of interest : 1 2 0 Padded", out WorldMapGump.WMapMarker? marker, out _);

        marker!.MarkerIconName.Should().Be("point of interest");
    }

    #endregion

    #region Malformed lines - the ArgumentOutOfRangeException regression

    /// <summary>
    /// A marker line with no separator drove <c>Substring(1, IndexOf(':') - 1)</c> to a length of
    /// -2, throwing out of the gump constructor and making the world map unopenable.
    /// </summary>
    [Theory]
    [InlineData("+NoColonHere")]
    [InlineData("-AlsoNoColon")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("+:")]
    [InlineData("-A:")]
    [InlineData("+point of interest")]
    public void MarkerLineWithoutUsableSeparatorIsReportedNotThrown(string line)
    {
        UoamLineResult result = UoamMarkerParser.ParseLine(
            line,
            out WorldMapGump.WMapMarker? marker,
            out string? reason
        );

        result.Should().Be(UoamLineResult.Malformed);
        marker.Should().BeNull();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("+rune: abc 200 0 Bad X")]
    [InlineData("+rune: 100 abc 0 Bad Y")]
    [InlineData("+rune: 100 200 abc Bad map id")]
    [InlineData("+rune: 99999999999999999999 1 0 Overflows Int32")]
    [InlineData("+rune: 100 200")]
    [InlineData("+rune: 1.5 2.5 0 Not integers")]
    public void UnparseableFieldsAreReportedAsMalformed(string line)
    {
        UoamLineResult result = UoamMarkerParser.ParseLine(
            line,
            out WorldMapGump.WMapMarker? marker,
            out string? reason
        );

        result.Should().Be(UoamLineResult.Malformed);
        marker.Should().BeNull();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Negative coordinates are accepted; the reference files do not use them, but the format
    /// carries a sign and rejecting them would be a guess.
    /// </summary>
    [Fact]
    public void NegativeCoordinatesAreAccepted()
    {
        UoamMarkerParser.ParseLine("+rune: -5 -6 0 Below origin", out WorldMapGump.WMapMarker? marker, out _)
            .Should().Be(UoamLineResult.Parsed);

        marker!.X.Should().Be(-5);
        marker.Y.Should().Be(-6);
    }

    /// <summary>No input may escape as an exception; the gump constructor has no guard above this.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(":")]
    [InlineData("::::")]
    [InlineData("+:::")]
    [InlineData("3")]
    [InlineData("\t")]
    [InlineData(" ")]
    [InlineData("+ :1 2 0 Empty icon name")]
    [InlineData("­+rune: 1 2 0 Soft hyphen first")]
    [InlineData("😀rune: 1 2 0 Emoji first")]
    [InlineData("+\ud83d: 1 2 0 Lone high surrogate icon")]
    [InlineData("+\ude00: 1 2 0 Lone low surrogate icon")]
    [InlineData("+rune: 1 2 0 ")]
    [InlineData("+rune:")]
    [InlineData("-rune: ")]
    public void NoInputEscapesAsAnException(string line)
    {
        Action parse = () => UoamMarkerParser.ParseLine(line, out _, out _);

        parse.Should().NotThrow();
    }

    #endregion

    #region Ignorable lines

    [Theory]
    [InlineData("3")]
    [InlineData("")]
    [InlineData("Some header text")]
    [InlineData("// a comment")]
    [InlineData(":leading separator")]
    [InlineData("   ")]
    public void NonMarkerLinesAreIgnoredWithoutCountingAsMalformed(string line)
    {
        UoamMarkerParser.ParseLine(line, out WorldMapGump.WMapMarker? marker, out _)
            .Should().Be(UoamLineResult.Ignored);

        marker.Should().BeNull();
    }

    [Theory]
    [InlineData("+rune: ")]
    [InlineData("+rune: 100")]
    public void MarkerLinesWithNoCoordinateFieldsAreIgnored(string line)
    {
        UoamMarkerParser.ParseLine(line, out WorldMapGump.WMapMarker? marker, out _)
            .Should().Be(UoamLineResult.Ignored);

        marker.Should().BeNull();
    }

    #endregion

    #region Whole-file loading

    /// <summary>Reference-shaped file: UOAM's "3" header, trailing-space lines, multi-word icons.</summary>
    private static readonly string[] ReferenceFileLines =
    [
        "3",
        "+teleporter: 791 1548 3 ",
        "+teleporter: 787 1537 3 ",
        "-point of interest: 63 1447 3 alchemy room",
        "-point of interest: 237 48 3 alchemy workshop + rifts",
        "+Body of Water: 1496 472 0 Arrow's Point Bay",
        "+Minax's Fortress: 5778 3376 1 Minax's Fortress",
        "+champion: 1645 1108 3 Champ \"Lord Oaks and the Forest Queen\" ",
        "-warriors guild: 1939 2739 0 The Brotherhood of Trinsic",
    ];

    private static string WriteTempMapFile(IEnumerable<string> lines, string newLine = "\r\n")
    {
        string path = Path.Combine(Path.GetTempPath(), $"uoam-test-{Guid.NewGuid():N}.map");

        File.WriteAllText(path, string.Join(newLine, lines) + newLine, Encoding.ASCII);

        return path;
    }

    /// <summary>The reference files are CRLF; a hand-edited one on Linux would not be.</summary>
    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    public void LoadsEveryMarkerRegardlessOfLineEndings(string newLine)
    {
        string path = WriteTempMapFile(ReferenceFileLines, newLine);

        try
        {
            List<WorldMapGump.WMapMarker> markers = UoamMarkerParser.LoadFile(path, out int skippedLines);

            skippedLines.Should().Be(0);
            markers.Should().HaveCount(ReferenceFileLines.Length - 1, "the \"3\" header is not a marker");
            markers.Select(marker => marker.MarkerIconName).Should().NotContain("point");
            markers.Should().OnlyContain(
                marker => marker.MarkerIcon == null,
                "texture resolution is the caller's job"
            );
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFilePreservesFileOrderAndFullIconNames()
    {
        string path = WriteTempMapFile(ReferenceFileLines);

        try
        {
            List<WorldMapGump.WMapMarker> markers = UoamMarkerParser.LoadFile(path, out _);

            markers.Select(marker => marker.MarkerIconName).Should().Equal(
                "teleporter",
                "teleporter",
                "point of interest",
                "point of interest",
                "body of water",
                "minax's fortress",
                "champion",
                "warriors guild"
            );

            markers[0].Name.Should().BeEmpty();
            markers[2].Name.Should().Be("alchemy room");
            markers[6].Name.Should().Be("Champ \"Lord Oaks and the Forest Queen\"");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MalformedLinesAreCountedAndTheRestStillLoad()
    {
        string[] lines =
        [
            "3",
            "+teleporter: 791 1548 3 ",
            "+NoColonHere",
            "-point of interest: 63 1447 3 alchemy room",
            "+rune: abc 200 0 Bad X",
            "+:",
            "+Body of Water: 1496 472 0 Arrow's Point Bay",
        ];

        string path = WriteTempMapFile(lines);

        try
        {
            List<WorldMapGump.WMapMarker> markers = UoamMarkerParser.LoadFile(path, out int skippedLines);

            skippedLines.Should().Be(3);
            markers.Should().HaveCount(3);
            markers.Select(marker => marker.MarkerIconName).Should()
                .Equal("teleporter", "point of interest", "body of water");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A file of nothing but the crash line must load as empty rather than throwing out of the
    /// gump constructor and taking the world map with it.
    /// </summary>
    [Fact]
    public void FileOfOnlyMalformedLinesLoadsEmptyWithoutThrowing()
    {
        string path = WriteTempMapFile(["3", "+NoColonHere", "-AlsoNoColon", "+"]);

        try
        {
            List<WorldMapGump.WMapMarker> markers = [];
            Action load = () => markers = UoamMarkerParser.LoadFile(path, out _);

            load.Should().NotThrow();
            markers.Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EmptyFileLoadsEmpty()
    {
        string path = WriteTempMapFile([]);

        try
        {
            UoamMarkerParser.LoadFile(path, out int skippedLines).Should().BeEmpty();
            skippedLines.Should().Be(0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion
}
