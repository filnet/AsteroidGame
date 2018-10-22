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
        public static Matrix Vector3ToMatrix(Vector3 Rotation)
        {
            return Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
        }

        // Returns Euler angles that point from one point to another
        public static Vector3 AngleTo(Vector3 from, Vector3 location)
        {
            Vector3 angle = new Vector3();
            Vector3 v3 = Vector3.Normalize(location - from);

            angle.X = (float) Math.Asin(v3.Y);
            angle.Y = (float) Math.Atan2((double) -v3.X, (double) -v3.Z);

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


    }
}
