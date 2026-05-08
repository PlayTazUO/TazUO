using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.SpellVisualRange;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class UpdateHitpoints
{
    public static void Receive(World world, ref StackDataReader p)
    {
        Entity entity = world.Get(p.ReadUInt32BE());

        if (entity == null)
            return;

        ushort oldHits = entity.Hits;
        entity.HitsMax = p.ReadUInt16BE();
        entity.Hits = p.ReadUInt16BE();

        if (entity.HitsRequest == HitsRequestStatus.Pending)
            entity.HitsRequest = HitsRequestStatus.Received;

        if (entity == world.Player)
        {
            SpellVisualRangeManager.Instance.ClearCasting();
            TitleBarStatsManager.UpdateTitleBar();
        }

        // Check for bandage healing for all mobiles
        if (SerialHelper.IsMobile(entity.Serial) && oldHits != entity.Hits)
        {
            var mobile = entity as Mobile;
            if (mobile != null)
                BandageManager.Instance.OnMobileHpChanged(mobile, oldHits, entity.Hits);
        }
    }
}
