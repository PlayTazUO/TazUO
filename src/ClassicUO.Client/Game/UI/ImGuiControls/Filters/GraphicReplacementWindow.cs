using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class GraphicReplacementWindow : SingletonImGuiWindow<GraphicReplacementWindow>
    {
        private string newOriginalGraphicInput = "";
        private string newReplacementGraphicInput = "";
        private string newHueInput = "";
        private bool showAddEntry = false;
        private Dictionary<ushort, string> entryOriginalInputs = new Dictionary<ushort, string>();
        private Dictionary<ushort, string> entryReplacementInputs = new Dictionary<ushort, string>();
        private Dictionary<ushort, string> entryHueInputs = new Dictionary<ushort, string>();

        private GraphicReplacementWindow() : base("Mobile Graphics Replacement")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
        }

        public override void DrawContent()
        {
            ImGui.Spacing();
            ImGuiComponents.Tooltip("This can be used to replace graphics of mobiles with other graphics (For example if dragons are too big, replace them with wyverns).");

            ImGui.Spacing();

            ImGui.SeparatorText("Options:");

            // Add entry section
            if (ImGui.Button("Add Entry"))
            {
                showAddEntry = !showAddEntry;
            }

            ImGui.SameLine();
            if (ImGui.Button("Target Entity"))
            {
                World.Instance.TargetManager.SetTargeting((targetedEntity) =>
                {
                    if (targetedEntity != null && targetedEntity is Entity entity)
                    {
                        GraphicChangeFilter filter = GraphicsReplacement.NewFilter(entity.Graphic, entity.Graphic, entity.Hue);
                        if (filter != null)
                        {
                            // Initialize input strings for the new entry
                            entryOriginalInputs[filter.OriginalGraphic] = filter.OriginalGraphic.ToString();
                            entryReplacementInputs[filter.OriginalGraphic] = filter.ReplacementGraphic.ToString();
                            entryHueInputs[filter.OriginalGraphic] = filter.NewHue == ushort.MaxValue ? "-1" : filter.NewHue.ToString();
                        }
                    }
                });
            }

            if (showAddEntry)
            {
                ImGui.Spacing();
                ImGui.SeparatorText("New Entry:");
                ImGui.Spacing();
                ImGui.BeginGroup();
                ImGui.Text("Original Graphic:");
                ImGui.SetNextItemWidth(150);
                ImGui.InputText("##NewOriginalGraphic", ref newOriginalGraphicInput, 10);
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();
                ImGui.Text("Replacement Graphic:");
                ImGui.SetNextItemWidth(150);
                ImGui.InputText("##NewReplacementGraphic", ref newReplacementGraphicInput, 10);
                ImGui.EndGroup();

                ImGui.Spacing();
                ImGui.Text("New Hue (-1 to leave original):");
                ImGui.SetNextItemWidth(150);
                ImGui.InputText("##NewHue", ref newHueInput, 10);

                ImGui.Spacing();
                if (ImGui.Button("Add##AddEntry"))
                {
                    if (StringHelper.TryParseGraphic(newOriginalGraphicInput, out int originalGraphic) &&
                        StringHelper.TryParseGraphic(newReplacementGraphicInput, out int replacementGraphic))
                    {
                        ushort newHue = ushort.MaxValue;
                        if (!string.IsNullOrEmpty(newHueInput) && newHueInput != "-1")
                        {
                            ushort.TryParse(newHueInput, out newHue);
                        }

                        GraphicChangeFilter filter = GraphicsReplacement.NewFilter((ushort)originalGraphic, (ushort)replacementGraphic, newHue);
                        if (filter != null)
                        {
                            // Initialize input strings for the new entry
                            entryOriginalInputs[filter.OriginalGraphic] = filter.OriginalGraphic.ToString();
                            entryReplacementInputs[filter.OriginalGraphic] = filter.ReplacementGraphic.ToString();
                            entryHueInputs[filter.OriginalGraphic] = filter.NewHue == ushort.MaxValue ? "-1" : filter.NewHue.ToString();
                        }

                        newOriginalGraphicInput = "";
                        newReplacementGraphicInput = "";
                        newHueInput = "";
                        showAddEntry = false;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel##AddEntry"))
                {
                    showAddEntry = false;
                    newOriginalGraphicInput = "";
                    newReplacementGraphicInput = "";
                    newHueInput = "";
                }
            }

            ImGui.Separator();

            // List of current filters
            ImGui.Text("Current Graphic Replacements:");

            Dictionary<ushort, GraphicChangeFilter> filters = GraphicsReplacement.GraphicFilters;
            if (filters.Count == 0)
            {
                ImGui.Text("No replacements configured");
            }
            else
            {
                // Table headers
                if (ImGui.BeginTable("GraphicReplacementTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
                {
                    ImGui.TableSetupColumn("Original Graphic", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                    ImGui.TableSetupColumn("Replacement Graphic", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                    ImGui.TableSetupColumn("New Hue", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                    ImGui.TableHeadersRow();

                    var filterList = filters.Values.ToList();
                    for (int i = filterList.Count - 1; i >= 0; i--)
                    {
                        GraphicChangeFilter filter = filterList[i];
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        // Initialize input string if not exists
                        if (!entryOriginalInputs.ContainsKey(filter.OriginalGraphic))
                        {
                            entryOriginalInputs[filter.OriginalGraphic] = filter.OriginalGraphic.ToString();
                        }
                        string originalStr = entryOriginalInputs[filter.OriginalGraphic];
                        if (ImGui.InputText($"##Original{i}", ref originalStr, 10))
                        {
                            entryOriginalInputs[filter.OriginalGraphic] = originalStr;
                            if (StringHelper.TryParseGraphic(originalStr, out int newOriginal))
                            {
                                filter.OriginalGraphic = (ushort)newOriginal;
                                GraphicsReplacement.ResetLists();
                            }
                        }

                        ImGui.TableNextColumn();
                        // Initialize input string if not exists
                        if (!entryReplacementInputs.ContainsKey(filter.OriginalGraphic))
                        {
                            entryReplacementInputs[filter.OriginalGraphic] = filter.ReplacementGraphic.ToString();
                        }
                        string replacementStr = entryReplacementInputs[filter.OriginalGraphic];
                        if (ImGui.InputText($"##Replacement{i}", ref replacementStr, 10))
                        {
                            entryReplacementInputs[filter.OriginalGraphic] = replacementStr;
                            if (StringHelper.TryParseGraphic(replacementStr, out int newReplacement))
                            {
                                filter.ReplacementGraphic = (ushort)newReplacement;
                            }
                        }

                        ImGui.TableNextColumn();
                        // Initialize input string if not exists
                        if (!entryHueInputs.ContainsKey(filter.OriginalGraphic))
                        {
                            entryHueInputs[filter.OriginalGraphic] = filter.NewHue == ushort.MaxValue ? "-1" : filter.NewHue.ToString();
                        }
                        string hueStr = entryHueInputs[filter.OriginalGraphic];
                        if (ImGui.InputText($"##Hue{i}", ref hueStr, 10))
                        {
                            entryHueInputs[filter.OriginalGraphic] = hueStr;
                            if (hueStr == "-1")
                            {
                                filter.NewHue = ushort.MaxValue;
                            }
                            else if (ushort.TryParse(hueStr, out ushort newHue))
                            {
                                filter.NewHue = newHue;
                            }
                        }

                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Delete##Delete{i}"))
                        {
                            GraphicsReplacement.DeleteFilter(filter.OriginalGraphic);
                            // Clean up input dictionaries
                            entryOriginalInputs.Remove(filter.OriginalGraphic);
                            entryReplacementInputs.Remove(filter.OriginalGraphic);
                            entryHueInputs.Remove(filter.OriginalGraphic);
                        }
                    }

                    ImGui.EndTable();
                }
            }
        }

    }
}
