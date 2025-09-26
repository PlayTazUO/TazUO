using System;
using System.Globalization;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.ImGuiControls;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.UI.Gumps;

public class AssistantGump : BaseOptionsGump
{
    private ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
    private Profile profile;

    public AssistantGump(World world) : base(world, 900, 700, "Assistant Features")
    {
        profile = ProfileManager.CurrentProfile;

        CenterXInViewPort();
        CenterYInViewPort();

        Build();
    }

    private void Build()
    {
        BuildAutoLoot();
        BuildAutoSell();
        BuildAutoBuy();
        BuildMobileGraphicFilter();
        BuildSpellBar();
        BuildHUD();
        BuildSpellIndicator();
        BuildJournalFilter();
        BuildTitleBar();
        BuildDressAgent();
        BuildBandageAgent();
        BuildFriendsList();
        BuildOrganizer();

        ChangePage((int)PAGE.AutoLoot);
    }

    private void BuildAutoLoot()
    {

        ModernButton button = new(0, 0, MainContent.LeftWidth, 40, ButtonAction.Default, "Auto loot", ThemeSettings.BUTTON_FONT_COLOR);
        button.MouseUp += (_, e) =>
        {
            if(e.Button == MouseButtonType.Left)
            {
                AssistantWindow.Show();
                AssistantWindow.Instance.SelectTab(PAGE.AutoLoot);
            }
        };

        MainContent.AddToLeft(button);
    }

    private void BuildAutoSell()
    {

        ModernButton button = new(0, 0, MainContent.LeftWidth, 40, ButtonAction.Default, "Auto sell", ThemeSettings.BUTTON_FONT_COLOR);
        button.MouseUp += (_, e) =>
        {
            if(e.Button == MouseButtonType.Left)
            {
                AssistantWindow.Show();
                AssistantWindow.Instance.SelectTab(PAGE.AutoSell);
            }
        };

        MainContent.AddToLeft(button);
    }

    private void BuildAutoBuy()
    {

        ModernButton button = new(0, 0, MainContent.LeftWidth, 40, ButtonAction.Default, "Auto buy", ThemeSettings.BUTTON_FONT_COLOR);
        button.MouseUp += (_, e) =>
        {
            if(e.Button == MouseButtonType.Left)
            {
                AssistantWindow.Show();
                AssistantWindow.Instance.SelectTab(PAGE.AutoBuy);
            }
        };

        MainContent.AddToLeft(button);
    }

    private void BuildAutoBuyLegacy()
    {
        var page = (int)PAGE.AutoBuy;
        MainContent.AddToLeft(CategoryButton("Auto buy", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);
        PositionHelper.Reset();

        scroll.Add(PositionHelper.PositionControl(new HttpClickableLink("Auto Buy Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Auto-Buy-Agent", ThemeSettings.TEXT_FONT_COLOR)));
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new CheckboxWithLabel(lang.GetTazUO.AutoBuyEnable, 0, profile.BuyAgentEnabled, b => profile.BuyAgentEnabled = b)));
        PositionHelper.BlankLine();

        Control c;
        scroll.Add(c = PositionHelper.PositionControl(new SliderWithLabel("Max total items (0 = unlimited)", 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.BuyAgentMaxItems, (r) => { profile.BuyAgentMaxItems = r; })));
        c.SetTooltip("Maximum total items to buy in a single transaction. Set to 0 for unlimited.");
        PositionHelper.BlankLine();

