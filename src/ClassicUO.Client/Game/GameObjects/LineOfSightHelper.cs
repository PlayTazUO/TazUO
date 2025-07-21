using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Game;

public static class LineOfSightHelper
{
    public static bool IsVisible(GameObject observer, GameObject target)
    {
        if (observer == null || target == null)
            return false;

        if (observer.X == target.X && observer.Y == target.Y && observer.Z == target.Z)
            return true;

        var coords = CoordsToTarget(observer, target);

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

        int observerEyeZ = observer.Z + 14;
        int targetEyeZ = target.Z + 14;

        foreach (var coord in coords)
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

        foreach (var obj in Pathfinder.GetAllObjectsAt(x, y))
        {
            if (Pathfinder.ObjectBlocksLOS(obj, losMinZ, losMaxZ))
                return false;
        }
        return true;
    }

    private static int GetLandZ(int x, int y)
    {
        GameObject tile = World.Map.GetTile(x, y, false);
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
                if (zlist[count] > acceptable + 14)
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
                if (zlist[count] > acceptable + 10)
                    return false;
            }
        }
        else
        {
            foreach (int entry in zlist)
            {
                if (entry != zlist[0] && entry > (playerZ + 8))
                    return false;
            }
            return true;
        }
        return true;
    }

    private struct Point3D
    {
        public int X, Y, Z;
        public Point3D(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }
    }
}
