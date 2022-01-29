using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Bounding;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static GameLibrary.Util.Raster.LineRasterizerUtil;
using static GameLibrary.VectorUtil;

// see:
// https://www.gamasutra.com/view/feature/192007/sponsored_the_world_of_just_cause_.php?page=2
// http://hhoppe.com/geomclipmap.pdf
namespace GameLibrary.Util.Grid
{

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

        public void ToVector3(ref Point3 p, out Vector3 v)
        {
            v.X = (p.X >= 0) ? p.X * chunkSize : p.X * chunkSize + 1;
            v.Y = (p.Y >= 0) ? p.X * chunkSize : p.Y * chunkSize + 1;
            v.Z = (p.Z >= 0) ? p.X * chunkSize : p.Z * chunkSize + 1;
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
            public Grid<T> Grid;
            public Visitor<T> Visitor;
            public Object Arg;

            public void RasterVisit(Point3 point)
            {
                GridItem<T> item = Grid.GetItem(point);
                if (item != null && item.obj != null)
                {
                    Visitor.Invoke(Grid, item, Arg);
                }
            }
        }

        public void Cull(RenderContext ctxt, Visitor<T> visitor, Object arg)
        {
            CullNaiveFrontToBack(ctxt, visitor, arg);
            //CullX(ctxt, visitor, arg);
            //CullNaive(ctxt, visitor, arg);
        }

        public void CullNaive(RenderContext ctxt, Visitor<T> visitor, Object arg)
        {
            RasterVisitorDelegate<T> visitorDelegate;
            visitorDelegate.Grid = this;
            visitorDelegate.Visitor = visitor;
            visitorDelegate.Arg = arg;

            ctxt.CullCamera.BoundingBox.MinMax(out Vector3 min, out Vector3 max);

            ToKey(ref min, out Point3 p1);
            ToKey(ref max, out Point3 p2);

            AABB(p1, p2, visitorDelegate.RasterVisit);
        }

