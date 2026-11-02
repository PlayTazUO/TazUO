#nullable enable

using System;
using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     How a search result offers itself to the user.
/// </summary>
public enum SearchPresentation
{
    /// <summary>
    ///     The entry's own editor is small and self-contained enough to be hosted directly in the
    ///     results list. Still subject to a space check at render time, which may fall back to
    ///     <see cref="Navigate" />.
    /// </summary>
    Inline,

    /// <summary>
    ///     The entry needs its real home to make sense - either it is too large for a result row,
    ///     or it depends on surrounding state (a toolbar, a selection) that only its own page has.
    ///     The row renders as a link that invokes <see cref="SearchEntry.OnActivate" />.
    /// </summary>
    Navigate
}

/// <summary>
///     One searchable thing, flattened out of whatever hierarchy produced it and annotated with
///     enough context to be displayed on its own: a label, the path it came from, and either an
///     inline editor or an activation callback.
///     <para>
///         <see cref="InlineFactory" /> is expected to return a fresh widget per call - the results
///         list may rebuild a row when the available space changes.
///     </para>
/// </summary>
/// <param name="Label">
///     Display caption for the result row. Distinct from <see cref="MatchText" /> so a group whose
///     match text is a concatenation of its children's labels still has something to show.
/// </param>
/// <param name="Path">Where this entry lives, used for grouping, breadcrumbs, and scoring</param>
/// <param name="MatchText">
///     Extra free text to match against beyond the label - descendant labels, aliases, anything the
///     producer wants discoverable. Never displayed.
/// </param>
/// <param name="Keywords">
///     Synonyms scored individually so the winning term can be reported back to the user. Matching
///     a keyword is how a query like <c>talk</c> finds a setting labelled <c>Speech</c>, which looks
///     like a bug without attribution.
/// </param>
/// <param name="InlineFactory">
///     Produces the inline editor. Required when <see cref="Presentation" /> is
///     <see cref="SearchPresentation.Inline" />; ignored otherwise.
/// </param>
/// <param name="Presentation">Whether this entry can be edited in the results list or must be navigated to</param>
/// <param name="OnActivate">
///     Invoked when the user activates a navigate-style row, or clicks through from an inline one.
/// </param>
public sealed record SearchEntry(
    string Label,
    SearchPath Path,
    string? MatchText = null,
    IReadOnlyList<string>? Keywords = null,
    Func<Widget>? InlineFactory = null,
    SearchPresentation Presentation = SearchPresentation.Navigate,
    Action? OnActivate = null
)
{
    /// <summary>
    ///     Whether this entry could be hosted inline, ignoring the space available. False when the
    ///     producer declared it navigate-only or gave no factory to render with.
    /// </summary>
    public bool CanInline => Presentation == SearchPresentation.Inline && InlineFactory != null;

    /// <summary>The full breadcrumb including the entry's own label</summary>
    public SearchPath FullPath => Path.Push(Label);
}

/// <summary>
///     Which field of a <see cref="SearchEntry" /> produced the winning match. Drives whether the
///     result row explains itself: a label hit is self-evident, the others are not.
/// </summary>
public enum SearchField
{
    Label,
    Path,
    MatchText,
    Keyword
}

/// <summary>
///     A <see cref="SearchEntry" /> paired with its score for one particular query.
/// </summary>
/// <param name="Entry">The matched entry</param>
/// <param name="Score">Weighted match quality; higher ranks first</param>
/// <param name="Field">Which field won</param>
/// <param name="MatchedTerm">
///     The specific term that matched when <see cref="Field" /> is <see cref="SearchField.Keyword" />,
///     for display as match attribution. Null otherwise.
/// </param>
/// <param name="Spans">
///     Matched character ranges within the winning field, when the strategy reported them. Carried
///     so match highlighting can be added without re-running the search; nothing renders these yet.
/// </param>
/// <param name="Order">
///     The entry's position in the index. Lets a consumer restore declared order within a group
///     after ranking has chosen the group order, so rows don't reshuffle as the user types.
/// </param>
public readonly record struct SearchHit(
    SearchEntry Entry,
    double Score,
    SearchField Field,
    string? MatchedTerm = null,
    (int Start, int Len)[]? Spans = null,
    int Order = 0
);
