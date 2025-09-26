using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class OrganizerWindow : SingletonImGuiWindow<OrganizerWindow>
    {
        private int _selectedConfigIndex = -1;
        private OrganizerConfig _selectedConfig = null;
        private string _addItemGraphicInput = "";
        private string _addItemHueInput = "";
        private bool _showAddItemManual = false;

        private OrganizerWindow() : base("Organizer")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
        }

        public override void DrawContent()
        {
            if (OrganizerAgent.Instance == null)
            {
                ImGui.Text("Organizer Agent not loaded");
                return;
            }

            // Main layout: left panel for organizer list, right panel for details
            if (ImGui.BeginTable("OrganizerTable", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Organizers", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                DrawOrganizerList();

                ImGui.TableSetColumnIndex(1);
                DrawOrganizerDetails();

                ImGui.EndTable();
            }
        }

        private void DrawOrganizerList()
        {
            ImGui.Text("Organizers");

            ImGui.Separator();
            if (ImGui.Button("Add Organizer"))
            {
                var newConfig = OrganizerAgent.Instance.NewOrganizerConfig();
                _selectedConfigIndex = OrganizerAgent.Instance.OrganizerConfigs.IndexOf(newConfig);
                _selectedConfig = newConfig;
            }

            ImGui.Separator();

            // List existing organizers
            var configs = OrganizerAgent.Instance.OrganizerConfigs;
            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                bool isSelected = i == _selectedConfigIndex;

                string label = $"{config.Name}##Config{i}";
                if (config.Enabled)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                }

                if (ImGui.Selectable(label, isSelected))
                {
                    _selectedConfigIndex = i;
                    _selectedConfig = config;
                }

                if (config.Enabled)
                {
                    ImGui.PopStyleColor();
                }

                // Show item count as tooltip
                if (ImGui.IsItemHovered())
                {
                    int enabledItems = config.ItemConfigs.Count(ic => ic.Enabled);
                    ImGui.SetTooltip($"{enabledItems} enabled items");
                }
            }
        }

        private void DrawOrganizerDetails()
        {
            if (_selectedConfig == null || _selectedConfigIndex == -1)
            {
                ImGui.Text("Select an organizer to view details");
                return;
            }

            // ImGui.Text($"Organizer Details: {_selectedConfig.Name}");
            // ImGui.Separator();

            // Name input
            string name = _selectedConfig.Name;
            if (ImGui.InputText("Name", ref name, 100))
            {
                _selectedConfig.Name = name;
            }

            ImGui.SameLine();
            bool enabled = _selectedConfig.Enabled;
            if(ImGui.Checkbox("Enabled", ref enabled))
                _selectedConfig.Enabled = enabled;

            // Action buttons
            if (ImGui.Button("Run Organizer"))
            {
                OrganizerAgent.Instance?.RunOrganizer(_selectedConfig.Name);
            }

            ImGui.SameLine();
            if (ImGui.Button("Duplicate"))
            {
                var duplicated = OrganizerAgent.Instance?.DupeConfig(_selectedConfig);
                if (duplicated != null)
                {
                    _selectedConfigIndex = OrganizerAgent.Instance.OrganizerConfigs.IndexOf(duplicated);
                    _selectedConfig = duplicated;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Create Macro"))
            {
                OrganizerAgent.Instance?.CreateOrganizerMacroButton(_selectedConfig.Name);
                GameActions.Print($"Created Organizer Macro: {_selectedConfig.Name}");
            }

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 1.0f));
            if (ImGui.Button("Delete"))
            {
                OrganizerAgent.Instance?.DeleteConfig(_selectedConfig);
                //_selectedConfig = null;
                _selectedConfigIndex = -1;
            }
            ImGui.PopStyleColor();
            ImGui.NewLine();
            ImGui.Separator();

            // Container settings
            DrawContainerSettings();

            ImGui.NewLine();
            ImGui.Separator();

            // Items section
            DrawItemsSection();
        }

        private void DrawContainerSettings()
        {
            ImGui.Text("Container Settings");

            if (ImGui.Button("Set Source Container"))
            {
                GameActions.Print("Select [SOURCE] Container", 82);
                World.Instance.TargetManager.SetTargeting((source) =>
                {
                    if (source == null || !(source is Entity sourceEntity) || !SerialHelper.IsItem(sourceEntity))
                    {
                        GameActions.Print("Only items can be selected!");
                        return;
                    }
                    _selectedConfig.SourceContSerial = sourceEntity.Serial;
                    GameActions.Print($"Source container set to {sourceEntity.Serial:X}", 63);
                });
            }

            ImGui.SameLine();
            if (ImGui.Button("Set Destination Container"))
            {
                GameActions.Print("Select [DESTINATION] Container", 82);
                World.Instance.TargetManager.SetTargeting((destination) =>
                {
                    if (destination == null || !(destination is Entity destEntity) || !SerialHelper.IsItem(destEntity))
                    {
                        GameActions.Print("Only items can be selected!");
                        return;
                    }
                    _selectedConfig.DestContSerial = destEntity.Serial;
                    GameActions.Print($"Destination container set to {destEntity.Serial:X}", 63);
                });
            }

            // Display current containers
            if (_selectedConfig.SourceContSerial != 0)
            {
                ImGui.Text($"Source: ({_selectedConfig.SourceContSerial:X})");
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f), "Source: Your backpack");
            }

            ImGui.SameLine();

            if (_selectedConfig.DestContSerial != 0)
            {
                ImGui.Text($"Destination: ({_selectedConfig.DestContSerial:X})");
            }
            else
            {
                ImGui.Text("Destination: Not set");
            }
        }

        private void DrawItemsSection()
        {
            ImGui.Text("Items to Organize");

            // Add item buttons
            if (ImGui.Button("Target Item to Add"))
            {
                World.Instance.TargetManager.SetTargeting((obj) =>
                {
                    if (obj == null || !(obj is Entity objEntity) || !SerialHelper.IsItem(objEntity))
                    {
                        GameActions.Print("Only items can be added!");
                        return;
                    }

                    var newItemConfig = _selectedConfig.NewItemConfig();
                    newItemConfig.Graphic = objEntity.Graphic;
                    newItemConfig.Hue = objEntity.Hue;

                    GameActions.Print($"Added item: Graphic {objEntity.Graphic:X}, Hue {objEntity.Hue:X}");
                });
            }

            ImGui.SameLine();
            if (ImGui.Button("Add Item Manually"))
            {
                _showAddItemManual = !_showAddItemManual;
            }

            // Manual add item section
            if (_showAddItemManual)
            {
                ImGui.InputText("Graphic (hex)", ref _addItemGraphicInput, 10);
                ImGui.SameLine();
                ImGui.InputText("Hue (-1 for any)", ref _addItemHueInput, 10);
                ImGui.SameLine();
                if (ImGui.Button("Add"))
                {
                    if (ushort.TryParse(_addItemGraphicInput, System.Globalization.NumberStyles.HexNumber, null, out ushort graphic))
                    {
                        var newItemConfig = _selectedConfig.NewItemConfig();
                        newItemConfig.Graphic = graphic;

                        if (int.TryParse(_addItemHueInput, out int hue) && hue >= -1)
                        {
                            newItemConfig.Hue = hue == -1 ? ushort.MaxValue : (ushort)hue;
                        }

                        _addItemGraphicInput = "";
                        _addItemHueInput = "";
                        _showAddItemManual = false;
                    }
                }
            }

            ImGui.Separator();

            // Items table
            if (ImGui.BeginTable("ItemsTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
            {
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, 55);
                ImGui.TableSetupColumn("Hue", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Amount", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableSetupColumn("Del", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                ImGui.TableHeadersRow();

                for (int i = _selectedConfig.ItemConfigs.Count - 1; i >= 0; i--)
                {
                    var itemConfig = _selectedConfig.ItemConfigs[i];
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    if (!DrawArt(itemConfig.Graphic, new Vector2(50, 50)))
                        ImGui.Text($"{itemConfig.Graphic:X4}");

                    ImGui.TableSetColumnIndex(1);
                    string hueText = itemConfig.Hue == ushort.MaxValue ? "ANY" : itemConfig.Hue.ToString();
                    if (ImGui.InputText($"##Hue{i}", ref hueText, 5))
                    {
                        if (ushort.TryParse(hueText, System.Globalization.NumberStyles.HexNumber, null, out ushort hue))
                            itemConfig.Hue = hue == 0xFFFF ? ushort.MaxValue : hue;
                        else if (hueText == "ANY")
                            itemConfig.Hue = ushort.MaxValue;
                    }

                    SetTooltip("Set to ANY to match any hue.");

                    ImGui.TableSetColumnIndex(2);
                    int amount = itemConfig.Amount;
                    if (ImGui.InputInt($"##Amount{i}", ref amount, 0, 0))
                    {
                        itemConfig.Amount = (ushort)Math.Max(0, Math.Min(65535, amount));
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("This takes into account the items in the destination container.\n(0 = move all items)");
                    }

                    ImGui.TableSetColumnIndex(3);
                    bool enabled = itemConfig.Enabled;
                    if (ImGui.Checkbox($"##Enabled{i}", ref enabled))
                        itemConfig.Enabled = enabled;

                    ImGui.TableSetColumnIndex(4);
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                    if (ImGui.Button($"X##Delete{i}"))
                    {
                        _selectedConfig.DeleteItemConfig(itemConfig);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Delete this item");
                    }

                    ImGui.PopStyleColor();
                }

                ImGui.EndTable();
            }
        }
    }
}
