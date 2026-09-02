// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    public sealed class AlphaBlendControl : Control
    {
        private Vector3 hueVector;
        private ushort hue;

        public AlphaBlendControl(float alpha = 0.5f)
        {
            Alpha = alpha;
            AcceptMouseInput = false;
            hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha);
        }

        public ushort Hue
        {
            get => hue; set
            {
                hue = value;
                hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha);
            }
        }

        public Color BaseColor { get; set; } = Color.Black;

        public override void AlphaChanged(float oldValue, float newValue)
        {
            base.AlphaChanged(oldValue, newValue);
            hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(BaseColor),
                new Rectangle
                (
                    x,
                    y,
                    Width,
                    Height
                ),
                hueVector
            );

            return base.Draw(batcher, x, y);
        }
    }
}