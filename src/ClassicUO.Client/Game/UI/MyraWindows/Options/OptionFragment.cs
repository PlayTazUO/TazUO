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

    public Widget Render() => _cachedWidget ??= renderFactory();

    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        var merged = SearchMetadata.Merge(Search, search);
        return children.SelectMany(c => c.Match(merged));
    }

    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        var merged = SearchMetadata.Merge(Search, inheritedSearch);
        return children.SelectMany(c => c.GetOptions(merged));
    }

    public OptionFragment WithSearch(SearchMetadata search)
    {
        Search = search;
        return this;
    }
}
