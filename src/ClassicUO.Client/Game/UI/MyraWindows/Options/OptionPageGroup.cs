#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// An <see cref="IOptionSource"/> that presents multiple pages of options using a
/// <see cref="PageControl"/>, where each page is produced by a separate factory.
/// A labelled page contributes a breadcrumb segment; unlabelled pages are transparent, so their
/// contents appear directly under the group's own path.
/// </summary>
/// <param name="search">Optional search metadata for this group</param>
internal sealed class OptionPageGroup(SearchMetadata? search = null) : IOptionSource
{
    private readonly List<OptionPageDefinition> _pages = [];

    /// <inheritdoc/>
    public SearchMetadata? Search { get; init; } = search;

    /// <inheritdoc/>
    public bool InheritsSearch { get; set; } = true;

    public int? MinWidth { get; set; }
    public int? MinHeight { get; set; }

    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }

    /// <param name="search">Optional search metadata for this group</param>
    /// <param name="pages">Factories producing the initial set of pages, added in order</param>
    public OptionPageGroup(SearchMetadata? search, params Func<IOptionSource>[] pages) : this(search)
    {
        foreach (Func<IOptionSource> page in pages)
            AddPage(page);
    }

    /// <summary>Appends a page to this group and returns this group for fluent chaining</summary>
    /// <param name="contentFactory">Factory that produces the page's option source</param>
    /// <param name="label">
    /// Optional breadcrumb name for the page. The <see cref="PageControl"/> itself shows page
    /// numbers rather than names, so this exists purely to locate the page's options in search.
    /// </param>
    /// <returns>This group</returns>
    public OptionPageGroup AddPage(Func<IOptionSource> contentFactory, string? label = null)
    {
        _pages.Add(new OptionPageDefinition(contentFactory, label));
        return this;
    }

    /// <summary>Sets the rendered control's size bounds and returns this group for fluent chaining</summary>
    /// <param name="minWidth">Minimum width, or <see langword="null"/> to leave unset</param>
    /// <param name="minHeight">Minimum height, or <see langword="null"/> to leave unset</param>
    /// <param name="maxWidth">Maximum width, or <see langword="null"/> to leave unset</param>
    /// <param name="maxHeight">Maximum height, or <see langword="null"/> to leave unset</param>
    /// <returns>This group</returns>
    public OptionPageGroup WithSize(int? minWidth = null, int? minHeight = null, int? maxWidth = null, int? maxHeight = null)
    {
        MinWidth = minWidth ?? MinWidth;
        MinHeight = minHeight ?? MinHeight;
        MaxWidth = maxWidth ?? MaxWidth;
        MaxHeight = maxHeight ?? MaxHeight;

        return this;
    }

    /// <summary>
    /// Renders all pages and wraps them in a <see cref="PageControl"/>.
    /// Each call returns a new widget instance.
    /// </summary>
    public Widget Render() => BuildPageControl();

    /// <inheritdoc/>
    public IEnumerable<SearchEntry> Index(OptionIndexContext context)
    {
        SearchMetadata? search = GetSearchMeta(context.Inherited);

        foreach (OptionPageDefinition page in _pages)
        {
            OptionIndexContext pageContext = context.Descend(page.Label).WithInherited(search);

            foreach (SearchEntry entry in page.ContentFactory().Index(pageContext))
                yield return entry;
        }
    }

    private SearchMetadata? GetSearchMeta(SearchMetadata? inheritedSearch) => InheritsSearch ? SearchMetadata.Merge(Search, inheritedSearch) : Search;


    private PageControl BuildPageControl()
    {
        var widgets = new Widget[_pages.Count];
        var labels = new string[_pages.Count];

        for (int i = 0; i < _pages.Count; i++)
        {
            widgets[i] = _pages[i].ContentFactory().Render();
            labels[i] = _pages[i].Label ?? string.Empty;
        }

        return new PageControl(widgets)
        {
            PageLabels = labels,
            RetainSizeWhenPaging = true,
            MinHeight = MinHeight,
            MinWidth = MinWidth,
            MaxHeight = MaxHeight,
            MaxWidth = MaxWidth,
        };
    }

    private readonly record struct OptionPageDefinition(Func<IOptionSource> ContentFactory, string? Label);
}
