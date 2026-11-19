#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using ClassicUO.Game.Managers;
using SDL3;

namespace ClassicUO.Configuration;

/// <summary>
///     Answers a <see cref="JsonSaveConflict"/> with a native SDL message box. Being a native modal,
///     it works with the game loop stopped and the game controller torn down, so a conflict raised
///     while the client is exiting can still be answered instead of silently keeping one copy.
/// </summary>
public static class JsonSaveConflictDialog
{
    /// <summary>Button that leaves the on-disk file as it is.</summary>
    private const int KEEP_DISK_BUTTON = 0;

    /// <summary>Button that overwrites the on-disk file with this client's version.</summary>
    private const int KEEP_SESSION_BUTTON = 1;

    /// <summary>Button that opens the full report, then returns to the prompt.</summary>
    private const int VIEW_DIFF_BUTTON = 2;

    /// <summary>
    ///     Enables the prompt. Call once during startup.
    /// </summary>
    public static void Register() => JsonSaveConflictHandler.Prompt = Show;

    private static void Show(JsonSaveConflict conflict)
    {
        // A save can originate off the main thread; the message box must not.
        bool keepMine = MainThreadQueue.BubblingInvokeOnMainThread(() => Ask(conflict));
        conflict.Resolve(keepMine);
    }

    /// <summary>
    ///     Shows the box and returns true when the client's own version was chosen. "View diff"
    ///     opens the full report and raises the same question again, so the report never answers it.
    /// </summary>
    private static unsafe bool Ask(JsonSaveConflict conflict)
    {
        while (true)
        {
            int choice = ShowBox(conflict);

            if (choice == VIEW_DIFF_BUTTON)
            {
                JsonSaveConflictReport.Open(conflict);
                continue;
            }

            return choice == KEEP_SESSION_BUTTON;
        }
    }

    /// <summary>Shows the box once and returns the button that was pressed.</summary>
    private static unsafe int ShowBox(JsonSaveConflict conflict)
    {
        string title = TazLang.Get("json_save_conflict_title", "File changed on disk");
        string message = BuildMessage(conflict);
        string keepDisk = TazLang.Get("json_save_conflict_keep_disk_btn", "Keep disk");
        string keepSession = TazLang.Get("json_save_conflict_keep_session_btn", "Keep session");
        string viewDiff = TazLang.Get("json_save_conflict_view_diff_btn", "View diff");

        byte[] titleUtf8 = Encoding.UTF8.GetBytes(title + '\0');
        byte[] messageUtf8 = Encoding.UTF8.GetBytes(message + '\0');
        byte[] keepDiskUtf8 = Encoding.UTF8.GetBytes(keepDisk + '\0');
        byte[] keepSessionUtf8 = Encoding.UTF8.GetBytes(keepSession + '\0');
        byte[] viewDiffUtf8 = Encoding.UTF8.GetBytes(viewDiff + '\0');

        fixed (byte* titlePtr = titleUtf8)
        fixed (byte* messagePtr = messageUtf8)
        fixed (byte* keepDiskPtr = keepDiskUtf8)
        fixed (byte* keepSessionPtr = keepSessionUtf8)
        fixed (byte* viewDiffPtr = viewDiffUtf8)
        {
            var buttons = stackalloc SDL.SDL_MessageBoxButtonData[3];

            // Disk wins on Enter and Escape; overwriting it must be a deliberate click.
            buttons[KEEP_DISK_BUTTON] = new SDL.SDL_MessageBoxButtonData
            {
                flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT
                    | SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_ESCAPEKEY_DEFAULT,
                buttonID = KEEP_DISK_BUTTON,
                text = keepDiskPtr
            };
            buttons[KEEP_SESSION_BUTTON] = new SDL.SDL_MessageBoxButtonData
            {
                flags = 0,
                buttonID = KEEP_SESSION_BUTTON,
                text = keepSessionPtr
            };
            buttons[VIEW_DIFF_BUTTON] = new SDL.SDL_MessageBoxButtonData
            {
                flags = 0,
                buttonID = VIEW_DIFF_BUTTON,
                text = viewDiffPtr
            };

            var data = new SDL.SDL_MessageBoxData
            {
                flags = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
                window = IntPtr.Zero,
                title = titlePtr,
                message = messagePtr,
                numbuttons = 3,
                buttons = buttons,
                colorScheme = null
            };

            if (SDL.SDL_ShowMessageBox(ref data, out int buttonId))
                return buttonId;

            // The box could not be shown; keep the disk version rather than risk a silent clobber.
            return KEEP_DISK_BUTTON;
        }
    }

    /// <summary>Most changes listed before the rest are summarised as a count.</summary>
    private const int MAX_CHANGES_SHOWN = 10;

    /// <summary>Longest a single change line may be, so a deep path cannot widen the box.</summary>
    private const int MAX_CHANGE_LINE_LENGTH = 120;

    /// <summary>Longest the field label inside a change line may be, leaving room for both values.</summary>
    private const int MAX_PATH_LENGTH = 48;

