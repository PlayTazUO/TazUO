#nullable enable
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

public class ContainsLevenshteinComboBox<T> : ScoredSearchComboBox<T>
{
    /// <summary>
    /// Deliberately shadows the base's interface-typed property with the concrete strategy, so
    /// its knobs (MaxDistance, MinScore, CaseSensitive, ...) are reachable without a cast.
    /// Resolved from the base property on every read rather than cached at construction: the
    /// strategy can be replaced afterwards (the public setter, or CopyFrom cloning it), and a
    /// cached field would go on exposing knobs that no longer drive what the dropdown searches
    /// with. Null once the strategy has been replaced with an unrelated one.
    /// </summary>
    public new ContainsThenLevenshteinSearchStrategy? Strategy => base.Strategy as ContainsThenLevenshteinSearchStrategy;

    public ContainsLevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : base(new ContainsThenLevenshteinSearchStrategy(), styleName)
    {
    }
}

public class ContainsLevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : ContainsLevenshteinComboBox<string>(styleName);
