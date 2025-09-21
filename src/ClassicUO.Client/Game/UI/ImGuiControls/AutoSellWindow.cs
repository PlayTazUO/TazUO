using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using System;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class AutoSellWindow : SingletonImGuiWindow<AutoSellWindow>
    {
        private Profile _profile;
        private bool _enableAutoSell;
        private int _maxItems;
        private int _maxUniques;

        private string _newGraphicInput = "";
        private string _newHueInput = "";
        private string _newMaxAmountInput = "";
        private string _newRestockInput = "";

        private List<BuySellItemConfig> _sellEntries;
        private bool _showAddEntry = false;
        private Dictionary<BuySellItemConfig, string> _entryGraphicInputs = new Dictionary<BuySellItemConfig, string>();
        private Dictionary<BuySellItemConfig, string> _entryHueInputs = new Dictionary<BuySellItemConfig, string>();
        private Dictionary<BuySellItemConfig, string> _entryMaxAmountInputs = new Dictionary<BuySellItemConfig, string>();
        private Dictionary<BuySellItemConfig, string> _entryRestockInputs = new Dictionary<BuySellItemConfig, string>();

        private AutoSellWindow() : base("Auto Sell")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            _profile = ProfileManager.CurrentProfile;

            _enableAutoSell = _profile.SellAgentEnabled;
            _maxItems = _profile.SellAgentMaxItems;
            _maxUniques = _profile.SellAgentMaxUniques;

            _sellEntries = BuySellAgent.Instance?.SellConfigs ?? new List<BuySellItemConfig>();
        }

        public override void DrawContent()
        {
            if (_profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            // Main settings
            if (ImGui.Checkbox("Enable auto sell", ref _enableAutoSell))
            {
                _profile.SellAgentEnabled = _enableAutoSell;
            }

            if (ImGui.SliderInt("Max total items (0 = unlimited)", ref _maxItems, 0, 100))
            {
                _profile.SellAgentMaxItems = _maxItems;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Maximum total items to sell in a single transaction. Set to 0 for unlimited.");

            if (ImGui.SliderInt("Max unique items", ref _maxUniques, 0, 100))
            {
                _profile.SellAgentMaxUniques = _maxUniques;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Maximum number of different items to sell in a single transaction.");

            ImGui.Separator();

            // Add entry section
            if (ImGui.Button("Add Entry"))
            {
                _showAddEntry = !_showAddEntry;
            }

            ImGui.SameLine();
            if (ImGui.Button("Target Item to Add"))
            {
                World.Instance.TargetManager.SetTargeting((targetedItem) =>
                {
                    if (targetedItem != null && targetedItem is Entity targetedEntity)
                    {
                        if (SerialHelper.IsItem(targetedEntity))
                        {
                            var newConfig = BuySellAgent.Instance.NewSellConfig();
                            newConfig.Graphic = targetedEntity.Graphic;
                            newConfig.Hue = targetedEntity.Hue;
                            _sellEntries = BuySellAgent.Instance.SellConfigs;
                        }
                    }
                });
            }

            if (_showAddEntry)
            {
                ImGui.Separator();
                ImGui.Text("Add New Entry:");

                ImGui.Text("Graphic:");
                ImGui.SameLine();
                ImGui.InputText("##NewGraphic", ref _newGraphicInput, 10);

                ImGui.Text("Hue (-1 for any):");
                ImGui.SameLine();
                ImGui.InputText("##NewHue", ref _newHueInput, 10);

                ImGui.Text("Max Amount:");
                ImGui.SameLine();
                ImGui.InputText("##NewMaxAmount", ref _newMaxAmountInput, 10);

                ImGui.Text("Min on Hand:");
                ImGui.SameLine();
                ImGui.InputText("##NewRestock", ref _newRestockInput, 10);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Minimum amount to keep on hand (0 = disabled)");

                if (ImGui.Button("Add##AddEntry"))
                {
                    if (TryParseGraphic(_newGraphicInput, out int graphic))
                    {
                        var newConfig = BuySellAgent.Instance.NewSellConfig();
                        newConfig.Graphic = (ushort)graphic;

                        if (!string.IsNullOrEmpty(_newHueInput) && _newHueInput != "-1")
                        {
                            if (ushort.TryParse(_newHueInput, out ushort hue))
                                newConfig.Hue = hue;
                        }
                        else
                        {
                            newConfig.Hue = ushort.MaxValue;
                        }

                        if (!string.IsNullOrEmpty(_newMaxAmountInput) && ushort.TryParse(_newMaxAmountInput, out ushort maxAmount))
                        {
                            newConfig.MaxAmount = maxAmount;
                        }

                        if (!string.IsNullOrEmpty(_newRestockInput) && ushort.TryParse(_newRestockInput, out ushort restock))
                        {
                            newConfig.RestockUpTo = restock;
                        }

                        _newGraphicInput = "";
                        _newHueInput = "";
                        _newMaxAmountInput = "";
                        _newRestockInput = "";
                        _showAddEntry = false;
                        _sellEntries = BuySellAgent.Instance.SellConfigs;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel##AddEntry"))
                {
                    _showAddEntry = false;
                    _newGraphicInput = "";
                    _newHueInput = "";
                    _newMaxAmountInput = "";
                    _newRestockInput = "";
                }
            }

            ImGui.Separator();

            if (_sellEntries.Count == 0)
            {
                ImGui.Text("No entries configured");
            }
            else
            {
                // Table headers
                if (ImGui.BeginTable("AutoSellTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 200)))
                {
                    ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 52);
                    ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Hue", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Max Amount", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Min on Hand", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 50);
                    ImGui.TableHeadersRow();

                    for (int i = _sellEntries.Count - 1; i >= 0; i--)
                    {
                        var entry = _sellEntries[i];
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        if (!DrawArt(entry.Graphic, new Vector2(50, 50)))
                            ImGui.Text($"{entry.Graphic:X4}");

                        // Graphic
                        ImGui.TableNextColumn();
                        if (!_entryGraphicInputs.ContainsKey(entry))
                        {
                            _entryGraphicInputs[entry] = entry.Graphic.ToString();
                        }
                        string graphicStr = _entryGraphicInputs[entry];
                        if (ImGui.InputText($"##Graphic{i}", ref graphicStr, 10))
                        {
                            _entryGraphicInputs[entry] = graphicStr;
                            if (TryParseGraphic(graphicStr, out int newGraphic))
                            {
                                entry.Graphic = (ushort)newGraphic;
                            }
                        }

                        // Hue
                        ImGui.TableNextColumn();
                        if (!_entryHueInputs.ContainsKey(entry))
                        {
                            _entryHueInputs[entry] = entry.Hue == ushort.MaxValue ? "-1" : entry.Hue.ToString();
                        }
                        string hueStr = _entryHueInputs[entry];
                        if (ImGui.InputText($"##Hue{i}", ref hueStr, 10))
                        {
                            _entryHueInputs[entry] = hueStr;
                            if (hueStr == "-1")
                            {
                                entry.Hue = ushort.MaxValue;
                            }
                            else if (ushort.TryParse(hueStr, out ushort newHue))
                            {
                                entry.Hue = newHue;
                            }
                        }

                        // Max Amount
                        ImGui.TableNextColumn();
                        if (!_entryMaxAmountInputs.ContainsKey(entry))
                        {
                            _entryMaxAmountInputs[entry] = entry.MaxAmount == ushort.MaxValue ? "0" : entry.MaxAmount.ToString();
                        }
                        string maxAmountStr = _entryMaxAmountInputs[entry];
                        if (ImGui.InputText($"##MaxAmount{i}", ref maxAmountStr, 10))
                        {
                            _entryMaxAmountInputs[entry] = maxAmountStr;
                            if (ushort.TryParse(maxAmountStr, out ushort newMaxAmount))
                            {
                                entry.MaxAmount = newMaxAmount == 0 ? ushort.MaxValue : newMaxAmount;
                            }
                        }

                        // Restock/Min on Hand
                        ImGui.TableNextColumn();
                        if (!_entryRestockInputs.ContainsKey(entry))
                        {
                            _entryRestockInputs[entry] = entry.RestockUpTo.ToString();
                        }
                        string restockStr = _entryRestockInputs[entry];
                        if (ImGui.InputText($"##Restock{i}", ref restockStr, 10))
                        {
                            _entryRestockInputs[entry] = restockStr;
                            if (ushort.TryParse(restockStr, out ushort newRestock))
                            {
                                entry.RestockUpTo = newRestock;
                            }
                        }

                        // Enabled
                        ImGui.TableNextColumn();
                        bool enabled = entry.Enabled;
                        if (ImGui.Checkbox($"##Enabled{i}", ref enabled))
                        {
                            entry.Enabled = enabled;
                        }

                        // Actions
                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Delete##Delete{i}"))
                        {
                            BuySellAgent.Instance?.DeleteConfig(entry);
                            // Clean up input dictionaries
                            _entryGraphicInputs.Remove(entry);
                            _entryHueInputs.Remove(entry);
                            _entryMaxAmountInputs.Remove(entry);
                            _entryRestockInputs.Remove(entry);
                            _sellEntries = BuySellAgent.Instance.SellConfigs;
                        }
                    }

                    ImGui.EndTable();
                }
            }
        }

        private bool TryParseGraphic(string text, out int graphic)
        {
            graphic = 0;
            if (string.IsNullOrEmpty(text)) return false;

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(text.Substring(2), NumberStyles.AllowHexSpecifier, null, out graphic);
            }
            else
            {
                return int.TryParse(text, out graphic);
            }
        }
    }
}
