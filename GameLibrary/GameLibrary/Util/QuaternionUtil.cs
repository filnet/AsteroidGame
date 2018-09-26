using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameLibrary.Component.Util
{
    public static class QuaternionUtil
    {

        private const float epsilon = 0.00001f; // floating point epsilon for single precision. todo: verify epsilon value and usage
        private const float epsilonSquared = epsilon * epsilon; // epsilon value squared

        public static Quaternion slerp(Quaternion a, Quaternion b, float t)
        {
            //assert(t>=0);
            //assert(t<=1);

            float flip = 1;

            float cosine = a.W * b.W + a.X * b.X + a.Y * b.Y + a.Z * b.Z;

            if (cosine < 0)
            {
                cosine = -cosine;
                flip = -1;
            }

            if ((1 - cosine) < epsilon)
            {
                return a * (1 - t) + b * (t * flip);
            }

            float theta = (float) Math.Acos(cosine);
            float sine = (float) Math.Sin(theta);
            float beta = (float) Math.Sin((1 - t) * theta) / sine;
            float alpha = (float) Math.Sin(t * theta) / sine * flip;

            return a * beta + b * alpha;
        }

        // Converts a Quaternion to Euler angles (X = Yaw, Y = Pitch, Z = Roll)
        public static Vector3 QuaternionToEulerAngleVector3(Quaternion rotation)
        {
            Vector3 forward = Vector3.Transform(Vector3.Forward, rotation);
            Vector3 up = Vector3.Transform(Vector3.Up, rotation);

            Vector3 rotationAxes = VectorUtil.AngleTo(Vector3.Zero, forward);

            if (rotationAxes.X == MathHelper.PiOver2)
            {
                rotationAxes.Y = (float) Math.Atan2((double) up.X, (double) up.Z);
                rotationAxes.Z = 0;
            }
            else if (rotationAxes.X == -MathHelper.PiOver2)
            {
                rotationAxes.Y = (float) Math.Atan2((double) -up.X, (double) -up.Z);
                rotationAxes.Z = 0;
            }
            else
            {
                up = Vector3.Transform(up, Matrix.CreateRotationY(-rotationAxes.Y));
                up = Vector3.Transform(up, Matrix.CreateRotationX(-rotationAxes.X));

                rotationAxes.Z = (float) Math.Atan2((double) -up.Z, (double) up.Y);
            }

            return rotationAxes;
        }

        // Converts a Rotation Matrix to a quaternion, then into a Vector3 containing
        // Euler angles (X: Pitch, Y: Yaw, Z: Roll)
        public static Vector3 MatrixToEulerAngleVector3(Matrix rotation)
        {
            Vector3 translation, scale;
            Quaternion rot;

            rotation.Decompose(out scale, out rot, out translation);

            Vector3 eulerVec = QuaternionToEulerAngleVector3(rot);

            return eulerVec;
        }


    }
}
