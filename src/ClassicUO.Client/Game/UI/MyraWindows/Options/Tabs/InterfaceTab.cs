using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs.CooldownBars;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class InterfaceTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.ButtonGameplay, GetInterfaceMenuTabs);
    }

    private static MyraTabControl GetInterfaceMenuTabs()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        var tabs = new MyraTabControl();
        tabs.AddTab(lang.ButtonContainers, ContainersTab.GetContent);
        tabs.AddTab(lang.ButtonNameplates, NameplatesTab.GetContent);
        tabs.AddTab(lang.LabelTooltips, TooltipsTab.GetContent);
        tabs.AddTab(lang.ButtonInfoBar, InfoBarsTab.GetContent);
        tabs.AddTab(lang.ButtonHealthBars, HealthBarsTab.GetContent);
        tabs.AddTab(lang.ButtonGumps, GumpsTab.GetContent);
        tabs.AddTab(lang.ButtonCounters, CountersTab.GetContent);
        tabs.AddTab(lang.ButtonPaperdoll, PaperdollTab.GetContent);
        tabs.AddTab(lang.CooldownsTab.CooldownBarsLabel, CooldownBarsTab.GetContent);
        return tabs;
    }
}
