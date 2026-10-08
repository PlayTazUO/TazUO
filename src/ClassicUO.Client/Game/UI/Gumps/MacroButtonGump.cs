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
    /// <summary>
    ///     A macro's standalone on-screen button, drawn with one of two appearances depending on whether
    ///     the macro is running.
    /// </summary>
    /// <remarks>
    ///     The macro's appearance is cached when it is handed over rather than read per frame, so an edit
    ///     reaches this button only by assigning <see cref="TheMacro" /> again. Only the run state itself
    ///     is polled, since nothing signals a macro starting or stopping.
    /// </remarks>
    public sealed class MacroButtonGump : AnchorableGump
    {
        private Texture2D _backgroundTexture;
        private Vector3 _hueVector;
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

        private Macro _macr;
        private const int DEFAULT_WIDTH = 88;
        private const int DEFAULT_HEIGHT = 44;
        private RenderedText _gText;

        /// <summary>The label drawn while the macro is running, which may differ in text from <see cref="_gText" />.</summary>
        private RenderedText _gTextActive;

        /// <summary>The backing plate at rest. Hued at draw time.</summary>
        public static readonly Color PlateColor = new(30, 30, 30);

        /// <summary>The backing plate under the pointer, which is the button's whole hover response.</summary>
        public static readonly Color PlateHoverColor = Color.DimGray;

        /// <summary>Label hue and opacity per run state, resolved from the macro when it is handed over.</summary>
        private ushort _labelHue = Macro.DEFAULT_LABEL_HUE;

        private ushort _activeLabelHue = Macro.DEFAULT_LABEL_HUE;
        private float _labelOpacity = 1f;
        private float _activeLabelOpacity = 1f;

        public MacroButtonGump(World world, Macro macro, int x, int y) : this(world)
        {
            X = x;
            Y = y;
            Width = DEFAULT_WIDTH;
            Height = DEFAULT_HEIGHT;
            TheMacro = macro;
        }

        public MacroButtonGump(World world) : base(world,0, 0)
        {
            _backgroundTexture = SolidColorTextureCache.GetTexture(PlateColor);
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

                // A button whose macro was deleted keeps its size and plate and simply draws no label.
                // Save() already declines to persist one, so it lasts the session and no longer.
                if (value == null)
                {
                    DestroyLabels();
                    return;
                }

                Scale = value.Scale;
                Graphic = value.Graphic;
                Hue = value.Hue;
                _labelHue = value.LabelHueFor(false);
                _activeLabelHue = value.LabelHueFor(true);
                _labelOpacity = value.LabelOpacityFor(false) / (float)Macro.FULL_OPACITY;
                _activeLabelOpacity = value.LabelOpacityFor(true) / (float)Macro.FULL_OPACITY;
                _activeGraphic = value.GraphicFor(true);
                _activeHueVector = ShaderHueTranslator.GetHueVector(value.HueFor(true));

                // Last, because the renderings bake the label text and the width it wraps at, both of
                // which the assignments above settle. Re-handing the macro is how an edit reaches a
                // button already on screen, so the labels have to be rebuilt here, not only at build.
                RebuildLabels();
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
            get;
            set
            {
                field = value;
                _hueVector = ShaderHueTranslator.GetHueVector(value);
            }
        }

        public new float Scale
        {
            get;
            set
            {
                field = value;

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
                var bounds = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);

                if (value.HasValue)
                {
                    ref readonly SpriteInfo texture = ref Client.Game.UO.Gumps.GetGump(value.Value);
                    bounds = texture.UV;
                    IsPartialHue = texture.Texture != null && Client.Game.UO.FileManager.TileData.StaticData[value.Value].IsPartialHue;
                }

                Width = (int)(bounds.Width * factor);
                Height = (int)(bounds.Height * factor);

                GroupMatrixHeight = Height;
                GroupMatrixWidth = Width;
                WidthMultiplier = 1;
            }
        }

        /// <summary>
        ///     Renders both labels, one per run state.
        /// </summary>
        /// <remarks>
        ///     Reached on every macro assignment, so the previous renderings are returned to the pool
        ///     first; they are pooled objects, and dropping them leaks one per rebuild.
        /// </remarks>
        private void RebuildLabels()
        {
            DestroyLabels();

            _gText = CreateLabel(TheMacro.LabelFor(false), _labelHue);
            _gTextActive = CreateLabel(TheMacro.LabelFor(true), _activeLabelHue);
        }

        /// <summary>Renders one of the button's labels.</summary>
        /// <param name="text">The text to render. Empty renders nothing, which is how a hidden label is drawn.</param>
        /// <param name="hue">
        ///     The label's hue, baked into the rendering. A unicode <see cref="RenderedText" /> colours
        ///     its glyphs as it generates them and ignores its <c>Hue</c> afterwards, so changing the
        ///     hue means rebuilding, not assigning.
        /// </param>
        /// <returns>The rendering.</returns>
        private RenderedText CreateLabel(string text, ushort hue) => RenderedText.Create
        (
            text ?? string.Empty,
            hue,
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
            _backgroundTexture = SolidColorTextureCache.GetTexture(PlateHoverColor);
            base.OnMouseEnter(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            _backgroundTexture = SolidColorTextureCache.GetTexture(PlateColor);
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
            Vector3 stateHueVector = isActive ? _activeHueVector : _hueVector;

            batcher.Draw
            (
                _backgroundTexture,
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
                // Hue is already in the rendering; see CreateLabel. Hover is answered by the plate
                // alone, since recolouring the label under the pointer would hide the hue that is set.

                // Multiplied, not replaced: the gump's own alpha is the whole button fading.
                float labelAlpha = Alpha * (isActive ? _activeLabelOpacity : _labelOpacity);

                label.Draw(batcher, x, y + ((Height >> 1) - (label.Height >> 1)), labelAlpha);
            }


            base.Draw(batcher, x, y);

            return true;
        }

        public override void Save(XmlTextWriter writer)
        {
            if (TheMacro == null)
                return;

            // hack to give macro buttons a unique id for use in anchor groups
            int macroId = World.Macros.GetAllMacros().IndexOf(TheMacro);
            LocalSerial = (uint)macroId + 1000;
            base.Save(writer);
            writer.WriteAttributeString("name", TheMacro.Name);
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            Macro macro = World.Macros.FindMacro(xml.GetAttribute("name"));

            if (macro != null)
            {
                TheMacro = macro;
            }
        }
    }
}
