// SPDX-License-Identifier: BSD-2-Clause

using System.Net;
using System.Net.NetworkInformation;
using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using SDL3;

namespace ClassicUO.Game.UI.Gumps.Login
{
    public class ServerSelectionGump : Gump
    {
        private const ushort SELECTED_COLOR = 0x0021;
        private const ushort NORMAL_COLOR = 0x034F;

        public ServerSelectionGump(World world) : base(world, 0, 0)
        {
            //AddChildren(new LoginBackground(true));

            Add
            (
                new Button((int) Buttons.Prev, 0x15A1, 0x15A3, 0x15A2)
                {
                    X = 586, Y = 445, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button((int) Buttons.Next, 0x15A4, 0x15A6, 0x15A5)
                {
                    X = 610, Y = 445, ButtonAction = ButtonAction.Activate
                }
            );

            if (Client.Game.UO.Version >= ClientVersion.CV_500A)
            {
                ushort textColor = 0xFFFF;

                Add
                (
                    new Label(Client.Game.UO.FileManager.Clilocs.GetString(1044579), true, textColor, font: 1)
                    {
                        X = 155, Y = 70
                    }
                ); // "Select which shard to play on:"

                if (CUOEnviroment.NoServerPing == false)
                {
                    Add
                    (
                        new Label(Client.Game.UO.FileManager.Clilocs.GetString(1044577), true, textColor, font: 1)
                        {
                            X = 400, Y = 70
                        }
                    ); // "Latency:"

                    Add
                    (
                        new Label(Client.Game.UO.FileManager.Clilocs.GetString(1044578), true, textColor, font: 1)
                        {
                            X = 470, Y = 70
                        }
                    ); // "Packet Loss:"
                }

                Add
                (
                    new Label(Client.Game.UO.FileManager.Clilocs.GetString(1044580), true, textColor, font: 1)
                    {
                        X = 153, Y = 368
                    }
                ); // "Sort by:"
            }
            else
            {
                ushort textColor = 0x0481;

                Add
                (
                    new Label(TazLang.Get("select_which_shard_to_play_on"), false, textColor, font: 9)
                    {
                        X = 155, Y = 70
                    }
                );

                Add
                (
                    new Label(TazLang.Get("latency"), false, textColor, font: 9)
                    {
                        X = 400, Y = 70
                    }
                );

                Add
                (
                    new Label(TazLang.Get("packet_loss"), false, textColor, font: 9)
                    {
                        X = 470, Y = 70
                    }
                );

                Add
                (
                    new Label(TazLang.Get("sort_by"), false, textColor, font: 9)
                    {
                        X = 153, Y = 368
                    }
                );
            }

            Add
            (
                new Button((int) Buttons.SortTimeZone, 0x093B, 0x093C, 0x093D)
                {
                    X = 230, Y = 366
                }
            );

            Add
            (
                new Button((int) Buttons.SortFull, 0x093E, 0x093F, 0x0940)
                {
                    X = 338, Y = 366
                }
            );

            Add
            (
                new Button((int) Buttons.SortConnection, 0x0941, 0x0942, 0x0943)
                {
                    X = 446, Y = 366
                }
            );

            // World Pic Bg
            Add(new GumpPic(150, 390, 0x0589, 0));

            // Earth
            Add
            (
                new Button((int) Buttons.Earth, 0x15E8, 0x15EA, 0x15E9)
                {
                    X = 160, Y = 400, ButtonAction = ButtonAction.Activate
                }
            );

            // Sever Scroll Area Bg
            Add
            (
                new ResizePic(0x0DAC)
                {
                    X = 150, Y = 90, Width = 393 - 14, Height = 271
                }
            );

            // Sever Scroll Area
            var scrollArea = new ScrollArea
            (
                150,
                90,
                393,
                271,
                true
            );

            var databox = new DataBox(0, 0, 1, 1);
            databox.WantUpdateSize = true;
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            scrollArea.ScissorRectangle.Y = 16;
            scrollArea.ScissorRectangle.Height = -32;

            int index = loginScene.GetServerIndexFromSettings();
            ServerListEntry selected = null;

            foreach (ServerListEntry server in loginScene.Servers)
            {
                databox.Add(new ServerEntryGump(server, 5, NORMAL_COLOR, SELECTED_COLOR));
                if(server.Index == index)
                    selected = server;
            }

            databox.ReArrangeChildren();

            Add(scrollArea);
            scrollArea.Add(databox);

            if (loginScene.Servers.Length != 0)
            {
                if (selected == null)
                    selected = loginScene.Servers[0];

                Add
                (
                    new Label(selected.Name, false, 0x0481, font: 9)
                    {
                        X = 243,
                        Y = 420
                    }
                );
            }

            AcceptKeyboardInput = true;
            CanCloseWithRightClick = false;
        }

        public override void OnButtonClick(int buttonID)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            if (buttonID >= (int) Buttons.Server)
            {
                int index = buttonID - (int) Buttons.Server;
                loginScene.SelectServer((byte) index);
            }
            else
            {
                switch ((Buttons) buttonID)
                {
                    case Buttons.Next:
                    case Buttons.Earth:

                        SelectServerFromSettings(loginScene);

                        break;

                    case Buttons.Prev:
                        loginScene.StepBack();

                        break;
                }
            }
        }

