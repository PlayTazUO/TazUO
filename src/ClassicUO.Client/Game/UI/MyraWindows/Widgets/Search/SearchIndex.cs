#nullable enable

using System;
using System.Collections.Generic;
using Myra.Utility.Search;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     A flat, queryable collection of <see cref="SearchEntry" /> built once from some hierarchy and
///     then scored per query. Results are memoised for the last query, since the per-entry
///     Levenshtein fallback is linear in the index size and a UI will ask for the same query
///     repeatedly while laying out.
///     <para>
///         Insertion order is meaningful: it breaks score ties, so entries added in their declared
///         order keep that order among equally-good matches instead of shuffling as the user types.
///     </para>
/// </summary>
public sealed class SearchIndex
{
    #region Members

    #region Consts/Readonly

    /// <summary>
    ///     Field weights. The label is the thing the user is looking at, so it dominates; a path hit
    ///     is a weaker but legitimate signal (searching a tab's name should surface its contents);
    ///     the catch-all match text ranks lowest because it is a blob of inherited terms.
    /// </summary>
    private const double LABEL_WEIGHT = 1.0;
    private const double KEYWORD_WEIGHT = 0.45;
    private const double PATH_WEIGHT = 0.5;
    private const double MATCH_TEXT_WEIGHT = 0.3;

    private readonly List<SearchEntry> _entries = [];
    private readonly ISearchStrategy _strategy;

    #endregion

    #region Mutables

    private string? _cachedQuery;
    private List<SearchHit>? _cachedHits;

    #endregion

    #endregion

    #region Public Accessors

    /// <summary>Every indexed entry, in insertion order</summary>
    public IReadOnlyList<SearchEntry> Entries => _entries;

    /// <summary>Number of indexed entries</summary>
    public int Count => _entries.Count;

    #endregion

    #region Ctor

    /// <param name="strategy">
    ///     Matcher used for every field. Defaults to substring-then-fuzzy
    ///     (<see cref="ContainsThenLevenshteinSearchStrategy" />).
    /// </param>
    public SearchIndex(ISearchStrategy? strategy = null)
    {
        _strategy = strategy ?? new ContainsThenLevenshteinSearchStrategy();
    }

    #endregion

    #region Public Methods

    /// <summary>Appends an entry to the index, invalidating any cached results</summary>
    /// <param name="entry">The entry to index</param>
    public void Add(SearchEntry entry)
    {
        _entries.Add(entry);
        InvalidateCache();
    }

    /// <summary>Appends several entries to the index, invalidating any cached results</summary>
    /// <param name="entries">The entries to index</param>
    public void AddRange(IEnumerable<SearchEntry> entries)
    {
        _entries.AddRange(entries);
        InvalidateCache();
    }

    /// <summary>Empties the index</summary>
    public void Clear()
    {
        _entries.Clear();
        InvalidateCache();
    }

    /// <summary>
    ///     Scores every entry against <paramref name="query" /> and returns the matches, best first.
    ///     An empty query returns nothing rather than everything - a search UI showing its whole
    ///     index when the box is empty is noise, and callers that want everything have
    ///     <see cref="Entries" />.
    /// </summary>
    /// <param name="query">The user's query</param>
    /// <returns>Matching entries ordered by descending score, ties broken by insertion order</returns>
    public IReadOnlyList<SearchHit> Search(string? query)
    {
        query = query?.Trim();

        if (string.IsNullOrEmpty(query))
            return [];

        if (_cachedHits != null && string.Equals(_cachedQuery, query, StringComparison.Ordinal))
            return _cachedHits;

        if (!_strategy.IsQueryValid(query))
            return [];

        List<ScoredIndex> scored = [];

        for (int i = 0; i < _entries.Count; i++)
        {
            SearchHit? hit = Score(_entries[i], query);

            if (hit.HasValue)
                scored.Add(new ScoredIndex(hit.Value with { Order = i }, i));
        }

        scored.Sort(CompareByScoreThenOrder);

        var hits = new List<SearchHit>(scored.Count);

        foreach (ScoredIndex entry in scored)
            hits.Add(entry.Hit);

        _cachedQuery = query;
        _cachedHits = hits;

        return hits;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Matches one entry across all of its fields and reports which field won, so the caller can
    ///     explain a result whose visible label has nothing to do with the query.
    /// </summary>
    /// <param name="entry">The entry to score</param>
    /// <param name="query">The user's query</param>
    /// <returns>The hit, or <see langword="null" /> when nothing matched</returns>
    private SearchHit? Score(SearchEntry entry, string query)
    {
        SearchMatch best = _strategy.Match(entry.Label, query).Scaled(LABEL_WEIGHT);
        SearchField field = SearchField.Label;
        string? matchedTerm = null;

        if (entry.Keywords != null)
        {
            foreach (string keyword in entry.Keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    continue;

                SearchMatch match = _strategy.Match(keyword, query).Scaled(KEYWORD_WEIGHT);

                if (!Beats(match, best))
                    continue;

                best = match;
                field = SearchField.Keyword;
                matchedTerm = keyword;
            }
        }

        if (!entry.Path.IsEmpty)
        {
            SearchMatch match = _strategy.Match(entry.Path.Text, query).Scaled(PATH_WEIGHT);

            if (Beats(match, best))
            {
                best = match;
                field = SearchField.Path;
                matchedTerm = BestMatchingSegment(entry.Path, query);
            }
        }

        if (!string.IsNullOrWhiteSpace(entry.MatchText))
        {
            SearchMatch match = _strategy.Match(entry.MatchText!, query).Scaled(MATCH_TEXT_WEIGHT);

            if (Beats(match, best))
            {
                best = match;
                field = SearchField.MatchText;
                matchedTerm = null;
            }
        }

        return best.IsMatch
            ? new SearchHit(entry, best.Score, field, matchedTerm, best.Spans)
            : null;
    }

    /// <summary>
    ///     The breadcrumb segment the query actually hit, for attribution.
    ///     <para>
    ///         Scoring runs against the whole breadcrumb, so the winning segment is not necessarily
    ///         the leaf: querying <c>visual</c> for something under <c>Video › Visual Effects ›
    ///         General</c> would otherwise report "matched: General", a term that doesn't contain the
    ///         query. A query spanning a separator matches no single segment and so names none.
    ///     </para>
    /// </summary>
    /// <param name="path">The entry's breadcrumb</param>
    /// <param name="query">The user's query</param>
    /// <returns>The best-matching segment, or <see langword="null" /> when none matches on its own</returns>
    private string? BestMatchingSegment(SearchPath path, string query)
    {
        string? bestSegment = null;
        double bestScore = 0d;

        foreach (string segment in path.Segments)
        {
            SearchMatch match = _strategy.Match(segment, query);

            if (!match.IsMatch || match.Score <= bestScore)
                continue;

            bestScore = match.Score;
            bestSegment = segment;
        }

        return bestSegment;
    }

    private void InvalidateCache()
    {
        _cachedQuery = null;
        _cachedHits = null;
    }

    private static bool Beats(SearchMatch candidate, SearchMatch incumbent) =>
        candidate.IsMatch && (!incumbent.IsMatch || candidate.Score > incumbent.Score);

    private static int CompareByScoreThenOrder(ScoredIndex left, ScoredIndex right)
    {
        int byScore = right.Hit.Score.CompareTo(left.Hit.Score);

        return byScore != 0 ? byScore : left.Order.CompareTo(right.Order);
    }

    #endregion

    /// <summary>Pairs a hit with its index position so sorting can fall back to declared order</summary>
    private readonly record struct ScoredIndex(SearchHit Hit, int Order);
}
