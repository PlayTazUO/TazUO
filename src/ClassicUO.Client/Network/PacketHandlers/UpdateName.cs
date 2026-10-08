using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class UpdateName
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
            return;

        uint serial = p.ReadUInt32BE();
        string name = p.ReadASCII();

        WMapEntity wme = world.WMapManager.GetEntity(serial);

        if (wme != null && wme.WaypointType == WaypointsType.None && !string.IsNullOrEmpty(name))
            wme.Name = name;

        Entity entity = world.Get(serial);

        if (entity != null)
        {
            entity.Name = name;

            if (
                serial == world.Player.Serial
                && !string.IsNullOrEmpty(name)
                && name != world.Player.Name
            )
            {
                Client.Game.SetWindowTitle(name);
                if (ProfileManager.CurrentProfile?.EnableTitleBarStats == true)
                    TitleBarStatsManager.ForceUpdate();
            }

            UIManager.GetGump<NameOverheadGump>(serial)?.SetName();
        }
    }
}
