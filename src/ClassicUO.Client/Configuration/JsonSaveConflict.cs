#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Configuration;

/// <summary>
///     A save whose file was changed on disk after this instance last loaded or wrote it. The UI
///     layer decides which copy survives by calling <see cref="Resolve"/>; nothing is written until
///     then, so the on-disk version is never lost by accident.
/// </summary>
public sealed class JsonSaveConflict
{
    private readonly Action<bool> _resolve;

    internal JsonSaveConflict(string filePath, DateTime diskModifiedUtc, Action<bool> resolve)
    {
        FilePath = filePath;
        DiskModifiedUtc = diskModifiedUtc;
        _resolve = resolve;
    }

    /// <summary>Full path of the file that changed on disk.</summary>
    public string FilePath { get; }

    /// <summary>When the on-disk file was last written, for the prompt's message.</summary>
    public DateTime DiskModifiedUtc { get; }

    /// <summary>
    ///     Answers the conflict. <c>true</c> overwrites the disk file with this instance's version,
    ///     <c>false</c> leaves the disk file as it is.
    /// </summary>
    public void Resolve(bool overwriteLocal)
    {
        JsonSaveConflictHandler.Complete(this);
        _resolve(overwriteLocal);
    }
}

/// <summary>
///     Routes external-change conflicts raised by <see cref="JsonSave{T}"/> to a UI prompt. The UI
///     layer sets <see cref="Prompt"/> once at startup; with no prompt registered the disk file is
///     left alone, so a headless or not-yet-initialized client can never clobber it.
/// </summary>
public static class JsonSaveConflictHandler
{
    private static readonly Dictionary<string, JsonSaveConflict> PendingConflicts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Shows the "which version do you want to keep" dialog. Set by the UI layer on the main
    ///     thread. When null, conflicts keep the on-disk version.
    /// </summary>
    public static Action<JsonSaveConflict>? Prompt { get; set; }

    /// <summary>
    ///     Whether any conflict is still waiting on an answer. A caller about to end the process
    ///     uses this to keep the window alive until the user has decided.
    /// </summary>
    public static bool HasPendingConflicts
    {
        get
        {
            lock (PendingConflicts)
                return PendingConflicts.Count > 0;
        }
    }

    /// <summary>
    ///     Hands <paramref name="conflict"/> to the prompt unless a prompt for the same file is
    ///     already open. Returns <c>false</c> when there is no prompt to show.
    /// </summary>
    internal static bool Request(JsonSaveConflict conflict)
    {
        if (Prompt == null)
            return false;

        lock (PendingConflicts)
            if (!PendingConflicts.TryAdd(conflict.FilePath, conflict))
                return true; // A prompt for this file is already open.

        Prompt(conflict);
        return true;
    }

    /// <summary>
    ///     Re-shows every conflict still awaiting an answer. Used when UI teardown discarded the
    ///     original prompt, so the pending question is not lost with the window it was drawn in.
    /// </summary>
    public static void ResurfacePending()
    {
        if (Prompt == null)
            return;

        JsonSaveConflict[] pending;

        lock (PendingConflicts)
            pending = PendingConflicts.Values.ToArray();

        foreach (JsonSaveConflict conflict in pending)
            Prompt(conflict);
    }

    internal static void Complete(JsonSaveConflict conflict)
    {
        lock (PendingConflicts)
            PendingConflicts.Remove(conflict.FilePath);
    }
}
