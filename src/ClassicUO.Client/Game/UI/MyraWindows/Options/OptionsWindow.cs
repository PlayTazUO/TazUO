#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
///     The top-level options window that hosts category navigation, a search field, and
///     the paged content area. Replaces any existing <see cref="OptionsWindow" /> when opened.
/// </summary>
public class OptionsWindow : MyraControl
{
    #region Members

    #region Consts/Readonly

    private const int MAX_HEIGHT = 850;
    private const int MAX_WIDTH = 1200;
    private const int SEARCH_DEBOUNCE_MS = 500;

    private readonly Dictionary<string, List<IOptionSource>> _optionSources = new();
    private readonly Dictionary<string, List<OptionEntry>> _searchIndex = new();
    private readonly MyraGrid _mainArea = new();

    #endregion

    #region Statics

    private static Point? _pageControlOverhead;

    #endregion

    #region Mutables

    private string _lastCategory = string.Empty;
    private string _pendingSearchText = string.Empty;
    private double _searchDebounceTimer;
    private bool _searchPending;
    private Point? _resultsBudget;

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

    private readonly MyraInputBox _searchField = new()
    {
        TextVerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(
            MyraStyle.STANDARD_SPACING,
            0,
            MyraStyle.STANDARD_SPACING,
            MyraStyle.STANDARD_SPACING
        ),
        Padding = new Thickness(
            MyraStyle.STANDARD_SPACING,
            5,
            MyraStyle.STANDARD_SPACING,
            5
        )
    };

    #endregion

    #endregion

    #region Events

    /// <summary>
    ///     Raised when the active category changes, either via a category button click or when
    ///     a search is cleared and the previous category is restored.
    ///     The event argument is the newly selected category label.
    /// </summary>
    public event EventHandler<string>? SelectedCategoryChanged;

    #endregion

    #region Constructors

    public OptionsWindow() : base(Language.Instance.GetModernOptionsGumpLanguage.Kw.Options)
    {
        UIManager.ForEach<OptionsWindow>(w =>
        {
            if (w != this) w.Dispose();
        });

        SetupOptions();
        Build();

        CenterInViewPort();

        _rootWindow.Props.Resize.MaxHeight = MAX_HEIGHT;
        _rootWindow.Props.Resize.MaxWidth = MAX_WIDTH;
        _rootWindow.MaxHeight = MAX_HEIGHT;
        _rootWindow.MaxWidth = MAX_WIDTH;

        _rootWindow.SizeChanged += (_, _) => _resultsBudget = null;
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Fires the debounced search when the debounce timer has elapsed since the last keystroke
    /// </summary>
    public override void Update()
    {
        base.Update();

        if (_searchPending && Environment.TickCount - _searchDebounceTimer >= SEARCH_DEBOUNCE_MS)
        {
            _searchPending = false;
            ApplySearch(_pendingSearchText);
        }
    }

    #endregion

    #region Private Methods

    private void SetupOptions()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        AddOptionSource(lang.GameplayTab.GameplayLabel, GameplayTab.GetContent());
        AddOptionSource(lang.Kw.Interface, InterfaceTab.GetContent());
        AddOptionSource(lang.LabelChatAndText, ChatTab.GetContent());
        AddOptionSource(lang.VideoTab.Label, VideoTab.GetContent());
        AddOptionSource(lang.SoundTab.Label, SoundsTab.GetContent());
        AddOptionSource(lang.MiscTab.Label, MiscTab.GetContent());
        AddOptionSource(lang.Kw.Profile, ProfileTab.GetContent());
    }

    private void AddOptionSource(string category, IOptionSource source)
    {
        if (!_optionSources.TryGetValue(category, out List<IOptionSource>? sources))
        {
            sources = [];
            _optionSources.Add(category, sources);
        }

        sources.Add(source);

        if (!_searchIndex.TryGetValue(category, out List<OptionEntry>? entries))
        {
            entries = [];
            _searchIndex.Add(category, entries);
        }

        entries.AddRange(source.GetOptions(new SearchMetadata(Tags: [category])));
    }

