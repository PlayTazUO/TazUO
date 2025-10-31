using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class JournalFilterWindow : SingletonImGuiWindow<JournalFilterWindow>
    {
        private Profile profile;
        private string newFilterInput = "";
        private bool showAddFilter = false;
        private Dictionary<string, string> filterInputs = new Dictionary<string, string>();
        private List<string> filterList;

        private JournalFilterWindow() : base("Journal Filter")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            profile = ProfileManager.CurrentProfile;
            RefreshFilterList();
        }

        private void RefreshFilterList()
        {
            filterList = JournalFilterManager.Instance.Filters.ToList();

            // Initialize input dictionaries for existing filters
            foreach (string filter in filterList)
            {
                if (!filterInputs.ContainsKey(filter))
                {
                    filterInputs[filter] = filter;
                }
            }
        }

        public override void DrawContent()
        {
            if (profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            ImGui.Spacing();
            ImGui.TextWrapped("Journal Filter allows you to hide specific messages from the journal. Messages that match exactly will be filtered out.");

            // Wiki link
            if (ImGui.Button("Journal Filter Wiki"))
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser("https://github.com/PlayTazUO/TazUO/wiki/Journal-Filters");
            }

            ImGui.SeparatorText("Import & Export:");

            if (ImGui.Button("Export JSON"))
            {
                FileSelector.ShowFileBrowser(Client.Game.UO.World, FileSelectorType.Directory, null, null, (selectedPath) =>
                {
                    if (string.IsNullOrWhiteSpace(selectedPath)) return;
                    string fileName = $"JournalFilters_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    string fullPath = Path.Combine(selectedPath, fileName);
                    JsonHelper.SaveAndBackup(JournalFilterManager.Instance.Filters, fullPath, HashSetContext.Default.HashSetString);
                }, "Export Journal Filter Configuration");
            }

            ImGui.SameLine();
            if (ImGui.Button("Import JSON"))
            {
                FileSelector.ShowFileBrowser(Client.Game.UO.World, FileSelectorType.File, null, new[] { "json" }, (selectedFile) =>
                {
                    if (string.IsNullOrWhiteSpace(selectedFile)) return;
                    if (JsonHelper.Load(selectedFile, HashSetContext.Default.HashSetString, out HashSet<string> importedFilters))
                    {
                        foreach (string filter in importedFilters)
                        {
                            JournalFilterManager.Instance.AddFilter(filter);
                        }
                        JournalFilterManager.Instance.Save(false);
                        RefreshFilterList();
                    }
                }, "Import Journal Filter Configuration");
            }

            // Add filter section
            ImGui.SeparatorText("Filters:");

            if (ImGui.Button("Add Filter Entry"))
            {
                showAddFilter = !showAddFilter;
            }

            if (showAddFilter)
            {
                ImGui.SeparatorText("Add New Filter:");
                ImGui.Spacing();

                ImGui.Text("Filter Text:");
                ImGuiComponents.Tooltip("Must match the journal entry exactly. Partial matches not supported.");
                ImGui.InputText("##NewFilter", ref newFilterInput, 500);

                ImGui.Spacing();

                if (ImGui.Button("Add##AddFilter"))
                {
                    if (!string.IsNullOrWhiteSpace(newFilterInput))
                    {
                        JournalFilterManager.Instance.AddFilter(newFilterInput);
                        JournalFilterManager.Instance.Save(false);
                        newFilterInput = "";
                        showAddFilter = false;
                        RefreshFilterList();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel##AddFilter"))
                {
                    showAddFilter = false;
                    newFilterInput = "";
                }
            }

            ImGui.SeparatorText("Current Journal Filters:");

            if (filterList.Count == 0)
            {
                ImGui.Text("No filters configured");
            }
            else
            {
                // Table for filters
                if (ImGui.BeginTable("JournalFilterTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
                {
                    ImGui.TableSetupColumn("Filter Text", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableHeadersRow();

                    for (int i = filterList.Count - 1; i >= 0; i--)
                    {
                        string filter = filterList[i];
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();

                        // Get or initialize input string for this filter
                        if (!filterInputs.ContainsKey(filter))
                        {
                            filterInputs[filter] = filter;
                        }

                        string filterStr = filterInputs[filter];
                        ImGui.InputText($"##Filter{i}", ref filterStr, 500);

                        // Update the local input string
                        filterInputs[filter] = filterStr;

                        // Only commit changes on Enter or blur, and avoid no-ops/empty values
                        if (ImGui.IsItemDeactivatedAfterEdit() && !string.IsNullOrWhiteSpace(filterStr) && filterStr != filter)
                        {
                            // Update the filter
                            string oldFilter = filter;
                            JournalFilterManager.Instance.RemoveFilter(oldFilter);
                            JournalFilterManager.Instance.AddFilter(filterStr);
                            JournalFilterManager.Instance.Save(false);

                            // Update our tracking - remove old key, add new one
                            filterInputs.Remove(oldFilter);
                            filterInputs[filterStr] = filterStr;

                            RefreshFilterList();
                            break; // Break out of loop after successful mutation to avoid iterating refreshed list
                        }

                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Delete##Delete{i}"))
                        {
                            JournalFilterManager.Instance.RemoveFilter(filter);
                            JournalFilterManager.Instance.Save(false);
                            filterInputs.Remove(filter);
                            RefreshFilterList();
                        }
                    }

                    ImGui.EndTable();
                }
            }
        }
    }
}