using ClassicUO.Configuration;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class AgentTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab(TazLang.Get("assistant_agent_tab_autoloot", "Auto Loot"), AutoLootAgentTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_agent_tab_dress", "Dress Agent"), DressAgentTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_agent_tab_autobuy", "Auto Buy"), AutoBuyAgentTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_agent_tab_autosell", "Auto Sell"), AutoSellAgentTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_agent_tab_bandage", "Bandage"), BandageAgentTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_agent_tab_selfheal", "Self Heal"), SelfHealTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_agent_tab_organizer", "Organizer"), OrganizerAgentTabContent.Build);
        tabs.AddTab(TazLang.Get("assistant_agent_tab_statlock", "Stat Lock"), AutoStatLockAgentTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
