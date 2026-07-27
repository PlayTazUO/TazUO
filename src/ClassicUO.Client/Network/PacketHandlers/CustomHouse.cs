using System;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Network.PacketHandlers.Helpers;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Network.PacketHandlers;

internal static class CustomHouse
{
    public static void Receive(World world, ref StackDataReader p)
    {
        bool compressed = p.ReadUInt8() == 0x03;
        bool enableResponse = p.ReadBool();
        uint serial = p.ReadUInt32BE();
        Item foundation = world.Items.Get(serial);
        uint revision = p.ReadUInt32BE();

        if (foundation == null)
            return;

        Rectangle? multi = foundation.MultiInfo;

        if (!foundation.IsMulti || multi == null)
            return;

        p.Skip(4);

        if (!world.HouseManager.TryGetHouse(foundation, out House house))
        {
            house = new House(world, foundation, revision, true);
            world.HouseManager.Add(foundation, house);
        }
        else
        {
            house.ClearComponents(true);
            house.Revision = revision;
            house.IsCustom = true;
        }

        short minX = (short)multi.Value.X;
        short minY = (short)multi.Value.Y;
        short maxY = (short)multi.Value.Height;

        if (minX == 0 && minY == 0 && maxY == 0 && multi.Value.Width == 0)
        {
            Log.Warn(
                "[CustomHouse (0xD8) - Invalid multi dimensions. Maybe missing some installation required files"
            );

            return;
        }

        byte planes = p.ReadUInt8();

        house.ClearCustomHouseComponents(0);

        for (int plane = 0; plane < planes; plane++)
        {
            uint header = p.ReadUInt32BE();
            int dlen = (int)(((header & 0xFF0000) >> 16) | ((header & 0xF0) << 4));
            int clen = (int)(((header & 0xFF00) >> 8) | ((header & 0x0F) << 8));
            int planeZ = (int)((header & 0x0F000000) >> 24);
            int planeMode = (int)((header & 0xF0000000) >> 28);

            if (clen <= 0)
                continue;

            try
            {
                HouseHelpers.ReadUnsafeCustomHouseData(
                    p.Buffer,
                    p.Position,
                    dlen,
                    clen,
                    planeZ,
                    planeMode,
                    minX,
                    minY,
                    maxY,
                    foundation,
                    house
                );
            }
            catch (Exception e)
            {
                Log.Error($"Failed to read custom house data: {e}");
            }

            p.Skip(clen);
        }

        if (world.CustomHouseManager != null)
        {
            world.CustomHouseManager.GenerateFloorPlace();

            UIManager.GetGump<HouseCustomizationGump>(house.Serial)?.Update();
        }

        UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();

        if (world.HouseManager.EntityIntoHouse(serial, world.Player))
            Client.Game.GetScene<GameScene>()?.UpdateMaxDrawZ(true);

        world.BoatMovingManager.ClearSteps(serial);

        // Custom house geometry just arrived/changed via packet. If the player is auto-walking a
        // path computed before these components existed, re-pathfind from the current position so
        // the walk can route around the newly-blocked tiles.
        world.Player?.Pathfinder?.RecalculatePath();
    }
}
