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
            "Select gump types to toggle visibility when using the Toggle Hud Visible macro.",
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
        buttonRow.Widgets.Add(new MyraButton("Select All", () => SetAllChecked(checkButtons, profile, true)));

        var deselectBtn = new MyraButton("Deselect All", () => SetAllChecked(checkButtons, profile, false));
        StackPanel.SetProportionType(deselectBtn, ProportionType.Fill);
        buttonRow.Widgets.Add(deselectBtn);

        buttonRow.Widgets.Add(new MyraButton("Toggle HUD Now", () => HideHudManager.ToggleHidden(profile.HideHudGumpFlags))
        {
            Tooltip = "Immediately toggle the visibility of selected HUD elements"
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
        HideHudFlags.Paperdoll => "Character paperdoll windows",
        HideHudFlags.WorldMap => "World map window",
        HideHudFlags.GridContainers => "Grid-style container windows",
        HideHudFlags.Containers => "Traditional container windows",
        HideHudFlags.Healthbars => "Health bar windows",
        HideHudFlags.StatusBar => "Character status windows",
        HideHudFlags.SpellBar => "Spell bar windows",
        HideHudFlags.Journal => "Journal/chat windows",
        HideHudFlags.XMLGumps => "Server-sent XML gump windows",
        HideHudFlags.NearbyCorpseLoot => "Nearby corpse loot windows",
        HideHudFlags.MacroButtons => "Macro button windows",
        HideHudFlags.SkillButtons => "Skill button windows",
        HideHudFlags.SkillsMenus => "Skills menu windows",
        HideHudFlags.TopMenuBar => "Top menu bar",
        HideHudFlags.DurabilityTracker => "Item durability tracker",
        HideHudFlags.BuffBar => "Buff/debuff status bars",
        HideHudFlags.CounterBar => "Item counter bars",
        HideHudFlags.InfoBar => "Information bars",
        HideHudFlags.SpellIcons => "Spell icon buttons",
        HideHudFlags.NameOverheadGump => "Name overhead displays",
        HideHudFlags.ScriptManagerGump => "Script manager window",
        HideHudFlags.PlayerChar => "Player character (your avatar in the game world)",
        HideHudFlags.Mouse => "Mouse cursor",
        HideHudFlags.HealthBarCollector => "Health bar collector window",
        HideHudFlags.AbilityButtons => "Ability button windows",
        HideHudFlags.DebugGump => "Debug information window",
        _ => null
    };
}
