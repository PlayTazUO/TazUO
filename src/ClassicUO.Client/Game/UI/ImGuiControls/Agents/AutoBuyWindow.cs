using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using System.Numerics;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class AutoBuyWindow : SingletonImGuiWindow<AutoBuyWindow>
    {
        private Profile _profile;
        private bool _enableAutoBuy;
        private int _maxItems;
        private int _maxUniques;

        private string _newGraphicInput = "";
        private string _newHueInput = "";
        private string _newMaxAmountInput = "";
        private string _newRestockInput = "";

        private List<BuySellItemConfig> _buyEntries;
        private bool _showAddEntry = false;
        private Dictionary<BuySellItemConfig, string> _entryGraphicInputs = new Dictionary<BuySellItemConfig, string>();
        private Dictionary<BuySellItemConfig, string> _entryHueInputs = new Dictionary<BuySellItemConfig, string>();
        private Dictionary<BuySellItemConfig, string> _entryMaxAmountInputs = new Dictionary<BuySellItemConfig, string>();
        private Dictionary<BuySellItemConfig, string> _entryRestockInputs = new Dictionary<BuySellItemConfig, string>();

        private AutoBuyWindow() : base("Auto Buy")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            _profile = ProfileManager.CurrentProfile;

            if (_profile == null)
            {
                Dispose();
                return;
            }

            _enableAutoBuy = _profile.BuyAgentEnabled;
            _maxItems = _profile.BuyAgentMaxItems;
            _maxUniques = _profile.BuyAgentMaxUniques;

            _buyEntries = BuySellAgent.Instance?.BuyConfigs ?? new List<BuySellItemConfig>();
        }

        public override void DrawContent()
        {
            if (_profile == null)
            {
                ImGui.Text("Profile not loaded");
                return;
            }

            // Main settings
            ImGui.Spacing();
            if (ImGui.Checkbox("Enable Auto Buy", ref _enableAutoBuy))
            {
                _profile.BuyAgentEnabled = _enableAutoBuy;
            }

            ImGui.SeparatorText("Options:");
            ImGui.Spacing();

            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("Max total items", ref _maxItems, 0, 1000))
            {
                _profile.BuyAgentMaxItems = _maxItems;
            }
            ImGuiComponents.Tooltip("Maximum total items to buy in a single transaction. Set to 0 for unlimited.");

            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("Max unique items", ref _maxUniques, 0, 100))
            {
                _profile.BuyAgentMaxUniques = _maxUniques;
            }
                ImGuiComponents.Tooltip("Maximum number of different items to buy in a single transaction.");

            ImGui.Spacing();

            ImGui.SeparatorText("Entries:");
            // Add entry section
            if (ImGui.Button("Add Manual Entry"))
            {
                _showAddEntry = !_showAddEntry;
            }

            ImGui.SameLine();
            if (ImGui.Button("Add from Target"))
            {
                GameActions.Print(Client.Game.UO.World, "Target item to add");
                World.Instance.TargetManager.SetTargeting((targetedItem) =>
                {
                    if (targetedItem != null && targetedItem is Entity targetedEntity)
                    {
                        if (SerialHelper.IsItem(targetedEntity))
                        {
                            BuySellItemConfig newConfig = BuySellAgent.Instance.NewBuyConfig();
                            newConfig.Graphic = targetedEntity.Graphic;
                            newConfig.Hue = targetedEntity.Hue;
                            _buyEntries = BuySellAgent.Instance.BuyConfigs;
                        }
                    }
                });
            }

            if (_showAddEntry)
            {
                ImGui.SeparatorText("Add New Entry:");
                ImGui.Spacing();

                ImGui.BeginGroup();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Graphic:");
                ImGui.SetNextItemWidth(70);
                ImGui.InputText("##NewGraphic", ref _newGraphicInput, 10);
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Hue (-1 for any):");
                ImGui.SetNextItemWidth(50);
                ImGui.InputText("##NewHue", ref _newHueInput, 10);
                ImGui.EndGroup();

                ImGui.BeginGroup();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Max Amount:");
                ImGui.SetNextItemWidth(100);
                ImGui.InputText("##NewMaxAmount", ref _newMaxAmountInput, 10);
                ImGuiComponents.Tooltip("Set to 0 for unlimited.");
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Restock Up To:");
                ImGui.SetNextItemWidth(100);
                ImGui.InputText("##NewRestock", ref _newRestockInput, 10);
                ImGuiComponents.Tooltip("Amount to restock up to when buying (0 = disabled)");

                ImGui.EndGroup();

                ImGui.Spacing();

                if (ImGui.Button("Add##AddEntry"))
                {
                    if (StringHelper.TryParseGraphic(_newGraphicInput, out int graphic))
                    {
                        BuySellItemConfig newConfig = BuySellAgent.Instance.NewBuyConfig();
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
                            newConfig.MaxAmount = maxAmount == 0 ? ushort.MaxValue : maxAmount;
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
                        _buyEntries = BuySellAgent.Instance.BuyConfigs;
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

            ImGui.Spacing();

            if (_buyEntries.Count == 0)
            {
                ImGui.Separator();
                ImGui.Text("No entries configured.");
            }
            else
            {
                // Table headers
                if (ImGui.BeginTable("AutoBuyTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
                {
                    ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 52);
                    ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                    ImGui.TableSetupColumn("Hue", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                    ImGui.TableSetupColumn("Max Amount", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                    ImGui.TableSetupColumn("Restock Up To", ImGuiTableColumnFlags.WidthFixed, ImGuiTheme.Dimensions.STANDARD_INPUT_WIDTH);
                    ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 50);
                    ImGui.TableHeadersRow();

                    for (int i = _buyEntries.Count - 1; i >= 0; i--)
                    {
                        BuySellItemConfig entry = _buyEntries[i];
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
                            if (StringHelper.TryParseGraphic(graphicStr, out int newGraphic))
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

                        // Restock Up To
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
                            _buyEntries = BuySellAgent.Instance.BuyConfigs;
                        }
                    }

                    ImGui.EndTable();
                }
            }
        }

    }
}
