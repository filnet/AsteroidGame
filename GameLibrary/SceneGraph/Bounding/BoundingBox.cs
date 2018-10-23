﻿using Microsoft.Xna.Framework;
using GameLibrary.Util;
using System;

namespace GameLibrary.SceneGraph.Bounding
{
    public class BoundingBox : BoundingVolume
    {

        //private static readonly float EPSILON = 0.00001f;
        //private static readonly float RADIUS_EPSILON = 1.0f + EPSILON;

        // see http://www.yosoygames.com.ar/wp/2013/07/good-bye-axisalignedbox-hello-aabb/
        // see https://bitbucket.org/sinbad/ogre/src/e40654a418bc10e39c97545d2a45966bb7a274d2/OgreMain/src/Math/Simple/C/OgreAabb.cpp?at=v2-0&fileviewer=file-view-default
        protected Vector3 center;
        protected Vector3 halfSize;

        protected Microsoft.Xna.Framework.BoundingBox xnaBoundingBox;

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

        public Vector3 HalfSize
        {
            get { return halfSize; }
            set
            {
                if (halfSize != value)
                {
                    halfSize = value;
                    dirty = true;
                }
            }
        }

        public BoundingBox()
        {
            this.center = Vector3.Zero;
            this.halfSize = Vector3.Zero; 
        }

        public BoundingBox(Vector3 center, Vector3 halfSize)
        {
            this.center = center;
            this.halfSize = halfSize;
        }

        public BoundingBox(BoundingBox bb)
        {
            center = bb.center;
            halfSize = bb.halfSize;
        }

        /// <summary>
        /// Creates a deep-copy of this BoundVolume
        /// </summary>
        /// <returns>A new copy of this volume</returns>
        public override BoundingVolume Clone()
        {
            return new BoundingBox(this);
        }

        private Microsoft.Xna.Framework.BoundingBox asXnaBoundingBox()
        {
            xnaBoundingBox.Max = center + halfSize;
            xnaBoundingBox.Min = center - halfSize;
            return xnaBoundingBox;
        }

        protected override void updateWorldMatrix()
        {
            worldMatrix = Matrix.CreateScale(halfSize) * Matrix.CreateTranslation(center);
        }

        /// <summary>
        /// Computes this BoundVolume from a set of 3D points
        /// </summary>
        /// <param name="points">Array of Vectors</param>
        public override void ComputeFromPoints(Vector3[] points)
        {
/*
            //Vector3[] copy = new Vector3[points.Length];
            //System.Array.Copy(points, copy, points.Length);
            //CalculateWelzl(copy, copy.Length, 0, 0);
            Sphere s = new Sphere();
            SphereUtil.FromPoints(ref s, points);
            Radius = s.radius;
            Center = s.center;
*/
        }

        //private void CalculateWelzl(Vector3[] points, int p, int b, int ap)
        //{
        //    switch (b)
        //    {
        //        case 0:
        //            radius = 0;
        //            center = Vector3.Zero;
        //            break;
        //        case 1:
        //            radius = -EPSILON;
        //            center = points[ap - 1];
        //            break;
        //        case 2:
        //            SetSphere(points[ap - 1], points[ap - 2]);
        //            break;
        //        case 3:
        //            SetSphere(points[ap - 1], points[ap - 2], points[ap - 3]);
        //            break;
        //        case 4:
        //            SetSphere(points[ap - 1], points[ap - 2], points[ap - 3], points[ap - 4]);
        //            return;
        //    }
        //    for (int i = 0; i < p; i++)
        //    {
        //        Vector3 compVec = points[i + ap];
        //        if (Vector3.DistanceSquared(compVec, center) - (radius * radius) > EPSILON)
        //        {
        //            for (int j = i; j > 0; j--)
        //            {
        //                Vector3 temp = points[j + ap];
        //                points[j + ap] = points[j - 1 + ap];
        //                points[j - 1 + ap] = temp;
        //            }
        //            CalculateWelzl(points, i, b + 1, ap + 1);
        //        }
        //    }
        //}

