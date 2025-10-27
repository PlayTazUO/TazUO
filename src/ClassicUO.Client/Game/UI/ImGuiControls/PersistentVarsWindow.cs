using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClassicUO.Configuration;
using ClassicUO.LegionScripting;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class PersistentVarsWindow : SingletonImGuiWindow<PersistentVarsWindow>
    {
        private API.PersistentVar _selectedScope = API.PersistentVar.Char;
        private string _filterText = "";
        private string _newKey = "";
        private string _newValue = "";
        private string _editingKey = null;
        private string _editingValue = "";
        private bool _showAddDialog = false;
        private string _deleteConfirmKey = null;

        private PersistentVarsWindow() : base("Persistent Variables Manager")
        {
            WindowFlags = ImGuiWindowFlags.None;
        }

        public override void DrawContent()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 6));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));

            // Scope selector
            DrawScopeSelector();
            ImGui.Separator();

            // Filter and Add button
            DrawToolbar();
            ImGui.Separator();

            // Variables table
            DrawVariablesTable();

            // Dialogs
            DrawAddDialog();
            DrawDeleteConfirmDialog();

            ImGui.PopStyleVar(3);
        }

        private void DrawScopeSelector()
        {
            ImGui.Text("Scope:");
            ImGui.SameLine();

            if (ImGui.RadioButton("Character", _selectedScope == API.PersistentVar.Char))
                _selectedScope = API.PersistentVar.Char;
            ImGui.SameLine();

            if (ImGui.RadioButton("Account", _selectedScope == API.PersistentVar.Account))
                _selectedScope = API.PersistentVar.Account;
            ImGui.SameLine();

            if (ImGui.RadioButton("Server", _selectedScope == API.PersistentVar.Server))
                _selectedScope = API.PersistentVar.Server;
            ImGui.SameLine();

            if (ImGui.RadioButton("Global", _selectedScope == API.PersistentVar.Global))
                _selectedScope = API.PersistentVar.Global;

            // Show current scope info
            ImGui.SameLine();
            ImGui.TextDisabled($"({GetScopeDescription()})");
        }

        private void DrawToolbar()
        {
            ImGui.SetNextItemWidth(200);
            ImGui.InputTextWithHint("##filter", "Filter variables...", ref _filterText, 256);

            ImGui.SameLine();
            if (ImGui.Button("Add New Variable"))
            {
                _showAddDialog = true;
                _newKey = "";
                _newValue = "";
            }

            ImGui.SameLine();
            if (ImGui.Button("Refresh"))
            {
                // Force reload from file
                PersistentVars.Load();
            }
        }

        private void DrawVariablesTable()
        {
            Dictionary<string, string> variables = GetVariablesForScope();

            // Apply filter
            if (!string.IsNullOrWhiteSpace(_filterText))
            {
                variables = variables.Where(kv =>
                    kv.Key.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                    kv.Value.Contains(_filterText, StringComparison.OrdinalIgnoreCase)
                ).ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            if (variables.Count == 0)
            {
                ImGui.TextDisabled("No variables found.");
                return;
            }

            // Table with columns: Key, Value, Actions
            if (ImGui.BeginTable("VarsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, 200);
                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupScrollFreeze(0, 1); // Freeze header row
                ImGui.TableHeadersRow();

                foreach (KeyValuePair<string, string> kvp in variables)
                {
                    ImGui.TableNextRow();
                    ImGui.PushID(kvp.Key);

                    // Key column
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(kvp.Key);

                    // Value column
                    ImGui.TableSetColumnIndex(1);
                    if (_editingKey == kvp.Key)
                    {
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.InputText("##edit", ref _editingValue, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
                        {
                            SaveEdit(kvp.Key);
                        }
                        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                        {
                            CancelEdit();
                        }
                    }
                    else
                    {
                        ImGui.Text(kvp.Value);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(kvp.Value);
                        }
                    }

                    // Actions column
                    ImGui.TableSetColumnIndex(2);
                    if (_editingKey == kvp.Key)
                    {
                        if (ImGui.Button("Save"))
                        {
                            SaveEdit(kvp.Key);
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Cancel"))
                        {
                            CancelEdit();
                        }
                    }
                    else
                    {
                        if (ImGui.Button("Edit"))
                        {
                            StartEdit(kvp.Key, kvp.Value);
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Delete"))
                        {
                            _deleteConfirmKey = kvp.Key;
                        }
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        private void DrawAddDialog()
        {
            if (_showAddDialog)
            {
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                if (ImGui.Begin("Add Variable", ref _showAddDialog, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text($"Add new variable to {_selectedScope} scope:");
                    ImGui.Separator();

                    ImGui.Text("Key:");
                    ImGui.SetNextItemWidth(300);
                    ImGui.InputText("##newkey", ref _newKey, 256);

                    ImGui.Text("Value:");
                    ImGui.SetNextItemWidth(300);
                    ImGui.InputText("##newvalue", ref _newValue, 1024);

                    ImGui.Separator();

                    if (ImGui.Button("Add"))
                    {
                        if (!string.IsNullOrWhiteSpace(_newKey))
                        {
                            PersistentVars.SaveVar(_selectedScope, _newKey.Trim(), _newValue);
                            _showAddDialog = false;
                            _newKey = "";
                            _newValue = "";
                        }
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        _showAddDialog = false;
                        _newKey = "";
                        _newValue = "";
                    }
                }
                ImGui.End();
            }
        }

        private void DrawDeleteConfirmDialog()
        {
            if (_deleteConfirmKey != null)
            {
                ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                bool open = true;
                if (ImGui.Begin("Confirm Delete", ref open, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text($"Are you sure you want to delete variable '{_deleteConfirmKey}'?");
                    ImGui.Separator();

                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1.0f));

                    if (ImGui.Button("Delete"))
                    {
                        PersistentVars.DeleteVar(_selectedScope, _deleteConfirmKey);
                        _deleteConfirmKey = null;
                    }

                    ImGui.PopStyleColor(3);

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        _deleteConfirmKey = null;
                    }
                }
                ImGui.End();

                if (!open)
                {
                    _deleteConfirmKey = null;
                }
            }
        }

        private Dictionary<string, string> GetVariablesForScope() => PersistentVars.GetAllVars(_selectedScope);

        private string GetScopeDescription()
        {
            switch (_selectedScope)
            {
                case API.PersistentVar.Char:
                    return $"{ProfileManager.CurrentProfile.ServerName} - {ProfileManager.CurrentProfile.CharacterName}";
                case API.PersistentVar.Account:
                    return $"{ProfileManager.CurrentProfile.ServerName} - {ProfileManager.CurrentProfile.Username}";
                case API.PersistentVar.Server:
                    return ProfileManager.CurrentProfile.ServerName;
                case API.PersistentVar.Global:
                    return "All servers and characters";
                default:
                    return "";
            }
        }

        private void StartEdit(string key, string value)
        {
            _editingKey = key;
            _editingValue = value;
        }

        private void SaveEdit(string key)
        {
            if (_editingKey == key)
            {
                PersistentVars.SaveVar(_selectedScope, key, _editingValue);
                CancelEdit();
            }
        }

        private void CancelEdit()
        {
            _editingKey = null;
            _editingValue = "";
        }
    }
}
