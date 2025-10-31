using ImGuiNET;
using ClassicUO.Configuration;
using System;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class BandageAgentWindow : SingletonImGuiWindow<BandageAgentWindow>
    {
        private Profile profile;
        private string _bandageDelayInput ;
        private string _bandageGraphicInput;

        private bool enabled;
        private int hpPercentage;
        private bool checkForBuff, useNewPacket, checkPoisoned, checkHidden, checkInvul, healfriends, dexFormula, disableSelfHeal;

        private BandageAgentWindow() : base("Bandage Agent")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            profile = ProfileManager.CurrentProfile;

            // Initialize input fields with current values
            _bandageDelayInput = profile.BandageAgentDelay.ToString();
            _bandageGraphicInput = $"0x{profile.BandageAgentGraphic:X4}";
            enabled = profile.EnableBandageAgent;
            hpPercentage = profile.BandageAgentHPPercentage;
            checkForBuff = profile.BandageAgentCheckForBuff;
            useNewPacket = profile.BandageAgentUseNewPacket;
            checkPoisoned = profile.BandageAgentCheckPoisoned;
            checkHidden = profile.BandageAgentCheckHidden;
            checkInvul = profile.BandageAgentCheckInvul;
            healfriends = profile.BandageAgentBandageFriends;
            dexFormula = profile.BandageAgentUseDexFormula;
            disableSelfHeal = profile.BandageAgentDisableSelfHeal;
        }

        public override void DrawContent()
        {
            if (profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            ImGui.TextWrapped("Automatically use bandages to heal when HP drops below threshold.");
            ImGui.Separator();

            // Enable bandage agent checkbox
            if (ImGui.Checkbox("Enable bandage agent", ref enabled))
                profile.EnableBandageAgent = enabled;

            ImGui.SameLine();
            if (ImGui.Checkbox("Bandage friends", ref healfriends))
                profile.BandageAgentBandageFriends = healfriends;

            if (ImGui.Checkbox("Disable self heal", ref disableSelfHeal))
                profile.BandageAgentDisableSelfHeal = disableSelfHeal;
            ImGuiComponents.Tooltip("When enabled, bandage agent will only heal friends and not yourself");

            ImGui.Separator();

            // Bandage delay input
            if (ImGui.InputText("##BandageDelay", ref _bandageDelayInput, 10))
            {
                if (int.TryParse(_bandageDelayInput, out int delay))
                {
                    profile.BandageAgentDelay = Math.Clamp(delay, 50, 30000);
                }
            }
            ImGui.SameLine();
            ImGui.Text("Delay (ms):");
            ImGuiComponents.Tooltip("Delay between bandage attempts in milliseconds (50-30000)");
            //ImGui.SameLine();
            if (ImGui.Checkbox("Use Dex Formula", ref dexFormula))
            {
                profile.BandageAgentUseDexFormula = dexFormula;
            }
            ImGuiComponents.Tooltip("Use the dex formula instead of a set delay");

            // HP percentage threshold slider
            if (ImGui.SliderInt("HP percentage threshold", ref hpPercentage, 10, 95))
            {
                profile.BandageAgentHPPercentage = hpPercentage;
            }
            ImGuiComponents.Tooltip("Heal when HP drops below this percentage");

            ImGui.Separator();

            // Use bandaging buff checkbox
            if (ImGui.Checkbox("Use bandaging buff instead of delay", ref checkForBuff))
            {
                profile.BandageAgentCheckForBuff = checkForBuff;
            }

            // Use new bandage packet checkbox
            if (ImGui.Checkbox("Use new bandage packet", ref useNewPacket))
            {
                profile.BandageAgentUseNewPacket = useNewPacket;
            }

            // Bandage if poisoned checkbox
            if (ImGui.Checkbox("Bandage if Poisoned", ref checkPoisoned))
            {
                profile.BandageAgentCheckPoisoned = checkPoisoned;
            }

            // Skip bandage if hidden checkbox
            if (ImGui.Checkbox("Skip Bandage if Hidden", ref checkHidden))
            {
                profile.BandageAgentCheckHidden = checkHidden;
            }

            // Skip bandage if yellow hits checkbox
            if (ImGui.Checkbox("Skip Bandage if yellow hits", ref checkInvul))
            {
                profile.BandageAgentCheckInvul = checkInvul;
            }

            ImGui.Separator();

            // Bandage graphic ID input
            ImGui.Text("Bandage graphic ID:");
            ImGui.SameLine();
            if (ImGui.InputText("##BandageGraphic", ref _bandageGraphicInput, 10))
            {
                if (TryParseBandageGraphic(_bandageGraphicInput, out ushort graphic))
                {
                    profile.BandageAgentGraphic = graphic;
                }
            }
            ImGuiComponents.Tooltip("Graphic ID of bandages to use (default: 0x0E21). Accepts hex (0x0E21) or decimal (3617)");
        }

        private bool TryParseBandageGraphic(string text, out ushort graphic)
        {
            // Try to parse as hex (0x prefix) or decimal
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || text.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
            {
                return ushort.TryParse(text.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out graphic);
            }
            else
            {
                return ushort.TryParse(text, out graphic);
            }
        }
    }
}
