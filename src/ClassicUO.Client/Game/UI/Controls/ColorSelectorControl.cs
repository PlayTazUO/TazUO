using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using XnaMathHelper = Microsoft.Xna.Framework.MathHelper;

namespace ClassicUO.Game.UI.Controls
{
    public class ColorSelectorControl : Control
    {
        private const int SLIDER_WIDTH = 200;
        private const int SLIDER_HEIGHT = 20;
        private const int SLIDER_MARGIN = 5;
        private const int LABEL_WIDTH = 20;
        private const int PREVIEW_SIZE = 40;
        private const int MARGIN = 10;
        private const int INPUT_WIDTH = 60;
        private const int INPUT_HEIGHT = 20;
        private const int HEX_INPUT_WIDTH = 80;

        private int _red = 255;
        private int _green = 0;
        private int _blue = 0;
        private int _alpha = 255;

        private Color _selectedColor = Color.Red;
        private bool _isDraggingRed;
        private bool _isDraggingGreen;
        private bool _isDraggingBlue;
        private bool _isDraggingAlpha;

        private Rectangle _redSliderBounds;
        private Rectangle _greenSliderBounds;
        private Rectangle _blueSliderBounds;
        private Rectangle _alphaSliderBounds;
        private Rectangle _previewBounds;
        private Rectangle _redHandleBounds;
        private Rectangle _greenHandleBounds;
        private Rectangle _blueHandleBounds;
        private Rectangle _alphaHandleBounds;

        private StbTextBox _hexInput;
        private StbTextBox _redInput;
        private StbTextBox _greenInput;
        private StbTextBox _blueInput;
        private StbTextBox _alphaInput;

        private bool _updatingFromInputs;

        private RenderedText _redLabel;
        private RenderedText _greenLabel;
        private RenderedText _blueLabel;
        private RenderedText _alphaLabel;

        public event EventHandler<ColorChangedEventArgs> ColorChanged;

        public ColorSelectorControl(int x = 0, int y = 0)
        {
            X = x;
            Y = y;
            Width = LABEL_WIDTH + SLIDER_WIDTH + INPUT_WIDTH + MARGIN * 4;
            Height = (SLIDER_HEIGHT + SLIDER_MARGIN) * 4 + PREVIEW_SIZE + INPUT_HEIGHT + MARGIN * 4;

            AcceptMouseInput = true;

            int yOffset = MARGIN;
            _redSliderBounds = new Rectangle(LABEL_WIDTH + MARGIN, yOffset, SLIDER_WIDTH - INPUT_WIDTH - MARGIN, SLIDER_HEIGHT);
            yOffset += SLIDER_HEIGHT + SLIDER_MARGIN;
            _greenSliderBounds = new Rectangle(LABEL_WIDTH + MARGIN, yOffset, SLIDER_WIDTH - INPUT_WIDTH - MARGIN, SLIDER_HEIGHT);
            yOffset += SLIDER_HEIGHT + SLIDER_MARGIN;
            _blueSliderBounds = new Rectangle(LABEL_WIDTH + MARGIN, yOffset, SLIDER_WIDTH - INPUT_WIDTH - MARGIN, SLIDER_HEIGHT);
            yOffset += SLIDER_HEIGHT + SLIDER_MARGIN;
            _alphaSliderBounds = new Rectangle(LABEL_WIDTH + MARGIN, yOffset, SLIDER_WIDTH - INPUT_WIDTH - MARGIN, SLIDER_HEIGHT);
            yOffset += SLIDER_HEIGHT + SLIDER_MARGIN;
            _previewBounds = new Rectangle(LABEL_WIDTH + MARGIN, yOffset, SLIDER_WIDTH, PREVIEW_SIZE);
            yOffset += PREVIEW_SIZE + MARGIN;

            CreateInputFields(yOffset);
            CreateLabels();

            UpdateColor();
            UpdateHandles();
            UpdateInputFields();
        }

