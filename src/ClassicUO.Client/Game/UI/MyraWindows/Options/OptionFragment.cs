#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// When <see langword="true"/>, a search hit against any descendant returns this fragment
    /// as a single rendered unit rather than individual leaf entries.
    /// </summary>
    public bool IsSearchGroup { get; private set; }

    /// <summary>
    /// Renders the fragment's container widget. Each call returns a new widget instance.
    /// </summary>
    public Widget Render() => renderFactory();

    /// <inheritdoc/>
    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        SearchMetadata? searchParams = InheritsSearch ? SearchMetadata.Merge(Search, search) : Search;
        if (searchParams == null)
            yield break;

        if (IsSearchGroup)
        {
            if (children.Any(c => c.Match(searchParams).Any()))
                yield return new OptionEntry(Render, Search);
            yield break;
        }

        foreach (OptionEntry entry in children.SelectMany(c => c.Match(searchParams)))
            yield return entry;
    }

    /// <inheritdoc/>
    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        if (IsSearchGroup)
        {
            SearchMetadata? ownMeta = InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search;
            SearchMetadata? groupMeta = children
                .SelectMany(c => c.GetOptions(null))
                .Select(e => e.Search)
                .OfType<SearchMetadata>()
                .Aggregate(ownMeta, DeepMerge);
            yield return new OptionEntry(Render, groupMeta ?? new SearchMetadata());
            yield break;
        }

        SearchMetadata? merged = InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search;
        foreach (OptionEntry entry in children.SelectMany(c => c.GetOptions(merged)))
            yield return entry;
    }

    /// <summary>
    /// Like <see cref="SearchMetadata.Merge"/> but concatenates both <c>SearchText</c> values
    /// so that any descendant label remains substring-searchable from the group entry.
    /// </summary>
    private static SearchMetadata DeepMerge(SearchMetadata? a, SearchMetadata b)
    {
        string? combinedText = a?.SearchText == null ? b.SearchText
            : b.SearchText == null ? a.SearchText
            : $"{a.SearchText} {b.SearchText}";
        string[] tags = [.. a?.Tags ?? [], .. b.Tags ?? []];
        string[] keywords = [.. a?.Keywords ?? [], .. b.Keywords ?? []];
        return new SearchMetadata(combinedText, tags.Distinct().ToArray(), keywords.Distinct().ToArray());
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
}
