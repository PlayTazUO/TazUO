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
        string message = string.Format(
            TazLang.Get(
                "json_save_conflict_message",
                "The file \"{0}\" was changed on disk after this client loaded it.\n\n"
                + "Keep this client's version, or the version on disk?"
            ),
            Path.GetFileName(conflict.FilePath)
        );
        string keepDisk = TazLang.Get("json_save_conflict_keep_disk", "Keep disk version");
        string keepMine = TazLang.Get("json_save_conflict_keep_mine", "Keep this client's version");

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
}
