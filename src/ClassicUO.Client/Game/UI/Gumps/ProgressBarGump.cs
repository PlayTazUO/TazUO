using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ProgressBarGump : Gump
    {
        public double MaxValue { get; set; } = 1;
        public double MinValue { get; set; } = 0;
        public double CurrentPercentage { get; set; }
        public Color ForegroundColor { get; set; } = Color.Blue;

        private Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 0.6f);
        public ProgressBarGump(string title, double startPercentage = 1.0, int width = 200, int height = 20) : base(0, 0)
        {
            CanCloseWithRightClick = true;
            AcceptMouseInput = false;

            Width = width;
            Height = height;
            CurrentPercentage = startPercentage;

            Add(new AlphaBlendControl() { Width = width, Height = height });

            if (!string.IsNullOrEmpty(title))
            {
                Add(TextBox.GetOne(title, TrueTypeLoader.EMBEDDED_FONT, 20, Color.White, TextBox.RTLOptions.DefaultCentered(width)));
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            // Clamp percentage to [0,1] to avoid negative/overflow widths
            var pct = CurrentPercentage < 0 ? 0 : (CurrentPercentage > 1 ? 1 : CurrentPercentage);
            if (double.IsNaN(pct)) pct = 0;
            batcher.Draw(
                SolidColorTextureCache.GetTexture(ForegroundColor),
                new Rectangle
                (
                    x,
                    y,
                    (int)(pct * Width),
                    Height
                ),
                hueVector);

            return true;
        }
    }
}
