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

    public bool RetainSizeWhenPaging { get; set; }

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

    private void OnFirstPage() => CurrentPage = 0;

    private void OnPrevPage()
    {
        if (CurrentPage > 0)
            CurrentPage--;
    }

    private void OnNextPage()
    {
        if (CurrentPage < _pages.Count - 1)
            CurrentPage++;
    }

    private void OnLastPage()
    {
        if (_pages.Count > 0)
            CurrentPage = _pages.Count - 1;
    }

    public void Add(params Widget[] pageWidgets)
    {
        Widget[] nonNullWidgets = pageWidgets.Where(w => w != null).ToArray();

        if (nonNullWidgets.Length <= 0)
            return;

        _pages.AddRange(nonNullWidgets);
        UpdateControlBar();
    }

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
