#nullable enable
using System;
using System.Linq;

namespace ClassicUO.Game.UI.MyraWindows.Options;

public record SearchMetadata(string? SearchText = null, string[]? Tags = null, string[]? Keywords = null)
{
    private string[]? NormalizedTags => Tags?.SelectMany(t => t.Split(',').Select(s => s.Trim())).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    private string[]? NormalizedKeywords => Keywords?.SelectMany(k => k.Split(',').Select(s => s.Trim())).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

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

    public static SearchMetadata Merge(SearchMetadata? a, SearchMetadata? b)
    {
        string? finalSearchText = a?.SearchText ?? b?.SearchText;
        string[] concatTags = [.. a?.Tags ?? [], .. b?.Tags ?? []];
        string[] concatKeywords = [.. a?.Keywords ?? [], .. b?.Keywords ?? []];

        return new SearchMetadata(finalSearchText, concatTags.Distinct().ToArray(), concatKeywords.Distinct().ToArray());
    }
}
