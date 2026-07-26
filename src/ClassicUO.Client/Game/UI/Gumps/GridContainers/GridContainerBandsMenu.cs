using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Gumps.GridContainers
{
    /// <summary>
    /// Myra-based editor for grid-container bands. Lists every band with per-row enable, rename,
    /// background-color, layer/graphic filter, reorder and delete actions, plus a toolbar to add a band.
    /// Bands are stored per-profile in <see cref="GridContainerBandsConfig"/>.
    /// </summary>
    internal class GridContainerBandsMenu : MyraControl
    {
        private readonly World _world;
        private readonly VerticalStackPanel _listPanel = new() { Spacing = MyraStyle.STANDARD_SPACING };

        public GridContainerBandsMenu(World world) : base(TazLang.Get("gridbands_title", "Grid Container Bands"))
        {
            _world = world;
            Build();
            CenterInViewPort();
        }

        public static void Open(World world)
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump is GridContainerBandsMenu w && !w.IsDisposed)
                {
                    w.BringOnTop();
                    return;
                }
            }

            UIManager.Add(new GridContainerBandsMenu(world));
        }

        /// <summary>Persists the band config and refreshes every open grid container.</summary>
        internal static void SaveAndRefresh()
        {
            GridContainerBandsConfig.Current.Save();
            GridContainer.UpdateAllGridContainers();
        }

        private void Build()
        {
            var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

            root.Widgets.Add(new MyraLabel(
                TazLang.Get("gridbands_desc", "Bands group items in a grid container into sections by item layer and/or graphic. The first matching band wins. Unmatched items are shown last."),
                MyraLabel.TextStyle.P) { Width = 460 });

            root.Widgets.Add(BuildToolbar());

            RebuildList();
            root.Widgets.Add(new ScrollViewer { MaxHeight = 400, Content = _listPanel });

            SetRootContent(root);
        }

        private Widget BuildToolbar()
        {
            var toolbar = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

            toolbar.Widgets.Add(new MyraButton(TazLang.Get("gridbands_add", "Add Band"), () =>
            {
                List<GridContainerBand> bands = GridContainerBandsConfig.Current.Bands;
                bands.Add(new GridContainerBand { Name = TazLang.Get("gridbands_defaultname", "Band") + " " + (bands.Count + 1) });
                GridContainerBandsConfig.Current.Save();
                RebuildList();
            }));

            return toolbar;
        }

        private void RebuildList()
        {
            _listPanel.Widgets.Clear();

            int count = GridContainerBandsConfig.Current.Bands.Count;
            if (count == 0)
            {
                _listPanel.Widgets.Add(new MyraLabel(TazLang.Get("gridbands_empty", "No bands configured yet."), MyraLabel.TextStyle.P));
                ForceSizeUpdate();
                return;
            }

            for (int i = 0; i < count; i++)
                _listPanel.Widgets.Add(BuildRow(i));

            ForceSizeUpdate();
        }

        private Widget BuildRow(int index)
        {
            List<GridContainerBand> bands = GridContainerBandsConfig.Current.Bands;
            GridContainerBand band = bands[index];

            var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

            row.Widgets.Add(MyraCheckButton.CreateWithCallback(band.Enabled, isChecked =>
            {
                band.Enabled = isChecked;
                SaveAndRefresh();
            }, tooltip: TazLang.Get("gridbands_enabled_tooltip", "Enable this band")));

            var nameBox = new MyraInputBox { Text = band.Name ?? "", Width = 130 };
            nameBox.TextChangedByUser += (_, _) => band.Name = nameBox.Text ?? "";
            nameBox.LostFocus = () => GridContainerBandsConfig.Current.Save();
            row.Widgets.Add(nameBox);

            row.Widgets.Add(MyraCheckButton.CreateWithCallback(band.UseBackgroundColor, isChecked =>
            {
                band.UseBackgroundColor = isChecked;
                SaveAndRefresh();
            }, tooltip: TazLang.Get("gridbands_usecolor_tooltip", "Use a custom background color for this band's slots")));

            var colorButton = new MyraButton(TazLang.Get("gridbands_color", "Color")) { Tooltip = TazLang.Get("gridbands_color_tooltip", "Pick this band's background color") };
            ApplyColorButtonStyle(colorButton, band.GetBackgroundColor());
            colorButton.OnClick = () => RGBColorPickerGump.Open(band.GetBackgroundColor(), selectedColor =>
            {
                band.SetBackgroundColor(selectedColor);
                ApplyColorButtonStyle(colorButton, selectedColor);
                SaveAndRefresh();
            });
            row.Widgets.Add(colorButton);

            row.Widgets.Add(new MyraButton(TazLang.Get("gridbands_layers", "Layers"), () => GridContainerBandLayerPicker.Show(_world, index))
            {
                Tooltip = TazLang.Get("gridbands_layers_tooltip", "Choose which item layers belong to this band")
            });

            row.Widgets.Add(new MyraButton(TazLang.Get("gridbands_graphics", "Graphics"), () => GridContainerBandGraphicsEditor.Show(_world, index))
            {
                Tooltip = TazLang.Get("gridbands_graphics_tooltip", "Choose which item graphics belong to this band")
            });

            row.Widgets.Add(new MyraButton(TazLang.Get("gridbands_up", "Up"), () => Move(index, true))
            {
                Tooltip = TazLang.Get("gridbands_up_tooltip", "Move band up")
            });

            row.Widgets.Add(new MyraButton(TazLang.Get("gridbands_down", "Down"), () => Move(index, false))
            {
                Tooltip = TazLang.Get("gridbands_down_tooltip", "Move band down")
            });

            row.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("X", () =>
            {
                bands.RemoveAt(index);
                SaveAndRefresh();
                RebuildList();
            }) { Tooltip = TazLang.Get("gridbands_delete_tooltip", "Delete this band") }));

            return row;
        }

        private void Move(int index, bool up)
        {
            List<GridContainerBand> bands = GridContainerBandsConfig.Current.Bands;
            int target = up ? index - 1 : index + 1;
            if (target < 0 || target >= bands.Count)
                return;

            (bands[index], bands[target]) = (bands[target], bands[index]);
            SaveAndRefresh();
            RebuildList();
        }

        private static void ApplyColorButtonStyle(MyraButton button, Color color)
        {
            var brush = new SolidBrush(color);
            button.Background = brush;
            button.OverBackground = brush;
            button.PressedBackground = brush;
            button.DisabledBackground = brush;
        }
    }

    /// <summary>Popup with a checkbox per item layer for editing a band's layer filter.</summary>
    internal class GridContainerBandLayerPicker : MyraControl
    {
        // Curated list of layers a container item can meaningfully carry.
        private static readonly Layer[] _layers =
        {
            Layer.OneHanded, Layer.TwoHanded, Layer.Shoes, Layer.Pants, Layer.Shirt, Layer.Helmet,
            Layer.Gloves, Layer.Ring, Layer.Talisman, Layer.Necklace, Layer.Waist, Layer.Torso,
            Layer.Bracelet, Layer.Tunic, Layer.Earrings, Layer.Arms, Layer.Cloak, Layer.Backpack,
            Layer.Robe, Layer.Skirt, Layer.Legs
        };

        private readonly int _bandIndex;

        private GridContainerBandLayerPicker(World world, int bandIndex) : base(TazLang.Get("gridbands_layers_title", "Band Layers"))
        {
            _bandIndex = bandIndex;
            Build();
            CenterInViewPort();
        }

        public static void Show(World world, int bandIndex)
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump is GridContainerBandLayerPicker w && !w.IsDisposed)
                {
                    w.Dispose();
                    break;
                }
            }

            UIManager.Add(new GridContainerBandLayerPicker(world, bandIndex));
        }

        private void Build()
        {
            List<GridContainerBand> bands = GridContainerBandsConfig.Current.Bands;
            if (_bandIndex < 0 || _bandIndex >= bands.Count)
            {
                Dispose();
                return;
            }

            GridContainerBand band = bands[_bandIndex];

            var root = new VerticalStackPanel { Spacing = 2 };
            root.Widgets.Add(new MyraLabel(TazLang.Get("gridbands_layers_desc", "Items on any checked layer belong to this band."), MyraLabel.TextStyle.P) { Width = 320 });

            // Two-column layout of checkboxes.
            var columns = new HorizontalStackPanel { Spacing = 12 };
            var left = new VerticalStackPanel { Spacing = 2 };
            var right = new VerticalStackPanel { Spacing = 2 };

            for (int i = 0; i < _layers.Length; i++)
            {
                Layer layer = _layers[i];
                var lyr = (byte)layer;
                bool isSet = band.Layers.Contains(lyr);

                MyraCheckButton cb = MyraCheckButton.CreateWithCallback(isSet, isChecked =>
                {
                    if (isChecked)
                    {
                        if (!band.Layers.Contains(lyr))
                            band.Layers.Add(lyr);
                    }
                    else
                    {
                        band.Layers.Remove(lyr);
                    }

                    GridContainerBandsMenu.SaveAndRefresh();
                }, text: layer.ToString());

                (i % 2 == 0 ? left : right).Widgets.Add(cb);
            }

            columns.Widgets.Add(left);
            columns.Widgets.Add(right);
            root.Widgets.Add(columns);

            root.Widgets.Add(new MyraButton(TazLang.Get("gridbands_clear", "Clear All"), () =>
            {
                band.Layers.Clear();
                GridContainerBandsMenu.SaveAndRefresh();
                // Rebuild to reflect cleared checkboxes.
                Defer(Build);
            }));

            SetRootContent(root);
        }
    }

    /// <summary>Popup with a multiline text box for editing a band's item-graphic filter.</summary>
    internal class GridContainerBandGraphicsEditor : MyraControl
    {
        private readonly int _bandIndex;

        private GridContainerBandGraphicsEditor(World world, int bandIndex) : base(TazLang.Get("gridbands_graphics_title", "Band Graphics"))
        {
            _bandIndex = bandIndex;
            Build();
            CenterInViewPort();
        }

        public static void Show(World world, int bandIndex)
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump is GridContainerBandGraphicsEditor w && !w.IsDisposed)
                {
                    w.Dispose();
                    break;
                }
            }

            UIManager.Add(new GridContainerBandGraphicsEditor(world, bandIndex));
        }

        private void Build()
        {
            List<GridContainerBand> bands = GridContainerBandsConfig.Current.Bands;
            if (_bandIndex < 0 || _bandIndex >= bands.Count)
            {
                Dispose();
                return;
            }

            GridContainerBand band = bands[_bandIndex];

            var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
            root.Widgets.Add(new MyraLabel(TazLang.Get("gridbands_graphics_desc", "One graphic per line. Accepts hex (0x1F03) or decimal. Items with these graphics belong to this band."), MyraLabel.TextStyle.P) { Width = 320 });

            var input = new MyraInputBox
            {
                Text = string.Join("\n", band.Graphics.Select(g => "0x" + g.ToString("X4"))),
                Width = 200,
                MinHeight = 260,
                Multiline = true,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            input.LostFocus = () =>
            {
                band.Graphics = ParseGraphics(input.Text);
                GridContainerBandsMenu.SaveAndRefresh();
            };

            root.Widgets.Add(new ScrollViewer { MaxHeight = 260, Content = input });

            SetRootContent(root);
        }

        private static List<ushort> ParseGraphics(string text)
        {
            var result = new List<ushort>();
            if (string.IsNullOrEmpty(text))
                return result;

            var seen = new HashSet<ushort>();

            foreach (string raw in text.Split(new[] { ',', '\n', '\r', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string token = raw.Trim();
                if (token.Length == 0)
                    continue;

                bool parsed;
                ushort value;

                if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    parsed = ushort.TryParse(token.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                }
                else
                {
                    parsed = ushort.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
                }

                if (parsed && seen.Add(value))
                    result.Add(value);
            }

            return result;
        }
    }
}
