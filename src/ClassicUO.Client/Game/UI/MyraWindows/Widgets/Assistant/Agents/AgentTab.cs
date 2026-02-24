using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class AgentTab
{
    public static Widget Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("Auto Loot", AutoLootAgentTabContent.Build);
        tabs.SelectFirst();
        return tabs;
    }
}
