// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    public class QuestionGump : Gump
    {
        private readonly Action<bool> _result;

        public QuestionGump(World world, string message, Action<bool> result) : base(world, 0, 0)
        {
            CanCloseWithRightClick = true;
            var ab = new AlphaBlendControl(0.15f) { Width = Client.Game.Window.ClientBounds.Width, Height = Client.Game.Window.ClientBounds.Height };
            Add(ab);

            Add(new GumpPic(0, 0, 0x0816, 0));

            ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0816);

            Width = gumpInfo.UV.Width;
            Height = gumpInfo.UV.Height;

            Add(new Label(message, false, 0x0386, 165, font: 1) { X = 33, Y = 30 });

            Add(
                new Button((int)Buttons.Cancel, 0x817, 0x818, 0x0819)
                {
                    X = 37,
                    Y = 75,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add(
                new Button((int)Buttons.Ok, 0x81A, 0x81B, 0x081C)
                {
                    X = 100,
                    Y = 75,
                    ButtonAction = ButtonAction.Activate
                }
            );

            CanMove = false;
            IsModal = true;

            CenterXInScreen();
            CenterYInScreen();

            ab.X = -X;
            ab.Y = -Y;

            WantUpdateSize = false;
            _result = result;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _result(false);
                    Dispose();

                    break;

                case 1:
                    _result(true);
                    Dispose();

                    break;
            }
        }

        private enum Buttons
        {
            Cancel,
            Ok
        }
    }
}
