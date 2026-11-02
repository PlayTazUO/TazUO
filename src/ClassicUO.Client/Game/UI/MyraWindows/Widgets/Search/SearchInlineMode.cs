#nullable enable

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     How eagerly search results host an entry's own editor rather than linking to its page.
///     Each value is a fraction of the available row width an editor may claim and still be inlined.
/// </summary>
public enum SearchInlineMode
{
    /// <summary>
    ///     Never inline; every result is a breadcrumb plus a link. Keeps the results list identically
    ///     compact at any window size, which is what makes a very narrow window usable.
    /// </summary>
    Never,

    /// <summary>Inline only clearly small controls</summary>
    Conservative,

    /// <summary>Inline anything that comfortably fits</summary>
    Balanced,

    /// <summary>Inline anything that fits at all</summary>
    Aggressive
}

/// <summary>Maps <see cref="SearchInlineMode" /> onto the width fraction the results list uses</summary>
public static class SearchInlineModeExtensions
{
    /// <summary>
    ///     Fraction of the available row width an editor may claim and still be inlined, or zero to
    ///     disable inlining.
    /// </summary>
    /// <param name="mode">The configured mode</param>
    /// <returns>The threshold fraction</returns>
    public static double ToThreshold(this SearchInlineMode mode) =>
        mode switch
        {
            SearchInlineMode.Conservative => 0.40,
            SearchInlineMode.Balanced => 0.65,
            SearchInlineMode.Aggressive => 0.95,
            _ => 0d
        };
}
