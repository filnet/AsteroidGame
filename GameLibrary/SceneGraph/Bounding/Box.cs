using Microsoft.Xna.Framework;
using GameLibrary.Util;
using System;

namespace GameLibrary.SceneGraph.Bounding
{
    public class Box : Volume
    {

        //private static readonly float EPSILON = 0.00001f;
        //private static readonly float RADIUS_EPSILON = 1.0f + EPSILON;

        // see http://www.yosoygames.com.ar/wp/2013/07/good-bye-axisalignedbox-hello-aabox/
        // see https://bitbucket.org/sinbad/ogre/src/e40654a418bc10e39c97545d2a45966box7a274d2/OgreMain/src/Math/Simple/C/OgreAabox.cpp?at=v2-0&fileviewer=file-view-default
        protected Vector3 center;
        protected Vector3 halfSize;

        public Vector3 Center
        {
            get { return center; }
            set { center = value; }
        }

        public Vector3 HalfSize
        {
            get { return halfSize; }
            set { halfSize = value; }
        }

        public Box()
        {
            this.center = Vector3.Zero;
            this.halfSize = Vector3.Zero;
        }

        public Box(Vector3 center, Vector3 halfSize)
        {
            this.center = center;
            this.halfSize = halfSize;
        }

        public Box(Box box)
        {
            center = box.center;
            halfSize = box.halfSize;
        }

        public override VolumeType Type()
        {
            return VolumeType.AABB;
        }

        public override Volume Clone()
        {
            return new Box(this);
        }

        public static Box CreateFromMinMax(Vector3 min, Vector3 max)
        {
            return new Box((max + min) / 2.0f, (max - min) / 2.0f);
        }

        public static Box CreateFromMinMax(ref Vector3 min, ref Vector3 max, Box store)
        {
            store.Center = (max + min) / 2.0f;
            store.HalfSize = (max - min) / 2.0f;
            return store;
        }

        #region Contains

        public override ContainmentType Contains(Box box, ContainmentHint hint)
        {
            return ContainmentType.Intersects;
        }

        public override ContainmentType Contains(Sphere sphere)
        {
            return ContainmentType.Intersects;
        }

        public override ContainmentType Contains(Frustum frustum)
        {
            return ContainmentType.Intersects;
        }

        public override void Contains(ref Vector3 point, out bool result)
        {
            result = (Math.Abs(Center.X - point.X) <= HalfSize.X) &&
                    (Math.Abs(Center.Y - point.Y) <= HalfSize.Y) &&
                    (Math.Abs(Center.Z - point.Z) <= HalfSize.Z);
        }

        #endregion

        #region Intersects

        /// see https://bitbucket.org/sinbad/ogre/src/e6536e06109e32edcce50b3546b7b4a367fcb14d/OgreMain/include/Math/Simple/C/OgreAabb.inl?at=v2-1
        // FIXME inline
        public override bool Intersects(Box box)
        {
            return ((Math.Abs(Center.X - box.Center.X) <= HalfSize.X + box.HalfSize.X) &&
                    (Math.Abs(Center.Y - box.Center.Y) <= HalfSize.Y + box.HalfSize.Y) &&
                    (Math.Abs(Center.Z - box.Center.Z) <= HalfSize.Z + box.HalfSize.Z));
        }

        // FIXME inline
        public override bool Intersects(Sphere sphere)
        {
            return ((Math.Abs(Center.X - sphere.Center.X) <= HalfSize.X + sphere.Radius) &&
                    (Math.Abs(Center.Y - sphere.Center.Y) <= HalfSize.Y + sphere.Radius) &&
                    (Math.Abs(Center.Z - sphere.Center.Z) <= HalfSize.Z + sphere.Radius));
        }

        public override bool Intersects(Frustum frustum)
        {
            return false;
        }

