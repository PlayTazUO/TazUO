#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal sealed class OptionTabGroup : IOptionSource
{
    private readonly List<OptionTabDefinition> _tabs = [];
    private readonly Func<MyraTabControl> _tabControlFactory;
    private Widget? _cachedWidget;

    public SearchMetadata? Search { get; init; }
    public bool InheritsSearch { get; set; } = true;

    public OptionTabGroup(Func<MyraTabControl>? tabControlFactory = null, SearchMetadata? search = null)
    {
        _tabControlFactory = tabControlFactory ?? (() => new MyraTabControl());
        Search = search;
    }

    public OptionTabGroup AddTab(string label, Func<IOptionSource> contentFactory, SearchMetadata? search = null)
    {
        _tabs.Add(new OptionTabDefinition(label, contentFactory, search ?? new SearchMetadata(label, [label])));
        return this;
    }

    public Widget Render() => _cachedWidget ??= BuildTabControl();

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
