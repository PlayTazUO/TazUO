#nullable enable

using System;
using System.Collections.Generic;

namespace ClassicUO.Assets;

/// <param name="Index">The sound's index in the client's data, which is what a packet names.</param>
/// <param name="Name">The name embedded in the sound's own header.</param>
public readonly record struct SoundEntry(int Index, string Name);

/// <summary>
///     The named sounds a <see cref="SoundsLoader" /> holds, read once and kept.
///     <para>
///         Building one is a seek and a 40-byte header read per sound across the whole file, so it is
///         worth doing once and sharing rather than per screen that wants to list sounds.
///     </para>
///     <para>
///         Names only - no formatting, no labels. What a sound should be called on screen is the caller's
///         business, and two callers wanting it written differently must not have to agree.
///     </para>
/// </summary>
public sealed class SoundCatalogue
{
    #region Public accessors

    /// <summary>The named sounds, in index order.</summary>
    public IReadOnlyList<SoundEntry> Entries => _entries;

    #endregion

    #region Private members

    private readonly List<SoundEntry> _entries = [];

    private readonly Dictionary<int, string> _namesByIndex = new();

    #endregion

    #region Public methods

    /// <summary>
    ///     Reads every named sound out of <paramref name="loader" />.
    /// </summary>
    /// <param name="loader">The loaded sound data, or null where none is loaded.</param>
    /// <returns>The catalogue; empty where there is no data, or none of it is named.</returns>
    /// <exception cref="Exception">
    ///     Whatever the loader throws on unreadable data. Left to the
    ///     caller: a screen that lists sounds can carry on without names, and only the caller knows
    ///     whether that is true of it.
    /// </exception>
    public static SoundCatalogue Build(SoundsLoader? loader)
    {
        var catalogue = new SoundCatalogue();

        if (loader == null)
            return catalogue;

        foreach (var (index, name) in loader.EnumerateSounds())
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;

            catalogue._entries.Add(new SoundEntry(index, name));
            catalogue._namesByIndex[index] = name;
        }

        return catalogue;
    }

    /// <summary>
    ///     The name of one sound.
    /// </summary>
    /// <param name="index">The sound's index.</param>
    /// <param name="name">Its name, where the data carries one.</param>
    /// <returns>Whether the sound is named.</returns>
    public bool TryGetName(int index, out string? name) => _namesByIndex.TryGetValue(index, out name);

    #endregion
}
