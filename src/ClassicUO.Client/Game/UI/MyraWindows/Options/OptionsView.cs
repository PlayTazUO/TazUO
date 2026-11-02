#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// One entry in the options window's navigation history: either a page at some breadcrumb, or the
/// search results for a query.
/// <para>
/// Holds location plus view state - collapse set, scroll offset - but never widgets, so a rebuild
/// triggered by a profile preset or a theme change can't strand the history on disposed controls.
/// </para>
/// </summary>
internal sealed class OptionsView
{
    /// <summary>
    /// Compares views by location alone, ignoring view state. Used by the history to collapse a
    /// push that repeats where the user already is, so re-clicking the current tab records nothing.
    /// </summary>
    public static readonly IEqualityComparer<OptionsView> LocationComparer = new ByLocation();

    /// <summary>
    /// The query when this is a search view, otherwise <see langword="null"/>. Settable so retyping
    /// updates the current entry in place instead of pushing one entry per keystroke.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>The breadcrumb to open when this is a page view</summary>
    public SearchPath Path { get; init; }

    /// <summary>Paths of the groups the user had collapsed in the results list</summary>
    public HashSet<string> Collapsed { get; } = [];

    /// <summary>Vertical scroll offset to restore</summary>
    public int ScrollOffset { get; set; }

    /// <summary>Whether this view shows search results rather than a page</summary>
    public bool IsSearch => Query != null;

    private sealed class ByLocation : IEqualityComparer<OptionsView>
    {
        public bool Equals(OptionsView? x, OptionsView? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x == null || y == null)
                return false;

            return string.Equals(x.Query, y.Query, StringComparison.Ordinal) && x.Path == y.Path;
        }

        public int GetHashCode(OptionsView view) =>
            HashCode.Combine(view.Query == null ? 0 : StringComparer.Ordinal.GetHashCode(view.Query), view.Path);
    }
}
