using ImGuiNET;
using ClassicUO.Configuration;
using System.Numerics;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class AgentsWindow : SingletonImGuiWindow<AgentsWindow>
    {
        private readonly Profile _profile = ProfileManager.CurrentProfile;
        private AgentsWindow() : base("Agents Tab")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;

        }
        private void DrawAutoLoot() => AutoLootWindow.GetInstance()?.DrawContent();
        private void DrawDressAgent() => DressAgentWindow.GetInstance()?.DrawContent();
        private void DrawAutoSell() => AutoSellWindow.GetInstance()?.DrawContent();
        private void DrawAutoBuy() => AutoBuyWindow.GetInstance()?.DrawContent();
        private void DrawBandageAgent() => BandageAgentWindow.GetInstance()?.DrawContent();

        public override void DrawContent()
        {
            if (_profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            ImGui.Spacing();

            if (ImGui.BeginTabBar("##Agents Tabs", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("Auto Loot"))
                {
                    DrawAutoLoot();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Dress Agent"))
                {
                    DrawDressAgent();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Auto Buy"))
                {
                    DrawAutoBuy();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Auto Sell"))
                {
                    DrawAutoSell();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Bandage"))
                {
                    DrawBandageAgent();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }
    }
}