    /// <summary>
    ///     Longest a single value may be in the prompt. Both sides are budgeted separately so a long
    ///     value cannot push the other side off the line; the report shows the values in full.
    /// </summary>
    private const int MAX_VALUE_LENGTH = 28;

    /// <summary>
    ///     Total characters the listed changes may occupy. Together with the line and count caps
    ///     this keeps the box within the screen, which native message boxes cannot scroll.
    /// </summary>
    private const int MAX_CHANGE_BLOCK_LENGTH = 1200;

    /// <summary>Builds the prompt text, listing what differs so the choice is an informed one.</summary>
    internal static string BuildMessage(JsonSaveConflict conflict)
    {
        var builder = new StringBuilder();

        builder.Append(
            TazLang.GetEx(
                "json_save_conflict_intro",
                "The file \"{0}\" was changed on disk after this client loaded it.",
                new[] { Path.GetFileName(conflict.FilePath) }
            )
        );

        if (conflict.DiskModifiedUtc != default)
        {
            builder.Append('\n');
            builder.Append(
                TazLang.GetEx(
                    "json_save_conflict_disk_modified",
                    "Disk version modified: {0}",
                    new[] { FormatTime(conflict.DiskModifiedUtc) }
                )
            );
        }

        if (conflict.Changes.Count > 0)
        {
            builder.Append("\n\n");
            builder.Append(
                TazLang.GetEx(
                    "json_save_conflict_summary",
                    "{0} differences: {1} changed, {2} only on disk, {3} only in this session.",
                    new[]
                    {
                        conflict.Changes.Count.ToString(CultureInfo.CurrentCulture),
                        Count(conflict, JsonChangeKind.Changed),
                        Count(conflict, JsonChangeKind.Removed),
                        Count(conflict, JsonChangeKind.Added)
                    }
                )
            );

            int listed = 0;
            int blockLength = 0;

            while (listed < conflict.Changes.Count && listed < MAX_CHANGES_SHOWN)
            {
                string line = TruncateMiddle(
                    FormatChange(conflict.Changes[listed]),
                    MAX_CHANGE_LINE_LENGTH
                );

                if (blockLength + line.Length > MAX_CHANGE_BLOCK_LENGTH)
                    break;

                builder.Append("\n  ");
                builder.Append(line);
                blockLength += line.Length;
                listed++;
            }

            int omitted = conflict.Changes.Count - listed;

            if (omitted > 0)
            {
                builder.Append('\n');
                builder.Append(
                    TazLang.GetEx(
                        "json_save_conflict_more_changes",
                        "... and {0} more. Use View diff to see them all.",
                        new[] { omitted.ToString(CultureInfo.CurrentCulture) }
                    )
                );
            }
        }

        builder.Append("\n\n");
        builder.Append(
            TazLang.Get(
                "json_save_conflict_keep_note",
                "Choosing the session version keeps the disk copy in the backups folder."
            )
        );

        builder.Append("\n\n");
        builder.Append(
            TazLang.Get("json_save_conflict_question", "Keep this session's version, or the version on disk?")
        );

        return builder.ToString();
    }

    /// <summary>
    ///     Renders one change as a single line. Each line names its own sides so the reader does not
    ///     have to remember a header, and the symbols a diff tool would use are left to the report.
    /// </summary>
    private static string FormatChange(JsonValueChange change)
    {
        string path = TruncateMiddle(JsonDiff.PathLabel(change.Path), MAX_PATH_LENGTH);

        return change.Kind switch
        {
            JsonChangeKind.Added => TazLang.GetEx(
                "json_save_conflict_session_line",
                "{0}: only in session ({1})",
                new[] { path, ShortValue(change.LocalValue) }
            ),
            JsonChangeKind.Removed => TazLang.GetEx(
                "json_save_conflict_disk_line",
                "{0}: only on disk ({1})",
                new[] { path, ShortValue(change.DiskValue) }
            ),
            _ => TazLang.GetEx(
                "json_save_conflict_changed_line",
                "{0}: disk {1} → session {2}",
                new[] { path, ShortValue(change.DiskValue), ShortValue(change.LocalValue) }
            )
        };
    }

    private static string ShortValue(string? value) =>
        TruncateMiddle(value ?? "null", MAX_VALUE_LENGTH);

    private static string Count(JsonSaveConflict conflict, JsonChangeKind kind)
    {
        int count = 0;

        foreach (JsonValueChange change in conflict.Changes)
        {
            if (change.Kind == kind)
                count++;
        }

        return count.ToString(CultureInfo.CurrentCulture);
    }

    private static string FormatTime(DateTime utc) =>
        utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);

    /// <summary>
    ///     Cuts a line down to <paramref name="maxLength" /> by dropping its middle, so a long path
    ///     shortens without taking the values this session's side is compared against with it.
    /// </summary>
    private static string TruncateMiddle(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        int keep = maxLength - 1;
        int head = keep / 2;
        int tail = keep - head;

        return string.Concat(text.AsSpan(0, head), "…", text.AsSpan(text.Length - tail));
    }
}
