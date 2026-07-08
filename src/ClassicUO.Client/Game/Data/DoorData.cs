// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    /// <summary>
    /// Helper for classifying door graphics as open or closed.
    ///
    /// Standard UO doors (regular doors, gates and secret doors) are laid out in blocks of
    /// 16 consecutive graphic IDs: 8 facings, each with a closed graphic immediately followed
    /// by its open graphic. For a block starting at <c>base</c>:
    ///   closed = base + (2 * facing)
    ///   open   = base + (2 * facing) + 1
    /// So within a block the closed graphics sit at even offsets and the open ones at odd
    /// offsets. This mirrors the layout used by RunUO/ServUO's BaseDoor derivatives.
    ///
    /// Detection is a hybrid: known block bases give an exact, verifiable anchor, and anything
    /// not in that list falls back to deriving the anchor dynamically from tiledata so new or
    /// custom door types are still handled.
    /// </summary>
    internal static class DoorData
    {
        private const int BLOCK_SIZE = 16;

        // Upper bound on the backward tiledata walk. The longest contiguous door run in
        // stock art is ~128 IDs (0x675..0x6F4); the cap just guards against runaway loops.
        private const int MAX_WALK = 256;

        // First (facing 0, closed) graphic of each known door block.
        private static readonly ushort[] _doorBaseGraphics =
        {
            0x00E8, // SecretStoneDoor1
            0x0314, // SecretDungeonDoor
            0x0324, // SecretStoneDoor2
            0x0334, // SecretWoodenDoor
            0x0344, // SecretLightWoodDoor
            0x0354, // SecretStoneDoor3
            0x0675, // MetalDoor
            0x0685, // BarredMetalDoor
            0x0695, // RattanDoor
            0x06A5, // DarkWoodDoor
            0x06B5, // MediumWoodDoor
            0x06C5, // MetalDoor2
            0x06D5, // LightWoodDoor
            0x06E5, // StrongWoodDoor
            0x0824, // IronGate
            0x0839, // LightWoodGate
            0x084C, // IronGateShort
            0x0866, // DarkWoodGate
            0x1FED, // BarredMetalDoor2
        };

        /// <summary>
        /// Returns true when <paramref name="graphic"/> is a door currently in the open
        /// position. Returns false for closed doors and for anything we can't classify (so
        /// callers safely fall back to their existing behavior).
        /// </summary>
        public static bool IsOpenDoor(ushort graphic)
        {
            // Known blocks: exact anchor, no dependency on tiledata layout.
            for (int i = 0; i < _doorBaseGraphics.Length; i++)
            {
                ushort baseGraphic = _doorBaseGraphics[i];

                if (graphic >= baseGraphic && graphic < baseGraphic + BLOCK_SIZE)
                {
                    return IsOpenOffset(graphic - baseGraphic);
                }
            }

            // Unknown door type: derive the anchor from tiledata. Walk back to the start of
            // the contiguous door run, then use parity. Because every door block is an even
            // number of IDs, the parity relative to the run start matches the parity relative
            // to the individual block base, so crossing packed blocks stays correct.
            return IsOpenDoorDynamic(graphic);
        }

        private static bool IsOpenDoorDynamic(ushort graphic)
        {
            var staticData = Client.Game.UO.FileManager.TileData.StaticData;

            if (graphic >= staticData.Length || !staticData[graphic].IsDoor)
            {
                return false;
            }

            int runStart = graphic;

            for (int steps = 0; steps < MAX_WALK && runStart > 0 && staticData[runStart - 1].IsDoor; steps++)
            {
                runStart--;
            }

            return IsOpenOffset(graphic - runStart);
        }

        // Odd offset from the block base is the open variant.
        private static bool IsOpenOffset(int offset) => (offset & 1) == 1;
    }
}
