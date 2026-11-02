#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.MyraWindows.Theme;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     Displays <see cref="SearchHit" />s as a scrolling tree of <see cref="Expander" />s grouped by
///     their paths, where each leaf either hosts its own editor or links to the page that owns it.
///     <para>
///         Owns its scroller so the host's query box and navigation chrome stay fixed while results
///         scroll. Expanded state and scroll offset are exposed so a caller can snapshot and restore
///         the view - returning from a navigation detour should land back on the same list, not on a
///         freshly re-run search.
///     </para>
///     <para>
///         <see cref="AvailableWidth" /> must be supplied by the host: a row's presentation is decided
///         before layout runs, so the tree cannot read its own bounds in time.
///     </para>
/// </summary>
public sealed class SearchResultTree : ScrollViewer
{
    #region Members

    #region Consts/Readonly

    /// <summary>
    ///     Slack added to <see cref="InlineThreshold" /> before an already-inlined row is demoted.
    ///     Without it, a row sitting exactly on the threshold flips back and forth while the window
    ///     edge is dragged.
    /// </summary>
    private const double INLINE_HYSTERESIS = 0.1;

    private readonly VerticalStackPanel _stack = new() { Spacing = 1 };
    private readonly HashSet<string> _collapsed = [];
    private readonly List<SearchResultRow> _rows = [];

    #endregion

    #endregion

    #region Events

    /// <summary>
    ///     Raised when the user activates a result that must be navigated to, or clicks a breadcrumb
    ///     segment on a group header. The argument is the path to open.
    /// </summary>
    public event EventHandler<SearchPath>? NavigationRequested;

    #endregion

    #region Public Accessors

    /// <summary>
    ///     Width a result row has to work with. Set by the host from its own content bounds, since
    ///     presentation is decided before the tree is laid out.
    /// </summary>
    public int AvailableWidth { get; set; }

    /// <summary>
    ///     Fraction of <see cref="AvailableWidth" /> an editor may claim and still be hosted inline.
    ///     Zero turns inlining off entirely, which is also how a very small window stays usable.
    /// </summary>
    public double InlineThreshold { get; set; } = 0.65;

    /// <summary>Whether rows explain matches that did not come from the entry's label</summary>
    public bool ShowAttribution { get; set; }

    /// <summary>Number of hits currently displayed</summary>
    public int ResultCount { get; private set; }

    /// <summary>
    ///     Paths of the collapsed groups. Mutable so a caller can restore a snapshot before calling
    ///     <see cref="SetResults" />.
    /// </summary>
    public ISet<string> CollapsedPaths => _collapsed;

    #endregion

    #region Ctor