        /// <summary>
        /// Ask this BoundVolume if a point is within its volume.
        /// </summary>
        /// <param name="point">Vector3</param>
        /// <returns>True if is inside the volume, false otherwise</returns>
        public override bool Contains(Vector3 point)
        {
            /*
            if (Vector3.DistanceSquared(Center, point) < Radius * Radius)
            {
                return true;
            }
            else
            {
                return false;
            }
            */
            return false;
        }

        /// <summary>
        /// Compute the distance from the nearest edge of the volume
        /// to the point
        /// </summary>
        /// <param name="point">Vector3</param>
        /// <returns>Distance from neatest edge</returns>
        public override float DistanceFromEdgeTo(Vector3 point)
        {
            return 0;// Vector3.Distance(Center, point) - Radius;
        }

        /// <summary>
        /// Return the volume of this bounding volume.
        /// </summary>
        /// <returns>Volume</returns>
        public override float GetVolume()
        {
            return 0;// (float)(4 * (1 / 3) * System.Math.PI * Radius * Radius * Radius);
        }

        /// <summary>
        /// Return the type of bounding volume
        /// </summary>
        /// <returns>Bounding type</returns>
        public override BoundingType GetBoundingType()
        {
            return BoundingType.AABB;
        }

        /// <summary>
        /// Determine if this volume intersects with another.
        /// Intersection occurs when one contains the other,
        /// they overlap, or if they touch.
        /// </summary>
        /// <param name="bv">BoundVolume to check</param>
        /// <returns>True if intersects, false otherwise</returns>
        public override bool Intersects(BoundingVolume bv)
        {
            if (bv == null)
            {
                return false;
            }
            return false;// bv.IntersectsBoundSphere(this);
        }

        /// <summary>
        /// Determine if this volume intersects with the ray.
        /// </summary>
        /// <param name="ray">Ray to test against</param>
        /// <returns>True if intersects, false otherwise</returns>
        //public override bool Intersects(Ray3 ray)
        //{
        //    if (!MathUtils.IsValidVector(Center))
        //    {
        //        return false;
        //    }

        //    //Test if the origin is inside the sphere
        //    Vector3 diff = ray.Origin - Center;
        //    float radSquared = Radius * Radius;
        //    float a = Vector3.Dot(diff, diff) - radSquared;
        //    if (a <= 0.0f)
        //    {
        //        return true;
        //    }

        //    //Outside sphere
        //    float b = Vector3.Dot(ray.Direction, diff);
        //    if (b >= 0.0f)
        //    {
        //        return false;
        //    }

        //    return b * b >= a;
        //}

        /// <summary>
        /// Determine if this volume intersects with a
        /// bounding box
        /// </summary>
        /// <param name="bb">BoundBox to check with</param>
        /// <returns>True if intersects, false otherwise</returns>
        //public override bool IntersectsBoundBox(BoundBox bb)
        //{
        //    if (!MathUtils.IsValidVector(Center) || !MathUtils.IsValidVector(bb.Center))
        //    {
        //        return false;
        //    }

        //    if (System.Math.Abs(bb.Center.X - Center.X) < Radius + bb.xExtent
        //        && System.Math.Abs(bb.Center.Y - Center.Y) < Radius + bb.yExtent
        //        && System.Math.Abs(bb.Center.Z - Center.Z) < Radius + bb.zExtent)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        /// <summary>
        /// Determine if this volume intersects with a bounding sphere
        /// </summary>
        /// <param name="sphere">BoundSphere to check with</param>
        /// <returns>True if intersects, false otherwise</returns>
        public override bool IntersectsBoundSphere(BoundingSphere sphere)
        {
            //if (!MathUtils.IsValidVector(Center) || !MathUtils.IsValidVector(sphere.Center))
            //{
            //    return false;
            //}
            /*
                        Vector3 diff = Center - sphere.Center;
                        float radSum = Radius + sphere.Radius;
                        if (Vector3.Dot(diff, diff) <= radSum * radSum)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
            */
            return false;
        }

        /// <summary>
        /// Determine if this volume intersects with an OrientedBoundBox
        /// </summary>
        /// <param name="obb">OrientedBoundBox to check against</param>
        /// <returns>True if intersects, false otherwise</returns>
        //public override bool IntersectsOrientedBoundBox(OrientedBoundBox obb)
        //{
        //    return obb.IntersectsBoundSphere(this);
        //}


