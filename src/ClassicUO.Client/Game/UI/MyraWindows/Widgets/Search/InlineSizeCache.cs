#nullable enable

using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     Measured sizes of entries' inline editors, shared by the rows of successive searches.
///     <para>
///         Measuring an editor means building its entire widget tree, and a one- or two-character
///         query matches most of the index. A cache held by a row would not survive the next
///         keystroke - the results list discards its rows per query - so this belongs to the list
///         itself. Entries are index-level and outlive any one query, which is what makes them
///         usable as keys.
///     </para>
///     <para>
///         Not thread-safe; it is only ever touched while deciding presentation on the UI thread.
///     </para>
/// </summary>
internal sealed class InlineSizeCache
{
    #region Members

    private readonly Dictionary<(SearchEntry Entry, int FontOffset), Point> _sizes = new();

    /// <summary>
    ///     The UI font every cached size was measured against. A measurement is only valid for the
    ///     font that produced it, so a font or size change invalidates the lot.
    /// </summary>
    private SpriteFontBase? _measuredWith;

    #endregion

    #region Public Methods

    /// <summary>
    ///     The size <paramref name="entry" />'s editor needs, measuring it on first ask.
    /// </summary>
    /// <param name="entry">The entry to measure; must be able to inline</param>
    /// <param name="fontOffset">Size offset the editor will be drawn at</param>
    /// <param name="built">
    ///     The editor built in order to measure it, or <see langword="null" /> on a cache hit. Handed
    ///     back so a caller that goes on to host the editor can use this one rather than building a
    ///     second copy of the same tree.
    /// </param>
    /// <returns>The editor's measured size</returns>
    public Point GetSize(SearchEntry entry, int fontOffset, out Widget? built)
    {
        SpriteFontBase font = MyraStyle.UiFont;

        if (!ReferenceEquals(font, _measuredWith))
        {
            _sizes.Clear();
            _measuredWith = font;
        }

        built = null;

        if (_sizes.TryGetValue((entry, fontOffset), out Point cached))
            return cached;

        // Measured at the size it will actually be drawn at. Measuring the unscaled editor reports
        // a width the row never needs, which demotes rows that fit perfectly well.
        built = entry.InlineFactory!().ApplyUiFontOffset(fontOffset);

        Point size = built.Measure(new Point(int.MaxValue / 2, int.MaxValue / 2));

        _sizes[(entry, fontOffset)] = size;

        return size;
    }

    #endregion
}
