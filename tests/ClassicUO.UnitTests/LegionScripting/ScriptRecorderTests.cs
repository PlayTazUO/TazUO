#nullable enable

using ClassicUO.LegionScripting;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.LegionScripting;

public class ScriptRecorderTests
{
    [Theory]
    [InlineData(100, 100, 100, 101, false, "API.Walk(\"south\")")]
    [InlineData(100, 100, 101, 100, false, "API.Walk(\"east\")")]
    [InlineData(100, 100, 100, 99, false, "API.Walk(\"north\")")]
    [InlineData(100, 100, 99, 100, false, "API.Walk(\"west\")")]
    [InlineData(100, 100, 101, 101, true, "API.Run(\"southeast\")")]
    public void RecordsDirectionFromActualMovement(int startX, int startY, int endX, int endY, bool run, string expected)
    {
        ScriptRecorder recorder = ScriptRecorder.Instance;
        recorder.StartRecording();

        try
        {
            recorder.UpdatePlayerPosition(startX, startY, false);
            recorder.UpdatePlayerPosition(endX, endY, run);

            recorder.GenerateScript(false).Should().Contain(expected);
        }
        finally
        {
            recorder.StopRecording();
        }
    }

    [Fact]
    public void DoesNotRecordTeleportAsWalking()
    {
        ScriptRecorder recorder = ScriptRecorder.Instance;
        recorder.StartRecording();

        try
        {
            recorder.UpdatePlayerPosition(100, 100, false);
            recorder.UpdatePlayerPosition(140, 160, false);

            recorder.GenerateScript(false).Should().NotContain("API.Walk");
        }
        finally
        {
            recorder.StopRecording();
        }
    }
}
