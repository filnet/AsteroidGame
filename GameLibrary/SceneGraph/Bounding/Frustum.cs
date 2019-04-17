using Microsoft.Xna.Framework;
using System;
using static GameLibrary.VolumeUtil;

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
            Array.Copy(frustum.planes, planes, PlaneCount);
            Array.Copy(frustum.corners, corners, CornerCount);
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
            return VolumeType.Frustum;
        }

        #region Contains

        public override ContainmentType Contains(Box box, ContainmentHint hint)
        {
            // To be accurate SAT needs quite a few axis:
            //
            // Box / Box
            // a - 3 axes from object A (face normals)
            // b - 3 axes from object B (face normals)
            // c - 9 axes from all the cross products of pairs of edges of A and B (3x3)
            // Total 15
            //
            // AABox / AABox
            // a - 3 axes from object A (face normals)
            // b - 0 axes from object B (given by A)
            // c - 0 axes from all the cross products of pairs of edges of A and B (given by A again)
            // Total 3
            //
            // Frustum/Box we should have a = 5, b = 3, c = 15 (for a total of 23...) - TO CONFIRM
            // a - 5 axes from frustum (face normals) : near/far + 4 sides
            // b - 3 axes from object B
            // c - 15 axes from all the cross products of pairs of edges of A and B (5x3)
            // Total 23
            // currently only a and b (8 axis) are done

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
                // check frustum outside/inside box
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
            result = Intersects(ref plane, ref corners[0]);
            for (int i = 1; i < CornerCount; i++)
            {
                if (Intersects(ref plane, ref corners[i]) != result)
                {
                    result = PlaneIntersectionType.Intersecting;
                }
            }
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

        #region Hull

        public override Vector3[] HullCorners(ref Vector3 eye)
        {
            // compute 6-bit code to classify eye with respect to the 6 defining planes
            int pos = 0;
            pos += (ClassifyPoint(ref planes[2], ref eye) > 0.0f) ? 1 << 0 : 0; //  1 = left
            pos += (ClassifyPoint(ref planes[3], ref eye) > 0.0f) ? 1 << 1 : 0; //  2 = right
            pos += (ClassifyPoint(ref planes[5], ref eye) > 0.0f) ? 1 << 2 : 0; //  4 = bottom
            pos += (ClassifyPoint(ref planes[4], ref eye) > 0.0f) ? 1 << 3 : 0; //  8 = top
            pos += (ClassifyPoint(ref planes[1], ref eye) > 0.0f) ? 1 << 5 : 0; // 32 = back / far !!!
            pos += (ClassifyPoint(ref planes[0], ref eye) > 0.0f) ? 1 << 4 : 0; // 16 = front / near !!!

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
                // FIXME frustrum corners are not ordered the same as BB_HULL_VERTICES
                if (j < 4) j = 3 - j; else j = 11 - j;
                dst[i] = corners[j];
            }
            return dst;
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
            // compute 6-bit code to classify eye with respect to the 6 defining planes
            int pos = 0;
            pos += (ClassifyPoint(ref planes[2], ref eye) > 0.0f) ? 1 << 0 : 0; //  1 = left
            pos += (ClassifyPoint(ref planes[3], ref eye) > 0.0f) ? 1 << 1 : 0; //  2 = right
            pos += (ClassifyPoint(ref planes[5], ref eye) > 0.0f) ? 1 << 2 : 0; //  4 = bottom
            pos += (ClassifyPoint(ref planes[4], ref eye) > 0.0f) ? 1 << 3 : 0; //  8 = top
            pos += (ClassifyPoint(ref planes[1], ref eye) > 0.0f) ? 1 << 5 : 0; // 32 = back / far !!!
            pos += (ClassifyPoint(ref planes[0], ref eye) > 0.0f) ? 1 << 4 : 0; // 16 = front / near !!!

            // return empty array if inside
            if (pos == 0)
            {
                return new int[0];
            }

            // look up number of vertices
            pos *= 7;
            int count = HULL_LOOKUP_TABLE[pos];
            if (count == 0)
            {
                throw new InvalidOperationException("invalid hull lookup index: " + pos);
            }

            // compute the hull
            int[] dst = new int[count];
            for (int i = 0; i < count; i++)
            {
                int j = HULL_LOOKUP_TABLE[++pos];
                // FIXME frustrum corners are not ordered the same as BB_HULL_VERTICES
                if (j < 4) j = 3 - j; else j = 11 - j;
                dst[i] = j;
            }
            return dst;
        }

        public override Vector3[] HullCornersFromDirection(ref Vector3 dir)
        {
            // compute 6-bit code to classify eye with respect to the 6 defining planes
            int pos = 0;
            pos += (planes[2].DotNormal(dir) > 0.0f) ? 1 << 0 : 0; //  1 = left
            pos += (planes[3].DotNormal(dir) > 0.0f) ? 1 << 1 : 0; //  2 = right
            pos += (planes[5].DotNormal(dir) > 0.0f) ? 1 << 2 : 0; //  4 = bottom
            pos += (planes[4].DotNormal(dir) > 0.0f) ? 1 << 3 : 0; //  8 = top
            pos += (planes[1].DotNormal(dir) > 0.0f) ? 1 << 5 : 0; // 32 = back / far !!!
            pos += (planes[0].DotNormal(dir) > 0.0f) ? 1 << 4 : 0; // 16 = front / near !!!

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
                // FIXME frustrum corners are not ordered the same as BB_HULL_VERTICES
                if (j < 4) j = 3 - j; else j = 11 - j;
                dst[i] = corners[j];
            }
            return dst;
        }

        public override int[] HullIndicesFromDirection(ref Vector3 dir)
        {
            // compute 6-bit code to classify direction with respect to the 6 defining planes
            int pos = 0;
            pos += (planes[2].DotNormal(dir) > 0.0f) ? 1 << 0 : 0; //  1 = left
            pos += (planes[3].DotNormal(dir) > 0.0f) ? 1 << 1 : 0; //  2 = right
            pos += (planes[5].DotNormal(dir) > 0.0f) ? 1 << 2 : 0; //  4 = bottom
            pos += (planes[4].DotNormal(dir) > 0.0f) ? 1 << 3 : 0; //  8 = top
            pos += (planes[1].DotNormal(dir) > 0.0f) ? 1 << 5 : 0; // 32 = back / far !!!
            pos += (planes[0].DotNormal(dir) > 0.0f) ? 1 << 4 : 0; // 16 = front / near !!!

            // return empty array if inside
            if (pos == 0)
            {
                return new int[0];
            }

            // look up number of vertices
            pos *= 7;
            int count = HULL_LOOKUP_TABLE[pos];
            if (count == 0)
            {
                throw new InvalidOperationException("invalid hull lookup index: " + pos);
            }

            // compute the hull
            int[] dst = new int[count];
            for (int i = 0; i < count; i++)
            {
                int j = HULL_LOOKUP_TABLE[++pos];
                // FIXME frustrum corners are not ordered the same as BB_HULL_VERTICES
                if (j < 4) j = 3 - j; else j = 11 - j;
                dst[i] = j;
            }
            return dst;
        }

        public /*override*/ bool RegionFromDirection(ref Vector3 dir, Region region)
        {
            // 
            region.Clear();

            // compute 6-bit code to classify eye with respect to the 6 defining planes
            // and add back faces
            int pos = 0;
            if (planes[2].DotNormal(dir) > 0.0f)
            {
                pos |= 1 << 0; //  1 = left
                region.addPlane(ref planes[2]);
            }
            if (planes[3].DotNormal(dir) > 0.0f)
            {
                pos |= 1 << 1; //  2 = right
                region.addPlane(ref planes[3]);
            }
            if (planes[5].DotNormal(dir) > 0.0f)
            {
                pos |= 1 << 2; //  4 = bottom
                region.addPlane(ref planes[5]);
            }
            if (planes[4].DotNormal(dir) > 0.0f)
            {
                pos |= 1 << 3; //  8 = top
                region.addPlane(ref planes[4]);
            }
            if (planes[1].DotNormal(dir) > 0.0f)
            {
                pos |= 1 << 5; // 32 = back / far !!!
                region.addPlane(ref planes[1]);
            }
            if (planes[0].DotNormal(dir) > 0.0f)
            {
                pos |= 1 << 4; // 16 = front / near !!!
                region.addPlane(ref planes[0]);
            }

            // return false if inside
            // FIXME makes no sense for a direction to be inside the frustum...
            if (pos == 0)
            {
                return false;// new Vector3[0];
            }

            // look up number of vertices
            pos *= 7;
            int count = HULL_LOOKUP_TABLE[pos];

            //Console.WriteLine((pos / 7) + " " + region.PlaneCount + " " + count);

            if (count == 0)
            {
                throw new InvalidOperationException("invalid hull lookup index: " + pos);
            }

            // add corners
            // the frustum and the generated region share the same corners
            // note: this is true only because the region is open ended (no near plane)
            // note: unlike for the frustum, it is not possible to use the corners to draw the region
            //       as we don't know how to connect them into faces..
            // FIXME : in some cases, some corners are redundant (they are inside the region and not on its boundary
            //         they don't cause harm... but waste CPU (see case 32 is such a case...)
            // FIXME : performances...
            for (int i = 0; i < CornerCount; i++)
            {
                region.addCorner(ref corners[i]);
            }
            Vector3 fakeDir = 100000.0f * dir;

            // compute lateral faces
            Vector3[] dst = new Vector3[count];
            int index = HULL_LOOKUP_TABLE[pos + count];
            // FIXME frustrum corners are not ordered the same as BB_HULL_VERTICES
            if (index < 4) index = 3 - index; else index = 11 - index;
            Vector3 v1 = corners[index];
            for (int i = 0; i < count; i++)
            {
                index = HULL_LOOKUP_TABLE[++pos];
                // FIXME frustrum corners are not ordered the same as BB_HULL_VERTICES
                if (index < 4) index = 3 - index; else index = 11 - index;
                Vector3 v2 = corners[index];

                Plane p = new Plane(v1, v2, v1 - dir);
                region.addPlane(ref p);

                // add fake corners...
                // HACK
                Vector3 c = v1 - fakeDir;
                region.addCorner(ref c);

                v1 = v2;
            }
            return true;
        }

        #endregion

        public void ComputeBoundingBox(Bounding.Box box)
        {
            box.ComputeFromPoints(corners);
        }

        public void NearFaceCenter(float dz, out Vector3 center)
        {
            Vector3 nearFaceCenter = (corners[0] + corners[2]) / 2;
            Vector3 farFaceCenter = (corners[4] + corners[6]) / 2;
            Vector3 dir = Vector3.Normalize(farFaceCenter - nearFaceCenter);
            center = nearFaceCenter + dir * (float)dz;
        }

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

        public void GetTransformedCorners(Vector3[] corners, ref Matrix m)
        {
            if (corners == null) throw new ArgumentNullException("corners");
            if (corners.Length < CornerCount) throw new ArgumentOutOfRangeException("corners");

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3.Transform(ref this.corners[i], ref m, out corners[i]);
            }
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

        #endregion
    }
}

