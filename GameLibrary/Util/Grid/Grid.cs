using GameLibrary.SceneGraph;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static GameLibrary.VectorUtil;

// see:
// https://www.gamasutra.com/view/feature/192007/sponsored_the_world_of_just_cause_.php?page=2
// http://hhoppe.com/geomclipmap.pdf
namespace GameLibrary.Util.Grid
{
    // See http://hhoppe.com/perfecthash.pdf
    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public struct Point3 : IEquatable<Point3>
    {
        //public static readonly IEqualityComparer<Point3> KeyEqualityComparerInstance = new KeyEqualityComparer();

        public int X;
        public int Y;
        public int Z;

        public Point3(int X, int Y, int Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point3))
                return false;

            var other = (Point3)obj;
            return ((X == other.X) && (Y == other.Y) && (Z == other.Z));
        }

        public bool Equals(Point3 other)
        {
            return ((X == other.X) && (Y == other.Y) && (Z == other.Z));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 31) ^ Y.GetHashCode();
                hashCode = (hashCode * 127) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.Append("{X:");
            sb.Append(this.X);
            sb.Append(" Y:");
            sb.Append(this.Y);
            sb.Append(" Z:");
            sb.Append(this.Z);
            sb.Append("}");
            return sb.ToString();
        }

        internal string DebugDisplayString
        {
            get
            {
                return string.Concat(
                    this.X.ToString(), "  ",
                    this.Y.ToString(), "  ",
                    this.Z.ToString()
                );
            }
        }

        /*private sealed class KeyEqualityComparer : IEqualityComparer<Point3>
        {
            public bool Equals(Point3 key1, Point3 key2)
            {
                return ((key1.X == key2.X) && (key1.Y == key2.Y) && (key1.Z == key2.Z));
            }

            public int GetHashCode(Point3 key)
            {
                int hash = key.X;
                hash = hash * 31 + key.Y;
                hash = hash * 31 + key.Z;
                return hash;
            }
        }*/
    }

    public sealed class GridItem<T>
    {
        internal T obj;
        internal Point3 key;
    }

    public class Grid<T>
    {
        public delegate bool Visitor<K>(Grid<K> grid, GridItem<K> item, Object arg);

        protected readonly int chunkSize;

        protected readonly Point3 extentMin = new Point3(0, -2, 0);
        protected readonly Point3 extentMax = new Point3(0, 1, 0);
        //protected readonly Point3 extentMin = new Point3(0, -2, 0);
        //protected readonly Point3 extentMax = new Point3(0, 1, 0);

        private readonly Dictionary<Point3, GridItem<T>> items;

        public Grid(int chunkSize)
        {
            this.chunkSize = chunkSize;
            items = new Dictionary<Point3, GridItem<T>>();
        }

        public GridItem<T> GetItem(Point3 p)
        {
            items.TryGetValue(p, out GridItem<T> item);
            /*if (item == null)
            {
                item = new GridItem<T>();
                item.key = p;
                item.obj = objectFactory(this, item);
            }*/
            return item;
        }

        public void AddItem(GridItem<T> item)
        {
            items.Add(item.key, item);
        }

        public void ToKey(ref Vector3 v, out Point3 p)
        {
            p.X = (int)(v.X / chunkSize) + (Math.Sign(v.X) - 1) / 2;
            p.Y = (int)(v.Y / chunkSize) + (Math.Sign(v.Y) - 1) / 2;
            p.Z = (int)(v.Z / chunkSize) + (Math.Sign(v.Z) - 1) / 2;
        }

        // TODO does not belong here
        public virtual bool LoadItem(GridItem<T> item, ref Object arg)
        {
            // NOOP
            return true;
        }

        // TODO does not belong here
        /*public virtual void ClearLoadQueue()
        {
            // NOOP
        }*/

        public delegate void RasterVisitor(Point3 point);

        struct RasterVisitorDelegate<T>
        {
            public Grid<T> grid;
            public Visitor<T> visitor;
            public Object arg;

            public void RasterVisit(Point3 point)
            {
                GridItem<T> item = grid.GetItem(point);
                if (item != null && item.obj != null)
                {
                    visitor.Invoke(grid, item, arg);
                }
            }
        }

        public void Cull(RenderContext ctxt, Visitor<T> visitor, Object arg)
        {
            CullNaiveFrontToBack(ctxt, visitor, arg);
            //CullNaive(ctxt, visitor, arg);
            //CullX(ctxt, visitor, arg);
        }

        public void CullNaive(RenderContext ctxt, Visitor<T> visitor, Object arg)
        {
            RasterVisitorDelegate<T> visitorDelegate;
            visitorDelegate.grid = this;
            visitorDelegate.visitor = visitor;
            visitorDelegate.arg = arg;
            
            ctxt.CullCamera.BoundingBox.MinMax(out Vector3 min, out Vector3 max);

            ToKey(ref min, out Point3 p1);
            ToKey(ref max, out Point3 p2);

            AABB(p1, p2, visitorDelegate.RasterVisit);
        }

        public void CullNaiveFrontToBack(RenderContext ctxt, Visitor<T> visitor, Object arg)
        {
            RasterVisitorDelegate<T> visitorDelegate;
            visitorDelegate.grid = this;
            visitorDelegate.visitor = visitor;
            visitorDelegate.arg = arg;

            ctxt.CullCamera.BoundingBox.MinMax(out Vector3 min, out Vector3 max);

            ToKey(ref min, out Point3 p1);
            ToKey(ref max, out Point3 p2);

            // cap Y (floor,sky)
            p1.Y = Math.Min(extentMax.Y, Math.Max(extentMin.Y, p1.Y));
            p2.Y = Math.Min(extentMax.Y, Math.Max(extentMin.Y, p2.Y));

            Point3 size = new Point3(p2.X - p1.X + 1, p2.Y - p1.Y + 1, p2.Z - p1.Z + 1);
            //Console.WriteLine("{0} {1} {2} ({3})", size.X, size.Y,size.Z, size.X * size.Y * size.Z);

            AABBFrontToBack(ctxt.CullCamera.VisitOrder, p1, p2, visitorDelegate.RasterVisit);
        }

        public void CullX(RenderContext ctxt, Visitor<T> visitor, Object arg)
        {
            RasterVisitorDelegate<T> visitorDelegate;
            visitorDelegate.grid = this;
            visitorDelegate.visitor = visitor;
            visitorDelegate.arg = arg;

            ctxt.CullCamera.Frustum.NearFarFaceCenters(out Vector3 nearFaceCenter, out Vector3 farFaceCenter);

            ToKey(ref nearFaceCenter, out Point3 p1);
            ToKey(ref farFaceCenter, out Point3 p2);

            Line(p1, p2, visitorDelegate.RasterVisit);
        }

        public void AABBFrontToBack(int order, Point3 p1, Point3 p2, RasterVisitor visitor)
        {
            // extract permutation and signs from visit order
            int perm = order >> 3;
            // signs are pre-permuted and, so must be applied after permutating
            int signs = (order & 0b111);

            // handle permutation
            Permute<int> permutation = VectorUtil.PERMUTATIONS[perm];// (x, y, z, out x, out y, out z);
            Permute<int> invPermutation = VectorUtil.INV_PERMUTATIONS[perm];// (x, y, z, out x, out y, out z);
            permutation(p1.X, p1.Y, p1.Z, out p1.X, out p1.Y, out p1.Z);
            permutation(p2.X, p2.Y, p2.Z, out p2.X, out p2.Y, out p2.Z);

            // handle signs
            int di = 1;
            if ((signs & 0b100) != 0)
            {
                int tmp = p1.X;
                p1.X = p2.X;
                p2.X = tmp;
                di = -1;
            }
            int dj = 1;
            if ((signs & 0b010) != 0)
            {
                int tmp = p1.Y;
                p1.Y = p2.Y;
                p2.Y = tmp;
                dj = -1;
            }
            int dk = 1;
            if ((signs & 0b001) != 0)
            {
                int tmp = p1.Z;
                p1.Z = p2.Z;
                p2.Z = tmp;
                dk = -1;
            }

            int i = p1.X - di;
            do 
            {
                i += di;
                int j = p1.Y - dj;
                do
                {
                    j += dj;
                    int k = p1.Z - dk;
                    do
                    {
                        k += dk;
                        Point3 p;
                        invPermutation(i, j, k, out p.X, out p.Y, out p.Z);
                        visitor.Invoke(p);
                    }
                    while (k != p2.Z);
                }
                while (j != p2.Y);
            }
            while (i != p2.X);
        }

        public void AABB(Point3 p1, Point3 p2, RasterVisitor visitor)
        {
            Point3 p;
            for (int y = p1.Y; y <= p2.Y; y++)
            {
                for (int z = p1.Z; z <= p2.Z; z++)
                {
                    for (int x = p1.X; x <= p2.X; x++)
                    {
                        p.X = x;
                        p.Y = y;
                        p.Z = z;
                        visitor.Invoke(p);
                    }
                }
            }
        }

        public void Line(Point3 p1, Point3 p2, RasterVisitor visitor)
        {
            // Bresenham 3D 
            int dx, dy, dz;
            int l, m, n;
            int x_inc, y_inc, z_inc;
            int dx2, dy2, dz2;

            Point3 p;
            p.X = p1.X;
            p.Y = p1.Y;
            p.Z = p1.Z;

            dx = p2.X - p1.X;
            dy = p2.Y - p1.Y;
            dz = p2.Z - p1.Z;

            x_inc = (dx < 0) ? -1 : 1;
            l = Math.Abs(dx);
            y_inc = (dy < 0) ? -1 : 1;
            m = Math.Abs(dy);
            z_inc = (dz < 0) ? -1 : 1;
            n = Math.Abs(dz);

            dx2 = l << 1;
            dy2 = m << 1;
            dz2 = n << 1;

            if ((l >= m) && (l >= n))
            {
                int err_1 = dy2 - l;
                int err_2 = dz2 - l;
                for (int i = 0; i < l; i++)
                {
                    visitor.Invoke(p);
                    if (err_1 > 0)
                    {
                        p.Y += y_inc;
                        err_1 -= dx2;
                    }
                    if (err_2 > 0)
                    {
                        p.Z += z_inc;
                        err_2 -= dx2;
                    }
                    err_1 += dy2;
                    err_2 += dz2;
                    p.X += x_inc;
                }
            }
            else if ((m >= l) && (m >= n))
            {
                int err_1 = dx2 - m;
                int err_2 = dz2 - m;
                for (int i = 0; i < m; i++)
                {
                    visitor.Invoke(p);
                    if (err_1 > 0)
                    {
                        p.X += x_inc;
                        err_1 -= dy2;
                    }
                    if (err_2 > 0)
                    {
                        p.Z += z_inc;
                        err_2 -= dy2;
                    }
                    err_1 += dx2;
                    err_2 += dz2;
                    p.Y += y_inc;
                }
            }
            else
            {
                int err_1 = dy2 - n;
                int err_2 = dx2 - n;
                for (int i = 0; i < n; i++)
                {
                    visitor.Invoke(p);
                    if (err_1 > 0)
                    {
                        p.Y += y_inc;
                        err_1 -= dz2;
                    }
                    if (err_2 > 0)
                    {
                        p.X += x_inc;
                        err_2 -= dz2;
                    }
                    err_1 += dy2;
                    err_2 += dx2;
                    p.Z += z_inc;
                }
            }
            visitor.Invoke(p);
        }

    }
}
