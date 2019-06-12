using System.Numerics;

namespace GameLibrary.Physics.Bepu
{
    public static class Vector3Extensions
    {
        public static Microsoft.Xna.Framework.Vector3 ToMonogame(this Vector3 v)
        {
            Microsoft.Xna.Framework.Vector3 vector;
            vector.X = v.X;
            vector.Y = v.Y;
            vector.Z = v.Z;
            return vector;
        }

        public static void ToMonogame(this Vector3 v, out Microsoft.Xna.Framework.Vector3 vector)
        {
            vector.X = v.X;
            vector.Y = v.Y;
            vector.Z = v.Z;
        }
    }

}
