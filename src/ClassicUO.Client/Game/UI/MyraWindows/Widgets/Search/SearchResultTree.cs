#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
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
    #region Events

    /// <summary>
    ///     Raised when the user activates a result that must be navigated to, or clicks a breadcrumb
    ///     segment on a group header. The argument is the path to open.
    ///     <para>
    ///         Only reached for an entry that carries no <see cref="SearchEntry.OnActivate" /> of its
    ///         own, which takes precedence.
    ///     </para>
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

    /// <summary>
    ///     Freezes the list's layout: measure reports the size it last settled on, and the rows keep
    ///     the bounds they were last arranged into. They stay on screen and keep drawing; they simply
    ///     stop reflowing.
    ///     <para>
    ///         Meant for the span of a window drag. Myra re-measures this whole subtree every frame -
    ///         its own measure cache cannot help, because the cached available size is recorded after
    ///         padding and border are subtracted but compared against the value before them, so any
    ///         widget with padding misses every time - and the cost is linear in row count.
    ///     </para>
    ///     <para>
    ///         While frozen the list is cropped rather than reflowed if the window shrinks, and
    ///         hit-testing uses the stale bounds. Both are only observable mid-drag, while the pointer
    ///         belongs to the resize handle.
    ///     </para>
    /// </summary>
    public bool IsLayoutFrozen
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;

            // Thawing has to re-run the layout that was skipped: nothing marked the subtree dirty
            // while it was frozen, so Myra has no reason to revisit it.
            if (!value)
                InvalidateMeasure();
        }
    }

    #endregion

    #region Members

    #region Consts/Readonly

    /// <summary>
    ///     Slack added to <see cref="InlineThreshold" /> before an already-inlined row is demoted.
    ///     Without it, a row sitting exactly on the threshold flips back and forth while the window
    ///     edge is dragged.
    /// </summary>
    private const double INLINE_HYSTERESIS = 0.1;

    /// <summary>
    ///     Result text is drawn below the user's configured size: this is a dense index of settings
    ///     rather than a page of them, and the rows read better tighter. Offsets, not absolute sizes,
    ///     so the user's own choice still drives it.
    /// </summary>
    private const int CRUMB_FONT_OFFSET = -1;

    private const int ROW_FONT_OFFSET = -2;

    /// <summary>Breathing room so rows are not flush against the window edge.</summary>
    private const int CONTENT_PADDING_X = 8;

    private const int CONTENT_PADDING_Y = 4;

    /// <summary>Gap between a breadcrumb segment and its separator.</summary>
    private const int CRUMB_SPACING = 2;

    /// <summary>Gap above the empty-state message, so it does not sit flush against the query box.</summary>
    private const int EMPTY_STATE_SPACING = 8;

    private readonly VerticalStackPanel _stack = new() { Spacing = 1 };
    private readonly HashSet<string> _collapsed = [];
    private readonly List<SearchResultRow> _rows = [];

    /// <summary>
    ///     Held by the tree rather than by its rows: rows are rebuilt per query, and re-measuring
    ///     every editor on every keystroke is the single most expensive thing search does.
    /// </summary>
    private readonly InlineSizeCache _inlineSizes = new();

    #endregion

    #region Mutables

    /// <summary>
    ///     The size reported by the last measure that actually ran, replayed while
    ///     <see cref="IsLayoutFrozen" /> is set. Null until the list has been measured once.
    /// </summary>
    private Point? _settledSize;

    #endregion

    #endregion

    #region Ctor

    public SearchResultTree()
    {
        Content = _stack;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Padding = new Thickness(CONTENT_PADDING_X, CONTENT_PADDING_Y, CONTENT_PADDING_X, CONTENT_PADDING_Y);
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

        if (hits.Count == 0)
        {
            _stack.Widgets.Add(BuildEmptyState());
            return;
        }

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
            if (row.ApplyPresentation(AvailableWidth, InlineThreshold, INLINE_HYSTERESIS, focused))
                changed++;

        return changed;
    }

    /// <summary>Current vertical scroll offset, for snapshotting the view</summary>
    /// <returns>The offset in pixels</returns>
    public int GetScrollOffset() => ScrollPosition.Y;

    /// <summary>Restores a vertical scroll offset captured by <see cref="GetScrollOffset" /></summary>
    /// <param name="offset">The offset in pixels; clamped by the scroller</param>
    public void SetScrollOffset(int offset) => ScrollPosition = new Point(ScrollPosition.X, Math.Max(offset, 0));

    #endregion

    #region Protected Methods

    /// <inheritdoc />
    protected override Point InternalMeasure(Point availableSize)
    {
        if (IsLayoutFrozen && _settledSize.HasValue)
            return _settledSize.Value;

        Point measured = base.InternalMeasure(availableSize);
        _settledSize = measured;

        return measured;
    }

    /// <inheritdoc />
    protected override void InternalArrange()
    {
        // Re-arranging re-measures the content, which is the cost being avoided; the rows keep the
        // bounds they already have.
        if (IsLayoutFrozen)
            return;

        base.InternalArrange();
    }

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

        // No header fill at all. Even one flat tint differs from the panel behind it, which
        // striped the list; the twisty and the indent already show the structure. Safe to clear
        // only because the twisty is the sole toggle here - a header that handles its own clicks
        // needs a background to receive them.
        expander.Header.Background = null;
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
            expander.Items.Add(BuildRow(hit, depth + 1));

        foreach (SearchResultGroup child in group.Children)
            expander.Items.Add(BuildGroup(child, depth + 1));

        return expander;
    }

    private Widget BuildRowStack(SearchResultGroup group)
    {
        var stack = new VerticalStackPanel { Spacing = 1, HorizontalAlignment = HorizontalAlignment.Stretch };

        foreach (SearchHit hit in group.Hits)
            stack.Widgets.Add(BuildRow(hit, 0));

        return stack;
    }

    /// <summary>
    ///     The header's breadcrumb and hit count. Segments are links rather than buttons: a row of
    ///     boxed buttons reads as a toolbar, not as a location.
    /// </summary>
    private Widget BuildHeaderContent(SearchResultGroup group)
    {
        var crumbs = new HorizontalStackPanel { Spacing = CRUMB_SPACING, VerticalAlignment = VerticalAlignment.Center };
        int ancestors = group.Path.Length - group.Segments.Count;

        for (int i = 0; i < group.Segments.Count; i++)
        {
            if (i > 0)
                crumbs.Widgets.Add(BuildSeparator());

            SearchPath target = group.Path.Take(ancestors + i + 1);

            // Every segment at every depth gets the same treatment - a breadcrumb is one
            // continuous trail, so varying the size by depth just looks like inconsistency.
            var crumb = new LinkLabel(
                group.Segments[i],
                () => NavigationRequested?.Invoke(this, target)
            ) { Wrap = false, SingleLine = true, VerticalAlignment = VerticalAlignment.Center };

            crumb.Font = MyraStyle.GetUiFont(CRUMB_FONT_OFFSET);
            crumbs.Widgets.Add(crumb);
        }

        return new SpaceBetweenRow(
            crumbs,
            MyraLabel.Hint(group.TotalHits.ToString()),
            MyraStyle.STANDARD_SPACING * 2
        );
    }

    private static MyraLabel BuildSeparator() => MyraLabel.Hint(SearchPath.SEPARATOR);

    /// <summary>
    ///     What a query with no matches shows. An empty panel reads as a list that failed to render
    ///     rather than as an answer to the query.
    /// </summary>
    private static Widget BuildEmptyState()
    {
        var message = MyraLabel.Hint(TazLang.Get("optsearch_no_results"));

        message.HorizontalAlignment = HorizontalAlignment.Center;
        message.Margin = new Thickness(0, EMPTY_STATE_SPACING, 0, 0);

        return message;
    }

    private Widget BuildRow(SearchHit hit, int depth)
    {
        var row = new SearchResultRow(hit, ShowAttribution, OnRowActivated, _inlineSizes)
        {
            FontOffset = ROW_FONT_OFFSET,
            // Each nesting level insets the row, so a deep row has measurably less to work with
            // than the list as a whole. Without this the guard budgets for width the row never had.
            IndentReserve = depth * Expander.DEFAULT_INDENT + CONTENT_PADDING_X * 2
        };

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
