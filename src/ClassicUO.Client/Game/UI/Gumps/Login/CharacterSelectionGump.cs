// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps.Login
{
    public class CharacterSelectionGump : CharacterSelectionGumpBase
    {
        public CharacterSelectionGump(World world) : base(world)
        {
            int posInList = 0;
            int yOffset = 150;
            int yBonus = 0;
            int listTitleY = 106;

            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            if (Client.Game.UO.Version >= ClientVersion.CV_6040 || Client.Game.UO.Version >= ClientVersion.CV_5020 && loginScene.Characters.Length > 5)
            {
                listTitleY = 96;
                yOffset = 125;
                yBonus = 45;
            }

            ResolveInitialSelection(loginScene);

            Add
            (
                new ResizePic(0x0A28)
                {
                    X = 160,
                    Y = 70,
                    Width = 408,
                    Height = 343 + yBonus
                },
                1
            );

            bool isAsianLang = string.Compare(Settings.GlobalSettings.Language, "CHT", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "KOR", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(Settings.GlobalSettings.Language, "JPN", StringComparison.InvariantCultureIgnoreCase) == 0;

            bool unicode = isAsianLang;
            byte font = (byte)(isAsianLang ? 1 : 2);
            ushort hue = (ushort)(isAsianLang ? 0xFFFF : 0x0386);

            Add
            (
                new Label(Client.Game.UO.FileManager.Clilocs.GetString(3000050, "Character Selection"), unicode, hue, font: font)
                {
                    X = 267,
                    Y = listTitleY
                },
                1
            );

            foreach (CharSlot slot in EnumerateValidCharacters(loginScene))
            {
                CharacterEntryGump g;
                Add
                (g =
                    new CharacterEntryGump(slot.Index, slot.Name, SelectCharacter, LoginCharacter)
                    {
                        X = 224,
                        Y = yOffset + posInList * 40,
                        Hue = slot.Index == _selectedCharacter ? SELECTED_COLOR : NORMAL_COLOR
                    },
                    1
                );

                CharOrder.Add(slot.Index);

                if (slot.Lem.HasValue)
                {
                    g.SetTooltip(BuildPaperDoll(slot.Lem.Value, new Vector2(200, 300), true));
                }

                posInList++;
            }

            if (CanCreateChar(loginScene))
            {
                Add
                (
                    new Button((int)Buttons.New, 0x159D, 0x159F, 0x159E)
                    {
                        X = 224,
                        Y = 350 + yBonus,
                        ButtonAction = ButtonAction.Activate
                    },
                    1
                );
            }

            Add
            (
                new Button((int)Buttons.Delete, 0x159A, 0x159C, 0x159B)
                {
                    X = 442,
                    Y = 350 + yBonus,
                    ButtonAction = ButtonAction.Activate
                },
                1
            );

            Add
            (
                new Button((int)Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
                {
                    X = 586,
                    Y = 445,
                    ButtonAction = ButtonAction.Activate
                },
                1
            );

            Add
            (
                new Button((int)Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
                {
                    X = 610,
                    Y = 445,
                    ButtonAction = ButtonAction.Activate
                },
                1
            );

            // Live switch to the campfire-style selection screen.
            Add
            (
                new NiceButton(10, 445, 120, 25, ButtonAction.Activate, "Modern View")
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

            foreach (CharacterEntryGump characterGump in FindControls<CharacterEntryGump>())
            {
                characterGump.Hue = characterGump.CharacterIndex == index ? SELECTED_COLOR : NORMAL_COLOR;
            }
        }

        private class CharacterEntryGump : Control
        {
            private readonly Label _label;
            private readonly Action<uint> _loginFn;
            private readonly Action<uint> _selectedFn;

            public CharacterEntryGump(uint index, string character, Action<uint> selectedFn, Action<uint> loginFn)
            {
                CharacterIndex = index;
                _selectedFn = selectedFn;
                _loginFn = loginFn;

                // Bg
                Add
                (
                    new ResizePic(0x0BB8)
                    {
                        X = 0,
                        Y = 0,
                        Width = 280,
                        Height = 30,
                        AcceptMouseInput = false
                    }
                );

                // Char Name
                Add
                (
                    _label = new Label
                    (
                        character,
                        false,
                        NORMAL_COLOR,
                        270,
                        5,
                        align: TEXT_ALIGN_TYPE.TS_CENTER
                    )
                    {
                        X = 0,
                        AcceptMouseInput = false
                    }
                );

                AcceptMouseInput = true;
            }

            public uint CharacterIndex { get; }

            public ushort Hue
            {
                get => _label.Hue;
                set => _label.Hue = value;
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


            public override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    _selectedFn(CharacterIndex);
                }
            }
        }
    }
}
