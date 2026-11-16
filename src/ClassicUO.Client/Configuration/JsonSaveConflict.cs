#nullable enable

using System;
using System.Collections.Generic;

namespace ClassicUO.Configuration;

/// <summary>
///     A save whose file was changed on disk after this instance last loaded or wrote it. The UI
///     layer decides which copy survives by calling <see cref="Resolve"/>; nothing is written until
///     then, so the on-disk version is never lost by accident.
/// </summary>
public sealed class JsonSaveConflict
{
    private readonly Action<bool> _resolve;

    internal JsonSaveConflict(
        string filePath,
        DateTime diskModifiedUtc,
        IReadOnlyList<JsonValueChange> changes,
        string? diskJson,
        string? localJson,
        Action<bool> resolve
    )
    {
        FilePath = filePath;
        DiskModifiedUtc = diskModifiedUtc;
        Changes = changes;
        DiskJson = diskJson;
        LocalJson = localJson;
        _resolve = resolve;
    }

    /// <summary>Full path of the file that changed on disk.</summary>
    public string FilePath { get; }

    /// <summary>When the on-disk file was last written, for the prompt's message.</summary>
    public DateTime DiskModifiedUtc { get; }

    /// <summary>
    ///     What differs between the on-disk file and this client's version, so the prompt can show
    ///     the user what is at stake. Empty when the two could not be compared - the question is
    ///     still answerable without it.
    /// </summary>
    public IReadOnlyList<JsonValueChange> Changes { get; }

    /// <summary>
    ///     The on-disk document, brought to the current shape and version, as it was when the
    ///     conflict was raised. Used to build the full-diff report. Null when it could not be read,
    ///     migrated, or parsed.
    /// </summary>
    public string? DiskJson { get; }

    /// <summary>
    ///     This client's document, shaped the same way as <see cref="DiskJson" /> and captured at the
    ///     same moment. Null when the comparison could not be made.
    /// </summary>
    public string? LocalJson { get; }

    /// <summary>
    ///     Answers the conflict. <c>true</c> overwrites the disk file with this instance's version,
    ///     <c>false</c> leaves the disk file as it is.
    /// </summary>
    public void Resolve(bool overwriteLocal) => _resolve(overwriteLocal);
}

/// <summary>
///     Routes external-change conflicts raised by <see cref="JsonSave{T}"/> to a prompt. The UI
///     layer sets <see cref="Prompt"/> once at startup; with no prompt registered the disk file is
///     left alone, so a headless or not-yet-initialized client can never clobber it.
/// </summary>
public static class JsonSaveConflictHandler
{
    /// <summary>
    ///     Shows the "which version do you want to keep" prompt. Set by the UI layer. When null,
    ///     conflicts keep the on-disk version.
    /// </summary>
    public static Action<JsonSaveConflict>? Prompt { get; set; }

    /// <summary>
    ///     Hands <paramref name="conflict"/> to the prompt. Returns <c>false</c> when there is no
    ///     prompt to show.
    /// </summary>
    internal static bool Request(JsonSaveConflict conflict)
    {
        if (Prompt == null)
            return false;

        Prompt(conflict);
        return true;
    }
}
