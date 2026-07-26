#nullable enable

using System;
using ClassicUO.Utility;
using Myra.Utility.Search;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

public class LevenshteinSearchStrategy : ISearchStrategy
{
    public Func<int, int> GetMaxDistanceForQueryLength { get; set; } = AutoFuzziness;
    public int MaxDistance { get; set; } = 4;
    public bool PerTokenBest { get; set; }
    public bool CaseSensitive { get; set; }

    public SearchMatch Match(string candidate, string query)
    {
        if (string.IsNullOrEmpty(query))
            return SearchMatch.Exact();

        if (!PerTokenBest)
            return MatchSingle(candidate, query);

        SearchMatch best = SearchMatch.None;
        foreach (string token in candidate.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            SearchMatch match = MatchSingle(token, query);
            if (match.IsMatch && (!best.IsMatch || match.Score > best.Score))
                best = match;
        }

        return best;
    }

    private SearchMatch MatchSingle(string candidate, string query)
    {
        string a = CaseSensitive ? candidate : candidate.ToLowerInvariant();
        string b = CaseSensitive ? query : query.ToLowerInvariant();

        // Raw edit distance against a fixed MaxDistance over-matches short queries: "ad" is
        // genuinely only 2 edits from "say", so MaxDistance=2 lets a 2-letter query match
        // almost any similarly short, unrelated word. Scale the effective cap down for short
        // queries the same way Elasticsearch's `fuzziness: AUTO` does (len<=2 -> 0 edits,
        // <=5 -> 1, else 2) - MaxDistance still applies as a ceiling on top of that.
        int effectiveMaxDistance = Math.Min(GetMaxDistanceForQueryLength(b.Length), MaxDistance);

        int dist = Levenshtein.Distance(a, b, effectiveMaxDistance);
        if (dist > effectiveMaxDistance)
            return SearchMatch.None;

        int denom = Math.Max(candidate.Length, query.Length);
        double score = denom == 0 ? 1d : 1d - (double)dist / denom;
        score = Math.Clamp(score, 0d, 1d);

        return SearchMatch.Exact(score);
    }

    private static int AutoFuzziness(int queryLength) => queryLength switch
    {
        <= 2 => 1,
        <= 5 => 2,
        _ => 3
    };
}
