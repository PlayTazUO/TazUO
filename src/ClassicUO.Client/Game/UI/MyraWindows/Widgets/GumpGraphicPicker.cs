#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     Picks a gump graphic: a hex-or-decimal number field beside a searchable list of the graphics the
///     client's gump archive actually holds, plus an entry for "no graphic" where that is a valid choice.
/// </summary>
/// <remarks>
///     The list names archive entries only. Loose <c>.gump</c> files and PNG overrides are reachable by
///     typing their number — which is the division <see cref="IndexedComboPicker" /> is built around, the
///     number being what gets stored either way.
/// </remarks>
public sealed class GumpGraphicPicker : IndexedComboPicker
{
    #region Public events

    /// <summary>Raised when the chosen graphic changes, from either input. Null means no graphic.</summary>
    public event EventHandler<ushort?>? GraphicChanged;

    #endregion

    #region Public constants

    /// <summary>
    ///     Value standing for a null graphic, offered as the list's first entry. What null means is the
    ///     caller's business - see the <c>noneLabel</c> ctor parameter.
    /// </summary>
    public const int NO_GRAPHIC = -1;

    #endregion

    #region Public accessors

    /// <summary>The chosen graphic, or null for none. Setting it moves both inputs.</summary>
    public ushort? Graphic
    {
        get => ToGraphic(Value);
        set => Value = value ?? NO_GRAPHIC;
    }

    #endregion

    #region Private members

    /// <summary>Built once per session: the archive is read-only at runtime, so the list cannot go stale.</summary>
    private static List<(int Value, string Label)>? _catalog;

    #endregion

    #region Ctor

    /// <param name="graphic">The graphic to start on, or null to start on <see cref="NO_GRAPHIC"/>.</param>
    /// <param name="noneLabel">
    ///     What to call the null entry, where "none" is not what null means for the caller's field - e.g.
    ///     a field whose null falls back to another graphic rather than drawing nothing.
    /// </param>
    public GumpGraphicPicker(ushort? graphic, string? noneLabel = null)
        : base(graphic ?? NO_GRAPHIC, Entries(noneLabel), NO_GRAPHIC, ushort.MaxValue, new HexIntInputBox())
    {
        ValueChanged += (_, value) => GraphicChanged?.Invoke(this, ToGraphic(value));
    }

    #endregion

    #region Private methods

    private static List<(int Value, string Label)> Catalog => _catalog ??= BuildCatalog();

    /// <summary>Prepends the caller's null entry to the shared catalog, lazily so the catalog is not copied.</summary>
    private static IEnumerable<(int Value, string Label)> Entries(string? noneLabel)
    {
        yield return (NO_GRAPHIC, noneLabel ?? TazLang.Get("gumppicker_none", "(None)"));

        foreach ((int value, string label) in Catalog)
            yield return (value, label);
    }

    private static List<(int Value, string Label)> BuildCatalog()
    {
        List<(int Value, string Label)> entries = [];

        UOFile? file = Client.Game.UO.FileManager?.Gumps?.File;

        if (file?.Entries == null)
            return entries;

        int count = Math.Min(file.Entries.Length, GumpsLoader.MAX_GUMP_DATA_INDEX_COUNT);

        for (int id = 0; id < count; id++)
        {
            // An unused slot carries a negative length, which is cheaper to test than resolving the
            // entry - the archive fills only a few thousand of its 65,536 numbers.
            if (file.Entries[id].Length <= 0)
                continue;

            entries.Add((id, $"0x{id:X4} ({id})"));
        }

        return entries;
    }

    private static ushort? ToGraphic(int value) => value < 0 ? null : (ushort)value;

    #endregion
}
