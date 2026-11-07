#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// A composite <see cref="IOptionSource"/> that renders its children into a single containing widget.
/// Useful for grouping related options under a shared layout without introducing a tab boundary.
/// </summary>
/// <param name="renderFactory">Factory that produces the container widget for this fragment's children</param>
/// <param name="children">The child option slots contained within this fragment</param>
internal sealed class OptionFragment(Func<Widget> renderFactory, IEnumerable<OptionContent> children) : IOptionSource
{
    /// <inheritdoc/>
    public SearchMetadata? Search { get; set; }

    /// <inheritdoc/>
    public bool InheritsSearch { get; set; } = true;

    /// <summary>
    /// This fragment's own name. Contributes a breadcrumb segment for a plain fragment, or the
    /// result row's caption when <see cref="IsSearchGroup"/> is set - where it outranks
    /// <see cref="SearchMetadata.Label"/>. Typically a visual container's caption or a gating
    /// checkbox's, so both breadcrumb and caption match what the user already reads on the page.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// When <see langword="true"/>, a search hit against any descendant returns this fragment
    /// as a single rendered unit rather than individual leaf entries.
    /// </summary>
    public bool IsSearchGroup { get; private set; }

    /// <summary>
    /// How this fragment offers itself in search results when it is a search group. Defaults to
    /// <see cref="SearchPresentation.Navigate"/>: a group's size depends on its child count, and
    /// several wrap editors that only work alongside their page's toolbar.
    /// </summary>
    public SearchPresentation Presentation { get; set; } = SearchPresentation.Navigate;

    /// <summary>
    /// Renders the fragment's container widget. Each call returns a new widget instance.
    /// </summary>
    public Widget Render() => renderFactory();

    /// <inheritdoc/>
    public IEnumerable<SearchEntry> Index(OptionIndexContext context)
    {
        SearchMetadata? own = InheritsSearch ? SearchMetadata.Merge(Search, context.Inherited) : Search;

        if (IsSearchGroup)
        {
            SearchEntry? entry = BuildGroupEntry(context, own);

            if (entry != null)
                yield return entry;

            yield break;
        }

        OptionIndexContext childContext = context.Descend(Label).WithInherited(own);

        foreach (SearchEntry entry in children.SelectMany(child => child.Index(childContext)))
            yield return entry;
    }

    /// <summary>Marks this fragment as a search group and returns itself for fluent chaining</summary>
    public OptionFragment AsSearchGroup()
    {
        IsSearchGroup = true;
        return this;
    }

    /// <summary>Marks this fragment as a standalone node, i.e., one with search independent of its children and returns itself for fluent chaining</summary>
    public OptionFragment AsStandalone()
    {
        IsSearchGroup = false;
        return this;
    }

    /// <summary>Attaches <paramref name="search"/> as this fragment's own search metadata and returns itself</summary>
    /// <param name="search">The metadata to attach</param>
    /// <returns>This fragment, for fluent chaining</returns>
    public OptionFragment WithSearch(SearchMetadata search)
    {
        Search = search;
        return this;
    }

    /// <summary>Sets this fragment's breadcrumb name and returns itself for fluent chaining</summary>
    /// <param name="label">The name to expose</param>
    /// <returns>This fragment, for fluent chaining</returns>
    public OptionFragment WithLabel(string? label)
    {
        Label = label;
        return this;
    }

    /// <summary>Marks this fragment as hostable inside a search result row and returns itself</summary>
    public OptionFragment AsInlineable()
    {
        Presentation = SearchPresentation.Inline;
        return this;
    }

    /// <summary>
    /// Marks this fragment as navigate-only, so search results link to its real page instead of
    /// hosting it, and returns itself
    /// </summary>
    public OptionFragment AsLinkOnly()
    {
        Presentation = SearchPresentation.Navigate;
        return this;
    }

    /// <summary>
    /// Collapses the whole subtree into one entry whose match text spans every descendant label, so
    /// the group surfaces for a query naming any of its children while still presenting as a unit.
    /// </summary>
    /// <param name="context">The accumulated path and inherited metadata</param>
    /// <param name="own">This fragment's effective metadata</param>
    /// <returns>The group entry, or <see langword="null"/> when nothing names it</returns>
    private SearchEntry? BuildGroupEntry(OptionIndexContext context, SearchMetadata? own)
    {
        SearchMetadata? merged = children
            .SelectMany(child => child.Index(new OptionIndexContext(SearchPath.Empty)))
            .Select(ToMetadata)
            .Aggregate(own, DeepMerge);

        // The container/binder's own name wins: it is the caption the user already reads on the
        // page, so a result row naming the group anything else is a row they cannot place. Metadata
        // ranks below it because SearchMetadata.Label defaults to SearchText - passing match text
        // positionally would otherwise retitle the group, which is how three separate groups came
        // to be captioned after their tab. Override a caption with WithLabel, not with metadata.
        string? label = Label ?? Search?.Label ?? merged?.Label;

        if (string.IsNullOrEmpty(label))
            return null;

        return new SearchEntry(
            label,
            context.Path,
            MatchText: merged?.SearchText,
            Keywords: merged?.MatchTerms,
            InlineFactory: Render,
            Presentation: Presentation
        );
    }

    /// <summary>Projects an already-flattened descendant back into metadata for merging</summary>
    private static SearchMetadata ToMetadata(SearchEntry entry) =>
        new(entry.MatchText ?? entry.Label, Keywords: entry.Keywords?.ToArray())
        {
            Label = entry.Label
        };

    /// <summary>
    /// Like <see cref="SearchMetadata.Merge"/> but concatenates both <c>SearchText</c> values
    /// so that any descendant label remains substring-searchable from the group entry.
    /// <see cref="SearchMetadata.Label"/> is not concatenated - the accumulator's own name wins, and
    /// a group with no name of its own borrows its first child's.
    /// </summary>
    private static SearchMetadata DeepMerge(SearchMetadata? a, SearchMetadata b)
    {
        string? combinedText = a?.SearchText == null ? b.SearchText
            : b.SearchText == null ? a.SearchText
            : $"{a.SearchText} {b.SearchText}";
        string[] tags = [.. a?.Tags ?? [], .. b.Tags ?? []];
        string[] keywords = [.. a?.Keywords ?? [], .. b.Keywords ?? []];

        return new SearchMetadata(combinedText, tags.Distinct().ToArray(), keywords.Distinct().ToArray())
        {
            Label = a?.Label ?? b.Label
        };
    }
}
