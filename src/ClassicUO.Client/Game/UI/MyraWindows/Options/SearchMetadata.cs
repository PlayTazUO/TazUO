#nullable enable
using System;
using System.Linq;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// Immutable metadata attached to an option or option source that drives the search system.
/// An option matches a user query if its <see cref="SearchText"/> contains the query string,
/// or if any of its <see cref="Tags"/> or <see cref="Keywords"/> overlap with those in the query.
/// </summary>
/// <param name="SearchText">
/// Free-form text checked for a substring match against the user's search string.
/// Typically the option's visible label.
/// </param>
/// <param name="Tags">
/// Category tags (e.g. a tab name) used for structural filtering.
/// An option matches when any of its tags appears in the query's tag set.
/// </param>
/// <param name="Keywords">
/// Secondary terms used for keyword-based discovery.
/// An option matches when any of its keywords appears in the query's keyword set.
/// </param>
public record SearchMetadata(string? SearchText = null, string[]? Tags = null, string[]? Keywords = null)
{
    private string[]? NormalizedTags => Tags?.SelectMany(t => t.Split(',').Select(s => s.Trim())).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    private string[]? NormalizedKeywords => Keywords?.SelectMany(k => k.Split(',').Select(s => s.Trim())).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

    /// <summary>
    /// Returns <see langword="true"/> when this metadata satisfies the given <paramref name="search"/> query.
    /// A match occurs if the search text is found as a substring of <see cref="SearchText"/>,
    /// or if any tag or keyword in this metadata intersects with those in <paramref name="search"/>.
    /// </summary>
    /// <param name="search">The query to match against</param>
    /// <returns>Whether this metadata satisfies the query</returns>
    public bool Matches(SearchMetadata search)
    {
        if (search.SearchText != null)
        {
            if (SearchText?.Contains(search.SearchText, StringComparison.InvariantCultureIgnoreCase) == true)
                return true;

            if (NormalizedTags?.Any(tag => search.SearchText.Contains(tag, StringComparison.InvariantCultureIgnoreCase)) == true)
                return true;

            if (NormalizedKeywords?.Any(keyword => search.SearchText.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)) == true)
                return true;
        }

        if (NormalizedTags?.Length > 0 && search.Tags?.Length > 0)
            return search.Tags.ContainsAny(NormalizedTags);

        if (NormalizedKeywords?.Length > 0 && search.Keywords?.Length > 0)
            return search.Keywords.ContainsAny(NormalizedKeywords);

        return false;
    }

    /// <summary>
    /// Combines two <see cref="SearchMetadata"/> instances into one, taking the search text from
    /// <paramref name="a"/> first (falling back to <paramref name="b"/>) and deduplicating the
    /// combined tag and keyword sets
    /// </summary>
    /// <param name="a">The primary metadata; its search text takes precedence</param>
    /// <param name="b">The secondary metadata merged into the result</param>
    /// <returns>A new <see cref="SearchMetadata"/> that is the union of both inputs</returns>
    public static SearchMetadata Merge(SearchMetadata? a, SearchMetadata? b)
    {
        string? finalSearchText = a?.SearchText ?? b?.SearchText;
        string[] concatTags = [.. a?.Tags ?? [], .. b?.Tags ?? []];
        string[] concatKeywords = [.. a?.Keywords ?? [], .. b?.Keywords ?? []];

        return new SearchMetadata(finalSearchText, concatTags.Distinct().ToArray(), concatKeywords.Distinct().ToArray());
    }
}
