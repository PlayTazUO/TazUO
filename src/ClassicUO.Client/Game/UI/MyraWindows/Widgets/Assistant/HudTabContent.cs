using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class HudTabContent
{
    public static Widget Build()
    {
        Profile profile = ProfileManager.CurrentProfile;

        var regularFlags = new List<HideHudFlags>();
        foreach (HideHudFlags flag in Enum.GetValues(typeof(HideHudFlags)))
        {
            if (flag == HideHudFlags.None || flag == HideHudFlags.All) continue;
            regularFlags.Add(flag);
        }

        var checkButtons = new Dictionary<HideHudFlags, CheckButton>();

        foreach (HideHudFlags flag in regularFlags)
        {
            checkButtons[flag] = MyraCheckButton.CreateWithCallback(ByteFlagHelper.HasFlag(profile.HideHudGumpFlags, (ulong)flag),
                b =>
                {
                    profile.HideHudGumpFlags = b ? ByteFlagHelper.AddFlag(profile.HideHudGumpFlags, (ulong)flag) : ByteFlagHelper.RemoveFlag(profile.HideHudGumpFlags, (ulong)flag);
                }, HideHudManager.GetFlagName(flag), GetTooltip(flag));
        }

        var outerStack = new VerticalStackPanel { Spacing = 6 };

        outerStack.Widgets.Add(new MyraLabel(
            TazLang.Get("assistant_hud_desc", "Select gump types to toggle visibility when using the Toggle Hud Visible macro."),
            MyraLabel.TextStyle.H3));


        var grid = new MyraGrid();
        grid.AddColumn(new Proportion(ProportionType.Auto), 4);
        grid.ColumnSpacing = 12;
        for (int i = 0; i < regularFlags.Count; i++) {
            HideHudFlags flag = regularFlags[i];
            grid.AddWidget(checkButtons[flag], i / 4, i % 4);
        }
        outerStack.Widgets.Add(grid);


        var buttonRow = new HorizontalStackPanel { Spacing = 4 };
        buttonRow.Widgets.Add(new MyraButton(TazLang.Get("shared_select_all", "Select All"), () => SetAllChecked(checkButtons, profile, true)));

        var deselectBtn = new MyraButton(TazLang.Get("shared_deselect_all", "Deselect All"), () => SetAllChecked(checkButtons, profile, false));
        StackPanel.SetProportionType(deselectBtn, ProportionType.Fill);
        buttonRow.Widgets.Add(deselectBtn);

        buttonRow.Widgets.Add(new MyraButton(TazLang.Get("assistant_hud_toggle_now", "Toggle HUD Now"), () => HideHudManager.ToggleHidden(profile.HideHudGumpFlags))
        {
            Tooltip = TazLang.Get("assistant_hud_toggle_now_tooltip", "Immediately toggle the visibility of selected HUD elements")
        });
        outerStack.Widgets.Add(buttonRow);

        return outerStack;
    }

    private static void SetAllChecked(Dictionary<HideHudFlags, CheckButton> buttons, Profile profile, bool state)
    {
        profile.HideHudGumpFlags = state ? (ulong)HideHudFlags.All : 0UL;
        foreach (var (_, cb) in buttons)
            cb.IsChecked = state;
    }

    private static string GetTooltip(HideHudFlags flag) => flag switch
    {
        HideHudFlags.Paperdoll => TazLang.Get("assistant_hud_tooltip_paperdoll", "Character paperdoll windows"),
        HideHudFlags.WorldMap => TazLang.Get("assistant_hud_tooltip_worldmap", "World map window"),
        HideHudFlags.GridContainers => TazLang.Get("assistant_hud_tooltip_gridcontainers", "Grid-style container windows"),
        HideHudFlags.Containers => TazLang.Get("assistant_hud_tooltip_containers", "Traditional container windows"),
        HideHudFlags.Healthbars => TazLang.Get("assistant_hud_tooltip_healthbars", "Health bar windows"),
        HideHudFlags.StatusBar => TazLang.Get("assistant_hud_tooltip_statusbar", "Character status windows"),
        HideHudFlags.SpellBar => TazLang.Get("assistant_hud_tooltip_spellbar", "Spell bar windows"),
        HideHudFlags.Journal => TazLang.Get("assistant_hud_tooltip_journal", "Journal/chat windows"),
        HideHudFlags.XMLGumps => TazLang.Get("assistant_hud_tooltip_xmlgumps", "Server-sent XML gump windows"),
        HideHudFlags.NearbyCorpseLoot => TazLang.Get("assistant_hud_tooltip_nearbycorpseloot", "Nearby corpse loot windows"),
        HideHudFlags.MacroButtons => TazLang.Get("assistant_hud_tooltip_macrobuttons", "Macro button windows"),
        HideHudFlags.SkillButtons => TazLang.Get("assistant_hud_tooltip_skillbuttons", "Skill button windows"),
        HideHudFlags.SkillsMenus => TazLang.Get("assistant_hud_tooltip_skillsmenus", "Skills menu windows"),
        HideHudFlags.TopMenuBar => TazLang.Get("assistant_hud_tooltip_topmenubar", "Top menu bar"),
        HideHudFlags.DurabilityTracker => TazLang.Get("assistant_hud_tooltip_durabilitytracker", "Item durability tracker"),
        HideHudFlags.BuffBar => TazLang.Get("assistant_hud_tooltip_buffbar", "Buff/debuff status bars"),
        HideHudFlags.CounterBar => TazLang.Get("assistant_hud_tooltip_counterbar", "Item counter bars"),
        HideHudFlags.InfoBar => TazLang.Get("assistant_hud_tooltip_infobar", "Information bars"),
        HideHudFlags.SpellIcons => TazLang.Get("assistant_hud_tooltip_spellicons", "Spell icon buttons"),
        HideHudFlags.NameOverheadGump => TazLang.Get("assistant_hud_tooltip_nameoverheadgump", "Name overhead displays"),
        HideHudFlags.ScriptManagerGump => TazLang.Get("assistant_hud_tooltip_scriptmanagergump", "Script manager window"),
        HideHudFlags.PlayerChar => TazLang.Get("assistant_hud_tooltip_playerchar", "Player character (your avatar in the game world)"),
        HideHudFlags.Mouse => TazLang.Get("assistant_hud_tooltip_mouse", "Mouse cursor"),
        HideHudFlags.HealthBarCollector => TazLang.Get("assistant_hud_tooltip_healthbarcollector", "Health bar collector window"),
        HideHudFlags.AbilityButtons => TazLang.Get("assistant_hud_tooltip_abilitybuttons", "Ability button windows"),
        HideHudFlags.DebugGump => TazLang.Get("assistant_hud_tooltip_debuggump", "Debug information window"),
        _ => null
    };
}
