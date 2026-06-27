#nullable enable

using System;
using System.Collections.Generic;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// A leaf node in the options tree: wraps a lazily-created widget and carries
/// <see cref="SearchMetadata"/> so the search system can locate it
/// </summary>
/// <param name="RenderFactory">Factory invoked once to create the underlying widget</param>
/// <param name="Search">Search metadata that controls when this entry appears in search results</param>
internal sealed record OptionEntry(Func<Widget> RenderFactory, SearchMetadata? Search = null) : IOptionSource
{
    /// <inheritdoc/>
    public bool InheritsSearch { get; set; } = true;

    /// <summary>
    /// Renders the option widget by invoking the factory. Each call returns a new widget instance.
    /// </summary>
    public Widget Render() => RenderFactory();

    /// <summary>
    /// Yields this entry if its own <see cref="Search"/> metadata matches <paramref name="search"/>
    /// </summary>
    /// <param name="search">The search criteria to evaluate</param>
    /// <returns>This entry if it matches; otherwise an empty sequence</returns>
    public IEnumerable<OptionEntry> Match(SearchMetadata search)
    {
        if (Search?.Matches(search) == true)
            yield return this;
    }

    /// <summary>
    /// Returns this entry with its search metadata merged with <paramref name="inheritedSearch"/>
    /// when <see cref="InheritsSearch"/> is <see langword="true"/>
    /// </summary>
    /// <param name="inheritedSearch">Search metadata propagated from a parent node</param>
    /// <returns>A copy of this entry carrying the merged metadata</returns>
    public IEnumerable<OptionEntry> GetOptions(SearchMetadata? inheritedSearch = null)
    {
        yield return this with { Search = InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search };
    }
}
