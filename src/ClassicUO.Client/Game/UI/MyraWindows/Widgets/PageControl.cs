using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A Myra <see cref="Container"/> that displays one widget at a time from an ordered list of
/// pages, together with a navigation bar (first / previous / page indicator / next / last).
/// Pages can be supplied at construction time or added later via <see cref="Add"/>.
/// </summary>
public class PageControl : Container
{
    private readonly List<Widget> _pages = [];

    private readonly VerticalStackPanel _mainPanel = new();
    private readonly Panel _contentPanel = new();
    private Point _contentPanelRetainedSize;

    private Button _firstButton;
    private Button _prevButton;
    private Button _nextButton;
    private Button _lastButton;

    private MyraLabel _currentPageDisplay;

    /// <summary>
    /// Gets or sets the current page.
    /// Note that this property is 'guarded' in that it will not allow the current page to be set to a value outside the range of the number of pages.
    /// </summary>
    public int CurrentPage
    {
        get => field;
        set
        {
            if (_pages.Count == 0)
            {
                field = 0;
                _contentPanel.Widgets.Clear();
                UpdateControlBar();
                return;
            }

            int clamped = Math.Clamp(value, 0, _pages.Count - 1);
            if (clamped == field)
                return;
            field = clamped;

            _contentPanel.Widgets.Clear();
            _contentPanel.Widgets.Add(_pages[field]);
            UpdateControlBar();
        }
    }

    /// <summary>
    /// When <see langword="true"/>, the content panel's width and height are locked to the size
    /// measured on page 0 so the control does not resize as the user navigates between pages.
    /// </summary>
    public bool RetainSizeWhenPaging { get; set; }

    /// <summary>
    /// Optional names for the pages, parallel to the page list. The control bar shows page numbers,
    /// not names, so these exist only so a caller holding a path of labels can locate a page - see
    /// <see cref="TrySelectByLabel"/>.
    /// </summary>
    public IReadOnlyList<string> PageLabels { get; set; } = [];

    /// <summary>
    /// Name of the page currently shown, or <see langword="null"/> when it has none. The counterpart
    /// to <see cref="TrySelectByLabel"/>: a caller recording where the user is needs the label that
    /// would bring them back.
    /// </summary>
    public string CurrentPageLabel =>
        CurrentPage >= 0 && CurrentPage < PageLabels.Count ? PageLabels[CurrentPage] : null;

    /// <summary>Selects the page whose entry in <see cref="PageLabels"/> matches <paramref name="label"/></summary>
    /// <param name="label">The page name to look for</param>
    /// <returns>Whether a matching page was found and selected</returns>
    public bool TrySelectByLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
            return false;

        for (int i = 0; i < PageLabels.Count && i < _pages.Count; i++)
        {
            if (!string.Equals(PageLabels[i], label, StringComparison.Ordinal))
                continue;

            CurrentPage = i;

            return true;
        }

