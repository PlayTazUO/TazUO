using ImGuiNET;
using System.Linq;
using System.Numerics;
using ClassicUO.LegionScripting;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class RunningScriptsWindow : SingletonImGuiWindow<RunningScriptsWindow>
    {
        private RunningScriptsWindow() : base("Running Scripts")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;
        }

        public override void DrawContent()
        {
            var runningScripts = LegionScripting.LegionScripting.RunningScripts;

            if (runningScripts.Count == 0)
            {
                ImGui.TextDisabled("No scripts currently running");
            }
            else
            {
                foreach (var script in runningScripts.ToArray())
                {
                    if (script == null) continue;

                    ImGui.PushID(script.FullPath?.GetHashCode() ?? 0);

                    if (ImGui.Button("Stop", new Vector2(50, 0)))
                        LegionScripting.LegionScripting.StopScript(script);

                    // Script name on the same line
                    ImGui.SameLine();
                    ImGui.Text(script.FileName ?? "Unknown");

                    // Show script type as tooltip
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"Type: {script.ScriptType}");
                        ImGui.Text($"Path: {script.FullPath ?? "N/A"}");
                        ImGui.EndTooltip();
                    }

                    ImGui.PopID();
                }
            }
        }
    }
}
