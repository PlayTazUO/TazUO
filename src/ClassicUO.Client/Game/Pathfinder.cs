// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game
{
    public sealed class Pathfinder
    {
        private const int PATHFINDER_MAX_NODES = 150000;

        // Node budget for the A* search. User-configurable; falls back to the default
        // when no profile is loaded (e.g. during early startup).
        private static int MaxNodes => ProfileManager.CurrentProfile?.PathfindingMaxNodes > 0
            ? ProfileManager.CurrentProfile.PathfindingMaxNodes
            : PATHFINDER_MAX_NODES;
        private static PathNode _goalNode;
        private static int _pathfindDistance;
        private static readonly PriorityQueue _openSet = new();
        private static readonly Dictionary<(int x, int y, int z), PathNode> _closedSet = new();
        private static readonly List<PathNode> _path = new();

        // True when _path holds pooled PathNodes that are NOT tracked in _openSet/_closedSet
        // (i.e. populated by StartComputedPath). In that case CleanupPathfinding must return
        // them to the pool itself. For normal A* paths the nodes are also in _closedSet and
        // get returned there, so this stays false to avoid a double-return.
        private static bool _ownsPathNodes;

        private static int _pointIndex;
        private static bool _run;
        private static readonly int[] _offsetX =
        {
            0, 1, 1, 1, 0, -1, -1, -1, 0, 1
        };
        private static readonly int[] _offsetY =
        {
            -1, -1, 0, 1, 1, 1, 0, -1, -1, -1
        };
        private static readonly sbyte[] _dirOffset =
        {
            1, -1
        };
        private Point _startPoint, _endPoint;

        private static int _endPointZ;
        private static readonly List<PathObject> _reusableList = new();

        // Extra A* cost applied to tiles adjacent to an impassable multi (house wall), giving
        // paths a soft 1-tile standoff from houses. 0 disables it. Cached per-search from the
        // profile so it can't change mid-search.
        private int _multiBufferPenalty;

        // Per-search memoization of "does this tile hold a blocking multi component?" so the
        // buffer check doesn't re-walk a tile's object list once per neighbouring node. Cleared
        // by CleanupPathfinding at the start of every search.
        private static readonly Dictionary<(int x, int y), bool> _blockingMultiCache = new();

        public Point StartPoint => _startPoint;
        public Point EndPoint => _endPoint;
        public int PathSize => _path.Count;

        public bool AutoWalking { get; set; }

        public static bool PathFindingCanBeCancelled { get; set; }

        public static bool FastRotation { get; set; }

        public bool BlockMoving { get; set; }

        private int _zLevelDiff;

        private World _world;

        /// <summary>
        /// Fired when a step of a path from <see cref="StartComputedPath"/> is rejected at
        /// the client or server — typically a dynamic item (lamp post, rock, placed door) that
        /// isn't in statics.mul. Hooked by WorldMapPathfinder to mark the tile and replan.
        /// Arguments: (blocked tile X, blocked tile Y).
        /// </summary>
        public event Action<int, int> OnComputedPathStepFailed;

        private bool _computedPathActive;

        public Pathfinder(World world)
        {
            _world = world;
        }

        public static bool ObjectBlocksLOS(GameObject obj, int losMinZ, int losMaxZ)
        {
            int objZ = obj.Z;
            int objHeight = 0;
            bool isBlocker = false;

            switch (obj)
            {
                case Land land:
                    objHeight = 1;
                    isBlocker = land.TileData.IsImpassable;
                    break;
                case Static s:
                    ref StaticTiles staticData = ref Client.Game.UO.FileManager.TileData.StaticData[s.OriginalGraphic];
                    objHeight = staticData.Height;
                    isBlocker = staticData.IsImpassable || staticData.IsWall;
                    break;
                case Item i:
                    objHeight = i.ItemData.Height;
                    isBlocker = i.ItemData.IsImpassable;
                    break;
                case Multi m:
                    objHeight = m.ItemData.Height;
                    isBlocker = m.ItemData.IsImpassable;
                    break;
                default:
                    return false;
            }

            if (!isBlocker)
                return false;

            int objTop = objZ + objHeight;

            int losMin = Math.Min(losMinZ, losMaxZ);
            int losMax = Math.Max(losMinZ, losMaxZ);

            if (objTop > losMin && objZ < losMax)
                return true;

            return false;
        }

        public static readonly ObjectPool<List<GameObject>> _listPool = new ObjectPool<List<GameObject>>(
            () => new List<GameObject>(),
            list => list.Clear(),
            100
        );

        public static List<GameObject> GetAllObjectsAt(int x, int y)
        {
            List<GameObject> result = _listPool.Get();
            GameObject tile = Client.Game.UO.World.Map.GetTile(x, y, false);
            if (tile == null)
                return result;

            GameObject obj = tile;
            while (obj.TPrevious != null)
                obj = obj.TPrevious;
            for (; obj != null; obj = obj.TNext)
                result.Add(obj);

            return result;
        }

        private bool CreateItemList(List<PathObject> list, int x, int y, int stepState)
        {
            GameObject tile = _world.Map.GetTile(x, y, false);

            if (tile == null)
            {
                return false;
            }

            bool ignoreGameCharacters = ProfileManager.CurrentProfile.IgnoreStaminaCheck || stepState == (int)PATH_STEP_STATE.PSS_DEAD_OR_GM || _world.Player.IgnoreCharacters || !(_world.Player.Stamina < _world.Player.StaminaMax && _world.Map.Index == 0);

            bool isGM = _world.Player.Graphic == 0x03DB;

            GameObject obj = tile;

            while (obj.TPrevious != null)
            {
                obj = obj.TPrevious;
            }

            for (; obj != null; obj = obj.TNext)
            {
                if (_world.CustomHouseManager != null && obj.Z < _world.Player.Z)
                {
                    continue;
                }

                ushort graphicHelper = obj.Graphic;

                switch (obj)
                {
                    case Land tile1:

                        if (graphicHelper < 0x01AE && graphicHelper != 2 || graphicHelper > 0x01B5 && graphicHelper != 0x01DB)
                        {
                            uint flags = (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE;

                            if (stepState == (int)PATH_STEP_STATE.PSS_ON_SEA_HORSE)
                            {
                                if (tile1.TileData.IsWet)
                                {
                                    flags = (uint)(PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE | PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
                                }
                            }
                            else
                            {
                                if (!tile1.TileData.IsImpassable)
                                {
                                    flags = (uint)(PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE | PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
                                }

                                if (stepState == (int)PATH_STEP_STATE.PSS_FLYING && tile1.TileData.IsNoDiagonal)
                                {
                                    flags |= (uint)PATH_OBJECT_FLAGS.POF_NO_DIAGONAL;
                                }
                            }

                            int landMinZ = tile1.MinZ;
                            int landAverageZ = tile1.AverageZ;
                            int landHeight = landAverageZ - landMinZ;

                            // TODO: Investigate reducing PathObject allocations here and below
                            list.Add
                            (
                                PathObject.Get
                                (
                                    flags,
                                    landMinZ,
                                    landAverageZ,
                                    landHeight,
                                    obj
                                )
                            );
                        }

                        break;

                    case GameEffect _: break;

                    default:
                        bool canBeAdd = true;
                        bool dropFlags = false;

                        switch (obj)
                        {
                            case Mobile mobile:
                                {
                                    if (!ignoreGameCharacters && !mobile.IsDead && !mobile.IgnoreCharacters)
                                    {
                                        list.Add
                                        (
                                            PathObject.Get
                                            (
                                                (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE,
                                                mobile.Z,
                                                mobile.Z + Constants.DEFAULT_CHARACTER_HEIGHT,
                                                Constants.DEFAULT_CHARACTER_HEIGHT,
                                                mobile
                                            )
                                        );
                                    }

                                    canBeAdd = false;

                                    break;
                                }

                            case Item item when item.IsMulti || item.ItemData.IsInternal:
                                {
                                    //canBeAdd = false;

                                    break;
                                }

                            case Item item2:
                                if (stepState == (int)PATH_STEP_STATE.PSS_DEAD_OR_GM && (item2.ItemData.IsDoor || item2.ItemData.Weight <= 0x5A || isGM && !item2.IsLocked))
                                {
                                    dropFlags = true;
                                }
                                else if (ProfileManager.CurrentProfile.SmoothDoors && item2.ItemData.IsDoor
                                    && (ProfileManager.CurrentProfile.AutoOpenDoorsIfHidden || !_world.Player.IsHidden))
                                {
                                    dropFlags = true;
                                }
                                else
                                {
                                    dropFlags = graphicHelper >= 0x3946 && graphicHelper <= 0x3964 || graphicHelper == 0x0082;
                                }

                                break;

                            case Multi m:

                                if ((_world.CustomHouseManager != null && m.IsCustom && (m.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0) || m.IsHousePreview)
                                {
                                    canBeAdd = false;
                                }

                                if ((m.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER) != 0)
                                {
                                    dropFlags = true;
                                }

                                break;
                        }

                        if (canBeAdd)
                        {
                            uint flags = 0;

                            if (!(obj is Mobile))
                            {
                                ushort graphic = obj is Item it && it.IsMulti ? it.MultiGraphic : obj.Graphic;
                                ref StaticTiles itemdata = ref Client.Game.UO.FileManager.TileData.StaticData[graphic];

                                if (stepState == (int)PATH_STEP_STATE.PSS_ON_SEA_HORSE)
                                {
                                    if (itemdata.IsWet)
                                    {
                                        flags = (uint)(PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
                                    }
                                }
                                else
                                {
                                    if (itemdata.IsImpassable || itemdata.IsSurface)
                                    {
                                        flags = (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE;
                                    }

                                    if (!itemdata.IsImpassable)
                                    {
                                        if (itemdata.IsSurface)
                                        {
                                            flags |= (uint)PATH_OBJECT_FLAGS.POF_SURFACE;
                                        }

                                        if (itemdata.IsBridge)
                                        {
                                            flags |= (uint)PATH_OBJECT_FLAGS.POF_BRIDGE;
                                        }
                                    }

                                    if (stepState == (int)PATH_STEP_STATE.PSS_DEAD_OR_GM)
                                    {
                                        if (graphicHelper <= 0x0846)
                                        {
                                            if (!(graphicHelper != 0x0846 && graphicHelper != 0x0692 && (graphicHelper <= 0x06F4 || graphicHelper > 0x06F6)))
                                            {
                                                dropFlags = true;
                                            }
                                        }
                                        else if (graphicHelper == 0x0873)
                                        {
                                            dropFlags = true;
                                        }
                                    }

                                    if (dropFlags)
                                    {
                                        flags &= 0xFFFFFFFE;
                                    }

                                    if (stepState == (int)PATH_STEP_STATE.PSS_FLYING && itemdata.IsNoDiagonal)
                                    {
                                        flags |= (uint)PATH_OBJECT_FLAGS.POF_NO_DIAGONAL;
                                    }
                                }

                                if (flags != 0)
                                {
                                    int objZ = obj.Z;
                                    int staticHeight = itemdata.Height;
                                    int staticAverageZ = staticHeight;

                                    if (itemdata.IsBridge)
                                    {
                                        staticAverageZ /= 2;
                                        // revert fix from fwiffo because it causes unwalkable stairs [down --> up]
                                        //staticAverageZ += staticHeight % 2;
                                    }

                                    list.Add
                                    (
                                        PathObject.Get
                                        (
                                            flags,
                                            objZ,
                                            staticAverageZ + objZ,
                                            staticHeight,
                                            obj
                                        )
                                    );
                                }
                            }
                        }

                        break;
                }
            }

            return list.Count != 0;
        }

        private int CalculateMinMaxZ
        (
            ref int minZ,
            ref int maxZ,
            int newX,
            int newY,
            int currentZ,
            int newDirection,
            int stepState
        )
        {
            minZ = -128;
            maxZ = currentZ;
            newDirection &= 7;
            int direction = newDirection ^ 4;
            newX += _offsetX[direction];
            newY += _offsetY[direction];

            for (int i = 0; i < _reusableList.Count; i++)
            {
                _reusableList[i]?.Return();
            }

            _reusableList.Clear();

            if (!CreateItemList(_reusableList, newX, newY, stepState) || _reusableList.Count == 0)
            {
                return 0;
            }

            foreach (PathObject obj in _reusableList)
            {
                GameObject o = obj.Object;
                int averageZ = obj.AverageZ;

                if (averageZ <= currentZ && o is Land tile && tile.IsStretched)
                {
                    int avgZ = tile.CalculateCurrentAverageZ(newDirection);

                    if (minZ < avgZ)
                    {
                        minZ = avgZ;
                    }

                    if (maxZ < avgZ)
                    {
                        maxZ = avgZ;
                    }
                }
                else
                {
                    if ((obj.Flags & (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE) != 0 && averageZ <= currentZ && minZ < averageZ)
                    {
                        minZ = averageZ;
                    }

                    if ((obj.Flags & (uint)PATH_OBJECT_FLAGS.POF_BRIDGE) != 0 && currentZ == averageZ)
                    {
                        int z = obj.Z;
                        int height = z + obj.Height;

                        if (maxZ < height)
                        {
                            maxZ = height;
                        }

                        if (minZ > z)
                        {
                            minZ = z;
                        }
                    }
                }
            }

            maxZ += 2;

            return maxZ;
        }

        public bool CalculateNewZ(int x, int y, ref sbyte z, int direction)
        {
            int stepState = (int)PATH_STEP_STATE.PSS_NORMAL;

            if (_world.Player.IsDead || _world.Player.Graphic == 0x03DB)
            {
                stepState = (int)PATH_STEP_STATE.PSS_DEAD_OR_GM;
            }
            else
            {
                if (_world.Player.IsGargoyle && _world.Player.IsFlying)
                    stepState = (int)PATH_STEP_STATE.PSS_FLYING;
                else
                {
                    Item mount = _world.Player.FindItemByLayer(Layer.Mount);

                    if (mount != null && mount.Graphic == 0x3EB3) // sea horse
                        stepState = (int)PATH_STEP_STATE.PSS_ON_SEA_HORSE;
                }
            }

            int minZ = -128;
            int maxZ = z;

            CalculateMinMaxZ
            (
                ref minZ,
                ref maxZ,
                x,
                y,
                z,
                direction,
                stepState
            );

            foreach (PathObject o in _reusableList) o.Return();
            _reusableList.Clear();

            if (_world.CustomHouseManager != null)
            {
                var rect = new Rectangle(_world.CustomHouseManager.StartPos.X, _world.CustomHouseManager.StartPos.Y, _world.CustomHouseManager.EndPos.X, _world.CustomHouseManager.EndPos.Y);

                if (!rect.Contains(x, y))
                {
                    return false;
                }
            }

            if (!CreateItemList(_reusableList, x, y, stepState) || _reusableList.Count == 0)
            {
                return false;
            }

            _reusableList.Sort();

            var pathObj = PathObject.Get(
                (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE,
                128,
                128,
                128,
                null
            );

            if(pathObj != null)
                _reusableList.Add(pathObj);

            int resultZ = -128;

            if (z < minZ)
            {
                z = (sbyte)minZ;
            }

            int currentTempObjZ = 1000000;
            int currentZ = -128;

            for (int i = 0; i < _reusableList.Count; i++)
            {
                PathObject obj = _reusableList[i];

                if ((obj.Flags & (uint)PATH_OBJECT_FLAGS.POF_NO_DIAGONAL) != 0 && stepState == (int)PATH_STEP_STATE.PSS_FLYING)
                {
                    int objAverageZ = obj.AverageZ;
                    int delta = Math.Abs(objAverageZ - z);

                    if (delta <= 25)
                    {
                        resultZ = objAverageZ != -128 ? objAverageZ : currentZ;

                        break;
                    }
                }

                if ((obj.Flags & (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE) != 0)
                {
                    int objZ = obj.Z;

                    if (objZ - minZ >= Constants.DEFAULT_BLOCK_HEIGHT)
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            PathObject tempObj = _reusableList[j];

                            if ((tempObj.Flags & (uint)(PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE)) != 0)
                            {
                                int tempAverageZ = tempObj.AverageZ;

                                if (tempAverageZ >= currentZ && objZ - tempAverageZ >= Constants.DEFAULT_BLOCK_HEIGHT && (tempAverageZ <= maxZ && (tempObj.Flags & (uint)PATH_OBJECT_FLAGS.POF_SURFACE) != 0 || (tempObj.Flags & (uint)PATH_OBJECT_FLAGS.POF_BRIDGE) != 0 && tempObj.Z <= maxZ))
                                {
                                    int delta = Math.Abs(z - tempAverageZ);

                                    if (delta < currentTempObjZ)
                                    {
                                        currentTempObjZ = delta;
                                        resultZ = tempAverageZ;
                                    }
                                }
                            }
                        }
                    }

                    int averageZ = obj.AverageZ;

                    if (minZ < averageZ)
                    {
                        minZ = averageZ;
                    }

                    if (currentZ < averageZ)
                    {
                        currentZ = averageZ;
                    }
                }
            }

            z = (sbyte)resultZ;

            return resultZ != -128;
        }

        public static void GetNewXY(byte direction, ref int x, ref int y)
        {
            switch (direction & 7)
            {
                case 0:

                    {
                        y--;

                        break;
                    }

                case 1:

                    {
                        x++;
                        y--;

                        break;
                    }

                case 2:

                    {
                        x++;

                        break;
                    }

                case 3:

                    {
                        x++;
                        y++;

                        break;
                    }

                case 4:

                    {
                        y++;

                        break;
                    }

                case 5:

                    {
                        x--;
                        y++;

                        break;
                    }

                case 6:

                    {
                        x--;

                        break;
                    }

                case 7:

                    {
                        x--;
                        y--;

                        break;
                    }
            }
        }

        public bool CanWalk(ref Direction direction, ref int x, ref int y, ref sbyte z, bool dontChangeXY = false)
        {
            int newX = x;
            int newY = y;
            sbyte newZ = z;
            byte newDirection = (byte)direction;
            if(!dontChangeXY) // if we dont want to change the xy, we can just use the current xy and direction
                GetNewXY((byte)direction, ref newX, ref newY);
            bool passed = CalculateNewZ(newX, newY, ref newZ, (byte)direction);

            if ((sbyte)direction % 2 != 0)
            {
                if (passed)
                {
                    for (int i = 0; i < 2 && passed; i++)
                    {
                        int testX = x;
                        int testY = y;
                        sbyte testZ = z;
                        byte testDir = (byte)(((byte)direction + _dirOffset[i]) % 8);
                        GetNewXY(testDir, ref testX, ref testY);
                        passed = CalculateNewZ(testX, testY, ref testZ, testDir);
                    }
                }

                if (!passed)
                {
                    for (int i = 0; i < 2 && !passed; i++)
                    {
                        newX = x;
                        newY = y;
                        newZ = z;
                        newDirection = (byte)(((byte)direction + _dirOffset[i]) % 8);
                        GetNewXY(newDirection, ref newX, ref newY);
                        passed = CalculateNewZ(newX, newY, ref newZ, newDirection);
                    }
                }
            }

            if (passed)
            {
                x = newX;
                y = newY;
                z = newZ;
                direction = (Direction)newDirection;
            }

            return passed;
        }

        private int GetGoalDistCost(Point point, int cost) =>
            //return (Math.Abs(_endPoint.X - point.X) + Math.Abs(_endPoint.Y - point.Y)) * cost;
            Math.Max(Math.Abs(_endPoint.X - point.X), Math.Abs(_endPoint.Y - point.Y));

        /// <summary>
        /// True when the tile holds a multi component that blocks movement (a house wall or
        /// other impassable piece). Floors/roofs are walkable surfaces and don't count, and
        /// house-preview pieces are ignored. Memoized per search via <see cref="_blockingMultiCache"/>.
        /// </summary>
        private bool TileHasBlockingMulti(int x, int y)
        {
            (int x, int y) key = (x, y);

            if (_blockingMultiCache.TryGetValue(key, out bool cached))
            {
                return cached;
            }

            bool result = false;
            GameObject tile = _world.Map.GetTile(x, y, false);

            if (tile != null)
            {
                GameObject obj = tile;

                while (obj.TPrevious != null)
                {
                    obj = obj.TPrevious;
                }

                for (; obj != null; obj = obj.TNext)
                {
                    if (obj is Multi m && !m.IsHousePreview && (m.ItemData.IsImpassable || m.ItemData.IsWall))
                    {
                        result = true;
                        break;
                    }
                }
            }

            _blockingMultiCache[key] = result;
            return result;
        }

        /// <summary>
        /// True when any of the eight tiles surrounding (x, y) holds a blocking multi, i.e. the
        /// tile sits within a 1-tile buffer around a house. Used to add a soft avoidance cost.
        /// </summary>
        private bool IsWithinMultiBuffer(int x, int y)
        {
            for (int i = 0; i < 8; i++)
            {
                if (TileHasBlockingMulti(x + _offsetX[i], y + _offsetY[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetTurnPenalty(PathNode parent, int direction) =>
            // The turn penalty prevents unnecessary zig-zagging that takes extra
            // time (turning pauses movement briefly) and makes the movement look
            // more natural. The turn penalty could be tweaked to a float value
            // less than 1, e.g. 0.5, to make the avoidance of turns less
            // aggressive, if needed.
            (parent.Parent != null && parent.Direction != direction) ? 1 : 0;

        private bool AddNodeToList(int direction, int x, int y, int z, PathNode parent, int cost)
        {
            (int x, int y, int z) coordinate = (x, y, z);
            if (_closedSet.ContainsKey(coordinate))
            {
                return false;
            }

            // In terms of the distance (number of tile steps) of the final
            // reconstructed path, simply adding a turn penalty in this manner
            // will result in paths no worse than without the turn cost. However,
            // to guarantee that the minimal number of turns are taken, the open
            // and closed sets would need to key off of (x, y, z, direction) to
            // account for arriving at a tile from every direction. Doing this would
            // be up to an 8x increase in time and space, and this would only be
            // a problem where the cost of one step varies between tiles e.g. if
            // walking through mud tiles cost 2 instead of 1, but that's not a
            // concern here to warrent the 8x cost.
            int turnPenalty = GetTurnPenalty(parent, direction);
            int newDistFromStart = parent.DistFromStartCost + cost + Math.Abs(z - parent.Z) + turnPenalty;

            // Soft 1-tile buffer around houses: nudge the search away from tiles that border an
            // impassable multi so paths don't scrape along house walls. It's a cost, not a block,
            // so tight town streets and destinations right next to a house stay reachable.
            if (_multiBufferPenalty > 0 && IsWithinMultiBuffer(x, y))
            {
                newDistFromStart += _multiBufferPenalty;
            }

            var updatedNode = PathNode.Get();

            updatedNode.X = x;
            updatedNode.Y = y;
            updatedNode.Z = z;
            updatedNode.Direction = direction;
            updatedNode.Parent = parent;
            updatedNode.DistFromStartCost = newDistFromStart;
            updatedNode.DistFromGoalCost = GetGoalDistCost(new Point(x, y), cost);
            updatedNode.Cost = updatedNode.DistFromStartCost + updatedNode.DistFromGoalCost;

            if (_openSet.Contains(coordinate))
            {
                // Since tile is already in the open list, we enqueue the better option that
                // has a lower cost (existing one will be ignored later by PriorityQueue impl)

                _openSet.Enqueue(updatedNode);
                return false;
            }

            _openSet.Enqueue(updatedNode);

            if (MathHelper.GetDistance(_endPoint, new Point(x, y)) <= _pathfindDistance &&
                Math.Abs(_endPointZ - z) < _zLevelDiff)
            {
                _goalNode = updatedNode;
            }

            return true;

        }

        private bool OpenNodes(PathNode node)
        {
            bool found = false;

            for (int i = 0; i < 8; i++)
            {
                var direction = (Direction)i;
                int x = node.X;
                int y = node.Y;
                sbyte z = (sbyte)node.Z;
                Direction oldDirection = direction;

                if (CanWalk(ref direction, ref x, ref y, ref z))
                {
                    if (direction != oldDirection)
                    {
                        continue;
                    }

                    int diagonal = i % 2;

                    if (diagonal != 0)
                    {
                        var wantDirection = (Direction)i;
                        int wantX = node.X;
                        int wantY = node.Y;
                        GetNewXY((byte)wantDirection, ref wantX, ref wantY);

                        if (x != wantX || y != wantY)
                        {
                            diagonal = -1;
                        }
                    }

                    if (diagonal >= 0)
                    {
                        int cost = (diagonal == 0) ? 1 : 2;

                        if (AddNodeToList((int)direction, x, y, z, node, cost))
                        {
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        private static PathNode FindCheapestNode()
        {
            while (!_openSet.IsEmpty())
            {
                PathNode node = _openSet.Dequeue();
                if (node == null) continue;

                (int X, int Y, int Z) key = (node.X, node.Y, node.Z);

                if (_closedSet.ContainsKey(key))
                {
                    // TODO: Maybe just remove this conditional.
                    // If it happens, there is a bug that needs to be fixed.
                    Log.Warn("[Pathfinder]Node in both open and closed set. This shouldn't happen.");
                    continue;
                }

                _closedSet[key] = node;
                return node;
            }

            return null;
        }

        private bool FindPath(int maxNodes, bool ignoreAutowalkState)
        {
            var startNode = PathNode.Get();

            startNode.X = _startPoint.X;
            startNode.Y = _startPoint.Y;
            startNode.Z = _world.Player.Z;
            startNode.Parent = null;
            startNode.DistFromStartCost = 0;

            var startPoint = new Point(_startPoint.X, _startPoint.Y);
            startNode.DistFromGoalCost = GetGoalDistCost(startPoint, 0);
            startNode.Cost = startNode.DistFromGoalCost;

            _openSet.Enqueue(startNode);

            int closedNodesCount = 0;

            if (startNode.DistFromGoalCost > 14)
            {
                _run = true;
            }

            while (ignoreAutowalkState || AutoWalking)
            {
                PathNode currentNode = FindCheapestNode();

                if (currentNode == null)
                {
                    return false;
                }

                closedNodesCount++;

                if (closedNodesCount >= maxNodes)
                {
                    break;
                }

                if (_goalNode is not null)
                {
                    ReconstructPath(_goalNode);
                    return true;
                }

                OpenNodes(currentNode);
            }

            return false;
        }

        private void ReconstructPath(PathNode goalNode)
        {
            var pathStack = new Stack<PathNode>();
            PathNode current = goalNode;
            var visited = new HashSet<PathNode>();
            int iterations = 0;

            while (current is not null && current.Parent != current && iterations < PATHFINDER_MAX_NODES)
            {
                // Check for cycles
                if (visited.Contains(current))
                {
                    // Cycle detected - break out
                    Log.Warn("[Pathfinder]Cycle detected in path reconstruction!");
                    break;
                }

                visited.Add(current);
                pathStack.Push(current);
                current = current.Parent;
                iterations++;
            }

            if (iterations >= PATHFINDER_MAX_NODES)
            {
                Log.Warn($"[Pathfinder]Path reconstruction hit iteration limit: {PATHFINDER_MAX_NODES}");
            }

            _path.Clear();
            while (pathStack.Count > 0)
            {
                _path.Add(pathStack.Pop());
            }
        }

        /// <summary>
        /// Loads a pre-computed path (from WorldMapPathfinder) into the walker and starts walking.
        /// Must be called on the main thread.
        /// Each point is (x, y, z, direction) for a single step.
        /// </summary>
        public void StartComputedPath(IReadOnlyList<(int X, int Y, int Z, int Direction)> points, bool run = true)
        {
            if (_world.Player == null || _world.Player.IsParalyzed || points == null || points.Count == 0)
                return;

            CleanupPathfinding();
            _pointIndex = 0;
            _goalNode = null;
            _run = run;
            _startPoint.X = _world.Player.X;
            _startPoint.Y = _world.Player.Y;

            // Prepend player's current tile as _path[0] so _path[1] is the first real step —
            // matching the convention used by WalkTo where _pointIndex starts at 1.
            var startNode = PathNode.Get();
            startNode.X = _world.Player.X;
            startNode.Y = _world.Player.Y;
            startNode.Z = _world.Player.Z;
            startNode.Direction = (int)_world.Player.Direction;
            startNode.IsValid = true;
            _path.Add(startNode);

            foreach (var p in points)
            {
                var node = PathNode.Get();
                node.X = p.X;
                node.Y = p.Y;
                node.Z = p.Z;
                node.Direction = p.Direction;
                node.IsValid = true;
                _path.Add(node);
            }

            // These nodes never enter the open/closed sets, so CleanupPathfinding
            // would otherwise leak them. Mark _path as owning pooled nodes.
            _ownsPathNodes = true;

            if (_path.Count > 1)
            {
                _endPoint.X = _path[_path.Count - 1].X;
                _endPoint.Y = _path[_path.Count - 1].Y;
                _endPointZ = _path[_path.Count - 1].Z;
                _pointIndex = 1;
                AutoWalking = true;
                _computedPathActive = true;
                ProcessAutoWalk();
            }
        }

        /// <summary>
        /// Appends additional pre-computed steps onto the path currently being walked by
        /// <see cref="StartComputedPath"/>, without restarting the walk. Used by the WorldMap
        /// to chain pathfinding segments (A-&gt;B-&gt;C). Must be called on the main thread.
        /// Returns <c>false</c> when there is no active computed path to extend (the caller
        /// should then start a fresh path instead).
        /// </summary>
        public bool AppendComputedPath(IReadOnlyList<(int X, int Y, int Z, int Direction)> points)
        {
            if (!_computedPathActive || !AutoWalking || points == null || points.Count == 0)
                return false;

            foreach (var p in points)
            {
                var node = PathNode.Get();
                node.X = p.X;
                node.Y = p.Y;
                node.Z = p.Z;
                node.Direction = p.Direction;
                node.IsValid = true;
                _path.Add(node);
            }

            // The appended nodes are owned by _path just like the originals (StartComputedPath
            // already set _ownsPathNodes), so CleanupPathfinding will return them to the pool.
            _endPoint.X = _path[_path.Count - 1].X;
            _endPoint.Y = _path[_path.Count - 1].Y;
            _endPointZ = _path[_path.Count - 1].Z;

            // Nudge the walker in case it had already caught up to the previous end and idled.
            ProcessAutoWalk();
            return true;
        }

        public List<(int X, int Y, int Z)> GetPathTo(int x, int y, int z, int distance)
        {
            _zLevelDiff = ProfileManager.CurrentProfile.PathfindingZLevelDiff;
            _multiBufferPenalty = ProfileManager.CurrentProfile.PathfindingMultiBuffer;

            CleanupPathfinding();
            _pointIndex = 0;
            _goalNode = null;
            _run = false;
            _startPoint.X = _world.Player.X;
            _startPoint.Y = _world.Player.Y;
            _endPoint.X = x;
            _endPoint.Y = y;
            _endPointZ = z;
            _pathfindDistance = distance;

            if (!FindPath(MaxNodes, ignoreAutowalkState: true))
            {
                return null;
            }

            var result = new List<(int X, int Y, int Z)>(_path.Count);

            foreach (PathNode node in _path)
            {
                result.Add((node.X, node.Y, node.Z));
            }

            return result;
        }

        public bool WalkTo(int x, int y, int z, int distance)
        {
            if (_world.Player == null /*|| World.Player.Stamina == 0*/ || _world.Player.IsParalyzed)
            {
                return false;
            }

            _zLevelDiff = ProfileManager.CurrentProfile.PathfindingZLevelDiff;
            _multiBufferPenalty = ProfileManager.CurrentProfile.PathfindingMultiBuffer;

            EventSink.InvokeOnPathFinding(null, new Vector4(x, y, z, distance));

            CleanupPathfinding();
            _pointIndex = 0;
            _goalNode = null;
            _run = false;
            _startPoint.X = _world.Player.X;
            _startPoint.Y = _world.Player.Y;
            _endPoint.X = x;
            _endPoint.Y = y;
            _endPointZ = z;
            _pathfindDistance = distance;
            AutoWalking = true;

            if (FindPath(MaxNodes, ignoreAutowalkState: false))
            {
                _pointIndex = 1;
                ProcessAutoWalk();
            }
            else
            {
                AutoWalking = false;
            }

            return _path.Count != 0;
        }

        /// <summary>
        /// Recomputes the active auto-walk path from the player's current position to the
        /// same target that <see cref="WalkTo"/> was last called with. Intended to be called
        /// when world geometry changes underneath a walk in progress — most notably when a
        /// house/multi is loaded via packet and its (previously absent) components now block
        /// tiles the current path runs through. Re-running the A* search from where the player
        /// stands lets it route around the newly-appeared blocker.
        ///
        /// No-op unless a local A* auto-walk is currently in progress. Computed paths
        /// (WorldMap navigation via <see cref="StartComputedPath"/>) are skipped — they have
        /// their own dynamic-block replanning via <see cref="OnComputedPathStepFailed"/>.
        /// </summary>
        public void RecalculatePath()
        {
            if (!AutoWalking || _computedPathActive || _world.Player == null || _world.Player.IsParalyzed)
            {
                return;
            }

            // Re-run from the player's present position to the previously requested target.
            WalkTo(_endPoint.X, _endPoint.Y, _endPointZ, _pathfindDistance);
        }

        public void ProcessAutoWalk()
        {
            if (AutoWalking && _world.InGame && _world.Player.Walker.StepsCount < Constants.MAX_STEP_COUNT && _world.Player.Walker.LastStepRequestTime <= Time.Ticks)
            {
                if (_pointIndex >= 0 && _pointIndex < _path.Count)
                {
                    PathNode p = _path[_pointIndex];

                    _world.Player.GetEndPosition(out int x, out int y, out sbyte z, out Direction dir);

                    if (dir == (Direction)p.Direction)
                    {
                        _pointIndex++;
                    }

                    if (!_world.Player.Walk((Direction)p.Direction, _run))
                    {
                        // For computed paths (WorldMap nav), give the pathfinder a chance to replan
                        // around the blocked tile — likely a dynamic item not in statics.mul.
                        // Tear down first so the hook can safely issue a new StartComputedPath.
                        if (_computedPathActive && OnComputedPathStepFailed != null)
                        {
                            var hook = OnComputedPathStepFailed;
                            int blockedX = p.X;
                            int blockedY = p.Y;
                            StopAutoWalk();
                            hook.Invoke(blockedX, blockedY);
                        }
                        else
                        {
                            StopAutoWalk();
                        }
                    }
                }
                else
                {
                    StopAutoWalk();
                }
            }
        }

        public void StopAutoWalk()
        {
            AutoWalking = false;
            _run = false;
            _computedPathActive = false;
            CleanupPathfinding();
        }

        private static void CleanupPathfinding()
        {
            // Clean up any remaining nodes in the open set
            while (!_openSet.IsEmpty())
            {
                PathNode node = _openSet.Dequeue();
                node?.Return();
            }

            _openSet.Clear();

            // Clean up any remaining nodes in the closed set
            foreach (KeyValuePair<(int x, int y, int z), PathNode> n in _closedSet)
            {
                n.Value.Return();
            }

            _closedSet.Clear();

            // Computed paths (StartComputedPath) own their pooled nodes directly because
            // they never pass through the closed set. Return them here. Normal A* paths
            // share their nodes with _closedSet (already returned above), so skip them.
            if (_ownsPathNodes)
            {
                foreach (PathNode node in _path)
                {
                    node?.Return();
                }

                _ownsPathNodes = false;
            }

            _path.Clear();
            _goalNode = null;
            _blockingMultiCache.Clear();
        }

        private enum PATH_STEP_STATE
        {
            PSS_NORMAL = 0,
            PSS_DEAD_OR_GM,
            PSS_ON_SEA_HORSE,
            PSS_FLYING
        }

        [Flags]
        private enum PATH_OBJECT_FLAGS : uint
        {
            POF_IMPASSABLE_OR_SURFACE = 0x00000001,
            POF_SURFACE = 0x00000002,
            POF_BRIDGE = 0x00000004,
            POF_NO_DIAGONAL = 0x00000008
        }

        private class PathObject : IComparable<PathObject>
        {
            private static ObjectPool<PathObject> _pool = new ObjectPool<PathObject>(
                ()=> new PathObject(0, 0, 0, 0, null), (po) =>
                {
                    po.Flags = 0;
                    po.Z = 0;
                    po.AverageZ = 0;
                    po.Height = 0;
                    po.Object = null;
                },
                15
                );
            private PathObject(uint flags, int z, int avgZ, int h, GameObject obj)
            {
                Flags = flags;
                Z = z;
                AverageZ = avgZ;
                Height = h;
                Object = obj;
            }

            public static PathObject Get(uint flags, int z, int avgZ, int h, GameObject obj)
            {
                PathObject po = _pool.Get();
                po.Flags = flags;
                po.Z = z;
                po.AverageZ = avgZ;
                po.Height = h;
                po.Object = obj;
                return po;
            }

            public void Return() => _pool.Return(this);

            public uint Flags { get; private set; }

            public int Z { get; private set; }

            public int AverageZ { get; private set; }

            public int Height { get; private set; }

            public GameObject Object { get; private set; }

            public int CompareTo(PathObject other)
            {
                int comparision = Z - other.Z;

                if (comparision == 0)
                {
                    comparision = Height - other.Height;
                }

                return comparision;
            }
        }

        private class PathNode
        {
            private static ObjectPool<PathNode> _pool = new(
                ()=>new PathNode(),
                (pn) => {pn.Reset();},
                15
                );

            private PathNode()
            {
            }

            public static PathNode Get() => _pool.Get();

            public void Return() => _pool.Return(this);

            public bool IsValid { get; set; }

            public int X { get; set; }

            public int Y { get; set; }

            public int Z { get; set; }

            public int Direction { get; set; }

            public bool Used { get; set; }

            public int Cost { get; set; }

            public int DistFromStartCost { get; set; }

            public int DistFromGoalCost { get; set; }

            public PathNode Parent { get; set; }

            public void Reset()
            {
                Parent = null;
                Used = IsValid = false;
                X = Y = Z = Direction = Cost = DistFromGoalCost = DistFromStartCost = 0;
            }
        }

        class PriorityQueue
        {
            readonly List<PathNode> _heap = new();
            readonly Dictionary<(int, int, int), PathNode> _lookup = new();
            readonly object _lock = new();

            internal bool Contains((int, int, int) coordinate)
            {
                lock (_lock)
                {
                    if (_lookup.TryGetValue(coordinate, out PathNode existing))
                    {
                        // The priority queue lazily remove duplicates, so we check
                        // whether the node is valid here.
                        return existing.IsValid;
                    }

                    return false;
                }
            }

            internal void Clear()
            {
                lock (_lock)
                {
                    _heap.Clear();
                    _lookup.Clear();
                }
            }

            internal bool IsEmpty()
            {
                lock (_lock)
                {
                    while (_heap.Count > 0)
                    {
                        // The priority queue lazily remove duplicates, so we check
                        // for them here. If should be removed lazily, remove it now
                        // and continue to next element.
                        if (_heap[0].IsValid) return false;

                        RemoveAt(0);
                    }

                    return true;
                }
            }

            internal void Enqueue(PathNode node)
            {
                lock (_lock)
                {
                    (int, int, int) key = GetKey(node);
                    if (_lookup.TryGetValue(key, out PathNode existing))
                    {
                        if (existing.IsValid && existing.Cost <= node.Cost)
                        {
                            // Existing priority is better or equal, so ignore this node.

                            // While it breaks encapsulation to perform this inside
                            // the priority queue, it is safe to return this node
                            // to the object pool early at this point because we know
                            // the caller will discard its reference to this node, so
                            // it cannot be used later during path reconstruction.
                            node.Return();

                            return;
                        }

                        // The priority queue lazily remove duplicates, so we mark existing to be deleted later.
                        existing.IsValid = false;
                    }

                    node.IsValid = true;
                    _lookup[key] = node;
                    _heap.Add(node);
                    int index = _heap.Count - 1;
                    HeapifyUp(index);
                }
            }

            internal PathNode Dequeue()
            {
                lock (_lock)
                {
                    while (_heap.Count > 0)
                    {
                        // The priority queue lazily remove duplicates, so we check
                        // for them here. If should be removed lazily, remove it now
                        // and continue to next element.
                        PathNode top = _heap[0];
                        if (!top.IsValid)
                        {
                            RemoveAt(0);
                            continue;
                        }

                        RemoveAt(0);
                        return top;
                    }

                    return null;
                }
            }

            void Swap(int i, int j) => (_heap[j], _heap[i]) = (_heap[i], _heap[j]);

            void HeapifyUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (_heap[index].Cost < _heap[parent].Cost)
                    {
                        Swap(index, parent);
                        index = parent;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            void HeapifyDown(int index)
            {
                int lastIndex = _heap.Count - 1;
                while (true)
                {
                    int left = index * 2 + 1;
                    int right = index * 2 + 2;
                    int smallest = index;

                    if (left <= lastIndex && _heap[left].Cost < _heap[smallest].Cost)
                    {
                        smallest = left;
                    }

                    if (right <= lastIndex && _heap[right].Cost < _heap[smallest].Cost)
                    {
                        smallest = right;
                    }

                    if (smallest != index)
                    {
                        Swap(index, smallest);
                        index = smallest;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            (int, int, int) GetKey(PathNode node) => (node.X, node.Y, node.Z);

            void RemoveAt(int index)
            {
                PathNode node = _heap[index];
                (int, int, int) key = GetKey(node);
                _lookup.Remove(key);

                int lastIndex = _heap.Count - 1;
                if (index != lastIndex)
                {
                    Swap(index, lastIndex);
                }

                _heap.RemoveAt(lastIndex);

                if (index < _heap.Count)
                {
                    HeapifyDown(index);
                    HeapifyUp(index);
                }

                if (!node.IsValid)
                {
                    // While it breaks encapsulation to perform this inside
                    // the priority queue, it is safe to return the invalid
                    // node to the object pool early at this point because
                    // we know no other data structures reference it any
                    // longer, so it won't be used in any subsequent
                    // pathfinding calculations nor in path reconstruction.
                    node.Return();
                }
            }
        }
    }
}
