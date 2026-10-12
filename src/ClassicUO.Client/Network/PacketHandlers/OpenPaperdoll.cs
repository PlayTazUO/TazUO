using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using Microsoft.Xna.Framework;

namespace ClassicUO.Network.PacketHandlers;

internal static class OpenPaperdoll
{
    public static void Receive(World world, ref StackDataReader p)
    {
        Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

        if (mobile == null)
            return;

        string text = p.ReadASCII(60);
        byte flags = p.ReadUInt8();

        mobile.Title = text;
        if (ProfileManager.CurrentProfile.UseModernPaperdoll && mobile.Serial == world.Player.Serial)
        {
            ModernPaperdoll modernPaperdoll = UIManager.GetGump<ModernPaperdoll>(mobile.Serial);
            if (modernPaperdoll != null)
            {
                modernPaperdoll.UpdateTitle(text);
                modernPaperdoll.SetInScreen();
                modernPaperdoll.BringOnTop();
            }
            else
                UIManager.Add(new ModernPaperdoll(world, mobile.Serial));
        }
        else
        {
            PaperDollGump paperdoll = UIManager.GetGump<PaperDollGump>(mobile);

            if (paperdoll == null)
            {
                if (!UIManager.GetGumpCachePosition(mobile, out Point location))
                    location = new Point(100, 100);

                UIManager.Add(
                    new PaperDollGump(world, mobile, (flags & 0x02) != 0) { Location = location }
                );
            }
            else
            {
                bool old = paperdoll.CanLift;
                bool newLift = (flags & 0x02) != 0;

                paperdoll.CanLift = newLift;
                paperdoll.UpdateTitle(text);

                if (old != newLift)
                    paperdoll.RequestUpdateContents();

                paperdoll.SetInScreen();
                paperdoll.BringOnTop();
            }
        }

        // Fetch the opened mobile's equipment properties so its durability can be tracked, even when
        // it is not the local player.
        GameActions.RequestEquippedOPL(world, mobile);
    }
}
