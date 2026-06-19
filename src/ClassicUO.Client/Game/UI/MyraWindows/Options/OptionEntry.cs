#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal sealed record OptionEntry(Func<Widget> RenderFactory, SearchMetadata? Search = null) : IOptionSource
{
    private Widget? _cachedWidget;

    public Widget Render() => _cachedWidget ??= RenderFactory();

    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        var merged = SearchMetadata.Merge(Search, search);
        if (merged.Matches(search))
            yield return this with { Search = merged };
    }

    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        yield return this with { Search = SearchMetadata.Merge(Search, inheritedSearch) };
    }
}
