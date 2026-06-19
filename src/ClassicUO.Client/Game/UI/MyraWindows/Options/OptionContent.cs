#nullable enable

using System;
using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal readonly struct OptionContent
{
    private readonly object? _content;

    public SearchMetadata? Search { get; private init; }

    private OptionContent(object content)
    {
        _content = content;
    }

    public Widget Render() =>
        _content switch
        {
            Widget widget => widget,
            OptionEntry entry => entry.Render(),
            OptionFragment fragment => fragment.Render(),
            _ => throw new InvalidOperationException("Invalid content type")
        };

    public IEnumerable<OptionEntry> Match(SearchMetadata search) =>
        _content switch
        {
            OptionEntry entry => entry.Match(SearchMetadata.Merge(Search, search)),
            OptionFragment fragment => fragment.Match(SearchMetadata.Merge(Search, search)),
            _ => []
        };

    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null) =>
        _content switch
        {
            OptionEntry entry => entry.GetOptions(SearchMetadata.Merge(Search, inheritedSearch)),
            OptionFragment fragment => fragment.GetOptions(SearchMetadata.Merge(Search, inheritedSearch)),
            _ => []
        };

    public static implicit operator OptionContent(Widget widget)
    {
        return new OptionContent(widget);
    }

    public static implicit operator OptionContent(OptionEntry entry)
    {
        return new OptionContent(entry);
    }

    public static implicit operator OptionContent(OptionFragment fragment)
    {
        return new OptionContent(fragment);
    }

    public OptionContent WithSearch(SearchMetadata search) => this with { Search = search };
}