        private void CreateLabels()
        {
            _redLabel = RenderedText.Create("R", 0xFFFF, 0);
            _greenLabel = RenderedText.Create("G", 0xFFFF, 0);
            _blueLabel = RenderedText.Create("B", 0xFFFF, 0);
            _alphaLabel = RenderedText.Create("A", 0xFFFF, 0);
        }

        private void CreateInputFields(int yOffset)
        {
            _hexInput = new StbTextBox(1, 7, HEX_INPUT_WIDTH, true, FontStyle.None, 0xFFFF)
            {
                X = LABEL_WIDTH + MARGIN,
                Y = yOffset,
                Width = HEX_INPUT_WIDTH,
                Height = INPUT_HEIGHT,
                Text = $"#{_red:X2}{_green:X2}{_blue:X2}"
            };
            _hexInput.TextChanged += OnHexInputChanged;
            Add(_hexInput);

            int sliderEndX = LABEL_WIDTH + MARGIN + (SLIDER_WIDTH - INPUT_WIDTH - MARGIN);
            int inputX = sliderEndX + MARGIN;

            _redInput = new StbTextBox(1, 3, INPUT_WIDTH, true, FontStyle.None, 0xFFFF)
            {
                X = inputX,
                Y = MARGIN,
                Width = INPUT_WIDTH,
                Height = INPUT_HEIGHT,
                Text = _red.ToString()
            };
            _redInput.TextChanged += OnRedInputChanged;
            Add(_redInput);

            _greenInput = new StbTextBox(1, 3, INPUT_WIDTH, true, FontStyle.None, 0xFFFF)
            {
                X = inputX,
                Y = MARGIN + SLIDER_HEIGHT + SLIDER_MARGIN,
                Width = INPUT_WIDTH,
                Height = INPUT_HEIGHT,
                Text = _green.ToString()
            };
            _greenInput.TextChanged += OnGreenInputChanged;
            Add(_greenInput);

            _blueInput = new StbTextBox(1, 3, INPUT_WIDTH, true, FontStyle.None, 0xFFFF)
            {
                X = inputX,
                Y = MARGIN + (SLIDER_HEIGHT + SLIDER_MARGIN) * 2,
                Width = INPUT_WIDTH,
                Height = INPUT_HEIGHT,
                Text = _blue.ToString()
            };
            _blueInput.TextChanged += OnBlueInputChanged;
            Add(_blueInput);

            _alphaInput = new StbTextBox(1, 3, INPUT_WIDTH, true, FontStyle.None, 0xFFFF)
            {
                X = inputX,
                Y = MARGIN + (SLIDER_HEIGHT + SLIDER_MARGIN) * 3,
                Width = INPUT_WIDTH,
                Height = INPUT_HEIGHT,
                Text = _alpha.ToString()
            };
            _alphaInput.TextChanged += OnAlphaInputChanged;
            Add(_alphaInput);
        }

        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    _red = value.R;
                    _green = value.G;
                    _blue = value.B;
                    _alpha = value.A;
                    UpdateHandles();
                    UpdateInputFields();
                    ColorChanged?.Invoke(this, new ColorChangedEventArgs(value));
                }
            }
        }

        public int Red
        {
            get => _red;
            set
            {
                _red = (int)XnaMathHelper.Clamp(value, 0, 255);
                UpdateColor();
            }
        }

        public int Green
        {
            get => _green;
            set
            {
                _green = (int)XnaMathHelper.Clamp(value, 0, 255);
                UpdateColor();
            }
        }

        public int Blue
        {
            get => _blue;
            set
            {
                _blue = (int)XnaMathHelper.Clamp(value, 0, 255);
                UpdateColor();
            }
        }

        public int Alpha
        {
            get => _alpha;
            set
            {
                _alpha = (int)XnaMathHelper.Clamp(value, 0, 255);
                UpdateColor();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            DrawSliders(batcher, x, y);
            DrawPreview(batcher, x, y);
            DrawLabels(batcher, x, y);
            DrawHandles(batcher, x, y);

            return base.Draw(batcher, x, y);
        }

        private void DrawSliders(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            DrawSliderTrack(batcher, x + _redSliderBounds.X, y + _redSliderBounds.Y, _redSliderBounds.Width, _redSliderBounds.Height, Color.Red, hueVector);
            DrawSliderTrack(batcher, x + _greenSliderBounds.X, y + _greenSliderBounds.Y, _greenSliderBounds.Width, _greenSliderBounds.Height, Color.Green, hueVector);
            DrawSliderTrack(batcher, x + _blueSliderBounds.X, y + _blueSliderBounds.Y, _blueSliderBounds.Width, _blueSliderBounds.Height, Color.Blue, hueVector);
            DrawSliderTrack(batcher, x + _alphaSliderBounds.X, y + _alphaSliderBounds.Y, _alphaSliderBounds.Width, _alphaSliderBounds.Height, Color.White, hueVector);
        }

        private void DrawSliderTrack(UltimaBatcher2D batcher, int x, int y, int width, int height, Color baseColor, Vector3 hueVector)
        {
            var trackRect = new Rectangle(x, y, width, height);

            int steps = width / 2;
            for (int i = 0; i < steps; i++)
            {
                float intensity = i / (float)(steps - 1);
                Color gradientColor;

                if (baseColor == Color.Red)
                    gradientColor = new Color((int)(255 * intensity), _green, _blue, 255);
                else if (baseColor == Color.Green)
                    gradientColor = new Color(_red, (int)(255 * intensity), _blue, 255);
                else if (baseColor == Color.Blue)
                    gradientColor = new Color(_red, _green, (int)(255 * intensity), 255);
                else
                    gradientColor = new Color(_red, _green, _blue, (int)(255 * intensity));

                var stepRect = new Rectangle(x + (i * 2), y, 2, height);
                batcher.Draw(SolidColorTextureCache.GetTexture(gradientColor), stepRect, hueVector);
            }

            DrawRectangleBorder(batcher, trackRect, Color.Black, hueVector);
        }

        private void DrawPreview(UltimaBatcher2D batcher, int x, int y)
        {
            var previewRect = new Rectangle(
                x + _previewBounds.X,
                y + _previewBounds.Y,
                _previewBounds.Width,
                _previewBounds.Height
            );

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.Draw(
                SolidColorTextureCache.GetTexture(_selectedColor),
                previewRect,
                hueVector
            );

            DrawRectangleBorder(batcher, previewRect, Color.Black, hueVector);
        }

        private void DrawLabels(UltimaBatcher2D batcher, int x, int y)
        {
            int centerY = SLIDER_HEIGHT / 2;

            _redLabel?.Draw(batcher, x + MARGIN + 2, y + _redSliderBounds.Y + centerY - (_redLabel.Height / 2));
            _greenLabel?.Draw(batcher, x + MARGIN + 2, y + _greenSliderBounds.Y + centerY - (_greenLabel.Height / 2));
            _blueLabel?.Draw(batcher, x + MARGIN + 2, y + _blueSliderBounds.Y + centerY - (_blueLabel.Height / 2));
            _alphaLabel?.Draw(batcher, x + MARGIN + 2, y + _alphaSliderBounds.Y + centerY - (_alphaLabel.Height / 2));
        }

        private void DrawHandles(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            DrawSliderHandle(batcher, x + _redHandleBounds.X, y + _redHandleBounds.Y, hueVector);
            DrawSliderHandle(batcher, x + _greenHandleBounds.X, y + _greenHandleBounds.Y, hueVector);
            DrawSliderHandle(batcher, x + _blueHandleBounds.X, y + _blueHandleBounds.Y, hueVector);
            DrawSliderHandle(batcher, x + _alphaHandleBounds.X, y + _alphaHandleBounds.Y, hueVector);
        }

        private void DrawSliderHandle(UltimaBatcher2D batcher, int x, int y, Vector3 hueVector)
        {
            var handle = new Rectangle(x - 3, y - 2, 6, SLIDER_HEIGHT + 4);
            batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), handle, hueVector);
            DrawRectangleBorder(batcher, handle, Color.Black, hueVector);
        }


        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                var localPos = new Point(x, y);

                if (_redSliderBounds.Contains(localPos))
                {
                    _isDraggingRed = true;
                    UpdateRedFromPosition(x);
                }
                else if (_greenSliderBounds.Contains(localPos))
                {
                    _isDraggingGreen = true;
                    UpdateGreenFromPosition(x);
                }
                else if (_blueSliderBounds.Contains(localPos))
                {
                    _isDraggingBlue = true;
                    UpdateBlueFromPosition(x);
                }
                else if (_alphaSliderBounds.Contains(localPos))
                {
                    _isDraggingAlpha = true;
                    UpdateAlphaFromPosition(x);
                }
            }

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _isDraggingRed = false;
                _isDraggingGreen = false;
                _isDraggingBlue = false;
                _isDraggingAlpha = false;
            }

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_isDraggingRed)
            {
                UpdateRedFromPosition(x);
            }
            else if (_isDraggingGreen)
            {
                UpdateGreenFromPosition(x);
            }
            else if (_isDraggingBlue)
            {
                UpdateBlueFromPosition(x);
            }
            else if (_isDraggingAlpha)
            {
                UpdateAlphaFromPosition(x);
            }

            base.OnMouseOver(x, y);
        }

        private void UpdateRedFromPosition(int x)
        {
            float t = XnaMathHelper.Clamp((x - _redSliderBounds.X) / (float)_redSliderBounds.Width, 0f, 1f);
            _red = (int)(t * 255);
            UpdateColor();
        }

        private void UpdateGreenFromPosition(int x)
        {
            float t = XnaMathHelper.Clamp((x - _greenSliderBounds.X) / (float)_greenSliderBounds.Width, 0f, 1f);
            _green = (int)(t * 255);
            UpdateColor();
        }

        private void UpdateBlueFromPosition(int x)
        {
            float t = XnaMathHelper.Clamp((x - _blueSliderBounds.X) / (float)_blueSliderBounds.Width, 0f, 1f);
            _blue = (int)(t * 255);
            UpdateColor();
        }

        private void UpdateAlphaFromPosition(int x)
        {
            float t = XnaMathHelper.Clamp((x - _alphaSliderBounds.X) / (float)_alphaSliderBounds.Width, 0f, 1f);
            _alpha = (int)(t * 255);
            UpdateColor();
        }

        private void UpdateColor()
        {
            var newColor = new Color(_red, _green, _blue, _alpha);
            if (_selectedColor != newColor)
            {
                _selectedColor = newColor;
                UpdateHandles();
                UpdateInputFields();
                ColorChanged?.Invoke(this, new ColorChangedEventArgs(_selectedColor));
            }
        }

        private void UpdateHandles()
        {
            float redPercent = _red / 255f;
            float greenPercent = _green / 255f;
            float bluePercent = _blue / 255f;
            float alphaPercent = _alpha / 255f;

            _redHandleBounds.X = _redSliderBounds.X + (int)(redPercent * _redSliderBounds.Width);
            _redHandleBounds.Y = _redSliderBounds.Y;

            _greenHandleBounds.X = _greenSliderBounds.X + (int)(greenPercent * _greenSliderBounds.Width);
            _greenHandleBounds.Y = _greenSliderBounds.Y;

            _blueHandleBounds.X = _blueSliderBounds.X + (int)(bluePercent * _blueSliderBounds.Width);
            _blueHandleBounds.Y = _blueSliderBounds.Y;

            _alphaHandleBounds.X = _alphaSliderBounds.X + (int)(alphaPercent * _alphaSliderBounds.Width);
            _alphaHandleBounds.Y = _alphaSliderBounds.Y;
        }

        private void UpdateInputFields()
        {
            if (_updatingFromInputs) return;

            _updatingFromInputs = true;

            if (_hexInput != null)
                _hexInput.Text = $"#{_red:X2}{_green:X2}{_blue:X2}";

            if (_redInput != null)
                _redInput.Text = _red.ToString();

            if (_greenInput != null)
                _greenInput.Text = _green.ToString();

            if (_blueInput != null)
                _blueInput.Text = _blue.ToString();

            if (_alphaInput != null)
                _alphaInput.Text = _alpha.ToString();

            _updatingFromInputs = false;
        }

        private void OnHexInputChanged(object sender, EventArgs e)
        {
            if (_updatingFromInputs) return;

            string hex = _hexInput.Text.Trim();
            if (hex.StartsWith("#")) hex = hex.Substring(1);

            if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int colorValue))
            {
                _updatingFromInputs = true;
                _red = (colorValue >> 16) & 0xFF;
                _green = (colorValue >> 8) & 0xFF;
                _blue = colorValue & 0xFF;
                UpdateColor();
                _updatingFromInputs = false;
            }
        }

        private void OnRedInputChanged(object sender, EventArgs e)
        {
            if (_updatingFromInputs) return;

            if (int.TryParse(_redInput.Text, out int value) && value >= 0 && value <= 255)
            {
                _updatingFromInputs = true;
                _red = value;
                UpdateColor();
                _updatingFromInputs = false;
            }
        }

        private void OnGreenInputChanged(object sender, EventArgs e)
        {
            if (_updatingFromInputs) return;

            if (int.TryParse(_greenInput.Text, out int value) && value >= 0 && value <= 255)
            {
                _updatingFromInputs = true;
                _green = value;
                UpdateColor();
                _updatingFromInputs = false;
            }
        }

        private void OnBlueInputChanged(object sender, EventArgs e)
        {
            if (_updatingFromInputs) return;

            if (int.TryParse(_blueInput.Text, out int value) && value >= 0 && value <= 255)
            {
                _updatingFromInputs = true;
                _blue = value;
                UpdateColor();
                _updatingFromInputs = false;
            }
        }

        private void OnAlphaInputChanged(object sender, EventArgs e)
        {
            if (_updatingFromInputs) return;

            if (int.TryParse(_alphaInput.Text, out int value) && value >= 0 && value <= 255)
            {
                _updatingFromInputs = true;
                _alpha = value;
                UpdateColor();
                _updatingFromInputs = false;
            }
        }


        private void DrawRectangleBorder(UltimaBatcher2D batcher, Rectangle rect, Color color, Vector3 hueVector)
        {
            batcher.Draw(SolidColorTextureCache.GetTexture(color), new Rectangle(rect.X, rect.Y, rect.Width, 1), hueVector);
            batcher.Draw(SolidColorTextureCache.GetTexture(color), new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), hueVector);
            batcher.Draw(SolidColorTextureCache.GetTexture(color), new Rectangle(rect.X, rect.Y, 1, rect.Height), hueVector);
            batcher.Draw(SolidColorTextureCache.GetTexture(color), new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), hueVector);
        }

        public override void Dispose()
        {
            _hexInput?.Dispose();
            _redInput?.Dispose();
            _greenInput?.Dispose();
            _blueInput?.Dispose();
            _alphaInput?.Dispose();

            _redLabel?.Destroy();
            _greenLabel?.Destroy();
            _blueLabel?.Destroy();
            _alphaLabel?.Destroy();

            base.Dispose();
        }

    }

    public class ColorChangedEventArgs : EventArgs
    {
        public Color Color { get; }

        public ColorChangedEventArgs(Color color)
        {
            Color = color;
        }
    }
}
