using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
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
    /// Myra-based editor for grid-container bands. Presents one tab per band group (corpses, backpack,
    /// other); each tab has an "enabled by default" toggle and a list of bands with per-row enable,
    /// rename, background-color, layer/graphic filter, reorder and delete actions. Bands are stored
    /// per-profile in <see cref="GridContainerBandsConfig"/>.
    /// </summary>
    internal class GridContainerBandsMenu : MyraControl
    {
        private readonly World _world;

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

        public override void Update()
        {
            // A band rename updates the config in memory on each keystroke but only persists on the
            // name box losing focus. Closing the window without blurring would otherwise drop the edit,
            // so flush the config once when a close/dispose has been requested.
            if (_disposeRequested && !IsDisposed)
                GridContainerBandsConfig.Current.Save();

            base.Update();
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
                TazLang.Get("gridbands_desc", "Bands group items in a grid container into sections by item layer and/or graphic. The first matching band wins; unmatched items are shown last. Each tab is a separate configuration applied to that kind of container."),
                MyraLabel.TextStyle.P) { Width = 480 });

            GridContainerBandsConfig config = GridContainerBandsConfig.Current;

            root.Widgets.Add(new LabeledIntegerInput(
                TazLang.Get("gridbands_padding", "Band padding (px)"),
                config.BandPadding,
                value =>
                {
                    config.BandPadding = value;
                    SaveAndRefresh();
                })
            {
                MinValue = 0,
                MaxValue = 200,
                InputBoxWidth = 60,
                Tooltip = TazLang.Get("gridbands_padding_tooltip", "Vertical gap in pixels inserted between bands.")
            });

            var tabs = new MyraTabControl();
            tabs.AddTab(TazLang.Get("gridbands_tab_corpses", "Corpses"), () => BuildGroupEditor(config.Corpses),
                TazLang.Get("gridbands_tab_corpses_tooltip", "Bands applied to corpses"));
            tabs.AddTab(TazLang.Get("gridbands_tab_backpack", "Backpack"), () => BuildGroupEditor(config.Backpack),
                TazLang.Get("gridbands_tab_backpack_tooltip", "Bands applied to your backpack"));
            tabs.AddTab(TazLang.Get("gridbands_tab_other", "Other"), () => BuildGroupEditor(config.Other),
                TazLang.Get("gridbands_tab_other_tooltip", "Bands applied to all other containers"));
            tabs.SelectFirst();

            root.Widgets.Add(tabs);

            SetRootContent(root);
        }

        /// <summary>Builds the editor panel for a single band group (used as a tab's content).</summary>
        private Widget BuildGroupEditor(GridContainerBandGroup group)
        {
            var panel = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
            var listPanel = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

            void Rebuild()
            {
                listPanel.Widgets.Clear();

                if (group.Bands.Count == 0)
                {
                    listPanel.Widgets.Add(new MyraLabel(TazLang.Get("gridbands_empty", "No bands configured yet."), MyraLabel.TextStyle.P));
                }
                else
                {
                    for (int i = 0; i < group.Bands.Count; i++)
                        listPanel.Widgets.Add(BuildRow(group, i, Rebuild));
                }

                ForceSizeUpdate();
            }

            panel.Widgets.Add(MyraCheckButton.CreateWithCallback(group.Enabled, isChecked =>
            {
                group.Enabled = isChecked;
                SaveAndRefresh();
            }, text: TazLang.Get("gridbands_group_enable", "Enabled by default"),
               tooltip: TazLang.Get("gridbands_group_enable_tooltip", "Use these bands for this kind of container (individual containers can still opt out)")));

            var toolbar = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            toolbar.Widgets.Add(new MyraButton(TazLang.Get("gridbands_add", "Add Band"), () =>
            {
                group.Bands.Add(new GridContainerBand { Name = TazLang.Get("gridbands_defaultname", "Band") + " " + (group.Bands.Count + 1) });
                GridContainerBandsConfig.Current.Save();
                Rebuild();
            }));
            panel.Widgets.Add(toolbar);

            Rebuild();
            panel.Widgets.Add(new ScrollViewer { MaxHeight = 360, Content = listPanel });

            return panel;
        }

        private Widget BuildRow(GridContainerBandGroup group, int index, Action rebuild)
        {
            GridContainerBand band = group.Bands[index];

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

            row.Widgets.Add(new MyraButton(TazLang.Get("gridbands_layers", "Layers"), () => GridContainerBandLayerPicker.Show(band))
            {
                Tooltip = TazLang.Get("gridbands_layers_tooltip", "Choose which item layers belong to this band")
            });

            row.Widgets.Add(new MyraButton(TazLang.Get("gridbands_graphics", "Graphics"), () => GridContainerBandGraphicsEditor.Show(_world, band))
            {
                Tooltip = TazLang.Get("gridbands_graphics_tooltip", "Choose which item graphics belong to this band")
            });

            row.Widgets.Add(new MyraButton(TazLang.Get("gridbands_up", "Up"), () => Move(group, index, true, rebuild))
            {
                Tooltip = TazLang.Get("gridbands_up_tooltip", "Move band up")
            });

            row.Widgets.Add(new MyraButton(TazLang.Get("gridbands_down", "Down"), () => Move(group, index, false, rebuild))
            {
                Tooltip = TazLang.Get("gridbands_down_tooltip", "Move band down")
            });

            row.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("X", () =>
            {
                // Close any open editors for this band so they don't keep mutating a detached instance.
                GridContainerBandLayerPicker.CloseFor(band);
                GridContainerBandGraphicsEditor.CloseFor(band);
                group.Bands.RemoveAt(index);
                SaveAndRefresh();
                rebuild();
            }) { Tooltip = TazLang.Get("gridbands_delete_tooltip", "Delete this band") }));

            return row;
        }

        private void Move(GridContainerBandGroup group, int index, bool up, Action rebuild)
        {
            int target = up ? index - 1 : index + 1;
            if (target < 0 || target >= group.Bands.Count)
                return;

            (group.Bands[index], group.Bands[target]) = (group.Bands[target], group.Bands[index]);
            SaveAndRefresh();
            rebuild();
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
        // Note: no World dependency is needed here (unlike the graphics editor, which targets items).
        // Curated list of layers a container item can meaningfully carry.
        private static readonly Layer[] _layers =
        {
            Layer.OneHanded, Layer.TwoHanded, Layer.Shoes, Layer.Pants, Layer.Shirt, Layer.Helmet,
            Layer.Gloves, Layer.Ring, Layer.Talisman, Layer.Necklace, Layer.Waist, Layer.Torso,
            Layer.Bracelet, Layer.Tunic, Layer.Earrings, Layer.Arms, Layer.Cloak, Layer.Backpack,
            Layer.Robe, Layer.Skirt, Layer.Legs
        };

        private readonly GridContainerBand _band;

        private GridContainerBandLayerPicker(GridContainerBand band) : base(TazLang.Get("gridbands_layers_title", "Band Layers"))
        {
            _band = band;
            Build();
            CenterInViewPort();
        }

        public static void Show(GridContainerBand band)
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump is GridContainerBandLayerPicker w && !w.IsDisposed)
                {
                    w.Dispose();
                    break;
                }
            }

            UIManager.Add(new GridContainerBandLayerPicker(band));
        }

        /// <summary>Closes an open layer picker if it is editing the given band.</summary>
        public static void CloseFor(GridContainerBand band)
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump is GridContainerBandLayerPicker w && !w.IsDisposed && ReferenceEquals(w._band, band))
                {
                    w.Dispose();
                    break;
                }
            }
        }

        private void Build()
        {
            if (_band == null)
            {
                Dispose();
                return;
            }

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
                bool isSet = _band.Layers.Contains(lyr);

                MyraCheckButton cb = MyraCheckButton.CreateWithCallback(isSet, isChecked =>
                {
                    if (isChecked)
                    {
                        if (!_band.Layers.Contains(lyr))
                            _band.Layers.Add(lyr);
                    }
                    else
                    {
                        _band.Layers.Remove(lyr);
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
                _band.Layers.Clear();
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
        private readonly World _world;
        private readonly GridContainerBand _band;

        private GridContainerBandGraphicsEditor(World world, GridContainerBand band) : base(TazLang.Get("gridbands_graphics_title", "Band Graphics"))
        {
            _world = world;
            _band = band;
            Build();
            CenterInViewPort();
        }

        public static void Show(World world, GridContainerBand band)
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump is GridContainerBandGraphicsEditor w && !w.IsDisposed)
                {
                    w.Dispose();
                    break;
                }
            }

            UIManager.Add(new GridContainerBandGraphicsEditor(world, band));
        }

        /// <summary>Closes an open graphics editor if it is editing the given band.</summary>
        public static void CloseFor(GridContainerBand band)
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump is GridContainerBandGraphicsEditor w && !w.IsDisposed && ReferenceEquals(w._band, band))
                {
                    w.Dispose();
                    break;
                }
            }
        }

        private void Build()
        {
            if (_band == null)
            {
                Dispose();
                return;
            }

            var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
            root.Widgets.Add(new MyraLabel(TazLang.Get("gridbands_graphics_desc", "One graphic per line. Accepts hex (0x1F03) or decimal. Add ';hue' to match a specific hue (e.g. 0x1F03;2); without a hue it matches any hue."), MyraLabel.TextStyle.P) { Width = 320 });

            var input = new MyraInputBox
            {
                Text = string.Join("\n", _band.Graphics.Select(FormatGraphic)),
                Width = 200,
                MinHeight = 260,
                Multiline = true,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            input.LostFocus = () =>
            {
                if (IsDisposed)
                    return;

                _band.Graphics = ParseGraphics(input.Text);
                GridContainerBandsMenu.SaveAndRefresh();
            };

            root.Widgets.Add(new ScrollViewer { MaxHeight = 260, Content = input });

            root.Widgets.Add(new MyraButton(TazLang.Get("gridbands_target", "Target Item"), () =>
            {
                // Commit any typed edits first so the targeted graphic is appended, not lost.
                _band.Graphics = ParseGraphics(input.Text);

                _world?.TargetManager.SetTargeting(o =>
                {
                    // The window may have been closed while the target cursor was up.
                    if (IsDisposed || o is not GameObject go || go.Graphic == 0)
                        return;

                    ushort graphic = go.Graphic;
                    // Capture the targeted item's hue so identically-graphic'd items of a
                    // different color don't match; hue 0 (no hue) is treated as "any".
                    int hue = go.Hue == 0 ? -1 : go.Hue;

                    if (!_band.Graphics.Any(g => g.Graphic == graphic && g.Hue == hue))
                    {
                        _band.Graphics.Add(new GridContainerBandGraphic { Graphic = graphic, Hue = hue });
                        GridContainerBandsMenu.SaveAndRefresh();
                    }

                    // Rebuild so the text box reflects the newly added graphic.
                    Defer(Build);
                });
            })
            {
                Tooltip = TazLang.Get("gridbands_target_tooltip", "Target an item in the world to add its graphic to this band")
            });

            SetRootContent(root);
        }

        private static string FormatGraphic(GridContainerBandGraphic g) =>
            g.Hue >= 0 ? $"0x{g.Graphic:X4};{g.Hue}" : $"0x{g.Graphic:X4}";

        private static List<GridContainerBandGraphic> ParseGraphics(string text)
        {
            var result = new List<GridContainerBandGraphic>();
            if (string.IsNullOrEmpty(text))
                return result;

            var seen = new HashSet<(ushort, int)>();

            // Lines are graphic[;hue]; only newlines separate entries so a hue can follow the ';'.
            foreach (string raw in text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = raw.Trim();
                if (line.Length == 0)
                    continue;

                string[] parts = line.Split(';');

                if (!TryParseNumber(parts[0], out int graphicValue) || graphicValue < 0 || graphicValue > ushort.MaxValue)
                    continue;

                int hue = -1;
                if (parts.Length > 1 && parts[1].Trim().Length > 0)
                {
                    if (!TryParseNumber(parts[1], out hue) || hue < 0 || hue > ushort.MaxValue)
                        hue = -1;
                }

                var graphic = (ushort)graphicValue;
                if (seen.Add((graphic, hue)))
                    result.Add(new GridContainerBandGraphic { Graphic = graphic, Hue = hue });
            }

            return result;
        }

        private static bool TryParseNumber(string token, out int value)
        {
            token = token.Trim();

            if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(token.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

            return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}