        return false;
    }

    /// <summary>
    /// The widget on the current page, or <see langword="null"/> when there are no pages.
    /// <para>
    /// Nullable despite the unannotated return type - this file is not <c>#nullable enable</c>, and
    /// enabling it would require annotating the control's deferred-built fields. Callers must
    /// null-check.
    /// </para>
    /// </summary>
    public Widget CurrentPageContent => _pages.Count == 0 ? null : _pages[CurrentPage];

    /// <summary>
    /// Pins the content panel to an explicit size so the control does not jump on navigation.
    /// The caller should pass the maximum effective page size so no page overflows.
    /// </summary>
    public Point? ContentSize
    {
        set
        {
            _contentPanel.Width = value?.X;
            _contentPanel.Height = value?.Y;
        }
    }

    /// <summary>
    /// Initializes a new <see cref="PageControl"/> and optionally pre-populates it with pages.
    /// </summary>
    /// <param name="widgets">Zero or more widgets to register as initial pages.</param>
    public PageControl(params Widget[] widgets)
    {
        Margin = new Thickness(4);
        Padding = new Thickness(4, 6, 4, 12);
        Background = new SolidBrush(new Color(0, 0, 0, 25));
        Border = new SolidBrush(new Color(0, 0, 0, 75));
        BorderThickness = new Thickness(2);

        if (widgets?.Length > 0)
        {
            _pages.AddRange(widgets);
            _contentPanel.Widgets.Add(widgets[0]);
        }

        ChildrenLayout = new WrapPanelLayout { Orientation = Orientation.Vertical };

        _mainPanel.Widgets.Add(_contentPanel);
        Children.Add(_mainPanel);
        CreateControlBar();
    }

    /// <summary>
    /// Builds the navigation bar (first/prev/indicator/next/last) and appends it to
    /// <see cref="_mainPanel"/>.
    /// </summary>
    private void CreateControlBar()
    {
        SpriteFontBase font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, 24);
        _firstButton = new MyraButton("⏮", OnFirstPage, labelFont: font);
        _prevButton = new MyraButton("⏴", OnPrevPage, labelFont: font);
        _currentPageDisplay = new MyraLabel("", MyraLabel.TextStyle.P) { VerticalAlignment = VerticalAlignment.Center };
        _nextButton = new MyraButton("⏵", OnNextPage, labelFont: font);
        _lastButton = new MyraButton("⏭", OnLastPage, labelFont: font);
        UpdateControlBar();

        StackPanel bar = OptionTabCommons.StyledStackPanel(
            Orientation.Horizontal,
            _firstButton,
            _prevButton,
            _currentPageDisplay,
            _nextButton,
            _lastButton
        );
        bar.VerticalAlignment = VerticalAlignment.Bottom;
        bar.HorizontalAlignment = HorizontalAlignment.Center;
        bar.Margin = new Thickness(0, 20, 0, 0);

        _mainPanel.Widgets.Add(bar);
    }

    /// <summary>
    /// Refreshes navigation button enabled states and the page indicator label to reflect
    /// <see cref="CurrentPage"/> and the total page count.
    /// </summary>
    private void UpdateControlBar()
    {
        bool backEnabled = CurrentPage > 0;
        bool forwardEnabled = CurrentPage < _pages.Count - 1;

        _firstButton.Enabled = backEnabled;
        _prevButton.Enabled = backEnabled;
        _currentPageDisplay.Text = $"{CurrentPage + 1}/{_pages.Count}";
        _nextButton.Enabled = forwardEnabled;
        _lastButton.Enabled = forwardEnabled;
    }

    /// <summary>Navigates to the first page.</summary>
    private void OnFirstPage() => CurrentPage = 0;

    /// <summary>Navigates to the previous page if one exists.</summary>
    private void OnPrevPage()
    {
        if (CurrentPage > 0)
            CurrentPage--;
    }

    /// <summary>Navigates to the next page if one exists.</summary>
    private void OnNextPage()
    {
        if (CurrentPage < _pages.Count - 1)
            CurrentPage++;
    }

    /// <summary>Navigates to the last page.</summary>
    private void OnLastPage()
    {
        if (_pages.Count > 0)
            CurrentPage = _pages.Count - 1;
    }

    /// <summary>
    /// Appends one or more widgets as new pages. <see langword="null"/> entries are silently
    /// ignored. If the control was empty before this call, the first added widget becomes the
    /// visible page.
    /// </summary>
    /// <param name="pageWidgets">Widgets to add as pages.</param>
    public void Add(params Widget[] pageWidgets)
    {
        Widget[] nonNullWidgets = pageWidgets.Where(w => w != null).ToArray();

        if (nonNullWidgets.Length <= 0)
            return;

        bool wasEmpty = _pages.Count == 0;
        _pages.AddRange(nonNullWidgets);

        if (wasEmpty)
        {
            _contentPanel.Widgets.Clear();
            _contentPanel.Widgets.Add(_pages[0]);
        }

        UpdateControlBar();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// On page 0, if <see cref="RetainSizeWhenPaging"/> is enabled and the content panel has no
    /// explicit size yet, measures and locks the panel dimensions so subsequent page changes do
    /// not cause layout reflows.
    /// </remarks>
    protected override Point InternalMeasure(Point availableSize)
    {
        if (CurrentPage != 0)
            return base.InternalMeasure(availableSize);

        if (RetainSizeWhenPaging && (!_contentPanel.Height.HasValue || !_contentPanel.Width.HasValue))
        {
            _contentPanelRetainedSize = _contentPanel.Measure(availableSize);
            _contentPanel.Width = _contentPanelRetainedSize.X;
            _contentPanel.Height = _contentPanelRetainedSize.Y;
        }

        return base.InternalMeasure(availableSize);;
    }
}
