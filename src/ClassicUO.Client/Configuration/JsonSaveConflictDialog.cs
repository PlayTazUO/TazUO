#nullable enable
using System;
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

    /// <summary>Shows the box and returns true when the client's own version was chosen.</summary>
    private static unsafe bool Ask(JsonSaveConflict conflict)
    {
        string title = TazLang.Get("json_save_conflict_title", "File changed on disk");
        string message = BuildMessage(conflict);
        string keepDisk = TazLang.Get("json_save_conflict_keep_disk_btn", "Keep disk");
        string keepMine = TazLang.Get("json_save_conflict_keep_mine_btn", "Keep mine");

        byte[] titleUtf8 = Encoding.UTF8.GetBytes(title + '\0');
        byte[] messageUtf8 = Encoding.UTF8.GetBytes(message + '\0');
        byte[] keepDiskUtf8 = Encoding.UTF8.GetBytes(keepDisk + '\0');
        byte[] keepMineUtf8 = Encoding.UTF8.GetBytes(keepMine + '\0');

        fixed (byte* titlePtr = titleUtf8)
        fixed (byte* messagePtr = messageUtf8)
        fixed (byte* keepDiskPtr = keepDiskUtf8)
        fixed (byte* keepMinePtr = keepMineUtf8)
        {
            var buttons = stackalloc SDL.SDL_MessageBoxButtonData[2];

            // Disk wins on Enter and Escape; overwriting it must be a deliberate click.
            buttons[0] = new SDL.SDL_MessageBoxButtonData
            {
                flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT
                    | SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_ESCAPEKEY_DEFAULT,
                buttonID = 0,
                text = keepDiskPtr
            };
            buttons[1] = new SDL.SDL_MessageBoxButtonData
            {
                flags = 0,
                buttonID = 1,
                text = keepMinePtr
            };

            var data = new SDL.SDL_MessageBoxData
            {
                flags = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
                window = IntPtr.Zero,
                title = titlePtr,
                message = messagePtr,
                numbuttons = 2,
                buttons = buttons,
                colorScheme = null
            };

            if (SDL.SDL_ShowMessageBox(ref data, out int buttonId))
                return buttonId == 1;
        }

        // The box could not be shown; keep the disk version rather than risk a silent clobber.
        return false;
    }

    /// <summary>Most changes listed before the rest are summarised as a count.</summary>
    private const int MAX_CHANGES_SHOWN = 10;

    /// <summary>Longest a single change line may be, so a deep path cannot widen the box.</summary>
    private const int MAX_CHANGE_LINE_LENGTH = 100;

    /// <summary>
    ///     Total characters the listed changes may occupy. Together with the line and count caps
    ///     this keeps the box within the screen, which native message boxes cannot scroll.
    /// </summary>
    private const int MAX_CHANGE_BLOCK_LENGTH = 1000;

    /// <summary>Builds the prompt text, listing what differs so the choice is an informed one.</summary>
    private static string BuildMessage(JsonSaveConflict conflict)
    {
        var builder = new StringBuilder();

        builder.Append(
            string.Format(
                TazLang.Get(
                    "json_save_conflict_intro",
                    "The file \"{0}\" was changed on disk after this client loaded it."
                ),
                Path.GetFileName(conflict.FilePath)
            )
        );

        if (conflict.Changes.Count > 0)
        {
            builder.Append("\n\n");
            builder.Append(TazLang.Get("json_save_conflict_changes_header", "Changes (disk -> this client):"));

            int listed = 0;
            int blockLength = 0;

            while (listed < conflict.Changes.Count && listed < MAX_CHANGES_SHOWN)
            {
                string line = Truncate(FormatChange(conflict.Changes[listed]), MAX_CHANGE_LINE_LENGTH);

                if (blockLength + line.Length > MAX_CHANGE_BLOCK_LENGTH)
                    break;

                builder.Append('\n');
                builder.Append(line);
                blockLength += line.Length;
                listed++;
            }

            int omitted = conflict.Changes.Count - listed;

            if (omitted > 0)
            {
                builder.Append('\n');
                builder.Append(
                    string.Format(TazLang.Get("json_save_conflict_more_changes", "... and {0} more"), omitted)
                );
            }
        }

        builder.Append("\n\n");
        builder.Append(TazLang.Get("json_save_conflict_question", "Keep this client's version, or the version on disk?"));

        return builder.ToString();
    }

    /// <summary>Renders one change as a single line; symbols keep the list language-neutral.</summary>
    private static string FormatChange(JsonValueChange change) => change.Kind switch
    {
        JsonChangeKind.Added => $"+ {change.Path} = {change.LocalValue}",
        JsonChangeKind.Removed => $"- {change.Path} = {change.DiskValue}",
        _ => $"~ {change.Path}: {change.DiskValue} -> {change.LocalValue}"
    };

    /// <summary>Cuts a line down to <paramref name="maxLength" /> so one long entry cannot widen the box.</summary>
    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 1), "…");
}