        protected override void OnControllerButtonUp(SDL.SDL_GamepadButton button)
        {
            base.OnControllerButtonUp(button);
            if (button == SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH)
            {
                LoginScene loginScene = Client.Game.GetScene<LoginScene>();

                SelectServerFromSettings(loginScene);
            }
        }

        public override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_RETURN || key == SDL.SDL_Keycode.SDLK_KP_ENTER)
            {
                LoginScene loginScene = Client.Game.GetScene<LoginScene>();

                SelectServerFromSettings(loginScene);
            }
        }

        private static void SelectServerFromSettings(LoginScene loginScene)
        {
            if (loginScene.Servers == null || loginScene.Servers.Length == 0)
                return;

            int index = loginScene.GetServerIndexFromSettings();

            // 'index' is a server Index value (not an array position) and may not
            // exist in the list, so resolve it to a valid server before selecting.
            ServerListEntry target = null;
            foreach (ServerListEntry s in loginScene.Servers)
                if (s.Index == index)
                {
                    target = s;
                    break;
                }

            target ??= loginScene.Servers[0];
            loginScene.SelectServer((byte)target.Index);
        }

        private enum Buttons
        {
            Prev,
            Next,
            SortTimeZone,
            SortFull,
            SortConnection,
            Earth,
            Server = 99
        }

        private class ServerEntryGump : Control
        {
            private readonly int _buttonId;
            private readonly ServerListEntry _entry;
            private readonly HoveredLabel _server_packet_loss;
            private readonly HoveredLabel _server_ping;
            private readonly HoveredLabel _serverName;
            private uint _pingCheckTime = 0;

            public ServerEntryGump(ServerListEntry entry, byte font, ushort normal_hue, ushort selected_hue)
            {
                _entry = entry;

                _buttonId = entry.Index;

                Add
                (
                    _serverName = new HoveredLabel
                    (
                        entry.Name,
                        false,
                        normal_hue,
                        selected_hue,
                        selected_hue,
                        font: font
                    )
                    {
                        X = 74,
                        AcceptMouseInput = false
                    }
                );

                Add
                (
                    _server_ping = new HoveredLabel
                    (
                        CUOEnviroment.NoServerPing ? string.Empty : "-",
                        false,
                        normal_hue,
                        selected_hue,
                        selected_hue,
                        font: font
                    )
                    {
                        X = 250,
                        AcceptMouseInput = false
                    }
                );

                Add
                (
                    _server_packet_loss = new HoveredLabel
                    (
                        CUOEnviroment.NoServerPing ? string.Empty : "-",
                        false,
                        normal_hue,
                        selected_hue,
                        selected_hue,
                        font: font
                    )
                    {
                        X = 320,
                        AcceptMouseInput = false
                    }
                );


                AcceptMouseInput = true;
                Width = 370;
                Height = 25;

                WantUpdateSize = false;
            }

            protected override void OnMouseEnter(int x, int y)
            {
                base.OnMouseEnter(x, y);

                _serverName.IsSelected = true;
                _server_packet_loss.IsSelected = true;
                _server_ping.IsSelected = true;
            }

            protected override void OnMouseExit(int x, int y)
            {
                base.OnMouseExit(x, y);

                _serverName.IsSelected = false;
                _server_packet_loss.IsSelected = false;
                _server_ping.IsSelected = false;
            }

            public override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    OnButtonClick((int) Buttons.Server + _buttonId);
                }
            }

            public override void Update()
            {
                base.Update();

                if (CUOEnviroment.NoServerPing == false && _pingCheckTime < Time.Ticks)
                {
                    _pingCheckTime = Time.Ticks + 2000;
                    _entry.DoPing();

                    switch (_entry.PingStatus)
                    {
                        case IPStatus.Success:
                            _server_ping.Text = _entry.Ping == -1 ? $"-" : _entry.Ping.ToString();

                            break;

                        case IPStatus.DestinationNetworkUnreachable:
                        case IPStatus.DestinationHostUnreachable:
                        case IPStatus.DestinationProtocolUnreachable:
                        case IPStatus.DestinationPortUnreachable:
                        case IPStatus.DestinationUnreachable:
                            _server_ping.Text = TazLang.Get("serverping_unreachable");

                            break;

                        case IPStatus.TimedOut:
                            _server_ping.Text = TazLang.Get("serverping_timeout");

                            break;

                        default:
                            _server_ping.Text = TazLang.Get("serverping_unknown", [((int) _entry.PingStatus).ToString()]);

                            break;
                    }

                    _server_packet_loss.Text = $"{_entry.PacketLoss}%";
                }
            }
        }
    }
}
