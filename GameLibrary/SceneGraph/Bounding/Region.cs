using Microsoft.Xna.Framework;
using System;
using static GameLibrary.VolumeUtil;

namespace GameLibrary.SceneGraph.Bounding
{
    public class Region : Volume
    {
        #region Private Fields

        //private Matrix matrix;
        private readonly Vector3[] corners = new Vector3[MaxCornerCount];
        private readonly Plane[] planes = new Plane[MaxPlaneCount];

        #endregion

        #region Public Fields

        /// <summary>
        /// The number of planes in the frustum.
        /// </summary>
        public int PlaneCount;

        public const int MaxPlaneCount = 10;

        /// <summary>
        /// The number of corner corners in the frustum.
        /// </summary>
        public const int MaxCornerCount = 8 + 6;

        public int CornerCount;

        #endregion

        #region Properties

        #endregion

        #region Constructors

        public Region(Region region)
        {
            Array.Copy(region.planes, planes, region.PlaneCount);
            Array.Copy(region.corners, corners, region.CornerCount);
        }

        public Region()
        {
        }

        #endregion

        #region Operators

        /// <summary>
        /// Compares whether two <see cref="Region"/> instances are equal.
        /// </summary>
        /// <param name="a"><see cref="Region"/> instance on the left of the equal sign.</param>
        /// <param name="b"><see cref="Region"/> instance on the right of the equal sign.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(Region a, Region b)
        {
            if (Equals(a, null))
                return (Equals(b, null));

            if (Equals(b, null))
                return (Equals(a, null));

            return false; //a.matrix == (b.matrix);
        }

        /// <summary>
        /// Compares whether two <see cref="Region"/> instances are not equal.
        /// </summary>
        /// <param name="a"><see cref="Region"/> instance on the left of the not equal sign.</param>
        /// <param name="b"><see cref="Region"/> instance on the right of the not equal sign.</param>
        /// <returns><c>true</c> if the instances are not equal; <c>false</c> otherwise.</returns>
        public static bool operator !=(Region a, Region b)
        {
            return !(a == b);
        }

        #endregion

        #region Public Methods

        public override Volume Clone()
        {
            return new Region(this);
        }

        public override VolumeType Type()
        {
            return VolumeType.Region;
        }

        public void Clear()
        {
            PlaneCount = 0;
            CornerCount = 0;
        }

        public void addPlane(ref Plane plane)
        {
            planes[PlaneCount] = plane;
            PlaneCount++;
        }

        public void addCorner(ref Vector3 point)
        {
            corners[CornerCount] = point;
            CornerCount++;
        }

        #region Contains

        public override ContainmentType Contains(Box box, ContainmentHint hint)
        {
            // See frustum for comments
            var intersects = false;
            for (int i = 0; i < PlaneCount; i++)
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
                // check region outside/inside box
                // https://iquilezles.org/www/articles/frustumcorrect/frustumcorrect.htm

                // FIXME use "SAT" to reduce the number of loops...
                // FIXME do Y last ?
                int c;
                c = 0;
                for (int i = 0; i < CornerCount; i++)
                {
                    c += ((corners[i].X > box.Center.X + box.HalfSize.X) ? 1 : 0);
                }
                if (c == CornerCount) return ContainmentType.Disjoint;
                c = 0;
                for (int i = 0; i < CornerCount; i++)
                {
                    c += ((corners[i].X < box.Center.X - box.HalfSize.X) ? 1 : 0);
                }
                if (c == CornerCount) return ContainmentType.Disjoint;
                c = 0;
                for (int i = 0; i < CornerCount; i++)
                {
                    c += ((corners[i].Y > box.Center.Y + box.HalfSize.Y) ? 1 : 0);
                }
                if (c == CornerCount) return ContainmentType.Disjoint;
                c = 0;
                for (int i = 0; i < CornerCount; i++)
                {
                    c += ((corners[i].Y < box.Center.Y - box.HalfSize.Y) ? 1 : 0);
                }
                if (c == CornerCount) return ContainmentType.Disjoint;
                c = 0;
                for (int i = 0; i < CornerCount; i++)
                {
                    c += ((corners[i].Z > box.Center.Z + box.HalfSize.Z) ? 1 : 0);
                }
                if (c == CornerCount) return ContainmentType.Disjoint;
                c = 0;
                for (int i = 0; i < CornerCount; i++)
                {
                    c += ((corners[i].Z < box.Center.Z - box.HalfSize.Z) ? 1 : 0);
                }
                if (c == CornerCount) return ContainmentType.Disjoint;
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

        public override void Contains(ref Vector3 point, out bool result)
        {
            for (var i = 0; i < PlaneCount; ++i)
            {
                // TODO: we might want to inline this for performance reasons
                if (ClassifyPoint(ref planes[i], ref point) > 0)
                {
                    result = false;
                    return;
                }
            }
            result = true;
        }

        #endregion

        #region Intersects

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
            /*result = Intersects(ref plane, ref corners[0]);
            for (int i = 1; i < CornerCount; i++)
            {
                if (Intersects(ref plane, ref corners[i]) != result)
                {
                    result = PlaneIntersectionType.Intersecting;
                }
            }*/
            throw new NotImplementedException();
        }

