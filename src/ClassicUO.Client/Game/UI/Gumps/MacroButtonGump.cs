// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    public class MacroButtonGump : AnchorableGump
    {
        private Texture2D backgroundTexture;
        private Vector3 hueVector;
        private ushort? _graphic;
        private ushort _hue;
        private float _scale;
        private bool _hideLabel;
        private Macro _macr;
        private readonly int DEFAULT_WIDTH = 88;
        private readonly int DEFAULT_HEIGHT = 44;
        private RenderedText _gText;

        public MacroButtonGump(World world, Macro macro, int x, int y) : this(world)
        {
            X = x;
            Y = y;
            Width = DEFAULT_WIDTH;
            Height = DEFAULT_HEIGHT;
            TheMacro = macro;

            BuildGump();
        }

        public MacroButtonGump(World world) : base(world,0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            WidthMultiplier = 2;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }

        public override GumpType GumpType => GumpType.MacroButton;

        public Macro TheMacro
        {
            get => _macr;
            set
            {
                _macr = value;
                Scale = value.Scale;
                Graphic = value.Graphic;
                Hue = value.Hue;
                HideLabel = value.HideLabel;
            }
        }
        public bool IsPartialHue { get; set; }
        public ushort Hue
        {
            get => _hue; set
            {
                _hue = value;
                hueVector = ShaderHueTranslator.GetHueVector(value);
            }
        }
        public bool HideLabel
        {
            get => _hideLabel;
            set
            {
                _hideLabel = value;
            }
        }
        public new float Scale
        {
            get => _scale;
            set
            {
                _scale = value;

                float factor = value / 100F;

                Width = (int)(Width * factor);
                Height = (int)(Height * factor);
                GroupMatrixHeight = Height;
                GroupMatrixWidth = Width;
                WidthMultiplier = 1;
            }
        }
        public ushort? Graphic
        {
            get => _graphic;
            set
            {
                _graphic = value;
                float factor = Scale / 100F;
                var _bounds = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);

                if (value.HasValue)
                {
                    ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(value.Value);
                    _bounds = texture.UV;
                    IsPartialHue = texture.Texture == null ? false : Client.Game.UO.FileManager.TileData.StaticData[value.Value].IsPartialHue;
                }

                Width = (int)(_bounds.Width * factor);
                Height = (int)(_bounds.Height * factor);

                GroupMatrixHeight = Height;
                GroupMatrixWidth = Width;
                WidthMultiplier = 1;
            }
        }

        private void BuildGump()
        {
            backgroundTexture = SolidColorTextureCache.GetTexture(new Color(30, 30, 30));
            _gText = RenderedText.Create
           (
               TheMacro.Name,
               0x03b2,
               255,
               true,
               FontStyle.BlackBorder,
               TEXT_ALIGN_TYPE.TS_CENTER,
               Width
           );
        }

        protected override void OnMouseEnter(int x, int y)
        {
            backgroundTexture = SolidColorTextureCache.GetTexture(Color.DimGray);
            base.OnMouseEnter(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            backgroundTexture = SolidColorTextureCache.GetTexture(new Color(30, 30, 30));
            base.OnMouseExit(x, y);
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, MouseButtonType.Left);

            Point offset = Mouse.LDragOffset;

            if (ProfileManager.CurrentProfile.CastSpellsByOneClick && button == MouseButtonType.Left && !Keyboard.Alt && Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
            {
                RunMacro();
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (ProfileManager.CurrentProfile.CastSpellsByOneClick || button != MouseButtonType.Left)
            {
                return false;
            }

            RunMacro();

            return true;
        }

        private void RunMacro()
        {
            if (TheMacro != null)
            {
                World.Macros.SetMacroToExecute(TheMacro.Items as MacroObject);
                World.Macros.WaitForTargetTimer = 0;
                World.Macros.Update();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible) return false;

            batcher.Draw
            (
                backgroundTexture,
                new Rectangle
                (
                    x,
                    y,
                    Width,
                    Height
                ),
                hueVector
            );

            if (Graphic.HasValue)
            {
                //var texture = GumpsLoader.Instance.GetGumpTexture(, out Rectangle bounds);
                ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(Graphic.Value);
                if (texture.Texture != null)
                {
                    var rect = new Rectangle(x, y, Width, Height);
                    batcher.Draw
                    (
                        texture.Texture,
                        rect,
                        texture.UV,
                        hueVector
                    );
                }
            }
            else
            {
                batcher.DrawRectangle
                    (
                        SolidColorTextureCache.GetTexture(Color.Gray),
                        x,
                        y,
                        Width,
                        Height,
                        hueVector
                    );
            }

            if (!HideLabel && _gText != null)
            {
                _gText.Hue = (ushort)(MouseIsOver ? 53 : 0x03b2);
                _gText.Draw(batcher, x, y + ((Height >> 1) - (_gText.Height >> 1)), Alpha);
            }


            base.Draw(batcher, x, y);

            return true;
        }

        public override void Save(XmlTextWriter writer)
        {
            if (TheMacro != null)
            {
                // hack to give macro buttons a unique id for use in anchor groups
                int macroid = World.Macros.GetAllMacros().IndexOf(TheMacro);

                LocalSerial = (uint)macroid + 1000;

                base.Save(writer);

                writer.WriteAttributeString("name", TheMacro.Name);
            }
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            Macro macro = World.Macros.FindMacro(xml.GetAttribute("name"));

            if (macro != null)
            {
                TheMacro = macro;
                BuildGump();
            }
        }
    }
}
