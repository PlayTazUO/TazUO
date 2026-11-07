#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
///     The top-level options window that hosts category navigation, a search field, and the content
///     area. Replaces any existing <see cref="OptionsWindow" /> when opened.
///     <para>
///         Everything that changes what the content area shows goes through
///         <see cref="NavigateTo(SearchPath)" /> or <see cref="ShowSearch" />, which record the move in
///         <see cref="_history" />. That makes the back and forward buttons work across categories,
///         nested tabs, and search results alike.
///     </para>
/// </summary>
public class OptionsWindow : MyraControl
{
    #region Members

    #region Consts/Readonly

    private const int MIN_HEIGHT = 250;
    private const int MIN_WIDTH = 350;

    private const int MAX_HEIGHT = 1050;
    private const int MAX_WIDTH = 1200;

    private const int SEARCH_DEBOUNCE_MS = 500;

    /// <summary>Horizontal correction for the back arrow, whose bearings sit it off-centre.</summary>
    private const int BACK_GLYPH_NUDGE_X = -3;

    /// <summary>
    ///     Width conceded to the window frame and the results list's own vertical scrollbar when
    ///     working out how much room a result row really has.
    /// </summary>
    private const int RESULTS_CHROME_ALLOWANCE = 24;

    /// <summary>Fallback rail width, matching the category buttons' own minimum.</summary>
    private const int RAIL_WIDTH_FALLBACK = 125;


    /// <summary>
    ///     Resize fires once per mouse-move tick, and re-deciding a row's presentation rebuilds its
    ///     widget, so the re-evaluation waits for the drag to settle.
    /// </summary>
    private const int RESIZE_DEBOUNCE_MS = 300;

    private readonly List<OptionCategory> _categories = [];
    private readonly Dictionary<string, OptionCategory> _categoriesByLabel = new();
    private readonly SearchIndex _searchIndex = new();
    private readonly NavigationHistory<OptionsView> _history = new(comparer: OptionsView.LocationComparer);
    private readonly HashSet<MyraTabControl> _watchedTabControls = [];

    /// <summary>
    ///     Each rendered tab control's own location, i.e. the path up to but excluding its selected
    ///     tab. Recorded while the tree is stable so a selection change only has to look itself up,
    ///     rather than re-deriving the whole path from a tree that is mutating underneath it.
    /// </summary>
    private readonly Dictionary<MyraTabControl, SearchPath> _tabControlPaths = new();
    private readonly MyraGrid _mainArea = new();

    /// <summary>
    ///     Points out the setting a search result was followed to. Driven from <see cref="Update" />
    ///     and cancelled whenever the content area is rebuilt under it.
    /// </summary>
    private readonly OptionHighlight _highlight = new();

    #endregion

    #region Mutables

    private string _currentCategory = string.Empty;

    /// <summary>
    ///     Where the user last was before search took over the content area. Search blanks the
    ///     current category, so this is what clearing the query box returns to.
    /// </summary>
    private SearchPath _lastPagePath;
    private string _pendingSearchText = string.Empty;

    /// <summary>
    ///     Debounce stamps, taken from <see cref="Time.Ticks" /> rather than
    ///     <see cref="Environment.TickCount" />: the latter is signed and wraps to
    ///     <see cref="int.MinValue" /> after ~25 days of uptime, which turns an elapsed check into a
    ///     hugely negative number and stalls the debounce for good. Unsigned subtraction wraps
    ///     correctly, so the comparison survives it.
    /// </summary>
    private uint _searchDebounceTimer;
    private uint _resizeDebounceTimer;

    private bool _searchPending;
    private bool _resizePending;


    /// <summary>
    ///     Set while the window is reproducing a history entry, so the selections it drives don't
    ///     record themselves as fresh navigations.
    /// </summary>
    private bool _restoring;

    #endregion

    #region Widgets

    private readonly WrapPanel _optionsPanel = new()
    {
        UniformSizing = false,
        Orientation = Orientation.Vertical,
        HorizontalSpacing = MyraStyle.STANDARD_SPACING,
        VerticalSpacing = MyraStyle.STANDARD_SPACING,
        Padding = new Thickness(3, 0, 0, 10)
    };

    private readonly WrapPanel _optionsStack = new() { UniformSizing = false, Orientation = Orientation.Vertical };

    private readonly SearchResultTree _resultTree = new();

