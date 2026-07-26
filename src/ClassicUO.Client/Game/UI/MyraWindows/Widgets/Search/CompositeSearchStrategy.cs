#nullable enable

using System;
using System.Linq;
using Myra.Utility.Search;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
/// Chains strategies in order, short-circuiting on the first that matches a given candidate.
/// Lets a cheap strategy (e.g. plain substring) gate a more expensive one (e.g. Levenshtein)
/// without either strategy needing to know about the other.
/// </summary>
public class CompositeSearchStrategy : ISearchStrategy
{
    private readonly ISearchStrategy[] _strategies;

    public CompositeSearchStrategy(params ISearchStrategy[] strategies)
    {
        if (strategies == null || strategies.Length == 0)
            throw new ArgumentException("At least one strategy is required.", nameof(strategies));

        _strategies = strategies;
    }

    public bool IsQueryValid(string query) => _strategies.Any(s => s.IsQueryValid(query));

    public SearchMatch Match(string candidate, string query)
    {
        foreach (ISearchStrategy strategy in _strategies)
        {
            SearchMatch match = strategy.Match(candidate, query);
            if (match.IsMatch)
                return match;
        }

        return SearchMatch.None;
    }
}
