#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// An <see cref="IOptionSource"/> that presents multiple pages of options using a
/// <see cref="PageControl"/>, where each page is produced by a separate factory.
/// All pages participate in search as if they were a flat union.
/// </summary>
/// <param name="search">Optional search metadata for this group</param>
internal sealed class OptionPageGroup(SearchMetadata? search = null) : IOptionSource
{
    private readonly List<OptionPageDefinition> _pages = [];

    /// <inheritdoc/>
    public SearchMetadata? Search { get; init; } = search;

    /// <inheritdoc/>
    public bool InheritsSearch { get; set; } = true;

    /// <param name="search">Optional search metadata for this group</param>
    /// <param name="pages">Factories producing the initial set of pages, added in order</param>
    public OptionPageGroup(SearchMetadata? search, params Func<IOptionSource>[] pages) : this(search)
    {
        foreach (Func<IOptionSource> page in pages)
            AddPage(page);
    }

    /// <summary>Appends a page to this group and returns this group for fluent chaining</summary>
    /// <param name="contentFactory">Factory that produces the page's option source</param>
    /// <returns>This group</returns>
    public OptionPageGroup AddPage(Func<IOptionSource> contentFactory)
    {
        _pages.Add(new OptionPageDefinition(contentFactory));
        return this;
    }

    /// <summary>
    /// Renders all pages and wraps them in a <see cref="PageControl"/>.
    /// Each call returns a new widget instance.
    /// </summary>
    public Widget Render() => BuildPageControl();

    /// <inheritdoc/>
    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        SearchMetadata? finalSearch = GetSearchMeta(search);
        if (finalSearch == null)
            yield break;

        foreach (OptionPageDefinition page in _pages)
            foreach (OptionEntry entry in page.ContentFactory().Match(finalSearch))
                yield return entry;
    }

    /// <inheritdoc/>
    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        SearchMetadata? search = GetSearchMeta(inheritedSearch);

        foreach (OptionPageDefinition page in _pages)
            foreach (OptionEntry entry in page.ContentFactory().GetOptions(search))
                yield return entry;
    }

    private SearchMetadata? GetSearchMeta(SearchMetadata? inheritedSearch) => InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search;


    private PageControl BuildPageControl()
    {
        var widgets = new Widget[_pages.Count];

        for (int i = 0; i < _pages.Count; i++)
            widgets[i] = _pages[i].ContentFactory().Render();

        return new PageControl(widgets) { RetainSizeWhenPaging = true };
    }

    private readonly record struct OptionPageDefinition(Func<IOptionSource> ContentFactory);
}