    private readonly SearchBar _searchBar = new(new SearchBarProps { HintText = TazLang.Get("mog_searchellipses") })
    {
        Margin = new Thickness(MyraStyle.STANDARD_SPACING, 0, MyraStyle.STANDARD_SPACING, MyraStyle.STANDARD_SPACING)
    };

    private ScrollViewer? _categoryScroller;
    private IconButton _backButton = null!;
    private IconButton _forwardButton = null!;

    #endregion

    #endregion

    #region Events

    /// <summary>
    ///     Raised when the active category changes, either via a category button click or because a
    ///     navigation landed inside that category. The argument is the newly selected category
    ///     label, or an empty string when search results are showing and no category is current.
    /// </summary>
    public event EventHandler<string>? SelectedCategoryChanged;

    #endregion

    #region Constructors

    public OptionsWindow() : base(TazLang.Get("mog_kw_options"))
    {
        UIManager.ForEach<OptionsWindow>(w =>
        {
            if (w != this)
                w.Dispose();
        });

        SetupOptions();
        Build();

        CenterInViewPort();

        _rootWindow.Props.Resize.MinHeight = MIN_HEIGHT;
        _rootWindow.Props.Resize.MinWidth = MIN_WIDTH;

        _rootWindow.Props.Resize.MaxHeight = MAX_HEIGHT;
        _rootWindow.Props.Resize.MaxWidth = MAX_WIDTH;

        _rootWindow.MinHeight = MIN_HEIGHT;
        _rootWindow.MinWidth = MIN_WIDTH;

        _rootWindow.MaxHeight = MAX_HEIGHT;
        _rootWindow.MaxWidth = MAX_WIDTH;

        // The accessor cannot verify whether the underlying singleton has a value so we provide
        // explicit getter/setter which use conditional logic to make sure we don't throw in the incredibly unlikely case
        // the profile is not ready.
        _rootWindow.Props.InitialSizeStore = new Accessor<Point?>(
            () => ProfileManager.CurrentProfile?.OptionsWindowsSize,
            (newValue) => ProfileManager.CurrentProfile?.OptionsWindowsSize = newValue
            );

        _rootWindow.SizeChanged += (_, _) =>
        {
            _resizeDebounceTimer = Time.Ticks;
            _resizePending = true;

            FreezeResultsWhileDragging();
        };

        NavigateTo(FirstCategoryPath());
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Fires the debounced search and the debounced inline re-evaluation once their timers have
    ///     elapsed since the last keystroke or resize tick.
    /// </summary>
    public override void Update()
    {
        base.Update();

        if (_searchPending && Time.Ticks - _searchDebounceTimer >= SEARCH_DEBOUNCE_MS)
        {
            _searchPending = false;
            ApplySearchText(_pendingSearchText);
        }

        if (_resizePending && Time.Ticks - _resizeDebounceTimer >= RESIZE_DEBOUNCE_MS)
        {
            _resizePending = false;
            _resultTree.IsLayoutFrozen = false;

            RefreshInlinePresentation();
        }

        _highlight.Update();
    }

    /// <summary>
    ///     Opens the options at <paramref name="path" />, selecting the category and then driving each
    ///     nested tab or page along the path. Records the move in history.
    /// </summary>
    /// <param name="path">
    ///     The breadcrumb to open. Segments past the last navigable boundary - a visual container's
    ///     caption, say - are ignored, landing on the closest page that does exist.
    /// </param>
    public void NavigateTo(SearchPath path)
    {
        if (path.IsEmpty)
            return;

        CaptureCurrentViewState();

        var view = new OptionsView { Path = path };

        // Restored before pushed: an under-specified path - a bare category, say - is rewritten by
        // ShowPage to the page it actually landed on, and pushing beforehand would compare the
        // unresolved path and record an entry that back then appears to do nothing.
        Restore(view);
        _history.Push(view);
    }

    /// <summary>Opens the options at the given breadcrumb segments</summary>
    /// <param name="segments">The path segments, outermost first</param>
    public void NavigateTo(params string[] segments) => NavigateTo(new SearchPath(segments));

    /// <summary>
    ///     Shows search results for <paramref name="query" />. Retyping while already on the results
    ///     updates them in place rather than pushing a new history entry, so back doesn't retype the
    ///     query a character at a time.
    /// </summary>
    /// <param name="query">The query to run</param>
    public void ShowSearch(string query)
    {
        if (_history.Current is { IsSearch: true } current)
        {
            // Retyping stays on the same view, so its scroll and collapse state have to be folded
            // back in first - ShowResults restores from the entry, and would otherwise re-expand
            // everything the user just collapsed on every keystroke.
            CaptureCurrentViewState();

            bool queryChanged = !string.Equals(current.Query, query, StringComparison.Ordinal);

            current.Query = query;

            // A different query is a different list: keeping the offset would open the narrower
            // result set scrolled past its own top-ranked hits. Only back and forward restore one.
            if (queryChanged)
                current.ScrollOffset = 0;

            Restore(current);

            return;
        }

        CaptureCurrentViewState();

        var view = new OptionsView { Query = query };

        _history.Push(view);
        Restore(view);
    }

    #endregion

    #region Private Methods

    private void SetupOptions()
    {
        AddOptionSource(TazLang.Get("mog_gameplaytab_gameplaylabel"), GameplayTab.GetContent());
        AddOptionSource(TazLang.Get("mog_kw_interface"), InterfaceTab.GetContent());
        AddOptionSource(TazLang.Get("mog_labelchatandtext"), ChatTab.GetContent());
        AddOptionSource(TazLang.Get("mog_videotab_label"), VideoTab.GetContent());
        AddOptionSource(TazLang.Get("mog_soundtab_label"), SoundsTab.GetContent());
        AddOptionSource(TazLang.Get("mog_misctab_label"), MiscTab.GetContent());
        AddOptionSource(TazLang.Get("mog_kw_profile"), ProfileTab.GetContent());
    }

    /// <summary>
    ///     Registers a top-level category and folds its subtree into the search index, with the
    ///     category's own label seeding each entry's breadcrumb.
    /// </summary>
    /// <remarks>
    ///     Note that window construction is effectively static, meaning runtime addition of nested settings, like
    ///     new VFX rules, will not be indexed/searchable until the window is closed and reopened.
    /// </remarks>
    /// <param name="category">The category label, shown on the navigation rail</param>
    /// <param name="source">The category's content</param>
    private void AddOptionSource(string category, IOptionSource source)
    {
        if (!_categoriesByLabel.TryGetValue(category, out OptionCategory? existing))
        {
            existing = new OptionCategory(category);
            _categories.Add(existing);
            _categoriesByLabel.Add(category, existing);
        }

        existing.Sources.Add(source);

        var context = new OptionIndexContext(new SearchPath(category));

        foreach (SearchEntry entry in source.Index(context))
            _searchIndex.Add(entry with { OnActivate = BuildActivation(entry) });
    }

    /// <summary>
    ///     An entry activated from search results navigates to the page that owns it and then points
    ///     the setting out, so the user lands on the thing they picked rather than on a page that
    ///     merely contains it. The entry's own label is appended to the path so the deepest navigable
    ///     boundary is reached even when the leaf itself is a tab or page.
    /// </summary>
    private Action BuildActivation(SearchEntry entry)
    {
        SearchPath target = entry.FullPath;
        string label = entry.Label;

        return () =>
        {
            NavigateTo(target);

            // The navigation has built the page but nothing has laid it out yet; the highlighter
            // waits for the target to have bounds before scrolling to it.
            _highlight.Start(_optionsPanel, label);
        };
    }

    private void Build()
    {
        _mainArea.MinWidth = 400;
        _mainArea.MinHeight = 400;

        _mainArea.AddColumn(Proportion.Auto);
        _mainArea.AddColumn(Proportion.Fill);
        _mainArea.AddRow(Proportion.Auto);
        _mainArea.AddRow(Proportion.Fill);

        _mainArea.AddWidget(BuildToolbar(), 0, 0, null, 2);

        WrapPanel categoryPanel = new()
        {
            Orientation = Orientation.Vertical, HorizontalSpacing = MyraStyle.STANDARD_SPACING, VerticalSpacing = MyraStyle.STANDARD_SPACING
        };

        _categoryScroller = categoryPanel.WrapInScroll(MAX_HEIGHT);
        _mainArea.AddWidget(_categoryScroller, 1, 0);

        _optionsStack.Widgets.Add(_optionsPanel);
        _mainArea.AddWidget(_optionsStack, 1, 1);

        _resultTree.NavigationRequested += (_, path) =>
        {
            NavigateTo(path);

            // A crumb names whatever the user clicked - a category, a tab, a section. Pointing the
            // cue at it covers the section case; a tab is its own feedback once selected, and
            // FindTarget simply finds nothing for it.
            if (path.Leaf is { Length: > 0 } segment)
                _highlight.Start(_optionsPanel, segment);
        };

        foreach (OptionCategory category in _categories)
            categoryPanel.Widgets.Add(GetCategoryButton(category.Label));

        SetRootContent(_mainArea);
    }

    /// <summary>
    ///     Builds the row holding the back and forward buttons, the query box, and the result count.
    /// </summary>
    private Widget BuildToolbar()
    {
        _backButton = new IconButton(
            "◀",
            GoBack,
            TazLang.Get("optsearch_nav_back"),
            glyphSize: StyleConstantsDefaults.RESET_ICON_FONT_SIZE
        )
        {
            // The glyph's own bearings seat it right of centre; the forward arrow's do not.
            Nudge = new Point(BACK_GLYPH_NUDGE_X, 0)
        };

        _forwardButton = new IconButton(
            "▶",
            GoForward,
            TazLang.Get("optsearch_nav_forward"),
            glyphSize: StyleConstantsDefaults.RESET_ICON_FONT_SIZE
        );

        _searchBar.QueryChanged += (_, query) =>
        {
            _pendingSearchText = query.Trim();
            _searchDebounceTimer = Time.Ticks;
            _searchPending = true;
        };

        _searchBar.Cleared += (_, _) =>
        {
            _pendingSearchText = string.Empty;
            _searchPending = false;
            ApplySearchText(string.Empty);
        };

        var toolbar = new HorizontalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        toolbar.Widgets.Add(_backButton);
        toolbar.Widgets.Add(_forwardButton);
        toolbar.Widgets.Add(_searchBar);

        StackPanel.SetProportionType(_searchBar, ProportionType.Fill);

        _history.Changed += (_, _) => UpdateNavigationButtons();
        UpdateNavigationButtons();

        return toolbar;
    }

    private void UpdateNavigationButtons()
    {
        _backButton.Enabled = _history.CanGoBack;
        _forwardButton.Enabled = _history.CanGoForward;
    }

    private void GoBack()
    {
        LeaveCurrentView();

        if (_history.TryGoBack(out OptionsView view))
            Restore(view);
    }

    private void GoForward()
    {
        LeaveCurrentView();

        if (_history.TryGoForward(out OptionsView view))
            Restore(view);
    }

    /// <summary>
    ///     Common preamble for an explicit history move: snapshot what the current view is showing,
    ///     and drop any keystroke still inside the debounce window so it cannot fire into the view
    ///     being navigated to.
    /// </summary>
    private void LeaveCurrentView()
    {
        CaptureCurrentViewState();

        _searchPending = false;
        _pendingSearchText = string.Empty;
    }

    private ButtonBase2 GetCategoryButton(string category)
    {
        var unstyledButton = new ToggleTextButton(category, _ => NavigateTo(new SearchPath(category))) { SpringLoaded = true };

        // Each button listens to the category selection event and updates its pressed state accordingly
        SelectedCategoryChanged += (_, _) => unstyledButton.IsPressed = _currentCategory == category;

        return ApplyTabStyleToButton(unstyledButton);
    }

    /// <summary>
    ///     Handles the debounced query box. Clearing the box is a navigation back to the last page
    ///     visited rather than a history pop, so forward stays meaningful.
    /// </summary>
    private void ApplySearchText(string searchText)
    {
        if (!string.IsNullOrEmpty(searchText))
        {
            ShowSearch(searchText);
            return;
        }

        SearchPath fallback = LastVisitedPage();

        NavigateTo(fallback.IsEmpty ? FirstCategoryPath() : fallback);
    }

    /// <summary>The most recent non-search location, used when the query box is cleared</summary>
    private SearchPath LastVisitedPage() =>
        _currentCategory.NotNullNotEmpty() ? CurrentPath() : _lastPagePath;

    /// <summary>
    ///     The first registered category's location, or empty when nothing registered - a tab whose
    ///     content threw while being built leaves the window with no categories at all.
    /// </summary>
    private SearchPath FirstCategoryPath() =>
        _categories.Count > 0 ? new SearchPath(_categories[0].Label) : SearchPath.Empty;

    /// <summary>
    ///     Folds the results list's live scroll offset and collapse set back into the current history
    ///     entry, so returning to it later lands on the same list rather than at the top of a freshly
    ///     re-run search.
    /// </summary>
    private void CaptureCurrentViewState()
    {
        if (_history.Current is not { IsSearch: true } current)
            return;

        current.ScrollOffset = _resultTree.GetScrollOffset();
        current.Collapsed.Clear();

        foreach (string collapsed in _resultTree.CollapsedPaths)
            current.Collapsed.Add(collapsed);
    }

    /// <summary>Reproduces <paramref name="view" /> in the content area without touching history</summary>
    private void Restore(OptionsView view)
    {
        // Whatever is being pointed at is about to be rebuilt or scrolled away from.
        _highlight.Cancel();

        _restoring = true;

        try
        {
            if (view.IsSearch)
                ShowResults(view);
            else
                ShowPage(view);
        }
        finally
        {
            _restoring = false;
        }

        UpdateNavigationButtons();
    }

    private void ShowResults(OptionsView view)
    {
        // A drag still in progress when the query changed leaves the list frozen, and its debounced
        // pass may never run - it bails once the view is no longer a search.
        _resultTree.IsLayoutFrozen = false;

        _searchBar.Query = view.Query ?? string.Empty;

        IReadOnlyList<SearchHit> hits = _searchIndex.Search(view.Query);

        _resultTree.CollapsedPaths.Clear();

        foreach (string collapsed in view.Collapsed)
            _resultTree.CollapsedPaths.Add(collapsed);

        _resultTree.AvailableWidth = ResultsWidth();
        _resultTree.InlineThreshold = CurrentInlineThreshold();
        _resultTree.ShowAttribution = ProfileManager.CurrentProfile?.OptionsSearchShowAttribution ?? false;
        _resultTree.SetResults(hits);

        // The scroller clamps against a maximum it only knows after layout, so restoring the
        // offset now would silently snap it to zero. Applied even when zero: SetResults leaves the
        // live scroll position alone, so a reset has to be written out to take effect.
        int restoredOffset = view.ScrollOffset;

        Defer(() => _resultTree.SetScrollOffset(restoredOffset));

        // The tree states an empty result itself, so the count beside the box would only repeat it.
        _searchBar.SetResultText(
            hits.Count > 0 ? TazLang.Get("optsearch_results_count", [hits.Count.ToString()]) : null
        );

        if (_optionsStack.Widgets.Count != 1 || !ReferenceEquals(_optionsStack.Widgets[0], _resultTree))
        {
            _optionsStack.Widgets.Clear();
            _optionsStack.Widgets.Add(_resultTree);
        }

        // Results span categories, so no single rail entry is current. The previous category's
        // controls stay constructed but are no longer on screen, so drop the map with it: a
        // selection raised on one of them would otherwise be recorded as a page navigation while
        // the user is looking at search results.
        _currentCategory = string.Empty;
        ForgetTabControls();
        SelectedCategoryChanged?.Invoke(this, string.Empty);
    }

    /// <summary>
    ///     Renders the category at the head of <paramref name="view" />'s path, then walks the rest of
    ///     the path selecting the matching nested tab or page at each level.
    /// </summary>
    private void ShowPage(OptionsView view)
    {
        SearchPath path = view.Path;

        if (path.IsEmpty)
            return;

        ShowCategory(path[0]);

        // A page has no query behind it; the bar follows the location rather than the other way
        // round. Any keystroke still inside the debounce window is dropped with it - otherwise it
        // fires moments later and drags the user into results for a query that is no longer shown.
        _searchPending = false;
        _pendingSearchText = string.Empty;
        _searchBar.Query = string.Empty;
        _searchBar.SetResultText(null);

        Widget current = _optionsPanel;

        for (int depth = 1; depth < path.Length; depth++)
        {
            Widget? next = SelectPathSegment(current, path[depth]);

            // The segment names something that isn't a navigable boundary - a visual container
            // caption, for instance - so this page is as deep as navigation goes.
            if (next == null)
                break;

            current = next;

            // Selecting a tab builds its content, which may itself contain tab controls that have
            // to be watched before the next segment is resolved.
            WatchTabControls();
        }

        WatchTabControls();

        _lastPagePath = CurrentPath();

        // Selecting a category records only the category, but the page it lands on has tabs already
        // selected. Without writing the resolved location back, returning to this entry would
        // restore the category and leave whatever tabs happen to be showing - which looks exactly
        // like back doing nothing.
        if (_lastPagePath.Length > path.Length && _lastPagePath.StartsWith(path))
            view.Path = _lastPagePath;
    }

    /// <summary>
    ///     Selects <paramref name="segment" /> in whichever tab or page control beneath
    ///     <paramref name="root" /> actually offers it.
    ///     <para>
    ///         Every candidate is tried, nearest first, rather than only the closest one: a tab's
    ///         content can hold more than one control, and the nearest need not be the one that owns
    ///         the segment.
    ///     </para>
    /// </summary>
    /// <returns>The newly selected content to continue descending into, or <see langword="null" /> when nothing matched</returns>
    private static Widget? SelectPathSegment(Widget root, string segment)
    {
        if (root is MyraTabControl self && self.TrySelectByLabel(segment))
            return self.SelectedContent ?? root;

        // Materialised first: selecting a tab builds its content into the very collection the
        // walk is enumerating, which would otherwise throw part-way through a navigation.
        // Live-only: a control cached inside an unselected tab is not on the user's route to the
        // segment, and selecting a segment inside one would move a page nobody is looking at.
        List<Widget> candidates = [.. root.Descendants(liveOnly: true)];

        foreach (Widget descendant in candidates)
        {
            switch (descendant)
            {
                case MyraTabControl tabs when tabs.TrySelectByLabel(segment):
                    return tabs.SelectedContent ?? root;

                case PageControl pages when pages.TrySelectByLabel(segment):
                    return pages.CurrentPageContent ?? root;
            }
        }

        return null;
    }

    /// <summary>Renders a top-level category into the content area, replacing whatever was there</summary>
    private void ShowCategory(string category)
    {
        if (_currentCategory == category && ReferenceEquals(FirstStackChild(), _optionsPanel))
            return;

        ForgetTabControls();

        _optionsStack.Widgets.Clear();
        _optionsStack.Widgets.Add(_optionsPanel);

        _optionsPanel.Widgets.Clear();

        _currentCategory = category;

        if (_categoriesByLabel.TryGetValue(category, out OptionCategory? resolved))
        {
            foreach (IOptionSource source in resolved.Sources)
                _optionsPanel.Widgets.Add(source.Render());
        }

        SelectedCategoryChanged?.Invoke(this, category);
    }

    private Widget? FirstStackChild() => _optionsStack.Widgets.Count > 0 ? _optionsStack.Widgets[0] : null;

    /// <summary>
    ///     Walks the rendered tree recording where each tab control sits and subscribing to it, so a
    ///     click on a nested tab records itself in history. Tab contents are built lazily, so deeper
    ///     controls only exist once their parent tab has been shown - hence re-running this after
    ///     every selection change.
    /// </summary>
    private void WatchTabControls()
    {
        if (!_currentCategory.NotNullNotEmpty())
            return;

        MapTabControls(_optionsPanel, new SearchPath(_currentCategory));
    }

    /// <summary>
    ///     Drops every tab-control subscription and its recorded location, for when the controls
    ///     they refer to are about to be replaced or are no longer on screen.
    /// </summary>
    private void ForgetTabControls()
    {
        foreach (MyraTabControl tabs in _watchedTabControls)
            tabs.SelectedIndexChanged -= OnNestedTabSelected;

        _watchedTabControls.Clear();
        _tabControlPaths.Clear();
    }

    /// <summary>
    ///     Records and subscribes to the tab controls directly beneath <paramref name="root" />, then
    ///     descends through each one's selected tab.
    /// </summary>
    /// <param name="root">The widget to search beneath</param>
    /// <param name="path">The location of <paramref name="root" /> itself</param>
    private void MapTabControls(Widget root, SearchPath path)
    {
        foreach (MyraTabControl tabs in NearestTabControls(root))
        {
            _tabControlPaths[tabs] = path;

            if (_watchedTabControls.Add(tabs))
                tabs.SelectedIndexChanged += OnNestedTabSelected;

            if (tabs.SelectedItem?.Text is not { } label || tabs.SelectedContent is not { } content)
                continue;

            MapTabControls(content, path.Push(label));
        }
    }

    /// <summary>
    ///     The tab controls nearest to <paramref name="root" />, not descending past one - a nested
    ///     control belongs to whichever tab contains it, and is mapped when that tab is descended
    ///     into.
    /// </summary>
    private static List<MyraTabControl> NearestTabControls(Widget root)
    {
        if (root is MyraTabControl self)
            return [self];

        List<MyraTabControl> found = [];
        Queue<Widget> pending = new();

        foreach (Widget child in root.ChildWidgets())
            pending.Enqueue(child);

        while (pending.Count > 0)
        {
            Widget current = pending.Dequeue();

            // Stop at a tab control rather than descending through it: anything inside belongs to
            // whichever tab holds it, and is mapped when that tab is descended into.
            if (current is MyraTabControl tabs)
            {
                found.Add(tabs);
                continue;
            }

            foreach (Widget child in current.ChildWidgets())
                pending.Enqueue(child);
        }

        return found;
    }

    private void OnNestedTabSelected(object? sender, EventArgs e)
    {
        WatchTabControls();

        if (_restoring || sender is not MyraTabControl tabs)
            return;

        // The cue is pointing into the tab content that just went off screen, and would otherwise
        // keep pulsing there for the rest of its run.
        _highlight.Cancel();

        if (!_tabControlPaths.TryGetValue(tabs, out SearchPath basePath) || tabs.SelectedItem?.Text is not { } label)
            return;

        // Append the deeper levels' own selections so forward and back reproduce the exact page,
        // not merely the tab that was clicked.
        SearchPath path = ExtendWithSelections(basePath.Push(label), tabs.SelectedContent);

        if (path.IsEmpty)
            return;

        CaptureCurrentViewState();
        _history.Push(new OptionsView { Path = path });

        // Clearing the query box returns to the last page, which is now this tab rather than
        // whichever one the category landed on.
        _lastPagePath = path;

        UpdateNavigationButtons();
    }

    /// <summary>Appends the selected tab at each level beneath <paramref name="root" /></summary>
    private static SearchPath ExtendWithSelections(SearchPath path, Widget? root)
    {
        Widget? current = root;

        while (current != null)
        {
            List<MyraTabControl> nested = NearestTabControls(current);

            if (nested.Count == 0 || nested[0].SelectedItem?.Text is not { } label)
                break;

            path = path.Push(label);
            current = nested[0].SelectedContent;
        }

        return path;
    }

    /// <summary>
    ///     Reads the location currently on screen by walking the selected tab at each level. Page
    ///     controls are deliberately skipped: paging within a page group is a pagination gesture
    ///     rather than a change of location, and most such pages are unnamed anyway.
    /// </summary>
    private SearchPath CurrentPath() =>
        _currentCategory.NotNullNotEmpty()
            ? ExtendWithSelections(new SearchPath(_currentCategory), _optionsPanel)
            : SearchPath.Empty;

    /// <summary>
    ///     Rebuilds the visible result rows, preserving scroll and collapse state. A no-op unless
    ///     search results are showing, which is what separates it from
    ///     <see cref="RefreshCurrentContent" />: a setting that only changes how results are drawn
    ///     must not rebuild a whole category when the user happens to be on a page.
    /// </summary>
    internal void RefreshSearchResults()
    {
        if (_history.Current is not { IsSearch: true } search)
            return;

        // Restore reads scroll and collapse state off the entry, so what the user has since done
        // to the live list has to be folded back in first.
        CaptureCurrentViewState();
        Restore(search);
    }

    /// <summary>
    ///     Re-decides inline versus link for the visible results against the current window size and
    ///     the configured threshold. A no-op unless search results are showing.
    /// </summary>
    internal void RefreshInlinePresentation()
    {
        if (_history.Current is not { IsSearch: true })
            return;

        _resultTree.AvailableWidth = ResultsWidth();
        _resultTree.InlineThreshold = CurrentInlineThreshold();
        _resultTree.ApplyPresentation();
    }

    /// <summary>
    ///     Holds the results list's layout still for the duration of a window drag. The rows stay on
    ///     screen and keep drawing; the debounced pass in <see cref="Update" /> thaws them and reflows
    ///     once, at the size the window actually ended up.
    ///     <para>
    ///         Re-measuring 90 rows costs around 12ms a frame - most of the budget - and the drag
    ///         re-measures them on every one of its frames. See
    ///         <see cref="SearchResultTree.IsLayoutFrozen" /> for why Myra's own measure cache does
    ///         not spare us that.
    ///     </para>
    /// </summary>
    private void FreezeResultsWhileDragging()
    {
        if (_history.Current is { IsSearch: true })
            _resultTree.IsLayoutFrozen = true;
    }

    /// <summary>
    ///     Width a result row really has.
    ///     <para>
    ///         Derived from the window's own bounds, not from <see cref="_optionsStack" />: the window
    ///         wraps its content in a scroll viewer that shows a horizontal bar, which hands the content
    ///         an unbounded width to measure against. The stack therefore grows to fit whatever it
    ///         holds, so reading its width would report how wide the rows already are and the demotion
    ///         guard could never fire - the window would just scroll sideways instead.
    ///     </para>
    /// </summary>
    private int ResultsWidth()
    {
        int window = _rootWindow.ActualBounds.Width;

        if (window <= 0)
            window = ProfileManager.CurrentProfile?.OptionsWindowsSize?.X ?? MIN_WIDTH;

        int rail = _categoryScroller?.ActualBounds.Width ?? 0;

        if (rail <= 0)
            rail = RAIL_WIDTH_FALLBACK;

        return Math.Max(window - rail - RESULTS_CHROME_ALLOWANCE, RAIL_WIDTH_FALLBACK);
    }

    private static double CurrentInlineThreshold() =>
        (ProfileManager.CurrentProfile?.OptionsSearchInlineMode ?? SearchInlineMode.Balanced).ToThreshold();

    private static ButtonStyle _lastUsedButtonStylesheet = null!;
    private static ButtonStyle _tabButtonStyle = null!;

    private static ButtonBase2 ApplyTabStyleToButton(ButtonBase2 tabButton)
    {
        if (_tabButtonStyle == null! || _lastUsedButtonStylesheet != Stylesheet.Current.ButtonStyle)
        {
            _lastUsedButtonStylesheet = Stylesheet.Current.ButtonStyle;
            _tabButtonStyle = new ButtonStyle(_lastUsedButtonStylesheet)
            {
                Background = new SolidBrush(Color.Transparent),
                Border = new SolidBrush(new Color(0, 0, 0, MyraStyle.STANDARD_BORDER_ALPHA)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                LabelStyle = { Font = MyraStyle.UiFont },
                OverBackground = new SolidBrush(new Color(0, 0, 0, 55)),
                PressedBackground = new SolidBrush(new Color(0, 0, 0, 155)),
                MinWidth = 125
            };
        }

        tabButton.ApplyButtonStyle(_tabButtonStyle);

        return tabButton;
    }

    // Presets change several settings at once; recreate the visible controls from
    // their accessors so the values on screen agree with the applied profile.
    internal void RefreshCurrentContent()
    {
        if (_history.Current is { IsSearch: true })
        {
            RefreshSearchResults();
            return;
        }

        if (!_categoriesByLabel.TryGetValue(_currentCategory, out OptionCategory? resolved))
            return;

        // The cue points into a widget this rebuild is about to replace.
        _highlight.Cancel();

        for (int i = 0; i < resolved.Sources.Count && i < _optionsPanel.Widgets.Count; i++)
        {
            // Rebuilding the category would reset nested tabs to their first page.
            if (_optionsPanel.Widgets[i] is MyraTabControl tabs)
            {
                tabs.RefreshSelectedContent();
                continue;
            }

            // A replaced PageControl is a fresh one showing its first page, so the page the user
            // was on has to be carried across the swap.
            int page = _optionsPanel.Widgets[i] is PageControl paged ? paged.CurrentPage : 0;
            Widget rendered = resolved.Sources[i].Render();

            if (rendered is PageControl renderedPages)
                renderedPages.CurrentPage = page;

            _optionsPanel.Widgets[i] = rendered;
        }

        // Whatever tab controls the rebuilt content holds are new objects; the subscriptions went
        // out with the widgets they were on.
        ForgetTabControls();
        WatchTabControls();
    }

    #endregion

    /// <summary>
    ///     Ends any effect preview started from the Visual Effects tab. The preview exists to be
    ///     looked at while tuning; nothing outside this window can turn it off, so leaving it running
    ///     would strand an overlay on screen with no visible control for it.
    /// </summary>
    public override void Dispose()
    {
        _highlight.Cancel();
        ScreenOverlayManager.Instance.ClearPreview();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>A top-level category and the sources rendered into it, in declared order</summary>
    private sealed class OptionCategory(string label)
    {
        public string Label { get; } = label;

        public List<IOptionSource> Sources { get; } = [];
    }
}
