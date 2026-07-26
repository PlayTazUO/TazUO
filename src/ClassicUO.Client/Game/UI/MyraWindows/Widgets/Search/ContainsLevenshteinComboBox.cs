#nullable enable
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

public class ContainsLevenshteinComboBox<T> : ScoredSearchComboBox<T>
{
    private readonly ContainsThenLevenshteinSearchStrategy _strategy;

    /// <summary>Deliberately shadows the base's interface-typed property with the concrete strategy, so its knobs (MaxDistance, MinScore, CaseSensitive, ...) are reachable without a cast.</summary>
    public new ContainsThenLevenshteinSearchStrategy Strategy => _strategy;

    public ContainsLevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : this(new ContainsThenLevenshteinSearchStrategy(), styleName)
    {
    }

    private ContainsLevenshteinComboBox(ContainsThenLevenshteinSearchStrategy strategy, string styleName) : base(strategy, styleName)
    {
        _strategy = strategy;
    }
}

public class ContainsLevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : ContainsLevenshteinComboBox<string>(styleName);
