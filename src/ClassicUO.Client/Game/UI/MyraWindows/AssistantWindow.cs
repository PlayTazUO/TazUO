using ClassicUO.Game.Managers;
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
    public static void Show() => UIManager.Add(new AssistantWindow());

    private SkillsTabContent _skillsTabContent;

    public AssistantWindow() : base("Legion Assistant")
    {
        UIManager.ForEach<AssistantWindow>(w => { if(w != this) w.Dispose(); });

        CanBeSaved = true;
        Build();
        CenterInViewPort();

        EventSink.SkillValueChangedEvent += EventSkillUpdated;
        EventSink.SkillBaseChangedEvent += EventSkillUpdated;
        EventSink.SkillCapChangedEvent += EventSkillUpdated;
    }

    private void EventSkillUpdated(object sender, SkillChangeArgs e) => _skillsTabContent?.UpdateSkills();

    public override void Dispose()
    {
        base.Dispose();

        MacrosTabContent.Cleanup();

        EventSink.SkillValueChangedEvent -= EventSkillUpdated;
        EventSink.SkillBaseChangedEvent -= EventSkillUpdated;
        EventSink.SkillCapChangedEvent -= EventSkillUpdated;
    }

    private void Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab("常规", GeneralTab.Build);
        tabs.AddTab("代理", AgentTab.Build);
        tabs.AddTab("过滤器", FiltersTab.Build);
        tabs.AddTab("物品数据库", ItemDatabaseTabContent.Build);
        tabs.AddTab("宏", MacrosTabContent.Build);
        tabs.AddTab("技能", () => _skillsTabContent = new());
        tabs.SelectFirst();
        SetRootContent(tabs);
    }
}
