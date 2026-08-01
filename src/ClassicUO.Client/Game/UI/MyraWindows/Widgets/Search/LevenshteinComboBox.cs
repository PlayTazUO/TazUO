#nullable enable
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

public class LevenshteinComboBox<T> : ScoredSearchComboBox<T>
{
    /// <summary>
    /// Shadows the base's interface-typed property with the concrete strategy. Resolved from the
    /// base property on every read rather than cached at construction: the strategy can be
    /// replaced afterwards (the public setter, or CopyFrom cloning it), and a cached field would
    /// go on exposing knobs that no longer drive what the dropdown searches with. Null once the
    /// strategy has been replaced with an unrelated one.
    /// </summary>
    public new LevenshteinSearchStrategy? Strategy => base.Strategy as LevenshteinSearchStrategy;

    /// <summary>
    /// Edit-distance ceiling of the active strategy. Reads back the strategy's own default while
    /// <see cref="Strategy"/> is null (the strategy was swapped for an unrelated one), and
    /// assigning is then a no-op rather than a write into a strategy nobody searches with.
    /// </summary>
    public int MaxDistance
    {
        get => Strategy?.MaxDistance ?? LevenshteinSearchStrategy.DEFAULT_MAX_DISTANCE;
        set
        {
            if (Strategy is { } strategy)
                strategy.MaxDistance = value;
        }
    }

    public LevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : base(new LevenshteinSearchStrategy(), styleName)
    {
    }
}

public class LevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : LevenshteinComboBox<string>(styleName);
