using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.SceneGraph.Bounding
{
    /// <summary>
    /// The type that this bounding volume is of.
    /// </summary>
    public enum BoundingType
    {
        //Bounding Sphere
        Sphere,
        //Axis-Aligned Bounding Box
        AABB,
        //Oriented Bounding Box
        OBB
    }

    /// <summary>
    /// Defines the base bounding volume. A bounding volume defines a simple
    /// shape (such as a box or a sphere) that encapsulates a spatial, or
    /// a group of spatials completely. This allows for coarse-grain
    /// collision checks and for basic culling.
    ///
    /// </summary>
    public abstract class BoundingVolume
    {
        protected Vector3 center;

        public Vector3 Center
        {
            get { return center; }
            set
            {
                if (center != value)
                {
                    center = value;
                    dirty = true;
                }
            }
        }

        private Matrix worldMatrix;

        protected bool dirty = true;

        public Matrix WorldMatrix
        {
            get
            {
                if (dirty)
                {
                    worldMatrix = ComputeWorldMatrix();
                    dirty = false;
                }
                return worldMatrix;
            }
        }

        public BoundingVolume()
        {
            center = new Vector3(0, 0, 0);
        }

        public BoundingVolume(Vector3 center)
        {
            this.center = center;
        }

        public BoundingVolume(BoundingVolume bv)
        {
            center = bv.center;
        }

        public abstract Matrix ComputeWorldMatrix();

        /// <summary>
        /// Creates a deep-copy of this BoundVolume, returns the same type.
        /// </summary>
        /// <returns>A new copy of this volume</returns>
        public abstract BoundingVolume Clone();

        /// <summary>
        /// Return the type of bounding volume
        /// </summary>
        /// <returns>Bounding type</returns>
        public abstract BoundingType GetBoundingType();

        /// <summary>
        /// Computes this BoundVolume from a set of 3D points
        /// </summary>
        /// <param name="points">Array of Vectors</param>
        public abstract void ComputeFromPoints(Vector3[] points);

        /// <summary>
        /// Ask this BoundVolume if a point is within its volume.
        /// </summary>
        /// <param name="point">Vector3</param>
        /// <returns>True if is inside the volume, false otherwise</returns>
        public abstract bool Contains(Vector3 point);

        /// <summary>
        /// Compute the distance from the center of this volume
        /// to the point.
        /// </summary>
        /// <param name="point">Vector3</param>
        /// <returns>Distance</returns>
        public float DistanceTo(Vector3 point)
        {
            return Vector3.Distance(Center, point);
        }

        /// <summary>
        /// Compute the distance from the center of this volume to
        /// the point and return the square.
        /// </summary>
        /// <param name="point">Vector3</param>
        /// <returns>Distance squared</returns>
        public float DistanceSquaredTo(Vector3 point)
        {
            return Vector3.DistanceSquared(Center, point);
        }

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
        /// Determine if this volume intersects with another.
        /// Intersection occurs when one contains the other,
        /// they overlap, or if they touch.
        /// </summary>
        /// <param name="bv">BoundVolume to check</param>
        /// <returns>True if intersects, false otherwise</returns>
        public abstract bool Intersects(BoundingVolume bv);

        /// <summary>
        /// Determine if this volume intersects with the ray.
        /// </summary>
        /// <param name="ray">Ray to test against</param>
        /// <returns>True if intersects, false otherwise</returns>
        //public abstract bool Intersects(Ray3 ray);

        /// <summary>
        /// Determine if this volume intersects with a
        /// bounding box
        /// </summary>
        /// <param name="bb">BoundBox to check with</param>
        /// <returns>True if intersects, false otherwise</returns>
        //public abstract bool IntersectsBoundBox(BoundBox bb);

        /// <summary>
        /// Determine if this volume intersects with a bounding sphere
        /// </summary>
        /// <param name="sphere">BoundSphere to check with</param>
        /// <returns>True if intersects, false otherwise</returns>
        public abstract bool IntersectsBoundSphere(BoundingSphere sphere);

        /// <summary>
        /// Determine if this volume intersects with an OrientedBoundBox
        /// </summary>
        /// <param name="obb">OrientedBoundBox to check against</param>
        /// <returns>True if intersects, false otherwise</returns>
        //public abstract bool IntersectsOrientedBoundBox(OrientedBoundBox obb);

        /// <summary>
        /// Merges the two bound volumes into a brand new
        /// bounding volume and leaves the two unchanged.
        /// </summary>
        /// <param name="bv">BoundVolume to merge with</param>
        /// <returns>A new volume containing both volumes</returns>
        public abstract BoundingVolume Merge(BoundingVolume bv);

        /// <summary>
        /// Merges the two bound volumes into a new volume, which is
        /// the one that called the method. Returns the itself.
        /// </summary>
        /// <param name="bv">BoundVolume to merge with</param>
        /// <returns>Itself</returns>
        public abstract BoundingVolume MergeLocal(BoundingVolume bv);

        /// <summary>
        /// Set the center of this BoundVolume with the
        /// specified x,y,z coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        public void SetCenter(float x, float y, float z)
        {
            //Vector3 center = Center;
            center.X = x;
            center.Y = y;
            center.Z = z;
            //Center = center;
        }

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
        /// Transforms this BoundVolume with the specified
        /// scale, rotation, and translation. Returns a new transformed volume.
        /// </summary>
        /// <param name="scale">Scale</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="translation">Translation</param>
        /// <param name="store">BoundVolume to store in</param>
        public abstract BoundingVolume Transform(Vector3 scale, Quaternion rotation, Vector3 translation, BoundingVolume store);

        public abstract BoundingVolume Transform(Matrix m, BoundingVolume store);

        //public abstract Camera.FrustumIntersect CheckFrustumPlane(Plane plane);
    }
}