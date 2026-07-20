#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// An <see cref="IOptionSource"/> that presents its children as named tabs within a
/// <see cref="MyraTabControl"/>. Each tab is produced on demand by a factory, and all tabs
/// participate in search as a flat union.
/// </summary>
internal sealed class OptionTabGroup : IOptionSource
{
    private readonly List<OptionTabDefinition> _tabs = [];
    private readonly Func<MyraTabControl> _tabControlFactory;

    /// <inheritdoc/>
    public SearchMetadata? Search { get; init; }

    /// <inheritdoc/>
    public bool InheritsSearch { get; set; } = true;

    /// <param name="tabControlFactory">
    /// Optional factory for the underlying tab control.
    /// When <see langword="null"/>, a default <see cref="MyraTabControl"/> is created.
    /// </param>
    /// <param name="search">Optional search metadata for this group</param>
    public OptionTabGroup(Func<MyraTabControl>? tabControlFactory = null, SearchMetadata? search = null)
    {
        _tabControlFactory = tabControlFactory ?? (() => new MyraTabControl());
        Search = search;
    }

    /// <summary>Appends a tab and returns this group for fluent chaining</summary>
    /// <param name="label">The tab header text</param>
    /// <param name="contentFactory">Factory that produces the tab's option source</param>
    /// <param name="search">
    /// Optional search metadata for the tab. Defaults to metadata seeded from <paramref name="label"/>.
    /// </param>
    /// <returns>This group</returns>
    public OptionTabGroup AddTab(string label, Func<IOptionSource> contentFactory, SearchMetadata? search = null)
    {
        _tabs.Add(new OptionTabDefinition(label, contentFactory, search ?? new SearchMetadata(label, [label])));
        return this;
    }

    /// <summary>
    /// Renders the tab control. Each call returns a new widget instance.
    /// </summary>
    public Widget Render() => BuildTabControl();

    /// <inheritdoc/>
    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        SearchMetadata? selfSearch = GetSearchMeta(search);
        if (selfSearch == null)
            yield break;

        foreach (OptionTabDefinition tab in _tabs)
        {
            var tabMerged = SearchMetadata.Merge(tab.Search, selfSearch);

            foreach (OptionEntry entry in tab.ContentFactory().Match(tabMerged))
                yield return entry;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        SearchMetadata? selfSearch = GetSearchMeta(inheritedSearch);

        foreach (OptionTabDefinition tab in _tabs)
        {
            var tabMerged = SearchMetadata.Merge(tab.Search, selfSearch);

            foreach (OptionEntry entry in tab.ContentFactory().GetOptions(tabMerged))
                yield return entry;
        }
    }

    private SearchMetadata? GetSearchMeta(SearchMetadata? inheritedSearch) => InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search;

    private MyraTabControl BuildTabControl()
    {
        MyraTabControl tabs = _tabControlFactory();

        foreach (OptionTabDefinition tab in _tabs)
            tabs.AddTab(tab.Label, () => tab.ContentFactory().Render());

        return tabs;
    }

    private readonly record struct OptionTabDefinition(
        string Label,
        Func<IOptionSource> ContentFactory,
        SearchMetadata Search
    );
}
