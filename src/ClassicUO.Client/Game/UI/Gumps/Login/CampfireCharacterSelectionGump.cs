using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.Login
{
    /// <summary>
    /// Diablo-style character selection: each character is rendered as an animated in-game mobile
    /// (playing its idle/stand animation and facing inward toward a central campfire), arranged in
    /// an arc/semicircle. Click a character to select, double-click to log in.
    /// Background artwork is a placeholder for now (see the TODO below).
    /// </summary>
    public class CampfireCharacterSelectionGump : CharacterSelectionGumpBase
    {
        // Facing directions so characters look inward toward the central fire.
        private const byte FACE_RIGHT = (byte)Direction.Right; // left-side characters face right
        private const byte FACE_LEFT = (byte)Direction.Left;  // right-side characters face left

        // Logical working area for the arc layout. Replace/align with the real campfire art later.
        private const int AREA_X = 100;
        private const int AREA_Y = 70;
        private const int AREA_W = 540;
        private const int AREA_H = 410;

        // Portrait box sized to roughly an actual mobile's footprint (characters render at native size).
        private const int PORTRAIT_W = 64;
        private const int PORTRAIT_H = 100;

        private const int MARGIN = 30;

        private readonly List<CampfirePortrait> _portraits = new();

        public CampfireCharacterSelectionGump(World world) : base(world)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            ResolveInitialSelection(loginScene);

            ExternalImageLoader.Instance.TryGetEmbeddedTexture("CharSelectBG.png", out Microsoft.Xna.Framework.Graphics.Texture2D upTexture);
            Add
            (
                new EmbeddedGumpPic(AREA_X, AREA_Y, upTexture),
                1
            );

            // World-viewport-style border framing the scene.
            Add
            (
                new BorderControl(AREA_X, AREA_Y, AREA_W, AREA_H, 4)
                {
                    AcceptMouseInput = false,
                    CanMove = false
                },
                1
            );

            float centerX = AREA_X + AREA_W / 2f;

            // Translucent backdrop so the title stays readable over the artwork.
            Add(new AlphaBlendControl(0.5f) { X = (int)(centerX - 80), 
                    Y = AREA_Y + 10, 
                    Width = 160, 
                    Height = 22, 
                    AcceptMouseInput = false,
                    BaseColor = Color.White }, 
                1);

            Add
            (
                new Label(TazLang.Get("characterselection"), true, 0, font: 1)
                {
                    X = (int)(centerX - 60),
                    Y = AREA_Y + 12
                },
                1
            );

            int btnY = AREA_Y + AREA_H - 40;

            List<CharSlot> slots = EnumerateValidCharacters(loginScene);
            int count = slots.Count;

            // Scale portraits down so even 7 fit side-by-side without overlap.
            int pw = count <= 4 ? PORTRAIT_W : Math.Max(48, (AREA_W - 2 * MARGIN) / count - 6);
            int ph = (int)(pw * (PORTRAIT_H / (float)PORTRAIT_W));

            int topMargin = AREA_Y + 34;
            int bottomLimit = btnY - 10;
            // Vertical rise from the arc's ends (lower, near the fire) up to its center (top).
            int arcHeight = Math.Clamp(bottomLimit - ph - topMargin, 60, 160);
            int endTopY = topMargin + arcHeight;
            float spanX = AREA_W - 2 * MARGIN - pw;

            for (int n = 0; n < count; n++)
            {
                CharSlot slot = slots[n];

                float tx = count == 1 ? 0.5f : n / (float)(count - 1);
                int px = AREA_X + MARGIN + (int)(tx * spanX);

                // Parabola peaking at the center (highest), ends dipping toward the fire.
                float curve = 1f - 4f * (tx - 0.5f) * (tx - 0.5f);
                int py = endTopY - (int)(curve * arcHeight);

                if ((px + pw / 2) == (int)centerX) //Top center
                    py -= 10;

                // Characters left of center face right, those on the right face left.
                // byte facing = (px + pw / 2) <= (int)centerX ? FACE_RIGHT : FACE_LEFT;
                // if ((px + pw / 2) == (int)centerX)
                //     facing = (byte)Direction.Down;

                byte facing = (byte)Direction.Down;

                WorldCharacterView view = slot.Lem.HasValue
                    ? new WorldCharacterView(World, slot.Lem.Value, facing, pw, ph, (uint)n)
                    : null;

                var portrait = new CampfirePortrait(slot.Index, slot.Name, view, pw, ph, SelectCharacter, LoginCharacter)
                {
                    X = px,
                    Y = py
                };

                portrait.SetSelected(slot.Index == _selectedCharacter);

                Add(portrait, 1);
                _portraits.Add(portrait);
                CharOrder.Add(slot.Index);
            }

            if (CanCreateChar(loginScene))
            {
                Add
                (
                    new Button((int)Buttons.New, 0x159D, 0x159F, 0x159E)
                    {
                        X = AREA_X + 30,
                        Y = btnY,
                        ButtonAction = ButtonAction.Activate
                    },
                    1
                );
            }

            Add
            (
                new Button((int)Buttons.Delete, 0x159A, 0x159C, 0x159B)
                {
                    X = AREA_X + 80,
                    Y = btnY,
                    ButtonAction = ButtonAction.Activate
                },
                1
            );

            Add
            (
                new Button((int)Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
                {
                    X = AREA_X + AREA_W - 90,
                    Y = btnY,
                    ButtonAction = ButtonAction.Activate
                },
                1
            );

            Add
            (
                new Button((int)Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
                {
                    X = AREA_X + AREA_W - 66,
                    Y = btnY,
                    ButtonAction = ButtonAction.Activate
                },
                1
            );

            // Live switch back to the classic list-style selection screen.
            Add
            (
                new NiceButton((int)(centerX - 60), btnY + 2, 120, 25, ButtonAction.Activate, TazLang.Get("classicview"))
                {
                    ButtonParameter = (int)Buttons.ToggleStyle,
                    IsSelectable = false,
                    DisplayBorder = true
                },
                1
            );

            ChangePage(1);
        }

        protected override void SelectCharacter(uint index)
        {
            base.SelectCharacter(index);

            foreach (CampfirePortrait portrait in _portraits)
            {
                portrait.SetSelected(portrait.CharacterIndex == index);
            }
        }

        private class CampfirePortrait : Control
        {
            private readonly Action<uint> _selectedFn;
            private readonly Action<uint> _loginFn;
            private readonly WorldCharacterView _view;
            private Label nameLabel;
            private AlphaBlendControl nameBg;
            private bool _selected;

            public CampfirePortrait(uint index, string name, WorldCharacterView view, int width, int height, Action<uint> selectedFn, Action<uint> loginFn)
            {
                CharacterIndex = index;
                _selectedFn = selectedFn;
                _loginFn = loginFn;
                _view = view;

                Width = width;
                Height = height + 18;

                if (view != null)
                {
                    // AcceptMouseInput=false on the view -> clicks fall through to this wrapper.
                    Add(view);
                }

                // Translucent backdrop so the name stays readable over the artwork.
                Add(nameBg = new AlphaBlendControl(0.5f) { X = 0, Y = height, AcceptMouseInput = false, BaseColor = Color.White });

                Add
                (
                    nameLabel = new Label(name, true, 0, 0, 5, align: TEXT_ALIGN_TYPE.TS_CENTER)
                    {
                        Y = height + 2,
                        AcceptMouseInput = false
                    }
                );

                nameLabel.ForceSizeUpdate();
                nameLabel.X = nameBg.X = ((view?.Width ?? width) - nameLabel.Width) / 2;
                nameBg.Width = nameLabel.Width;
                nameBg.Height = nameLabel.Height;

                AcceptMouseInput = true;
            }

            public uint CharacterIndex { get; }

            public void SetSelected(bool value)
            {
                _selected = value;
                _view?.SetSelected(value);

                nameBg.BaseColor = value ? Color.Gold : Color.White;
            }

            public override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    _selectedFn(CharacterIndex);
                }
            }

            public override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    _loginFn(CharacterIndex);

                    return true;
                }

                return false;
            }
        }
    }
}
