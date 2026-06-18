using System.Linq;
using ClassicUO.LegionScripting;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.LegionScript;

public class ApiHousingPlacementTests
{
    [Fact]
    public void BuildOfficialHousePlacementAreas_SouthFacing_UsesExpectedAtlanticClearanceBands()
    {
        var areas = LegionAPI.BuildOfficialHousePlacementAreas(100, 200, 3, 2, "south", 6, 5, 1, true).ToArray();

        areas.Should().HaveCount(5);
        areas[0].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "step" && a.MinX == 100 && a.MaxX == 102 && a.MinY == 202 && a.MaxY == 202 && a.RequiresBuildableLand);
        areas[1].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "front" && a.MinX == 100 && a.MaxX == 102 && a.MinY == 203 && a.MaxY == 207 && !a.RequiresBuildableLand);
        areas[2].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "back" && a.MinX == 100 && a.MaxX == 102 && a.MinY == 195 && a.MaxY == 199);
        areas[3].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "left" && a.MinX == 103 && a.MaxX == 103 && a.MinY == 200 && a.MaxY == 201);
        areas[4].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "right" && a.MinX == 99 && a.MaxX == 99 && a.MinY == 200 && a.MaxY == 201);
    }

    [Fact]
    public void BuildOfficialHousePlacementAreas_EastFacing_RotatesFrontBackAndSides()
    {
        var areas = LegionAPI.BuildOfficialHousePlacementAreas(10, 20, 4, 3, "east", 6, 5, 1, true).ToArray();

        areas.Should().HaveCount(5);
        areas[0].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "step" && a.MinX == 14 && a.MaxX == 14 && a.MinY == 20 && a.MaxY == 22);
        areas[1].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "front" && a.MinX == 15 && a.MaxX == 19 && a.MinY == 20 && a.MaxY == 22);
        areas[2].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "back" && a.MinX == 5 && a.MaxX == 9 && a.MinY == 20 && a.MaxY == 22);
        areas[3].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "left" && a.MinX == 10 && a.MaxX == 13 && a.MinY == 19 && a.MaxY == 19);
        areas[4].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.Name == "right" && a.MinX == 10 && a.MaxX == 13 && a.MinY == 23 && a.MaxY == 23);
    }

    [Fact]
    public void BuildOfficialHousePlacementAreas_WhenStepsDisabled_UsesSingleFrontBand()
    {
        var areas = LegionAPI.BuildOfficialHousePlacementAreas(100, 200, 3, 2, "south", 6, 5, 1, false).ToArray();

        areas.Select(a => a.Name).Should().Equal("front", "back", "left", "right");
        areas[0].Should().Match<LegionAPI.HousePlacementArea>(a =>
            a.MinX == 100 && a.MaxX == 102 && a.MinY == 202 && a.MaxY == 207 && !a.RequiresBuildableLand);
    }

    [Theory]
    [InlineData("dirt road", true)]
    [InlineData("paved road", true)]
    [InlineData("cobblestone", true)]
    [InlineData("grass", false)]
    public void IsRoadCandidate_UsesConservativeLandTileNames(string name, bool expected)
    {
        LegionAPI.IsRoadCandidate(0, name, 0).Should().Be(expected);
    }
}
