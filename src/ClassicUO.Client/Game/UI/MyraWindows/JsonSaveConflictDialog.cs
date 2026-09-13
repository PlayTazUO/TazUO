#nullable enable
using System;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
///     Shows the save-conflict prompt for <see cref="JsonSaveConflictHandler"/> and wires it up at
///     startup, keeping the "which copy wins" UI out of the persistence layer.
/// </summary>
public static class JsonSaveConflictDialog
{
    /// <summary>
    ///     Enables the prompt. Call once on the main thread during startup.
    /// </summary>
    public static void Register() => JsonSaveConflictHandler.Prompt = Show;

    private static void Show(JsonSaveConflict conflict)
    {
        // A save can originate off the main thread; the modal must not.
        MainThreadQueue.InvokeOnMainThread(() => ShowOnMainThread(conflict));
    }

    private static void ShowOnMainThread(JsonSaveConflict conflict)
    {
        string fileName = Path.GetFileName(conflict.FilePath);
        string question = TazLang.Get(
            "json_save_conflict_message",
            "The file \"{0}\" was changed on disk after this client loaded it.\n\n"
            + "Keep this client's version, or the version on disk?"
        );

        new ConfirmationModal(
            TazLang.Get("json_save_conflict_title", "File changed on disk"),
            string.Format(question, fileName),
            overwriteLocal => conflict.Resolve(overwriteLocal),
            TazLang.Get("json_save_conflict_keep_mine", "Keep this client's version"),
            TazLang.Get("json_save_conflict_keep_disk", "Keep disk version")
        );
    }
}
