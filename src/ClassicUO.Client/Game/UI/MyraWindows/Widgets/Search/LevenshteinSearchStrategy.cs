#nullable enable

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ClassicUO.Utility;
using Myra.Utility.Search;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

public partial class LevenshteinSearchStrategy : ISearchStrategy
{
    /// <summary>Default ceiling on the edit distance a match may have, before <see cref="GetMaxDistanceForQueryLength"/> narrows it further.</summary>
    public const int DEFAULT_MAX_DISTANCE = 4;

    private static readonly Regex _wordBoundaryRegex = WordBoundaryRegex();

    public Func<int, int> GetMaxDistanceForQueryLength { get; set; } = AutoFuzziness;
    public int MaxDistance { get; set; } = DEFAULT_MAX_DISTANCE;
    public float MinScore { get; set; } = 0.6f;
    public bool PerTokenBest { get; set; }
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Splits a candidate into the tokens compared individually when <see cref="PerTokenBest"/>
    /// is set. Defaults to word-boundary splitting so punctuation doesn't get glued onto
    /// adjacent words the way a plain whitespace split would.
    /// </summary>
    public Func<string, IEnumerable<string>> Tokenizer { get; set; } = WordBoundaryTokenizer;

    public SearchMatch Match(string candidate, string query)
    {
        if (string.IsNullOrEmpty(query))
            return SearchMatch.Exact();

        if (!PerTokenBest)
            return MatchSingle(candidate, query);

        SearchMatch best = SearchMatch.None;
        foreach (string token in Tokenizer(candidate))
        {
            SearchMatch match = MatchSingle(token, query);
            if (match.IsMatch && (!best.IsMatch || match.Score > best.Score))
                best = match;
        }

        return best;
    }

    // Shallow is enough - every field is a value or an immutable delegate.
    /// <inheritdoc />
    /// <remarks>Any string is a valid edit-distance query.</remarks>
    public bool IsQueryValid(string query) => true;

    public ISearchStrategy Clone() => (ISearchStrategy)MemberwiseClone();

    public static IEnumerable<string> WordBoundaryTokenizer(string s)
    {
        foreach (Match m in _wordBoundaryRegex.Matches(s))
            yield return m.Value;
    }

    private SearchMatch MatchSingle(string candidate, string query)
    {
        string a = CaseSensitive ? candidate : candidate.ToLowerInvariant();
        string b = CaseSensitive ? query : query.ToLowerInvariant();

        // Raw edit distance against a fixed MaxDistance over-matches short queries: "ad" is
        // genuinely only 2 edits from "say", so MaxDistance=2 lets a 2-letter query match
        // almost any similarly short, unrelated word. Scale the effective cap down for short
        // queries, in the spirit of Elasticsearch's `fuzziness: AUTO` but one edit more
        // permissive at every step - see AutoFuzziness for the authoritative thresholds.
        // MaxDistance still applies as a ceiling on top of that.
        int effectiveMaxDistance = Math.Max(0, Math.Min(GetMaxDistanceForQueryLength(b.Length), MaxDistance));

        int dist = Levenshtein.Distance(a, b, effectiveMaxDistance);
        if (dist > effectiveMaxDistance)
            return SearchMatch.None;

        int denom = Math.Max(candidate.Length, query.Length);
        double score = denom == 0 ? 1d : 1d - (double)dist / denom;
        score = Math.Clamp(score, 0d, 1d);

        return score >= MinScore ? SearchMatch.Exact(score) : SearchMatch.None;
    }

    /// <summary>
    /// Edit budget allowed for a query of <paramref name="queryLength"/> characters, before
    /// <see cref="MaxDistance"/> is applied as a ceiling on top. These thresholds are the
    /// authoritative definition of the default fuzziness curve.
    /// </summary>
    private static int AutoFuzziness(int queryLength) => queryLength switch
    {
        <= 2 => 1,
        <= 5 => 2,
        _ => 3
    };

    [GeneratedRegex(@"\b\w+\b", RegexOptions.Compiled)]
    private static partial Regex WordBoundaryRegex();
}