        public void CullNaiveFrontToBack(RenderContext ctxt, Visitor<T> visitor, Object arg)
        {
            RasterVisitorDelegate<T> visitorDelegate;
            visitorDelegate.Grid = this;
            visitorDelegate.Visitor = visitor;
            visitorDelegate.Arg = arg;

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


        // the initial planes of traversal are:
        // (1)    zy if (|x| > |y|) ∧ (|x| > |z|)
        // (2)    zx if (|y| > |x|) ∧ (|y| > |z|)
        // (3)    xy if (|z| > |x|) ∧ (|z| > |y|)
        // (4)    zy and xy if |x| = |z|;
        // (5)    zx and yx if |y| = |z|;
        // (6)    yz and xz if |y| = |x|
        public void CullX(RenderContext ctxt, Visitor<T> visitor, Object arg)
        {
            /*RasterVisitorDelegate<T> visitorDelegate;
            visitorDelegate.Grid = this;
            visitorDelegate.Visitor = visitor;
            visitorDelegate.Arg = arg;*/

            Vector3 cameraPosition = ctxt.CullCamera.Position;
            ToKey(ref cameraPosition, out Point3 o);

            ctxt.CullCamera.Frustum.NearFarFaceCenters(out Vector3 nearFaceCenter, out Vector3 farFaceCenter);

            Vector3[] corners = ctxt.CullCamera.Frustum.GetCorners();
            ToKey(ref corners[4], out Point3 p4);
            ToKey(ref corners[5], out Point3 p5);
            ToKey(ref corners[6], out Point3 p6);
            ToKey(ref corners[7], out Point3 p7);
            p4 -= o;
            p5 -= o;
            p6 -= o;
            p7 -= o;
            int mp4 = p4.ManhatanDistance();
            int mp5 = p4.ManhatanDistance();
            int mp6 = p4.ManhatanDistance();
            int mp7 = p4.ManhatanDistance();
            Debug.Assert(mp4 == mp5);
            Debug.Assert(mp4 == mp6);
            Debug.Assert(mp4 == mp7);

            ToKey(ref nearFaceCenter, out Point3 p1);
            ToKey(ref farFaceCenter, out Point3 p2);
            p1 -= o;
            p2 -= o;

            LineRasterizer rasterizer = CreateLineRasterizer(p1, p2);
            int maxZ = -1;
            while (true /*rasterizer.HasNext()*/)
            {
#if DEBUG
                bool hasNext = rasterizer.HasNext();
#endif             
                rasterizer.Next(out Point3 seed);
                if (seed.ManhatanDistance() > mp4)
                {
                    break;
                }
                ToVector3(ref seed, out Vector3  v);
                //v.Z = Math.Max(v.Z, nearFaceCenter.Z);
                //v.Z = Math.Min(v.Z, farFaceCenter.Z);
                Ray ray = new Ray(v, Vector3.Left);

                Point3 n = new Point3();
                Point3 f = new Point3();

                Vector3? near;
                Vector3? far;
                int rit = ctxt.CullCamera.Frustum.Intersects(ref ray, out near, out far);
                switch (rit)
                {
                    case Frustum.MISSED:
                        //Debug.Assert(false);
                        break;
                    case Frustum.FRONT:
                        {
                            //Debug.Assert(!hasNext);
                            Debug.Assert(near != null);
                            Debug.Assert(far != null);
                            // seed is outside frustum
                            Vector3 nearValue = near.Value;
                            Vector3 farValue = far.Value;
                            ToKey(ref nearValue, out n);
                            ToKey(ref farValue, out f);
                        }
                        break;
                    case Frustum.BACK:
                        {
                            Debug.Assert(near != null);
                            //Debug.Assert(hasNext);
                            Debug.Assert(near != null);
                            // seed is inside frustum
                            n = seed;
                            Vector3 nearValue = near.Value;
                            ToKey(ref nearValue, out f);
                        }
                        break;
                }
                //Debug.Assert(rit != Frustum.MISSED);
                if (rit == Frustum.MISSED)
                {
                    break;
                }
                bool seedCulled = false;
                GridItem<T> seedItem = GetItem(o + seed);
                if (seedItem != null && seedItem.obj != null)
                {
                    seedCulled = !visitor.Invoke(this, seedItem, arg);
                }
                //continue;

                Point3 p = seed;
                if (n == seed)
                {
                    n.X -= 1;
                }
                for (int x = n.X; x >= f.X; x--)
                {
                    p.X = x;
                    GridItem<T> item = GetItem(o + p);
                    if (item != null && item.obj != null)
                    {
                        bool culled = !visitor.Invoke(this, item, arg);
                        //Debug.Assert(!culled);
                    }
                }
            }

        }

        // the initial planes of traversal are:
        // (1)    zy if (|x| > |y|) ∧ (|x| > |z|)
        // (2)    zx if (|y| > |x|) ∧ (|y| > |z|)
        // (3)    xy if (|z| > |x|) ∧ (|z| > |y|)
        // (4)    zy and xy if |x| = |z|;
        // (5)    zx and yx if |y| = |z|;
        // (6)    yz and xz if |y| = |x|
        public void CullXX(RenderContext ctxt, Visitor<T> visitor, Object arg)
        {
            /*RasterVisitorDelegate<T> visitorDelegate;
            visitorDelegate.Grid = this;
            visitorDelegate.Visitor = visitor;
            visitorDelegate.Arg = arg;*/

            Vector3 cameraPosition = ctxt.CullCamera.Position;
            ToKey(ref cameraPosition, out Point3 o);

            ctxt.CullCamera.Frustum.NearFarFaceCenters(out Vector3 nearFaceCenter, out Vector3 farFaceCenter);

            Vector3[] corners = ctxt.CullCamera.Frustum.GetCorners();
            ToKey(ref corners[4], out Point3 p4);
            ToKey(ref corners[5], out Point3 p5);
            ToKey(ref corners[6], out Point3 p6);
            ToKey(ref corners[7], out Point3 p7);
            p4 -= o;
            p5 -= o;
            p6 -= o;
            p7 -= o;
            int mp4 = p4.ManhatanDistance();
            int mp5 = p4.ManhatanDistance();
            int mp6 = p4.ManhatanDistance();
            int mp7 = p4.ManhatanDistance();
            Debug.Assert(mp4 == mp5);
            Debug.Assert(mp4 == mp6);
            Debug.Assert(mp4 == mp7);

            ToKey(ref nearFaceCenter, out Point3 p1);
            ToKey(ref farFaceCenter, out Point3 p2);
            p1 -= o;
            p2 -= o;

            LineRasterizer rasterizer = CreateLineRasterizer(p1, p2);
            int maxZ = -1;
            while (true /*rasterizer.HasNext()*/)
            {
                rasterizer.Next(out Point3 seed);
                if (seed.ManhatanDistance() > mp4)
                {
                    break;
                }
                bool seedCulled = false;
                GridItem<T> seedItem = GetItem(o + seed);
                if (seedItem != null && seedItem.obj != null)
                {
                    seedCulled = !visitor.Invoke(this, seedItem, arg);
                }
                if (seed.X == 0 && seed.Y == 0 && seed.Z == 0)
                {
                    continue;
                }
                GridItem<T> item;
                Point3 p = seed;
                bool culled = false;
                if (!seedCulled)
                {
                    while (!culled && (Math.Abs(p.X) != Math.Abs(p.Z)))
                    {
                        p.X -= 1;
                        item = GetItem(o + p);
                        if (item != null && item.obj != null)
                        {
                            culled = !visitor.Invoke(this, item, arg);
                        }
                    }
                }
                else
                {
                    if (maxZ == -1)
                    {
                        maxZ = p.Z;
                    }
                    p.X = -Math.Abs(p.Z);
                    p.Z = maxZ;
                    culled = false;
                }
                if (!culled)
                {
                    p.Z += 1;
                    item = GetItem(o + p);
                    if (item != null && item.obj != null)
                    {
                        culled = !visitor.Invoke(this, item, arg);
                    }
                    while (!culled && (Math.Abs(p.X) != Math.Abs(p.Z)))
                    {
                        p.Z += 1;
                        item = GetItem(o + p);
                        if (item != null && item.obj != null)
                        {
                            culled = !visitor.Invoke(this, item, arg);
                        }
                    }
                }
                /*culled = false;
                p = seed;
                while (!culled && (Math.Abs(p.X) != Math.Abs(p.Z)))
                {
                    p.X += 1;
                    item = GetItem(o + p);
                    if (item != null && item.obj != null)
                    {
                        culled = !visitor.Invoke(this, item, arg);
                    }
                }
                if (!culled)
                {
                    p.Z += 1;
                    item = GetItem(o + p);
                    if (item != null && item.obj != null)
                    {
                        culled = !visitor.Invoke(this, item, arg);
                    }
                    while (!culled && (Math.Abs(p.X) != Math.Abs(p.Z)))
                    {
                        p.Z += 1;
                        item = GetItem(o + p);
                        if (item != null && item.obj != null)
                        {
                            culled = !visitor.Invoke(this, item, arg);
                        }
                    }
                }*/
            }

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
