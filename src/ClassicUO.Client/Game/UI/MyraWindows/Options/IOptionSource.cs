#nullable enable

using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

internal interface IOptionSource
{
    SearchMetadata? Search { get; }
    IEnumerable<OptionEntry> Match(SearchMetadata search);
    Widget Render();
    IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null);
}
