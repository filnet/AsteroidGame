using BepuUtilities;

namespace GameLibrary.Physics.Bepu
{
    public static class QuaternionExtensions
    {
        public static Microsoft.Xna.Framework.Quaternion ToMonogame(this Quaternion q)
        {
            Microsoft.Xna.Framework.Quaternion quaternion;
            quaternion.X = q.X;
            quaternion.Y = q.Y;
            quaternion.Z = q.Z;
            quaternion.W = q.Z;
            return quaternion;
        }

        public static void ToMonogame(this Quaternion q, out Microsoft.Xna.Framework.Quaternion quaternion)
        {
            quaternion.X = q.X;
            quaternion.Y = q.Y;
            quaternion.Z = q.Z;
            quaternion.W = q.Z;
        }
    }

}
