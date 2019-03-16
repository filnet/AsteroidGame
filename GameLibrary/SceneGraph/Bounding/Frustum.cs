using Microsoft.Xna.Framework;
using System;

namespace GameLibrary.SceneGraph.Bounding
{
    public class Frustum : Volume
    {
        #region Private Fields

        private Matrix matrix;
        private readonly Vector3[] corners = new Vector3[CornerCount];
        private readonly Plane[] planes = new Plane[PlaneCount];

        #endregion

        #region Public Fields

        /// <summary>
        /// The number of planes in the frustum.
        /// </summary>
        public const int PlaneCount = 6;

        /// <summary>
        /// The number of corner corners in the frustum.
        /// </summary>
        public const int CornerCount = 8;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="Matrix"/> of the frustum.
        /// </summary>
        public Matrix Matrix
        {
            get { return matrix; }
            set
            {
                matrix = value;
                CreatePlanes();    // FIXME: The odds are the planes will be used a lot more often than the matrix
                CreateCorners();   // is updated, so this should help performance. I hope ;)
            }
        }

        /// <summary>
        /// Gets the near plane of the frustum.
        /// </summary>
        public Plane Near
        {
            get { return planes[0]; }
        }

        /// <summary>
        /// Gets the far plane of the frustum.
        /// </summary>
        public Plane Far
        {
            get { return planes[1]; }
        }

        /// <summary>
        /// Gets the left plane of the frustum.
        /// </summary>
        public Plane Left
        {
            get { return planes[2]; }
        }

        /// <summary>
        /// Gets the right plane of the frustum.
        /// </summary>
        public Plane Right
        {
            get { return planes[3]; }
        }

        /// <summary>
        /// Gets the top plane of the frustum.
        /// </summary>
        public Plane Top
        {
            get { return planes[4]; }
        }

