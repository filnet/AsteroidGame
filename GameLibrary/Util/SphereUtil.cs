using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameLibrary.Util
{
    public static class SphereUtil
    {
        private static readonly float EPSILON = 0.00001f;
        //private static readonly float RADIUS_EPSILON = 1.0f + EPSILON;

        /// <summary>
        /// Computes this BoundVolume from a set of 3D points
        /// </summary>
        /// <param name="points">Array of Vectors</param>
        public static void FromPoints(ref Sphere sphere, Vector3 O, Vector3 A)
        {
            double radiusSquared = ((A.X - O.X) * (A.X - O.X) + (A.Y - O.Y) * (A.Y - O.Y) + (A.Z - O.Z) * (A.Z - O.Z)) / 4.0f;
            sphere.radius = (float) Math.Sqrt(radiusSquared);
            sphere.radiusSquared = (float) radiusSquared;
            sphere.center.X = (O.X + A.X) / 2;
            sphere.center.Y = (O.Y + A.Y) / 2;
            sphere.center.Z = (O.Z + A.Z) / 2;
        }

        public static void FromPoints(ref Sphere sphere, Vector3 O, Vector3 A, Vector3 B)
        {
            Vector3 a = A - O;
            Vector3 b = B - O;
            Vector3 aCrossB = Vector3.Cross(a, b);
            float denom = 2.0f * Vector3.Dot(aCrossB, aCrossB);
            if (denom == 0)
            {
                sphere.radius = 0;
                sphere.radiusSquared = 0;
                sphere.center = Vector3.Zero;
            }
            else
            {
                Vector3 o = ((Vector3.Cross(aCrossB, a) * b.LengthSquared()) + (Vector3.Cross(b, aCrossB) * a.LengthSquared())) / denom;
                double radiusSquared = o.LengthSquared();
                sphere.radius = (float) Math.Sqrt(radiusSquared);
                sphere.radiusSquared = (float) radiusSquared;
                sphere.center = O + o;
            }
        }

        public static void FromPoints(ref Sphere sphere, Vector3 O, Vector3 A, Vector3 B, Vector3 C)
        {
            Vector3 a = A - O;
            Vector3 b = B - O;
            Vector3 c = C - O;

            float denom = 2.0f * (a.X * (b.Y * c.Z - c.Y * b.Z) - b.X * (a.Y * c.Z - c.Y * a.Z) + c.X * (a.Y * b.Z - b.Y * a.Z));
            if (denom == 0)
            {
                sphere.radius = 0;
                sphere.radiusSquared = 0;
                sphere.center = Vector3.Zero;
            }
            else
            {
                Vector3 o = ((Vector3.Cross(a, b) * c.LengthSquared()) + (Vector3.Cross(c, a) * b.LengthSquared()) + (Vector3.Cross(b, c) * a.LengthSquared())) / denom;
                double radiusSquared = o.LengthSquared();
                sphere.radius = (float) global::System.Math.Sqrt(radiusSquared);
                sphere.radiusSquared = (float) radiusSquared;
                sphere.center = O + o;
            }
        }

        public static void FromPoints(ref Sphere sphere, Vector3[] points)
        {
            Vector3[] copy = new Vector3[points.Length];
            global::System.Array.Copy(points, copy, points.Length);
            calculateWelzl(ref sphere, copy, copy.Length, 0, 0);
        }

        private static void calculateWelzl(ref Sphere sphere, Vector3[] points, int p, int b, int ap)
        {
            switch (b)
            {
                case 0:
                    sphere.radius = 0;
                    sphere.radiusSquared = 0;
                    sphere.center = Vector3.Zero;
                    break;
                case 1:
                    sphere.radius = 0;
                    sphere.radiusSquared = 0;
                    sphere.center = points[ap - 1];
                    break;
                case 2:
                    FromPoints(ref sphere, points[ap - 1], points[ap - 2]);
                    break;
                case 3:
                    FromPoints(ref sphere, points[ap - 1], points[ap - 2], points[ap - 3]);
                    break;
                case 4:
                    FromPoints(ref sphere, points[ap - 1], points[ap - 2], points[ap - 3], points[ap - 4]);
                    return;
            }
            for (int i = 0; i < p; i++)
            {
                Vector3 compVec = points[i + ap];
                if (Vector3.DistanceSquared(compVec, sphere.center) - (sphere.radiusSquared) > EPSILON)
                {
                    for (int j = i; j > 0; j--)
                    {
                        Vector3 temp = points[j + ap];
                        points[j + ap] = points[j - 1 + ap];
                        points[j - 1 + ap] = temp;
                    }
                    calculateWelzl(ref sphere, points, i, b + 1, ap + 1);
                }
            }
        }
    }
}
