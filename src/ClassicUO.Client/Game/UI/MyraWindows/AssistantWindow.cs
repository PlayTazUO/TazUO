using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

namespace ClassicUO.Game.UI.MyraWindows;

public class AssistantWindow : MyraControl
{
    public const int WIDTH = 450;

    public AssistantWindow() : base("Legion Assistant")
    {
        Build();
        CenterInViewPort();
    }

    private void Build()
    {
        var tabs = new MyraTabControl { MinWidth = WIDTH };
        tabs.AddTab("General", GeneralTab.Build);
        tabs.AddTab("Agents", AgentTab.Build);
        tabs.SelectFirst();
        SetRootContent(tabs);
    }
}
