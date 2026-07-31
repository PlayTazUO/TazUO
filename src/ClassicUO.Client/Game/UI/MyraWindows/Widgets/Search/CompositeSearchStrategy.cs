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

    /// <summary>
    /// Valid only when every chained strategy accepts the query. <see cref="Match"/> runs them
    /// all, so a query one of them can't compile (a malformed pattern in a regex-based strategy,
    /// say) has to be reported as invalid - answering "valid" because some other strategy would
    /// take it turns the callers' invalid-query affordance off and leaves the user with a
    /// silently empty result list instead.
    /// </summary>
    public bool IsQueryValid(string query) => _strategies.All(s => s.IsQueryValid(query));

    /// <summary>
    /// Deep: the inner strategies are mutable too, so a shallow copy would still let one composite
    /// retune another's. Virtual so subclasses can preserve their own runtime type - the base
    /// implementation returns a plain <see cref="CompositeSearchStrategy"/>, which would strip a
    /// subclass's configuration knobs off any widget that clones its strategy.
    /// </summary>
    public virtual ISearchStrategy Clone() => new CompositeSearchStrategy(CloneStrategies());

    /// <summary>Independent copies of the chained strategies, for a subclass's own <see cref="Clone"/>.</summary>
    protected ISearchStrategy[] CloneStrategies() => _strategies.Select(s => s.Clone()).ToArray();

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
