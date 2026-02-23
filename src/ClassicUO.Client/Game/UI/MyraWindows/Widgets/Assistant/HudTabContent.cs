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
            if(flag == HideHudFlags.All) continue;
            checkButtons[flag] = MyraCheckButton.CreateWithCallback(ByteFlagHelper.HasFlag(profile.HideHudGumpFlags,  (ulong)flag),
                b =>
                {
                    profile.HideHudGumpFlags = b ? ByteFlagHelper.AddFlag(profile.HideHudGumpFlags, (ulong)flag) : ByteFlagHelper.RemoveFlag(profile.HideHudGumpFlags, (ulong)flag);
                }, HideHudManager.GetFlagName(flag), GetTooltip(flag));
        }


        var allCb = MyraCheckButton.CreateWithCallback(ByteFlagHelper.HasFlag(profile.HideHudGumpFlags,  (ulong)HideHudFlags.All),
            b => { SetAllChecked(checkButtons, profile, b); }, HideHudManager.GetFlagName(HideHudFlags.All), GetTooltip(HideHudFlags.All));

        checkButtons[HideHudFlags.All] = allCb;

        var outerStack = new VerticalStackPanel { Spacing = 6 };

        outerStack.Widgets.Add(new MyraLabel(
            "Select gump types to toggle visibility when using the Toggle Hud Visible macro.",
            MyraLabel.Style.P));

        var buttonRow = new HorizontalStackPanel { Spacing = 4 };
        buttonRow.Widgets.Add(new MyraButton("Select All", () => SetAllChecked(checkButtons, profile, true)));
        buttonRow.Widgets.Add(new MyraButton("Deselect All", () => SetAllChecked(checkButtons, profile, false)));
        buttonRow.Widgets.Add(new MyraButton("Toggle HUD Now", () => HideHudManager.ToggleHidden(profile.HideHudGumpFlags))
        {
            Tooltip = "Immediately toggle the visibility of selected HUD elements"
        });
        outerStack.Widgets.Add(buttonRow);

        var grid = new MyraGrid();
        grid.AddColumn(new Proportion(ProportionType.Auto));
        grid.AddColumn(new Proportion(ProportionType.Pixels, 12));
        grid.AddColumn(new Proportion(ProportionType.Auto));

        int row = 0;
        bool leftCol = true;

        foreach (HideHudFlags flag in regularFlags)
        {
            grid.AddWidget(checkButtons[flag], row, leftCol ? 0 : 2);
            if (!leftCol) row++;
            leftCol = !leftCol;
        }

        //All button at the end
        grid.AddWidget(allCb, row, leftCol ? 0 : 2);

        outerStack.Widgets.Add(grid);
        return outerStack;
    }

    private static HorizontalStackPanel MakePair(CheckButton cb, string text, string tooltip)
    {
        var label = new MyraLabel(text, MyraLabel.Style.P);
        if (!string.IsNullOrEmpty(tooltip))
            label.Tooltip = tooltip;

        var pair = new HorizontalStackPanel { Spacing = 4 };
        pair.Widgets.Add(cb);
        pair.Widgets.Add(label);
        return pair;
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
        _ => null
    };
}
