using Microsoft.Xna.Framework;
using GameLibrary.Util;
using System;
using static GameLibrary.VolumeUtil;

namespace GameLibrary.SceneGraph.Bounding
{
    public class Box : Volume
    {
        #region Private Fields

        //private static readonly float EPSILON = 0.00001f;
        //private static readonly float RADIUS_EPSILON = 1.0f + EPSILON;

        // see http://www.yosoygames.com.ar/wp/2013/07/good-bye-axisalignedbox-hello-aabox/
        // see https://bitbucket.org/sinbad/ogre/src/e40654a418bc10e39c97545d2a45966box7a274d2/OgreMain/src/Math/Simple/C/OgreAabox.cpp?at=v2-0&fileviewer=file-view-default
        protected Vector3 center;
        protected Vector3 halfSize;

        #endregion

        #region Properties

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

        #endregion

        #region Constructors

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

        #endregion

        #region Public Methods

        public override VolumeType Type()
        {
            return VolumeType.Box;
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
            throw new NotImplementedException();
        }

        public override ContainmentType Contains(Sphere sphere)
        {
            throw new NotImplementedException();
        }

        public override ContainmentType Contains(Frustum frustum)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public override bool Intersects(Frustum frustum)
        {
            throw new NotImplementedException();
        }

        public override void Intersects(ref Plane plane, out PlaneIntersectionType planeIntersectionType)
        {
            Intersects1(ref plane, out planeIntersectionType);
        }

        private void Intersects1(ref Plane plane, out PlaneIntersectionType planeIntersectionType)
        {
            // Compute the projection interval of box half size onto plane
            float r = halfSize.X * Math.Abs(plane.Normal.X) + halfSize.Y * Math.Abs(plane.Normal.Y) + halfSize.Z * Math.Abs(plane.Normal.Z);

            // Compute signed distance of box center to plane
            // TODO inline
            float s;
            plane.DotCoordinate(ref center, out s);

            // Intersection occurs when distance s falls within [-r,+r] interval
            if (s > r)
                planeIntersectionType = PlaneIntersectionType.Front;
            else if (s < -r)
                planeIntersectionType = PlaneIntersectionType.Back;
            else
                planeIntersectionType = PlaneIntersectionType.Intersecting;
            return;
        }

        private void Intersects2(ref Plane plane, out PlaneIntersectionType planeIntersectionType)
        {
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
            var distanceSquared = plane.Normal.X * negativeVertex.X + plane.Normal.Y * negativeVertex.Y + plane.Normal.Z * negativeVertex.Z + plane.D;
            if (distanceSquared > 0)
            {
                planeIntersectionType = PlaneIntersectionType.Front;
                return;
            }

            // Inline Vector3.Dot(plane.Normal, positiveVertex) + plane.D;
            distanceSquared = plane.Normal.X * positiveVertex.X + plane.Normal.Y * positiveVertex.Y + plane.Normal.Z * positiveVertex.Z + plane.D;
            if (distanceSquared < 0)
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

        #region Hull

        public override Vector3[] HullCorners(ref Vector3 eye)
        {
            // transform eye to BB coordinates
            Vector3 e = new Vector3(eye.X - Center.X, eye.Y - Center.Y, eye.Z - Center.Z);

            // compute 6-bit code to classify eye with respect to the 6 defining planes
            int pos = 0;
            pos += (e.X < -HalfSize.X) ? 1 << 0 : 0; //  1 = left
            pos += (e.X > +HalfSize.X) ? 1 << 1 : 0; //  2 = right
            pos += (e.Y < -HalfSize.Y) ? 1 << 2 : 0; //  4 = bottom
            pos += (e.Y > +HalfSize.Y) ? 1 << 3 : 0; //  8 = top
            pos += (e.Z < -HalfSize.Z) ? 1 << 5 : 0; // 32 = back !!!
            pos += (e.Z > +HalfSize.Z) ? 1 << 4 : 0; // 16 = front !!!

            // return empty array if inside
            if (pos == 0)
            {
                return new Vector3[0];
            }

            // look up number of vertices
            pos *= 7;
            int count = HULL_LOOKUP_TABLE[pos];
            if (count == 0)
            {
                throw new InvalidOperationException("invalid hull lookup index: " + pos);
            }

            // compute the hull
            Vector3[] dst = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                int j = HULL_LOOKUP_TABLE[++pos];
                Vector3 v = BB_HULL_VERTICES[j];
                v.X *= HalfSize.X;
                v.Y *= HalfSize.Y;
                v.Z *= HalfSize.Z;
                v.X += Center.X;
                v.Y += Center.Y;
                v.Z += Center.Z;
                dst[i] = v;
            }
            return dst;
        }

