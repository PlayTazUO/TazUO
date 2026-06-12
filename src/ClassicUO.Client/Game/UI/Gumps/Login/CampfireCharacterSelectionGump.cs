// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.Login
{
    /// <summary>
    /// Diablo-style character selection: the generated character paperdolls are arranged in an
    /// arc/semicircle around a central campfire. Click a portrait to select, double-click to log in.
    /// Background artwork is a placeholder for now (see the TODO below).
    /// </summary>
    public class CampfireCharacterSelectionGump : CharacterSelectionGumpBase
    {
        // Logical working area for the arc layout. Replace/align with the real campfire art later.
        private const int AREA_X = 100;
        private const int AREA_Y = 70;
        private const int AREA_W = 540;
        private const int AREA_H = 410;

        // Base (max) portrait size; scaled down automatically as the character count grows.
        private const int PORTRAIT_W = 96;
        private const int PORTRAIT_H = 150;

        private const int MARGIN = 30;

        private readonly List<CampfirePortrait> _portraits = new();

        public CampfireCharacterSelectionGump(World world) : base(world)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            ResolveInitialSelection(loginScene);

            // TODO: replace with the supplied campfire background art.
            Add
            (
                new ResizePic(0x0A28)
                {
                    X = AREA_X,
                    Y = AREA_Y,
                    Width = AREA_W,
                    Height = AREA_H
                },
                1
            );

            float centerX = AREA_X + AREA_W / 2f;

            Add
            (
                new Label("Character Selection", true, 0x0386, font: 1)
                {
                    X = (int)(centerX - 60),
                    Y = AREA_Y + 12
                },
                1
            );

            // Campfire placeholder marker, centered below the arc.
            Add
            (
                new Label("~ campfire ~", true, 0x0044, font: 1)
                {
                    X = (int)(centerX - 40),
                    Y = AREA_Y + (int)(AREA_H * 0.55f)
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

                StaticPaperDollView view = slot.Lem.HasValue
                    ? BuildPaperDoll(slot.Lem.Value, new Vector2(pw, ph), false)
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
                new NiceButton((int)(centerX - 60), btnY + 2, 120, 25, ButtonAction.Activate, "Classic View")
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
            private bool _selected;

            public CampfirePortrait(uint index, string name, StaticPaperDollView view, int width, int height, Action<uint> selectedFn, Action<uint> loginFn)
            {
                CharacterIndex = index;
                _selectedFn = selectedFn;
                _loginFn = loginFn;

                Width = width;
                Height = height + 18;

                if (view != null)
                {
                    // background=false -> draws only the body+equipment, falls through to this wrapper.
                    Add(view);
                }

                Add
                (
                    new Label(name, false, NORMAL_COLOR, Width, 5, align: TEXT_ALIGN_TYPE.TS_CENTER)
                    {
                        X = 0,
                        Y = height + 2,
                        AcceptMouseInput = false
                    }
                );

                AcceptMouseInput = true;
            }

            public uint CharacterIndex { get; }

            public void SetSelected(bool value) => _selected = value;

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (_selected)
                {
                    batcher.DrawRectangle
                    (
                        SolidColorTextureCache.GetTexture(Color.Gold),
                        x,
                        y,
                        Width,
                        Height,
                        ShaderHueTranslator.GetHueVector(0, false, 1f)
                    );
                }

                return base.Draw(batcher, x, y);
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
