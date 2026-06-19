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
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return new OptionTabGroup(search: new SearchMetadata(Tags: [kw.Interface, kw.Container]))
            .AddTab(
                lang.ButtonContainers,
                ContainersTab.GetContent,
                new SearchMetadata(lang.ButtonContainers)
            )
            .AddTab(
                lang.ButtonNameplates,
                NameplatesTab.GetContent,
                new SearchMetadata(lang.ButtonNameplates, [kw.Nameplate, kw.Name])
            )
            .AddTab(
                lang.LabelTooltips,
                TooltipsTab.GetContent,
                new SearchMetadata(lang.LabelTooltips, [kw.Tooltip, kw.Hover])
            )
            .AddTab(
                lang.ButtonInfoBar,
                InfoBarsTab.GetContent,
                new SearchMetadata(lang.ButtonInfoBar, [kw.InfoBarSpaced, kw.InfoBar, kw.Stat])
            )
            .AddTab(
                lang.ButtonHealthBars,
                HealthBarsTab.GetContent,
                new SearchMetadata(lang.ButtonHealthBars, [kw.HealthBar, kw.HP])
            )
            .AddTab(
                lang.ButtonGumps,
                GumpsTab.GetContent,
                new SearchMetadata(lang.ButtonGumps, [kw.Gump, kw.Window])
            )
            .AddTab(
                lang.ButtonCounters,
                CountersTab.GetContent,
                new SearchMetadata(lang.ButtonCounters, [kw.Counter, kw.Item, kw.Reagent])
            )
            .AddTab(
                lang.ButtonPaperdoll,
                PaperdollTab.GetContent,
                new SearchMetadata(lang.ButtonPaperdoll, [kw.Paperdoll, kw.Character, kw.Equipment])
            )
            .AddTab(
                lang.CooldownsTab.CooldownBarsLabel,
                CooldownBarsTab.GetContent,
                new SearchMetadata(lang.CooldownsTab.CooldownBarsLabel, [kw.Cooldown, kw.Timer])
            );
    }
}
