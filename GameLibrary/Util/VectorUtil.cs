using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameLibrary
{
    public static class VectorUtil
    {

        public static float GetMaxAxis(Vector3 v)
        {
            float x = Math.Abs(v.X);
            float y = Math.Abs(v.Y);
            float z = Math.Abs(v.Z);

            if (x >= y)
            {
                if (x >= z)
                    return x;
                return z;
            }

            if (y >= z)
                return y;

            return z;
        }

        // Converts a rotation vector into a rotation matrix
        public static Matrix Vector3ToMatrix(Vector3 rotation)
        {
            return Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        }

        // Returns Euler angles that point from one point to another
        public static Vector3 AngleTo(Vector3 from, Vector3 location)
        {
            Vector3 angle = new Vector3();
            Vector3 v3 = Vector3.Normalize(location - from);

            angle.X = (float)Math.Asin(v3.Y);
            angle.Y = (float)Math.Atan2((double)-v3.X, (double)-v3.Z);

            return angle;
        }

        // Create plane from normal and offset along normal
        public static Vector4 CreatePlane(Vector3 normal, float offset)
        {
            return new Vector4(normal, offset);
        }

        // Create plane from three points
        public static Vector4 CreatePlane(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float A, B, C, D;
            A = p1.Y * (p2.Z - p3.Z) + p2.Y * (p3.Z - p1.Z) + p3.Y * (p1.Z - p2.Z);
            B = p1.Z * (p2.X - p3.X) + p2.Z * (p3.X - p1.X) + p3.Z * (p1.X - p2.X);
            C = p1.X * (p2.Y - p3.Y) + p2.X * (p3.Y - p1.Y) + p3.X * (p1.Y - p2.Y);
            D = p1.X * (p2.Y * p3.Z - p3.Y * p2.Z) + p2.X * (p3.Y * p1.Z - p1.Y * p3.Z) + p3.X * (p1.Y * p2.Z - p2.Y * p1.Z);
            D = -D;
            return new Vector4(A, B, C, D);
        }

        public delegate void Permute<T>(T x, T y, T z, out T px, out T py, out T pz);

        //XXXX
        //FIXME need to test that both octree and grid are traversed front to back properly....
        //    XXXX
        public static readonly Permute<int>[] PERMUTATIONS = new Permute<int>[] {
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = x; py = y; pz = z; }, // x y z
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = x; py = z; pz = y; }, // x z y
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = y; py = x; pz = z; }, // y x z
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = y; py = z; pz = x; }, // y z x
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = z; py = x; pz = y; }, // z x y
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = z; py = y; pz = x; }  // z y x
        };

        //delegate (int x, int y, int z, out int px, out int py, out int pz) {  px = z; py = x; pz = y; }, // y z x
            //delegate (int x, int y, int z, out int px, out int py, out int pz) {  px = y; py = z; pz = x; }, // z x y

//delegate (int x, int y, int z, out int px, out int py, out int pz) {  px = y; py = z; pz = x; }, // y z x
        //delegate (int x, int y, int z, out int px, out int py, out int pz) {  px = z; py = x; pz = y; }, // z x y

        public static readonly Permute<int>[] INV_PERMUTATIONS = new Permute<int>[] {
            PERMUTATIONS[0],
            PERMUTATIONS[1],
            PERMUTATIONS[2],
            PERMUTATIONS[4], // !!!
            PERMUTATIONS[3], // !!!
            PERMUTATIONS[5],
        };

// taken from http://www.iquilezles.org/www/articles/volumesort/volumesort.htm
// returns the direction visit order (one of the 48 visit orders between 0 and 47)
// bits 5, 4, 3 give the 6 x,y z permutations
// bits 2, 1, 0 give the the sign bit (0=+, 1=-) for x, y, z respectivly (8 sign permutations)
public static int VisitOrder(Vector3 dir)
{
    int sx = (dir.X < 0.0f) ? 1 : 0;
    int sy = (dir.Y < 0.0f) ? 1 : 0;
    int sz = (dir.Z < 0.0f) ? 1 : 0;
    float ax = Math.Abs(dir.X);
    float ay = Math.Abs(dir.Y);
    float az = Math.Abs(dir.Z);

    int signs;
    if (ax > ay && ax > az)
    {
        if (ay > az)
            signs = 0 + ((sx << 2) | (sy << 1) | sz);
        else
            signs = 8 + ((sx << 2) | (sz << 1) | sy);
    }
    else if (ay > ax && ay > az)
    {
        if (ax > az)
            signs = 16 + ((sy << 2) | (sx << 1) | sz);
        else
            signs = 24 + ((sy << 2) | (sz << 1) | sx);
    }
    else
    {
        if (ax > ay)
            signs = 32 + ((sz << 2) | (sx << 1) | sy);
        else
            signs = 40 + ((sz << 2) | (sy << 1) | sx);
    }

    return signs;
}
    }
}
