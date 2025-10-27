using ImGuiNET;
using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class AssistantWindow : SingletonImGuiWindow<AssistantWindow>
    {
        private readonly List<TabItem> _tabs = new();
        private AssistantWindow() : base("Legion Assistant")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;

            AddTab("General", DrawGeneral, GeneralWindow.Show, () => GeneralWindow.Instance?.Dispose());
            AddTab("Agents", DrawAgents, AgentsWindow.Show, () => AgentsWindow.Instance?.Dispose());
            AddTab("Organizer", DrawOrganizer, OrganizerWindow.Show, () => OrganizerWindow.Instance?.Dispose());
            AddTab("Filters", DrawFilters, FiltersWindow.Show, () => FiltersWindow.Instance?.Dispose());
            AddTab("Item Database", DrawItemDatabase, ItemDatabaseSearchWindow.Show, () => ItemDatabaseSearchWindow.Instance?.Dispose());
        }

        public void AddTab(string title, Action drawContent, Action showFullWindow, Action dispose) => _tabs.Add(new TabItem { Title = title, DrawContent = drawContent, ShowFullWindow = showFullWindow, Dispose = dispose });

        public void RemoveTab(int index)
        {
            if (index >= 0 && index < _tabs.Count)
            {
                _tabs.RemoveAt(index);
            }
        }

        public void ClearTabs() => _tabs.Clear();

        public override void DrawContent()
        {
            if (_tabs.Count == 0)
            {
                ImGui.Text("No tabs available");
                return;
            }

            // Draw tab bar
            if (ImGui.BeginTabBar("TabMenuTabs", ImGuiTabBarFlags.None))
            {
                for (int i = 0; i < _tabs.Count; i++)
                {
                    TabItem tab = _tabs[i];
                    if (ImGui.BeginTabItem(tab.Title))
                    {
                        tab.DrawContent?.Invoke();

                        // if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        // {
                        //     if (tab.ShowFullWindow != null)
                        //     {
                        //         tab.ShowFullWindow.Invoke();
                        //         RemoveTab(i);
                        //     }
                        // }

                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }
        }

        private void DrawGeneral() => GeneralWindow.GetInstance()?.DrawContent();
        private void DrawAgents() => AgentsWindow.GetInstance()?.DrawContent();
        private void DrawOrganizer() => OrganizerWindow.GetInstance()?.DrawContent();
        private void DrawFilters() => FiltersWindow.GetInstance()?.DrawContent();
        private void DrawItemDatabase() => ItemDatabaseSearchWindow.GetInstance()?.DrawContent();

        public override void Dispose()
        {
            base.Dispose();
            foreach (TabItem tab in _tabs)
                tab.Dispose?.Invoke();
            ClearTabs();
        }

        private class TabItem
        {
            public string Title { get; set; }
            public Action DrawContent { get; set; }
            public Action ShowFullWindow { get; set; }
            public Action Dispose { get; set; }
        }
    }
}
