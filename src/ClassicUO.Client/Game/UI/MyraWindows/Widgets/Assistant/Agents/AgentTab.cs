using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class AgentTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("自动拾取", AutoLootAgentTabContent.Build);
        tabs.AddTab("换装代理", DressAgentTabContent.Build);
        tabs.AddTab("自动购买", AutoBuyAgentTabContent.Build);
        tabs.AddTab("自动出售", AutoSellAgentTabContent.Build);
        tabs.AddTab("绷带", BandageAgentTabContent.Build);
        tabs.AddTab("整理", OrganizerAgentTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
