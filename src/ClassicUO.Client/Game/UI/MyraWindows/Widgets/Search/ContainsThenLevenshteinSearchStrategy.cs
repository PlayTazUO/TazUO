#nullable enable

using Myra.Utility.Search;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
/// Plain substring match first (short-circuits as soon as the query literally appears),
/// falling back to per-token Levenshtein distance when nothing contains it outright. Built on
/// <see cref="CompositeSearchStrategy"/> so the two halves stay independently testable.
/// </summary>
public class ContainsThenLevenshteinSearchStrategy : CompositeSearchStrategy
{
    private readonly SubstringSearchStrategy _contains;
    private readonly LevenshteinSearchStrategy _levenshtein;

    public bool CaseSensitive
    {
        get => _contains.CaseSensitive;
        set
        {
            _contains.CaseSensitive = value;
            _levenshtein.CaseSensitive = value;
        }
    }

    public int MaxDistance
    {
        get => _levenshtein.MaxDistance;
        set => _levenshtein.MaxDistance = value;
    }

    public float MinScore
    {
        get => _levenshtein.MinScore;
        set => _levenshtein.MinScore = value;
    }

    public ContainsThenLevenshteinSearchStrategy()
        : this(new SubstringSearchStrategy(), new LevenshteinSearchStrategy { PerTokenBest = true })
    {
    }

    private ContainsThenLevenshteinSearchStrategy(SubstringSearchStrategy contains, LevenshteinSearchStrategy levenshtein)
        : base(contains, levenshtein)
    {
        _contains = contains;
        _levenshtein = levenshtein;
    }
}
