#nullable enable

using System;
using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// A discriminated union that holds either a raw <see cref="Widget"/>, an <see cref="OptionEntry"/>,
/// an <see cref="OptionFragment"/>, or an <see cref="OptionTabGroup"/> at a position in an option layout.
/// Implicit conversions eliminate explicit wrapping at every call site.
/// </summary>
internal readonly struct OptionContent
{
    private readonly object? _content;

    /// <summary>Search metadata attached to this slot</summary>
    public SearchMetadata? Search { get; private init; }

    /// <summary>
    /// When <see langword="true"/>, the effective search metadata is the merge of <see cref="Search"/>
    /// and any metadata inherited from the parent. When <see langword="false"/>, only <see cref="Search"/> is used.
    /// </summary>
    public bool InheritsSearch { get; init; } = true;

    private OptionContent(object content)
    {
        _content = content;
    }

    /// <summary>Renders the wrapped content as a widget</summary>
    /// <returns>The rendered widget</returns>
    /// <exception cref="InvalidOperationException">Thrown when the wrapped object is not a known content type</exception>
    public Widget Render() =>
        _content switch
        {
            Widget widget => widget,
            IOptionSource source => source.Render(),
            _ => throw new InvalidOperationException("Invalid content type")
        };

    /// <summary>
    /// Delegates to the wrapped <see cref="IOptionSource"/> and returns its matching entries,
    /// or an empty sequence if the wrapped object is a raw widget
    /// </summary>
    /// <param name="search">The search criteria to evaluate</param>
    /// <returns>Matching option entries</returns>
    public IEnumerable<OptionEntry> Match(SearchMetadata search) =>
        _content switch
        {
            IOptionSource source => GetMatches(source, search),
            _ => []
        };

    /// <summary>
    /// Delegates to the wrapped <see cref="IOptionSource"/> and returns all leaf entries,
    /// or an empty sequence if the wrapped object is a raw widget
    /// </summary>
    /// <param name="inheritedSearch">Search metadata propagated from a parent node</param>
    /// <returns>All leaf entries with their merged metadata</returns>
    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null) =>
        _content switch
        {
            IOptionSource source => source.GetOptions(GetSearch(inheritedSearch)),
            _ => []
        };

    private IEnumerable<OptionEntry> GetMatches(IOptionSource source, SearchMetadata search)
    {
        SearchMetadata? finalSearch = GetSearch(search);
        return finalSearch == null ? [] : source.Match(finalSearch);
    }

    private SearchMetadata? GetSearch(SearchMetadata? inheritedSearch) => InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search;

    /// <summary>Wraps a raw widget in an <see cref="OptionContent"/> with no search metadata</summary>
    /// <param name="widget">The widget to wrap</param>
    public static implicit operator OptionContent(Widget widget)
    {
        return new OptionContent(widget);
    }

    /// <summary>Wraps an <see cref="OptionEntry"/> in an <see cref="OptionContent"/></summary>
    /// <param name="entry">The entry to wrap</param>
    public static implicit operator OptionContent(OptionEntry entry)
    {
        return new OptionContent(entry);
    }

    /// <summary>Wraps an <see cref="OptionFragment"/> in an <see cref="OptionContent"/></summary>
    /// <param name="fragment">The fragment to wrap</param>
    public static implicit operator OptionContent(OptionFragment fragment)
    {
        return new OptionContent(fragment);
    }

    /// <summary>Wraps an <see cref="OptionTabGroup"/> in an <see cref="OptionContent"/></summary>
    /// <param name="group">The tab group to wrap</param>
    public static implicit operator OptionContent(OptionTabGroup group)
    {
        return new OptionContent(group);
    }

    /// <summary>Returns a copy of this slot with its <see cref="Search"/> property replaced</summary>
    /// <param name="search">The search metadata to attach</param>
    /// <returns>A copy of this slot with the given search metadata</returns>
    public OptionContent WithSearch(SearchMetadata search) => this with { Search = search };
}
