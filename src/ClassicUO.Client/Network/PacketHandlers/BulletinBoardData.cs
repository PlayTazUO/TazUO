using System;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Utility;

namespace ClassicUO.Network.PacketHandlers;

internal static class BulletinBoardData
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
            return;

        switch (p.ReadUInt8())
        {
            case 0: // open

            {
                uint serial = p.ReadUInt32BE();
                Item item = world.Items.Get(serial);

                if (item != null)
                {
                    BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(
                        serial
                    );
                    bulletinBoard?.Dispose();

                    int x = (ScaleHelper.LogicalWindowWidth >> 1) - 245;
                    int y = (ScaleHelper.LogicalWindowHeight >> 1) - 205;

                    bulletinBoard = new BulletinBoardGump(world, item, x, y, p.ReadUTF8(22, true)); //p.ReadASCII(22));
                    UIManager.Add(bulletinBoard);

                    item.Opened = true;
                }
            }

                break;

            case 1: // summary msg

            {
                uint boardSerial = p.ReadUInt32BE();
                BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(
                    boardSerial
                );

                if (bulletinBoard != null)
                {
                    uint serial = p.ReadUInt32BE();
                    uint parendID = p.ReadUInt32BE();

                    // poster
                    int len = p.ReadUInt8();
                    string text = (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                    // subject
                    len = p.ReadUInt8();
                    text += (len <= 0 ? string.Empty : p.ReadUTF8(len, true)) + " - ";

                    // datetime
                    len = p.ReadUInt8();
                    text += len <= 0 ? string.Empty : p.ReadUTF8(len, true);

                    bulletinBoard.AddBulletinObject(serial, text);
                }
            }

                break;

            case 2: // message

            {
                uint boardSerial = p.ReadUInt32BE();
                BulletinBoardGump bulletinBoard = UIManager.GetGump<BulletinBoardGump>(
                    boardSerial
                );

                if (bulletinBoard != null)
                {
                    uint serial = p.ReadUInt32BE();

                    int len = p.ReadUInt8();
                    string poster = len > 0 ? p.ReadASCII(len) : string.Empty;

                    len = p.ReadUInt8();
                    string subject = len > 0 ? p.ReadUTF8(len, true) : string.Empty;

                    len = p.ReadUInt8();
                    string dataTime = len > 0 ? p.ReadASCII(len) : string.Empty;

                    p.Skip(4);

                    byte unk = p.ReadUInt8();

                    if (unk > 0)
                        p.Skip(unk * 4);

                    byte lines = p.ReadUInt8();

                    Span<char> span = stackalloc char[256];
                    var sb = new ValueStringBuilder(span);

                    for (int i = 0; i < lines; i++)
                    {
                        byte lineLen = p.ReadUInt8();

                        if (lineLen > 0)
                        {
                            string putta = p.ReadUTF8(lineLen, true);
                            sb.Append(putta);
                            sb.Append('\n');
                        }
                    }

                    string msg = sb.ToString();
                    byte variant = (byte)(1 + (poster == world.Player.Name ? 1 : 0));

                    UIManager.Add(
                        new BulletinBoardItem(
                            world,
                            boardSerial,
                            serial,
                            poster,
                            subject,
                            dataTime,
                            msg.TrimStart(),
                            variant
                        ) { X = 40, Y = 40 }
                    );

                    sb.Dispose();
                }
            }

                break;
        }
    }
}
