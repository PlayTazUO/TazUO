#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal sealed class OptionFragment(Func<Widget> renderFactory, IEnumerable<OptionContent> children) : IOptionSource
{
    private Widget? _cachedWidget;

    public SearchMetadata? Search { get; set; }
    public bool InheritsSearch { get; set; } = true;

    public Widget Render() => _cachedWidget ??= renderFactory();

    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        SearchMetadata? finalSearch = InheritsSearch ? SearchMetadata.Merge(Search, search) : Search;
        return finalSearch == null
            ? []
            : children.SelectMany(c => c.Match(finalSearch));
    }

    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        SearchMetadata? merged = InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search;
        return children.SelectMany(c => c.GetOptions(merged));
    }

    public OptionFragment WithSearch(SearchMetadata search)
    {
        Search = search;
        return this;
    }
}