    public SearchResultTree()
    {
        Content = _stack;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Replaces the displayed results. Collapse state is kept across calls so retyping a query
    ///     doesn't re-expand everything the user just folded away.
    /// </summary>
    /// <param name="hits">The hits to display</param>
    public void SetResults(IReadOnlyList<SearchHit> hits)
    {
        ResultCount = hits.Count;

        _stack.Widgets.Clear();
        _rows.Clear();

        foreach (SearchResultGroup group in SearchResultGroup.Build(hits))
            _stack.Widgets.Add(BuildGroup(group, 0));

        ApplyPresentation();
    }

    /// <summary>
    ///     Re-decides inline versus link for every row against the current
    ///     <see cref="AvailableWidth" /> and <see cref="InlineThreshold" />, rebuilding only the rows
    ///     whose presentation actually changed.
    /// </summary>
    /// <returns>Number of rows rebuilt</returns>
    public int ApplyPresentation()
    {
        Widget? focused = Desktop?.FocusedKeyboardWidget;
        int changed = 0;

        foreach (SearchResultRow row in _rows)
        {
            if (row.ApplyPresentation(AvailableWidth, InlineThreshold, INLINE_HYSTERESIS, focused))
                changed++;
        }

        return changed;
    }

    /// <summary>Current vertical scroll offset, for snapshotting the view</summary>
    /// <returns>The offset in pixels</returns>
    public int GetScrollOffset() => ScrollPosition.Y;

    /// <summary>Restores a vertical scroll offset captured by <see cref="GetScrollOffset" /></summary>
    /// <param name="offset">The offset in pixels; clamped by the scroller</param>
    public void SetScrollOffset(int offset) => ScrollPosition = new Point(ScrollPosition.X, Math.Max(offset, 0));

    #endregion

    #region Private Methods

    /// <summary>
    ///     Builds one group and everything beneath it. A group with no segments - the bucket for hits
    ///     that carry no path at all - contributes its rows directly, with no expander to fold.
    /// </summary>
    private Widget BuildGroup(SearchResultGroup group, int depth)
    {
        if (group.Segments.Count == 0)
            return BuildRowStack(group);

        var expander = new Expander(BuildHeaderContent(group))
        {
            // The header carries breadcrumb links; a click there must follow the link, not fold the
            // group. The twisty remains the toggle.
            ToggleOnHeaderClick = false
        };

        // One flat header fill at every depth. Tinting per level made the headers read as
        // alternating grid rows rather than as a breadcrumb; the indent already shows the nesting.
        expander.Header.Background = new SolidBrush(MyraTheme.Current.PanelFill);
        expander.SetExpandedSilently(!_collapsed.Contains(group.Path.Text));

        string key = group.Path.Text;
        expander.ExpandedChanged += (_, _) =>
        {
            if (expander.IsExpanded)
                _collapsed.Remove(key);
            else
                _collapsed.Add(key);
        };

        foreach (SearchHit hit in group.Hits)
            expander.Items.Add(BuildRow(hit));

        foreach (SearchResultGroup child in group.Children)
            expander.Items.Add(BuildGroup(child, depth + 1));

        return expander;
    }

    private Widget BuildRowStack(SearchResultGroup group)
    {
        var stack = new VerticalStackPanel { Spacing = 1, HorizontalAlignment = HorizontalAlignment.Stretch };

        foreach (SearchHit hit in group.Hits)
            stack.Widgets.Add(BuildRow(hit));

        return stack;
    }

    /// <summary>
    ///     The header's breadcrumb and hit count. Segments are links rather than buttons: a row of
    ///     boxed buttons reads as a toolbar, not as a location.
    /// </summary>
    private Widget BuildHeaderContent(SearchResultGroup group)
    {
        var crumbs = new HorizontalStackPanel { Spacing = 0, VerticalAlignment = VerticalAlignment.Center };
        int ancestors = group.Path.Length - group.Segments.Count;

        for (int i = 0; i < group.Segments.Count; i++)
        {
            if (i > 0)
                crumbs.Widgets.Add(BuildSeparator());

            SearchPath target = group.Path.Take(ancestors + i + 1);

            // Every segment at every depth gets the same treatment - a breadcrumb is one
            // continuous trail, so varying the size by depth just looks like inconsistency.
            crumbs.Widgets.Add(new LinkLabel(
                group.Segments[i],
                () => NavigationRequested?.Invoke(this, target)
            )
            {
                Wrap = false,
                SingleLine = true,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        return new SpaceBetweenRow(
            crumbs,
            MyraLabel.Hint(group.TotalHits.ToString()),
            MyraStyle.STANDARD_SPACING * 2
        );
    }

    private static MyraLabel BuildSeparator() => MyraLabel.Hint(SearchPath.SEPARATOR);

    private Widget BuildRow(SearchHit hit)
    {
        var row = new SearchResultRow(hit, ShowAttribution, OnRowActivated);

        _rows.Add(row);

        return row;
    }

    private void OnRowActivated(SearchEntry entry)
    {
        if (entry.OnActivate != null)
        {
            entry.OnActivate();
            return;
        }

        NavigationRequested?.Invoke(this, entry.Path);
    }

    #endregion
}
