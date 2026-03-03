using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Filters;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.ItemDatabase;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Macros;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Skills;

namespace ClassicUO.Game.UI.MyraWindows;

public class AssistantWindow : MyraControl
{
    public AssistantWindow() : base("Legion Assistant")
    {
        Build();
        CenterInViewPort();
    }

    public override void Dispose()
    {
        base.Dispose();

        MacrosTabContent.Cleanup();
    }

    private void Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("General", GeneralTab.Build);
        tabs.AddTab("Agents", AgentTab.Build);
        tabs.AddTab("Filters", FiltersTab.Build);
        tabs.AddTab("Item Database", ItemDatabaseTabContent.Build);
        tabs.AddTab("Macros", MacrosTabContent.Build);
        tabs.AddTab("Skills", SkillsTabContent.Build);
        tabs.SelectFirst();
        SetRootContent(tabs);
    }
}
