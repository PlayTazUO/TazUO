using System;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class BuffDebuff
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player == null)
            return;

        const ushort BUFF_ICON_START = 0x03E9;
        const ushort BUFF_ICON_START_NEW = 0x466;

        uint serial = p.ReadUInt32BE();
        var ic = (BuffIconType)p.ReadUInt16BE();

        ushort iconID =
            (ushort)ic >= BUFF_ICON_START_NEW
                ? (ushort)(ic - (BUFF_ICON_START_NEW - 125))
                : (ushort)((ushort)ic - BUFF_ICON_START);

        if (iconID < BuffTable.Table.Length)
        {
            BuffGump gump = UIManager.GetGump<BuffGump>();
            ushort count = p.ReadUInt16BE();

            if (count == 0)
            {
                world.Player.RemoveBuff(ic);
                gump?.RequestUpdateContents();
            }
            else
                for (int i = 0; i < count; i++)
                {
                    ushort source_type = p.ReadUInt16BE();
                    p.Skip(2);
                    ushort icon = p.ReadUInt16BE();
                    ushort queue_index = p.ReadUInt16BE();
                    p.Skip(4);
                    ushort timer = p.ReadUInt16BE();
                    p.Skip(3);

                    uint titleCliloc = p.ReadUInt32BE();
                    uint descriptionCliloc = p.ReadUInt32BE();
                    uint wtfCliloc = p.ReadUInt32BE();

                    ushort arg_length = p.ReadUInt16BE();
                    string str = p.ReadUnicodeLE(2);
                    string args = str + p.ReadUnicodeLE();
                    string title = Client.Game.UO.FileManager.Clilocs.Translate(
                        (int)titleCliloc,
                        args,
                        true
                    ) ?? string.Empty;

                    arg_length = p.ReadUInt16BE();
                    string args_2 = p.ReadUnicodeLE();
                    string description = string.Empty;

                    if (descriptionCliloc != 0)
                    {
                        description =
                            "\n"
                            + Client.Game.UO.FileManager.Clilocs.Translate(
                                (int)descriptionCliloc,
                                String.IsNullOrEmpty(args_2) ? args : args_2,
                                true
                            );

                        if (description.Length < 2)
                            description = string.Empty;
                    }

                    arg_length = p.ReadUInt16BE();
                    string args_3 = p.ReadUnicodeLE();
                    string wtf = string.Empty;

                    if (wtfCliloc != 0)
                    {
                        wtf = Client.Game.UO.FileManager.Clilocs.Translate(
                            (int)wtfCliloc,
                            String.IsNullOrEmpty(args_3) ? args : args_3,
                            true
                        );

                        if (!string.IsNullOrWhiteSpace(wtf))
                            wtf = $"\n{wtf}";
                    }

                    string text = $"<left>{title}{description}{wtf}</left>";
                    bool alreadyExists = world.Player.IsBuffIconExists(ic);
                    world.Player.AddBuff(ic, BuffTable.Table[iconID], timer, text, title);

                    if (!alreadyExists)
                        gump?.RequestUpdateContents();
                }
        }
    }
}