        /// <summary>
        /// Gets the bottom plane of the frustum.
        /// </summary>
        public Plane Bottom
        {
            get { return planes[5]; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the frustum by extracting the view planes from a matrix.
        /// </summary>
        /// <param name="value">Combined matrix which usually is (View * Projection).</param>
        public Frustum(Matrix value)
        {
            matrix = value;
            CreatePlanes();
            CreateCorners();
        }

        public Frustum(Frustum frustum)
        {
            matrix = frustum.matrix;
            Array.Copy(frustum.planes, planes, frustum.planes.Length);
            Array.Copy(frustum.corners, corners, frustum.corners.Length);
        }

        public Frustum()
        {
        }

        #endregion

        #region Operators

        /// <summary>
        /// Compares whether two <see cref="Frustum"/> instances are equal.
        /// </summary>
        /// <param name="a"><see cref="Frustum"/> instance on the left of the equal sign.</param>
        /// <param name="b"><see cref="Frustum"/> instance on the right of the equal sign.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(Frustum a, Frustum b)
        {
            if (Equals(a, null))
                return (Equals(b, null));

            if (Equals(b, null))
                return (Equals(a, null));

            return a.matrix == (b.matrix);
        }

        /// <summary>
        /// Compares whether two <see cref="Frustum"/> instances are not equal.
        /// </summary>
        /// <param name="a"><see cref="Frustum"/> instance on the left of the not equal sign.</param>
        /// <param name="b"><see cref="Frustum"/> instance on the right of the not equal sign.</param>
        /// <returns><c>true</c> if the instances are not equal; <c>false</c> otherwise.</returns>
        public static bool operator !=(Frustum a, Frustum b)
        {
            return !(a == b);
        }

        #endregion

        #region Public Methods

        public override Volume Clone()
        {
            return new Frustum(this);
        }

        public override VolumeType Type()
        {
            return VolumeType.AABB;
        }

        #region Contains

        public override ContainmentType Contains(Box box, ContainmentHint hint)
        {
            var intersects = false;
            for (int i = 0; i < Frustum.PlaneCount; i++)
            {
                PlaneIntersectionType planeIntersectionType;
                box.Intersects(ref planes[i], out planeIntersectionType);
                switch (planeIntersectionType)
                {
                    case PlaneIntersectionType.Front:
                        return ContainmentType.Disjoint;
                    case PlaneIntersectionType.Intersecting:
                        intersects = true;
                        break;
                }
            }
            if (!intersects)
            {
                return ContainmentType.Contains;
            }

            if (hint == ContainmentHint.Precise)
            {
                int c;
                // check frustum outside/inside box
                c = 0;
                for (int i = 0; i < corners.Length; i++)
                {
                    c += ((corners[i].X > box.Center.X + box.HalfSize.X) ? 1 : 0);
                }
                if (c == 8) return ContainmentType.Disjoint;
                c = 0;
                for (int i = 0; i < corners.Length; i++)
                {
                    c += ((corners[i].X < box.Center.X - box.HalfSize.X) ? 1 : 0);
                }
                if (c == 8) return ContainmentType.Disjoint;

                c = 0;
                for (int i = 0; i < corners.Length; i++)
                {
                    c += ((corners[i].Y > box.Center.Y + box.HalfSize.Y) ? 1 : 0);
                }
                if (c == 8) return ContainmentType.Disjoint;
                c = 0;
                for (int i = 0; i < corners.Length; i++)
                {
                    c += ((corners[i].Y < box.Center.Y - box.HalfSize.Y) ? 1 : 0);
                }
                if (c == 8) return ContainmentType.Disjoint;

                c = 0;
                for (int i = 0; i < corners.Length; i++)
                {
                    c += ((corners[i].Z > box.Center.Z + box.HalfSize.Z) ? 1 : 0);
                }
                if (c == 8) return ContainmentType.Disjoint;
                c = 0;
                for (int i = 0; i < corners.Length; i++)
                {
                    c += ((corners[i].Z < box.Center.Z - box.HalfSize.Z) ? 1 : 0);
                }
                if (c == 8) return ContainmentType.Disjoint;
            }

            return ContainmentType.Intersects;
        }

        public override ContainmentType Contains(Sphere sphere)
        {
            var intersects = false;
            for (var i = 0; i < PlaneCount; ++i)
            {
                PlaneIntersectionType planeIntersectionType;
                // TODO: we might want to inline this for performance reasons
                sphere.Intersects(ref planes[i], out planeIntersectionType);
                switch (planeIntersectionType)
                {
                    case PlaneIntersectionType.Front:
                        return ContainmentType.Disjoint;
                    case PlaneIntersectionType.Intersecting:
                        intersects = true;
                        break;
                }
            }
            return intersects ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        public override ContainmentType Contains(Frustum frustum)
        {
            // We check to see if the two frustums are equal
            // If they are, there's no need to go any further.
            if (this == frustum)
            {
                return ContainmentType.Contains;
            }
            var intersects = false;
            for (var i = 0; i < PlaneCount; ++i)
            {
                PlaneIntersectionType planeIntersectionType;
                frustum.Intersects(ref planes[i], out planeIntersectionType);
                switch (planeIntersectionType)
                {
                    case PlaneIntersectionType.Front:
                        return ContainmentType.Disjoint;
                    case PlaneIntersectionType.Intersecting:
                        intersects = true;
                        break;
                }
            }
            return intersects ? ContainmentType.Intersects : ContainmentType.Contains;
        }

        // FIXME duplicated in VectorUtil
        public static float ClassifyPoint(ref Vector3 point, ref Plane plane)
        {
            return point.X * plane.Normal.X + point.Y * plane.Normal.Y + point.Z * plane.Normal.Z + plane.D;
        }

        public override void Contains(ref Vector3 point, out bool result)
        {
            for (var i = 0; i < PlaneCount; ++i)
            {
                // TODO: we might want to inline this for performance reasons
                if (ClassifyPoint(ref point, ref planes[i]) > 0)
                {
                    result = false;
                    return;
                }
            }
            result = true;
        }

        #endregion

        #region Contains

        public override bool Intersects(Box box)
        {
            return (Contains(box, ContainmentHint.Precise) != ContainmentType.Disjoint);
        }

        public override bool Intersects(Sphere sphere)
        {
            return (Contains(sphere) != ContainmentType.Disjoint);
        }

        public override bool Intersects(Frustum frustum)
        {
            return (Contains(frustum) != ContainmentType.Disjoint);
        }


        public override void Intersects(ref Plane plane, out PlaneIntersectionType result)
        {
            result = Intersects(ref plane, ref corners[0]);
            for (int i = 1; i < corners.Length; i++)
            {
                if (Intersects(ref plane, ref corners[i]) != result)
                {
                    result = PlaneIntersectionType.Intersecting;
                }
            }
        }

        // Taken from XNA Frustum
        internal static PlaneIntersectionType Intersects(ref Plane plane, ref Vector3 point)
        {
            float distance;
            plane.DotCoordinate(ref point, out distance);

            if (distance > 0)
                return PlaneIntersectionType.Front;

            if (distance < 0)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }

        /// <summary>
        /// Gets the distance of intersection of <see cref="Ray"/> and this <see cref="Frustum"/> or null if no intersection happens.
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> for intersection test.</param>
        /// <returns>Distance at which ray intersects with this <see cref="Frustum"/> or null if no intersection happens.</returns>
        public float? Intersects(Ray ray)
        {
            float? result;
            Intersects(ref ray, out result);
            return result;
        }

        /// <summary>
        /// Gets the distance of intersection of <see cref="Ray"/> and this <see cref="Frustum"/> or null if no intersection happens.
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> for intersection test.</param>
        /// <param name="result">Distance at which ray intersects with this <see cref="Frustum"/> or null if no intersection happens as an output parameter.</param>
        public void Intersects(ref Ray ray, out float? result)
        {
            result = 0.0f;
        }

        #endregion

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Frustum"/>.
        /// </summary>
        /// <param name="other">The <see cref="Frustum"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public bool Equals(Frustum other)
        {
            return (this == other);
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Frustum"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            return (obj is Frustum) && this == ((Frustum)obj);
        }

        /// <summary>
        /// Gets the hash code of this <see cref="Frustum"/>.
        /// </summary>
        /// <returns>Hash code of this <see cref="Frustum"/>.</returns>
        public override int GetHashCode()
        {
            return matrix.GetHashCode();
        }

        /// <summary>
        /// Returns a copy of internal corners array.
        /// </summary>
        /// <returns>The array of corners.</returns>
        public Vector3[] GetCorners()
        {
            return (Vector3[])corners.Clone();
        }

        /// <summary>
        /// Returns a copy of internal corners array.
        /// </summary>
        /// <param name="corners">The array which values will be replaced to corner values of this instance. It must have size of <see cref="Frustum.CornerCount"/>.</param>
        public void GetCorners(Vector3[] corners)
        {
            if (corners == null) throw new ArgumentNullException("corners");
            if (corners.Length < CornerCount) throw new ArgumentOutOfRangeException("corners");

            this.corners.CopyTo(corners, 0);
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

        public override Volume Transform(Vector3 scale, Quaternion rotation, Vector3 translation, Volume store)
        {
            return null;
        }

        public override Volume Transform(Matrix m, Volume store)
        {
            return null;
        }

        public override void WorldMatrix(out Matrix m)
        {
            m = Matrix.Identity;
        }

        /// <summary>
        /// Returns a <see cref="String"/> representation of this <see cref="Frustum"/> in the format:
        /// {Near:[nearPlane] Far:[farPlane] Left:[leftPlane] Right:[rightPlane] Top:[topPlane] Bottom:[bottomPlane]}
        /// </summary>
        /// <returns><see cref="String"/> representation of this <see cref="Frustum"/>.</returns>
        public override string ToString()
        {
            return "{Near: " + this.planes[0] +
                   " Far:" + this.planes[1] +
                   " Left:" + this.planes[2] +
                   " Right:" + this.planes[3] +
                   " Top:" + this.planes[4] +
                   " Bottom:" + planes[5] +
                   "}";
        }

        #endregion

        #region Private Methods

        private void CreateCorners()
        {
            IntersectionPoint(ref planes[0], ref planes[2], ref planes[4], out corners[0]);
            IntersectionPoint(ref planes[0], ref planes[3], ref planes[4], out corners[1]);
            IntersectionPoint(ref planes[0], ref planes[3], ref planes[5], out corners[2]);
            IntersectionPoint(ref planes[0], ref planes[2], ref planes[5], out corners[3]);
            IntersectionPoint(ref planes[1], ref planes[2], ref planes[4], out corners[4]);
            IntersectionPoint(ref planes[1], ref planes[3], ref planes[4], out corners[5]);
            IntersectionPoint(ref planes[1], ref planes[3], ref planes[5], out corners[6]);
            IntersectionPoint(ref planes[1], ref planes[2], ref planes[5], out corners[7]);
        }

        private void CreatePlanes()
        {
            planes[0] = new Plane(-matrix.M13, -matrix.M23, -matrix.M33, -matrix.M43);
            planes[1] = new Plane(matrix.M13 - matrix.M14, matrix.M23 - matrix.M24, matrix.M33 - matrix.M34, matrix.M43 - matrix.M44);
            planes[2] = new Plane(-matrix.M14 - matrix.M11, -matrix.M24 - matrix.M21, -matrix.M34 - matrix.M31, -matrix.M44 - matrix.M41);
            planes[3] = new Plane(matrix.M11 - matrix.M14, matrix.M21 - matrix.M24, matrix.M31 - matrix.M34, matrix.M41 - matrix.M44);
            planes[4] = new Plane(matrix.M12 - matrix.M14, matrix.M22 - matrix.M24, matrix.M32 - matrix.M34, matrix.M42 - matrix.M44);
            planes[5] = new Plane(-matrix.M14 - matrix.M12, -matrix.M24 - matrix.M22, -matrix.M34 - matrix.M32, -matrix.M44 - matrix.M42);

            NormalizePlane(ref planes[0]);
            NormalizePlane(ref planes[1]);
            NormalizePlane(ref planes[2]);
            NormalizePlane(ref planes[3]);
            NormalizePlane(ref planes[4]);
            NormalizePlane(ref planes[5]);
        }

        private static void IntersectionPoint(ref Plane a, ref Plane b, ref Plane c, out Vector3 result)
        {
            // Formula used
            //                d1 ( N2 * N3 ) + d2 ( N3 * N1 ) + d3 ( N1 * N2 )
            //P =   -------------------------------------------------------------------------
            //                             N1 . ( N2 * N3 )
            //
            // Note: N refers to the normal, d refers to the displacement. '.' means dot product. '*' means cross product

            Vector3 v1, v2, v3;
            Vector3 cross;

            Vector3.Cross(ref b.Normal, ref c.Normal, out cross);

            float f;
            Vector3.Dot(ref a.Normal, ref cross, out f);
            f *= -1.0f;

            Vector3.Cross(ref b.Normal, ref c.Normal, out cross);
            Vector3.Multiply(ref cross, a.D, out v1);
            //v1 = (a.D * (Vector3.Cross(b.Normal, c.Normal)));


            Vector3.Cross(ref c.Normal, ref a.Normal, out cross);
            Vector3.Multiply(ref cross, b.D, out v2);
            //v2 = (b.D * (Vector3.Cross(c.Normal, a.Normal)));


            Vector3.Cross(ref a.Normal, ref b.Normal, out cross);
            Vector3.Multiply(ref cross, c.D, out v3);
            //v3 = (c.D * (Vector3.Cross(a.Normal, b.Normal)));

            result.X = (v1.X + v2.X + v3.X) / f;
            result.Y = (v1.Y + v2.Y + v3.Y) / f;
            result.Z = (v1.Z + v2.Z + v3.Z) / f;
        }

        private void NormalizePlane(ref Plane p)
        {
            float factor = 1f / p.Normal.Length();
            p.Normal.X *= factor;
            p.Normal.Y *= factor;
            p.Normal.Z *= factor;
            p.D *= factor;
        }

        #endregion
    }
}

