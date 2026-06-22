using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network.PacketHandlers;

internal static class UnicodeTalk
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
        {
            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                //Serial serial = p.ReadUInt32BE();
                //ushort graphic = p.ReadUInt16BE();
                //MessageType type = (MessageType)p.ReadUInt8();
                //Hue hue = p.ReadUInt16BE();
                //MessageFont font = (MessageFont)p.ReadUInt16BE();
                //string lang = p.ReadASCII(4);
                //string name = p.ReadASCII(30);
                Log.Warn("UnicodeTalk received during LoginScene");

                if (p.Length > 48)
                {
                    p.Seek(48);
                    Log.PushIndent();
                    Log.Warn("Handled UnicodeTalk in LoginScene");
                    Log.PopIndent();
                }
            }

            return;
        }

        uint serial = p.ReadUInt32BE();
        Entity entity = world.Get(serial);
        ushort graphic = p.ReadUInt16BE();
        var type = (MessageType)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        ushort font = p.ReadUInt16BE();
        string lang = p.ReadASCII(4);
        string name = p.ReadASCII();

        if (
            serial == 0
            && graphic == 0
            && type == MessageType.Regular
            && font == 0xFFFF
            && hue == 0xFFFF
            && name.ToLower() == "system"
        )
        {
            Span<byte> buffer =
                stackalloc byte[]
                {
                    0x03, 0x00, 0x28, 0x20, 0x00, 0x34, 0x00, 0x03, 0xdb, 0x13, 0x14, 0x3f, 0x45, 0x2c, 0x58, 0x0f,
                    0x5d, 0x44, 0x2e, 0x50, 0x11, 0xdf, 0x75, 0x5c, 0xe0, 0x3e, 0x71, 0x4f, 0x31, 0x34, 0x05, 0x4e,
                    0x18, 0x1e, 0x72, 0x0f, 0x59, 0xad, 0xf5, 0x00
                };

            AsyncNetClient.Socket.Send(buffer);

            return;
        }

        string text = string.Empty;

        if (p.Length > 48)
        {
            p.Seek(48);
            text = p.ReadUnicodeBE();
            // Convert Traditional Chinese speech to Simplified when CHS is selected.
            text = ClassicUO.Utility.ChineseConverter.ToSimplified(text);
        }

        TextType text_type = TextType.SYSTEM;

        if (type == MessageType.Alliance || type == MessageType.Guild)
            text_type = TextType.GUILD_ALLY;
        else if (
            type == MessageType.System
            || serial == 0xFFFF_FFFF
            || serial == 0
            || (name.ToLower() == "system" && entity == null)
        )
        {
            // do nothing
        }
        else if (entity != null)
        {
            text_type = TextType.OBJECT;

            if (string.IsNullOrEmpty(entity.Name))
                entity.Name = string.IsNullOrEmpty(name) ? text : name;
        }

        world.MessageManager.HandleMessage(
            entity,
            text,
            name,
            hue,
            type,
            ProfileManager.CurrentProfile.ChatFont,
            text_type,
            true,
            lang
        );
    }
}