        public override ContainmentType IsContained(BoundingFrustum boundingFrustum)
        {
            return boundingFrustum.Contains(asXnaBoundingBox());
        }

        /// <summary>
        /// Merges the two bound volumes into a brand new
        /// bounding volume and leaves the two unchanged.
        /// </summary>
        /// <param name="bv">BoundVolume to merge with</param>
        /// <returns>A new volume containing both volumes</returns>
        public override BoundingVolume Merge(BoundingVolume bv)
        {
            if (bv == null)
            {
                return this;
            }

            switch (bv.GetBoundingType())
            {
                //case BoundingType.AABB:
                //    BoundBox box = bv as BoundBox;
                //    return Merge(new Vector3(box.xExtent, box.yExtent, box.zExtent).Length(), box.Center, new BoundSphere());
                case BoundingType.Sphere:
                    BoundingSphere sphere = bv as BoundingSphere;
                    return null;// Merge(sphere.Radius, sphere.Center, new BoundingSphere());
                //case BoundingType.OBB:
                //    OrientedBoundBox obb = bv as OrientedBoundBox;
                //    return MergeOBB(obb, new BoundSphere());
                default:
                    return null;
            }
        }

        /// <summary>
        /// Merges the two bound volumes into a new volume, which is
        /// the one that called the method. Returns the itself.
        /// </summary>
        /// <param name="bv">BoundVolume to merge with</param>
        /// <returns>Itself</returns>
        public override BoundingVolume MergeLocal(BoundingVolume bv)
        {
            if (bv == null)
            {
                return this;
            }

            switch (bv.GetBoundingType())
            {
                //case BoundingType.AABB:
                //    BoundBox box = bv as BoundBox;
                //    return Merge(new Vector3(box.xExtent, box.yExtent, box.zExtent).Length(), box.Center, this);
                case BoundingType.Sphere:
                    BoundingSphere sphere = bv as BoundingSphere;
                    return null;// Merge(sphere.Radius, sphere.Center, this);
                //case BoundingType.OBB:
                //    OrientedBoundBox obb = bv as OrientedBoundBox;
                //    return MergeOBB(obb, this);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Transforms this BoundVolume with the specified
        /// scale, rotation, and translation. Returns a new transformed volume.
        /// </summary>
        /// <param name="scale">Scale</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="translation">Translation</param>
        public override BoundingVolume Transform(Vector3 scale, Quaternion rotation, Vector3 translation, ref BoundingVolume store)
        {
            Matrix m = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);
            return Transform(m, ref store);
        }

        public override BoundingVolume Transform(Matrix m, ref BoundingVolume store)
        {
            BoundingBox rVal = store as BoundingBox;
            if (store == null)
            {
                rVal = new BoundingBox();
            }

            Vector3.Transform(ref center, ref m, out rVal.center);

            rVal.halfSize.X = Math.Abs(m.M11) * halfSize.X + Math.Abs(m.M12) * halfSize.Y + Math.Abs(m.M13) * halfSize.Z;
            rVal.halfSize.Y = Math.Abs(m.M21) * halfSize.X + Math.Abs(m.M22) * halfSize.Y + Math.Abs(m.M23) * halfSize.Z;
            rVal.halfSize.Z = Math.Abs(m.M31) * halfSize.X + Math.Abs(m.M32) * halfSize.Y + Math.Abs(m.M33) * halfSize.Z;

            return rVal;
        }

        private static float GetMaxAxis(Vector3 scale)
        {
            float x = Math.Abs(scale.X);
            float y = Math.Abs(scale.Y);
            float z = Math.Abs(scale.Z);

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

        //private void SetSphere(Vector3 O, Vector3 A)
        //{
        //    radius = (float) System.Math.Sqrt(((A.X - O.X) * (A.X - O.X) + (A.Y - O.Y)
        //        * (A.Y - O.Y) + (A.Z - O.Z) * (A.Z - O.Z)) / 4.0f) + EPSILON;
        //    center.X = (1 - .5f) * O.X + .5f * A.X;
        //    center.Y = (1 - .5f) * O.Y + .5f * A.Y;
        //    center.Z = (1 - .5f) * O.Z + .5f * A.Z;
        //}

        //private void SetSphere(Vector3 O, Vector3 A, Vector3 B)
        //{
        //    Vector3 a = A - O;
        //    Vector3 b = B - O;
        //    Vector3 aCrossB = Vector3.Cross(a, b);
        //    float denom = 2.0f * Vector3.Dot(aCrossB, aCrossB);
        //    if (denom == 0)
        //    {
        //        radius = 0;
        //        center = Vector3.Zero;
        //    }
        //    else
        //    {
        //        Vector3 o = ((Vector3.Cross(aCrossB, a) * b.LengthSquared())
        //            + (Vector3.Cross(b, aCrossB) * a.LengthSquared())) / denom;
        //        radius = o.Length() * RADIUS_EPSILON;
        //        center = O + o;
        //    }
        //}

        //private void SetSphere(Vector3 O, Vector3 A, Vector3 B, Vector3 C)
        //{
        //    Vector3 a = A - O;
        //    Vector3 b = B - O;
        //    Vector3 c = C - O;

        //    float denom = 2.0f * (a.X * (b.Y * c.Z - c.Y * b.Z) - b.X
        //        * (a.Y * c.Z - c.Y * a.Z) + c.X * (a.Y * b.Z - b.Y * a.Z));
        //    if (denom == 0)
        //    {
        //        radius = 0;
        //        center = Vector3.Zero;
        //    }
        //    else
        //    {
        //        Vector3 o = ((Vector3.Cross(a, b) * c.LengthSquared())
        //            + (Vector3.Cross(c, a) * b.LengthSquared())
        //            + (Vector3.Cross(b, c) * a.LengthSquared())) / denom;
        //        radius = o.Length() * RADIUS_EPSILON;
        //        center = O + o;
        //    }
        //}

/*
        private BoundingSphere Merge(float radius, Vector3 center, BoundingSphere sphere)
        {

            Vector3 diff = center - Center;
            float radiusDiff = radius - Radius;

            if (radiusDiff * radiusDiff >= diff.LengthSquared())
            {
                if (radiusDiff <= 0.0f)
                {
                    return this;
                }
                sphere.Center = center;
                sphere.Radius = radius;
                return sphere;
            }

            Vector3 rCenter;
            if (diff.Length() > RADIUS_EPSILON)
            {
                float coeff = (diff.Length() + radiusDiff) / (2.0f * diff.Length());
                rCenter = Center + (diff * coeff);
            }
            else
            {
                rCenter = Center;
            }
            sphere.Center = rCenter;
            sphere.Radius = (0.5f * (diff.Length() + Radius + radius));

            return sphere;
        }
*/
        //private BoundSphere MergeOBB(OrientedBoundBox obb, BoundSphere sphere)
        //{

        //    if (!obb.CorrectCorners)
        //    {
        //        obb.ComputeCorners();
        //    }
        //    float oldRadius = sphere.Radius;
        //    Vector3 oldCenter = sphere.Center;
        //    sphere.ComputeFromPoints(obb.Corners);
        //    return Merge(oldRadius, oldCenter, sphere);
        //}

        #region Object overrides

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            else if (!(obj is BoundingBox))
            {
                return false;
            }
            BoundingBox b = obj as BoundingBox;
            return (center.Equals(b.center)) && (halfSize.Equals(b.halfSize));
        }

        public override int GetHashCode()
        {
            return center.GetHashCode() + halfSize.GetHashCode();
        }

        #endregion

        //    public override Camera.FrustumIntersect CheckFrustumPlane(Plane plane)
        //    {
        //        float distance = plane.DotCoordinate(_center);
        //        if (distance < -Radius)
        //        {
        //            return Camera.FrustumIntersect.Outside;
        //        }
        //        else if (distance > Radius)
        //        {
        //            return Camera.FrustumIntersect.Inside;
        //        }
        //        else
        //        {
        //            return Camera.FrustumIntersect.Intersects;
        //        }
        //    }
    }
}