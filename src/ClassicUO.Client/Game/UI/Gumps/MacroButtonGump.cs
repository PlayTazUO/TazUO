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

        /// <summary>
        ///     Graphic drawn while the macro is running, already resolved through
        ///     <see cref="Macro.GraphicFor" /> so it equals <see cref="_graphic" /> when the macro has no
        ///     separate active graphic.
        /// </summary>
        /// <remarks>
        ///     Kept out of the <see cref="Graphic" /> setter deliberately: that setter resizes the gump and
        ///     its anchor-group cells, which must not happen per frame. The button therefore keeps the
        ///     resting graphic's size and stretches the active one into it, so switching state never moves
        ///     the button or reflows the group it is anchored in.
        /// </remarks>
        private ushort? _activeGraphic;

        /// <summary>Draw tint for the running state, resolved through <see cref="Macro.HueFor" />.</summary>
        private Vector3 _activeHueVector;

        private ushort _hue;
        private float _scale;
        private Macro _macr;
        private readonly int _defaultWidth = 88;
        private readonly int _defaultHeight = 44;
        private RenderedText _gText;

        /// <summary>The label drawn while the macro is running, which may differ in text from <see cref="_gText" />.</summary>
        private RenderedText _gTextActive;

        public MacroButtonGump(World world, Macro macro, int x, int y) : this(world)
        {
            X = x;
            Y = y;
            Width = _defaultWidth;
            Height = _defaultHeight;
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
                _activeGraphic = value.GraphicFor(true);
                _activeHueVector = ShaderHueTranslator.GetHueVector(value.HueFor(true));
            }
        }

        /// <summary>
        ///     Head of the macro's action chain, which is what <see cref="MacroManager.IsActive" />
        ///     identifies a run by.
        /// </summary>
        /// <remarks>Read live rather than cached: editing the macro's actions can replace the head node.</remarks>
        private MacroObject ActionHead => _macr?.Items as MacroObject;
        public bool IsPartialHue { get; set; }
        public ushort Hue
        {
            get => _hue; set
            {
                _hue = value;
                hueVector = ShaderHueTranslator.GetHueVector(value);
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
                var _bounds = new Rectangle(0, 0, _defaultWidth, _defaultHeight);

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

        /// <summary>
        ///     Builds the backing plate and both label renderings, one per run state.
        /// </summary>
        /// <remarks>
        ///     Reached again from <see cref="Restore" />, so the previous renderings are returned to the
        ///     pool first; they are pooled objects, and dropping them leaks one per restore.
        /// </remarks>
        private void BuildGump()
        {
            backgroundTexture = SolidColorTextureCache.GetTexture(new Color(30, 30, 30));

            DestroyLabels();

            _gText = CreateLabel(TheMacro.LabelFor(false));
            _gTextActive = CreateLabel(TheMacro.LabelFor(true));
        }

        /// <summary>Renders one of the button's labels.</summary>
        /// <param name="text">The text to render. Empty renders nothing, which is how a hidden label is drawn.</param>
        /// <returns>The rendering.</returns>
        private RenderedText CreateLabel(string text) => RenderedText.Create
        (
            text ?? string.Empty,
            0x03b2,
            255,
            true,
            FontStyle.BlackBorder,
            TEXT_ALIGN_TYPE.TS_CENTER,
            Width
        );

        /// <inheritdoc />
        /// <remarks>Hands the pooled label renderings back; nothing else reclaims them.</remarks>
        public override void Dispose()
        {
            DestroyLabels();
            base.Dispose();
        }

        /// <summary>Returns both label renderings to the pool. Idempotent.</summary>
        private void DestroyLabels()
        {
            _gText?.Destroy();
            _gTextActive?.Destroy();

            _gText = null;
            _gTextActive = null;
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


        public override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, MouseButtonType.Left);

            Point offset = Mouse.LDragOffset;

            if (ProfileManager.GlobalSettings.SingleClickIconUse && button == MouseButtonType.Left && !Keyboard.Alt && Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
            {
                RunMacro();
            }
        }

        public override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (ProfileManager.GlobalSettings.SingleClickIconUse || button != MouseButtonType.Left)
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

            // Resolved per frame rather than cached on a state change: nothing signals the macro
            // starting or stopping, and the test is two reference compares.
            bool isActive = World.Macros.IsActive(ActionHead);
            ushort? graphic = isActive ? _activeGraphic : Graphic;
            Vector3 stateHueVector = isActive ? _activeHueVector : hueVector;

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
                stateHueVector
            );

            if (graphic.HasValue)
            {
                ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(graphic.Value);
                if (texture.Texture != null)
                {
                    var rect = new Rectangle(x, y, Width, Height);
                    batcher.Draw
                    (
                        texture.Texture,
                        rect,
                        texture.UV,
                        stateHueVector
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
                        stateHueVector
                    );
            }

            // An empty label renders nothing, so "hidden" needs no branch of its own here.
            RenderedText label = isActive ? _gTextActive : _gText;

            if (label != null)
            {
                label.Hue = (ushort)(MouseIsOver ? 53 : 0x03b2);
                label.Draw(batcher, x, y + ((Height >> 1) - (label.Height >> 1)), Alpha);
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
