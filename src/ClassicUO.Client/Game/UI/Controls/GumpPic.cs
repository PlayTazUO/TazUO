// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    public abstract class GumpPicBase : Control
    {
        public bool IsPartialHue { get; set; }
        private ushort _graphic;

        protected GumpPicBase()
        {
            CanMove = true;
            AcceptMouseInput = true;
        }

        public ushort Graphic
        {
            get => _graphic;
            set
            {
                _graphic = value;

                ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(_graphic);

                if (gumpInfo.Texture == null)
                {
                    Dispose();

                    return;
                }

                Width = gumpInfo.UV.Width;
                Height = gumpInfo.UV.Height;
            }
        }

        public ushort Hue { get; set; }

        public override bool Contains(int x, int y)
        {
            ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(_graphic);

            if (gumpInfo.Texture == null)
            {
                return false;
            }

            if (Client.Game.UO.Gumps.PixelCheck(Graphic, x - Offset.X, y - Offset.Y, InternalScale))
            {
                return true;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                Control c = Children[i];

                // might be wrong x, y. They should be calculated by position
                if (c.Contains(x, y))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class EmbeddedGumpPic : GumpPicBase
    {
        private Texture2D _customTexture;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="texture">Texture does not dispose, make sure you handle disposing elsewhere.</param>
        /// <param name="hue"></param>
        public EmbeddedGumpPic(int x, int y, Texture2D texture, ushort hue = 0)
        {
            X = x;
            Y = y;
            _customTexture = texture;
            Hue = hue;
            
            if(_customTexture != null){
                Width = _customTexture.Width;
                Height = _customTexture.Height;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || _customTexture == null || _customTexture.IsDisposed)
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

            batcher.Draw(
                _customTexture,
                new Rectangle(x, y, Width, Height),
                _customTexture.Bounds,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }
    }

    public class GumpPic : GumpPicBase
    {
        public GumpPic(int x, int y, ushort graphic, ushort hue)
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Hue = hue;
            IsFromServer = true;
        }

        public GumpPic(List<string> parts)
            : this(
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                UInt16Converter.Parse(parts[3]),
                (ushort)(
                    parts.Count > 4
                        ? TransformHue(
                            (ushort)(
                                UInt16Converter.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1))
                                + 1
                            )
                        )
                        : 0
                )
            )
        { }
        
        public bool ContainsByBounds { get; set; }

        public override bool Contains(int x, int y) => ContainsByBounds || base.Contains(x, y);

        private static ushort TransformHue(ushort hue)
        {
            if (hue <= 2)
            {
                hue = 0;
            }

            //if (hue < 2)
            //    hue = 1;
            return hue;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, IsPartialHue, Alpha, true);

            ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);

            if (gumpInfo.Texture != null)
            {
                batcher.Draw(
                    gumpInfo.Texture,
                    new Rectangle(x, y, (int)(Width * Scale), (int)(Height * Scale)),
                    gumpInfo.UV,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }
    }

    public class VirtueGumpPic : GumpPic
    {
        private readonly World _world;

        public VirtueGumpPic(World world, List<string> parts) : base(parts)
        {
            _world = world;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                AsyncNetClient.Socket.Send_VirtueGumpResponse(_world.Player, Graphic);

                return true;
            }

            return base.OnMouseDoubleClick(x, y, button);
        }
    }

    public class GumpPicInPic : GumpPicBase
    {
        private Rectangle _picInPicBounds;

        public Vector2 DrawOffset { get; set; } = Vector2.Zero;

        public Rectangle PicInPicBounds
        {
            get => _picInPicBounds; set
            {
                _picInPicBounds = value;
                Width = _picInPicBounds.Width;
                Height = _picInPicBounds.Height;
            }
        }

        public GumpPicInPic(
            int x,
            int y,
            ushort graphic,
            ushort sx,
            ushort sy,
            ushort width,
            ushort height
        )
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Width = width;
            Height = height;
            _picInPicBounds = new Rectangle(sx, sy, Width, Height);
            IsFromServer = true;
        }

        public GumpPicInPic(List<string> parts)
            : this(
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                UInt16Converter.Parse(parts[3]),
                UInt16Converter.Parse(parts[4]),
                UInt16Converter.Parse(parts[5]),
                UInt16Converter.Parse(parts[6]),
                UInt16Converter.Parse(parts[7])
            )
        { }

        public override bool Contains(int x, int y) => true;

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

            ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);

            var sourceBounds = new Rectangle(gumpInfo.UV.X + _picInPicBounds.X, gumpInfo.UV.Y + _picInPicBounds.Y, _picInPicBounds.Width, _picInPicBounds.Height);

            if (gumpInfo.Texture != null)
            {
                batcher.Draw(
                    gumpInfo.Texture,
                    new Rectangle((int)(x + DrawOffset.X), (int)(y + DrawOffset.Y), (int)(Width * Scale), (int)(Height * Scale)),
                    sourceBounds,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }
    }
}
