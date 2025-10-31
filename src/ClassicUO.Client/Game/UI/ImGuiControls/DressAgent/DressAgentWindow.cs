using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class DressAgentWindow : SingletonImGuiWindow<DressAgentWindow>
    {
        private static readonly Vector4 DeleteButtonColor = new(0.8f, 0.2f, 0.2f, 1.0f);
        private static readonly Vector4 DefaultInfoColor = new(0.0f, 1.0f, 0.0f, 1.0f);

        private int _selectedConfigIndex = -1;
        private DressConfig _selectedConfig = null;
        private bool _editingName = false;
        private string _nameInput = "";

        private DressAgentWindow() : base("Dress Agent")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
        }

        public override void DrawContent()
        {
            if (DressAgentManager.Instance == null)
            {
                ImGui.Text("Dress Agent not loaded");
                return;
            }

            // Main layout: left panel for config list, right panel for details
            if (ImGui.BeginTable("DressAgentTable", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Configurations", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                DrawConfigList();

                ImGui.TableSetColumnIndex(1);
                DrawConfigDetails();

                ImGui.EndTable();
            }
        }

        private void DrawConfigList()
        {
            ImGui.Text("Dress Configurations");

            ImGui.Separator();
            if (ImGui.Button("Add Configuration"))
            {
                DressConfig newConfig = DressAgentManager.Instance.CreateNewConfig($"Config {DressAgentManager.Instance.CurrentPlayerConfigs.Count + 1}");
                _selectedConfigIndex = DressAgentManager.Instance.CurrentPlayerConfigs.IndexOf(newConfig);
                _selectedConfig = newConfig;
            }

            ImGui.Separator();

            // List existing configurations
            System.Collections.Generic.List<DressConfig> configs = DressAgentManager.Instance.CurrentPlayerConfigs;
            for (int i = 0; i < configs.Count; i++)
            {
                DressConfig config = configs[i];
                bool isSelected = i == _selectedConfigIndex;

                string label = $"{config.Name} ({config.Items.Count} items)##Config{i}";

                if (ImGui.Selectable(label, isSelected))
                {
                    _selectedConfigIndex = i;
                    _selectedConfig = config;
                    _editingName = false;
                }

                // Show character name as tooltip
                if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(config.CharacterName))
                {
                    SetTooltip($"Character: {config.CharacterName}");
                }
            }
        }

        private void DrawConfigDetails()
        {
            if (_selectedConfig == null || _selectedConfigIndex == -1)
            {
                ImGui.Text("Select a configuration to view details");
                return;
            }

            // Name input
            if (_editingName)
            {
                _nameInput = _selectedConfig.Name;
                bool enterPressed = ImGui.InputText("##Name", ref _nameInput, 100, ImGuiInputTextFlags.EnterReturnsTrue);
                bool deactivated = ImGui.IsItemDeactivated();

                if (enterPressed || deactivated)
                {
                    if (!string.IsNullOrWhiteSpace(_nameInput))
                    {
                        _selectedConfig.Name = _nameInput.Trim();
                        DressAgentManager.Instance.Save();
                    }
                    _editingName = false;
                }
            }
            else
            {
                ImGui.Text($"Name: {_selectedConfig.Name}");
                ImGui.SameLine();
                if (ImGui.Button("Edit##Name"))
                {
                    _editingName = true;
                }
            }

            // Action buttons
            if (ImGui.Button("Dress"))
            {
                DressAgentManager.Instance.DressFromConfig(_selectedConfig);
                GameActions.Print($"Dressing from config: {_selectedConfig.Name}");
            }

            ImGui.SameLine();
            if (ImGui.Button("Undress"))
            {
                DressAgentManager.Instance.UndressFromConfig(_selectedConfig);
                GameActions.Print($"Undressing from config: {_selectedConfig.Name}");
            }

            ImGui.SameLine();
            if (ImGui.Button("Create Dress Macro"))
            {
                DressAgentManager.Instance.CreateDressMacro(_selectedConfig.Name);
                GameActions.Print($"Created Dress Macro: {_selectedConfig.Name}");
            }

            ImGui.SameLine();
            if (ImGui.Button("Create Undress Macro"))
            {
                DressAgentManager.Instance.CreateUndressMacro(_selectedConfig.Name);
                GameActions.Print($"Created Undress Macro: {_selectedConfig.Name}");
            }

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, DeleteButtonColor);
            if (ImGui.Button("Delete"))
            {
                DressAgentManager.Instance.DeleteConfig(_selectedConfig);

                // Update selection to avoid invalid state
                if (DressAgentManager.Instance.CurrentPlayerConfigs.Count > 0)
                {
                    // Select the previous item, or first if we deleted index 0
                    _selectedConfigIndex = Math.Max(0, _selectedConfigIndex - 1);
                    _selectedConfig = DressAgentManager.Instance.CurrentPlayerConfigs[_selectedConfigIndex];
                }
                else
                {
                    _selectedConfigIndex = -1;
                    _selectedConfig = null;
                }
            }
            ImGui.PopStyleColor();

            if (_selectedConfig == null)
                return;

            ImGui.NewLine();
            ImGui.Separator();

            // KR Equip Packet setting
            bool useKREquipPacket = _selectedConfig.UseKREquipPacket;
            if (ImGui.Checkbox("Use KR Equip Packet (faster)", ref useKREquipPacket))
            {
                _selectedConfig.UseKREquipPacket = useKREquipPacket;
                DressAgentManager.Instance.Save();
            }

            SetTooltip("Uses KR equip/unequip packets for faster operation");

            ImGui.Separator();

            // Undress bag settings
            DrawUndressBagSettings();

            ImGui.NewLine();
            ImGui.Separator();

            // Items section
            DrawItemsSection();
        }

        private void DrawUndressBagSettings()
        {
            ImGui.Text("Undress Bag Settings");

            if (ImGui.Button("Set Undress Bag"))
            {
                GameActions.Print("Select container for undressed items", 82);
                World.Instance.TargetManager.SetTargeting((target) =>
                {
                    if (target == null || !(target is Entity entity) || !SerialHelper.IsItem(entity))
                    {
                        GameActions.Print("Only items can be selected!");
                        return;
                    }

                    // Safety check in case config was deleted during targeting
                    if (_selectedConfig == null)
                    {
                        GameActions.Print("Configuration no longer available!");
                        return;
                    }

                    DressAgentManager.Instance.SetUndressBag(_selectedConfig, entity.Serial);
                    GameActions.Print($"Undress bag set to {entity.Serial:X}", 63);
                });
            }

            ImGui.SameLine();
            if (_selectedConfig.UndressBagSerial != 0)
            {
                ImGui.Text($"Current: ({_selectedConfig.UndressBagSerial:X})");
                ImGui.SameLine();
                if (ImGui.Button("Clear##UndressBag"))
                {
                    DressAgentManager.Instance.SetUndressBag(_selectedConfig, 0);
                }
            }
            else
            {
                ImGui.TextColored(DefaultInfoColor, "Default: Your backpack");
            }
        }

        private void DrawItemsSection()
        {
            ImGui.Text("Items to Dress/Undress");

            // Add item buttons
            if (ImGui.Button("Add Currently Equipped"))
            {
                DressAgentManager.Instance.AddCurrentlyEquippedItems(_selectedConfig);
                GameActions.Print("Added currently equipped items to config");
            }

            ImGui.SameLine();
            if (ImGui.Button("Target Item to Add"))
            {
                GameActions.Print("Target an item to add to this config", 82);
                World.Instance.TargetManager.SetTargeting((obj) =>
                {
                    if (obj == null || !(obj is Entity entity) || !SerialHelper.IsItem(entity))
                    {
                        GameActions.Print("Only items can be added!");
                        return;
                    }

                    // Safety check in case config was deleted during targeting
                    if (_selectedConfig == null)
                    {
                        GameActions.Print("Configuration no longer available!");
                        return;
                    }

                    DressAgentManager.Instance.AddItemToConfig(_selectedConfig, entity.Serial, entity.Name);
                    GameActions.Print($"Added item: {entity.Name}");
                });
            }

            ImGui.SameLine();
            if (ImGui.Button("Clear All Items"))
            {
                DressAgentManager.Instance.ClearConfig(_selectedConfig);
                GameActions.Print("Cleared all items from config");
            }

            ImGui.Separator();

            // Items table
            // Iterate in reverse to safely handle deletions during iteration
            if (ImGui.BeginTable("ItemsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
            {
                ImGui.TableSetupColumn("Serial", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Layer", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Del", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableHeadersRow();

                for (int i = _selectedConfig.Items.Count - 1; i >= 0; i--)
                {
                    DressItem item = _selectedConfig.Items[i];
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"{item.Serial:X}");

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(item.Name);

                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(((Layer)item.Layer).ToString());

                    ImGui.TableSetColumnIndex(3);
                    ImGui.PushStyleColor(ImGuiCol.Button, DeleteButtonColor);
                    if (ImGui.Button($"X##Delete{i}"))
                    {
                        DressAgentManager.Instance.RemoveItemFromConfig(_selectedConfig, item.Serial);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        SetTooltip("Remove this item");
                    }

                    ImGui.PopStyleColor();
                }

                ImGui.EndTable();
            }
        }
    }
}
