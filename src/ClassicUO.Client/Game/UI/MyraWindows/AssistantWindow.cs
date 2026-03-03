using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;

namespace ClassicUO.Game.UI.MyraWindows;

public class AssistantWindow : MyraControl
{
    public AssistantWindow() : base("Legion Assistant")
    {
        Build();
        CenterInViewPort();
    }

    private void Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("General", GeneralTab.Build);
        tabs.AddTab("Agents", AgentTab.Build);
        tabs.AddTab("Filters", FiltersTab.Build);
        tabs.SelectFirst();
        SetRootContent(tabs);
    }
}
