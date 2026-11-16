#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Configuration;

/// <summary>
///     Writes a self-contained HTML report for a <see cref="JsonSaveConflict" /> and opens it in the
///     browser: a field-by-field table plus a GitHub-style unified diff of the two documents. Native
///     message boxes cannot scroll or colour their text, so this is where the full picture lives when
///     the prompt's short summary is not enough.
/// </summary>
internal static class JsonSaveConflictReport
{
    /// <summary>Most diff cells (old lines x new lines) the LCS table may allocate.</summary>
    private const long MAX_DIFF_CELLS = 2_000_000;

    /// <summary>Unchanged lines kept at each end of a collapsed run.</summary>
    private const int CONTEXT_LINES = 3;

    /// <summary>Unchanged runs longer than this are collapsed into a hunk header.</summary>
    private const int COLLAPSE_THRESHOLD = CONTEXT_LINES * 2 + 2;

    // Relaxed escaping keeps quoted values readable in the report; everything is HTML-encoded
    // before it reaches the page, so the generated file is still inert.
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Writes the report to the temp folder and asks the shell to open it.</summary>
    /// <param name="conflict">The conflict to describe.</param>
    internal static void Open(JsonSaveConflict conflict)
    {
        try
        {
            string path = Write(conflict);
            PlatformHelper.LaunchBrowser(path, skipValidation: true);
            Log.Trace($"Save conflict report written to '{path}'.");
        }
        catch (Exception e)
        {
            Log.Warn($"Could not open the full diff for '{conflict.FilePath}': {e.Message}");
        }
    }

    /// <summary>Writes the report to the temp folder and returns where it landed.</summary>
    /// <param name="conflict">The conflict to describe.</param>
    internal static string Write(JsonSaveConflict conflict)
    {
        string path = Path.Combine(Path.GetTempPath(), BuildFileName(conflict));
        FileSystemHelper.WriteAllTextSafe(path, CreateHtml(conflict));

        return path;
    }

