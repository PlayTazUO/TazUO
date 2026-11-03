#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// Immutable metadata attached to an option or option source that drives the search system.
/// <para>
/// <see cref="Label"/> is what the user sees in a search result; the remaining members are match
/// fuel and are never displayed. They are kept apart because grouped sources merge their
/// descendants' match text into one blob, which would be unusable as a caption.
/// </para>
/// </summary>
/// <param name="SearchText">
/// Free-form text matched against the user's query. Seeded from the option's visible label, then
/// extended with descendant labels when a group is merged.
/// </param>
/// <param name="Tags">
/// Category tags, historically a tab name. Structural position is now carried by the indexed
/// path instead, so these survive only as additional match fuel.
/// </param>
/// <param name="Keywords">
/// Synonyms that should find this option, e.g. <c>talk</c> for a speech setting. Scored
/// individually so the matching term can be reported back to the user.
/// </param>
public record SearchMetadata(string? SearchText = null, string[]? Tags = null, string[]? Keywords = null)
{
    /// <summary>
    /// Display caption for a search result. Defaults to <see cref="SearchText"/>, since option
    /// factories seed both from the visible label; merging preserves the innermost one rather than
    /// concatenating, so it stays a single name.
    /// </summary>
    public string? Label { get; init; } = SearchText;

    /// <summary>Tags split on commas and trimmed, as authors write them either way</summary>
    public string[] NormalizedTags => Split(Tags);

    /// <summary>Keywords split on commas and trimmed, as authors write them either way</summary>
    public string[] NormalizedKeywords => Split(Keywords);

    /// <summary>
    /// Combines two instances, keeping the innermost <see cref="SearchText"/> and deduplicating the
    /// combined tag and keyword sets.
    /// <para>
    /// <see cref="Label"/> is deliberately <b>not</b> inherited: it names one specific option, so
    /// taking an ancestor's would give a name to everything that has none of its own - spacers and
    /// decorative widgets would be indexed under their section's title and surface as blank rows.
    /// Match text and keywords still propagate, since those are only ever matched against.
    /// </para>
    /// </summary>
    /// <param name="a">The inner metadata; its text and label take precedence</param>
    /// <param name="b">The outer metadata merged into the result</param>
    /// <returns>A new instance that is the union of both inputs</returns>
    public static SearchMetadata Merge(SearchMetadata? a, SearchMetadata? b)
    {
        string[] concatTags = [.. a?.Tags ?? [], .. b?.Tags ?? []];
        string[] concatKeywords = [.. a?.Keywords ?? [], .. b?.Keywords ?? []];

        return new SearchMetadata(
            a?.SearchText ?? b?.SearchText,
            concatTags.Distinct().ToArray(),
            concatKeywords.Distinct().ToArray()
        )
        {
            Label = a?.Label
        };
    }

    private static string[] Split(string[]? values) =>
        values == null
            ? []
            : values
                .SelectMany(value => value.Split(',').Select(part => part.Trim()))
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();
}
