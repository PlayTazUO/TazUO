using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.UI.Gumps
{
    internal class RGBColorPickerGump : NineSliceGump
    {
        private const int WIDTH = 280, HEIGHT = 300;
        private ColorSelectorControl _colorSelector;
        private readonly Action<Color> _onColorSelected;

        private RGBColorPickerGump(Color initialColor, Action<Color> onColorSelected) : base(0, 0, WIDTH, HEIGHT, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, false)
        {
            _onColorSelected = onColorSelected;

            X = 100;
            Y = 100;
            Width = WIDTH;
            Height = HEIGHT;

            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            AcceptKeyboardInput = false;

            BuildGump(initialColor);
        }

        private void BuildGump(Color initialColor)
        {
            Clear();

            Add(new Label("Select Color", true, 0xFFFF, font: 1)
            {
                X = 10,
                Y = 10
            });

            Add(_colorSelector = new ColorSelectorControl(10, 40));
            _colorSelector.SelectedColor = initialColor;
            _colorSelector.ColorChanged += OnColorChanged;

            NiceButton okButton, cancelButton;

            Add(okButton = new NiceButton(WIDTH - 120, HEIGHT - 40, 50, 25, ButtonAction.Activate, "OK")
            {
                IsSelectable = false,
                ButtonParameter = 1
            });

            Add(cancelButton = new NiceButton(WIDTH - 60, HEIGHT - 40, 50, 25, ButtonAction.Activate, "Cancel")
            {
                IsSelectable = false,
                ButtonParameter = 2
            });

            okButton.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    _onColorSelected?.Invoke(_colorSelector.SelectedColor);
                    Dispose();
                }
            };

            cancelButton.MouseUp += (s, e) =>
            {
                if (e.Button == Input.MouseButtonType.Left)
                {
                    Dispose();
                }
            };
        }

        private void OnColorChanged(object sender, ColorChangedEventArgs e)
        {
            // Color preview updates automatically through the ColorSelectorControl
        }

        public static void Open(Color initialColor, Action<Color> onColorSelected)
        {
            UIManager.GetGump<RGBColorPickerGump>()?.Dispose();
            UIManager.Add(new RGBColorPickerGump(initialColor, onColorSelected));
        }
    }
}
