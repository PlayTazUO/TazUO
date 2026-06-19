#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;
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

    /// <summary>
    ///     Category, (subcategory? widget)
    /// </summary>
    private readonly Dictionary<string, List<OptionItem>> _options = new();

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
        SetupGameplayTab();
        SetupInterfaceOptions();
        SetupVideo();
        SetupSound();
        SetupChatOptions();
        SetupMiscOptions();
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

        _searchField.HintText = "Search...";
        _searchField.TextChangedByUser += SearchFieldOnTextChangedByUser;
        _mainArea.AddWidget(_searchField, 0, 0, null, 2);

        WrapPanel categoryPanel = new()
        {
            Orientation = Orientation.Vertical, HorizontalSpacing = MyraStyle.STANDARD_SPACING, VerticalSpacing = MyraStyle.STANDARD_SPACING
        };

        _mainArea.AddWidget(categoryPanel.WrapInScroll(MAX_HEIGHT), 1, 0);

        _optionsStack.Widgets.Add(_optionsPanel);
        _mainArea.AddWidget(_optionsStack, 1, 1);

        foreach (string category in _options.Keys.Concat(_optionSources.Keys).Distinct())
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

        foreach ((string _, List<OptionEntry> entries) in _searchIndex)
        {
            foreach (OptionEntry entry in entries)
            {
                foreach (OptionEntry match in entry.Match(new SearchMetadata(searchText)))
                    _searchPanel.Widgets.Add(match.Render());
            }
        }

        foreach ((string _, List<OptionItem> items) in _options)
        {
            foreach (OptionItem item in items.Where(item => item.MatchesSearch(searchText)))
                _searchPanel.Widgets.Add(item);
        }

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

        if (_optionSources.TryGetValue(category, out List<IOptionSource>? sources))
        {
            foreach (IOptionSource source in sources)
                _optionsPanel.Widgets.Add(source.Render());

            return;
        }

        if (_options.TryGetValue(category, out List<OptionItem>? legacyItems))
        {
            foreach (OptionItem optionItem in legacyItems)
                _optionsPanel.Widgets.Add(optionItem);
        }
    }

    private void SetupInterfaceOptions()
    {
        const string interfaceKey = "Interface";
        AddOptionSource(interfaceKey, InterfaceTab.GetContent());
    }

    private void SetupMiscOptions()
    {
        const string miscKey = "Misc";

        if (!_options.ContainsKey(miscKey))
            _options.Add(miscKey, []);

        _options[miscKey].Add(MiscTab.GetContent());
    }

    private void SetupGameplayTab()
    {
        const string gameplayKey = "Gameplay";
        if (!_options.ContainsKey(gameplayKey))
            _options.Add(gameplayKey, []);
        _options[gameplayKey].Add(GameplayTab.GetContent());
    }

    private void SetupSound()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Sound soundLang = lang.GetSound;

        if (!_options.ContainsKey("Sound"))
            _options.Add("Sound", []);

        List<OptionItem> opt = _options["Sound"];

        opt.Add(
            new OptionItem(
                soundLang.EnableSound,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.EnableSound), soundLang.EnableSound),
                    OptionsFactory.CreateSliderOption(
                        soundLang.SharedVolume,
                        0,
                        100,
                        profile.SoundVolume,
                        f => profile.SoundVolume = (int)f
                    )
                )
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            new OptionItem(
                soundLang.EnableMusic,
                () => new CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.EnableMusic), soundLang.EnableMusic),
                    OptionsFactory.CreateSliderOption(
                        soundLang.SharedVolume,
                        0,
                        100,
                        profile.MusicVolume,
                        f => profile.MusicVolume = (int)f
                    )
                )
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            new OptionItem(
                soundLang.LoginMusic,
                () => new CheckBoxGroup(
                    new PropertyBinder(
                        new Accessor<bool>(() => Settings.GlobalSettings.LoginMusic),
                        soundLang.LoginMusic
                    ),
                    OptionsFactory.CreateSliderOption(
                        soundLang.SharedVolume,
                        0,
                        100,
                        Settings.GlobalSettings.LoginMusicVolume,
                        f => Settings.GlobalSettings.LoginMusicVolume = (int)f
                    )
                )
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                soundLang.PlayFootsteps,
                new Accessor<bool>(() => profile.EnableFootstepsSound)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                soundLang.CombatMusic,
                new Accessor<bool>(() => profile.EnableCombatMusic)
            )
        );
        opt.Add(
            OptionsFactory.CreateCheckboxOption(
                soundLang.BackgroundMusic,
                new Accessor<bool>(() => profile.ReproduceSoundsInBackground)
            )
        );

        opt.Add(OptionsFactory.CreateSpacer());

        opt.Add(
            new OptionItem(
                "Voice to text",
                () => new MyraButton(
                    "Create voice toggle button",
                    () =>
                    {
                        var macroManager = MacroManager.TryGetMacroManager(World.Instance);
                        if (macroManager == null)
                            return;
                        var macro = Macro.CreateFastMacro(
                            "Toggle Voice",
                            MacroType.ToggleVoiceRecognition,
                            MacroSubType.MSC_NONE
                        );
                        macroManager.PushToBack(macro);
                        UIManager.Add(
                            new MacroButtonGump(
                                World.Instance,
                                macro,
                                Mouse.Position.X,
                                Mouse.Position.Y
                            )
                        );
                    }
                )
            )
        );
        ModernOptionsGumpLanguage.TazUO voiceLang = lang.GetTazUO;
        opt.Add(
            OptionsFactory.CreateInputField(
                voiceLang.VoiceModelPath,
                profile.VoiceModelPath,
                s => profile.VoiceModelPath = s,
                voiceLang.VoiceModelPathTooltip
            )
        );
    }

    private void SetupVideo()
    {
        const string videoKey = "Video";

        if (!_options.ContainsKey(videoKey))
            _options.Add(videoKey, []);

        List<OptionItem> optionsList = _options[videoKey];
        optionsList.Add(VideoTab.GetContent());
    }

    private void SetupChatOptions()
    {
        string chatAndSpeechKey = Language.Instance.GetModernOptionsGumpLanguage.LabelChatAndText;

        if (!_options.ContainsKey(chatAndSpeechKey))
            _options.Add(chatAndSpeechKey, []);

        _options[chatAndSpeechKey].Add(ChatTab.GetContent());
    }
}