    private void Build()
    {
        _mainArea.MinWidth = 400;
        _mainArea.MinHeight = 400;

        _mainArea.AddColumn(Proportion.Auto);
        _mainArea.AddColumn(Proportion.Fill);
        _mainArea.AddRow(Proportion.Auto);
        _mainArea.AddRow(Proportion.Fill);

        _searchField.HintText = Language.Instance.GetModernOptionsGumpLanguage.SearchEllipses;
        _searchField.TextChangedByUser += SearchFieldOnTextChangedByUser;
        _mainArea.AddWidget(_searchField, 0, 0, null, 2);

        WrapPanel categoryPanel = new()
        {
            Orientation = Orientation.Vertical, HorizontalSpacing = MyraStyle.STANDARD_SPACING, VerticalSpacing = MyraStyle.STANDARD_SPACING
        };

        _mainArea.AddWidget(categoryPanel.WrapInScroll(MAX_HEIGHT), 1, 0);

        _optionsStack.Widgets.Add(_optionsPanel);
        _mainArea.AddWidget(_optionsStack, 1, 1);

        foreach (string category in _optionSources.Keys)
            categoryPanel.Widgets.Add(GetCategoryButton(category));

        SetRootContent(_mainArea);
    }

    private ButtonBase2 GetCategoryButton(string category)
    {
        var unstyledButton = new ToggleTextButton(category, _ => ShowPage(category)) { SpringLoaded = true };

        // Each button listens to the category selection event and updates its pressed state accordingly
        SelectedCategoryChanged += (_, _) => unstyledButton.IsPressed = _lastCategory == category;

        return ApplyTabStyleToButton(unstyledButton);
    }

    private void SearchFieldOnTextChangedByUser(object? sender, ValueChangedEventArgs<string> e)
    {
        _pendingSearchText = e.NewValue?.Trim() ?? string.Empty;
        _searchDebounceTimer = Environment.TickCount;
        _searchPending = true;
    }

    private void ApplySearch(string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            _optionsStack.Widgets.Clear();
            _optionsStack.Widgets.Add(_optionsPanel);

            if (_lastCategory.NotNullNotEmpty())
                ShowPage(_lastCategory);

            return;
        }

        List<Widget> matches = CollectSearchMatches(searchText);

