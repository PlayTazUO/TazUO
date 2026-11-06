#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FontStashSharp;
using Microsoft.Xna.Framework;

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
    #region Public Methods

    /// <summary>
    ///     The size <paramref name="entry" />'s editor needs, measuring it on first ask.
    /// </summary>
    /// <param name="entry">The entry to measure; must be able to inline</param>
    /// <param name="fontOffset">Size offset the editor will be drawn at</param>
    /// <returns>The editor's measured size</returns>
    public Point GetSize(SearchEntry entry, int fontOffset)
    {
        SpriteFontBase font = MyraStyle.UiFont;

        if (!ReferenceEquals(font, _measuredWith))
        {
            _sizes.Clear();
            _measuredWith = font;
        }

        if (_sizes.TryGetValue((entry, fontOffset), out Point cached))
            return cached;

        // Measured at the size it will actually be drawn at. Measuring the unscaled editor reports
        // a width the row never needs, which demotes rows that fit perfectly well.
        //
        // The widget built here is measured and dropped rather than handed back for hosting: a
        // measured instance carries Myra's cached measurement, taken against an unbounded width,
        // and the host's scroller does not always hand it a different one to force a recompute.
        Point size = entry.InlineFactory!()
            .ApplyUiFontOffset(fontOffset)
            .Measure(new Point(int.MaxValue / 2, int.MaxValue / 2));

        _sizes[(entry, fontOffset)] = size;

        return size;
    }

    #endregion

    #region Private types

    /// <summary>
    ///     Identity equality for the entry half of the key. Entries are index-level singletons, so
    ///     reference identity is exact - and a record's generated hash walks every member, including
    ///     a group entry's match text, which is a concatenation of every descendant label.
    /// </summary>
    private sealed class EntryIdentityComparer : IEqualityComparer<(SearchEntry Entry, int FontOffset)>
    {
        public static readonly EntryIdentityComparer Instance = new();

        public bool Equals((SearchEntry Entry, int FontOffset) left, (SearchEntry Entry, int FontOffset) right) =>
            ReferenceEquals(left.Entry, right.Entry) && left.FontOffset == right.FontOffset;

        public int GetHashCode((SearchEntry Entry, int FontOffset) key) =>
            HashCode.Combine(RuntimeHelpers.GetHashCode(key.Entry), key.FontOffset);
    }

    #endregion

    #region Members

    private readonly Dictionary<(SearchEntry Entry, int FontOffset), Point> _sizes = new(EntryIdentityComparer.Instance);

    /// <summary>
    ///     The UI font every cached size was measured against. A measurement is only valid for the
    ///     font that produced it, so a font or size change invalidates the lot.
    /// </summary>
    private SpriteFontBase? _measuredWith;

    #endregion
}
