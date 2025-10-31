using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Game;

namespace ClassicUO.Game.GameObjects
{
    public static class LineOfSightHelper
    {
        private const int TERRAIN_STEEP_RISE_THRESHOLD = 14;
        private const int TERRAIN_BLOCKING_THRESHOLD = 10;
        private const int TERRAIN_SAME_HEIGHT_THRESHOLD = 8;
        private const int MOBILE_EYE_HEIGHT = 14;

        public static bool IsVisible(GameObject observer, GameObject target)
        {
            if (observer == null || target == null)
                return false;

            if (observer.X == target.X && observer.Y == target.Y && observer.Z == target.Z)
                return true;

            List<Point3D> coords = CoordsToTarget(observer, target);

            return CheckCoords(observer, target, coords);
        }

        private static List<Point3D> CoordsToTarget(GameObject observer, GameObject target)
        {
            var coords = new List<Point3D>();
            int x0 = observer.X, y0 = observer.Y;
            int x1 = target.X, y1 = target.Y;

            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                int z = GetLandZ(x0, y0);
                coords.Add(new Point3D(x0, y0, z));

                if (x0 == x1 && y0 == y1)
                    break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return coords;
        }

        private static bool CheckCoords(GameObject observer, GameObject target, List<Point3D> coords)
        {
            List<int> zlist = new();

            int observerEyeZ = observer.Z + MOBILE_EYE_HEIGHT;
            int targetEyeZ = target.Z + MOBILE_EYE_HEIGHT;

            foreach (Point3D coord in coords)
            {
                zlist.Add(coord.Z);
                if (!CheckTile(coord.X, coord.Y, observerEyeZ, targetEyeZ))
                    return false;
            }

            if (!Terrain(observer, target, zlist))
                return false;
            return true;
        }

        private static bool CheckTile(int x, int y, int observerEyeZ, int targetEyeZ)
        {
            int losMinZ = Math.Min(observerEyeZ, targetEyeZ);
            int losMaxZ = Math.Max(observerEyeZ, targetEyeZ);

            List<GameObject> objects = Pathfinder.GetAllObjectsAt(x, y);
            try
            {
                foreach (GameObject obj in objects)
                {
                    if (Pathfinder.ObjectBlocksLOS(obj, losMinZ, losMaxZ))
                        return false;
                }
                return true;
            }
            finally
            {
                Pathfinder._listPool.Return(objects);
            }
        }

        private static int GetLandZ(int x, int y)
        {
            GameObject tile = Client.Game.UO.World.Map.GetTile(x, y, false);
            if (tile is Land land)
                return land.Z;
            return 0;
        }

        private static bool Terrain(GameObject observer, GameObject target, List<int> zlist)
        {
            if (zlist.Count == 0)
                return true;

            int playerZ = observer.Z;
            int mobileZ = target.Z;

            if (playerZ > mobileZ)
            {
                int altitude = playerZ - mobileZ;
                int steps = zlist.Count == 0 ? 0 : altitude / zlist.Count;
                for (int count = 0; count < zlist.Count; count++)
                {
                    int acceptable = mobileZ + (steps * count);
                    if (zlist[count] > acceptable + TERRAIN_STEEP_RISE_THRESHOLD)
                        return false;
                }
            }
            else if (playerZ < mobileZ)
            {
                int altitude = mobileZ - playerZ;
                int steps = zlist.Count == 0 ? 0 : altitude / zlist.Count;
                for (int count = 0; count < zlist.Count; count++)
                {
                    int acceptable = mobileZ - (steps * count);
                    if (zlist[count] > acceptable + TERRAIN_BLOCKING_THRESHOLD)
                        return false;
                }
            }
            else
            {
                foreach (int entry in zlist)
                {
                    if (entry != zlist[0] && entry > (playerZ + TERRAIN_SAME_HEIGHT_THRESHOLD))
                        return false;
                }
                return true;
            }
            return true;
        }

        private readonly struct Point3D
        {
            public readonly int X, Y, Z;
            public Point3D(int x, int y, int z)
            {
                X = x; Y = y; Z = z;
            }
        }
    }
}
