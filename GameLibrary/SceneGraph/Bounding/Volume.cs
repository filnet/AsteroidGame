using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.SceneGraph.Bounding
{
    public enum VolumeType
    {
        Sphere,
        AABB,
        OBB,
        Frustum,
        Region
    }

    public enum ContainmentHint
    {
        Fast,
        Precise
    }

    /// <summary>
    /// Defines the base bounding volume. A bounding volume defines a simple
    /// shape (such as a box or a sphere) that encapsulates a spatial, or
    /// a group of spatials completely. This allows for coarse-grain
    /// collision checks and for basic culling.
    ///
    /// </summary>
    public abstract class Volume
    {
        public Volume()
        {
        }

        /// <summary>
        /// Returns the type of volume
        /// </summary>
        /// <returns>Volume type</returns>
        public abstract VolumeType Type();

        /// <summary>
        /// Creates a deep-copy of this Volume, returns the same type.
        /// </summary>
        /// <returns>A new copy of this volume</returns>
        public abstract Volume Clone();

        #region Contains

        /// <summary>
        /// Containment test between this <see cref="Frustum"/> and specified <see cref="Box"/>.
        /// </summary>
        /// <param name="box">A <see cref="Box"/> for testing.</param>
        /// <returns>Result of testing for containment between this <see cref="Frustum"/> and specified <see cref="Box"/>.</returns>
        public abstract ContainmentType Contains(Box box, ContainmentHint hint);

        /// <summary>
        /// Containment test between this <see cref="Frustum"/> and specified <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere">A <see cref="Sphere"/> for testing.</param>
        /// <returns>Result of testing for containment between this <see cref="Frustum"/> and specified <see cref="Sphere"/>.</returns>
        public abstract ContainmentType Contains(Sphere sphere);

        /// <summary>
        /// Containment test between this <see cref="Frustum"/> and specified <see cref="Frustum"/>.
        /// </summary>
        /// <param name="frustum">A <see cref="Frustum"/> for testing.</param>
        /// <returns>Result of testing for containment between this <see cref="Frustum"/> and specified <see cref="Frustum"/>.</returns>
        public abstract ContainmentType Contains(Frustum frustum);

        /// <summary>
        /// Containment test between this <see cref="Frustum"/> and specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="point">A <see cref="Vector3"/> for testing.</param>
        /// <returns>Result of testing for containment between this <see cref="Frustum"/> and specified <see cref="Vector3"/>.</returns>
        public bool Contains(Vector3 point)
        {
            bool result;
            Contains(ref point, out result);
            return result;
        }

        public abstract void Contains(ref Vector3 point, out bool result);

        #endregion

        #region Intersects

        public abstract bool Intersects(Box box);

        public abstract bool Intersects(Sphere sphere);

        public abstract bool Intersects(Frustum frustum);

        public PlaneIntersectionType Intersects(Plane plane)
        {
            PlaneIntersectionType result;
            Intersects(ref plane, out result);
            return result;
        }

        public abstract void Intersects(ref Plane plane, out PlaneIntersectionType result);

        //public Nullable<float> Intersects(Ray ray);

        //public void Intersects(ref Ray ray, out Nullable<float> result);

        #endregion

        /// <summary>
        /// Computes this BoundVolume from a set of 3D points
        /// </summary>
        /// <param name="points">Array of Vectors</param>
        //public abstract void ComputeFromPoints(Vector3[] points);

        /// <summary>
        /// Compute the distance from the center of this volume
        /// to the point.
        /// </summary>
        /// <param name="point">Vector3</param>
        /// <returns>Distance</returns>
        public abstract float DistanceTo(Vector3 point);

        /// <summary>
        /// Compute the distance from the center of this volume to
        /// the point and return the square.
        /// </summary>
        /// <param name="point">Vector3</param>
        /// <returns>Distance squared</returns>
        public abstract float DistanceSquaredTo(Vector3 point);

        /// <summary>
        /// Compute the distance from the nearest edge of the volume
        /// to the point
        /// </summary>
        /// <param name="point">Vector3</param>
        /// <returns>Distance from neatest edge</returns>
        public abstract float DistanceFromEdgeTo(Vector3 point);

        /// <summary>
        /// Return the volume of this bounding volume.
        /// </summary>
        /// <returns>Volume</returns>
        public abstract float GetVolume();

        /// <summary>
        /// Merges the two bound volumes into a brand new
        /// bounding volume and leaves the two unchanged.
        /// </summary>
        /// <param name="bv">BoundVolume to merge with</param>
        /// <returns>A new volume containing both volumes</returns>
        //public abstract Volume Merge(Volume bv);

        /// <summary>
        /// Merges the two bound volumes into a new volume, which is
        /// the one that called the method. Returns itself.
        /// </summary>
        /// <param name="bv">BoundVolume to merge with</param>
        /// <returns>Itself</returns>
        //public abstract Volume MergeLocal(Volume bv);

        /// <summary>
        /// Set the center of this BoundVolume with the
        /// specified x,y,z coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
/*     
        public void SetCenter(float x, float y, float z)
        {
            //Vector3 center = Center;
            center.X = x;
            center.Y = y;
            center.Z = z;
            //Center = center;
        }
*/
        /// <summary>
        /// Transforms this BoundVolume using the specified Transform
        /// object.
        /// </summary>
        /// <param name="transform">The transformation</param>
        /// <param name="store">BoundVolume to store in</param>
        //public BoundVolume Transform(Transform transform, BoundVolume store)
        //{
        //    return Transform(transform.Scale, transform.Rotation, transform.Translation, store);
        //}

        /// <summary>
        /// Transforms this Volume with the specified
        /// scale, rotation, and translation. Returns a new transformed volume.
        /// </summary>
        /// <param name="scale">Scale</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="translation">Translation</param>
        /// <param name="store">Volume to store in</param>
        public abstract Volume Transform(Vector3 scale, Quaternion rotation, Vector3 translation, Volume store);

        public abstract Volume Transform(Matrix m, Volume store);

        //public abstract Matrix WorldMatrix();

        public abstract void WorldMatrix(out Matrix m);
    }
}