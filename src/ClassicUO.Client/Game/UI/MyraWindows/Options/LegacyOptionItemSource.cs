#nullable enable

using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal sealed class LegacyOptionItemSource(OptionItem item) : IOptionSource
{
    public SearchMetadata? Search => null;

    public Widget Render() => item;

    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        yield break;
    }

    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        yield break;
    }
}
