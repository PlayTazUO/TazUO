using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Utility;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Resources;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using static ClassicUO.Game.UI.Gumps.WorldMapGump;

namespace ClassicUO.Game.UI.Gumps
{
    public sealed class UserMarkersGump : MyraControl
    {
        private readonly MyraInputBox _textBoxX;
        private readonly MyraInputBox _textBoxY;
        private readonly MyraInputBox _markerName;

        private readonly string[] _colors;
        private int _selectedColorIndex;

        private readonly string[] _icons;
        private readonly bool _hasIcons;
        private int _selectedIconIndex;

        private readonly List<WMapMarker> _markers;
        private readonly int _markerIdx;
        private readonly int _mapIndex;

        private const int MAX_NAME_LEN = 25;

        private const int MAP_MIN_CORD = 0;
        private readonly int _mapMaxX;
        private readonly int _mapMaxY;

        private readonly string _userMarkersFilePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", $"{USER_MARKERS_FILE}.usr");

        public event EventHandler EditEnd;

        internal UserMarkersGump(World world, int x, int y, List<WMapMarker> markers, string color = "none", string icon = "exit", bool isEdit = false, int markerIdx = -1, int? mapIndex = null)
            : base(isEdit ? ResGumps.EditMarker : ResGumps.AddMarker)
        {
            _markers = markers;
            _markerIdx = markerIdx;

            // Persist against the map the marker actually belongs to: an existing marker keeps its
            // own MapId on edit, new markers use the explicitly displayed map (the world map can
            // free-view a different map than the player is currently on), falling back to the player's map.
            _mapIndex = isEdit && _markerIdx >= 0 ? _markers[_markerIdx].MapId : mapIndex ?? world.MapIndex;

            _mapMaxX = Client.Game.UO.FileManager.Maps.MapsDefaultSize[_mapIndex, 0];
            _mapMaxY = Client.Game.UO.FileManager.Maps.MapsDefaultSize[_mapIndex, 1];

            _colors = new[] { "none", "red", "green", "blue", "purple", "black", "yellow", "white" };
            _icons = _markerIcons.Keys.ToArray();

            string markerName = _markerIdx < 0 ? ResGumps.MarkerDefName : _markers[_markerIdx].Name;

            int selectedIcon = Array.IndexOf(_icons, icon);
            if (selectedIcon < 0)
                selectedIcon = 0;

            int selectedColor = isEdit ? Array.IndexOf(_colors, color) : (_icons.Length == 0 ? 1 : 0);
            if (selectedColor < 0)
                selectedColor = 0;

            _hasIcons = _icons.Length > 0;
            _selectedColorIndex = selectedColor;
            _selectedIconIndex = selectedIcon;

            var layout = new VerticalStackPanel { Spacing = 6, Padding = new Thickness(8) };

            // X Field
            layout.Widgets.Add(BuildLabeledRow(ResGumps.MarkerX, _textBoxX = new MyraInputBox
            {
                Text = x.ToString(),
                Width = 200,
                InputFilter = MyraInputBox.DigitInputFilter
            }));

            // Y Field
            layout.Widgets.Add(BuildLabeledRow(ResGumps.MarkerY, _textBoxY = new MyraInputBox
            {
                Text = y.ToString(),
                Width = 200,
                InputFilter = MyraInputBox.DigitInputFilter
            }));

            // Marker Name field
            layout.Widgets.Add(BuildLabeledRow(ResGumps.MarkerName, _markerName = new MyraInputBox
            {
                Text = markerName,
                Width = 200
            }));

            // Color Combobox
            layout.Widgets.Add(BuildLabeledRow(ResGumps.MarkerColor,
                BuildCombo(_colors, selectedColor, idx => _selectedColorIndex = idx)));

            // Icon combobox
            if (_hasIcons)
            {
                layout.Widgets.Add(BuildLabeledRow(ResGumps.MarkerIcon,
                    BuildCombo(_icons, selectedIcon, idx => _selectedIconIndex = idx)));
            }

            // Buttons Add/Edit and Cancel depend on state
            var btnRow = new HorizontalStackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
            btnRow.Widgets.Add(new MyraButton(isEdit ? ResGumps.Edit : ResGumps.CreateMarker, isEdit ? EditMarker : (Action)AddNewMarker));
            btnRow.Widgets.Add(new MyraButton(ResGumps.Cancel, Dispose));
            layout.Widgets.Add(btnRow);

            SetRootContent(layout);
            CenterInViewPort();
            UIManager.Add(this);
            BringOnTop();
        }

        private static HorizontalStackPanel BuildLabeledRow(string label, Widget widget)
        {
            var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            row.Widgets.Add(new MyraLabel(label, MyraLabel.TextStyle.P) { Width = 90 });
            row.Widgets.Add(widget);
            return row;
        }

        private static Widget BuildCombo(string[] items, int selectedIndex, Action<int> onChange)
        {
#pragma warning disable CS0612, CS0618
            var combo = new ComboBox { VerticalAlignment = VerticalAlignment.Center, Width = 200 };

            foreach (var item in items)
                combo.Items.Add(new ListItem(item));

            if (selectedIndex >= 0 && selectedIndex < items.Length)
                combo.SelectedIndex = selectedIndex;

            combo.SelectedIndexChanged += (_, _) =>
            {
                if (combo.SelectedIndex.HasValue)
                    onChange(combo.SelectedIndex.Value);
            };
#pragma warning restore CS0612, CS0618

            return combo;
        }

        private void EditMarker()
        {
            WMapMarker editedMarker = PrepareMarker();
            if (editedMarker == null)
                return;

            _markers[_markerIdx] = editedMarker;

            EditEnd.Raise(editedMarker);

            Dispose();
        }

        private void AddNewMarker()
        {
            if (!File.Exists(_userMarkersFilePath))
            {
                return;
            }

            WMapMarker newMarker = PrepareMarker();
            if (newMarker == null)
            {
                return;
            }

            string newLine = $"{newMarker.X},{newMarker.Y},{newMarker.MapId},{newMarker.Name},{newMarker.MarkerIconName},{newMarker.ColorName},4\r";

            File.AppendAllText(_userMarkersFilePath, newLine);

            _markers.Add(newMarker);

            Dispose();
        }

        private WMapMarker PrepareMarker()
        {
            if (!int.TryParse(_textBoxX.Text, out int x))
                return null;
            if (!int.TryParse(_textBoxY.Text, out int y))
                return null;

            // Validate User Enter Data
            if (x > _mapMaxX || x < MAP_MIN_CORD)
            {
                return null;
            }

            if (y > _mapMaxY || y < MAP_MIN_CORD)
            {
                return null;
            }

            string markerName = _markerName.Text;
            if (string.IsNullOrEmpty(markerName))
                return null;

            if (markerName.Length > MAX_NAME_LEN)
                markerName = markerName.Substring(0, MAX_NAME_LEN);

            if (markerName.Contains(","))
                markerName = markerName.Replace(",", "");

            int mapIdx = _mapIndex;
            string color = _colors[_selectedColorIndex];
            string icon = _hasIcons ? _icons[_selectedIconIndex] : string.Empty;

            var marker = new WMapMarker
            {
                Name = markerName,
                X = x,
                Y = y,
                MapId = mapIdx,
                ColorName = color,
                Color = GetColor(color),
            };

            if (!_markerIcons.TryGetValue(icon, out Microsoft.Xna.Framework.Graphics.Texture2D iconTexture))
            {
                return marker;
            }

            marker.MarkerIcon = iconTexture;
            marker.MarkerIconName = icon;

            return marker;
        }
    }
}
