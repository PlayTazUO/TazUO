#nullable enable
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

public class ContainsLevenshteinComboBox<T> : ScoredSearchComboBox<T>
{
    private readonly ContainsThenLevenshteinSearchStrategy _strategy;

    public int MaxDistance
    {
        get => _strategy.MaxDistance;
        set => _strategy.MaxDistance = value;
    }

    public bool CaseSensitive
    {
        get => _strategy.CaseSensitive;
        set => _strategy.CaseSensitive = value;
    }

    public ContainsLevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : this(new ContainsThenLevenshteinSearchStrategy(), styleName)
    {
    }

    private ContainsLevenshteinComboBox(ContainsThenLevenshteinSearchStrategy strategy, string styleName) : base(strategy, styleName)
    {
        _strategy = strategy;
    }
}

public class ContainsLevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : ContainsLevenshteinComboBox<string>(styleName);