        public override void Intersects(ref Plane plane, out PlaneIntersectionType planeIntersectionType)
        {
            // See http://zach.in.tu-clausthal.de/teaching/cg_literatur/lighthouse3d_view_frustum_culling/index.html

            Vector3 positiveVertex = center;
            Vector3 negativeVertex = center;

            if (plane.Normal.X >= 0)
            {
                positiveVertex.X += halfSize.X;
                negativeVertex.X -= halfSize.X;
            }
            else
            {
                positiveVertex.X -= halfSize.X;
                negativeVertex.X += halfSize.X;
            }

            if (plane.Normal.Y >= 0)
            {
                positiveVertex.Y += halfSize.Y;
                negativeVertex.Y -= halfSize.Y;
            }
            else
            {
                positiveVertex.Y -= halfSize.Y;
                negativeVertex.Y += halfSize.Y;
            }

            if (plane.Normal.Z >= 0)
            {
                positiveVertex.Z += halfSize.Z;
                negativeVertex.Z -= halfSize.Z;
            }
            else
            {
                positiveVertex.Z -= halfSize.Z;
                negativeVertex.Z += halfSize.Z;
            }

            // Inline Vector3.Dot(plane.Normal, negativeVertex) + plane.D;
            var distance = plane.Normal.X * negativeVertex.X + plane.Normal.Y * negativeVertex.Y + plane.Normal.Z * negativeVertex.Z + plane.D;
            if (distance > 0)
            {
                planeIntersectionType = PlaneIntersectionType.Front;
                return;
            }

            // Inline Vector3.Dot(plane.Normal, positiveVertex) + plane.D;
            distance = plane.Normal.X * positiveVertex.X + plane.Normal.Y * positiveVertex.Y + plane.Normal.Z * positiveVertex.Z + plane.D;
            if (distance < 0)
            {
                planeIntersectionType = PlaneIntersectionType.Back;
                return;
            }
            planeIntersectionType = PlaneIntersectionType.Intersecting;
            return;
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

        #endregion

        public override void WorldMatrix(out Matrix m)
        {
            m = Matrix.CreateScale(halfSize) * Matrix.CreateTranslation(center);
        }

        /// <summary>
        /// Computes this BoundVolume from a set of 3D points
        /// </summary>
        /// <param name="points">Array of Vectors</param>
        public void ComputeFromPoints(Vector3[] points)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);

            foreach (Vector3 v in points)
            {
                min.X = Math.Min(min.X, v.X);
                min.Y = Math.Min(min.Y, v.Y);
                min.Z = Math.Min(min.Z, v.Z);
                max.X = Math.Max(max.X, v.X);
                max.Y = Math.Max(max.Y, v.Y);
                max.Z = Math.Max(max.Z, v.Z);
            }
            Center = (max + min) / 2.0f;
            HalfSize = (max - min) / 2.0f;
        }

        public override float DistanceTo(Vector3 point)
        {
            return 0;
        }

        public override float DistanceSquaredTo(Vector3 point)
        {
            return 0;
        }

        public override float DistanceFromEdgeTo(Vector3 point)
        {
            return 0;// Vector3.Distance(Center, point) - Radius;
        }

        public override float GetVolume()
        {
            return 0;// (float)(4 * (1 / 3) * boxMath.PI * Radius * Radius * Radius);
        }

        public /*override*/ Volume Merge(Volume bv)
        {
            if (bv == null)
            {
                return this;
            }

            switch (bv.Type())
            {
                //case BoundingType.AABB:
                //    BoundBox box = bv as BoundBox;
                //    return Merge(new Vector3(box.xExtent, box.yExtent, box.zExtent).Length(), box.Center, new BoundSphere());
                case VolumeType.Sphere:
                    Sphere sphere = bv as Sphere;
                    return null;// Merge(sphere.Radius, sphere.Center, new BoundingSphere());
                //case BoundingType.OBB:
                //    OrientedBoundBox obox = bv as OrientedBoundBox;
                //    return MergeOBB(obox, new BoundSphere());
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
        public /*override*/ Volume MergeLocal(Volume bv)
        {
            if (bv == null)
            {
                return this;
            }

            switch (bv.Type())
            {
                //case BoundingType.AABB:
                //    BoundBox box = bv as BoundBox;
                //    return Merge(new Vector3(box.xExtent, box.yExtent, box.zExtent).Length(), box.Center, this);
                case VolumeType.Sphere:
                    Sphere sphere = bv as Sphere;
                    return null;// Merge(sphere.Radius, sphere.Center, this);
                //case BoundingType.OBB:
                //    OrientedBoundBox obox = bv as OrientedBoundBox;
                //    return MergeOBB(obox, this);
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
        public override Volume Transform(Vector3 scale, Quaternion rotation, Vector3 translation, Volume store)
        {
            Matrix m = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);
            return Transform(m, store);
        }

        public override Volume Transform(Matrix m, Volume store)
        {
            Box rVal = store as Box;
            if (store == null)
            {
                rVal = new Box();
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
        //    radius = (float) boxMath.Sqrt(((A.X - O.X) * (A.X - O.X) + (A.Y - O.Y)
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
        //private BoundSphere MergeOBB(OrientedBoundBox obox, BoundSphere sphere)
        //{

        //    if (!obox.CorrectCorners)
        //    {
        //        obox.ComputeCorners();
        //    }
        //    float oldRadius = sphere.Radius;
        //    Vector3 oldCenter = sphere.Center;
        //    sphere.ComputeFromPoints(obox.Corners);
        //    return Merge(oldRadius, oldCenter, sphere);
        //}

        #region Object overrides

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            else if (!(obj is Box))
            {
                return false;
            }
            Box b = obj as Box;
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