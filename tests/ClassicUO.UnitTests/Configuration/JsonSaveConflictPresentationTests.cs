using System;
using System.Globalization;
using System.Linq;
using ClassicUO.Configuration;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

/// <summary>
///     Covers the two places a save conflict is explained to the user: the short native prompt and
///     the full HTML report. Both must name which side a value belongs to without the reader having
///     to remember a header, and the report must render a GitHub-style line diff around them.
/// </summary>
public class JsonSaveConflictPresentationTests
{
    [Fact]
    public void Prompt_Labels_Each_Side_On_Its_Own_Line()
    {
        JsonSaveConflict conflict = Conflict(
            """{"window_position":{"x":120,"y":80}}""",
            """{"window_position":{"x":240,"y":96}}""",
            new JsonValueChange("$.window_position", JsonChangeKind.Changed, "{\"x\":120,\"y\":80}", "{\"x\":240,\"y\":96}"),
            new JsonValueChange("$.grid_containers_open", JsonChangeKind.Added, null, "true"),
            new JsonValueChange("$.hotkeys[2].key", JsonChangeKind.Removed, "\"F1\"", null)
        );

        string message = JsonSaveConflictDialog.BuildMessage(conflict);

        message.Should().Contain("window position: disk");
        message.Should().Contain("session");
        message.Should().Contain("grid containers open: only in session (true)");
        message.Should().Contain("hotkeys #3 › key: only on disk (\"F1\")");
        message.Should().NotContain("$.window_position");
        message.Should().NotContain("disk ->");
        message.Should().NotContain("~ ");
    }

    [Fact]
    public void Prompt_Shows_When_The_Disk_File_Was_Modified()
    {
        JsonSaveConflict conflict = Conflict(
            "{}",
            "{}",
            new DateTime(2026, 11, 16, 14, 32, 0, DateTimeKind.Utc),
            Array.Empty<JsonValueChange>()
        );

        string message = JsonSaveConflictDialog.BuildMessage(conflict);

        // The exact text depends on the machine's time zone; the date must still be there.
        DateTime local = new DateTime(2026, 11, 16, 14, 32, 0, DateTimeKind.Utc).ToLocalTime();
        message.Should().Contain(local.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Report_Renders_Field_Table_And_GitHub_Style_Diff()
    {
        JsonSaveConflict conflict = Conflict(
            """{"volume":5,"name":"disk"}""",
            """{"volume":9,"name":"session","new":true}""",
            new JsonValueChange("$.volume", JsonChangeKind.Changed, "5", "9"),
            new JsonValueChange("$.name", JsonChangeKind.Changed, "\"disk\"", "\"session\""),
            new JsonValueChange("$.new", JsonChangeKind.Added, null, "true")
        );

        string html = JsonSaveConflictReport.CreateHtml(conflict);

        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("Save file conflict");
        html.Should().Contain("profile.json");
        html.Should().Contain("Fields that differ");
        html.Should().Contain("<td>volume</td>");
        html.Should().Contain("class=\"row del\"");
        html.Should().Contain("class=\"row add\"");
        html.Should().Contain("prefers-color-scheme: dark");
    }

    [Fact]
    public void Report_Collapses_Long_Unchanged_Runs_Into_Hunk_Headers()
    {
        string disk = "{" + string.Join(",", Enumerable.Range(0, 40).Select(i => $"\"k{i}\":{i}")) + "}";
        string local = "{" + string.Join(",", Enumerable.Range(0, 40).Select(i => $"\"k{i}\":{(i == 20 ? 999 : i)}")) + "}";

        JsonSaveConflict conflict = Conflict(
            disk,
            local,
            new JsonValueChange("$.k20", JsonChangeKind.Changed, "20", "999")
        );

        string html = JsonSaveConflictReport.CreateHtml(conflict);

        html.Should().Contain("class=\"hunk\"");
        html.Should().Contain("@@ -");
    }

    [Fact]
    public void Report_Encodes_Values_So_A_File_Cannot_Inject_Markup()
    {
        JsonSaveConflict conflict = Conflict(
            """{"label":"<script>alert(1)</script>"}""",
            """{"label":"<b>&</b>"}""",
            new JsonValueChange("$.label", JsonChangeKind.Changed, "\"<script>alert(1)</script>\"", "\"<b>&</b>\"")
        );

        string html = JsonSaveConflictReport.CreateHtml(conflict);

        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;");
        html.Should().Contain("&amp;");
    }

    [Fact]
    public void Report_Without_Comparable_Documents_Explains_Instead_Of_Throwing()
    {
        var conflict = new JsonSaveConflict(
            "/tmp/profile.json",
            new DateTime(2026, 11, 16, 14, 32, 0, DateTimeKind.Utc),
            Array.Empty<JsonValueChange>(),
            diskJson: null,
            localJson: null,
            resolve: _ => { }
        );

        string html = JsonSaveConflictReport.CreateHtml(conflict);

        html.Should().Contain("could not be compared");
        html.Should().Contain("No field-level differences could be computed");
    }

    private static JsonSaveConflict Conflict(string diskJson, string localJson, params JsonValueChange[] changes) =>
        Conflict(diskJson, localJson, new DateTime(2026, 11, 16, 14, 32, 0, DateTimeKind.Utc), changes);

    private static JsonSaveConflict Conflict(string diskJson, string localJson, DateTime diskModifiedUtc, params JsonValueChange[] changes) =>
        new(
            "/tmp/profile.json",
            diskModifiedUtc,
            changes,
            diskJson,
            localJson,
            _ => { }
        );
}
