using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLibrary.Component.Camera;
using Microsoft.Xna.Framework;

namespace GameLibrary
{
    public static class VolumeUtil
    {
        public delegate Vector2 ProjectToScreen(ref Vector3 vector);

        public static float ClassifyPoint(ref Plane p, ref Vector3 point)
        {
            return point.X * p.Normal.X + point.Y * p.Normal.Y + point.Z * p.Normal.Z + p.D;
        }

        public static void IntersectionPoint(ref Plane a, ref Plane b, ref Plane c, out Vector3 result)
        {
            // Formula used
            //                d1 ( N2 * N3 ) + d2 ( N3 * N1 ) + d3 ( N1 * N2 )
            //P =   -------------------------------------------------------------------------
            //                             N1 . ( N2 * N3 )
            //
            // Note: N refers to the normal, d refers to the displacement. '.' means dot product. '*' means cross product

            Vector3 v1, v2, v3;
            Vector3 cross;

            Vector3.Cross(ref b.Normal, ref c.Normal, out cross);

            float f;
            Vector3.Dot(ref a.Normal, ref cross, out f);
            f *= -1.0f;

            Vector3.Cross(ref b.Normal, ref c.Normal, out cross);
            Vector3.Multiply(ref cross, a.D, out v1);
            //v1 = (a.D * (Vector3.Cross(b.Normal, c.Normal)));


            Vector3.Cross(ref c.Normal, ref a.Normal, out cross);
            Vector3.Multiply(ref cross, b.D, out v2);
            //v2 = (b.D * (Vector3.Cross(c.Normal, a.Normal)));


            Vector3.Cross(ref a.Normal, ref b.Normal, out cross);
            Vector3.Multiply(ref cross, c.D, out v3);
            //v3 = (c.D * (Vector3.Cross(a.Normal, b.Normal)));

            result.X = (v1.X + v2.X + v3.X) / f;
            result.Y = (v1.Y + v2.Y + v3.Y) / f;
            result.Z = (v1.Z + v2.Z + v3.Z) / f;
        }

        public static void NormalizePlane(ref Plane p)
        {
            float factor = 1f / p.Normal.Length();
            p.Normal.X *= factor;
            p.Normal.Y *= factor;
            p.Normal.Z *= factor;
            p.D *= factor;
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
            6, 3, 2, 6, 5, 4, 7, // 23 - front, left, right, bottom (frustum)
            6, 0, 3, 7, 6, 2, 1, // 24 - front, top
            6, 0, 4, 7, 6, 2, 1, // 25 - front, top, left
            6, 0, 3, 7, 6, 5, 1, // 26 - front, top, right
            6, 1, 0, 4, 7, 6, 5, // 27 - front, left, right, top (frustum)
            0, 0, 0, 0, 0, 0, 0, // 28 -
            6, 2, 1, 5, 4, 7, 6, // 29 - front, left, top, bottom (frustum)
            6, 0, 3, 7, 6, 5, 4, // 30 - front, right, top, bottom (frustum)
            4, 7, 6, 5, 4, 0, 0, // 31 - front, left, right, top, bottom (frustum)
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

        public static readonly Vector3[] BB_HULL_VERTICES = new Vector3[] {
                new Vector3(-1, -1, +1),
                new Vector3(+1, -1, +1),
                new Vector3(+1, +1, +1),
                new Vector3(-1, +1, +1),
                new Vector3(-1, -1, -1),
                new Vector3(+1, -1, -1),
                new Vector3(+1, +1, -1),
                new Vector3(-1, +1, -1),
            };

        public static void ComputeFrustumCentroidSphere(Vector3[] frustumCorners, out Vector3 center, out float radius)
        {
            // - center = view frustrum centroid
            // - radius = maximum distance between center and (all?) view frustrum corners
            // works but bounding sphere is not optimal
            // all computations can be done in WS as the computed BS is rotation invariant

            // compute frustrum centroid
            // TODO do we really need to use all 8 corners?
            center = frustumCorners[0];
            for (int i = 1; i < frustumCorners.Length; i++)
            {
                center += frustumCorners[i];
            }
            center /= 8.0f;

            // find maximum distance from centroid to (all?) 
            // the resulting bounding sphere surrounds the frustum corners
            // TODO do we need to check all 8 corners? two antagonist corners should suffice?
            radius = 0.0f;
            for (int i = 0; i < frustumCorners.Length; i++)
            {
                Vector3 v = frustumCorners[i] - center;
                radius = Math.Max(v.Length(), radius);
            }
            // see http://gsteph.blogspot.com/2010/11/minimum-bounding-sphere-for-frustum.html
            // sphere from 4 points: http://www.ambrsoft.com/TrigoCalc/Sphere/Spher3D_.htm
        }

        public static void ComputeFrustumBestFittingBoundingSphere(Camera camera, out double dz, out double radius)
        {
            ComputeFrustumBestFittingSphere(camera.FovX, camera.AspectRatio, camera.ZNear, camera.ZNear, out dz, out radius);
        }

        public static void ComputeFrustumBestFittingSphere(double fovX, double aspectRatio, double near, double far, out double center, out double radius)
        {
            // The basic idea is to pick four points that form a maximal cross section
            // (two opposite corners from the near plane, the corresponding corners from the far plane).
            // Then find the minimal circle that encloses those four points in 2D, and finally extrude that into a sphere in 3D.
            // Sample code for the minimal enclosing circle (in 2D) is much easier to find than the corresponding 3D problems.

            // see https://lxjk.github.io/2017/04/15/Calculate-Minimal-Bounding-Sphere-of-Frustum.html

            //double w2 = w * w;
            //double h2 = h * h;
            // h2w2 = (h * h) / (w * w)
            double h2w2 = (1.0 / (aspectRatio * aspectRatio));

            double n = near;
            double f = far;

            double k = Math.Sqrt(1.0 + h2w2) * Math.Tan(fovX / 2.0);
            double k2 = k * k;

            if (k2 >= ((f - n) / (f + n)))
            {
                center = f;
                radius = f * k;
            }
            else
            {
                center = (f + n) * (1.0 + k2) / 2.0;
                radius = Math.Sqrt((f - n) * (f - n) + 2 * (f * f + n * n) * k2 + (f + n) * (f + n) * k2 * k2) / 2;
            }
        }

    }
}
