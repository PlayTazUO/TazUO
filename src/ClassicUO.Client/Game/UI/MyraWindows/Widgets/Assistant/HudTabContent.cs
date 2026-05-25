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
            "选择在使用切换HUD可见性宏时要切换可见性的窗口类型。",
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
        buttonRow.Widgets.Add(new MyraButton("全选", () => SetAllChecked(checkButtons, profile, true)));

        var deselectBtn = new MyraButton("取消全选", () => SetAllChecked(checkButtons, profile, false));
        StackPanel.SetProportionType(deselectBtn, ProportionType.Fill);
        buttonRow.Widgets.Add(deselectBtn);

        buttonRow.Widgets.Add(new MyraButton("立即切换HUD", () => HideHudManager.ToggleHidden(profile.HideHudGumpFlags))
        {
            Tooltip = "立即切换所选HUD元素的可见性"
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
        HideHudFlags.Paperdoll => "角色纸娃娃窗口",
        HideHudFlags.WorldMap => "世界地图窗口",
        HideHudFlags.GridContainers => "网格容器窗口",
        HideHudFlags.Containers => "传统容器窗口",
        HideHudFlags.Healthbars => "血条窗口",
        HideHudFlags.StatusBar => "角色状态窗口",
        HideHudFlags.SpellBar => "法术条窗口",
        HideHudFlags.Journal => "日志/聊天窗口",
        HideHudFlags.XMLGumps => "服务器发送的XML窗口",
        HideHudFlags.NearbyCorpseLoot => "附近尸体拾取窗口",
        HideHudFlags.MacroButtons => "宏按钮窗口",
        HideHudFlags.SkillButtons => "技能按钮窗口",
        HideHudFlags.SkillsMenus => "技能菜单窗口",
        HideHudFlags.TopMenuBar => "顶部菜单栏",
        HideHudFlags.DurabilityTracker => "物品耐久度追踪器",
        HideHudFlags.BuffBar => "增益/减益效果状态条",
        HideHudFlags.CounterBar => "物品计数条",
        HideHudFlags.InfoBar => "信息栏",
        HideHudFlags.SpellIcons => "法术图标按钮",
        HideHudFlags.NameOverheadGump => "名称头顶显示",
        HideHudFlags.ScriptManagerGump => "脚本管理器窗口",
        HideHudFlags.PlayerChar => "玩家角色（您在游戏世界中的化身）",
        HideHudFlags.Mouse => "鼠标光标",
        HideHudFlags.HealthBarCollector => "血条收集器窗口",
        HideHudFlags.AbilityButtons => "能力按钮窗口",
        HideHudFlags.DebugGump => "调试信息窗口",
        _ => null
    };
}
