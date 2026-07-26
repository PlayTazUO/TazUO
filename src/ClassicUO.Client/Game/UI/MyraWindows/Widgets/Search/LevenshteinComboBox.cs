#nullable enable
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

public class LevenshteinComboBox<T> : ScoredSearchComboBox<T>
{
    private readonly LevenshteinSearchStrategy _strategy;

    public int MaxDistance
    {
        get => _strategy.MaxDistance;
        set => _strategy.MaxDistance = value;
    }

    public LevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : this(new LevenshteinSearchStrategy(), styleName)
    {
    }

    private LevenshteinComboBox(LevenshteinSearchStrategy strategy, string styleName) : base(strategy, styleName)
    {
        _strategy = strategy;
    }
}

public class LevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : LevenshteinComboBox<string>(styleName);