        // Taken from XNA BoundingFrustum
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
        /// Gets the distance of intersection of <see cref="Ray"/> and this <see cref="Region"/> or null if no intersection happens.
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> for intersection test.</param>
        /// <returns>Distance at which ray intersects with this <see cref="Region"/> or null if no intersection happens.</returns>
        public float? Intersects(Ray ray)
        {
            float? result;
            Intersects(ref ray, out result);
            return result;
        }

        /// <summary>
        /// Gets the distance of intersection of <see cref="Ray"/> and this <see cref="Region"/> or null if no intersection happens.
        /// </summary>
        /// <param name="ray">A <see cref="Ray"/> for intersection test.</param>
        /// <param name="result">Distance at which ray intersects with this <see cref="Region"/> or null if no intersection happens as an output parameter.</param>
        public void Intersects(ref Ray ray, out float? result)
        {
            throw new NotImplementedException();
        }

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
        /// Compares whether current instance is equal to specified <see cref="Region"/>.
        /// </summary>
        /// <param name="other">The <see cref="Region"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public bool Equals(Region other)
        {
            return (this == other);
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Region"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            return (obj is Region) && this == ((Region)obj);
        }

        /// <summary>
        /// Gets the hash code of this <see cref="Region"/>.
        /// </summary>
        /// <returns>Hash code of this <see cref="Region"/>.</returns>
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a copy of internal corners array.
        /// </summary>
        /// <returns>The array of corners.</returns>
        public Vector3[] GetCorners()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a copy of internal corners array.
        /// </summary>
        /// <param name="corners">The array which values will be replaced to corner values of this instance. It must have size of <see cref="Frustum.CornerCount"/>.</param>
        public void GetCorners(Vector3[] corners)
        {
            /*if (corners == null) throw new ArgumentNullException("corners");
            if (corners.Length < CornerCount) throw new ArgumentOutOfRangeException("corners");

            this.corners.CopyTo(corners, 0);*/
            throw new NotImplementedException();
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

        public override Volume Transform(Vector3 scale, Quaternion rotation, Vector3 translation, Volume store)
        {
            throw new NotImplementedException();
        }

        public override Volume Transform(Matrix m, Volume store)
        {
            throw new NotImplementedException();
        }

        public override void WorldMatrix(out Matrix m)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a <see cref="String"/> representation of this <see cref="Region"/> in the format:
        /// {Near:[nearPlane] Far:[farPlane] Left:[leftPlane] Right:[rightPlane] Top:[topPlane] Bottom:[bottomPlane]}
        /// </summary>
        /// <returns><see cref="String"/> representation of this <see cref="Region"/>.</returns>
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

        #endregion
    }
}

