using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

[Flags]
internal enum AffixType
{
    Append = 0x00,
    Prepend = 0x01,
    System = 0x02
}

internal static class DisplayClilocString
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player == null)
            return;

        uint serial = p.ReadUInt32BE();
        Entity entity = world.Get(serial);
        ushort graphic = p.ReadUInt16BE();
        var type = (MessageType)p.ReadUInt8();
        ushort hue = p.ReadUInt16BE();
        ushort font = p.ReadUInt16BE();
        uint cliloc = p.ReadUInt32BE();
        AffixType flags = p[0] == 0xCC ? (AffixType)p.ReadUInt8() : 0x00;
        string name = p.ReadASCII(30);
        string affix = p[0] == 0xCC ? p.ReadASCII() : string.Empty;

        string arguments = null;

        SpellVisualRangeManager.Instance.OnClilocReceived((int)cliloc);

        if (cliloc == 1008092 ||
            cliloc == 1005445) // value for "You notify them you don't want to join the party" || "You have been added to the party"
            for (LinkedListNode<IGui> g = UIManager.Gumps.Last; g != null; g = g.Previous)
                if (g.Value is PartyInviteGump pg)
                    pg.Dispose();

        int remains = p.Remaining;

        if (remains > 0)
        {
            if (p[0] == 0xCC)
                arguments = p.ReadUnicodeBE(remains);
            else
                arguments = p.ReadUnicodeLE(remains / 2);
        }

        string text = Client.Game.UO.FileManager.Clilocs.Translate((int)cliloc, arguments);

        if (text == null)
            return;

        if (!string.IsNullOrWhiteSpace(affix))
        {
            if ((flags & AffixType.Prepend) != 0)
                text = $"{affix}{text}";
            else
                text = $"{text}{affix}";
        }

        if ((flags & AffixType.System) != 0)
            type = MessageType.System;

        if (!Client.Game.UO.FileManager.Fonts.UnicodeFontExists((byte)font))
            font = 0;

        TextType text_type = TextType.SYSTEM;

        if (
            serial == 0xFFFF_FFFF
            || serial == 0
            || (!string.IsNullOrEmpty(name)
                && string.Equals(name, "system", StringComparison.InvariantCultureIgnoreCase))
        )
        {
            // do nothing
        }
        else if (entity != null)
        {
            //entity.Graphic = graphic;
            text_type = TextType.OBJECT;

            if (string.IsNullOrEmpty(entity.Name))
                entity.Name = name;
        }

        EventSink.InvokeClilocMessageReceived(entity,
            new MessageEventArgs(entity, text, name, hue, type, (byte)font, text_type, true) { Cliloc = cliloc });

        world.MessageManager.HandleMessage(
            entity,
            text,
            name,
            hue,
            type,
            (byte)font,
            text_type,
            true
        );
    }
}
