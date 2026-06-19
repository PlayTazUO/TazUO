using System;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs.CooldownBars;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class InterfaceTab
{
    internal static IOptionSource GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionTabGroup()
            .AddTab(
                lang.ButtonContainers,
                ContainersTab.GetContent,
                new SearchMetadata(lang.ButtonContainers, ["Container", "Containers"])
            )
            .AddTab(
                lang.ButtonNameplates,
                NameplatesTab.GetContent,
                new SearchMetadata(lang.ButtonNameplates, ["Nameplate", "Nameplates", "Names"])
            )
            .AddTab(
                lang.LabelTooltips,
                TooltipsTab.GetContent,
                new SearchMetadata(lang.LabelTooltips, ["Tooltip", "Tooltips", "Hover"])
            )
            .AddTab(
                lang.ButtonInfoBar,
                (Func<Widget>)InfoBarsTab.GetContent,
                new SearchMetadata(lang.ButtonInfoBar, ["Info Bar", "InfoBar", "Stats"])
            )
            .AddTab(
                lang.ButtonHealthBars,
                HealthBarsTab.GetContent,
                new SearchMetadata(lang.ButtonHealthBars, ["Health Bar", "Health Bars", "HP"])
            )
            .AddTab(
                lang.ButtonGumps,
                GumpsTab.GetContent,
                new SearchMetadata(lang.ButtonGumps, ["Gump", "Gumps", "Window", "Windows"])
            )
            .AddTab(
                lang.ButtonCounters,
                CountersTab.GetContent,
                new SearchMetadata(lang.ButtonCounters, ["Counter", "Counters", "Items", "Reagents"])
            )
            .AddTab(
                lang.ButtonPaperdoll,
                PaperdollTab.GetContent,
                new SearchMetadata(lang.ButtonPaperdoll, ["Paperdoll", "Paper Doll", "Character", "Equipment"])
            )
            .AddTab(
                lang.CooldownsTab.CooldownBarsLabel,
                CooldownBarsTab.GetContent,
                new SearchMetadata(lang.CooldownsTab.CooldownBarsLabel, ["Cooldown", "Cooldowns", "Cooldown Bars", "Timers"])
            );
    }
}
