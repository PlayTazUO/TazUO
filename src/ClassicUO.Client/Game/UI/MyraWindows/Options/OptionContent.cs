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
            IOptionSource source => source.Render(),
            _ => throw new InvalidOperationException("Invalid content type")
        };

    public IEnumerable<OptionEntry> Match(SearchMetadata search) =>
        _content switch
        {
            IOptionSource source => source.Match(SearchMetadata.Merge(Search, search)),
            _ => []
        };

    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null) =>
        _content switch
        {
            IOptionSource source => source.GetOptions(SearchMetadata.Merge(Search, inheritedSearch)),
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

    public static implicit operator OptionContent(OptionTabGroup group)
    {
        return new OptionContent(group);
    }

    public OptionContent WithSearch(SearchMetadata search) => this with { Search = search };
}
