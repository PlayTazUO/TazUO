using System;
using System.Globalization;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps;

public class AssistantGump : BaseOptionsGump
{
    private ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
    private Profile profile;

    public AssistantGump() : base(900, 700, "Assistant Features")
    {
        profile = ProfileManager.CurrentProfile;

        CenterXInScreen();
        CenterYInScreen();

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

        ChangePage((int)PAGE.AutoLoot);
    }

    private void BuildAutoLoot()
    {
        int page = (int)PAGE.AutoLoot;

        MainContent.AddToLeft(CategoryButton("Auto loot", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);
        PositionHelper.Reset();

        scroll.Add
            (PositionHelper.PositionControl(new HttpClickableLink("Autoloot Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Simple-Auto-Loot", ThemeSettings.TEXT_FONT_COLOR)));

        PositionHelper.BlankLine();

        ModernButton b;
        scroll.Add(PositionHelper.PositionControl(b = new ModernButton(0, 0, 200, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Set grab bag", ThemeSettings.BUTTON_FONT_COLOR)));
        b.MouseUp += (s, e) =>
        {
            GameActions.Print(ResGumps.TargetContainerToGrabItemsInto);
            TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
        };
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new CheckboxWithLabel(lang.GetTazUO.AutoLootEnable, 0, profile.EnableAutoLoot, b => profile.EnableAutoLoot = b)));
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new CheckboxWithLabel(lang.GetTazUO.ScavengerEnable, 0, profile.EnableScavenger, b => profile.EnableScavenger = b)));
        PositionHelper.BlankLine();

        scroll.Add
        (
            PositionHelper.PositionControl
                (new CheckboxWithLabel(lang.GetTazUO.AutoLootProgessBarEnable, 0, profile.EnableAutoLootProgressBar, b => profile.EnableAutoLootProgressBar = b))
        );

        PositionHelper.BlankLine();

        scroll.Add
            (PositionHelper.PositionControl(new CheckboxWithLabel(lang.GetTazUO.AutoLootHumanCorpses, 0, profile.AutoLootHumanCorpses, b => profile.AutoLootHumanCorpses = b)));

        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new AutoLootConfigs(MainContent.RightWidth - ThemeSettings.SCROLL_BAR_WIDTH - 10)));
    }

    private void BuildAutoSell()
    {
        var page = (int)PAGE.AutoSell;
        Control c;
        MainContent.AddToLeft(CategoryButton("Auto sell", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);
        PositionHelper.Reset();

        scroll.Add(PositionHelper.PositionControl(new HttpClickableLink("Auto Sell Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Auto-Sell-Agent", ThemeSettings.TEXT_FONT_COLOR)));
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new CheckboxWithLabel(lang.GetTazUO.AutoSellEnable, 0, profile.SellAgentEnabled, b => profile.SellAgentEnabled = b)));
        PositionHelper.BlankLine();

        scroll.Add(c = PositionHelper.PositionControl(new SliderWithLabel(lang.GetTazUO.AutoSellMaxItems, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.SellAgentMaxItems, (r) => { profile.SellAgentMaxItems = r; })));
        c.SetTooltip(lang.GetTazUO.AutoSellMaxItemsTooltip);
        PositionHelper.BlankLine();

        scroll.Add(c = PositionHelper.PositionControl(new SliderWithLabel(lang.GetTazUO.AutoSellMaxUniques, 0, ThemeSettings.SLIDER_WIDTH, 0, 100, profile.SellAgentMaxUniques, (r) => { profile.SellAgentMaxUniques = r; })));
        c.SetTooltip(lang.GetTazUO.AutoSellMaxUniquesTooltip);
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(new SellAgentConfigs(MainContent.RightWidth - ThemeSettings.SCROLL_BAR_WIDTH - 10)));
    }

    private void BuildAutoBuy()
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

        scroll.Add(PositionHelper.PositionControl(new BuyAgentConfigs(MainContent.RightWidth - ThemeSettings.SCROLL_BAR_WIDTH - 10)));
    }

    private void BuildMobileGraphicFilter()
    {
        var page = (int)PAGE.MobileGraphicFilter;
        MainContent.AddToLeft(CategoryButton("Mobile Graphics", page, MainContent.LeftWidth));
        MainContent.ResetRightSide();

        ScrollArea scroll = new(0, 0, MainContent.RightWidth, MainContent.Height);
        MainContent.AddToRight(scroll, false, page);
        PositionHelper.Reset();

        scroll.Add(PositionHelper.PositionControl(new HttpClickableLink("Mobile Graphic Filter Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Mobile-Graphics-Filter", ThemeSettings.TEXT_FONT_COLOR)));
        scroll.Add(PositionHelper.PositionControl(TextBox.GetOne("This can be used to replace graphics of mobiles with other graphics(For example if dragons are too big, replace them with wyverns).", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(MainContent.RightWidth - 20))));
        PositionHelper.BlankLine();
        scroll.Add(PositionHelper.PositionControl(new GraphicFilterConfigs(MainContent.RightWidth - ThemeSettings.SCROLL_BAR_WIDTH - 10)));
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
                UIManager.Add(new SpellBar.SpellBar());
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
                        var ag = new AssistantGump(){X = X, Y = Y};
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
                        var ag = new AssistantGump(){X = X, Y = Y};
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
        scroll.Add(c = PositionHelper.PositionControl(new SliderWithLabel("Update interval (ms)", 0, ThemeSettings.SLIDER_WIDTH, 500, 5000, profile.TitleBarUpdateInterval, (r) => { profile.TitleBarUpdateInterval = r; })));
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

        scroll.Add(PositionHelper.PositionControl(TextBox.GetOne("Preview:", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default())));
        PositionHelper.BlankLine();

        scroll.Add(PositionHelper.PositionControl(TextBox.GetOne("Note: Progress bars use Unicode block characters (█▓▒░) and may not display correctly on all systems.", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(MainContent.RightWidth - 20))));
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
        TitleBar
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

            Add(pos.Position(cursorHue = new ModernColorPickerWithLabel("Cursor Hue", 0, onHueSelected)));
            Add(pos.Position(hue = new ModernColorPickerWithLabel("Range Hue", 0, onHueSelected)));

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
    private class AutoLootConfigs : Control
    {
        private DataBox _dataBox;

        public AutoLootConfigs(int width)
        {
            AcceptMouseInput = true;
            CanMove = true;
            Width = width;

            Add(_dataBox = new DataBox(0, 0, width, 0));

            ModernButton b;
            _dataBox.Add(b = new ModernButton(0, 0, 100, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Add entry", ThemeSettings.BUTTON_FONT_COLOR));

            b.MouseUp += (s, e) =>
            {
                var nl = AutoLootManager.Instance.AddAutoLootEntry();
                _dataBox.Insert(2, GenConfigEntry(nl, width));
                RearrangeDataBox();
            };

            _dataBox.Add(b = new ModernButton(0, 0, 200, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Target item to add", ThemeSettings.BUTTON_FONT_COLOR));

            b.MouseUp += (s, e) =>
            {
                TargetHelper.TargetObject
                ((o) =>
                    {
                        if (o != null)
                        {
                            var nl = AutoLootManager.Instance.AddAutoLootEntry(o.Graphic, o.Hue, o.Name);

                            if (_dataBox != null)
                            {
                                _dataBox.Insert(2, GenConfigEntry(nl, width));
                                RearrangeDataBox();
                            }
                        }
                    }
                );
            };

            Area titles = new Area(false);
            TextBox tempTextBox1 = TextBox.GetOne("Graphic", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
            tempTextBox1.X = 55;
            titles.Add(tempTextBox1);

            tempTextBox1 = TextBox.GetOne("Hue", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
            tempTextBox1.X = ((width - 90 - 50) >> 1) + 60;
            titles.Add(tempTextBox1);

            titles.ForceSizeUpdate();
            _dataBox.Add(titles);

            for (int i = 0; i < AutoLootManager.Instance.AutoLootList.Count; i++)
            {
                AutoLootManager.AutoLootConfigEntry autoLootItem = AutoLootManager.Instance.AutoLootList[i];
                _dataBox.Add(GenConfigEntry(autoLootItem, width));
            }

            RearrangeDataBox();
        }

        private Control GenConfigEntry(AutoLootManager.AutoLootConfigEntry autoLootItem, int width)
        {
            int ewidth = (width - 90 - 60) >> 1;

            Area area = new Area()
            {
                Width = width,
                Height = 107
            };

            int x = 0;

            if (autoLootItem.Graphic > 0)
            {
                ResizableStaticPic rsp;

                area.Add
                (
                    rsp = new ResizableStaticPic((uint)autoLootItem.Graphic, 50, 50)
                    {
                        Hue = (ushort)(autoLootItem.Hue == ushort.MaxValue ? 0 : autoLootItem.Hue)
                    }
                );

                rsp.SetTooltip(autoLootItem.Name);
            }

            x += 50;

            InputField graphicInput = new InputField
            (
                ewidth, 50, 100, -1, autoLootItem.Graphic.ToString(), false, (s, e) =>
                {
                    InputField.StbTextBox graphicInput = (InputField.StbTextBox)s;

                    if (graphicInput.Text.StartsWith("0x") && short.TryParse(graphicInput.Text.Substring(2), NumberStyles.AllowHexSpecifier, null, out var ngh))
                    {
                        autoLootItem.Graphic = ngh;
                    }
                    else if (int.TryParse(graphicInput.Text, out var ng))
                    {
                        autoLootItem.Graphic = ng;
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
                ewidth, 50, 100, -1, autoLootItem.Hue == ushort.MaxValue ? "-1" : autoLootItem.Hue.ToString(), false, (s, e) =>
                {
                    InputField.StbTextBox hueInput = (InputField.StbTextBox)s;

                    if (hueInput.Text == "-1")
                    {
                        autoLootItem.Hue = ushort.MaxValue;
                    }
                    else if (ushort.TryParse(hueInput.Text, out var ng))
                    {
                        autoLootItem.Hue = ng;
                    }
                }
            )
            {
                X = x
            };

            hueInput.SetTooltip("Hue (-1 to match any)");
            area.Add(hueInput);
            x += hueInput.Width + 5;

            NiceButton delete;

            area.Add
            (
                delete = new NiceButton(x, 0, 90, 49, ButtonAction.Activate, "Delete")
                {
                    IsSelectable = false,
                    DisplayBorder = true
                }
            );

            delete.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    AutoLootManager.Instance.TryRemoveAutoLootEntry(autoLootItem.UID);
                    area.Dispose();
                    RearrangeDataBox();
                }
            };

            InputField regxInput = new InputField
            (
                width, 50, width, -1, autoLootItem.RegexSearch, false, (s, e) =>
                {
                    InputField.StbTextBox regxInput = (InputField.StbTextBox)s;
                    autoLootItem.RegexSearch = string.IsNullOrEmpty(regxInput.Text) ? string.Empty : regxInput.Text;
                }
            )
            {
                Y = 52
            };

            regxInput.SetTooltip("Regex to match items against");
            area.Add(regxInput);

            return area;
        }

        private void RearrangeDataBox()
        {
            _dataBox.ReArrangeChildren();
            _dataBox.ForceSizeUpdate();
            Height = _dataBox.Height;
        }
    }
    private class SellAgentConfigs : Control
    {
        private DataBox _dataBox;

        public SellAgentConfigs(int width)
        {
            AcceptMouseInput = true;
            CanMove = true;
            Width = width;

            Add(_dataBox = new DataBox(0, 0, width, 0));

            ModernButton b;
            _dataBox.Add(b = new ModernButton(0, 0, 100, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Add entry", ThemeSettings.BUTTON_FONT_COLOR));

            b.MouseUp += (s, e) =>
            {
                _dataBox.Insert(3, GenConfigEntry(BuySellAgent.Instance.NewSellConfig(), width));
                RearrangeDataBox();
            };

            _dataBox.Add(b = new ModernButton(0, 0, 150, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Target item", ThemeSettings.BUTTON_FONT_COLOR));

            b.MouseUp += (s, e) =>
            {
                TargetHelper.TargetObject
                ((e) =>
                    {
                        if (e == null)
                            return;

                        var sc = BuySellAgent.Instance.NewSellConfig();
                        sc.Graphic = e.Graphic;
                        sc.Hue = e.Hue;
                        _dataBox.Insert(3, GenConfigEntry(sc, width));
                        RearrangeDataBox();
                    }
                );
            };

            Area titles = new Area(false);

            TextBox tempTextBox1 = TextBox.GetOne("Graphic", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
            tempTextBox1.X = 50;
            titles.Add(tempTextBox1);

            tempTextBox1 = TextBox.GetOne("Hue", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
            tempTextBox1.X = ((width - 90 - 60) / 3) + 55;
            titles.Add(tempTextBox1);

            tempTextBox1 = TextBox.GetOne("Max Amount", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
            tempTextBox1.X = (((width - 90 - 60) / 3) * 2) + 60;
            titles.Add(tempTextBox1);

            titles.ForceSizeUpdate();
            _dataBox.Add(titles);

            if (BuySellAgent.Instance.SellConfigs != null)
                foreach (var item in BuySellAgent.Instance.SellConfigs)
                {
                    _dataBox.Add(GenConfigEntry(item, width));
                }

            RearrangeDataBox();
        }

        private Control GenConfigEntry(BuySellItemConfig itemConfig, int width)
        {
            int ewidth = (width - 90 - 60) / 3;

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
                    area.Dispose();
                    RearrangeDataBox();
                }
            };

            return area;
        }

        private void RearrangeDataBox()
        {
            _dataBox.ReArrangeChildren();
            _dataBox.ForceSizeUpdate();
            Height = _dataBox.Height;
        }
    }
    private class BuyAgentConfigs : Control
    {
        private DataBox _dataBox;

        public BuyAgentConfigs(int width)
        {
            AcceptMouseInput = true;
            CanMove = true;
            Width = width;

            Add(_dataBox = new DataBox(0, 0, width, 0));

            ModernButton b;
            _dataBox.Add(b = new ModernButton(0, 0, 100, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Add entry", ThemeSettings.BUTTON_FONT_COLOR));

            b.MouseUp += (s, e) =>
            {
                _dataBox.Insert(3, GenConfigEntry(BuySellAgent.Instance.NewBuyConfig(), width));
                RearrangeDataBox();
            };

            _dataBox.Add(b = new ModernButton(0, 0, 150, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Target item", ThemeSettings.BUTTON_FONT_COLOR));

            b.MouseUp += (s, e) =>
            {
                TargetHelper.TargetObject
                ((e) =>
                    {
                        if (e == null)
                            return;

                        var sc = BuySellAgent.Instance.NewBuyConfig();
                        sc.Graphic = e.Graphic;
                        sc.Hue = e.Hue;
                        _dataBox.Insert(3, GenConfigEntry(sc, width));
                        RearrangeDataBox();
                    }
                );
            };

            Area titles = new Area(false);

            TextBox tempTextBox1 = TextBox.GetOne("Graphic", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(null));

            tempTextBox1.X = 50;
            titles.Add(tempTextBox1);

            tempTextBox1 = TextBox.GetOne("Hue", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(null));
            tempTextBox1.X = ((width - 90 - 60) / 3) + 55;
            titles.Add(tempTextBox1);

            tempTextBox1 = TextBox.GetOne("Max Amount", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(null));
            tempTextBox1.X = (((width - 90 - 60) / 3) * 2) + 60;
            titles.Add(tempTextBox1);

            titles.ForceSizeUpdate();
            _dataBox.Add(titles);

            if (BuySellAgent.Instance.BuyConfigs != null)
                foreach (var item in BuySellAgent.Instance.BuyConfigs)
                {
                    _dataBox.Add(GenConfigEntry(item, width));
                }

            RearrangeDataBox();
        }

        private Control GenConfigEntry(BuySellItemConfig itemConfig, int width)
        {
            int ewidth = (width - 90 - 60) / 3;

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
                    area.Dispose();
                    RearrangeDataBox();
                }
            };

            return area;
        }

        private void RearrangeDataBox()
        {
            _dataBox.ReArrangeChildren();
            _dataBox.ForceSizeUpdate();
            Height = _dataBox.Height;
        }
    }
    private class GraphicFilterConfigs : Control
        {
            private DataBox _dataBox;

            public GraphicFilterConfigs(int width)
            {
                AcceptMouseInput = true;
                CanMove = true;
                Width = width;

                Add(_dataBox = new DataBox(0, 0, width, 0));

                ModernButton b;
                _dataBox.Add(b = new ModernButton(0, 0, 150, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Add blank entry", ThemeSettings.BUTTON_FONT_COLOR));

                b.MouseUp += (s, e) =>
                {
                    var newConfig = GraphicsReplacement.NewFilter(0, 0);

                    if (newConfig != null)
                    {
                        _dataBox.Insert(3, GenConfigEntry(newConfig, width));
                        RearrangeDataBox();
                    }
                };

                _dataBox.Add(b = new ModernButton(0, 0, 150, ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Target entity", ThemeSettings.BUTTON_FONT_COLOR));

                b.MouseUp += (s, e) =>
                {
                    TargetHelper.TargetObject
                    ((e) =>
                        {
                            if (e == null)
                                return;

                            // if (e == null || !SerialHelper.IsMobile(e)) return;
                            var sc = GraphicsReplacement.NewFilter(e.Graphic, e.Graphic, e.Hue);

                            if (sc != null && _dataBox != null)
                            {
                                _dataBox.Insert(3, GenConfigEntry(sc, width));
                                RearrangeDataBox();
                            }
                        }
                    );
                };

                Area titles = new Area(false);

                Control c;
                titles.Add(TextBox.GetOne("Graphic", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default()));
                titles.Add(c = TextBox.GetOne("New Graphic", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default()));
                c.X = ((width - 90 - 5) / 3) + 5;
                titles.Add(c = TextBox.GetOne("New Hue", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default()));
                c.X = (((width - 90 - 5) / 3) * 2) + 10;
                titles.ForceSizeUpdate();
                _dataBox.Add(titles);

                foreach (var item in GraphicsReplacement.GraphicFilters)
                {
                    _dataBox.Add(GenConfigEntry(item.Value, width));
                }

                RearrangeDataBox();
            }

            private Control GenConfigEntry(GraphicChangeFilter filter, int width)
            {
                int ewidth = (width - 90) / 3;

                Area area = new Area()
                {
                    Width = width,
                    Height = 50
                };

                int x = 0;

                InputField graphicInput = new InputField
                (
                    ewidth, 50, 100, -1, filter.OriginalGraphic.ToString(), false, (s, e) =>
                    {
                        InputField.StbTextBox graphicInput = (InputField.StbTextBox)s;

                        if (graphicInput.Text.StartsWith("0x") && ushort.TryParse(graphicInput.Text.Substring(2), NumberStyles.AllowHexSpecifier, null, out var ngh))
                        {
                            filter.OriginalGraphic = ngh;
                            GraphicsReplacement.ResetLists();
                        }
                        else if (ushort.TryParse(graphicInput.Text, out var ng))
                        {
                            filter.OriginalGraphic = ng;
                            GraphicsReplacement.ResetLists();
                        }
                    }
                )
                {
                    X = x
                };

                graphicInput.SetTooltip("Original Graphic");
                area.Add(graphicInput);
                x += graphicInput.Width + 5;

                InputField newgraphicInput = new InputField
                (
                    ewidth, 50, 100, -1, filter.ReplacementGraphic.ToString(), false, (s, e) =>
                    {
                        InputField.StbTextBox graphicInput = (InputField.StbTextBox)s;

                        if (graphicInput.Text.StartsWith("0x") && ushort.TryParse(graphicInput.Text.Substring(2), NumberStyles.AllowHexSpecifier, null, out var ngh))
                        {
                            filter.ReplacementGraphic = ngh;
                        }
                        else if (ushort.TryParse(graphicInput.Text, out var ng))
                        {
                            filter.ReplacementGraphic = ng;
                        }
                    }
                )
                {
                    X = x
                };

                newgraphicInput.SetTooltip("Replacement Graphic");
                area.Add(newgraphicInput);
                x += newgraphicInput.Width + 5;

                InputField hueInput = new InputField
                (
                    ewidth, 50, 100, -1, filter.NewHue == ushort.MaxValue ? "-1" : filter.NewHue.ToString(), false, (s, e) =>
                    {
                        InputField.StbTextBox hueInput = (InputField.StbTextBox)s;

                        if (hueInput.Text == "-1")
                        {
                            filter.NewHue = ushort.MaxValue;
                        }
                        else if (ushort.TryParse(hueInput.Text, out var ng))
                        {
                            filter.NewHue = ng;
                        }
                    }
                )
                {
                    X = x
                };

                hueInput.SetTooltip("Hue (-1 to leave original)");
                area.Add(hueInput);
                x += hueInput.Width + 5;

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
                        GraphicsReplacement.DeleteFilter(filter.OriginalGraphic);
                        area.Dispose();
                        RearrangeDataBox();
                    }
                };

                return area;
            }

            private void RearrangeDataBox()
            {
                _dataBox.ReArrangeChildren();
                _dataBox.ForceSizeUpdate();
                Height = _dataBox.Height;
            }
        }

    #endregion
}
