using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class AssistantWindow : MyraControl
{
    public const int WIDTH = 550;
    private TabItem _selectedTab;

    public AssistantWindow() : base("Legion Assistant")
    {
        Build();

        CenterInViewPort();
    }

    private void Build()
    {
        var tabs = new TabControl()
        {
            Width = WIDTH
        };

        tabs.SelectedIndexChanged += (s, e) =>
        {
            _selectedTab = tabs.SelectedItem;

            if (_selectedTab.Content != null) return;

            if (_selectedTab.Tag is int tPage)
                switch ((TabPage)tPage)
                {
                    default:
                    case TabPage.General:
                        _selectedTab.Content = GeneralTab.Build();
                        break;
                }
        };

        tabs.Items.Add(new TabItem("General") { Tag = TabPage.General.ToInt() });

        tabs.SelectedIndex = 0;

        SetRootContent(tabs);
    }

    private enum TabPage
    {
        General
    }
}
