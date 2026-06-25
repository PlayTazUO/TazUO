using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class Damage
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player == null)
            return;

        Entity entity = world.Get(p.ReadUInt32BE());

        if (entity != null)
        {
            ushort damage = p.ReadUInt16BE();

            if (damage > 0)
            {
                if (!MessageTypeFilter.IsEnabled(ProfileManager.CurrentProfile.DisabledOverheadMessageTypes, MessageType.Damage))
                    world.WorldTextManager.AddDamage(entity, damage);

                EventSink.InvokeOnEntityDamage(entity, damage);
            }
        }
    }
}
