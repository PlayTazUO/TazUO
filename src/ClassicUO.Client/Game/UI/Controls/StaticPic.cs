// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Controls
{
    public class StaticPic : Control
    {
        private ushort graphic;
        private Vector3 hueVector;
        private ushort hue;
        private bool isPartialHue;

        public StaticPic(ushort graphic, ushort hue)
        {
            Hue = hue;
            Graphic = graphic;
            CanMove = true;
            WantUpdateSize = false;
        }

        public StaticPic(List<string> parts)
            : this(
                UInt16Converter.Parse(parts[3]),
                parts.Count > 4 ? UInt16Converter.Parse(parts[4]) : (ushort)0
            )
        {
            X = int.Parse(parts[1]);
            Y = int.Parse(parts[2]);
            IsFromServer = true;
        }

        public ushort Hue
        {
            get => hue; set
            {
                hue = value;
                hueVector = ShaderHueTranslator.GetHueVector(value, IsPartialHue, 1);
            }
        }
        public bool IsPartialHue
        {
            get => isPartialHue; set
            {
                isPartialHue = value;
                hueVector = ShaderHueTranslator.GetHueVector(Hue, value, 1);
            }
        }

        public ushort Graphic
        {
            get => graphic;
            set
            {
                graphic = value;

                ref readonly SpriteInfo artInfo = ref Client.Game.UO.Arts.GetArt(value);

                if (artInfo.Texture == null)
                {
                    Dispose();

                    return;
                }

                Width = artInfo.UV.Width;
                Height = artInfo.UV.Height;

                IsPartialHue = Client.Game.UO.FileManager.TileData.StaticData[value].IsPartialHue;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (hueVector == default)
            {
                hueVector = ShaderHueTranslator.GetHueVector(Hue, IsPartialHue, 1);
            }

            ref readonly SpriteInfo artInfo = ref Client.Game.UO.Arts.GetArt(Graphic);

            if (artInfo.Texture != null)
            {
                batcher.Draw(
                    artInfo.Texture,
                    new Rectangle(x, y, Width, Height),
                    artInfo.UV,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y) => Client.Game.UO.Arts.PixelCheck(Graphic, x - Offset.X, y - Offset.Y);
    }
}
