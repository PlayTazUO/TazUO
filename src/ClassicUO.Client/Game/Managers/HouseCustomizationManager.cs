// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    public struct CustomBuildObject
    {
        public CustomBuildObject(ushort graphic)
        {
            Graphic = graphic;
            X = Y = Z = 0;
        }

        public ushort Graphic;
        public int X, Y, Z;
    }

    public sealed class HouseCustomizationManager
    {
        private const int FLOOR_HEIGHT = 20;

        public readonly List<CustomHouseWallCategory> Walls = new List<CustomHouseWallCategory>();
        public readonly List<CustomHouseFloor> Floors = new List<CustomHouseFloor>();
        public readonly List<CustomHouseDoor> Doors = new List<CustomHouseDoor>();
        public readonly List<CustomHouseMiscCategory> Miscs = new List<CustomHouseMiscCategory>();
        public readonly List<CustomHouseStair> Stairs = new List<CustomHouseStair>();
        public readonly List<CustomHouseTeleport> Teleports = new List<CustomHouseTeleport>();
        public readonly List<CustomHouseRoofCategory> Roofs = new List<CustomHouseRoofCategory>();
        public readonly List<CustomHousePlaceInfo> ObjectsInfo = new List<CustomHousePlaceInfo>();

        private readonly World _world;
        private Rectangle _bounds;

        public HouseCustomizationManager(World world, uint serial)
        {
            _world = world;
            Serial = serial;

            UOFileManager fileManager = Client.Game.UO.FileManager;
            // TODO: don't load the file txt every time the housemanager get initialized
            ParseFileWithCategory<CustomHouseWall, CustomHouseWallCategory>(Walls, fileManager.GetUOFilePath("walls.txt"));

            ParseFile(Floors, fileManager.GetUOFilePath("floors.txt"));
            ParseFile(Doors, fileManager.GetUOFilePath("doors.txt"));

            ParseFileWithCategory<CustomHouseMisc, CustomHouseMiscCategory>(Miscs, fileManager.GetUOFilePath("misc.txt"));

            ParseFile(Stairs, fileManager.GetUOFilePath("stairs.txt"));
            ParseFile(Teleports, fileManager.GetUOFilePath("teleprts.txt"));

            ParseFileWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(Roofs, fileManager.GetUOFilePath("roof.txt"));

            ParseFile(ObjectsInfo, fileManager.GetUOFilePath("suppinfo.txt"));
            //


            InitializeHouse();
        }

        public int Category = -1, MaxPage = 1, CurrentFloor = 1, FloorCount = 4, RoofZ = 1, MinHouseZ = -120, Components, Fixtures, MaxComponets, MaxFixtures;
        public bool Erasing, SeekTile, ShowWindow, CombinedStair;


        public readonly int[] FloorVisionState = new int[4];


        public ushort SelectedGraphic;

        public readonly uint Serial;
        public Point StartPos, EndPos;
        public CUSTOM_HOUSE_GUMP_STATE State = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;

        private void InitializeHouse()
        {
            Item foundation = _world.Items.Get(Serial);

            if (foundation == null)
            {
                return;
            }

            MinHouseZ = foundation.Z + 7;

            if (foundation.MultiInfo.HasValue)
            {
                Rectangle multi = foundation.MultiInfo.Value;

                StartPos.X = foundation.X + multi.X + 1;
                StartPos.Y = foundation.Y + multi.Y + 1;
                EndPos.X = foundation.X + multi.Width + 1;
                EndPos.Y = foundation.Y + multi.Height + 1;
            }

            int width = Math.Abs(EndPos.X - StartPos.X);
            int height = Math.Abs(EndPos.Y - StartPos.Y);

            _bounds = new Rectangle(StartPos.X - 1, StartPos.Y - 1, width + 1, height + 2);

            if (width >= 13 || height >= 13)
            {
                FloorCount = 4;
            }
            else
            {
                FloorCount = 3;
            }

            int plotWidth = width + 1;
            int plotHeight = height + 1;
            int componentsOnFloor = (plotWidth - 1) * (plotHeight - 1);

            MaxComponets = FloorCount * (componentsOnFloor + 2 * (plotWidth + plotHeight) - 4) - (int) (FloorCount * componentsOnFloor * -0.25) + 2 * plotWidth + 3 * plotHeight - 5;

            MaxFixtures = MaxComponets / FLOOR_HEIGHT;
        }

        /// <summary>
        /// Generates the visual state and validates the placement of all components in the custom house.
        /// This includes identifying floors, stairs, roofs, and fixtures, and updating their rendering state based on visibility settings.
        /// </summary>
        public void GenerateFloorPlace()
        {
            Item foundationItem = _world.Items.Get(Serial);

            if (foundationItem == null || !_world.HouseManager.TryGetHouse(Serial, out House house))
            {
                return;
            }

            // Clear previously generated internal components
            house.ClearCustomHouseComponents(CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL);

            foreach (Multi item in house.Components)
            {
                if (!item.IsCustom)
                {
                    continue;
                }

                int currentFloor = -1;
                int floorZ = foundationItem.Z + 7;
                int itemZ = item.Z;

                bool ignore = false;

                // Determine which floor the item belongs to
                for (int i = 0; i < 4; i++)
                {
                    int offset = 0 /*i != 0 ? 0 : 7*/;

                    if (itemZ >= floorZ - offset && itemZ < floorZ + FLOOR_HEIGHT)
                    {
                        currentFloor = i;

                        break;
                    }

                    floorZ += FLOOR_HEIGHT;
                }

                if (currentFloor == -1)
                {
                    ignore = true;
                    currentFloor = 0;
                    //continue;
                }

                (int floorCheck1, int floorCheck2) = SeekGraphicInCustomHouseObjectList(Floors, item.Graphic);

                CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state = item.State;

                // Identify if the item is a floor and update its vision state
                if (floorCheck1 != -1 && floorCheck2 != -1)
                {
                    state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR;

                    if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_FLOOR)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                    }
                    else if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_FLOOR
                             || FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSLUCENT_FLOOR)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                    }
                }
                else
                {
                    // Identify other component types (stairs, roofs, fixtures)
                    (int stairCheck1, int stairCheck2) = SeekGraphicInCustomHouseObjectList(Stairs, item.Graphic);

                    if (stairCheck1 != -1 && stairCheck2 != -1)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR;
                    }
                    else
                    {
                        (int roofCheck1, int roofCheck2) = SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(Roofs, item.Graphic);

                        if (roofCheck1 != -1 && roofCheck2 != -1)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF;
                        }
                        else
                        {
                            (int fixtureCheck1, int fixtureCheck2) = SeekGraphicInCustomHouseObjectList(Doors, item.Graphic);

                            if (fixtureCheck1 == -1 || fixtureCheck2 == -1)
                            {
                                (fixtureCheck1, fixtureCheck2) = SeekGraphicInCustomHouseObjectList(Teleports, item.Graphic);

                                if (fixtureCheck1 != -1 && fixtureCheck2 != -1)
                                {
                                    state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR;
                                }
                            }
                            else
                            {
                                state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE;
                            }
                        }
                    }

                    // Apply general vision states for non-floor content
                    if (!ignore)
                    {
                        if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_CONTENT)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                        }
                        else if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_CONTENT)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        }
                    }
                }

                if (!ignore)
                {
                    if (FloorVisionState[currentFloor] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_ALL)
                    {
                        state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                    }
                }

                item.State = state;
            }

            int z = foundationItem.Z + 7;

            for (int x = StartPos.X + 1; x < EndPos.X; x++)
            {
                for (int y = StartPos.Y + 1; y < EndPos.Y; y++)
                {
                    IEnumerable<Multi> multi = house.Components.Where(s => s.X == x && s.Y == y);

                    if (multi == null)
                    {
                        continue;
                    }

                    Multi floorMulti = null;
                    Multi floorCustomMulti = null;

                    foreach (Multi item in multi)
                    {
                        if (item.Z != z || (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) == 0)
                        {
                            continue;
                        }

                        if (item.IsCustom)
                        {
                            floorCustomMulti = item;
                        }
                        else
                        {
                            floorMulti = item;
                        }
                    }

                    if (floorMulti != null && floorCustomMulti == null)
                    {
                        Multi mo = house.Add
                        (
                            floorMulti.Graphic,
                            0,
                            (ushort) (foundationItem.X + (x - foundationItem.X)),
                            (ushort) (foundationItem.Y + (y - foundationItem.Y)),
                            (sbyte) z,
                            true,
                            false
                        );

                        mo.AlphaHue = 0xFF;

                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS state = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL;

                        if (FloorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_FLOOR)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER;
                        }
                        else if (FloorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_FLOOR
                                 || FloorVisionState[0] == (int) CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSLUCENT_FLOOR)
                        {
                            state |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        }

                        mo.State = state;
                    }
                }
            }

            var validatedFloors = new List<Point>();
            for (int i = 0; i < FloorCount; i++)
            {
                int minZ = foundationItem.Z + 7 + i * FLOOR_HEIGHT;
                int maxZ = minZ + FLOOR_HEIGHT;

                for (int j = 0; j < 2; j++)
                {
                    validatedFloors.Clear();

                    // First pass: validate items on each floor
                    for (int x = _bounds.X; x < EndPos.X + 1; x++)
                    {
                        for (int y = _bounds.Y; y < EndPos.Y + 1; y++)
                        {
                            IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                            if (multi == null)
                            {
                                continue;
                            }

                            foreach (Multi item in multi)
                            {
                                if (!item.IsCustom)
                                {
                                    continue;
                                }

                                if (j == 0)
                                {
                                    if (i == 0 && item.Z < minZ)
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;

                                        continue;
                                    }

                                    if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) == 0)
                                    {
                                        continue;
                                    }

                                    if (i == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;

                                        continue;
                                    }
                                }

                                if ((item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0 &&
                                    item.Z >= minZ && item.Z < maxZ)
                                {
                                    if (!ValidateItemPlace
                                        (
                                            foundationItem,
                                            item,
                                            minZ,
                                            maxZ,
                                            validatedFloors
                                        ))
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    }
                                    else
                                    {
                                        item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;
                                    }
                                }
                            }
                        }
                    }

                    if (i != 0 && j == 0)
                    {
                        // Re-validate floor items based on connectivity (validatedFloors)
                        foreach (Point point in validatedFloors)
                        {
                            IEnumerable<Multi> multi = house.GetMultiAt(point.X, point.Y);

                            if (multi == null)
                            {
                                continue;
                            }

                            foreach (Multi item in multi)
                            {
                                if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 && item.Z >= minZ && item.Z < maxZ)
                                {
                                    item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                }
                            }
                        }

                        // Fill in floor gaps for validation
                        for (int x = _bounds.X; x < EndPos.X + 1; x++)
                        {
                            int minY = 0, maxY = 0;

                            for (int y = _bounds.Y; y < EndPos.Y + 1; y++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        minY = y;

                                        break;
                                    }
                                }

                                if (minY != 0)
                                {
                                    break;
                                }
                            }

                            for (int y = EndPos.Y; y >= _bounds.Y; y--)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        maxY = y;

                                        break;
                                    }
                                }

                                if (maxY != 0)
                                {
                                    break;
                                }
                            }

                            for (int y = minY; y < maxY; y++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    }
                                }
                            }
                        }

                        for (int y = _bounds.Y; y < EndPos.Y + 1; y++)
                        {
                            int minX = 0;
                            int maxX = 0;

                            for (int x = _bounds.X; x < EndPos.X + 1; x++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        minX = x;

                                        break;
                                    }
                                }

                                if (minX != 0)
                                {
                                    break;
                                }
                            }

                            for (int x = EndPos.X; x >= _bounds.X; x--)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        maxX = x;

                                        break;
                                    }
                                }

                                if (maxX != 0)
                                {
                                    break;
                                }
                            }

                            for (int x = minX; x < maxX; x++)
                            {
                                IEnumerable<Multi> multi = house.GetMultiAt(x, y);

                                if (multi == null)
                                {
                                    continue;
                                }

                                foreach (Multi item in multi)
                                {
                                    if (item.IsCustom && (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                                        (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 && item.Z >= minZ && item.Z < maxZ)
                                    {
                                        item.State = item.State & ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    }
                                }
                            }
                        }
                    }
                }

                // After both validation passes, flood-fill propagate correctness
                // from walls with direct support to connected same-floor walls.
                // This fixes processing-order dependency in same-floor propagation.
                if (i > 0)
                {
                    var propagationQueue = new Queue<Multi>();

                    const CUSTOM_HOUSE_MULTI_OBJECT_FLAGS excludeMask =
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE |
                        CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL;

                    // Seed: all wall-type items on this floor that are validated and correct.
                    for (int x = _bounds.X; x < EndPos.X + 1; x++)
                    {
                        for (int y = _bounds.Y; y < EndPos.Y + 1; y++)
                        {
                            foreach (Multi item in house.GetMultiAt(x, y))
                            {
                                if (item.IsCustom &&
                                    item.Z >= minZ && item.Z < maxZ &&
                                    (item.State & excludeMask) == 0 &&
                                    (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                    (item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                                {
                                    propagationQueue.Enqueue(item);
                                }
                            }
                        }
                    }

                    int[] pdx = { -1, 1, 0, 0 };
                    int[] pdy = { 0, 0, -1, 1 };

                    while (propagationQueue.Count > 0)
                    {
                        Multi seed = propagationQueue.Dequeue();

                        for (int d = 0; d < 4; d++)
                        {
                            foreach (Multi neighbor in house.GetMultiAt(seed.X + pdx[d], seed.Y + pdy[d]))
                            {
                                if (neighbor.IsCustom &&
                                    neighbor.Z >= minZ && neighbor.Z < maxZ &&
                                    (neighbor.State & excludeMask) == 0 &&
                                    (neighbor.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                                    (neighbor.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) != 0)
                                {
                                    neighbor.State &= ~CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                    propagationQueue.Enqueue(neighbor);
                                }
                            }
                        }
                    }
                }
            }

            z = foundationItem.Z + 7 + FLOOR_HEIGHT;

            ushort color = 0x0051;

            for (int i = 1; i < CurrentFloor; i++)
            {
                for (int x = _bounds.X; x < EndPos.X; x++)
                {
                    for (int y = _bounds.Y; y < EndPos.Y; y++)
                    {
                        Multi mo = house.Add
                        (
                            0x0496,
                            (ushort)(x == _bounds.X || y == _bounds.Y ? 0x34 : color),
                            (ushort)(foundationItem.X + (x - foundationItem.X)),
                            (ushort)(foundationItem.Y + (y - foundationItem.Y)),
                            (sbyte) z,
                            true,
                            false
                        );

                        mo.AlphaHue = 0xFF;
                        mo.State = CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_TRANSPARENT;
                        mo.AddToTile();
                    }
                }

                color += 5;
                z += FLOOR_HEIGHT;
            }

        }

        /// <summary>
        /// Handles the target event when clicking on the world during house customization.
        /// This can either seek a tile graphic, erase an existing component, or build a new one.
        /// </summary>
        /// <param name="place">The game object that was targeted.</param>
        public void OnTargetWorld(GameObject place)
        {
            // Check whether the action is in the house's premises
            if (place == null || !_bounds.Contains(place.X, place.Y))
                return;

            if (SeekTile && place is Multi)
            {
                SeekGraphic(place.Graphic);
                return;
            }

            // Apply a minor offset for roof tiles
            int zOffset = CurrentFloor == 1 ? -7 : -3;
            if (place.Z < _world.Player.Z + zOffset || place.Z >= _world.Player.Z + FLOOR_HEIGHT)
                return;

            Item foundationItem = _world.Items.Get(Serial);
            if (foundationItem == null || !_world.HouseManager.TryGetHouse(Serial, out House house))
                return;

            if (Erasing && !ProcessErasing(place, house, foundationItem))
                return;

            HouseCustomizationGump gump = UIManager.GetGump<HouseCustomizationGump>(Serial);
            if (SelectedGraphic != 0)
                ProcessBuilding(place, house, foundationItem, gump);

            GenerateFloorPlace();
            gump.Update();
        }

        /// <summary>
        /// Processes the erasure of a component at the targeted location.
        /// </summary>
        /// <param name="place">The targeted game object to erase.</param>
        /// <param name="house">The house object containing the component.</param>
        /// <param name="foundationItem">The foundation item of the house.</param>
        /// <returns>True if erasure was processed; otherwise, false.</returns>
        private bool ProcessErasing(GameObject place, House house, Item foundationItem)
        {
            if (place is not Multi)
                return false;

            if (!CanEraseHere(place, out CUSTOM_HOUSE_BUILD_TYPE type))
                return true;

            IEnumerable<Multi> multi = house.GetMultiAt(place.X, place.Y);

            if (multi?.Any() != true)
                return false;

            int z = 7 + (CurrentFloor - 1) * FLOOR_HEIGHT;

            // Adjust Z for stair or roof erasure to match the target's actual Z
            if (type is CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR or CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                z = place.Z - (foundationItem.Z + z) + z;

            switch (type)
            {
                case CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR:
                    // When holding Ctrl, we delete only the specific stair piece, otherwise, we delete the entire stair group
                    // This preserves existing, most-compatible behavior while still supporting newer servers that allow for individual pieces to be removed.
                    EraseStair(place, house, foundationItem, z, !Keyboard.Ctrl);
                    break;
                case CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF:
                    EraseRoof(place, foundationItem, z);
                    break;
                default:
                    EraseItem(place, foundationItem, z);
                    break;
            }

            return true;
        }

        /// <summary>
        ///     Erases a stair component, potentially deleting the entire stair group if requested.
        /// </summary>
        /// <param name="place">The targeted stair piece.</param>
        /// <param name="house">The house object.</param>
        /// <param name="foundationItem">The foundation item.</param>
        /// <param name="z">The relative Z coordinate for erasure.</param>
        /// <param name="deleteEntireGroup">
        ///     <para>If true, attempts to find and delete all connected stair pieces.</para>
        ///     <para>
        ///         Note that this logic has a "life of its own" and may not always behave the same way.
        ///         Something to improve upon, perhaps.
        ///     </para>
        /// </param>
        private void EraseStair(GameObject place, House house, Item foundationItem, int z, bool deleteEntireGroup = true)
        {
            List<Multi> stairPieces;

            if (deleteEntireGroup)
            {
                int stairFloorBase = GetStairFloorBase(place, foundationItem);
                (List<Multi> sameX, List<Multi> sameY) = CollectPotentialStairPieces(house, place, stairFloorBase);
                stairPieces = FindStairGroup(place, sameX, sameY);
            }
            else
                stairPieces = [];

            DeleteStairPieces(place, foundationItem, stairPieces, z);
        }

        /// <summary>
        /// Calculates the base Z coordinate of the floor containing the targeted stair piece.
        /// </summary>
        /// <param name="place">The stair piece.</param>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <returns>The base Z coordinate of the floor.</returns>
        private int GetStairFloorBase(GameObject place, Item foundationItem)
        {
            int floorBase = foundationItem.Z;
            for (int f = 0; f < FloorCount; f++)
            {
                int fz = floorBase + 7 + f * FLOOR_HEIGHT;
                if (place.Z >= fz && place.Z < fz + FLOOR_HEIGHT)
                    return fz;
            }

            return floorBase;
        }

        /// <summary>
        /// Collects all custom stair pieces on the same floor and X or Y axis as the targeted piece.
        /// </summary>
        /// <param name="house">The house object.</param>
        /// <param name="place">The targeted stair piece.</param>
        /// <param name="stairFloorBase">The base Z coordinate of the floor.</param>
        /// <returns>A tuple containing lists of pieces on the same X and Y axis.</returns>
        private static (List<Multi> sameX, List<Multi> sameY) CollectPotentialStairPieces(House house, GameObject place, int stairFloorBase)
        {
            var sameX = new List<Multi>();
            var sameY = new List<Multi>();

            foreach (Multi comp in house.Components)
            {
                if (comp.IsDestroyed || !comp.IsCustom || (comp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) == 0)
                    continue;

                if (comp.Z < stairFloorBase || comp.Z >= stairFloorBase + FLOOR_HEIGHT)
                    continue;

                if (comp.X == place.X)
                    sameX.Add(comp);

                if (comp.Y == place.Y)
                    sameY.Add(comp);
            }

            return (sameX, sameY);
        }

        /// <summary>
        /// Identifies the group of stair pieces that form a continuous staircase around the targeted piece.
        /// </summary>
        /// <param name="place">The targeted stair piece.</param>
        /// <param name="sameX">Stair pieces on the same X axis.</param>
        /// <param name="sameY">Stair pieces on the same Y axis.</param>
        /// <returns>A list of stair pieces belonging to the same group.</returns>
        private List<Multi> FindStairGroup(GameObject place, List<Multi> sameX, List<Multi> sameY)
        {
            var stairPieces = new List<Multi>();
            int bestStart;

            // Staircases usually span 4 tiles; find which axis they are aligned on
            if (sameX.Count >= sameY.Count && sameX.Count > 0)
            {
                bestStart = FindBestStairWindow(place.Y, sameX, p => p.Y);

                foreach (Multi p in sameX)
                {
                    if (p.Y >= bestStart && p.Y <= bestStart + 3)
                        stairPieces.Add(p);
                }
                return stairPieces;
            }

            if (sameY.Count <= 0)
                return stairPieces;

            bestStart = FindBestStairWindow(place.X, sameY, p => p.X);
            stairPieces.AddRange(sameY.Where(p => p.X >= bestStart && p.X <= bestStart + 3));

            return stairPieces;
        }

        /// <summary>
        /// Finds the best 4-tile window along an axis that contains the most stair pieces, centered around the target coordinate.
        /// </summary>
        /// <param name="targetCoord">The coordinate of the targeted piece.</param>
        /// <param name="pieces">The list of pieces to search.</param>
        /// <param name="getCoord">A function to extract the relevant coordinate from a piece.</param>
        /// <returns>The starting coordinate of the best 4-tile window.</returns>
        private static int FindBestStairWindow(int targetCoord, List<Multi> pieces, Func<Multi, int> getCoord)
        {
            int bestCount = 0;
            int bestStart = targetCoord;

            // Iterate through possible 4-tile windows that could contain the target coordinate
            for (int start = targetCoord - 3; start <= targetCoord; start++)
            {
                int count = 0;

                foreach (Multi p in pieces)
                {
                    int coord = getCoord(p);
                    if (coord >= start && coord <= start + 3)
                        count++;
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestStart = start;
                }
            }

            return bestStart;
        }

        /// <summary>
        /// Deletes the specified stair pieces from the house.
        /// </summary>
        /// <param name="place">The targeted stair piece.</param>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="stairPieces">The group of stair pieces to delete.</param>
        /// <param name="z">The relative Z coordinate for erasure.</param>
        private void DeleteStairPieces(GameObject place, Item foundationItem, List<Multi> stairPieces, int z)
        {
            bool isCombined = IsCombinedStaircase(stairPieces);

            if (isCombined)
            {
                int currentFloorOffset = 7 + (CurrentFloor - 1) * FLOOR_HEIGHT;

                foreach (Multi piece in stairPieces)
                {
                    // Calculate relative Z for each piece in the group
                    int pz = piece.Z - (foundationItem.Z + currentFloorOffset) + currentFloorOffset;
                    AsyncNetClient.Socket.Send_CustomHouseDeleteItem(_world, piece.Graphic, piece.X - foundationItem.X, piece.Y - foundationItem.Y, pz);
                    piece.Destroy();
                }
            }
            else
            {
                AsyncNetClient.Socket.Send_CustomHouseDeleteItem(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
                place.Destroy();
            }
        }

        /// <summary>
        /// Determines if the given stair pieces form a combined (multi-level) staircase.
        /// </summary>
        /// <param name="stairPieces">The list of stair pieces to check.</param>
        /// <returns>True if they form a combined staircase; otherwise, false.</returns>
        private bool IsCombinedStaircase(List<Multi> stairPieces)
        {
            if (stairPieces.Count <= 1)
            {
                return false;
            }

            int firstZ = stairPieces[0].Z;

            for (int i = 1; i < stairPieces.Count; i++)
            {
                if (stairPieces[i].Z != firstZ)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a request to erase a roof component.
        /// </summary>
        /// <param name="place">The roof component to erase.</param>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="z">The relative Z coordinate.</param>
        private void EraseRoof(GameObject place, Item foundationItem, int z)
        {
            AsyncNetClient.Socket.Send_CustomHouseDeleteRoof(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
            place.Destroy();
        }

        /// <summary>
        /// Sends a request to erase a general item component.
        /// </summary>
        /// <param name="place">The item component to erase.</param>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="z">The relative Z coordinate.</param>
        private void EraseItem(GameObject place, Item foundationItem, int z)
        {
            AsyncNetClient.Socket.Send_CustomHouseDeleteItem(_world, place.Graphic, place.X - foundationItem.X, place.Y - foundationItem.Y, z);
            place.Destroy();
        }

        /// <summary>
        /// Processes building a new component at the targeted location.
        /// </summary>
        /// <param name="place">The targeted game object (used for coordinates).</param>
        /// <param name="house">The house object.</param>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="gump">The house customization gump.</param>
        private void ProcessBuilding(GameObject place, House house, Item foundationItem, HouseCustomizationGump gump)
        {
            var list = new List<CustomBuildObject>();

            if (!CanBuildHere(list, out CUSTOM_HOUSE_BUILD_TYPE type) || list.Count <= 0)
            {
                return;
            }

            int placeX = place.X;
            int placeY = place.Y;

            if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && CombinedStair)
            {
                BuildCombinedStair(foundationItem, gump, placeX, placeY);
            }
            else
            {
                BuildItem(place, house, foundationItem, list, type);
            }

            int xx = placeX - foundationItem.X;
            int yy = placeY - foundationItem.Y;
            int z = foundationItem.Z + 7 + (CurrentFloor - 1) * FLOOR_HEIGHT;

            if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && !CombinedStair)
            {
                z = foundationItem.Z;
            }

            AddBuiltObjectsToHouse(house, foundationItem, list, xx, yy, z);
        }

        /// <summary>
        /// Adds a list of built objects to the house's internal component list for rendering.
        /// </summary>
        /// <param name="house">The house object.</param>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="list">The list of objects to add.</param>
        /// <param name="xx">Relative X offset from foundation.</param>
        /// <param name="yy">Relative Y offset from foundation.</param>
        /// <param name="z">Absolute Z base coordinate.</param>
        private void AddBuiltObjectsToHouse(House house, Item foundationItem, List<CustomBuildObject> list, int xx, int yy, int z)
        {
            foreach (CustomBuildObject item in list)
            {
                house.Add
                (
                    item.Graphic,
                    0,
                    (ushort)(foundationItem.X + xx + item.X),
                    (ushort)(foundationItem.Y + yy + item.Y),
                    (sbyte)(z + item.Z),
                    true,
                    false
                );
            }
        }

        /// <summary>
        /// Sends a request to build a combined (multi-tile) staircase.
        /// </summary>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="gump">The house customization gump.</param>
        /// <param name="placeX">The target X coordinate.</param>
        /// <param name="placeY">The target Y coordinate.</param>
        private void BuildCombinedStair(Item foundationItem, HouseCustomizationGump gump, int placeX, int placeY)
        {
            if (gump.Page < 0 || gump.Page >= Stairs.Count)
                return;

            CustomHouseStair stair = Stairs[gump.Page];
            ushort graphic = 0;

            if (SelectedGraphic == stair.North)
                graphic = (ushort)stair.MultiNorth;
            else if (SelectedGraphic == stair.East)
                graphic = (ushort)stair.MultiEast;
            else if (SelectedGraphic == stair.South)
                graphic = (ushort)stair.MultiSouth;
            else if (SelectedGraphic == stair.West)
                graphic = (ushort)stair.MultiWest;

            if (graphic != 0)
                AsyncNetClient.Socket.Send_CustomHouseAddStair(_world, graphic, placeX - foundationItem.X, placeY - foundationItem.Y);
        }

        /// <summary>
        /// Builds a single item or non-combined stair piece.
        /// </summary>
        /// <param name="place">The targeted game object.</param>
        /// <param name="house">The house object.</param>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="list">The list of objects to build.</param>
        /// <param name="type">The type of build action.</param>
        private void BuildItem(GameObject place, House house, Item foundationItem, List<CustomBuildObject> list, CUSTOM_HOUSE_BUILD_TYPE type)
        {
            CustomBuildObject item = list[0];
            int x = place.X - foundationItem.X + item.X;
            int y = place.Y - foundationItem.Y + item.Y;
            IEnumerable<Multi> multiAtTarget = house.GetMultiAt(place.X + item.X, place.Y + item.Y);

            if (multiAtTarget?.Any() != true && type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                return;

            if (!CombinedStair)
                ClearExistingItemsForBuild(foundationItem, multiAtTarget, type);

            SendBuildRequest(item, type, x, y);
        }

        /// <summary>
        /// Clears existing custom items at the target location that would conflict with a new build.
        /// </summary>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="multiAtTarget">The collection of components at the target location.</param>
        /// <param name="type">The type of build action.</param>
        private void ClearExistingItemsForBuild(Item foundationItem, IEnumerable<Multi> multiAtTarget, CUSTOM_HOUSE_BUILD_TYPE type)
        {
            int minZ = foundationItem.Z + 7 + (CurrentFloor - 1) * FLOOR_HEIGHT;
            int maxZ = minZ + FLOOR_HEIGHT;

            if (CurrentFloor == 1)
                minZ -= 7;

            foreach (Multi multiObject in multiAtTarget)
            {
                if (ShouldClearItem(multiObject, type, minZ, maxZ))
                {
                    multiObject.Destroy();
                }
            }
        }

        /// <summary>
        /// Determines if a specific component should be cleared to make room for a new build of the specified type.
        /// </summary>
        /// <param name="multiObject">The existing component to check.</param>
        /// <param name="type">The type of build action.</param>
        /// <param name="minZ">The floor's minimum Z.</param>
        /// <param name="maxZ">The floor's maximum Z.</param>
        /// <returns>True if the item should be cleared; otherwise, false.</returns>
        private static bool ShouldClearItem(Multi multiObject, CUSTOM_HOUSE_BUILD_TYPE type, int minZ, int maxZ)
        {
            int testMinZ = minZ;

            if ((multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0)
                testMinZ -= 3;

            if (multiObject.Z < testMinZ || multiObject.Z >= maxZ || !multiObject.IsCustom || (multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) != 0)
                return false;

            return type switch
            {
                CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR => (multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) != 0,
                CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF => (multiObject.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0,
                CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR => (multiObject.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) != 0,
                _ => (multiObject.State & (
                    CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                    CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                    CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                    CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE)
                ) == 0
            };
        }

        /// <summary>
        /// Sends a network request to build a custom house component.
        /// </summary>
        /// <param name="item">The object to build.</param>
        /// <param name="type">The type of build action.</param>
        /// <param name="x">The relative X coordinate.</param>
        /// <param name="y">The relative Y coordinate.</param>
        private void SendBuildRequest(CustomBuildObject item, CUSTOM_HOUSE_BUILD_TYPE type, int x, int y)
        {
            if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF)
                AsyncNetClient.Socket.Send_CustomHouseAddRoof(_world, item.Graphic, x, y, item.Z);
            else
                AsyncNetClient.Socket.Send_CustomHouseAddItem(_world, item.Graphic, x, y);
        }

        /// <summary>
        /// Seeks a graphic in the available customization lists and updates the UI state if found.
        /// </summary>
        /// <param name="graphic">The graphic to seek.</param>
        private void SeekGraphic(ushort graphic)
        {
            CUSTOM_HOUSE_GUMP_STATE state = 0;
            (int res1, int res2) = ExistsInList(ref state, graphic);

            if (res1 == -1 || res2 == -1)
                return;

            State = state;
            HouseCustomizationGump gump = UIManager.GetGump<HouseCustomizationGump>(Serial);

            if (State is CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL or CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF or CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC)
            {
                Category = res1;
                gump.Page = res2;
            }
            else
            {
                Category = -1;
                gump.Page = res1;
            }

            gump.UpdateMaxPage();
            SetTargetMulti();
            SelectedGraphic = graphic;
            gump.Update();
        }

        /// <summary>
        /// Resets the targeting state for custom house building.
        /// </summary>
        public void SetTargetMulti()
        {
            _world.TargetManager.SetTargetingMulti
            (
                0,
                0,
                0,
                0,
                0,
                0
            );

            Erasing = false;
            SeekTile = false;
            SelectedGraphic = 0;
            CombinedStair = false;
        }

        /// <summary>
        /// Validates if the currently selected component can be built at the current targeting location.
        /// This method checks for component limits, floor restrictions, and collisions with existing components.
        /// </summary>
        /// <param name="list">A list to be populated with the components to be built.</param>
        /// <param name="type">Outputs the identified build type.</param>
        /// <returns>True if building is allowed; otherwise, false.</returns>
        public bool CanBuildHere(List<CustomBuildObject> list, out CUSTOM_HOUSE_BUILD_TYPE type)
        {
            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_NORMAL;

            if (SelectedGraphic == 0)
            {
                return false;
            }

            Item foundationItem = _world.Items.Get(Serial);

            if (foundationItem == null || !_world.HouseManager.TryGetHouse(foundationItem, out House house))
                return false;

            bool result = true;

            // Handle combined staircase building logic
            if (CombinedStair)
            {
                if (Components + 10 > MaxComponets || CurrentFloor >= FloorCount)
                {
                    return false;
                }

                (int res1, int res2) = SeekGraphicInCustomHouseObjectList(Stairs, SelectedGraphic);

                if (res1 == -1 || res2 == -1 || res1 >= Stairs.Count)
                {
                    list.Add(new CustomBuildObject()
                    {
                        Graphic = SelectedGraphic,
                        X = 0,
                        Y = 0,
                        Z = 0
                    });

                    return false;
                }

                CustomHouseStair item = Stairs[res1];

                // Add all pieces of the multi-tile staircase to the build list
                if (SelectedGraphic == item.North)
                {
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -3, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -2, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -1, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.North, X = 0, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -3, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -2, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.North, X = 0, Y = -1, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = -3, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.North, X = 0, Y = -2, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.North, X = 0, Y = -3, Z = 15 });
                }
                else if (SelectedGraphic == item.East)
                {
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.East, X = 0, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 1, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 2, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 3, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.East, X = 1, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 2, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 3, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.East, X = 2, Y = 0, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 3, Y = 0, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.East, X = 3, Y = 0, Z = 15 });
                }
                else if (SelectedGraphic == item.South)
                {
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.South, X = 0, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 1, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 2, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 3, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.South, X = 0, Y = 1, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 2, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 3, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.South, X = 0, Y = 2, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = 0, Y = 3, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.South, X = 0, Y = 3, Z = 15 });
                }
                else if (SelectedGraphic == item.West)
                {
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -3, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -2, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -1, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.West, X = 0, Y = 0, Z = 0 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -3, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -2, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.West, X = -1, Y = 0, Z = 5 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.Block, X = -3, Y = 0, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.West, X = -2, Y = 0, Z = 10 });
                    list.Add(new CustomBuildObject { Graphic = (ushort)item.West, X = -3, Y = 0, Z = 15 });
                }
                else
                {
                    list.Add(new CustomBuildObject { Graphic = SelectedGraphic, X = 0, Y = 0, Z = 0 });
                }

                type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
            }
            else
            {
                // Check if building a door or teleport (fixtures)
                (int fixCheck1, int fixCheck2) = SeekGraphicInCustomHouseObjectList(Doors, SelectedGraphic);

                bool isFixture = false;

                if (fixCheck1 == -1 || fixCheck2 == -1)
                {
                    (fixCheck1, fixCheck2) = SeekGraphicInCustomHouseObjectList(Teleports, SelectedGraphic);

                    isFixture = fixCheck1 != -1 && fixCheck2 != -1;

                    if (isFixture)
                    {
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR;
                    }
                }
                else
                {
                    isFixture = true;
                    type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_NORMAL;
                }

                if (isFixture)
                {
                    if (Fixtures + 1 > MaxFixtures)
                    {
                        result = false;
                    }
                }
                else if (Components + 1 > MaxComponets)
                {
                    result = false;
                }

                if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF)
                {
                    list.Add(new CustomBuildObject { Graphic = SelectedGraphic, X = 0, Y = 0, Z = (RoofZ - 2) * 3 });
                    type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF;
                }
                else
                {
                    if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR)
                    {
                        list.Add(new CustomBuildObject { Graphic = SelectedGraphic, X = 0, Y = 1, Z = 0 });
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
                    }
                    else
                    {
                        if (State == CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR)
                        {
                            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR;
                        }

                        list.Add(new CustomBuildObject { Graphic = SelectedGraphic, X = 0, Y = 0, Z = 0 });
                    }
                }
            }

            if (SelectedObject.Object is GameObject gobj)
            {
                if (!_bounds.Contains(gobj.X, gobj.Y))
                    return false;

                int minZ = foundationItem.Z + 0 + (CurrentFloor - 1) * FLOOR_HEIGHT;
                int maxZ = minZ + FLOOR_HEIGHT;

                // var boundsOffset = State != CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL ? 1 : 0;

                for (int i = 0; i < list.Count; ++i)
                {
                    CustomBuildObject item = list[i];
                    if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                    {
                        if (CombinedStair)
                        {
                            if (item.Z != 0)
                                continue;
                        }
                        else
                        {
                            if (gobj.Y + item.Y < EndPos.Y || gobj.X + item.X == _bounds.X || gobj.Z >= MinHouseZ)
                                return false;

                            if (gobj.Y + item.Y != EndPos.Y)
                            {
                                item.Y = 0;
                                list[0] = item;
                            }
                            continue;
                        }
                    }

                    if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && CombinedStair)
                    {
                        int tileX = gobj.X + item.X;
                        int tileY = gobj.Y + item.Y;

                        if (tileX < StartPos.X || tileX >= EndPos.X || tileY < StartPos.Y || tileY >= EndPos.Y)
                            return false;
                    }
                    else if (!ValidateItemPlace(_bounds, item.Graphic, gobj.X + item.X, gobj.Y + item.Y))
                    {
                        return false;
                    }

                    if (type != CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR)
                    {
                        foreach (Multi multi in house.GetMultiAt(gobj.X + item.X, gobj.Y + item.Y))
                        {
                            if (!multi.IsCustom)
                                continue;

                            int collisionMaxZ = (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR && CombinedStair) ? maxZ + FLOOR_HEIGHT : maxZ;

                            if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0 && multi.Z >= minZ && multi.Z < collisionMaxZ)
                            {
                                if (type == CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR)
                                {
                                    if ((multi.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_DONT_REMOVE)) == 0)
                                        return false;
                                }
                                else
                                {
                                    if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) != 0)
                                        return false;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if a component can be erased at the targeted location.
        /// </summary>
        /// <param name="place">The game object to check for erasure.</param>
        /// <param name="type">Outputs the identified build type of the component at the location.</param>
        /// <returns>True if erasure is allowed; otherwise, false.</returns>
        public bool CanEraseHere(GameObject place, out CUSTOM_HOUSE_BUILD_TYPE type)
        {
            type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_NORMAL;

            if (place != null && place is Multi multi)
            {
                if (multi.IsCustom && (multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0)
                {
                    if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0)
                    {
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_FLOOR;
                    }
                    else if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR) != 0)
                    {
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_STAIR;
                    }
                    else if ((multi.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0)
                    {
                        type = CUSTOM_HOUSE_BUILD_TYPE.CHBT_ROOF;
                    }
                    else if (_bounds.Contains(place.X, place.Y) && place.Z >= MinHouseZ)
                    {
                        // it's into the bounds
                    }
                    else
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a graphic exists in any of the custom house component lists and returns its location.
        /// </summary>
        /// <param name="state">The gump state corresponding to the component type found.</param>
        /// <param name="graphic">The graphic to search for.</param>
        /// <returns>A tuple containing the category index and item index (or page index).</returns>
        public (int, int) ExistsInList(ref CUSTOM_HOUSE_GUMP_STATE state, ushort graphic)
        {
            (int res1, int res2) = SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseWall, CustomHouseWallCategory>(Walls, graphic);

            if (res1 == -1 || res2 == -1)
            {
                (res1, res2) = SeekGraphicInCustomHouseObjectList(Floors, graphic);

                if (res1 == -1 || res2 == -1)
                {
                    (res1, res2) = SeekGraphicInCustomHouseObjectList(Doors, graphic);

                    if (res1 == -1 || res2 == -1)
                    {
                        (res1, res2) = SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseMisc, CustomHouseMiscCategory>(Miscs, graphic);

                        if (res1 == -1 || res2 == -1)
                        {
                            (res1, res2) = SeekGraphicInCustomHouseObjectList(Stairs, graphic);

                            if (res1 == -1 || res2 == -1)
                            {
                                (res1, res2) = SeekGraphicInCustomHouseObjectListWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(Roofs, graphic);

                                if (res1 != -1 && res2 != -1)
                                {
                                    state = CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF;
                                }
                            }
                            else
                            {
                                state = CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR;
                            }
                        }
                        else
                        {
                            (int res_1, int res_2) = SeekGraphicInCustomHouseObjectList(Teleports, graphic);

                            if (res_1 != -1 && res_2 != -1)
                            {
                                state = CUSTOM_HOUSE_GUMP_STATE.CHGS_FIXTURE;
                                res1 = res_1;
                                res2 = res_2;
                            }
                            else
                            {
                                state = CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC;
                            }
                        }
                    }
                    else
                    {
                        state = CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR;
                    }
                }
                else
                {
                    state = CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR;
                }
            }
            else
            {
                state = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;
            }

            return (res1, res2);
        }

        /// <summary>
        /// Validates if a graphic can be placed at the specified coordinates within a bounding rectangle.
        /// This checks for plot boundaries and specific placement rules (e.g. CanGoN, CanGoW).
        /// </summary>
        /// <param name="rect">The bounding rectangle of the house.</param>
        /// <param name="graphic">The graphic to place.</param>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>True if placement is valid; otherwise, false.</returns>
        private bool ValidateItemPlace(Rectangle rect, ushort graphic, int x, int y)
        {
            if (!rect.Contains(x, y))
            {
                return false;
            }

            (int infoCheck1, int infoCheck2) = SeekGraphicInCustomHouseObjectList(ObjectsInfo, graphic);

            if (infoCheck1 != -1 && infoCheck2 != -1)
            {
                CustomHousePlaceInfo info = ObjectsInfo[infoCheck1];

                if (info.CanGoW == 0 && x == rect.X)
                {
                    return false;
                }

                if (info.CanGoN == 0 && y == rect.Y)
                {
                    return false;
                }

                if (info.CanGoNWS == 0 && x == rect.X && y == rect.Y)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Performs structural validation for a specific multi component within a floor's Z range.
        /// This ensures items are properly supported and follow house building rules.
        /// </summary>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="item">The multi component to validate.</param>
        /// <param name="minZ">The floor's minimum Z.</param>
        /// <param name="maxZ">The floor's maximum Z.</param>
        /// <param name="validatedFloors">A list of points representing validated floor tiles.</param>
        /// <returns>True if the component's placement is structurally valid; otherwise, false.</returns>
        public bool ValidateItemPlace(Item foundationItem, Multi item, int minZ, int maxZ, List<Point> validatedFloors)
        {
            if (item == null || !_world.HouseManager.TryGetHouse(foundationItem, out House house) || !item.IsCustom)
            {
                return true;
            }

            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0)
            {
                bool existsInList(List<Point> list, Point testedPoint)
                {
                    foreach (Point point in list)
                    {
                        if (testedPoint == point)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (ValidatePlaceStructure
                (
                    foundationItem,
                    house,
                    house.GetMultiAt(item.X, item.Y),
                    minZ - FLOOR_HEIGHT,
                    maxZ - FLOOR_HEIGHT,
                    (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT
                ) || ValidatePlaceStructure
                (
                    foundationItem,
                    house,
                    house.GetMultiAt(item.X - 1, item.Y),
                    minZ - FLOOR_HEIGHT,
                    maxZ - FLOOR_HEIGHT,
                    (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_W)
                ) || ValidatePlaceStructure
                (
                    foundationItem,
                    house,
                    house.GetMultiAt(item.X, item.Y - 1),
                    minZ - FLOOR_HEIGHT,
                    maxZ - FLOOR_HEIGHT,
                    (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_N)
                ))
                {
                    Point[] table =
                    {
                        new Point(-1, 0),
                        new Point(0, -1),
                        new Point(1, 0),
                        new Point(0, 1)
                    };

                    for (int i = 0; i < 4; i++)
                    {
                        var testPoint = new Point(item.X + table[i].X, item.Y + table[i].Y);

                        if (!existsInList(validatedFloors, testPoint))
                        {
                            validatedFloors.Add(testPoint);
                        }
                    }

                    return true;
                }

                return false;
            }


            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF) != 0)
            {
                return true;
            }

            if ((item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) != 0)
            {
                foreach (Multi temp in house.GetMultiAt(item.X, item.Y))
                {
                    if (temp == item)
                    {
                        continue;
                    }

                    if ((temp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 && temp.Z >= minZ && temp.Z < maxZ)
                    {
                        if ((temp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 && (temp.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }


            (int infoCheck1, int infoCheck2) = SeekGraphicInCustomHouseObjectList(ObjectsInfo, item.Graphic);

            if (infoCheck1 != -1 && infoCheck2 != -1)
            {
                CustomHousePlaceInfo info = ObjectsInfo[infoCheck1];

                if (info.CanGoW == 0 && item.X == _bounds.X)
                {
                    return false;
                }

                if (info.CanGoN == 0 && item.Y == _bounds.Y)
                {
                    return false;
                }

                if (info.CanGoNWS == 0 && item.X == _bounds.X && item.Y == _bounds.Y)
                {
                    return false;
                }

                if (info.Bottom == 0)
                {
                    bool found = false;

                    if (info.AdjUN != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X, item.Y + 1),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N)
                        );
                    }

                    if (!found && info.AdjUE != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X - 1, item.Y),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E)
                        );
                    }

                    if (!found && info.AdjUS != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X, item.Y - 1),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S)
                        );
                    }

                    if (!found && info.AdjUW != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X + 1, item.Y),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W)
                        );
                    }

                    if (!found && minZ == foundationItem.Z + 7)
                    {
                        return false;
                    }
                }

                if (info.Top == 0)
                {
                    bool found = false;

                    if (info.AdjLN != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X, item.Y + 1),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N)
                        );
                    }

                    if (!found && info.AdjLE != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X - 1, item.Y),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E)
                        );
                    }

                    if (!found && info.AdjLS != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X, item.Y - 1),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S)
                        );
                    }

                    if (!found && info.AdjLW != 0)
                    {
                        found = ValidatePlaceStructure
                        (
                            foundationItem,
                            house,
                            house.GetMultiAt(item.X + 1, item.Y),
                            minZ,
                            maxZ,
                            (int) (CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP | CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W)
                        );
                    }

                    if (!found && minZ == foundationItem.Z + 7)
                    {
                        return false;
                    }
                }

            }

            if (minZ > foundationItem.Z + 7)
            {
                int belowMinZ = minZ - FLOOR_HEIGHT;

                // 1) Check same position on the floor below for wall-type support.
                bool foundAnyWallBelow = false;
                bool hasFloorTileBelow = false;

                foreach (Multi below in house.GetMultiAt(item.X, item.Y))
                {
                    if (below.IsCustom && below.Z >= belowMinZ && below.Z < minZ)
                    {
                        if ((below.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR) != 0 &&
                            (below.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0)
                        {
                            hasFloorTileBelow = true;
                        }

                        if ((below.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                                           CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                                           CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                                           CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE |
                                           CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0)
                        {
                            foundAnyWallBelow = true;

                            if ((below.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                            {
                                return true;
                            }
                        }
                    }
                }

                if (foundAnyWallBelow)
                {
                    return false;
                }

                // 2) No wall at same position below. If there's a floor tile below,
                //    check ±1 adjacent positions on the floor below for wall support.
                if (hasFloorTileBelow)
                {
                    int[] adx = { -1, 1, 0, 0 };
                    int[] ady = { 0, 0, -1, 1 };

                    for (int d = 0; d < 4; d++)
                    {
                        foreach (Multi adj in house.GetMultiAt(item.X + adx[d], item.Y + ady[d]))
                        {
                            if (adj.IsCustom &&
                                adj.Z >= belowMinZ && adj.Z < minZ &&
                                (adj.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                                             CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                                             CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                                             CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE |
                                             CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0 &&
                                (adj.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                            {
                                return true;
                            }
                        }
                    }
                }

                // 3) No below-support. Check if there's a validated same-floor wall
                //    neighbor (propagation from walls that do have below-support).
                int[] dx = { -1, 1, 0, 0 };
                int[] dy = { 0, 0, -1, 1 };

                for (int d = 0; d < 4; d++)
                {
                    foreach (Multi neighbor in house.GetMultiAt(item.X + dx[d], item.Y + dy[d]))
                    {
                        if (neighbor.IsCustom &&
                            neighbor.Z >= minZ && neighbor.Z < maxZ &&
                            (neighbor.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR |
                                              CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR |
                                              CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF |
                                              CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE |
                                              CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL)) == 0 &&
                            (neighbor.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) != 0 &&
                            (neighbor.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the structural integrity of a group of multi components.
        /// This method checks if components have proper support based on their type and placement rules.
        /// </summary>
        /// <param name="foundationItem">The house foundation item.</param>
        /// <param name="house">The house object.</param>
        /// <param name="multi">The collection of multi components to validate.</param>
        /// <param name="minZ">The floor's minimum Z.</param>
        /// <param name="maxZ">The floor's maximum Z.</param>
        /// <param name="flags">Validation flags that define what kind of support is being checked.</param>
        /// <returns>True if the structure is valid according to the provided flags; otherwise, false.</returns>
        public bool ValidatePlaceStructure
        (
            Item foundationItem,
            House house,
            IEnumerable<Multi> multi,
            int minZ,
            int maxZ,
            int flags
        )
        {
            if (house == null)
            {
                return false;
            }

            var validatedFloors = new List<Point>();
            foreach (Multi item in multi)
            {
                validatedFloors.Clear();

                if (item.IsCustom && (item.State & (CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FLOOR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_STAIR | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_ROOF | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_FIXTURE)) == 0 && item.Z >= minZ && item.Z < maxZ)
                {
                    (int info1, int info2) = SeekGraphicInCustomHouseObjectList(ObjectsInfo, item.Graphic);

                    if (info1 != -1 && info2 != -1)
                    {
                        CustomHousePlaceInfo info = ObjectsInfo[info1];

                        if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_DIRECT_SUPPORT) != 0)
                        {
                            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) != 0 || info.DirectSupports == 0)
                            {
                                continue;
                            }

                            if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_W) != 0)
                            {
                                if (info.CanGoW != 0)
                                {
                                    return true;
                                }
                            }
                            else if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_CANGO_N) != 0)
                            {
                                if (info.CanGoN != 0)
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else if (((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM) != 0 && info.Bottom != 0) || ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_TOP) != 0 && info.Top != 0))
                        {
                            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE) == 0)
                            {
                                item.State |= CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;

                                if (!ValidateItemPlace
                                (
                                    foundationItem,
                                    item,
                                    minZ,
                                    maxZ,
                                    validatedFloors
                                ))
                                {
                                    item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE;
                                }
                                else
                                {
                                    item.State = item.State | CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_VALIDATED_PLACE;
                                }
                            }

                            if ((item.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_INCORRECT_PLACE) == 0)
                            {
                                if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_BOTTOM) != 0)
                                {
                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N) != 0 && info.AdjUN != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E) != 0 && info.AdjUE != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S) != 0 && info.AdjUS != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W) != 0 && info.AdjUW != 0)
                                    {
                                        return true;
                                    }
                                }
                                else
                                {
                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_N) != 0 && info.AdjLN != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_E) != 0 && info.AdjLE != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_S) != 0 && info.AdjLS != 0)
                                    {
                                        return true;
                                    }

                                    if ((flags & (int) CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS.CHVCF_W) != 0 && info.AdjLW != 0)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void ParseFile<T>(List<T> list, string path) where T : CustomHouseObject, new()
        {
            var file = new FileInfo(path);

            if (!file.Exists)
            {
                return;
            }

            using (StreamReader reader = File.OpenText(file.FullName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var item = new T();

                    if (item.Parse(line))
                    {
                        if (item.FeatureMask == 0 || ((int)_world.ClientLockedFeatures.Flags & item.FeatureMask) != 0)
                        {
                            list.Add(item);
                        }
                    }
                }
            }
        }

        private void ParseFileWithCategory<T, U>(List<U> list, string path) where T : CustomHouseObject, new() where U : CustomHouseObjectCategory<T>, new()
        {
            var file = new FileInfo(path);

            if (!file.Exists)
            {
                return;
            }

            using (StreamReader reader = File.OpenText(file.FullName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var item = new T();

                    if (item.Parse(line))
                    {
                        if (item.FeatureMask != 0 && ((int)_world.ClientLockedFeatures.Flags & item.FeatureMask) == 0)
                        {
                            continue;
                        }

                        bool found = false;

                        foreach (U c in list)
                        {
                            if (c.Index == item.Category)
                            {
                                c.Items.Add(item);
                                found = true;

                                break;
                            }
                        }


                        if (!found)
                        {
                            var c = new U
                            {
                                Index = item.Category
                            };

                            c.Items.Add(item);
                            list.Add(c);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Seeks a graphic in a list of custom house object categories.
        /// </summary>
        /// <typeparam name="T">The type of custom house object.</typeparam>
        /// <typeparam name="U">The type of custom house object category.</typeparam>
        /// <param name="list">The list of categories to search.</param>
        /// <param name="graphic">The graphic to search for.</param>
        /// <returns>A tuple containing the category index and item index if found; otherwise, (-1, -1).</returns>
        private static (int, int) SeekGraphicInCustomHouseObjectListWithCategory<T, U>(List<U> list, ushort graphic) where T : CustomHouseObject where U : CustomHouseObjectCategory<T>
        {
            for (int i = 0; i < list.Count; i++)
            {
                U c = list[i];

                for (int j = 0; j < c.Items.Count; j++)
                {
                    int contains = c.Items[j].Contains(graphic);

                    if (contains != -1)
                    {
                        return (i, j);
                    }
                }
            }

            return (-1, -1);
        }

        /// <summary>
        /// Seeks a graphic in a list of custom house objects.
        /// </summary>
        /// <typeparam name="T">The type of custom house object.</typeparam>
        /// <param name="list">The list of objects to search.</param>
        /// <param name="graphic">The graphic to search for.</param>
        /// <returns>A tuple containing the object index and internal index if found; otherwise, (-1, -1).</returns>
        private static (int, int) SeekGraphicInCustomHouseObjectList<T>(List<T> list, ushort graphic) where T : CustomHouseObject
        {
            for (int i = 0; i < list.Count; i++)
            {
                int contains = list[i].Contains(graphic);

                if (contains != -1)
                {
                    return (i, contains);
                }
            }

            return (-1, -1);
        }
    }

    public enum CUSTOM_HOUSE_GUMP_STATE
    {
        CHGS_WALL = 0,
        CHGS_DOOR,
        CHGS_FLOOR,
        CHGS_STAIR,
        CHGS_ROOF,
        CHGS_MISC,
        CHGS_MENU,
        CHGS_FIXTURE
    }

    public enum CUSTOM_HOUSE_FLOOR_VISION_STATE
    {
        CHGVS_NORMAL = 0,
        CHGVS_TRANSPARENT_CONTENT,
        CHGVS_HIDE_CONTENT,
        CHGVS_TRANSPARENT_FLOOR,
        CHGVS_HIDE_FLOOR,
        CHGVS_TRANSLUCENT_FLOOR,
        CHGVS_HIDE_ALL
    }

    public enum CUSTOM_HOUSE_BUILD_TYPE
    {
        CHBT_NORMAL = 0,
        CHBT_ROOF,
        CHBT_FLOOR,
        CHBT_STAIR
    }

    [Flags]
    public enum CUSTOM_HOUSE_MULTI_OBJECT_FLAGS
    {
        CHMOF_GENERIC_INTERNAL = 0x01,
        CHMOF_FLOOR = 0x02,
        CHMOF_STAIR = 0x04,
        CHMOF_ROOF = 0x08,
        CHMOF_FIXTURE = 0x10,
        CHMOF_TRANSPARENT = 0x20,
        CHMOF_IGNORE_IN_RENDER = 0x40,
        CHMOF_VALIDATED_PLACE = 0x80,
        CHMOF_INCORRECT_PLACE = 0x100,

        CHMOF_DONT_REMOVE = 0x200,
        CHMOF_PREVIEW = 0x400
    }

    [Flags]
    public enum CUSTOM_HOUSE_VALIDATE_CHECK_FLAGS
    {
        CHVCF_TOP = 0x01,
        CHVCF_BOTTOM = 0x02,
        CHVCF_N = 0x04,
        CHVCF_E = 0x08,
        CHVCF_S = 0x10,
        CHVCF_W = 0x20,
        CHVCF_DIRECT_SUPPORT = 0x40,
        CHVCF_CANGO_W = 0x80,
        CHVCF_CANGO_N = 0x100
    }
}
