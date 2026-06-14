using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighlightProperties : NineSliceGump
    {
        private const int WIDTH = 400, HEIGHT = 540;
        private ScrollArea mainScrollArea;
        GridHighlightData data;
        private readonly int keyLoc;
        private readonly Dictionary<string, Checkbox> slotCheckboxes = new();
        public GridHighlightProperties(World world, int keyLoc, int x, int y) : base(world, x, y, WIDTH, HEIGHT, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BorderSize, true, WIDTH, HEIGHT)
        {
            data = GridHighlightData.GetGridHighlightData(keyLoc);
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            this.keyLoc = keyLoc;
            Build();
        }

        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
            Build();
        }

        private void Build()
        {
            Clear();
            Positioner pos = new();
            IGui temp;

            // Scroll area
            Add(mainScrollArea = new ScrollArea(BorderSize, BorderSize, Width - (BorderSize * 2), Height - (BorderSize * 2), true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            // Accept extra properties checkbox
            string acceptExtraPropertiesTooltip = TazLang.Get("gridhighlight_acceptextra_tooltip");

            Checkbox acceptExtraPropertiesCheckbox;
            mainScrollArea.Add(pos.Position(acceptExtraPropertiesCheckbox = new Checkbox(0x00D2, 0x00D3) { IsChecked = data.AcceptExtraProperties }));
            acceptExtraPropertiesCheckbox.SetTooltip(acceptExtraPropertiesTooltip);
            acceptExtraPropertiesCheckbox.ValueChanged += (s, e) =>
            {
                data.AcceptExtraProperties = acceptExtraPropertiesCheckbox.IsChecked;
            };

            mainScrollArea.Add(pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_allowextra"), true, 0xffff), acceptExtraPropertiesCheckbox));

            // Loot on match checkbox
            string lootOnMatchTooltip = TazLang.Get("gridhighlight_lootonmatch_tooltip");

            Checkbox lootOnMatchCheckbox;
            mainScrollArea.Add(pos.Position(lootOnMatchCheckbox = new Checkbox(0x00D2, 0x00D3) { IsChecked = data.LootOnMatch }));
            lootOnMatchCheckbox.SetTooltip(lootOnMatchTooltip);
            lootOnMatchCheckbox.ValueChanged += (s, e) =>
            {
                data.LootOnMatch = lootOnMatchCheckbox.IsChecked;
            };

            mainScrollArea.Add(pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_lootonmatch"), true, 0xffff), lootOnMatchCheckbox));

            // Destination container input and target button
            InputField destinationInput;
            mainScrollArea.Add(pos.Position(destinationInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 100, 20)));
            string destStr = data.DestinationContainer == 0 ? "" : $"0x{data.DestinationContainer:X}";
            destinationInput.SetText(destStr);
            destinationInput.SetTooltip(TazLang.Get("gridhighlight_destcontainer_tooltip"));
            destinationInput.TextChanged += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(destinationInput.Text))
                {
                    data.DestinationContainer = 0;
                }
                else if (uint.TryParse(destinationInput.Text.Replace("0x", "").Replace("0X", ""), System.Globalization.NumberStyles.HexNumber, null, out uint destSerial))
                {
                    data.DestinationContainer = destSerial;
                }
            };
            mainScrollArea.Add(temp = pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_loottocontainer"), true, 0xffff), destinationInput));

            NiceButton targetContainerBtn;
            mainScrollArea.Add(pos.PositionRightOf(targetContainerBtn = new NiceButton(0, 0, 60, 20, ButtonAction.Activate, TazLang.Get("gridhighlight_target")) { IsSelectable = false }, temp, 10));
            targetContainerBtn.SetTooltip(TazLang.Get("gridhighlight_target_tooltip"));
            targetContainerBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    World.Instance.TargetManager.SetTargeting((targetedContainer) =>
                    {
                        if (targetedContainer != null && targetedContainer is Entity targetedEntity)
                        {
                            if (SerialHelper.IsItem(targetedEntity))
                            {
                                data.DestinationContainer = targetedEntity.Serial;
                                destinationInput.SetText($"0x{targetedEntity.Serial:X}");
                            }
                        }
                    });
                }
            };

            InputField minMatchingInput;
            mainScrollArea.Add(pos.Position(minMatchingInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 40, 20)));
            minMatchingInput.SetText(data.MinimumMatchingProperty.ToString());
            minMatchingInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(minMatchingInput.Text, out int val))
                {
                    data.MinimumMatchingProperty = val;
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
                else
                {
                    minMatchingInput.Add(new FadingLabel(20, TazLang.Get("gridhighlight_parsenumber"), true, 0xff) { X = 0, Y = 0 });
                }
            };
            mainScrollArea.Add(temp = pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_minmatchcount"), true, 0xffff), minMatchingInput));

            InputField maxMatchingInput;
            mainScrollArea.Add(pos.PositionRightOf(maxMatchingInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 40, 20), temp, 20));
            maxMatchingInput.SetText(data.MaximumMatchingProperty.ToString());
            maxMatchingInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(maxMatchingInput.Text, out int val))
                {
                    data.MaximumMatchingProperty = val;
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
                else
                {
                    maxMatchingInput.Add(new FadingLabel(20, TazLang.Get("gridhighlight_parsenumber"), true, 0xff) { X = 0, Y = 0 });
                }
            };
            mainScrollArea.Add(pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_maxmatchcount"), true, 0xffff), maxMatchingInput));

            InputField minPropertiesInput;
            mainScrollArea.Add(pos.Position(minPropertiesInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 40, 20)));
            minPropertiesInput.SetText(data.MinimumProperty.ToString());
            minPropertiesInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(minPropertiesInput.Text, out int val))
                {
                    data.MinimumProperty = val;
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
                else
                {
                    minPropertiesInput.Add(new FadingLabel(20, TazLang.Get("gridhighlight_parsenumber"), true, 0xff) { X = 0, Y = 0 });
                }
            };
            mainScrollArea.Add(temp = pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_minpropcount"), true, 0xffff), minPropertiesInput));

            InputField maxPropertiesInput;
            mainScrollArea.Add(pos.PositionRightOf(maxPropertiesInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 40, 20), temp, 20));
            maxPropertiesInput.SetText(data.MaximumProperty.ToString());
            maxPropertiesInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(maxPropertiesInput.Text, out int val))
                {
                    data.MaximumProperty = val;
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
                else
                {
                    maxPropertiesInput.Add(new FadingLabel(20, TazLang.Get("gridhighlight_parsenumber"), true, 0xff) { X = 0, Y = 0 });
                }
            };
            mainScrollArea.Add(pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_maxpropcount"), true, 0xffff), maxPropertiesInput));

            #region Name

            mainScrollArea.Add(pos.Position(SectionDivider()));
            mainScrollArea.Add(pos.Position(new Label(TazLang.Get("gridhighlight_itemname"), true, 0xffff, 120)));

            for (int i = 0; i < data.ItemNames.Count; i++)
            {
                AddOther(data.ItemNames, i, pos.Y);
                pos.Y += 25;
            }

            NiceButton addItemNameBtn;
            mainScrollArea.Add(pos.Position(addItemNameBtn = new NiceButton(0, 0, 180, 20, ButtonAction.Activate, TazLang.Get("gridhighlight_additemname")) { IsSelectable = false }));
            addItemNameBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.ItemNames.Add("");
                    Build();
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
            };

            #endregion

            #region Properties

            mainScrollArea.Add(pos.Position(SectionDivider()));
            mainScrollArea.Add(new Label(TazLang.Get("gridhighlight_propertyname"), true, 0xffff, 120) { X = 0, Y = pos.Y });
            mainScrollArea.Add(new Label(TazLang.Get("gridhighlight_minvalue"), true, 0xffff, 120) { X = mainScrollArea.Width - 38 - 63 - 75, Y = pos.Y });
            mainScrollArea.Add(new Label(TazLang.Get("gridhighlight_optional"), true, 0xffff, 120) { X = mainScrollArea.Width - 38 - 63, Y = pos.Y });
            pos.Y += 20;

            for (int i = 0; i < data.Properties.Count; i++)
            {
                AddProperty(data.Properties, i, pos.Y, [GridHighlightRules.Properties, GridHighlightRules.SuperSlayerProperties, GridHighlightRules.SlayerProperties]);
                pos.Y += 25;
            }

            NiceButton addPropBtn;
            mainScrollArea.Add(pos.Position(addPropBtn = new NiceButton(0, 0, 180, 20, ButtonAction.Activate, TazLang.Get("gridhighlight_addproperty")) { IsSelectable = false }));
            addPropBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.Properties.Add(new GridHighlightProperty { Name = "", MinValue = -1, IsOptional = false });
                    data.InvalidateCache();
                    Build();
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
            };

            #endregion Properties

            #region Equipment slot

            mainScrollArea.Add(pos.Position(SectionDivider()));
            string[] slotNames = new[] { "Talisman", "RightHand", "LeftHand", "Head", "Earring", "Neck", "Chest", "Shirt", "Back", "Robe", "Arms", "Hands", "Bracelet", "Ring", "Belt", "Skirt", "Legs", "Footwear" };

            mainScrollArea.Add(temp = pos.Position(new Label(TazLang.Get("gridhighlight_selectslots"), true, 0xffff)));
            Checkbox otherCheckbox;
            mainScrollArea.Add(pos.PositionRightOf(otherCheckbox = new Checkbox(0x00D2, 0x00D3) { IsChecked = (bool)typeof(GridHighlightSlot).GetProperty("Other").GetValue(data.EquipmentSlots) }, temp, 20));
            otherCheckbox.ValueChanged += (s, e) =>
            {
                foreach (string slotName in slotNames)
                {
                    typeof(GridHighlightSlot).GetProperty(slotName).SetValue(data.EquipmentSlots, !otherCheckbox.IsChecked);

                    if (slotCheckboxes.TryGetValue(slotName, out Checkbox cb))
                    {
                        cb.IsChecked = !otherCheckbox.IsChecked;
                    }
                }
                data.EquipmentSlots.Other = otherCheckbox.IsChecked;
            };
            mainScrollArea.Add(pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_otherslot"), true, 0xffff), otherCheckbox));

            int columns = Math.Max(1, (mainScrollArea.Width - 18) / 110);

            pos.StartTable(columns, mainScrollArea.Width / columns, 0);

            for (int i = 0; i < slotNames.Length; i++)
            {
                string slotName = slotNames[i];
                bool isChecked = (bool)typeof(GridHighlightSlot).GetProperty(slotName).GetValue(data.EquipmentSlots);

                var cb = new Checkbox(0x00D2, 0x00D3) { IsChecked = isChecked };
                cb.ValueChanged += (s, e) =>
                {
                    typeof(GridHighlightSlot).GetProperty(slotName).SetValue(data.EquipmentSlots, cb.IsChecked);
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                };
                slotCheckboxes[slotName] = cb;

                var label = new Label(SplitCamelCase(slotName), true, 0xFFFF);

                mainScrollArea.Add(pos.Position(cb));
                mainScrollArea.Add(pos.PositionRightOf(label, cb));
            }
            pos.EndTable();

            #endregion Equipment slot


            #region Negative

            mainScrollArea.Add(pos.Position(SectionDivider()));
            mainScrollArea.Add(pos.Position(new Label(TazLang.Get("gridhighlight_disqualifying"), true, 0xffff)));

            // Weight filter
            Checkbox weightCheckbox;
            mainScrollArea.Add(pos.Position(weightCheckbox = new Checkbox(0x00D2, 0x00D3) { IsChecked = data.Overweight }));
            string weightTooltip = TazLang.Get("gridhighlight_weight_tooltip");
            weightCheckbox.SetTooltip(weightTooltip);
            weightCheckbox.ValueChanged += (s, e) =>
            {
                data.Overweight = weightCheckbox.IsChecked;
                GridHighlightData.RecheckMatchStatus();
            };
            mainScrollArea.Add(temp = pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_weightfilter"), true, 0xffff), weightCheckbox));

            InputField minWeightInput;
            mainScrollArea.Add(pos.PositionRightOf(minWeightInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 40, 20) { NumbersOnly = true }, temp, 10));
            minWeightInput.SetText(data.MinimumWeight.ToString());
            minWeightInput.SetTooltip(TazLang.Get("gridhighlight_minweight_tooltip"));
            minWeightInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(minWeightInput.Text, out int val))
                {
                    data.MinimumWeight = val;
                    GridHighlightData.RecheckMatchStatus();
                }
                else
                {
                    minWeightInput.Add(new FadingLabel(20, TazLang.Get("gridhighlight_parsenumber"), true, 0xff) { X = 0, Y = 0 });
                }
            };
            mainScrollArea.Add(temp = pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_min"), true, 0xffff), minWeightInput));

            InputField maxWeightInput;
            mainScrollArea.Add(pos.PositionRightOf(maxWeightInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 40, 20) { NumbersOnly = true }, temp, 10));
            maxWeightInput.SetText(data.MaximumWeight.ToString());
            maxWeightInput.SetTooltip(TazLang.Get("gridhighlight_maxweight_tooltip"));
            maxWeightInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(maxWeightInput.Text, out int val))
                {
                    data.MaximumWeight = val;
                    GridHighlightData.RecheckMatchStatus();
                }
                else
                {
                    maxWeightInput.Add(new FadingLabel(20, TazLang.Get("gridhighlight_parsenumber"), true, 0xff) { X = 0, Y = 0 });
                }
            };
            mainScrollArea.Add(pos.PositionRightOf(new Label(TazLang.Get("gridhighlight_max"), true, 0xffff), maxWeightInput));

            mainScrollArea.Add(pos.Position(new Label(TazLang.Get("gridhighlight_excludedesc"), true, 0xffff)));

            for (int i = 0; i < data.ExcludeNegatives.Count; i++)
            {
                AddOther(data.ExcludeNegatives, i, pos.Y, [GridHighlightRules.NegativeProperties, GridHighlightRules.Properties, GridHighlightRules.SuperSlayerProperties, GridHighlightRules.SlayerProperties]);
                pos.Y += 25;
            }

            mainScrollArea.Add(pos.Position(addItemNameBtn = new NiceButton(0, 0, 180, 20, ButtonAction.Activate, TazLang.Get("gridhighlight_adddisqualifying")) { IsSelectable = false }));
            addItemNameBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.ExcludeNegatives.Add("");
                    data.InvalidateCache();
                    Build();
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
            };

            #endregion Negative

            #region Rarity

            mainScrollArea.Add(pos.Position(SectionDivider()));

            mainScrollArea.Add(pos.Position(new Label(TazLang.Get("gridhighlight_rarityfilters"), true, 0xffff)));
            mainScrollArea.Add(pos.Position(new Label(TazLang.Get("gridhighlight_raritydesc"), true, 0xffff)));

            for (int i = 0; i < data.RequiredRarities.Count; i++)
            {
                AddOther(data.RequiredRarities, i, pos.Y, [GridHighlightRules.RarityProperties]);
                pos.Y += 25;
            }

            NiceButton addRarityBtn;
            mainScrollArea.Add(pos.Position(addRarityBtn = new NiceButton(0, 0, 180, 20, ButtonAction.Activate, TazLang.Get("gridhighlight_addrarity")) { IsSelectable = false }));
            addRarityBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.RequiredRarities.Add("");
                    data.InvalidateCache();
                    Build();
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
            };

            #endregion Rarity
        }

        private Control SectionDivider() => new Line(0, 0, mainScrollArea.Width - 20, 1, Color.Gray.PackedValue);

        private string SplitCamelCase(string input) => System.Text.RegularExpressions.Regex.Replace(input, "(\\B[A-Z])", " $1");

        private void AddOther(List<string> others, int index, int y, HashSet<string>[] propertySets = null)
        {
            while (others.Count <= index)
            {
                others.Add("");
            }

            InputField propInput;
            propInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, mainScrollArea.Width - 65, 25) { Y = y };
            if (propertySets != null)
            {
                string[] values = GridHighlightRules.FlattenAndDistinctParameters(propertySets);
                Combobox propCombobox;
                mainScrollArea.Add(propCombobox = new Combobox(0, y, propInput.Width + 15, values, 0, 200, true) { });
                propCombobox.OnOptionSelected += (s, e) =>
                {
                    int tVal = propCombobox.SelectedIndex;

                    string v = values[tVal];
                    propInput.SetText(v);
                };
            }

            mainScrollArea.Add(propInput);
            propInput.SetText(others[index]);
            propInput.TextChanged += (s, e) =>
            {
                others[index] = propInput.Text;
            };

            NiceButton _del;
            mainScrollArea.Add(_del = new NiceButton(mainScrollArea.Width - 38, y, 20, 20, ButtonAction.Activate, "X") { IsSelectable = false });
            _del.SetTooltip(TazLang.Get("gridhighlight_deleteproperty_tooltip"));
            _del.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    others.RemoveAt(index);
                    Build();
                }
            };
        }

        private void AddProperty(List<GridHighlightProperty> properties, int index, int y, HashSet<string>[] propertySets)
        {
            while (properties.Count <= index)
            {
                var property = new GridHighlightProperty { Name = "", MinValue = -1, IsOptional = false, };
                properties.Add(property);
            }

            Combobox propCombobox;
            InputField propInput;
            propInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, mainScrollArea.Width - 38 - 63 - 97, 25) { Y = y };
            string[] values = GridHighlightRules.FlattenAndDistinctParameters(propertySets);
            mainScrollArea.Add(propCombobox = new Combobox(0, y, mainScrollArea.Width - 38 - 63 - 80, values, 0, 200, true) { });
            propCombobox.OnOptionSelected += (s, e) =>
            {
                int tVal = propCombobox.SelectedIndex;

                string v = values[tVal];
                propInput.SetText(v);
            };

            mainScrollArea.Add(propInput);
            propInput.SetText(properties[index].Name);
            propInput.TextChanged += (s, e) =>
            {
                properties[index].Name = propInput.Text;
            };

            InputField valInput;
            mainScrollArea.Add(valInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 60, 25) { X = mainScrollArea.Width - 38 - 63 - 75, Y = y, NumbersOnly = true });
            valInput.SetText(properties[index].MinValue.ToString());
            valInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(valInput.Text, out int val))
                {
                    properties[index].MinValue = val;
                }
                else
                {
                    valInput.Add(new FadingLabel(20, TazLang.Get("gridhighlight_parsenumber"), true, 0xff) { X = 0, Y = 0 });
                }
            };

            Checkbox isOptionalCheckbox;
            mainScrollArea.Add(isOptionalCheckbox = new Checkbox(0x00D2, 0x00D3) { X = mainScrollArea.Width - 38 - 63, Y = y + 2, IsChecked = properties[index].IsOptional });
            isOptionalCheckbox.ValueChanged += (s, e) =>
            {
                properties[index].IsOptional = isOptionalCheckbox.IsChecked;
            };

            NiceButton _del;
            mainScrollArea.Add(_del = new NiceButton(mainScrollArea.Width - 38, y, 20, 20, ButtonAction.Activate, "X") { IsSelectable = false });
            _del.SetTooltip(TazLang.Get("gridhighlight_deleteproperty_tooltip"));
            _del.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    properties.RemoveAt(index);
                    Build();
                }
            };
        }
    }
}
