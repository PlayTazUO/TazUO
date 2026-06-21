#nullable enable

using System;
using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal sealed record OptionEntry(Func<Widget> RenderFactory, SearchMetadata? Search = null) : IOptionSource
{
    private Widget? _cachedWidget;

    public bool InheritsSearch { get; set; } = true;

    public Widget Render() => _cachedWidget ??= RenderFactory();

    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        if (Search?.Matches(search) == true)
            yield return this;
    }

    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        yield return this with { Search = InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search };
    }
}
