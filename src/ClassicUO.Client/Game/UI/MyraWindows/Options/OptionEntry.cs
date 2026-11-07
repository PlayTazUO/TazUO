#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// A leaf node in the options tree: wraps a lazily-created widget and carries
/// <see cref="SearchMetadata"/> so the search system can locate it.
/// <para>
/// An entry with no <see cref="SearchMetadata.Label"/> is deliberately unsearchable - spacers and
/// decorative content pass no metadata and are skipped by <see cref="Index"/>.
/// </para>
/// </summary>
/// <param name="RenderFactory">Factory invoked to create the underlying widget; returns a new instance per call</param>
/// <param name="Search">Search metadata that controls when this entry appears in search results</param>
internal sealed record OptionEntry(Func<Widget> RenderFactory, SearchMetadata? Search = null) : IOptionSource
{
    /// <inheritdoc/>
    public bool InheritsSearch { get; set; } = true;

    /// <summary>
    /// Whether this entry's widget is compact enough to be edited directly in a search result row.
    /// Defaults to <see cref="SearchPresentation.Inline"/> because the typed <c>Option.*</c>
    /// factories all produce a label plus a single bounded control; <c>Option.Custom</c> opts out,
    /// as do entries whose editor needs its own page's surrounding state.
    /// </summary>
    public SearchPresentation Presentation { get; set; } = SearchPresentation.Inline;

    /// <summary>
    /// Renders the option widget by invoking the factory. Each call returns a new widget instance.
    /// </summary>
    public Widget Render() => RenderFactory();

    /// <inheritdoc/>
    public IEnumerable<SearchEntry> Index(OptionIndexContext context)
    {
        SearchMetadata? merged = InheritsSearch ? SearchMetadata.Merge(Search, context.Inherited) : Search;

        if (merged?.Label is not { Length: > 0 } label)
            yield break;

        yield return new SearchEntry(
            label,
            context.Path,
            MatchText: merged.SearchText,
            Keywords: merged.MatchTerms,
            InlineFactory: RenderFactory,
            Presentation: Presentation
        );
    }

    /// <summary>Marks this entry as editable inside a search result row and returns itself</summary>
    public OptionEntry AsInlineable()
    {
        Presentation = SearchPresentation.Inline;
        return this;
    }

    /// <summary>
    /// Marks this entry as navigate-only, so search results link to its real page instead of
    /// hosting it, and returns itself
    /// </summary>
    public OptionEntry AsLinkOnly()
    {
        Presentation = SearchPresentation.Navigate;
        return this;
    }
}
