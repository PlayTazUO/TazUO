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

public class OptionsWindow : MyraControl
{
    private const int MAX_HEIGHT = 850;
    private const int MAX_WIDTH = 1200;

    private readonly Dictionary<string, List<IOptionSource>> _optionSources = new();
    private readonly Dictionary<string, List<OptionEntry>> _searchIndex = new();

    private readonly MyraGrid _mainArea = new();

    private readonly WrapPanel _optionsPanel = new()
    {
        UniformSizing = false,
        Orientation = Orientation.Vertical,
        HorizontalSpacing = MyraStyle.STANDARD_SPACING,
        VerticalSpacing = MyraStyle.STANDARD_SPACING,
        Padding = new Thickness(3, 0, 0, 10)
    };

    private readonly WrapPanel _searchPanel = new() { UniformSizing = false, Orientation = Orientation.Vertical };
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

    private string _lastCategory = string.Empty;

    public event EventHandler<string>? SelectedCategoryChanged;

    public OptionsWindow() : base("Options")
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
    }

    private void SetupOptions()
    {
        AddOptionSource(Language.Instance.GetModernOptionsGumpLanguage.Kw.Interface, InterfaceTab.GetContent());
        AddOptionSource(Language.Instance.GetModernOptionsGumpLanguage.MiscTab.Label, MiscTab.GetContent());
        AddOptionSource(Language.Instance.GetModernOptionsGumpLanguage.GameplayTab.GameplayLabel, GameplayTab.GetContent());
        AddOptionSource(Language.Instance.GetModernOptionsGumpLanguage.SoundTab.Label, SoundsTab.GetContent());
        AddOptionSource(Language.Instance.GetModernOptionsGumpLanguage.VideoTab.Label, VideoTab.GetContent());
        AddOptionSource(Language.Instance.GetModernOptionsGumpLanguage.LabelChatAndText, ChatTab.GetContent());
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
        var unstyledButton = new ToggleTextButton(category, sender =>
        {
            ShowPage(category);
            SelectedCategoryChanged?.Invoke(sender, category);
        });

        // Each button listens to the category selection event and updates its pressed state accordingly
        SelectedCategoryChanged += (sender, _) => unstyledButton.IsPressed = sender == unstyledButton;

        return ApplyTabStyleToButton(unstyledButton);
    }

    private void SearchFieldOnTextChangedByUser(object? sender, ValueChangedEventArgs<string> e)
    {
        string searchText = e.NewValue?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(searchText))
        {
            _optionsStack.Widgets.Clear();
            _optionsStack.Widgets.Add(_optionsPanel);

            if (_lastCategory.NotNullNotEmpty())
                ShowPage(_lastCategory);

            return;
        }

        _searchPanel.Widgets.Clear();

        // This gets collapsed by IDE; Need to construct a proper search results page anyways
        foreach ((string _, List<OptionEntry> entries) in _searchIndex)
        foreach (OptionEntry entry in entries)
        foreach (OptionEntry match in entry.Match(new SearchMetadata(searchText)))
            _searchPanel.Widgets.Add(match.Render());

        _optionsStack.Widgets.Clear();
        _optionsStack.Widgets.Add(_searchPanel);
    }

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
                MinWidth = 150
            };
        }

        tabButton.ApplyButtonStyle(_tabButtonStyle);

        return tabButton;
    }

    private void ShowPage(string category)
    {
        _searchField.Text = string.Empty;
        _optionsStack.Widgets.Clear();
        _optionsStack.Widgets.Add(_optionsPanel);

        _optionsPanel.Widgets.Clear();

        _lastCategory = category;

        if (!_optionSources.TryGetValue(category, out List<IOptionSource>? sources))
            return;
        foreach (IOptionSource source in sources)
            _optionsPanel.Widgets.Add(source.Render());
    }
}