        scroll.Add(c = PositionHelper.PositionControl(new SliderWithLabel("Max unique items", 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.BuyAgentMaxUniques, (r) => { profile.BuyAgentMaxUniques = r; })));
        c.SetTooltip("Maximum number of different items to buy in a single transaction.");
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new BuyAgentConfigs(World, MainContent.RightWidth - ThemeSettings.SCROLL_BAR_WIDTH - 10)));
    }

    private void BuildMobileGraphicFilter()
    {

        ModernButton button = new(0, 0, MainContent.LeftWidth, 40, ButtonAction.Default, "Mobile Graphics", ThemeSettings.BUTTON_FONT_COLOR);
        button.MouseUp += (_, e) =>
        {
            if(e.Button == MouseButtonType.Left)
            {
                AssistantWindow.Show();
                AssistantWindow.Instance.SelectTab(PAGE.MobileGraphicFilter);
            }
        };

        MainContent.AddToLeft(button);
    }

    private void BuildSpellBar()
    {
        var page = (int)PAGE.SpellBar;
        MainContent.AddToLeft(CategoryButton("Spell Bar", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);
        PositionHelper.Reset();

        scroll.Add(PositionHelper.PositionControl(new HttpClickableLink("SpellBar Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.SpellBar", ThemeSettings.TEXT_FONT_COLOR)));
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new CheckboxWithLabel("Enable spellbar", 0, SpellBarManager.IsEnabled(), (b) =>
        {
            if (SpellBarManager.ToggleEnabled())
            {
                UIManager.Add(new SpellBar.SpellBar(World));
            }
            else
            {
                SpellBar.SpellBar.Instance?.Dispose();
            }

        })));
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new CheckboxWithLabel("Display hotkeys on spellbar", 0, profile.SpellBar_ShowHotkeys, (b) =>
        {
            profile.SpellBar_ShowHotkeys = b;
            SpellBar.SpellBar.Instance?.SetupHotkeyLabels();
        })));
        PositionHelper.BlankLine();

        ModernButton b;
        scroll.Add(PositionHelper.PositionControl(b = new ModernButton(0, 0, 100, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Add row", ThemeSettings.BUTTON_FONT_COLOR)));
        b.MouseUp += (s, e) =>
        {
            SpellBarManager.SpellBarRows.Add(new SpellBarRow());
            SpellBar.SpellBar.Instance?.Build();
        };

        ModernButton bb;
        scroll.Add(PositionHelper.ToRightOf(bb = new ModernButton(0, 0, 150, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Remove row", ThemeSettings.BUTTON_FONT_COLOR), b));
        bb.SetTooltip("This will remove the last row. If you have 5 rows, row 5 will be removed.");
        bb.MouseUp += (s, e) =>
        {
            if(SpellBarManager.SpellBarRows.Count > 1) //Make sure to always leave one row.
                SpellBarManager.SpellBarRows.RemoveAt(SpellBarManager.SpellBarRows.Count - 1);
            SpellBar.SpellBar.Instance?.Build();
        };

        var controllerHotkeys = SpellBarManager.GetControllerButtons();
        var hotkeys = SpellBarManager.GetHotKeys();
        var keymods = SpellBarManager.GetModKeys();


        for(var c = 0; c < 10; c++)
        {
            PositionHelper.BlankLine();
            Control tb;
            scroll.Add(tb = PositionHelper.PositionControl(TextBox.GetOne($"Slot {c} hotkeys: ", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default())));

            HotkeyBox hotkey = new();
            var c1 = c;

            hotkey.HotkeyChanged += (s, e) =>
            {
                SpellBarManager.SetButtons(c1, hotkey.Mod, hotkey.Key, hotkey.Buttons);
            };

            if (controllerHotkeys.Length > c)
                hotkey.SetButtons(controllerHotkeys[c]);

            if(hotkeys.Length > c && keymods.Length > c)
                hotkey.SetKey(hotkeys[c], keymods[c]);

            scroll.Add(PositionHelper.ToRightOf(hotkey, tb));
        }
    }

    private void BuildHUD()
    {
        var page = (int)PAGE.HUD;
        MainContent.AddToLeft(CategoryButton("HUD", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);

        TableContainer table = new(scroll.Width - 20, 2, (scroll.Width - 21) / 2);
        PositionHelper.Reset();

        scroll.Add(PositionHelper.PositionControl(TextBox.GetOne("Check the types of gumps you would like to toggle visibility when using the Toggle Hud Visible macro.", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(scroll.Width - 10))));
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(table));

        foreach (ulong hud in Enum.GetValues(typeof(HideHudFlags)))
        {
            if (hud == (ulong)HideHudFlags.None) continue;

            table.Add(GenHudOption(HideHudManager.GetFlagName((HideHudFlags)hud), hud));
        }

        Control GenHudOption(string name, ulong flag)
        {
            return new CheckboxWithLabel(name, 0, ByteFlagHelper.HasFlag(profile.HideHudGumpFlags, flag), b =>
            {
                if (b)
                {
                    profile.HideHudGumpFlags = ByteFlagHelper.AddFlag(profile.HideHudGumpFlags, flag);
                    if ((HideHudFlags)flag == HideHudFlags.All)
                    {
                        var ag = new AssistantGump(World){X = X, Y = Y};
                        ag.ChangePage((int)PAGE.HUD);
                        UIManager.Add(ag);
                        Dispose();
                    }
                }
                else
                {
                    if ((HideHudFlags)flag == HideHudFlags.All)
                    {
                        profile.HideHudGumpFlags = 0;
                        var ag = new AssistantGump(World){X = X, Y = Y};
                        ag.ChangePage((int)PAGE.HUD);
                        UIManager.Add(ag);
                        Dispose();
                    }
                    else
                        profile.HideHudGumpFlags = ByteFlagHelper.RemoveFlag(profile.HideHudGumpFlags, flag);
                }
            });
        }
    }

    private void BuildSpellIndicator()
    {
        var page = (int)PAGE.SpellIndicator;
        MainContent.AddToLeft(CategoryButton("Spell Indicators", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);

        PositionHelper.Reset();

        SpellRangeEditor editor = new (scroll.Width - 20);

        InputFieldWithLabel search = new ("Spell search", ThemeSettings.INPUT_WIDTH, string.Empty, false, (s, e) =>
        {
            if (SpellDefinition.TryGetSpellFromName(((BaseOptionsGump.InputField.StbTextBox)s).Text, out SpellDefinition spell))
            {
                if(SpellVisualRangeManager.Instance.SpellRangeCache.TryGetValue(spell.ID, out SpellVisualRangeManager.SpellRangeInfo info))
                    editor.SetSpellRangeInfo(info);
            }
        });

        scroll.Add(PositionHelper.PositionControl(search));
        PositionHelper.BlankLine();
        PositionHelper.BlankLine();
        scroll.Add(PositionHelper.PositionControl(editor));
    }

    private void BuildJournalFilter()
    {
        var page = (int)PAGE.JournalFilter;
        MainContent.AddToLeft(CategoryButton("Journal Filter", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);

        PositionHelper.Reset();

        scroll.Add(PositionHelper.PositionControl(new HttpClickableLink("Journal Filter Wiki", "https://github.com/PlayTazUO/TazUO/wiki/Journal-Filters", Color.White)));

        VBoxContainer vbox = new(scroll.Width - 18);

        ModernButton addButton = new(0, 0, 150, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Add entry", ThemeSettings.BUTTON_FONT_COLOR);
        addButton.MouseUp += (s, e) =>
        {
            JournalFilterManager.Instance.AddFilter("Type your matching text here");
            buildEntries();
        };

        scroll.Add(PositionHelper.PositionControl(addButton));
        scroll.Add(PositionHelper.PositionControl(vbox));
        buildEntries();

        void buildEntries()
        {
            vbox.Clear();

            foreach (string filter in JournalFilterManager.Instance.Filters)
            {
                vbox.Add(entryArea(filter));
            }
        }

        Area entryArea(string filter)
        {
            string currentFilter = filter;
            Area area = new Area();

            InputField input = new InputField(scroll.Width - 18 - 25, ThemeSettings.CHECKBOX_SIZE, text: filter);
            input.SetTooltip("Must match the journal entry exactly. Partial matches not supported.");
            input.TextChanged += (s, e) =>
            {
                var newText = ((InputField.StbTextBox)s).Text;
                JournalFilterManager.Instance.RemoveFilter(currentFilter);
                JournalFilterManager.Instance.AddFilter(newText);
                currentFilter = newText;
            };

            ModernButton del = new(scroll.Width-18-25, 0, 25, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "X", ThemeSettings.BUTTON_FONT_COLOR);
            del.SetTooltip("Delete entry");
            del.MouseUp += (s, e) =>
            {
                JournalFilterManager.Instance.RemoveFilter(currentFilter);
                buildEntries();
            };

            area.Add(input);
            area.Add(del);

            area.ForceSizeUpdate();

            return area;
        }
    }

    private void BuildTitleBar()
    {
        var page = (int)PAGE.TitleBar;
        MainContent.AddToLeft(CategoryButton("Title Bar", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);

        PositionHelper.Reset();

        scroll.Add(PositionHelper.PositionControl(TextBox.GetOne("Configure window title bar to show HP, Mana, and Stamina information.", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(MainContent.RightWidth - 20))));

        scroll.Add(PositionHelper.PositionControl(new HttpClickableLink("Title Bar Status Wiki", "https://github.com/PlayTazUO/TazUO/wiki/Title-Bar-Status", Color.White)));
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new CheckboxWithLabel("Enable title bar stats", 0, profile.EnableTitleBarStats, b =>
        {
            profile.EnableTitleBarStats = b;
            if (b)
            {
                TitleBarStatsManager.ForceUpdate();
            }
            else
            {
                Client.Game.SetWindowTitle(string.IsNullOrEmpty(World.Player?.Name) ? string.Empty : World.Player.Name);
            }
        })));
        PositionHelper.BlankLine();

        Control c;
        scroll.Add(c = PositionHelper.PositionControl(new SliderWithLabel("Update interval (ms)", 0, ThemeSettings.SLIDER_WIDTH, 500, 5000, profile.TitleBarUpdateInterval, (r) =>
        {
            profile.TitleBarUpdateInterval = r;
            TitleBarStatsManager.ForceUpdate();
        })));
        c.SetTooltip("How often to update the title bar (500ms - 5000ms)");
        PositionHelper.BlankLine();

        // Display mode radio buttons
        scroll.Add(PositionHelper.PositionControl(TextBox.GetOne("Display Mode:", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default())));
        PositionHelper.BlankLine();

        CheckboxWithLabel textCheck = null;
        CheckboxWithLabel percentCheck = null;
        CheckboxWithLabel progressCheck = null;

        textCheck = new CheckboxWithLabel("Text (HP 85/100, MP 42/50, SP 95/100)", 0, profile.TitleBarStatsMode == TitleBarStatsMode.Text, b => {
            if (b)
            {
                // Uncheck others
                percentCheck.IsChecked = false;
                progressCheck.IsChecked = false;

                profile.TitleBarStatsMode = TitleBarStatsMode.Text;
                TitleBarStatsManager.ForceUpdate();
            }
        });

        percentCheck = new CheckboxWithLabel("Percent (HP 85%, MP 84%, SP 95%)", 0, profile.TitleBarStatsMode == TitleBarStatsMode.Percent, b => {
            if (b)
            {
                // Uncheck others
                textCheck.IsChecked = false;
                progressCheck.IsChecked = false;

                profile.TitleBarStatsMode = TitleBarStatsMode.Percent;
                TitleBarStatsManager.ForceUpdate();
            }
        });

        progressCheck = new CheckboxWithLabel("Progress Bar (HP [||||||    ] MP [||||||    ] SP [||||||    ])", 0, profile.TitleBarStatsMode == TitleBarStatsMode.ProgressBar, b => {
            if (b)
            {
                // Uncheck others
                textCheck.IsChecked = false;
                percentCheck.IsChecked = false;

                profile.TitleBarStatsMode = TitleBarStatsMode.ProgressBar;
                TitleBarStatsManager.ForceUpdate();
            }
        });

        scroll.Add(PositionHelper.PositionControl(textCheck));
        scroll.Add(PositionHelper.PositionControl(percentCheck));
        scroll.Add(PositionHelper.PositionControl(progressCheck));

        PositionHelper.BlankLine();

        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(TextBox.GetOne("Note: Progress bars use Unicode block characters (█▓▒░) and may not display correctly on all systems.", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(MainContent.RightWidth - 20))));
    }

    private void BuildDressAgent()
    {
        var page = (int)PAGE.DressAgent;
        MainContent.AddToLeft(CategoryButton("Dress Agent", page, MainContent.LeftWidth));

        LeftSideMenuRightSideContent content = new LeftSideMenuRightSideContent(MainContent.RightWidth, MainContent.Height, (int)(MainContent.RightWidth * 0.3));
        MainContent.AddToRight(content, false, page);

        // Build character menu on the left
        BuildDressAgentCharacterMenu(content);
    }

    private void BuildDressAgentCharacterMenu(LeftSideMenuRightSideContent content)
    {
        DressAgentManager.Instance.Load();

        int pageBase = (int)PAGE.DressAgent + 10000; // Base page number for dress agent

        // Current character
        string currentCharacterName = ProfileManager.CurrentProfile?.CharacterName ?? "";
        ModernButton currentCharButton = SubCategoryButton(currentCharacterName, pageBase, content.LeftWidth);
        content.AddToLeft(currentCharButton);

        // Build current character's configs page
        BuildCharacterConfigsList(content, currentCharacterName, false, pageBase);

        // Other characters
        var otherCharacters = DressAgentManager.Instance.OtherCharacterConfigs.GroupBy(c => c.CharacterName).ToList();
        int index = 0;
        foreach (var characterGroup in otherCharacters)
        {
            index++;
            int charPageBase = pageBase + 1000 + index;
            ModernButton charButton = SubCategoryButton(characterGroup.Key, charPageBase, content.LeftWidth);
            content.AddToLeft(charButton);

            // Build other character's configs page
            BuildCharacterConfigsList(content, characterGroup.Key, true, charPageBase);
        }
    }

    private void BuildCharacterConfigsList(LeftSideMenuRightSideContent content, string characterName, bool readOnly, int page)
    {
        content.ResetRightSide();

        // Character header
        content.AddToRight(TextBox.GetOne($"Dress Configurations for: {characterName}", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
            ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(content.RightWidth - 40)), true, page);

        VBoxContainer configsList = new VBoxContainer(content.RightWidth - 40);

        // Add "Create New Config" button for current character
        if (!readOnly)
        {
            ModernButton createButton = new(0, 0, 200, 30, ButtonAction.Default, "+ Create New Config", ThemeSettings.BUTTON_FONT_COLOR);
            createButton.MouseUp += (s, e) =>
            {
                string configName = $"Config {DressAgentManager.Instance.CurrentPlayerConfigs.Count + 1}";
                var newConfig = DressAgentManager.Instance.CreateNewConfig(configName);
                UIManager.Add(new DressAgentConfigGump(newConfig, readOnly));
                configsList.Add(GenArea(newConfig));
            };
            configsList.Add(createButton);
        }

        // Get configs for this character
        var configs = readOnly ?
            DressAgentManager.Instance.OtherCharacterConfigs.Where(c => c.CharacterName == characterName).ToList() :
            DressAgentManager.Instance.CurrentPlayerConfigs;

        // Show configs list
        foreach (var config in configs)
        {
            configsList.Add(GenArea(config));
        }

        if (configs.Count == 0)
        {
            string message = readOnly ?
                "This character has no dress configurations." :
                "No dress configurations yet. Create your first one!";
            configsList.Add(TextBox.GetOne(message, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE,
                Color.Gray, TextBox.RTLOptions.Default()));
        }

        content.AddToRight(configsList, true, page);

        Area GenArea(DressConfig config)
        {
            Area configArea = new Area { Width = content.RightWidth - 60, Height = 40 };

            // Config name and item count
            var configLabel = TextBox.GetOne($"{config.Name} ({config.Items.Count} items)",
                ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
            configLabel.Y = 5;
            configArea.Add(configLabel);

            // Edit button
            ModernButton editButton = new(configArea.Width - 70, 4, 40, 25, ButtonAction.Default, "Edit", ThemeSettings.BUTTON_FONT_COLOR);
            editButton.MouseUp += (s, e) =>
            {
                UIManager.Add(new DressAgentConfigGump(config, readOnly));
            };
            configArea.Add(editButton);

            // Delete button for current character
            if (!readOnly)
            {
                ModernButton deleteButton = new(configArea.Width - 25, 4, 20, 25, ButtonAction.Default, "X", Color.Red);
                deleteButton.MouseUp += (s, e) =>
                {
                    DressAgentManager.Instance.DeleteConfig(config);
                    configArea.Dispose();
                };
                configArea.Add(deleteButton);
            }
            configArea.ForceSizeUpdate();
            return configArea;
        }
    }


    private void BuildBandageAgent()
    {

        ModernButton button = new(0, 0, MainContent.LeftWidth, 40, ButtonAction.Default, "Auto Bandage", ThemeSettings.BUTTON_FONT_COLOR);
        button.MouseUp += (_, e) =>
        {
            if(e.Button == MouseButtonType.Left)
            {
                AssistantWindow.Show();
                AssistantWindow.Instance.SelectTab(PAGE.BandageAgent);
            }
        };

        MainContent.AddToLeft(button);
    }

    private void BuildFriendsList()
    {
        var page = (int)PAGE.FriendsList;
        MainContent.AddToLeft(CategoryButton("Friends List", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);
        PositionHelper.Reset();

        scroll.Add(PositionHelper.PositionControl(TextBox.GetOne("Manage your friends list.", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(MainContent.RightWidth - 20))));
        PositionHelper.BlankLine();

        VBoxContainer friendsContainer = new(MainContent.RightWidth - 20);

        ModernButton addByTargetButton;
        scroll.Add(PositionHelper.PositionControl(addByTargetButton = new ModernButton(0, 0, 120, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Add by Target", ThemeSettings.BUTTON_FONT_COLOR)));
        addByTargetButton.MouseUp += (s, e) =>
        {
            GameActions.Print(World, "Target a player to add to friends list");
            World.TargetManager.SetTargeting(targeted =>
            {
                if (targeted != null && targeted is Mobile mobile)
                {
                    if (FriendsListManager.Instance.AddFriend(mobile))
                    {
                        GameActions.Print(World, $"Added {mobile.Name} to friends list");
                        friendsContainer.Add(GenFriend(FriendsListManager.Instance.GetFriend(mobile)), true);
                    }
                    else
                    {
                        GameActions.Print(World, $"Could not add {mobile.Name} - already in friends list");
                    }
                }
                else
                {
                    GameActions.Print(World, "Invalid target - must be a player");
                }
            });
        };

        PositionHelper.BlankLine();
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(TextBox.GetOne("Current Friends:", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default())));
        PositionHelper.BlankLine();

        // Display friends list
        var friends = FriendsListManager.Instance.GetFriends();

        scroll.Add(PositionHelper.PositionControl(friendsContainer));

        if (friends.Count == 0)
        {
            friendsContainer.Add(TextBox.GetOne("No friends added yet.", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default()));
        }

        foreach (var friend in friends)
        {
            friendsContainer.Add(GenFriend(friend));
        }

        Area GenFriend(FriendEntry friend)
            {
                Area friendArea = new Area();
                friendArea.Width = MainContent.RightWidth - 20;
                friendArea.Height = 30;

                // Friend name
                TextBox nameText = TextBox.GetOne(friend.Name, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
                nameText.X = 5;
                nameText.Y = 5;
                friendArea.Add(nameText);

                // Serial info (if not 0)
                if (friend.Serial != 0)
                {
                    TextBox serialText = TextBox.GetOne($"(Serial: {friend.Serial})", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE - 2, Color.Gray, TextBox.RTLOptions.Default());
                    serialText.X = nameText.Width + 15;
                    serialText.Y = 7;
                    friendArea.Add(serialText);
                }

                // Remove button
                ModernButton removeButton = new ModernButton(MainContent.RightWidth - 160, 2, 150, 26, ButtonAction.Default, "Remove", ThemeSettings.BUTTON_FONT_COLOR);
                removeButton.MouseUp += (s, e) =>
                {
                    bool removed = friend.Serial != 0
                        ? FriendsListManager.Instance.RemoveFriend(friend.Serial)
                        : FriendsListManager.Instance.RemoveFriend(friend.Name);
                    if (removed)
                    {
                        GameActions.Print(World, $"Removed {friend.Name} from friends list");
                        friendArea.Dispose();
                    }
                };
                friendArea.Add(removeButton);

                friendArea.ForceSizeUpdate();
                return friendArea;
            }
    }

    private void BuildOrganizer()
    {
        ModernButton orgButton = new(0, 0, MainContent.LeftWidth, 40, ButtonAction.Default, "Organizer", ThemeSettings.BUTTON_FONT_COLOR);
        orgButton.MouseUp += (_, e) =>
        {
            if(e.Button == MouseButtonType.Left)
            {
                AssistantWindow.Show();
                AssistantWindow.Instance.SelectTab(PAGE.Organizer);
            }
        };

        MainContent.AddToLeft(orgButton);
    }

    public override void Dispose()
    {
        base.Dispose();
        DressAgentManager.Instance?.Save();
        OrganizerAgent.Instance?.Save();
    }


    public enum PAGE
    {
        None,
        AutoLoot,
        AutoSell,
        AutoBuy,
        MobileGraphicFilter,
        SpellBar,
        HUD,
        SpellIndicator,
        JournalFilter,
        TitleBar,
        DressAgent,
        BandageAgent,
        FriendsList,
        Organizer
    }

    #region CustomControls

    private class SpellRangeEditor : Control
    {
        private int id = -1;
        private SpellVisualRangeManager.SpellRangeInfo spellRangeInfo;
        private InputFieldWithLabel name, powerWords, cursorSize, castRange, maxDuration, castTime;
        private CheckboxWithLabel islinear, showRangeDuringCast, freezeWhileCasting, targetCursorExpected;
        private ModernColorPickerWithLabel cursorHue, hue;
        public SpellRangeEditor(int width)
        {
            CanMove = true;
            AcceptMouseInput = true;
            Width = width;
            Build();
        }

        private void Build()
        {
            Positioner pos = new Positioner();
            pos.StartTable(2, (Width-40) / 2);
            pos.SetColumnAlignment(0, TableColumnAlignment.Right);
            pos.SetColumnAlignment(1, TableColumnAlignment.Right);

            Add(pos.Position(name = new InputFieldWithLabel("Name", ThemeSettings.INPUT_WIDTH, string.Empty, false, onInputChanged)));
            Add(pos.Position(powerWords = new InputFieldWithLabel("Power Words", ThemeSettings.INPUT_WIDTH, string.Empty, false, onInputChanged)));
            powerWords.SetTooltip("Power words must be exact, this is the best way we can detect spells.");

            Add(pos.Position(cursorSize = new InputFieldWithLabel("Cursor Size", ThemeSettings.INPUT_WIDTH, string.Empty, true, onInputChanged)));
            cursorSize.SetTooltip("This is the area to show around the cursor, intended for area spells that affect the area near your target.");
            Add(pos.Position(castRange = new InputFieldWithLabel("Cast Range", ThemeSettings.INPUT_WIDTH, string.Empty, true, onInputChanged)));
            Add(pos.Position(castTime = new InputFieldWithLabel("Cast Time", ThemeSettings.INPUT_WIDTH, string.Empty, false, onInputChanged)));
            Add(pos.Position(maxDuration = new InputFieldWithLabel("Max Duration", ThemeSettings.INPUT_WIDTH, string.Empty, true, onInputChanged)));
            maxDuration.SetTooltip("This is a fallback in-case the spell detection fails.");

            pos.SetColumnAlignment(0, TableColumnAlignment.Left);
            pos.SetColumnAlignment(1, TableColumnAlignment.Left);

            Add(pos.Position(cursorHue = new ModernColorPickerWithLabel(World.Instance, "Cursor Hue", 0, onHueSelected)));
            Add(pos.Position(hue = new ModernColorPickerWithLabel(World.Instance, "Range Hue", 0, onHueSelected)));

            Add(pos.Position(islinear = new CheckboxWithLabel("Is linear", 0, false, onCheckBoxChanged)));
            islinear.SetTooltip("Used for spells like wall of stone that create a line.");

            Add(pos.Position(showRangeDuringCast = new CheckboxWithLabel("Show range while casting", 0, false, onCheckBoxChanged)));
            Add(pos.Position(freezeWhileCasting = new CheckboxWithLabel("Freeze yourself while casting", 0, false, onCheckBoxChanged)));
            freezeWhileCasting.SetTooltip("Prevent yourself from moving and disrupting your spell.");

            Add(pos.Position(targetCursorExpected = new CheckboxWithLabel("Spell should create a target cursor", 0, false, onCheckBoxChanged)));
        }

        public void SetSpellRangeInfo(SpellVisualRangeManager.SpellRangeInfo info)
        {
            id = -1;
            spellRangeInfo = null;
            if (info == null)
                return;

            name.SetText(info.Name);
            powerWords.SetText(info.PowerWords);
            cursorSize.SetText(info.CursorSize.ToString());
            cursorHue.Hue = info.CursorHue;
            castRange.SetText(info.CastRange.ToString());
            hue.Hue = info.Hue;
            castTime.SetText(info.CastTime.ToString());
            maxDuration.SetText(info.MaxDuration.ToString());
            islinear.IsChecked = info.IsLinear;
            showRangeDuringCast.IsChecked = info.ShowCastRangeDuringCasting;
            freezeWhileCasting.IsChecked = info.FreezeCharacterWhileCasting;
            targetCursorExpected.IsChecked = info.ExpectTargetCursor;

            //Set these at the end to prevent the input saving being triggered via SetText
            id = info.ID;
            spellRangeInfo = info;
        }
        private void onHueSelected(ushort _)
        {
            Save();
        }

        private void onCheckBoxChanged(bool _)
        {
            Save();
        }

        private void onInputChanged(object _, EventArgs __)
        {
            Save();
        }

        private void Save()
        {
            if(id < 0 || spellRangeInfo == null)
                return;

            spellRangeInfo.Name = name.Text;
            spellRangeInfo.PowerWords = powerWords.Text;

            if (int.TryParse(cursorSize.Text, out int ri))
                spellRangeInfo.CursorSize = ri;

            spellRangeInfo.CursorHue = cursorHue.Hue;

            if(int.TryParse(castRange.Text, out ri))
                spellRangeInfo.CastRange = ri;

            spellRangeInfo.Hue = hue.Hue;

            if(double.TryParse(castTime.Text, out double d))
                spellRangeInfo.CastTime = d;

            if(int.TryParse(maxDuration.Text, out ri))
                spellRangeInfo.MaxDuration = ri;

            spellRangeInfo.IsLinear = islinear.IsChecked;
            spellRangeInfo.ShowCastRangeDuringCasting = showRangeDuringCast.IsChecked;
            spellRangeInfo.FreezeCharacterWhileCasting = freezeWhileCasting.IsChecked;
            spellRangeInfo.ExpectTargetCursor = targetCursorExpected.IsChecked;
            SpellVisualRangeManager.Instance.DelayedSave();
        }
    }
    private class BuyAgentConfigs : Control
    {
        private VBoxContainer _container;

        public BuyAgentConfigs(World world, int width)
        {
            AcceptMouseInput = true;
            CanMove = true;
            Width = width;

            Add(_container = new VBoxContainer(width));

            ModernButton b;
            _container.Add(b = new ModernButton(0, 0, 100, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Add entry", ThemeSettings.BUTTON_FONT_COLOR));

            b.MouseUp += (s, e) =>
            {
                var newEntry = GenConfigEntry(BuySellAgent.Instance.NewBuyConfig(), width);
                _container.Add(newEntry);
                RefreshLayout();
            };

            _container.Add(b = new ModernButton(0, 0, 150, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Target item", ThemeSettings.BUTTON_FONT_COLOR));

            b.MouseUp += (s, e) =>
            {
                world.TargetManager.SetTargeting((e) =>
                    {
                        if (e == null || !(e is Entity entity))
                            return;

                        var sc = BuySellAgent.Instance.NewBuyConfig();
                        sc.Graphic = entity.Graphic;
                        sc.Hue = entity.Hue;
                        var newEntry = GenConfigEntry(sc, width);
                        _container.Add(newEntry);
                        RefreshLayout();
                    }
                );
            };

            Area titles = new Area(false);

            TextBox tempTextBox1 = TextBox.GetOne("Graphic", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(null));
            tempTextBox1.X = 50;
            titles.Add(tempTextBox1);

            tempTextBox1 = TextBox.GetOne("Hue", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(null));
            tempTextBox1.X = 50 + ((width - 90 - 60) / 5) + 5;
            titles.Add(tempTextBox1);

            tempTextBox1 = TextBox.GetOne("Max Amount", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(null));
            tempTextBox1.X = 50 + (((width - 90 - 60) / 5) * 2) + 10;
            titles.Add(tempTextBox1);

            tempTextBox1 = TextBox.GetOne("Restock Up To", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(null));
            tempTextBox1.X = 50 + (((width - 90 - 60) / 5) * 3) + 15;
            titles.Add(tempTextBox1);

            titles.ForceSizeUpdate();
            _container.Add(titles);

            if (BuySellAgent.Instance.BuyConfigs != null)
                foreach (var item in BuySellAgent.Instance.BuyConfigs)
                {
                    _container.Add(GenConfigEntry(item, width));
                }

            RefreshLayout();
        }

        private Control GenConfigEntry(BuySellItemConfig itemConfig, int width)
        {
            int ewidth = (width - 90 - 60) / 5; // Divide by 5 instead of 3 for smaller inputs

            Area area = new Area()
            {
                Width = width,
                Height = 50
            };

            int x = 0;

            if (itemConfig.Graphic > 0)
            {
                ResizableStaticPic rsp;

                area.Add
                (
                    rsp = new ResizableStaticPic(itemConfig.Graphic, 50, 50)
                    {
                        Hue = (ushort)(itemConfig.Hue == ushort.MaxValue ? 0 : itemConfig.Hue)
                    }
                );
            }

            x += 50;

            InputField graphicInput = new InputField
            (
                ewidth, 50, 100, -1, itemConfig.Graphic.ToString(), false, (s, e) =>
                {
                    InputField.StbTextBox graphicInput = (InputField.StbTextBox)s;

                    if (graphicInput.Text.StartsWith("0x") && ushort.TryParse(graphicInput.Text.Substring(2), NumberStyles.AllowHexSpecifier, null, out var ngh))
                    {
                        itemConfig.Graphic = ngh;
                    }
                    else if (ushort.TryParse(graphicInput.Text, out var ng))
                    {
                        itemConfig.Graphic = ng;
                    }
                }
            )
            {
                X = x
            };

            graphicInput.SetTooltip("Graphic");
            area.Add(graphicInput);
            x += graphicInput.Width + 5;


            InputField hueInput = new InputField
            (
                ewidth, 50, 100, -1, itemConfig.Hue == ushort.MaxValue ? "-1" : itemConfig.Hue.ToString(), false, (s, e) =>
                {
                    InputField.StbTextBox hueInput = (InputField.StbTextBox)s;

                    if (hueInput.Text == "-1")
                    {
                        itemConfig.Hue = ushort.MaxValue;
                    }
                    else if (ushort.TryParse(hueInput.Text, out var ng))
                    {
                        itemConfig.Hue = ng;
                    }
                }
            )
            {
                X = x
            };

            hueInput.SetTooltip("Hue (-1 to match any)");
            area.Add(hueInput);
            x += hueInput.Width + 5;

            InputField maxInput = new InputField
            (
                ewidth, 50, 100, -1, itemConfig.MaxAmount.ToString(), false, (s, e) =>
                {
                    InputField.StbTextBox maxInput = (InputField.StbTextBox)s;

                    if (ushort.TryParse(maxInput.Text, out var ng))
                    {
                        itemConfig.MaxAmount = ng;
                    }
                }
            )
            {
                X = x
            };

            maxInput.SetTooltip("Max Amount");
            area.Add(maxInput);
            x += maxInput.Width + 5;

            InputField restockInput = new InputField
            (
                ewidth, 50, 100, -1, itemConfig.RestockUpTo.ToString(), false, (s, e) =>
                {
                    InputField.StbTextBox restockInput = (InputField.StbTextBox)s;

                    if (ushort.TryParse(restockInput.Text, out var ng))
                    {
                        itemConfig.RestockUpTo = ng;
                    }
                }
            )
            {
                X = x
            };

            restockInput.SetTooltip("Restock up to this amount (0 = disabled)");
            area.Add(restockInput);
            x += restockInput.Width + 5;

            CheckboxWithLabel enabled = new CheckboxWithLabel(isChecked: itemConfig.Enabled, valueChanged: (e) => { itemConfig.Enabled = e; })
            {
                X = x
            };

            enabled.Y = (area.Height - enabled.Height) >> 1;
            enabled.SetTooltip("Enable this entry?");
            area.Add(enabled);
            x += enabled.Width;

            NiceButton delete;

            area.Add
            (
                delete = new NiceButton(x, 0, area.Width - x, 49, ButtonAction.Activate, "X")
                {
                    IsSelectable = false,
                    DisplayBorder = true
                }
            );

            delete.SetTooltip("Delete this entry");

            delete.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    BuySellAgent.Instance?.DeleteConfig(itemConfig);
                    _container.Remove(area);
                    area.Dispose();
                    RefreshLayout();
                }
            };

            return area;
        }

        private void RefreshLayout()
        {
            _container.ForceSizeUpdate();
            Height = _container.Height;
        }
    }

    #endregion
}
