using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Renderer.Lights;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ClassicUO.Game.UI.Gumps.OptionsGump;
using static IronPython.Modules._ast;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighlightProperties : Gump
    {
        private const int WIDTH = 350, HEIGHT = 500;
        private int lastYitem = 0;
        private ScrollArea mainScrollArea;
        GridHighlightData data;
        private readonly int keyLoc;
        private readonly Dictionary<string, Checkbox> slotCheckboxes = new();
        public GridHighlightProperties(int keyLoc, int x, int y) : base(0, 0)
        {
            data = GridHighlightData.GetGridHighlightData(keyLoc);
            X = x;
            Y = y;
            Width = WIDTH;
            Height = HEIGHT;
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Add(new AlphaBlendControl(0.85f) { Width = WIDTH, Height = HEIGHT });

            lastYitem = 0;

            // Scroll area
            Add(mainScrollArea = new ScrollArea(0, 0, WIDTH, HEIGHT, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            // Accept extra properties checkbox
            string acceptExtraPropertiesTooltip =
                "Highlight items with properties beyond your configuration.\n" +
                "When checked: The item must match all configured properties and may have extra ones.\n" +
                "When un-checked: The item must match all configured properties and must not have any extra properties.";

            Checkbox acceptExtraPropertiesCheckbox;
            mainScrollArea.Add(acceptExtraPropertiesCheckbox = new Checkbox(0x00D2, 0x00D3)
            {
                X = 0,
                Y = 0,
                IsChecked = data.AcceptExtraProperties
            });
            acceptExtraPropertiesCheckbox.SetTooltip(acceptExtraPropertiesTooltip);
            acceptExtraPropertiesCheckbox.ValueChanged += (s, e) =>
            {
                data.AcceptExtraProperties = acceptExtraPropertiesCheckbox.IsChecked;
            };

            Label acceptExtraPropertiesLabel;
            mainScrollArea.Add(acceptExtraPropertiesLabel = new Label("Allow extra properties", true, 0xffff) { X = 20, Y = 0 });

            lastYitem += 20;

            InputField minPropertiesInput;
            mainScrollArea.Add(minPropertiesInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 40, 20) { X = 0, Y = lastYitem });
            minPropertiesInput.SetText(data.MinimumProperty.ToString());
            minPropertiesInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(minPropertiesInput.Text, out int val))
                {
                    data.MinimumProperty = val;
                }
                else
                {
                    minPropertiesInput.Add(new FadingLabel(20, "Couldn't parse number", true, 0xff) { X = 0, Y = 0 });
                }
            };
            Label minPropertiesLabel;
            mainScrollArea.Add(minPropertiesLabel = new Label("Min. property count", true, 0xffff) { X = minPropertiesInput.X + minPropertiesInput.Width, Y = lastYitem });

            InputField maxPropertiesInput;
            mainScrollArea.Add(maxPropertiesInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 40, 20) { X = 180, Y = lastYitem });
            maxPropertiesInput.SetText(data.MaximumProperty.ToString());
            maxPropertiesInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(maxPropertiesInput.Text, out int val))
                {
                    data.MaximumProperty = val;
                }
                else
                {
                    maxPropertiesInput.Add(new FadingLabel(20, "Couldn't parse number", true, 0xff) { X = 0, Y = 0 });
                }
            };
            Label maxPropertiesLabel;
            mainScrollArea.Add(maxPropertiesLabel = new Label("Max. property count", true, 0xffff) { X = maxPropertiesInput.X + maxPropertiesInput.Width, Y = lastYitem });

            lastYitem += 20;

            #region Properties
            mainScrollArea.Add(new Label("Property name", true, 0xffff, 120) { X = 0, Y = lastYitem });
            mainScrollArea.Add(new Label("Min value", true, 0xffff, 120) { X = 180, Y = lastYitem });
            mainScrollArea.Add(new Label("Optional", true, 0xffff, 120) { X = 255, Y = lastYitem });
            lastYitem += 20;

            for (int i = 0; i < data.Properties.Count; i++)
            {
                AddProperty(data.Properties, i, lastYitem, [GridHighlightRules.Properties, GridHighlightRules.SuperSlayerProperties, GridHighlightRules.SlayerProperties]);
                lastYitem += 25;
            }

            NiceButton addPropBtn;
            mainScrollArea.Add(addPropBtn = new NiceButton(0, lastYitem, 180, 20, ButtonAction.Activate, "Add Property") { IsSelectable = false });
            addPropBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                data.Properties.Add(new GridHighlightProperty
                {
                    Name = "",
                    MinValue = -1,
                    IsOptional = false
                });
                    Dispose();
                    UIManager.Add(new GridHighlightProperties(keyLoc, X, Y));
                }
            };
            #endregion Properties

            lastYitem += 30;

            #region Equipment slot
            string[] slotNames = new[]
            {
                "Talisman", "RightHand", "LeftHand",
                "Head", "Earring", "Neck",
                "Chest", "Shirt", "Back",
                "Robe", "Arms", "Hands",
                "Bracelet", "Ring", "Belt",
                "Skirt", "Legs", "Footwear"
            };

            mainScrollArea.Add(new Label("Select equipment slots", true, 0xffff) { X = 0, Y = lastYitem });
            Checkbox otherCheckbox;
            mainScrollArea.Add(otherCheckbox = new Checkbox(0x00D2, 0x00D3)
            {
                X = 150,
                Y = lastYitem,
                IsChecked = (bool)typeof(GridHighlightSlot).GetProperty("Other").GetValue(data.EquipmentSlots)
            });
            otherCheckbox.ValueChanged += (s, e) =>
            {
                foreach (string slotName in slotNames)
                {
                    typeof(GridHighlightSlot).GetProperty(slotName).SetValue(data.EquipmentSlots, !otherCheckbox.IsChecked);

                    if (slotCheckboxes.TryGetValue(slotName, out var cb))
                    {
                        cb.IsChecked = !otherCheckbox.IsChecked;
                    }
                }
                typeof(GridHighlightSlot).GetProperty("Other").SetValue(data.EquipmentSlots, otherCheckbox.IsChecked);
            };
            mainScrollArea.Add(new Label("Other / No Slot Assigned", true, 0xffff) { X = otherCheckbox.X + 20, Y = lastYitem });

            lastYitem += 20;

            int colWidth = 110;
            int checkboxHeight = 22;
            int colCount = 3;

            for (int i = 0; i < slotNames.Length; i++)
            {
                int col = i % colCount;
                int row = i / colCount;

                string slotName = slotNames[i];
                bool isChecked = (bool)typeof(GridHighlightSlot).GetProperty(slotName).GetValue(data.EquipmentSlots);

                Checkbox cb = new Checkbox(0x00D2, 0x00D3)
                {
                    X = col * colWidth,
                    Y = lastYitem + row * checkboxHeight,
                    IsChecked = isChecked
                };
                string currentSlotName = slotName;
                cb.ValueChanged += (s, e) =>
                {
                    typeof(GridHighlightSlot).GetProperty(slotName).SetValue(data.EquipmentSlots, cb.IsChecked);
                };
                slotCheckboxes[slotName] = cb;

                Label label = new Label(SplitCamelCase(slotName), true, 0xFFFF)
                {
                    X = cb.X + 20,
                    Y = cb.Y
                };

                mainScrollArea.Add(cb);
                mainScrollArea.Add(label);
            }
            #endregion Equipment slot

            lastYitem += ((slotNames.Length + colCount - 1) / colCount) * checkboxHeight + 10;

            #region Negative
            mainScrollArea.Add(new Label("Disqualifying Properties", true, 0xffff) { X = 0, Y = lastYitem });
            Checkbox weightCheckbox;
            mainScrollArea.Add(weightCheckbox = new Checkbox(0x00D2, 0x00D3)
            {
                X = 150,
                Y = lastYitem,
                IsChecked = data.Overweight
            });
            weightCheckbox.ValueChanged += (s, e) =>
            {
                data.Overweight = weightCheckbox.IsChecked;
            };
            mainScrollArea.Add(new Label("Overweight (=50)", true, 0xffff) { X = weightCheckbox.X + 20, Y = lastYitem });

            lastYitem += 20;
            mainScrollArea.Add(new Label("Items with any of these properties will be excluded", true, 0xffff) { X = 0, Y = lastYitem });
            lastYitem += 20;

            for (int i = 0; i < data.ExcludeNegatives.Count; i++)
            {
                AddOther(data.ExcludeNegatives, i, lastYitem, [GridHighlightRules.NegativeProperties, GridHighlightRules.Properties, GridHighlightRules.SuperSlayerProperties, GridHighlightRules.SlayerProperties]);
                lastYitem += 25;
            }

            NiceButton addNegBtn;
            mainScrollArea.Add(addNegBtn = new NiceButton(0, lastYitem, 180, 20, ButtonAction.Activate, "Add Disqualifying Property") { IsSelectable = false });
            addNegBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.ExcludeNegatives.Add("");
                    Dispose();
                    UIManager.Add(new GridHighlightProperties(keyLoc, X, Y));
                }
            };
            #endregion Negative

            lastYitem += 30;

            #region Rarity
            mainScrollArea.Add(new Label("Item Rarity Filters", true, 0xffff) { X = 0, Y = lastYitem });
            lastYitem += 20;
            mainScrollArea.Add(new Label("Only items with at least one of these rarities will match", true, 0xffff) { X = 0, Y = lastYitem });
            lastYitem += 20;

            for (int i = 0; i < data.RequiredRarities.Count; i++)
            {
                AddOther(data.RequiredRarities, i, lastYitem, [GridHighlightRules.RarityProperties]);
                lastYitem += 25;
            }

            NiceButton addRarityBtn;
            mainScrollArea.Add(addRarityBtn = new NiceButton(0, lastYitem, 180, 20, ButtonAction.Activate, "Add Rarity Filter") { IsSelectable = false });
            addRarityBtn.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    data.RequiredRarities.Add("");
                    Dispose();
                    UIManager.Add(new GridHighlightProperties(keyLoc, X, Y));
                }
            };
            #endregion Rarity

            this.keyLoc = keyLoc;
        }

        private string SplitCamelCase(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "(\\B[A-Z])", " $1");
        }

        private void AddOther(List<string> others, int index, int y, HashSet<string>[] propertySets)
        {
            while (others.Count <= index)
            {
                others.Add("");
            }

            Combobox propCombobox;
            InputField propInput;
            propInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 157, 25) { Y = y };
            string[] values = GridHighlightRules.FlattenAndDistinctParameters(propertySets);
            mainScrollArea.Add(propCombobox = new Combobox(0, lastYitem, 175, values, 0, 200, true) { });
            propCombobox.OnOptionSelected += (s, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    var tVal = propCombobox.SelectedIndex;
                    if (propCombobox.SelectedIndex == tVal)
                    {
                        string v = values[tVal];
                        propInput.SetText(v);
                    }
                });
            };

            mainScrollArea.Add(propInput);
            propInput.SetText(others[index]);
            propInput.TextChanged += (s, e) =>
            {
                others[index] = propInput.Text;
            };

            NiceButton _del;
            mainScrollArea.Add(_del = new NiceButton(315, y, 20, 20, ButtonAction.Activate, "X") { IsSelectable = false });
            _del.SetTooltip("Delete this property");
            _del.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    Dispose();
                    others.RemoveAt(index);
                    UIManager.Add(new GridHighlightProperties(keyLoc, X, Y));
                }
            };
        }

        private void AddProperty(List<GridHighlightProperty> properties, int index, int y, HashSet<string>[] propertySets)
        {
            while (properties.Count <= index)
            {
                GridHighlightProperty property = new GridHighlightProperty
                {
                    Name = "",
                    MinValue = -1,
                    IsOptional = false,
                };
                properties.Add(property);
            }

            Combobox propCombobox;
            InputField propInput;
            propInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 157, 25) { Y = y };
            string[] values = GridHighlightRules.FlattenAndDistinctParameters(propertySets);
            mainScrollArea.Add(propCombobox = new Combobox(0, lastYitem, 175, values, 0, 200, true) { });
            propCombobox.OnOptionSelected += (s, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    var tVal = propCombobox.SelectedIndex;
                    if (propCombobox.SelectedIndex == tVal)
                    {
                        string v = values[tVal];
                        propInput.SetText(v);
                    }
                });
            };

            mainScrollArea.Add(propInput);
            propInput.SetText(properties[index].Name);
            propInput.TextChanged += (s, e) =>
            {
                properties[index].Name = propInput.Text;
            };

            InputField valInput;
            mainScrollArea.Add(valInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 60, 25) { X = 180, Y = y, NumbersOnly = true });
            valInput.SetText(properties[index].MinValue.ToString());
            valInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(valInput.Text, out int val))
                {
                    properties[index].MinValue = val;
                }
                else
                {
                    valInput.Add(new FadingLabel(20, "Couldn't parse number", true, 0xff) { X = 180, Y = 0 });
                }
            };

            Checkbox isOptionalCheckbox;
            mainScrollArea.Add(isOptionalCheckbox = new Checkbox(0x00D2, 0x00D3)
            {
                X = 255,
                Y = y + 2,
                IsChecked = properties[index].IsOptional
            });
            isOptionalCheckbox.ValueChanged += (s, e) =>
            {
                properties[index].IsOptional = isOptionalCheckbox.IsChecked;
            };

            NiceButton _del;
            mainScrollArea.Add(_del = new NiceButton(315, y, 20, 20, ButtonAction.Activate, "X") { IsSelectable = false });
            _del.SetTooltip("Delete this property");
            _del.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    Dispose();
                    properties.RemoveAt(index);
                    UIManager.Add(new GridHighlightProperties(keyLoc, X, Y));
                }
            };
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.LightGray),
                x - 1, y - 1,
                WIDTH + 2, HEIGHT + 2,
                new Vector3(0, 0, 1)
                );

            return true;
        }
    }
}
