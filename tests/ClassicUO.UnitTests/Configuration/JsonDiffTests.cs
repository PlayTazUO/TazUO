using System.Text.Json.Nodes;
using ClassicUO.Configuration;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>
///     Covers the structural comparison behind the save-conflict prompt: the user should be told
///     which fields differ, at the path they differ, not merely that the file changed.
/// </summary>
public class JsonDiffTests
{
    [Fact]
    public void Identical_Documents_Produce_No_Changes()
    {
        JsonNode disk = JsonNode.Parse("""{"a":1,"b":[1,2]}""");
        JsonNode local = JsonNode.Parse("""{"b":[1,2],"a":1}""");

        JsonDiff.Compare(disk, local).Should().BeEmpty();
    }

    [Fact]
    public void A_Changed_Leaf_Is_Reported_At_Its_Path()
    {
        JsonNode disk = JsonNode.Parse("""{"settings":{"volume":5}}""");
        JsonNode local = JsonNode.Parse("""{"settings":{"volume":9}}""");

        JsonDiff.Compare(disk, local)
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(new JsonValueChange("$.settings.volume", JsonChangeKind.Changed, "5", "9"));
    }

    [Fact]
    public void Added_And_Removed_Keys_Are_Reported()
    {
        JsonNode disk = JsonNode.Parse("""{"kept":1,"removed":2}""");
        JsonNode local = JsonNode.Parse("""{"kept":1,"added":3}""");

        JsonDiff.Compare(disk, local)
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    new JsonValueChange("$.removed", JsonChangeKind.Removed, "2", null),
                    new JsonValueChange("$.added", JsonChangeKind.Added, null, "3"),
                }
            );
    }

    [Fact]
    public void Array_Length_Changes_Are_Reported_By_Index()
    {
        JsonNode disk = JsonNode.Parse("""{"list":[1,2,3]}""");
        JsonNode local = JsonNode.Parse("""{"list":[1,2]}""");

        JsonDiff.Compare(disk, local)
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(new JsonValueChange("$.list[2]", JsonChangeKind.Removed, "3", null));
    }

    [Fact]
    public void A_Type_Change_Is_Reported_Whole()
    {
        JsonNode disk = JsonNode.Parse("""{"v":1}""");
        JsonNode local = JsonNode.Parse("""{"v":"1"}""");

        JsonDiff.Compare(disk, local)
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be(new JsonValueChange("$.v", JsonChangeKind.Changed, "1", "\"1\""));
    }

    [Fact]
    public void Long_Values_Are_Truncated()
    {
        string longText = new('x', 200);
        JsonNode disk = JsonNode.Parse("""{"v":"short"}""");
        JsonNode local = JsonNode.Parse($$"""{"v":"{{longText}}"}""");

        JsonDiff.Compare(disk, local)
            .Should()
            .ContainSingle()
            .Which.LocalValue.Should()
            .Be("\"" + new string('x', 49) + "…");
    }
}
