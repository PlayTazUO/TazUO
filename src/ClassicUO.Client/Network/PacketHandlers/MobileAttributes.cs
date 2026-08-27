using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class MobileAttributes
{
    public static void Receive(World world, ref StackDataReader p)
    {
        uint serial = p.ReadUInt32BE();

        Entity entity = world.Get(serial);

        if (entity == null)
            return;

        ushort oldHits = entity.Hits;
        entity.HitsMax = p.ReadUInt16BE();
        entity.Hits = p.ReadUInt16BE();

        if (entity.HitsRequest == HitsRequestStatus.Pending)
            entity.HitsRequest = HitsRequestStatus.Received;

        if (SerialHelper.IsMobile(serial))
        {
            var mobile = entity as Mobile;

            if (mobile == null)
                return;

            mobile.ManaMax = p.ReadUInt16BE();
            mobile.Mana = p.ReadUInt16BE();
            mobile.StaminaMax = p.ReadUInt16BE();
            mobile.Stamina = p.ReadUInt16BE();

            if (mobile == world.Player)
            {
                TitleBarStatsManager.UpdateTitleBar();
                EventSink.InvokePlayerStatsUpdated();
            }

            // Check for bandage healing
            if (oldHits != mobile.Hits)
                BandageManager.Instance.OnMobileHpChanged(mobile, oldHits, mobile.Hits);
        }
    }
}