    /// <summary>Builds the report document. Kept separate so tests can inspect it without a browser.</summary>
    /// <param name="conflict">The conflict to describe.</param>
    internal static string CreateHtml(JsonSaveConflict conflict)
    {
        string fileName = Path.GetFileName(conflict.FilePath);
        List<DiffRow> rows = BuildDiff(conflict.DiskJson, conflict.LocalJson);
        int added = 0;
        int removed = 0;

        foreach (DiffRow row in rows)
        {
            if (row.Kind == DiffKind.Added)
                added++;
            else if (row.Kind == DiffKind.Removed)
                removed++;
        }

        var html = new StringBuilder(16 * 1024);

        html.Append(
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="color-scheme" content="light dark">
            <title>Save conflict: [FILE]</title>
            <style>
            :root {
              --bg: #ffffff; --fg: #1f2328; --muted: #59636e; --border: #d0d7de;
              --header-bg: #f6f8fa; --add-bg: #e6ffec; --add-num-bg: #ccffd8;
              --add-fg: #1a7f37; --del-bg: #ffebe9; --del-num-bg: #ffd7d5;
              --del-fg: #cf222e; --accent: #0969da;
            }
            @media (prefers-color-scheme: dark) {
              :root {
                --bg: #0d1117; --fg: #e6edf3; --muted: #9198a1; --border: #30363d;
                --header-bg: #151b23; --add-bg: #12261e; --add-num-bg: #1b4721;
                --add-fg: #3fb950; --del-bg: #25171c; --del-num-bg: #4c1e22;
                --del-fg: #f85149; --accent: #4493f8;
              }
            }
            * { box-sizing: border-box; }
            body {
              margin: 0; background: var(--bg); color: var(--fg);
              font: 14px/1.5 -apple-system, BlinkMacSystemFont, "Segoe UI", Noto Sans, Helvetica, Arial, sans-serif;
            }
            main { max-width: 1200px; margin: 0 auto; padding: 24px; }
            h1 { font-size: 22px; margin: 0 0 4px; }
            h2 { font-size: 16px; margin: 28px 0 8px; }
            code, .code, td.num {
              font-family: ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, "Liberation Mono", monospace;
            }
            .subtitle { margin: 0 0 16px; color: var(--muted); }
            dl.meta { display: grid; grid-template-columns: max-content 1fr; gap: 4px 16px; margin: 0 0 16px; font-size: 13px; }
            dl.meta dt { color: var(--muted); }
            dl.meta dd { margin: 0; overflow-wrap: anywhere; }
            .note {
              border-left: 3px solid var(--accent); padding: 8px 12px; margin: 0 0 8px;
              background: var(--header-bg); border-radius: 0 6px 6px 0; font-size: 13px;
            }
            .note p { margin: 0; }
            .note p + p { margin-top: 6px; }
            .legend { display: flex; gap: 16px; flex-wrap: wrap; font-size: 13px; color: var(--muted); margin: 0 0 8px; }
            .legend span { display: inline-flex; align-items: center; gap: 6px; }
            .swatch { width: 12px; height: 12px; border-radius: 3px; display: inline-block; }
            .swatch.del { background: var(--del-num-bg); }
            .swatch.add { background: var(--add-num-bg); }
            table { border-collapse: collapse; width: 100%; }
            table.fields { border: 1px solid var(--border); border-radius: 6px; font-size: 13px; }
            table.fields th, table.fields td {
              text-align: left; padding: 6px 10px; border-bottom: 1px solid var(--border);
              vertical-align: top; overflow-wrap: anywhere;
            }
            table.fields th { background: var(--header-bg); font-weight: 600; }
            table.fields tr:last-child td { border-bottom: 0; }
            table.fields td.on-disk { background: var(--del-bg); }
            table.fields td.in-session { background: var(--add-bg); }
            table.fields td.empty, .empty-state { color: var(--muted); }
            .empty-state { font-style: italic; padding: 12px; }
            .file { border: 1px solid var(--border); border-radius: 6px; overflow: hidden; }
            .file-header {
              background: var(--header-bg); padding: 8px 12px; display: flex;
              justify-content: space-between; gap: 12px; font-size: 13px;
              border-bottom: 1px solid var(--border);
            }
            .stat .add { color: var(--add-fg); }
            .stat .del { color: var(--del-fg); }
            table.diff { font-size: 12px; line-height: 20px; }
            table.diff td { padding: 0 10px; vertical-align: top; }
            table.diff td.num {
              width: 1%; min-width: 44px; text-align: right; color: var(--muted);
              user-select: none; padding: 0 8px;
            }
            table.diff td.sign { width: 1%; user-select: none; text-align: center; padding: 0 4px; }
            table.diff td.code { width: 97%; white-space: pre-wrap; overflow-wrap: anywhere; }
            tr.row.del td.code { background: var(--del-bg); }
            tr.row.del td.num, tr.row.del td.sign { background: var(--del-num-bg); }
            tr.row.add td.code { background: var(--add-bg); }
            tr.row.add td.num, tr.row.add td.sign { background: var(--add-num-bg); }
            tr.row.del td.sign { color: var(--del-fg); }
            tr.row.add td.sign { color: var(--add-fg); }
            tr.hunk td {
              background: var(--header-bg); color: var(--muted); text-align: center;
              font-size: 12px; padding: 2px 10px;
              border-top: 1px solid var(--border); border-bottom: 1px solid var(--border);
            }
            </style>
            </head>
            <body>
            <main>
            """
        );

        html.Append("<h1>Save file conflict</h1>\n<p class=\"subtitle\"><code>")
            .Append(Encode(fileName))
            .Append("</code></p>\n<dl class=\"meta\">")
            .Append("<dt>Full path</dt><dd><code>")
            .Append(Encode(conflict.FilePath))
            .Append("</code></dd>");

        if (conflict.DiskModifiedUtc != default)
        {
            html.Append("<dt>Disk version modified</dt><dd>")
                .Append(Encode(FormatTime(conflict.DiskModifiedUtc)))
                .Append("</dd>");
        }

        html.Append("<dt>Report generated</dt><dd>")
            .Append(Encode(FormatTime(DateTime.UtcNow)))
            .Append("</dd><dt>Differences</dt><dd>")
            .Append(
                Encode(
                    conflict.Changes.Count == 0 && conflict.DiskJson == null
                        ? "Could not be compared"
                        : DescribeCounts(conflict.Changes)
                )
            )
            .Append("</dd></dl>\n");

        html.Append(
            """
            <div class="note">
            <p><strong>Nothing has been changed yet.</strong> Choose a version in the TazUO prompt. In the table
            below, the left column is the file on disk and the right column is this session; in the line diff,
            red lines are only on disk and green lines are only in this session.</p>
            <p>Keeping this session's version moves the disk copy into the backups folder first, so neither
            version is lost.</p>
            </div>
            """
        );

        AppendFieldsTable(html, conflict);
        AppendDiff(html, conflict, rows, added, removed);

        html.Append("\n</main>\n</body>\n</html>\n");

        // The title is the one place the file name appears before it is known to the template.
        return html.ToString().Replace("[FILE]", Encode(fileName), StringComparison.Ordinal);
    }

    /// <summary>Renders every field that differs, with each side in its own column.</summary>
    private static void AppendFieldsTable(StringBuilder html, JsonSaveConflict conflict)
    {
        html.Append("<h2>Fields that differ</h2>\n");

        if (conflict.Changes.Count == 0)
        {
            html.Append("<p class=\"empty-state\">No field-level differences could be computed for this file.</p>\n");
            return;
        }

        html.Append(
            """
            <table class="fields">
            <thead><tr><th>Field</th><th>On disk</th><th>This session</th></tr></thead>
            <tbody>
            """
        );

        foreach (JsonValueChange change in conflict.Changes)
        {
            string disk = change.DiskValue ?? string.Empty;
            string local = change.LocalValue ?? string.Empty;

            html.Append("<tr><td>")
                .Append(Encode(JsonDiff.PathLabel(change.Path)))
                .Append("</td><td class=\"on-disk")
                .Append(disk.Length == 0 ? " empty" : string.Empty)
                .Append("\">")
                .Append(Encode(disk.Length == 0 ? "—" : disk))
                .Append("</td><td class=\"in-session")
                .Append(local.Length == 0 ? " empty" : string.Empty)
                .Append("\">")
                .Append(Encode(local.Length == 0 ? "—" : local))
                .Append("</td></tr>\n");
        }

        html.Append("</tbody></table>\n");
    }

    /// <summary>Renders the unified diff with GitHub-style gutters, colours and collapsed context.</summary>
    private static void AppendDiff(StringBuilder html, JsonSaveConflict conflict, List<DiffRow> rows, int added, int removed)
    {
        html.Append("<h2>JSON diff</h2>\n");

        if (rows.Count == 0)
        {
            html.Append(
                "<p class=\"empty-state\">The two files could not be compared line by line.</p>\n"
            );
            return;
        }

        html.Append(
            """
            <div class="legend">
            <span><span class="swatch del"></span>only on disk</span>
            <span><span class="swatch add"></span>only in this session</span>
            </div>
            """
        );

        html.Append("<div class=\"file\"><div class=\"file-header\"><span><code>")
            .Append(Encode(Path.GetFileName(conflict.FilePath)))
            .Append("</code></span><span class=\"stat\"><span class=\"add\">+")
            .Append(added.ToString(CultureInfo.InvariantCulture))
            .Append("</span> <span class=\"del\">-")
            .Append(removed.ToString(CultureInfo.InvariantCulture))
            .Append("</span></span></div>\n");

        html.Append("<table class=\"diff\"><tbody>\n");

        int index = 0;

        while (index < rows.Count)
        {
            if (rows[index].Kind != DiffKind.Context)
            {
                AppendDiffRow(html, rows[index]);
                index++;
                continue;
            }

            int runStart = index;
            while (index < rows.Count && rows[index].Kind == DiffKind.Context)
                index++;

            int runLength = index - runStart;

            if (runLength > COLLAPSE_THRESHOLD)
            {
                for (int i = 0; i < CONTEXT_LINES; i++)
                    AppendDiffRow(html, rows[runStart + i]);

                DiffRow firstSkipped = rows[runStart + CONTEXT_LINES];
                int skipped = runLength - CONTEXT_LINES * 2;

                html.Append("<tr class=\"hunk\"><td colspan=\"4\">@@ -")
                    .Append(firstSkipped.OldNumber.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(skipped.ToString(CultureInfo.InvariantCulture))
                    .Append(" +")
                    .Append(firstSkipped.NewNumber.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(skipped.ToString(CultureInfo.InvariantCulture))
                    .Append(" @@</td></tr>\n");

                for (int i = runLength - CONTEXT_LINES; i < runLength; i++)
                    AppendDiffRow(html, rows[runStart + i]);
            }
            else
            {
                for (int i = runStart; i < index; i++)
                    AppendDiffRow(html, rows[i]);
            }
        }

        html.Append("</tbody></table></div>\n");
    }

    private static void AppendDiffRow(StringBuilder html, DiffRow row)
    {
        string rowClass;
        string sign;

        switch (row.Kind)
        {
            case DiffKind.Removed:
                rowClass = "del";
                sign = "-";
                break;

            case DiffKind.Added:
                rowClass = "add";
                sign = "+";
                break;

            default:
                rowClass = "ctx";
                sign = string.Empty;
                break;
        }

        html.Append("<tr class=\"row ")
            .Append(rowClass)
            .Append("\"><td class=\"num\">")
            .Append(row.Kind == DiffKind.Added ? string.Empty : row.OldNumber.ToString(CultureInfo.InvariantCulture))
            .Append("</td><td class=\"num\">")
            .Append(row.Kind == DiffKind.Removed ? string.Empty : row.NewNumber.ToString(CultureInfo.InvariantCulture))
            .Append("</td><td class=\"sign\">")
            .Append(sign)
            .Append("</td><td class=\"code\">")
            .Append(Encode(row.Text))
            .Append("</td></tr>\n");
    }

    /// <summary>
    ///     Computes a line diff of the two documents, indented so a setting sits on its own line.
    ///     Both sides are already migrated and stamped by the caller, so a renamed field is not
    ///     reported as a difference.
    /// </summary>
    private static List<DiffRow> BuildDiff(string? diskJson, string? localJson)
    {
        var rows = new List<DiffRow>();

        if (diskJson == null || localJson == null)
            return rows;

        string[] oldLines = SplitLines(Indent(diskJson));
        string[] newLines = SplitLines(Indent(localJson));

        int prefix = 0;
        while (prefix < oldLines.Length && prefix < newLines.Length && oldLines[prefix] == newLines[prefix])
            prefix++;

        int suffix = 0;

        while (
            suffix < oldLines.Length - prefix
            && suffix < newLines.Length - prefix
            && oldLines[^(suffix + 1)] == newLines[^(suffix + 1)]
        )
            suffix++;

        int oldMiddle = oldLines.Length - prefix - suffix;
        int newMiddle = newLines.Length - prefix - suffix;

        int oldNumber = 1;
        int newNumber = 1;

        for (int i = 0; i < prefix; i++)
            rows.Add(new DiffRow(DiffKind.Context, oldLines[i], oldNumber++, newNumber++));

        if ((long)oldMiddle * newMiddle > MAX_DIFF_CELLS)
        {
            // Too large for an LCS table; showing the block replaced is still truthful and cheap.
            for (int i = 0; i < oldMiddle; i++)
                rows.Add(new DiffRow(DiffKind.Removed, oldLines[prefix + i], oldNumber++, 0));

            for (int i = 0; i < newMiddle; i++)
                rows.Add(new DiffRow(DiffKind.Added, newLines[prefix + i], 0, newNumber++));
        }
        else
        {
            AppendMiddleDiff(rows, oldLines, newLines, prefix, oldMiddle, newMiddle, ref oldNumber, ref newNumber);
        }

        for (int i = 0; i < suffix; i++)
        {
            string line = oldLines[oldLines.Length - suffix + i];
            rows.Add(new DiffRow(DiffKind.Context, line, oldNumber++, newNumber++));
        }

        return rows;
    }

    /// <summary>LCS walk over the changed middle, emitting unchanged, removed, then added lines.</summary>
    private static void AppendMiddleDiff(
        List<DiffRow> rows,
        string[] oldLines,
        string[] newLines,
        int offset,
        int oldMiddle,
        int newMiddle,
        ref int oldNumber,
        ref int newNumber
    )
    {
        var lcs = new int[oldMiddle + 1, newMiddle + 1];

        for (int i = oldMiddle - 1; i >= 0; i--)
        {
            for (int j = newMiddle - 1; j >= 0; j--)
            {
                lcs[i, j] = oldLines[offset + i] == newLines[offset + j]
                    ? lcs[i + 1, j + 1] + 1
                    : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        int oldIndex = 0;
        int newIndex = 0;

        while (oldIndex < oldMiddle && newIndex < newMiddle)
        {
            if (oldLines[offset + oldIndex] == newLines[offset + newIndex])
            {
                rows.Add(new DiffRow(DiffKind.Context, oldLines[offset + oldIndex], oldNumber++, newNumber++));
                oldIndex++;
                newIndex++;
            }
            else if (lcs[oldIndex + 1, newIndex] >= lcs[oldIndex, newIndex + 1])
            {
                rows.Add(new DiffRow(DiffKind.Removed, oldLines[offset + oldIndex], oldNumber++, 0));
                oldIndex++;
            }
            else
            {
                rows.Add(new DiffRow(DiffKind.Added, newLines[offset + newIndex], 0, newNumber++));
                newIndex++;
            }
        }

        while (oldIndex < oldMiddle)
        {
            rows.Add(new DiffRow(DiffKind.Removed, oldLines[offset + oldIndex], oldNumber++, 0));
            oldIndex++;
        }

        while (newIndex < newMiddle)
        {
            rows.Add(new DiffRow(DiffKind.Added, newLines[offset + newIndex], 0, newNumber++));
            newIndex++;
        }
    }

    /// <summary>Formats the document so each property sits on its own line, when it parses.</summary>
    private static string Indent(string json)
    {
        try
        {
            JsonNode? node = JsonNode.Parse(json);

            return node?.ToJsonString(IndentedOptions) ?? json;
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static string[] SplitLines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    private static string BuildFileName(JsonSaveConflict conflict)
    {
        string stem = Path.GetFileNameWithoutExtension(conflict.FilePath);

        foreach (char invalid in Path.GetInvalidFileNameChars())
            stem = stem.Replace(invalid, '_');

        return $"TazUO_SaveConflict_{stem}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
    }

    private static string FormatTime(DateTime utc) =>
        utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);

    private static string DescribeCounts(IReadOnlyList<JsonValueChange> changes)
    {
        int added = 0;
        int removed = 0;
        int changed = 0;

        foreach (JsonValueChange change in changes)
        {
            switch (change.Kind)
            {
                case JsonChangeKind.Added:
                    added++;
                    break;

                case JsonChangeKind.Removed:
                    removed++;
                    break;

                default:
                    changed++;
                    break;
            }
        }

        return string.Format(
            CultureInfo.CurrentCulture,
            "{0} fields differ ({1} changed, {2} only on disk, {3} only in this session)",
            changes.Count,
            changed,
            removed,
            added
        );
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private enum DiffKind
    {
        Context,
        Removed,
        Added,
    }

    private readonly record struct DiffRow(DiffKind Kind, string Text, int OldNumber, int NewNumber);
}
