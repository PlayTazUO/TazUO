using System;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// A solid-color horizontal progress bar drawn from two XNA <see cref="Color"/>s. The background fills
    /// the control's full size; the foreground is sized by <see cref="UpdateProgress"/>. A centered label
    /// reports the current and maximum values over the bar.
    /// </summary>
    public class FlatProgressBar : Control
    {
        private const float LABEL_FONT_SIZE = 10;
        private readonly Color _background;
        private readonly Color _foreground;
        private readonly TextBox _label;
        private Vector3 _hueVector;
        private int _current;
        private int _max;

        /// <summary>
        /// Creates a progress bar with a fixed size and solid colors.
        /// </summary>
        /// <param name="width">Full width of the bar.</param>
        /// <param name="height">Height of the bar.</param>
        /// <param name="background">Color of the unfilled area.</param>
        /// <param name="foreground">Color of the filled area.</param>
        public FlatProgressBar(int width, int height, Color background, Color foreground)
        {
            Width = width;
            Height = height;
            _background = background;
            _foreground = foreground;
            AcceptMouseInput = false;
            WantUpdateSize = false;
            _hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

            _label = TextBox.GetOne(string.Empty, EmbeddedFontNames.ALAGARD, LABEL_FONT_SIZE, Color.White, new TextBox.RTLOptions { Width = width, Align = TextHorizontalAlignment.Center });
            _label.AcceptMouseInput = false;
            _label.Y = Math.Max(0, _label.Height >> 1);
            Add(_label);
        }

        public override void AlphaChanged(float oldValue, float newValue)
        {
            base.AlphaChanged(oldValue, newValue);
            _hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);
        }

        /// <summary>
        /// Sets the current and maximum values that drive the fill width and the label text.
        /// </summary>
        public void UpdateProgress(int current, int max)
        {
            _current = current;
            _max = max;
            _label.Text = $"{current} / {max}";
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(_background),
                new Rectangle(x, y, Width, Height),
                _hueVector,
                0
            );

            if (_max > 0 && _current > 0)
            {
                int fill = (int)(Width * Math.Clamp(_current, 0, _max) / (double)_max);

                if (fill > 0)
                {
                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(_foreground),
                        new Rectangle(x, y, fill, Height),
                        _hueVector,
                        0
                    );
                }
            }

            return base.Draw(batcher, x, y);
        }
    }
}
