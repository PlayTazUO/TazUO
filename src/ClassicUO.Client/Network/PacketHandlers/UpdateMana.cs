using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class UpdateMana
{
    public static void Receive(World world, ref StackDataReader p)
    {
        Mobile mobile = world.Mobiles.Get(p.ReadUInt32BE());

        if (mobile == null)
            return;

        mobile.ManaMax = p.ReadUInt16BE();
        mobile.Mana = p.ReadUInt16BE();

        if (mobile == world.Player)
        {
            TitleBarStatsManager.UpdateTitleBar();
            EventSink.InvokePlayerStatsUpdated();
        }
    }
}
