using ImGuiNET;
using System;
using System.Collections.Generic;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class AssistantWindow : SingletonImGuiWindow<AssistantWindow>
    {
        private readonly List<TabItem> _tabs = new();
        private int _selectedTabIndex = -1;
        private int _preSelectIndex = -1;
        private AssistantWindow() : base("Legion Assistant")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;

            AddTab("General", DrawGeneral, GeneralWindow.Show, () => GeneralWindow.Instance?.Dispose());
            AddTab("Agents", DrawAgents, AgentsWindow.Show, () => AgentsWindow.Instance?.Dispose());
            AddTab("Organizer", DrawOrganizer, OrganizerWindow.Show, () => OrganizerWindow.Instance?.Dispose());
            AddTab("Dress Agent", DrawDressAgent, DressAgentWindow.Show, () => DressAgentWindow.Instance?.Dispose());
            AddTab("Item Database", DrawItemDatabase, ItemDatabaseSearchWindow.Show, () => ItemDatabaseSearchWindow.Instance?.Dispose());
        }

        public void SelectTab(AssistantGump.PAGE page)
        {
            switch (page)
            {
                case AssistantGump.PAGE.None:
                    break;
                case AssistantGump.PAGE.AutoLoot:
                    _preSelectIndex = 1;
                    break;
                case AssistantGump.PAGE.AutoSell:
                    _preSelectIndex = 2;
                    break;
                case AssistantGump.PAGE.AutoBuy:
                    _preSelectIndex = 3;
                    break;
                case AssistantGump.PAGE.MobileGraphicFilter:
                    _preSelectIndex = 4;
                    break;
                case AssistantGump.PAGE.SpellBar:
                    SpellBarWindow.Show();
                    Dispose(); // Close the assistant window since we're opening the dedicated window
                    break;
                case AssistantGump.PAGE.HUD:
                    break;
                case AssistantGump.PAGE.SpellIndicator:
                    _preSelectIndex = 0; // General tab since spell indicators are now in General window
                    break;
                case AssistantGump.PAGE.JournalFilter:
                    _preSelectIndex = 1; // Agents tab since Journal Filter is in Agents Window
                    break;
                case AssistantGump.PAGE.TitleBar:
                    break;
                case AssistantGump.PAGE.DressAgent:
                    _preSelectIndex = 3;
                    break;
                case AssistantGump.PAGE.BandageAgent:
                    _preSelectIndex = 6;
                    break;
                case AssistantGump.PAGE.FriendsList:
                    break;
                case AssistantGump.PAGE.Organizer:
                    _preSelectIndex = 5;
                    break;
            }
        }

        public void AddTab(string title, Action drawContent, Action showFullWindow, Action dispose)
        {
            _tabs.Add(new TabItem { Title = title, DrawContent = drawContent, ShowFullWindow = showFullWindow, Dispose = dispose});
        }

        public void RemoveTab(int index)
        {
            if (index >= 0 && index < _tabs.Count)
            {
                _tabs.RemoveAt(index);
                if (_selectedTabIndex >= _tabs.Count)
                    _selectedTabIndex = Math.Max(0, _tabs.Count - 1);
            }
        }

        public void ClearTabs()
        {
            _tabs.Clear();
            _selectedTabIndex = 0;
        }

        public override void DrawContent()
        {
            if (_tabs.Count == 0)
            {
                ImGui.Text("No tabs available");
                return;
            }

            bool open = true;

            // Draw tab bar
            if (ImGui.BeginTabBar("TabMenuTabs", ImGuiTabBarFlags.Reorderable))
            {
                bool hasPreSelection = _preSelectIndex > -1;
                for (int i = 0; i < _tabs.Count; i++)
                {
                    TabItem tab = _tabs[i];
                    if (ImGui.BeginTabItem(tab.Title, ref open, (hasPreSelection && i == _preSelectIndex) ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None))
                    {
                        _selectedTabIndex = i;
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
                if (hasPreSelection)
                    _preSelectIndex = -1;
                ImGui.EndTabBar();
            }
        }

        private void DrawGeneral() => GeneralWindow.GetInstance()?.DrawContent();
        private void DrawAgents() => AgentsWindow.GetInstance()?.DrawContent();
        private void DrawOrganizer() => OrganizerWindow.GetInstance()?.DrawContent();
        private void DrawDressAgent() => DressAgentWindow.GetInstance()?.DrawContent();
        private void DrawItemDatabase() => ItemDatabaseSearchWindow.GetInstance()?.DrawContent();

        public override void Dispose()
        {
            base.Dispose();
            foreach (var tab in _tabs)
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
