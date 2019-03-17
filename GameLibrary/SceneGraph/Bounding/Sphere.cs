using Microsoft.Xna.Framework;
using GameLibrary.Util;
using System;
using static GameLibrary.VolumeUtil;

namespace GameLibrary.SceneGraph.Bounding
{
    public class Sphere : Volume
    {
        #region Private Fields

        private static readonly float EPSILON = 0.00001f;
        private static readonly float RADIUS_EPSILON = 1.0f + EPSILON;

        private Vector3 center;
        private float radius;

        #endregion

        #region Properties

        public Vector3 Center
        {
            get { return center; }
            set { center = value; }
        }

        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        #endregion

        #region Constructors

        public Sphere()
        {
            center = Vector3.Zero;
            radius = 0;
        }

        public Sphere(float radius)
        {
            this.center = Vector3.Zero;
            this.radius = radius;
        }

        public Sphere(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public Sphere(Sphere sphere)
        {
            center = sphere.center;
            radius = sphere.Radius;
        }

        #endregion

        #region Public Methods

        public override VolumeType Type()
        {
            return VolumeType.Sphere;
        }

        public override Volume Clone()
        {
            return new Sphere(this);
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

        // FIXME inline
        public override void Contains(ref Vector3 point, out bool result)
        {
            result = (Vector3.DistanceSquared(Center, point) <= Radius * Radius);
        }

        #endregion

        #region Intersects

        // FIXME inline
        public override bool Intersects(Box box)
        {
            return box.Intersects(this);
        }

        // FIXME inline
        public override bool Intersects(Sphere sphere)
        {
            float sumRadius = Radius + sphere.Radius;
            return (Vector3.DistanceSquared(Center, sphere.Center) <= sumRadius * sumRadius);
        }

        public override bool Intersects(Frustum frustum)
        {
            throw new NotImplementedException();
        }

        public override void Intersects(ref Plane plane, out PlaneIntersectionType planeIntersectionType)
        {
            var distance = default(float);
            // TODO: we might want to inline this for performance reasons
            Vector3.Dot(ref plane.Normal, ref center, out distance);
            distance += plane.D;
            if (distance > this.Radius)
                planeIntersectionType = PlaneIntersectionType.Front;
            else if (distance < -this.Radius)
                planeIntersectionType = PlaneIntersectionType.Back;
            else
                planeIntersectionType = PlaneIntersectionType.Intersecting;
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
            throw new NotImplementedException();
        }

        public override Vector3[] HullProjectedCorners(ref Vector3 eye, ProjectToScreen projectToScreen)
        {
            throw new NotImplementedException();
        }

        public override float HullArea(ref Vector3 eye, ProjectToScreen projectToScreen)
        {
            throw new NotImplementedException();
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

        /// <summary>
        /// Computes this BoundVolume from a set of 3D points
        /// </summary>
        /// <param name="points">Array of Vectors</param>
        public void ComputeFromPoints(Vector3[] points)
        {
            //Vector3[] copy = new Vector3[points.Length];
            //System.Array.Copy(points, copy, points.Length);
            //CalculateWelzl(copy, copy.Length, 0, 0);
            Util.Sphere s = new Util.Sphere();
            SphereUtil.FromPoints(ref s, points);
            Radius = s.radius;
            Center = s.center;
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

        public override float DistanceTo(Vector3 point)
        {
            throw new NotImplementedException();
        }

        public override float DistanceSquaredTo(Vector3 point)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Compute the distance from the nearest edge of the volume
        /// to the point
        /// </summary>
        /// <param name="point">Vector3</param>
        /// <returns>Distance from neatest edge</returns>
        public override float DistanceFromEdgeTo(Vector3 point)
        {
            return Vector3.Distance(Center, point) - Radius;
        }

        /// <summary>
        /// Return the volume of this bounding volume.
        /// </summary>
        /// <returns>Volume</returns>
        public override float GetVolume()
        {
            return (float)(4 * (1 / 3) * Math.PI * Radius * Radius * Radius);
        }

        /// <summary>
        /// Merges the two bound volumes into a brand new
        /// bounding volume and leaves the two unchanged.
        /// </summary>
        /// <param name="bv">BoundVolume to merge with</param>
        /// <returns>A new volume containing both volumes</returns>
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
                    return Merge(sphere.Radius, sphere.Center, new Sphere());
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
                    return Merge(sphere.Radius, sphere.Center, this);
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
        public override Volume Transform(Vector3 scale, Quaternion rotation, Vector3 translation, Volume store)
        {
            Sphere rVal = store as Sphere;
            if (store == null)
            {
                rVal = new Sphere();
            }

            rVal.Center = center * scale;
            rVal.Center = Vector3.Transform(center, rotation);
            rVal.Center += translation;
            rVal.Radius = Math.Abs(VectorUtil.GetMaxAxis(scale) * radius) + EPSILON;

            return rVal;
        }

        public override Volume Transform(Matrix m, Volume store)
        {
            Sphere rVal = store as Sphere;
            if (store == null)
            {
                rVal = new Sphere();
            }

            Vector3 scale;// = new Vector3();
            Quaternion rotation;// = new Quaternion(); ;
            Vector3 translation;// = new Vector3();
            if (m.Decompose(out scale, out rotation, out translation))
            {
                rVal.Center = Vector3.Transform(center, m);
                rVal.Radius = Math.Abs(VectorUtil.GetMaxAxis(scale) * Radius) + EPSILON;
                return rVal;
            }
            return null;
        }

        public override void WorldMatrix(out Matrix m)
        {
            m = Matrix.CreateScale(Radius) * Matrix.CreateTranslation(Center);
        }

        #endregion

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

        private Sphere Merge(float radius, Vector3 center, Sphere sphere)
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
            else if (!(obj is Sphere))
            {
                return false;
            }
            Sphere b = obj as Sphere;
            return (Center.Equals(b.Center)) && (Radius.Equals(b.Radius));
        }

        public override int GetHashCode()
        {
            return Center.GetHashCode() + Radius.GetHashCode();
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