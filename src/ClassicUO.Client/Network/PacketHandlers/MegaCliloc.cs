using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network.PacketHandlers;

internal static class MegaCliLoc
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
            return;

        ushort unknown = p.ReadUInt16BE();

        if (unknown > 1)
            return;

        uint serial = p.ReadUInt32BE();

        p.Skip(2);

        uint revision = p.ReadUInt32BE();

        Entity entity = world.Mobiles.Get(serial);

        if (entity == null)
        {
            if (SerialHelper.IsMobile(serial))
                Log.Warn("Searching a mobile into World.Items from MegaCliloc packet");

            entity = world.Items.Get(serial);
        }

        var list = new List<(int, string, string, int)>();
        int totalLength = 0;
        int totalEnglishLength = 0;

        while (p.Position < p.Length)
        {
            int cliloc = (int)p.ReadUInt32BE();

            if (cliloc == 0)
                break;

            ushort length = p.ReadUInt16BE();

            string argument = string.Empty;

            if (length != 0)
                argument = p.ReadUnicodeLE(length / 2);

            string str = Client.Game.UO.FileManager.Clilocs.Translate(cliloc, argument, true);
            string strEnglish = Client.Game.UO.FileManager.Clilocs.TranslateEnglish(cliloc, argument, true) ?? str;

            if (str == null)
                continue;

            int argcliloc = 0;

            string[] argcheck = argument.Split(
                new[] { '#' },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (argcheck.Length == 2)
                int.TryParse(argcheck[1], out argcliloc);

            // hardcoded colors lol
            switch (cliloc)
            {
                case 1080418:
                    if (Client.Game.UO.Version >= ClassicUO.Utility.ClientVersion.CV_60143)
                    {
                        str = "<basefont color=#40a4fe>" + str + "</basefont>";
                        strEnglish = "<basefont color=#40a4fe>" + strEnglish + "</basefont>";
                    }
                    break;
                case 1061170:
                    if (int.TryParse(argument, out int strength) && world.Player.Strength < strength)
                    {
                        str = "<basefont color=#FF0000>" + str + "</basefont>";
                        strEnglish = "<basefont color=#FF0000>" + strEnglish + "</basefont>";
                    }
                    break;
                case 1062613:
                    str = "<basefont color=#FFCC33>" + str + "</basefont>";
                    strEnglish = "<basefont color=#FFCC33>" + strEnglish + "</basefont>";
                    break;
                case 1159561:
                    str = "<basefont color=#b66dff>" + str + "</basefont>";
                    strEnglish = "<basefont color=#b66dff>" + strEnglish + "</basefont>";
                    break;
            }


            for (int i = 0; i < list.Count; i++)
                if (
                    list[i].Item1 == cliloc
                    && string.Equals(list[i].Item2, str, StringComparison.Ordinal)
                )
                {
                    list.RemoveAt(i);

                    break;
                }

            list.Add((cliloc, str, strEnglish, argcliloc));

            totalLength += str.Length;
            totalEnglishLength += strEnglish?.Length ?? 0;
        }

        Item container = null;

        if (entity is Item it && SerialHelper.IsValid(it.Container))
            container = world.Items.Get(it.Container);

        bool inBuyList = false;

        if (container != null)
            inBuyList =
                container.Layer == Layer.ShopBuy
                || container.Layer == Layer.ShopBuyRestock
                || container.Layer == Layer.ShopSell;

        bool first = true;

        string name = string.Empty;
        string data = string.Empty;
        string nameEnglish = string.Empty;
        string dataEnglish = string.Empty;
        int namecliloc = 0;

        if (list.Count != 0)
        {
            Span<char> span = stackalloc char[totalLength];
            var sb = new ValueStringBuilder(span);

            Span<char> spanEnglish = stackalloc char[totalEnglishLength];
            var sbEnglish = new ValueStringBuilder(spanEnglish);

            foreach ((int, string, string, int) s in list)
            {
                string str = s.Item2;
                string strEnglish = s.Item3;

                if (first)
                {
                    name = str;
                    nameEnglish = strEnglish;

                    if (entity != null && !SerialHelper.IsMobile(serial))
                    {
                        entity.Name = str;
                        namecliloc = s.Item4 > 0 ? s.Item4 : s.Item1;
                    }

                    first = false;
                }
                else
                {
                    if (sb.Length != 0)
                        sb.Append('\n');

                    sb.Append(str);

                    if (sbEnglish.Length != 0)
                        sbEnglish.Append('\n');

                    sbEnglish.Append(strEnglish);
                }
            }

            data = sb.ToString();
            dataEnglish = sbEnglish.ToString();

            sb.Dispose();
            sbEnglish.Dispose();
        }

        world.OPL.Add(serial, revision, name, data, namecliloc, nameEnglish, dataEnglish);

        if (inBuyList && container != null && SerialHelper.IsValid(container.Serial))
        {
            UIManager.GetGump<ShopGump>(container.RootContainer)?.SetNameTo((Item)entity, name);
            UIManager.GetGump<ModernShopGump>(container.RootContainer)?.SetNameTo((Item)entity, name);
        }
    }
}
