#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal sealed class OptionPageGroup(SearchMetadata? search = null) : IOptionSource
{
    private readonly List<OptionPageDefinition> _pages = [];
    private Widget? _cachedWidget;

    public SearchMetadata? Search { get; init; } = search;

    public OptionPageGroup(SearchMetadata? search, params Func<IOptionSource>[] pages) : this(search)
    {
        foreach (Func<IOptionSource> page in pages)
            AddPage(page);
    }

    public OptionPageGroup AddPage(Func<IOptionSource> contentFactory)
    {
        _pages.Add(new OptionPageDefinition(contentFactory));
        return this;
    }

    public Widget Render() => _cachedWidget ??= BuildPageControl();

    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        var merged = SearchMetadata.Merge(Search, search);

        foreach (OptionPageDefinition page in _pages)
            foreach (OptionEntry entry in page.ContentFactory().Match(merged))
                yield return entry;
    }

    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        var merged = SearchMetadata.Merge(Search, inheritedSearch);

        foreach (OptionPageDefinition page in _pages)
            foreach (OptionEntry entry in page.ContentFactory().GetOptions(merged))
                yield return entry;
    }

    private PageControl BuildPageControl()
    {
        var widgets = new Widget[_pages.Count];

        for (int i = 0; i < _pages.Count; i++)
            widgets[i] = _pages[i].ContentFactory().Render();

        return new PageControl(widgets) { RetainSizeWhenPaging = true };
    }

    private readonly record struct OptionPageDefinition(Func<IOptionSource> ContentFactory);
}