        public override Vector3[] HullProjectedCorners(ref Vector3 eye, ProjectToScreen projectToScreen)
        {
            Vector3[] dst = this.HullCorners(ref eye);
            for (int i = 0; i < dst.Length; i++)
            {
                Vector3 v = dst[i];
                dst[i] = new Vector3(projectToScreen(ref v), 0);
            }
            return dst;
        }

        public override float HullArea(ref Vector3 eye, ProjectToScreen projectToScreen)
        {
            // transform eye to BB coordinates
            Vector3 e = new Vector3(eye.X - Center.X, eye.Y - Center.Y, eye.Z - Center.Z);

            // compute 6-bit code to classify eye with respect to the 6 defining planes
            int pos = 0;
            pos += ((e.X < -HalfSize.X ? 1 : 0) << 0); //  1 = left
            pos += ((e.X > +HalfSize.X ? 1 : 0) << 1); //  2 = right
            pos += ((e.Y < -HalfSize.Y ? 1 : 0) << 2); //  4 = bottom
            pos += ((e.Y > +HalfSize.Y ? 1 : 0) << 3); //  8 = top
            pos += ((e.Z < -HalfSize.Z ? 1 : 0) << 5); // 32 = back !!!
            pos += ((e.Z > +HalfSize.Z ? 1 : 0) << 4); // 16 = front !!!

            // return -1 if inside
            if (pos == 0)
            {
                return -1.0f;
            }

            // look up number of vertices
            pos *= 7;
            int count = HULL_LOOKUP_TABLE[pos];
            if (count == 0)
            {
                throw new InvalidOperationException("invalid hull lookup index: " + pos);
            }

            // project hull vertices
            // TODO this loop can be merged into the next one (and get rid of dst array)
            Vector2[] dst = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                int j = HULL_LOOKUP_TABLE[++pos];
                Vector3 v = BB_HULL_VERTICES[j];
                v.X *= HalfSize.X;
                v.Y *= HalfSize.Y;
                v.Z *= HalfSize.Z;
                v.X += Center.X;
                v.Y += Center.Y;
                v.Z += Center.Z;
                dst[i] = projectToScreen(ref v);
            }

            // compute the area of the polygon using a contour integral
            float sum = (dst[count - 1].X - dst[0].X) * (dst[count - 1].Y + dst[0].Y);
            for (int i = 0; i < count - 1; i++)
            {
                sum += (dst[i].X - dst[i + 1].X) * (dst[i].Y + dst[i + 1].Y);
            }
            // return corrected value
            return sum * 0.5f;
        }

        public override int[] HullIndices(ref Vector3 eye)
        {
            throw new NotImplementedException();
        }

        public override Vector3[] HullCornersFromDirection(ref Vector3 dir)
        {
            throw new NotImplementedException();
        }

        public override int[] HullIndicesFromDirection(ref Vector3 dir)
        {
            throw new NotImplementedException();
        }

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
            throw new NotImplementedException();
        }

        public override float DistanceSquaredTo(Vector3 point)
        {
            throw new NotImplementedException();
        }

        public override float DistanceFromEdgeTo(Vector3 point)
        {
            throw new NotImplementedException();
        }

        public override float GetVolume()
        {
            throw new NotImplementedException();
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

        #endregion

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
    }
}