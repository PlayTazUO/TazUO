using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class TooltipConfigGump : NineSliceGump
    {
        private const int WIDTH = 600, HEIGHT = 500;
        private VBoxContainer mainContainer;
        private VBoxContainer dataContainer;

        public TooltipConfigGump(int x = 100, int y = 100) : base(World.Instance, x, y, WIDTH, HEIGHT, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, true, WIDTH, HEIGHT)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            CanCloseWithEsc = true;

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

            mainContainer = new VBoxContainer(Width - (BorderSize * 2), 0, 5) {X = BorderSize, Y = BorderSize};
            Add(mainContainer);

            AddHeader();
            AddButtonRow();
            AddDataContainer();
            BuildTooltipData();
        }

        private void AddHeader()
        {
            var titleLabel = TextBox.GetOne("Tooltip Override Configuration", TrueTypeLoader.EMBEDDED_FONT, 18, Color.OrangeRed, TextBox.RTLOptions.Default());

            mainContainer.Add(titleLabel);

            var wikiLink = new HttpClickableLink("Tooltip Overrides Wiki", "https://github.com/PlayTazUO/TazUO/wiki/TazUO.Tooltip-Override", Color.White);

            mainContainer.Add(wikiLink);
        }

        private void AddButtonRow()
        {
            var buttonContainer = new Area(false);

            var addButton = new NiceButton(0, 0, 60, 20, ButtonAction.Activate, "Add +")
            {
                IsSelectable = false,
                DisplayBorder = true
            };

            addButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    AddNewTooltipRow(ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count);
                }
            };

            buttonContainer.Add(addButton);

            var exportButton = new NiceButton(65, 0, 60, 20, ButtonAction.Activate, "Export")
            {
                IsSelectable = false,
                DisplayBorder = true
            };

            exportButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    ToolTipOverrideData.ExportOverrideSettings(World);
                }
            };

            buttonContainer.Add(exportButton);

            var importButton = new NiceButton(130, 0, 60, 20, ButtonAction.Activate, "Import")
            {
                IsSelectable = false,
                DisplayBorder = true
            };

            importButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    ToolTipOverrideData.ImportOverrideSettings();
                }
            };

            buttonContainer.Add(importButton);

            var deleteAllButton = new NiceButton(195, 0, 100, 20, ButtonAction.Activate, "Delete All")
            {
                IsSelectable = false,
                DisplayBorder = true
            };

            deleteAllButton.SetTooltip("/c[red]This will remove ALL tooltip override settings.\nThis is not reversible.");

            deleteAllButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    UIManager.Add(new QuestionGump(World, "Are you sure?", (confirmed) =>
                    {
                        if (confirmed)
                        {
                            ClearAllTooltipData();
                        }
                    }));
                }
            };

            buttonContainer.Add(deleteAllButton);
            buttonContainer.ForceSizeUpdate();
            mainContainer.Add(buttonContainer);
        }

        private void AddDataContainer()
        {
            var scrollArea = new ModernScrollArea(0, 0, mainContainer.Width, Height - 120)
            {
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            dataContainer = new VBoxContainer(scrollArea.Width - 20);
            scrollArea.Add(dataContainer);

            mainContainer.Add(scrollArea);
        }

        private void BuildTooltipData()
        {
            dataContainer.Clear();

            for (int i = 0; i < ProfileManager.CurrentProfile.ToolTipOverride_SearchText.Count; i++)
            {
                AddTooltipRow(i);
            }
        }

        private void AddTooltipRow(int index)
        {
            var data = ToolTipOverrideData.Get(index);
            Control rowContainer = CreateTooltipRow(data, index);
            dataContainer.Add(rowContainer);
        }

        private void AddNewTooltipRow(int index)
        {
            var data = ToolTipOverrideData.Get(index);
            Control rowContainer = CreateTooltipRow(data, index);
            dataContainer.Add(rowContainer);
        }

        private Control CreateTooltipRow(ToolTipOverrideData data, int index)
        {
            var rowContainer = new Area(true)
            {
                Width = dataContainer.Width,
                Height = 50,
                AcceptMouseInput = true
            };

            // Row 1: Search text and Format text
            var searchTextInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, (rowContainer.Width - 25 - 18) / 2, 20)
            {
                X = 25,
                Y = 5,
                AcceptKeyboardInput = true
            };
            searchTextInput.SetText(data.SearchText);
            searchTextInput.TextChanged += (s, e) => SaveWithDelay(() =>
            {
                if (!string.IsNullOrEmpty(searchTextInput.Text))
                {
                    data.SearchText = searchTextInput.Text;
                    data.Save();
                    ShowSavedMessage(searchTextInput);
                }
            }, searchTextInput.Text);
            searchTextInput.SetTooltip("This is the search text for matching tooltip lines.");

            rowContainer.Add(searchTextInput);

            var formatTextInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, (rowContainer.Width - 25 - 18) / 2, 20)
            {
                X = searchTextInput.X + searchTextInput.Width + 5,
                Y = 5,
                AcceptKeyboardInput = true
            };
            formatTextInput.SetText(data.FormattedText);
            formatTextInput.TextChanged += (s, e) => SaveWithDelay(() =>
            {
                data.FormattedText = formatTextInput.Text;
                data.Save();
                ShowSavedMessage(formatTextInput);
            }, formatTextInput.Text);
            formatTextInput.SetTooltip("This is what the matching tooltip line will be replaced with. See the wiki for more details!");

            rowContainer.Add(formatTextInput);

            // Row 2: Min/Max values and Layer
            var minMaxLabel = new Label("Min/Max", true, 0xFFFF)
            {
                X = 5,
                Y = 25
            };
            rowContainer.Add(minMaxLabel);

            InputField min1Input = CreateNumericInput(data.Min1.ToString(), 50, 20, minMaxLabel.X + minMaxLabel.Width + 3, 25);
            min1Input.TextChanged += (s, e) => SaveWithDelay(() =>
            {
                if (int.TryParse(min1Input.Text, out int val))
                {
                    data.Min1 = val;
                    data.Save();
                    ShowSavedMessage(min1Input);
                }
            }, min1Input.Text);
            rowContainer.Add(min1Input);

            InputField max1Input = CreateNumericInput(data.Max1.ToString(), 50, 20, min1Input.X + min1Input.Width + 3, 25);
            max1Input.TextChanged += (s, e) => SaveWithDelay(() =>
            {
                if (int.TryParse(max1Input.Text, out int val))
                {
                    data.Max1 = val;
                    data.Save();
                    ShowSavedMessage(max1Input);
                }
            }, max1Input.Text);
            rowContainer.Add(max1Input);

            var minMaxLabel2 = new Label("Min/Max", true, 0xFFFF)
            {
                X = max1Input.X + max1Input.Width + 15,
                Y = 25
            };
            rowContainer.Add(minMaxLabel2);

            InputField min2Input = CreateNumericInput(data.Min2.ToString(), 50, 20, minMaxLabel2.X + minMaxLabel2.Width + 3, 25);
            min2Input.TextChanged += (s, e) => SaveWithDelay(() =>
            {
                if (int.TryParse(min2Input.Text, out int val))
                {
                    data.Min2 = val;
                    data.Save();
                    ShowSavedMessage(min2Input);
                }
            }, min2Input.Text);
            rowContainer.Add(min2Input);

            InputField max2Input = CreateNumericInput(data.Max2.ToString(), 50, 20, min2Input.X + min2Input.Width + 3, 25);
            max2Input.TextChanged += (s, e) => SaveWithDelay(() =>
            {
                if (int.TryParse(max2Input.Text, out int val))
                {
                    data.Max2 = val;
                    data.Save();
                    ShowSavedMessage(max2Input);
                }
            }, max2Input.Text);
            rowContainer.Add(max2Input);

            var layerCombobox = new Combobox(max2Input.X + max2Input.Width + 5, max2Input.Y, 110,
                Enum.GetNames(typeof(TooltipLayers)),
                Array.IndexOf(Enum.GetValues(typeof(TooltipLayers)), data.ItemLayer));

            layerCombobox.OnOptionSelected += (s, e) =>
            {
                data.ItemLayer = (TooltipLayers)(Enum.GetValues(typeof(TooltipLayers))).GetValue(layerCombobox.SelectedIndex);
                data.Save();
                ShowSavedMessage(layerCombobox);
            };

            rowContainer.Add(layerCombobox);

            // Delete button
            var deleteButton = new NiceButton(0, 5, 20, 20, ButtonAction.Activate, "X")
            {
                IsSelectable = false
            };
            deleteButton.SetTooltip("Delete this override");
            deleteButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    data.Delete();
                    BuildTooltipData();
                }
            };

            rowContainer.Add(deleteButton);
            rowContainer.ForceSizeUpdate();
            return rowContainer;
        }

        private InputField CreateNumericInput(string text, int width, int height, int x, int y)
        {
            var input = new InputField(0x0BB8, 0xFF, 0xFFFF, true, width, height)
            {
                X = x,
                Y = y,
                AcceptKeyboardInput = true,
                NumbersOnly = true
            };
            input.SetText(text);
            return input;
        }

        private void SaveWithDelay(Action saveAction, string currentValue) => Task.Factory.StartNew(() =>
                                                                                       {
                                                                                           System.Threading.Thread.Sleep(1500);
                                                                                           MainThreadQueue.EnqueueAction(saveAction);
                                                                                       });

        private void ShowSavedMessage(Control control) => UIManager.Add(new SimpleTimedTextGump(World, "Saved", Color.LightGreen, TimeSpan.FromSeconds(1))
        {
            X = control.ScreenCoordinateX,
            Y = control.ScreenCoordinateY - 20
        });

        private void ClearAllTooltipData()
        {
            ProfileManager.CurrentProfile.ToolTipOverride_SearchText = new List<string>();
            ProfileManager.CurrentProfile.ToolTipOverride_NewFormat = new List<string>();
            ProfileManager.CurrentProfile.ToolTipOverride_MinVal1 = new List<int>();
            ProfileManager.CurrentProfile.ToolTipOverride_MinVal2 = new List<int>();
            ProfileManager.CurrentProfile.ToolTipOverride_MaxVal1 = new List<int>();
            ProfileManager.CurrentProfile.ToolTipOverride_MaxVal2 = new List<int>();
            ProfileManager.CurrentProfile.ToolTipOverride_Layer = new List<byte>();

            BuildTooltipData();
        }
    }
}