        _optionsStack.Widgets.Clear();
        _optionsStack.Widgets.Add(BuildPagedResults(matches));
    }

    private List<Widget> CollectSearchMatches(string searchText)
    {
        List<Widget> matches = [];

        foreach ((string _, List<OptionEntry> entries) in _searchIndex)
        foreach (OptionEntry entry in entries)
        foreach (OptionEntry match in entry.Match(new SearchMetadata(searchText)))
            matches.Add(match.Render());

        return matches;
    }

    /// <summary>
    ///     Builds a page control with the given search matches. Each page is sized to fit the available space.
    /// </summary>
    /// <param name="matches">The search results to display</param>
    /// <returns>A ready to use, sized page control with the search results</returns>
    private PageControl BuildPagedResults(List<Widget> matches)
    {
        Point stackBounds = _resultsBudget ??= ComputeResultsBudget();
        Point overhead = GetPageControlOverhead();

        Point budget = new(
            Math.Max(stackBounds.X - overhead.X, 1),
            Math.Max(stackBounds.Y - overhead.Y, 1)
        );

        budget.X = Math.Max(budget.X, Math.Min(WidestMatchWidth(matches), MAX_WIDTH - overhead.X));

        return new PageControl(PackIntoPages(matches, budget).ToArray());
    }

    /// <summary>
    ///     The cached budget can be narrower than an individual match (e.g., a long option row),
    ///     which would otherwise overflow the page's width. Widen to fit, capped at MAX_WIDTH.
    /// </summary>
    private static int WidestMatchWidth(List<Widget> matches)
    {
        int widest = 0;

        foreach (Widget widget in matches)
            widest = Math.Max(widest, widget.Measure(new Point(MAX_WIDTH, MAX_HEIGHT)).X);

        return widest;
    }

    /// <summary>
    ///     Derived from the main area and search bar bounds so the budget reflects the actual
    ///     fill-row height regardless of how much content the current category renders.
    ///     Using <see cref="_optionsStack"/> ActualBounds instead would give the current tab's
    ///     rendered content height, which can be shorter than the window on sparse tabs.
    /// </summary>
    private Point ComputeResultsBudget()
    {
        Rectangle mainBounds = _mainArea.ActualBounds;
        Rectangle searchBounds = _searchField.ActualBounds;
        int width = _optionsStack.ActualBounds.Width;

        int height = mainBounds.Height - searchBounds.Height;

        return height > 0 && width > 0
            ? new Point(width, height)
            : new Point(MAX_WIDTH, MAX_HEIGHT);
    }

    /// <summary>
    ///     Mirrors WrapPanel's vertical-bias column packing (fill height, wrap column), then adds
    ///     the width-based page break WrapPanel itself doesn't have, since it grows columns unbounded.
    /// </summary>
    private static List<Widget> PackIntoPages(List<Widget> matches, Point budget)
    {
        List<Widget> pages = [];
        WrapPanel page = NewResultsPage(budget);

        int columnHeight = 0;
        int columnWidth = 0;
        int pageWidth = 0;

        foreach (Widget widget in matches)
        {
            Point size = widget.Measure(budget);

            if (columnHeight > 0 && columnHeight + MyraStyle.STANDARD_SPACING + size.Y > budget.Y)
            {
                pageWidth += columnWidth + MyraStyle.STANDARD_SPACING;
                columnHeight = 0;
                columnWidth = 0;
            }

            if (pageWidth > 0 && pageWidth + size.X > budget.X)
            {
                pages.Add(page);
                page = NewResultsPage(budget);
                pageWidth = 0;
            }

            page.Widgets.Add(widget);
            columnHeight += (columnHeight > 0 ? MyraStyle.STANDARD_SPACING : 0) + size.Y;
            columnWidth = Math.Max(columnWidth, size.X);
        }

        pages.Add(page);

        return pages;
    }

    private static WrapPanel NewResultsPage(Point budget) => new()
    {
        UniformSizing = false,
        Aligned = false,
        Orientation = Orientation.Vertical,
        HorizontalSpacing = MyraStyle.STANDARD_SPACING,
        VerticalSpacing = MyraStyle.STANDARD_SPACING,
        MaxWidth = budget.X,
        MaxHeight = budget.Y
    };

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

    private void ShowPage(string category)
    {
        if (_lastCategory == category)
            return;

        _searchField.Text = null;
        // This internally calls some setters that render the hint again. Without this, and even though the property hasn't changes, hint will be lost.
        _searchField.HintText = Language.Instance.GetModernOptionsGumpLanguage.SearchEllipses;

        _optionsStack.Widgets.Clear();
        _optionsStack.Widgets.Add(_optionsPanel);

        _optionsPanel.Widgets.Clear();

        _lastCategory = category;

        if (!_optionSources.TryGetValue(category, out List<IOptionSource>? sources))
            return;
        foreach (IOptionSource source in sources)
            _optionsPanel.Widgets.Add(source.Render());

        SelectedCategoryChanged?.Invoke(this, category);
    }

    #region Static Methods

    private static Point GetPageControlOverhead()
    {
        if (_pageControlOverhead.HasValue)
            return _pageControlOverhead.Value;

        // Measure an empty PageControl to determine the total space its frame + control bar consume.
        // Content size is zero, so the entire measured size is overhead the host must reserve.
        var dummy = new PageControl();
        _pageControlOverhead = dummy.Measure(new Point(MAX_WIDTH, MAX_HEIGHT));
        return _pageControlOverhead.Value;
    }

    #endregion

    #endregion
}
