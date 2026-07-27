using ClassicUO.Configuration;
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
    public static void Show()
    {
        foreach (IGui g in UIManager.Gumps)
        {
            if (g is AssistantWindow w)
            {
                w.CenterInViewPort();
                w.BringOnTop();
                return;
            }
        }
        UIManager.Add(new AssistantWindow());
    }

    private SkillsTabContent _skillsTabContent;

    public AssistantWindow() : base(TazLang.Get("assistantwindow_title", "Legion Assistant"))
    {
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

        HotkeysTabContent.Cleanup();
        SpellBarTabContent.Cleanup();

        EventSink.SkillValueChangedEvent -= EventSkillUpdated;
        EventSink.SkillBaseChangedEvent -= EventSkillUpdated;
        EventSink.SkillCapChangedEvent -= EventSkillUpdated;
    }

    private void Build()
    {
        var tabs = new MyraTabControl();
        tabs.AddTab(TazLang.Get("assistantwindow_tab_general", "General"), GeneralTab.Build);
        tabs.AddTab(TazLang.Get("assistantwindow_tab_agents", "Agents"), AgentTab.Build);
        tabs.AddTab(TazLang.Get("assistantwindow_tab_filters", "Filters"), FiltersTab.Build);
        tabs.AddTab(TazLang.Get("assistantwindow_tab_itemdatabase", "Item Database"), ItemDatabaseTabContent.Build);
        tabs.AddTab(TazLang.Get("assistantwindow_tab_macros", "Macros"), () => MacrosTabContent.Build(this));
        tabs.AddTab(TazLang.Get("assistantwindow_tab_hotkeys", "Hotkeys"), HotkeysTabContent.Build);
        tabs.AddTab(TazLang.Get("assistantwindow_tab_skills", "Skills"), () => _skillsTabContent = new SkillsTabContent());
        tabs.SelectFirst();
        SetRootContent(tabs);
    }
}
