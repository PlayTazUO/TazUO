using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network.PacketHandlers;

internal static class DisplayWaypoint
{
    public static void Receive(World world, ref StackDataReader p)
    {
        uint serial = p.ReadUInt32BE();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        sbyte z = p.ReadInt8();
        byte map = p.ReadUInt8();
        var type = (WaypointsType)p.ReadUInt16BE();
        bool ignoreobject = p.ReadUInt16BE() != 0;
        uint cliloc = p.ReadUInt32BE();
        string name = p.ReadUnicodeLE();

        Log.Info($"Waypoint received: {type} - {name}");

        if (type == WaypointsType.Resurrection)
        {
            if (cliloc != 0)
            {
                string translated = Client.Game.UO.FileManager.Clilocs.Translate((int)cliloc, name ?? string.Empty);

                if (!string.IsNullOrEmpty(translated))
                {
                    name = translated;
                }
            }

            if (string.IsNullOrEmpty(name))
            {
                name = type.ToString();
            }
        }

        switch (type)
        {
            case WaypointsType.Corpse:
                world.WMapManager.AddOrUpdate(serial, x, y, 0, map, true, "Corpse");
                break;
            case WaypointsType.PartyMember:
                break;
            case WaypointsType.RallyPoint:
                break;
            case WaypointsType.QuestGiver:
                break;
            case WaypointsType.QuestDestination:
                break;
            case WaypointsType.Resurrection:
                world.WMapManager.AddOrUpdate(serial, x, y, 0, map, false, name, type: type);
                break;
            case WaypointsType.PointOfInterest:
                break;
            case WaypointsType.Landmark:
                break;
            case WaypointsType.Town:
                break;
            case WaypointsType.Dungeon:
                break;
            case WaypointsType.Moongate:
                break;
            case WaypointsType.Shop:
                break;
            case WaypointsType.Player:
                break;
        }
    }
}
