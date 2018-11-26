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
        //public delegate int Permute3Bits(int 3bits);

        public static readonly Permute<int>[] PERMUTATIONS = new Permute<int>[] {
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = x; py = y; pz = z; }, // x y z
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = x; py = z; pz = y; }, // x z y
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = y; py = x; pz = z; }, // y x z
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = z; py = x; pz = y; }, // y z x
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = y; py = z; pz = x; }, // z x y
            delegate(int x, int y, int z, out int px, out int py, out int pz) {  px = z; py = y; pz = x; }  // z y x
        };

        // taken from http://www.iquilezles.org/www/articles/volumesort/volumesort.htm
        // returns the on of the 48 visit orders between 0 and 47
        public static int visitOrder(Vector3 dir)
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

        // see https://pdfs.semanticscholar.org/1f59/8266e387cf367702d16acf5a4e02cc72cb99.pdf
        // or http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.43.1845&rep=rep1&type=pdf
        // 1st column is the number of vertices, then up to 6 vertices
        public static readonly int[] HULL_LOOKUP_TABLE = new int[] {
            0, 0, 0, 0, 0, 0, 0, //  0 - inside
            4, 0, 4, 7, 3, 0, 0, //  1 - left
            4, 1, 2, 6, 5, 0, 0, //  2 - right
            0, 0, 0, 0, 0, 0, 0, //  3 -
            4, 0, 1, 5, 4, 0, 0, //  4 - bottom
            6, 0, 1, 5, 4, 7, 3, //  5 - bottom, left
            6, 0, 1, 2, 6, 5, 4, //  6 - bottom, right
            0, 0, 0, 0, 0, 0, 0, //  7 -
            4, 2, 3, 7, 6, 0, 0, //  8 - top
            6, 4, 7, 6, 2, 3, 0, //  9 - top, left
            6, 2, 3, 7, 6, 5, 1, // 10 - top, right
            0, 0, 0, 0, 0, 0, 0, // 11 -
            0, 0, 0, 0, 0, 0, 0, // 12 -
            0, 0, 0, 0, 0, 0, 0, // 13 -
            0, 0, 0, 0, 0, 0, 0, // 14 -
            0, 0, 0, 0, 0, 0, 0, // 15 -
            4, 0, 3, 2, 1, 0, 0, // 16 - front
            6, 0, 4, 7, 3, 2, 1, // 17 - front, left
            6, 0, 3, 2, 6, 5, 1, // 18 - front, right
            0, 0, 0, 0, 0, 0, 0, // 19 -
            6, 0, 3, 2, 1, 5, 4, // 20 - front, bottom
            6, 1, 5, 4, 7, 3, 2, // 21 - front, bottom, left
            6, 0, 3, 2, 6, 5, 4, // 22 - front, bottom, right
            0, 0, 0, 0, 0, 0, 0, // 23 -
            6, 0, 3, 7, 6, 2, 1, // 24 - front, top
            6, 0, 4, 7, 6, 2, 1, // 25 - front, top, left
            6,  0, 3, 7, 6, 5, 1,// 26 - front, top, right
            0, 0, 0, 0, 0, 0, 0, // 27 -
            0, 0, 0, 0, 0, 0, 0, // 28 -
            0, 0, 0, 0, 0, 0, 0, // 29 -
            0, 0, 0, 0, 0, 0, 0, // 30 -
            0, 0, 0, 0, 0, 0, 0, // 31 -
            4, 4, 5, 6, 7, 0, 0, // 32 - back
            6, 4, 5, 6, 7, 3, 0, // 33 - back, left
            6, 1, 2, 6, 7, 4, 5, // 34 - back, right
            0, 0, 0, 0, 0, 0, 0, // 35 -
            6, 0, 1, 5, 6, 7, 4, // 36 - back, bottom
            6, 0, 1, 5, 6, 7, 3, // 37 - back, bottom, left
            6, 0, 1, 2, 6, 7, 4, // 38 - back, bottom, right
            0, 0, 0, 0, 0, 0, 0, // 39 -
            6, 2, 3, 7, 4, 5, 6, // 40 - back, top
            6, 0, 4, 5, 6, 2, 3, // 41 - back, top, left
            6, 1, 2, 3, 7, 4, 5, // 42 - back, top, right
        };

        public static readonly Vector3[] HULL_VERTICES = new Vector3[] {
                new Vector3(-1, -1, +1),
                new Vector3(+1, -1, +1),
                new Vector3(+1, +1, +1),
                new Vector3(-1, +1, +1),
                new Vector3(-1, -1, -1),
                new Vector3(+1, -1, -1),
                new Vector3(+1, +1, -1),
                new Vector3(-1, +1, -1),
            };

        public delegate Vector2 ProjectToScreen(ref Vector3 vector);

        public static Vector3[] BBoxProjectedHull(ref Vector3 eye, ref GameLibrary.SceneGraph.Bounding.BoundingBox boundingBox, ProjectToScreen projectToScreen)
        {
            Vector3[] dst = BBoxHull(ref eye, ref boundingBox);
            for (int i = 0; i < dst.Length; i++)
            {
                Vector3 v = dst[i];
                dst[i] = new Vector3(projectToScreen(ref v), 0);
            }
            return dst;
        }

        public static Vector3[] BBoxHull(ref Vector3 eye, ref GameLibrary.SceneGraph.Bounding.BoundingBox boundingBox)
        {
            Vector3 c = boundingBox.Center;
            Vector3 s2 = boundingBox.HalfSize;

            // transform eye to BB coordinates
            eye.X -= c.X;
            eye.Y -= c.Y;
            eye.Z -= c.Z;

            // compute 6-bit code to classify eye with respect to the 6 defining planes
            int pos = 0;
            pos += ((eye.X < -s2.X ? 1 : 0) << 0); //  1 = left
            pos += ((eye.X > +s2.X ? 1 : 0) << 1); //  2 = right
            pos += ((eye.Y < -s2.Y ? 1 : 0) << 2); //  4 = bottom
            pos += ((eye.Y > +s2.Y ? 1 : 0) << 3); //  8 = top
            pos += ((eye.Z < -s2.Z ? 1 : 0) << 5); // 32 = back !!!
            pos += ((eye.Z > +s2.Z ? 1 : 0) << 4); // 16 = front !!!  

            // look up number of vertices
            pos *= 7;
            int num = HULL_LOOKUP_TABLE[pos];
            if (num == 0)
            {
                // return empty array if inside
                return new Vector3[0];
            }

            Vector3[] dst = new Vector3[num];
            for (int i = 0; i < num; i++)
            {
                Vector3 v = VectorUtil.HULL_VERTICES[VectorUtil.HULL_LOOKUP_TABLE[++pos]];
                v.X = c.X + v.X * s2.X;
                v.Y = c.Y + v.Y * s2.Y;
                v.Z = c.Z + v.Z * s2.Z;
                dst[i] = v;
            }
            return dst;
        }

        public static float BBoxArea(ref Vector3 eye, ref GameLibrary.SceneGraph.Bounding.BoundingBox boundingBox, ProjectToScreen projectToScreen)
        {
            Vector3 c = boundingBox.Center;
            Vector3 s2 = boundingBox.HalfSize;

            // transform eye to BB coordinates
            eye.X -= c.X;
            eye.Y -= c.Y;
            eye.Z -= c.Z;

            // compute 6-bit code to classify eye with respect to the 6 defining planes
            int pos = 0;
            pos += ((eye.X < -s2.X ? 1 : 0) << 0); //  1 = left
            pos += ((eye.X > +s2.X ? 1 : 0) << 1); //  2 = right
            pos += ((eye.Y < -s2.Y ? 1 : 0) << 2); //  4 = bottom
            pos += ((eye.Y > +s2.Y ? 1 : 0) << 3); //  8 = top
            pos += ((eye.Z < -s2.Z ? 1 : 0) << 5); // 32 = back !!!
            pos += ((eye.Z > +s2.Z ? 1 : 0) << 4); // 16 = front !!!
       
            // look up number of vertices
            pos *= 7;
            int num = HULL_LOOKUP_TABLE[pos];
            if (num == 0)
            {
                // return -1 if inside
                return -1.0f;
            }

            // project hull vertices
            Vector2[] dst = new Vector2[num];
            for (int i = 0; i < num; i++)
            {
                Vector3 v = VectorUtil.HULL_VERTICES[VectorUtil.HULL_LOOKUP_TABLE[++pos]];
                v.X = c.X + v.X * s2.X;
                v.Y = c.Y + v.Y * s2.Y;
                v.Z = c.Z + v.Z * s2.Z;
                dst[i] = projectToScreen(ref v);
            }
            // compute the area of the polygon using a contour integral
            float sum = (dst[num - 1].X - dst[0].X) * (dst[num - 1].Y + dst[0].Y);
            for (int i = 0; i < num - 1; i++)
            {
                sum += (dst[i].X - dst[i + 1].X) * (dst[i].Y + dst[i + 1].Y);
            }
            // return corrected value
            return sum * 0.5f;
        }

    }

}
