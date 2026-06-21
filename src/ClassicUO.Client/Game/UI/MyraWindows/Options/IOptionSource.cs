#nullable enable

using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal interface IOptionSource
{
    SearchMetadata? Search { get; }
    bool InheritsSearch { get; set; }
    IEnumerable<OptionEntry> Match(SearchMetadata search);
    Widget Render();
    IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null);
}
