#nullable enable

using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// State threaded down the options tree while it is being flattened for search. Carries the
/// breadcrumb accumulated so far, so each node contributes its own name on the way down rather
/// than walking back up to its parents - which it could not do anyway, since children are either
/// value types or lazy factories with no back-reference.
/// </summary>
/// <param name="Path">Labels of the labelled ancestors visited so far, outermost first</param>
/// <param name="Inherited">Search metadata propagated from ancestors</param>
internal readonly record struct OptionIndexContext(SearchPath Path, SearchMetadata? Inherited = null)
{
    /// <summary>Returns this context with <paramref name="segment"/> appended to the path</summary>
    /// <param name="segment">The segment to append; blank values are ignored</param>
    /// <returns>The descended context</returns>
    public OptionIndexContext Descend(string? segment) => this with { Path = Path.Push(segment) };

    /// <summary>Returns this context with <paramref name="metadata"/> as the inherited metadata</summary>
    /// <param name="metadata">The metadata children should inherit</param>
    /// <returns>The updated context</returns>
    public OptionIndexContext WithInherited(SearchMetadata? metadata) => this with { Inherited = metadata };
}

/// <summary>
/// Implemented by every node in the options tree: produces a widget for rendering and flattens
/// itself into <see cref="SearchEntry"/> values for the search index
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

    /// <summary>Returns the widget that represents this source in the options UI</summary>
    /// <returns>The rendered widget</returns>
    Widget Render();

    /// <summary>
    /// Flattens this node into searchable entries, annotating each with the breadcrumb and merged
    /// metadata accumulated in <paramref name="context"/>.
    /// <para>
    /// Entries are emitted without an activation callback - only the hosting window knows how to
    /// navigate to a path, so it fills that in.
    /// </para>
    /// </summary>
    /// <param name="context">The accumulated path and inherited metadata</param>
    /// <returns>Every searchable entry under this node</returns>
    IEnumerable<SearchEntry> Index(OptionIndexContext context);
}
