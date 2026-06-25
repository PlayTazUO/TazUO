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
    private Widget? _cachedWidget;

    /// <inheritdoc/>
    public SearchMetadata? Search { get; set; }

    /// <inheritdoc/>
    public bool InheritsSearch { get; set; } = true;

    /// <summary>
    /// Renders the fragment's container widget, creating it on first call and caching it for subsequent calls
    /// </summary>
    /// <returns>The cached container widget</returns>
    public Widget Render() => _cachedWidget ??= renderFactory();

    /// <inheritdoc/>
    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        SearchMetadata? finalSearch = InheritsSearch ? SearchMetadata.Merge(Search, search) : Search;
        return finalSearch == null
            ? []
            : children.SelectMany(c => c.Match(finalSearch));
    }

    /// <inheritdoc/>
    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        SearchMetadata? merged = InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search;
        return children.SelectMany(c => c.GetOptions(merged));
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
