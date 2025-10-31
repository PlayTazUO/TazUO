using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighlightProperties : NineSliceGump
    {
        private const int WIDTH = 400, HEIGHT = 540;
        private ScrollArea mainScrollArea;
        GridHighlightData data;
        private readonly int keyLoc;
        private readonly Dictionary<string, Checkbox> slotCheckboxes = new();
        public GridHighlightProperties(World world, int keyLoc, int x, int y) : base(world, x, y, WIDTH, HEIGHT, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, true, WIDTH, HEIGHT)
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
            Control temp;

            // Scroll area
            Add(mainScrollArea = new ScrollArea(BorderSize, BorderSize, Width - (BorderSize * 2), Height - (BorderSize * 2), true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            // Accept extra properties checkbox
            string acceptExtraPropertiesTooltip =
                "Highlight items with properties beyond your configuration.\n" +
                "When checked: The item must match all configured properties and may have extra ones.\n" +
                "When un-checked: The item must match all configured properties and must not have any extra properties.";

            Checkbox acceptExtraPropertiesCheckbox;
            mainScrollArea.Add(pos.Position(acceptExtraPropertiesCheckbox = new Checkbox(0x00D2, 0x00D3) { IsChecked = data.AcceptExtraProperties }));
            acceptExtraPropertiesCheckbox.SetTooltip(acceptExtraPropertiesTooltip);
            acceptExtraPropertiesCheckbox.ValueChanged += (s, e) =>
            {
                data.AcceptExtraProperties = acceptExtraPropertiesCheckbox.IsChecked;
            };

            mainScrollArea.Add(pos.PositionRightOf(new Label("Allow extra properties", true, 0xffff), acceptExtraPropertiesCheckbox));

            // Loot on match checkbox
            string lootOnMatchTooltip =
                "Automatically loot items that match this highlight configuration.\n" +
                "When checked: Items matching this configuration will be added to the auto loot queue.";

            Checkbox lootOnMatchCheckbox;
            mainScrollArea.Add(pos.Position(lootOnMatchCheckbox = new Checkbox(0x00D2, 0x00D3) { IsChecked = data.LootOnMatch }));
            lootOnMatchCheckbox.SetTooltip(lootOnMatchTooltip);
            lootOnMatchCheckbox.ValueChanged += (s, e) =>
            {
                data.LootOnMatch = lootOnMatchCheckbox.IsChecked;
            };

            mainScrollArea.Add(pos.PositionRightOf(new Label("Auto loot on match", true, 0xffff), lootOnMatchCheckbox));

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
                    minPropertiesInput.Add(new FadingLabel(20, "Couldn't parse number", true, 0xff) { X = 0, Y = 0 });
                }
            };
            mainScrollArea.Add(temp = pos.PositionRightOf(new Label("Min. property count", true, 0xffff), minPropertiesInput));

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
                    maxPropertiesInput.Add(new FadingLabel(20, "Couldn't parse number", true, 0xff) { X = 0, Y = 0 });
                }
            };
            mainScrollArea.Add(pos.PositionRightOf(new Label("Max. property count", true, 0xffff), maxPropertiesInput));;

            #region Name

            mainScrollArea.Add(pos.Position(SectionDivider()));
            mainScrollArea.Add(pos.Position(new Label("Item name", true, 0xffff, 120)));

            for (int i = 0; i < data.ItemNames.Count; i++)
            {
                AddOther(data.ItemNames, i, pos.Y);
                pos.Y += 25;
            }

            NiceButton addItemNameBtn;
            mainScrollArea.Add(pos.Position(addItemNameBtn = new NiceButton(0, 0, 180, 20, ButtonAction.Activate, "Add Item Name") { IsSelectable = false }));
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
            mainScrollArea.Add(new Label("Property name", true, 0xffff, 120) { X = 0, Y = pos.Y });
            mainScrollArea.Add(new Label("Min value", true, 0xffff, 120) { X = mainScrollArea.Width - 38 - 63 - 75, Y = pos.Y });
            mainScrollArea.Add(new Label("Optional", true, 0xffff, 120) { X = mainScrollArea.Width - 38 - 63, Y = pos.Y });
            pos.Y += 20;

            for (int i = 0; i < data.Properties.Count; i++)
            {
                AddProperty(data.Properties, i, pos.Y, [GridHighlightRules.Properties, GridHighlightRules.SuperSlayerProperties, GridHighlightRules.SlayerProperties]);
                pos.Y += 25;
            }

            NiceButton addPropBtn;
            mainScrollArea.Add(pos.Position(addPropBtn = new NiceButton(0, 0, 180, 20, ButtonAction.Activate, "Add Property") { IsSelectable = false }));
            addPropBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.Properties.Add(new GridHighlightProperty { Name = "", MinValue = -1, IsOptional = false });
                    Build();
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
            };

            #endregion Properties

            #region Equipment slot

            mainScrollArea.Add(pos.Position(SectionDivider()));
            string[] slotNames = new[] { "Talisman", "RightHand", "LeftHand", "Head", "Earring", "Neck", "Chest", "Shirt", "Back", "Robe", "Arms", "Hands", "Bracelet", "Ring", "Belt", "Skirt", "Legs", "Footwear" };

            mainScrollArea.Add(temp = pos.Position(new Label("Select equipment slots", true, 0xffff)));
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
            mainScrollArea.Add(pos.PositionRightOf(new Label("Other / No Slot Assigned", true, 0xffff), otherCheckbox));

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
            mainScrollArea.Add(temp = pos.Position(new Label("Disqualifying Properties", true, 0xffff)));
            Checkbox weightCheckbox;
            mainScrollArea.Add(pos.PositionRightOf(weightCheckbox = new Checkbox(0x00D2, 0x00D3) { IsChecked = data.Overweight }, temp));
            weightCheckbox.ValueChanged += (s, e) =>
            {
                data.Overweight = weightCheckbox.IsChecked;
            };
            mainScrollArea.Add(pos.PositionRightOf(new Label("Overweight (=50)", true, 0xffff), weightCheckbox));

            mainScrollArea.Add(pos.Position(new Label("Items with any of these properties will be excluded", true, 0xffff)));

            for (int i = 0; i < data.ExcludeNegatives.Count; i++)
            {
                AddOther(data.ExcludeNegatives, i, pos.Y, [GridHighlightRules.NegativeProperties, GridHighlightRules.Properties, GridHighlightRules.SuperSlayerProperties, GridHighlightRules.SlayerProperties]);
                pos.Y += 25;
            }

            mainScrollArea.Add(pos.Position(addItemNameBtn = new NiceButton(0, 0, 180, 20, ButtonAction.Activate, "Add Disqualifying Property") { IsSelectable = false }));
            addItemNameBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.ExcludeNegatives.Add("");
                    Build();
                    GridHighlightData.RecheckMatchStatus(); //Request new opl data and re-check item matches
                }
            };

            #endregion Negative

            #region Rarity

            mainScrollArea.Add(pos.Position(SectionDivider()));

            mainScrollArea.Add(pos.Position(new Label("Item Rarity Filters", true, 0xffff)));
            mainScrollArea.Add(pos.Position(new Label("Only items with at least one of these rarities will match", true, 0xffff)));

            for (int i = 0; i < data.RequiredRarities.Count; i++)
            {
                AddOther(data.RequiredRarities, i, pos.Y, [GridHighlightRules.RarityProperties]);
                pos.Y += 25;
            }

            NiceButton addRarityBtn;
            mainScrollArea.Add(pos.Position(addRarityBtn = new NiceButton(0, 0, 180, 20, ButtonAction.Activate, "Add Rarity Filter") { IsSelectable = false }));
            addRarityBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.RequiredRarities.Add("");
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
            _del.SetTooltip("Delete this property");
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
                    valInput.Add(new FadingLabel(20, "Couldn't parse number", true, 0xff) { X = 0, Y = 0 });
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
            _del.SetTooltip("Delete this property");
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
