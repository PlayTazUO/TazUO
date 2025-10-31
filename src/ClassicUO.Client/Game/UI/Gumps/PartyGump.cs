// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class PartyGump : NineSliceGump
    {
        private const int GUMP_WIDTH = 450;
        private const int GUMP_HEIGHT = 420;
        private SimpleProgressBar[] _healthBars = new SimpleProgressBar[10];

        public PartyGump(World world, int x, int y, bool canloot) : base(world, x, y, GUMP_WIDTH, GUMP_HEIGHT, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, true, GUMP_WIDTH, GUMP_HEIGHT)
        {
            CanLoot = canloot;

            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            BuildGump();
        }

        public bool CanLoot;

        public override void Update()
        {
            base.Update();

            // Update health bars for party members
            for (int i = 0; i < 10; i++)
            {
                if (_healthBars[i] != null && World.Party.Members[i] != null)
                {
                    if (World.Mobiles.TryGetValue(World.Party.Members[i].Serial, out GameObjects.Mobile mobile))
                    {
                        _healthBars[i].SetProgress(mobile.Hits, mobile.HitsMax);
                    }
                }
            }
        }


        protected override void UpdateContents()
        {
            Clear();
            // Clear health bar references when rebuilding
            for (int i = 0; i < 10; i++)
            {
                _healthBars[i] = null;
            }
            BuildGump();
        }

        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
            Clear();
            // Clear health bar references when rebuilding
            for (int i = 0; i < 10; i++)
            {
                _healthBars[i] = null;
            }
            BuildGump();
        }

        private void BuildGump()
        {
            // Modern UI panel background is provided by NineSliceGump base class

            // Calculate button dimensions - 70% of gump width
            int buttonWidth = (int)((Width - BorderSize * 2) * 0.7f);
            int buttonX = BorderSize + (Width - BorderSize * 2 - buttonWidth) / 2;

            // Calculate player name field width based on available space
            int nameFieldWidth = Width - BorderSize * 2 - 117; // Available space after Tell/Kick buttons

            // Calculate header positioning to align with centered party entries
            bool isLeader = World.Party.Leader == 0 || World.Party.Leader == World.Player;
            int entryAreaWidth = Width - BorderSize * 2;
            int buttonAreaWidth = isLeader ? 90 : 60; // Space for Msg + Kick or just Msg
            int nameAreaWidth = entryAreaWidth - buttonAreaWidth - 120; // Reserve 120 for health bar
            int entryStartX = BorderSize + (entryAreaWidth - (buttonAreaWidth + nameAreaWidth + 120)) / 2;

            // Header labels with TTF font and orange color aligned with buttons
            var msgLabel = TextBox.GetOne("Msg", TrueTypeLoader.EMBEDDED_FONT, 14, Color.Orange, TextBox.RTLOptions.Default());
            msgLabel.X = entryStartX;
            msgLabel.Y = BorderSize + 17;
            msgLabel.AcceptMouseInput = false;
            Add(msgLabel);

            if (isLeader)
            {
                var kickLabel = TextBox.GetOne(ResGumps.Kick, TrueTypeLoader.EMBEDDED_FONT, 14, Color.Orange, TextBox.RTLOptions.Default());
                kickLabel.X = entryStartX + 30; // Same spacing as buttons
                kickLabel.Y = BorderSize + 17;
                kickLabel.AcceptMouseInput = false;
                Add(kickLabel);
            }

            // Center Party Manifest label with larger font
            var partyManifestLabel = TextBox.GetOne(ResGumps.PartyManifest, TrueTypeLoader.EMBEDDED_FONT, 18, Color.Orange, TextBox.RTLOptions.Default());
            partyManifestLabel.AcceptMouseInput = false;
            Add(partyManifestLabel);
            // Center the label horizontally in the gump
            partyManifestLabel.X = BorderSize + (Width - BorderSize * 2 - partyManifestLabel.Width) / 2;
            partyManifestLabel.Y = BorderSize + 7;

            bool isMemeber = World.Party.Leader != 0 && World.Party.Leader != World.Player;

            int yPtr = BorderSize + 35;

            for (int i = 0; i < 10; i++)
            {
                int currentX = entryStartX;

                // Msg button
                Add
                (
                    new NiceButton(currentX, yPtr + 2, 25, 20, ButtonAction.Activate, "Msg")
                    {
                        ButtonParameter = (int)(Buttons.TellMember + i),
                        IsSelectable = false
                    }
                );
                currentX += 30;

                // Kick button (only for leaders)
                if (isLeader)
                {
                    Add
                    (
                        new NiceButton(currentX, yPtr + 2, 25, 20, ButtonAction.Activate, "Kick", hue: 34)
                        {
                            ButtonParameter = (int)(Buttons.KickMember + i),
                            IsSelectable = false
                        }
                    );
                    currentX += 30;
                }
                else
                {
                    currentX += 30; // Add space even when no kick button for alignment
                }

                // Name background
                Add(new AlphaBlendControl(0.2f)
                {
                    X = currentX,
                    Y = yPtr,
                    Width = nameAreaWidth,
                    Height = 23
                });

                string name = "";
                if (World.Party.Members[i] != null && World.Party.Members[i].Name != null)
                {
                    name = World.Party.Members[i].Name;
                }

                // Name label
                Add
                (
                    new Label
                    (
                        name,
                        false,
                        0xFFFF,
                        font: 0,
                        maxwidth: nameAreaWidth - 10,
                        align: TEXT_ALIGN_TYPE.TS_CENTER
                    )
                    {
                        X = currentX + 5,
                        Y = yPtr + 1
                    }
                );
                currentX += nameAreaWidth + 5;

                // Health bar (only if party member exists in World.Mobiles)
                if (World.Party.Members[i] != null)
                {
                    if (World.Mobiles.TryGetValue(World.Party.Members[i].Serial, out GameObjects.Mobile mobile))
                    {
                        _healthBars[i] = new SimpleProgressBar("#FF1010", "#0000FF", 110, 18)
                        {
                            X = currentX,
                            Y = yPtr + 3
                        };
                        _healthBars[i].SetProgress(mobile.Hits, mobile.HitsMax);
                        Add(_healthBars[i]);
                    }
                }

                yPtr += 25;
            }

            Add
            (
                new NiceButton(buttonX, BorderSize + 294, buttonWidth, 25, ButtonAction.Activate, ResGumps.SendThePartyAMessage)
                {
                    ButtonParameter = (int)Buttons.SendMessage,
                    IsSelectable = false
                }
            );

            string lootText = CanLoot ? ResGumps.PartyCanLootMe : ResGumps.PartyCannotLootMe;
            Add
            (
                new NiceButton(buttonX, BorderSize + 321, buttonWidth, 25, ButtonAction.Activate, lootText)
                {
                    ButtonParameter = (int)Buttons.LootType,
                    IsSelectable = false
                }
            );

            string leaveText = isMemeber ? ResGumps.LeaveTheParty : ResGumps.DisbandTheParty;
            Add
            (
                new NiceButton(buttonX, BorderSize + 347, buttonWidth, 25, ButtonAction.Activate, leaveText, hue: 32)
                {
                    ButtonParameter = (int)Buttons.Leave,
                    IsSelectable = false
                }
            );

            if (isLeader)
            {
                Add
                (
                    new NiceButton(buttonX, BorderSize + 372, buttonWidth, 25, ButtonAction.Activate, ResGumps.AddNewMember, hue: 66)
                    {
                        ButtonParameter = (int)Buttons.Add,
                        IsSelectable = false
                    }
                );
            }

            // OK and Cancel buttons removed - not needed
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {

                case Buttons.SendMessage:
                    if (World.Party.Leader == 0)
                    {
                        GameActions.Print
                        (
                            World,
                            ResGumps.YouAreNotInAParty,
                            0,
                            MessageType.System,
                            3,
                            false
                        );
                    }
                    else
                    {
                        if(!UIManager.SystemChat.IsActive)
                            UIManager.SystemChat.IsActive = true;
                        UIManager.SystemChat.TextBoxControl.ClearText();
                        UIManager.SystemChat.Mode = ChatMode.Default;
                        UIManager.SystemChat.TextBoxControl.SetText("/");
                    }

                    break;

                case Buttons.LootType:
                    CanLoot = !CanLoot;
                    if (World.Party.Leader != 0)
                    {
                        World.Party.CanLoot = CanLoot;
                        AsyncNetClient.Socket.Send_PartyChangeLootTypeRequest(CanLoot);
                    }
                    RequestUpdateContents();

                    break;

                case Buttons.Leave:
                    if (World.Party.Leader == 0)
                    {
                        GameActions.Print
                        (
                            World,
                            ResGumps.YouAreNotInAParty,
                            0,
                            MessageType.System,
                            3,
                            false
                        );
                    }
                    else
                    {
                        // NetClient.Socket.Send(new PPartyRemoveRequest(World.Player));
                        GameActions.RequestPartyQuit(World.Player);
                        //for (int i = 0; i < 10; i++)
                        //{
                        //    if (World.Party.Members[i] != null && World.Party.Members[i].Serial != 0)
                        //    {
                        //        NetClient.Socket.Send(new PPartyRemoveRequest(World.Party.Members[i].Serial));
                        //    }
                        //}
                    }

                    break;

                case Buttons.Add:
                    if (World.Party.Leader == 0 || World.Party.Leader == World.Player)
                    {
                        AsyncNetClient.Socket.Send_PartyInviteRequest();
                    }

                    break;

                default:
                    if (buttonID >= (int) Buttons.TellMember && buttonID < (int) Buttons.KickMember)
                    {
                        int index = (int) (buttonID - Buttons.TellMember);

                        if (World.Party.Members[index] == null || World.Party.Members[index].Serial == 0)
                        {
                            GameActions.Print
                            (
                                World,
                                ResGumps.ThereIsNoOneInThatPartySlot,
                                0,
                                MessageType.System,
                                3,
                                false
                            );
                        }
                        else
                        {
                            if(!UIManager.SystemChat.IsActive)
                                UIManager.SystemChat.IsActive = true;
                            UIManager.SystemChat.TextBoxControl.ClearText();
                            UIManager.SystemChat.Mode = ChatMode.Default;
                            UIManager.SystemChat.TextBoxControl.SetText($"/{index + 1} ");
                        }
                    }
                    else if (buttonID >= (int) Buttons.KickMember)
                    {
                        int index = (int) (buttonID - Buttons.KickMember);

                        if (World.Party.Members[index] == null || World.Party.Members[index].Serial == 0)
                        {
                            GameActions.Print
                            (
                                World,
                                ResGumps.ThereIsNoOneInThatPartySlot,
                                0,
                                MessageType.System,
                                3,
                                false
                            );
                        }
                        else
                        {
                            AsyncNetClient.Socket.Send_PartyRemoveRequest(World.Party.Members[index].Serial);
                        }
                    }

                    break;
            }
        }

        private enum Buttons
        {
            SendMessage,
            LootType,
            Leave,
            Add,
            TellMember,
            KickMember = TellMember + 10
        }
    }
}
