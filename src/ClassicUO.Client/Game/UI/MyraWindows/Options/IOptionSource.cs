#nullable enable

using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// Implemented by every node in the options tree: produces a widget for rendering and exposes its
/// contained <see cref="OptionEntry"/> leaves so the search system can locate them
/// </summary>
internal interface IOptionSource
{
    /// <summary>Search metadata attached directly to this source</summary>
    SearchMetadata? Search { get; }

    /// <summary>
    /// When <see langword="true"/>, this source merges its own <see cref="Search"/> with metadata
    /// inherited from a parent node before dispatching to children.
    /// When <see langword="false"/>, only <see cref="Search"/> is used regardless of the parent.
    /// </summary>
    bool InheritsSearch { get; set; }

    /// <summary>
    /// Returns every <see cref="OptionEntry"/> leaf under this node whose metadata matches
    /// <paramref name="search"/>
    /// </summary>
    /// <param name="search">The search criteria to evaluate against each leaf's metadata</param>
    /// <returns>The matching option entries</returns>
    IEnumerable<OptionEntry> Match(SearchMetadata search);

    /// <summary>Returns the widget that represents this source in the options UI</summary>
    /// <returns>The rendered widget</returns>
    Widget Render();

    /// <summary>
    /// Returns every <see cref="OptionEntry"/> leaf under this node, each annotated with the
    /// effective search metadata obtained by merging this node's own <see cref="Search"/> with
    /// <paramref name="inheritedSearch"/>
    /// </summary>
    /// <param name="inheritedSearch">Search metadata propagated down from a parent node</param>
    /// <returns>All leaf entries with their effective merged metadata</returns>
    IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null);
}
