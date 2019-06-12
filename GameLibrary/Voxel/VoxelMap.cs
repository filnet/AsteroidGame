using GameLibrary.SceneGraph.Bounding;
using GameLibrary.Util;
using GameLibrary.Util.Grid;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameLibrary.Voxel
{
    /*public interface Callbacks
    {
        int Value(int x, int y, int z, Direction dir);

        VoxelMap GetNeighbourMap(Direction dir);
    }*/

    public interface Visitor
    {
        bool Begin(int size);
        bool Visit(VoxelMapIterator ite);
        bool End();
    }

    public interface EagerVisitor
    {
        bool Begin();
        VoxelMap GetNeighbourMap(VoxelMap map, Direction dir);
        byte ComputeAmbientOcclusion(VoxelMapIterator ite, FaceType type, Direction dir, int x, int y, int z);
        byte ComputeAmbientOcclusion(VoxelMap map, FaceType type, Direction dir, int x, int y, int z);
        bool AddFace(FaceType type, Direction dir, int x, int y, int z, int w, int h, byte ao);
        bool End();
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MapIterator
    {
        [FieldOffset(0)]
        private readonly VoxelMap map;
        [FieldOffset(4)]
        internal int index;
        [FieldOffset(8)]
        internal int count;
        [FieldOffset(4)]
        internal int x;
        [FieldOffset(8)]
        internal int y;
        [FieldOffset(12)]
        internal int z;

        public MapIterator(VoxelMap map)
        {
            this.map = map;
            index = 0;
            count = 0;
            x = 0;
            y = 0;
            z = 0;
        }

        public bool Next(out ushort value)
        {
            return map.Next(ref this, out value);
        }
    }

    public interface VoxelMap
    {
        int X0();
        int Y0();
        int Z0();

        int Size();

        bool IsEmpty();

        bool Contains(int x, int y, int z);

        ushort Get(int x, int y, int z);
        ushort GetSafe(int x, int y, int z);

        void Set(int x, int y, int z, ushort v);
        void SetSafe(int x, int y, int z, ushort v);

        bool Next(ref MapIterator ite, out ushort value);

        void Visit(Visitor visitor, VoxelMapIterator ite);

        void Read(BinaryReader reader);
        void Write(BinaryWriter writer);
    }

    class VoxelMapEqualityComparer : IEqualityComparer<VoxelMap>
    {
        public bool Equals(VoxelMap map1, VoxelMap map2)
        {
            return ((map1.X0() == map2.X0()) && (map1.Y0() == map2.Y0()) && (map1.Z0() == map2.Z0()));
        }

        public int GetHashCode(VoxelMap map)
        {
            int hash = map.X0();
            hash = hash * 31 + map.Y0();
            hash = hash * 31 + map.Z0();
            return hash;
        }
    }

    class EmptyVoxelMap : VoxelMap
    {
        public static readonly VoxelMap INSTANCE = new EmptyVoxelMap();

        private EmptyVoxelMap() { }

        public int X0() { return 0; }
        public int Y0() { return 0; }
        public int Z0() { return 0; }

        public int Size() { return 0; }

        public bool IsEmpty() { return true; }

        public bool Contains(int x, int y, int z) { return true; }

        public ushort Get(int x, int y, int z) { return 0; }
        public ushort GetSafe(int x, int y, int z) { return 0; }

        public void Set(int x, int y, int z, ushort v) { }
        public void SetSafe(int x, int y, int z, ushort v) { }

        public bool Next(ref MapIterator ite, out ushort value)
        {
            value = 0;
            return false;
        }

        public void Visit(Visitor visitor, VoxelMapIterator ite) { }

        public void Read(BinaryReader reader) { }
        public void Write(BinaryWriter writer) { }
    }

    public abstract class AbstractVoxelMap : VoxelMap
    {
        public static readonly IEqualityComparer<VoxelMap> EqualityComparerInstance = new VoxelMapEqualityComparer();

        protected readonly int size;
        protected readonly int size2;
        protected readonly int size3;

        protected int x0;
        protected int y0;
        protected int z0;

        public AbstractVoxelMap(int size, int x0, int y0, int z0)
        {
            this.size = size;
            this.size2 = size * size;
            this.size3 = size2 * size;
            this.x0 = x0;
            this.y0 = y0;
            this.z0 = z0;
        }

        public AbstractVoxelMap(VoxelMap map) : this(map.Size(), map.X0(), map.Y0(), map.Z0())
        {
        }

        public int X0() { return x0; }
        public int Y0() { return y0; }
        public int Z0() { return z0; }

        public int Size()
        {
            return size;
        }

        public virtual bool IsEmpty()
        {
            return (size == 0);
        }

        public bool Contains(int x, int y, int z)
        {
            return (x >= x0 && x < x0 + size && y >= y0 && y < y0 + size && z >= z0 && z < z0 + size);
        }

        public abstract ushort Get(int x, int y, int z);

        public ushort GetSafe(int x, int y, int z)
        {
            return Contains(x, y, z) ? Get(x, y, z) : (ushort)0;
        }

        public virtual void Set(int x, int y, int z, ushort v)
        {
            throw new Exception("Unsupported operation");
        }

        public void SetSafe(int x, int y, int z, ushort v)
        {
            if (Contains(x, y, z))
            {
                Set(x, y, z, v);
            }
        }

        public abstract bool Next(ref MapIterator ite, out ushort value);

        public void Visit(Visitor visitor, VoxelMapIterator ite)
        {
            bool abort = !visitor.Begin(size);
            if (abort)
            {
                visitor.End();
                return;
            }

            // TODO do front to back (to benefit from depth culling)
            // do back face culling
            // transparency...

            int x = 0;
            int y = 0;
            int z = 0;

            MapIterator iterator = new MapIterator(this);
            ite.Begin(this);
            while (iterator.Next(out ushort value))
            {
                ite.Set(x0 + x, y0 + y, z0 + z, value);
                abort = !visitor.Visit(ite);
                if (abort) break;
                x++;
                if (x >= size)
                {
                    x = 0;
                    y++;
                    if (y >= size)
                    {
                        y = 0;
                        z++;
                    }
                }
            }
            ite.End();
            visitor.End();

            if (false)
            {
                int voxelsCount = size * size * size;
                int facesCount = voxelsCount * 6;
                Console.Out.WriteLine(String.Format("Empty voxels  {0} / {1} ({2:P0})", ite.emptyVoxelsCount, voxelsCount, ite.emptyVoxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled voxels {0} / {1} ({2:P0})", ite.voxelsCount, voxelsCount, ite.voxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled faces  {0} / {1} ({2:P0})", ite.facesCount, facesCount, ite.facesCount / (float)facesCount));
            }
        }

        public void VisitCulled(EagerVisitor visitor, VoxelMapIterator ite)
        {
            bool abort = !visitor.Begin();
            if (abort)
            {
                visitor.End();
                return;
            }

            // TODO do front to back (to benefit from depth culling)
            // do back face culling
            // transparency...

            for (int y = -1; y < size; y++)
            {
                for (int z = -1; z < size; z++)
                {
                    for (int x = -1; x < size; x++)
                    {

                    }
                }

            }
        }

        public List<Box> ExtractBoundingBoxes()
        {
            /*int c = 0;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        VoxelType voxelType = f(x, y, z);
                        if (VoxelInfo.Get(voxelType).IsSolid)
                        {
                            c++;
                        }
                    }
                }
            }
            Console.WriteLine(c);*/
            Console.WriteLine(this);
            List<Box> boxes = new List<Box>();
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        VoxelType voxelType = F(x, y, z);
                        if (VoxelInfo.Get(voxelType).IsPhysical)
                        {
                            // compute depth
                            int d;
                            for (d = 1; (z + d) < size; ++d)
                            {
                                VoxelType type = F(x, y, z + d);
                                if (!VoxelInfo.Get(type).IsPhysical)
                                {
                                    break;
                                }
                            }
                            // compute width (this is slightly awkward)
                            var done = false;
                            int w;
                            for (w = 1; (x + w) < size; ++w)
                            {
                                for (int i = 0; i < d; ++i)
                                {
                                    VoxelType type = F(x + w, y, z + i);
                                    if (!VoxelInfo.Get(type).IsPhysical)
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                if (done)
                                {
                                    break;
                                }
                            }
                            // compute height (this is slightly awkward)
                            done = false;
                            int h;
                            for (h = 1; (y + h) < size; ++h)
                            {
                                for (int j = 0; j < w; ++j)
                                {
                                    for (int i = 0; i < d; ++i)
                                    {
                                        VoxelType type = F(x + j, y + h, z + i);
                                        if (!VoxelInfo.Get(type).IsPhysical)
                                        {
                                            done = true;
                                            break;
                                        }
                                    }
                                    if (done)
                                    {
                                        break;
                                    }
                                }
                                if (done)
                                {
                                    break;
                                }
                            }

                            // add bounding box
                            Box box = Box.CreateFromMinMax(new Vector3(x, y, z), new Vector3(x + w, y + h, z + d));
                            boxes.Add(box);

                            // clear
                            Point3 min = new Point3(x, y, z);
                            Point3 max = new Point3(x + w, y + h, z + d);
                            Point3 dim = new Point3(w, h, d);
                            //Console.WriteLine("{0} + {1} = {2}", min, dim, max);

                            Clear(min, max);
                        }
                    }
                }
            }
            Console.WriteLine(boxes.Count);
            c += boxes.Count;
            Console.WriteLine(c);
            return boxes;
        }

        static int c = 0;


        public void Clear(Point3 min, Point3 max)
        {
            for (int x = min.X; x < max.X; x++)
            {
                for (int y = min.Y; y < max.Y; y++)
                {
                    for (int z = min.Z; z < max.Z; z++)
                    {
                        Set(x0 + x, y0 + y, z0 + z, 0);
                    }
                }
            }
        }

        struct Quad
        {
            public bool Flipped;
            public uint Type;
            public byte AmbientOcclusion;

            public bool Equals(Quad quad)
            {
                return ((Flipped == quad.Flipped) && (Type == quad.Type) && (AmbientOcclusion == quad.AmbientOcclusion));
            }
        }

        // FIXME ite is not used...
        // TODO keep an array of where each slice starts (size of array is 96 = 32 slices per sweep time 3 sweeps)
        public void VisitEager(EagerVisitor visitor)
        {
            bool abort = !visitor.Begin();
            if (abort)
            {
                visitor.End();
                return;
            }

            //int totalFaceCount = 0;
            //int totalQuadCount = 0;

            // TODO do front to back (to benefit from depth culling)
            // do back face culling
            // transparency...

            // sweep over 3-axes
            // TODO start with Y (the most occluding when there is a ground)
            //int[] dims = { size, size, size };
            //int n = 0;
            /*for (int d = 0; d < 3; ++d)
            {
                //if (d != 0) continue;
                int u = (d + 1) % 3;
                int v = (d + 2) % 3;

                Sweep(visitor, d, u, v);
            }*/
            // first sweep along Y axis (the most occluding axis when there is a ground)
            // TODO should do it from top to bottom
            Sweep(visitor, 1, 0, 2);
            // then Z
            Sweep(visitor, 2, 0, 1);
            // and X
            Sweep(visitor, 0, 2, 1);


            visitor.End();

            //ExtractBoundingBoxes();

            //Console.WriteLine("Faces {0} / {1}", totalFaceCount, totalQuadCount);
            /*if (false)
            {
                int voxelsCount = size * size * size;
                int facesCount = voxelsCount * 6;
                Console.Out.WriteLine(String.Format("Empty voxels  {0} / {1} ({2:P0})", ite.emptyVoxelsCount, voxelsCount, ite.emptyVoxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled voxels {0} / {1} ({2:P0})", ite.voxelsCount, voxelsCount, ite.voxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled faces  {0} / {1} ({2:P0})", ite.facesCount, facesCount, ite.facesCount / (float)facesCount));
            }*/
        }

        private bool Sweep(EagerVisitor visitor, int d, int u, int v)
        {
            // FIXME array related garbage (dims, x, q, mask)
            int[] dims = { size, size, size };

            int[] x = { 0, 0, 0 };
            int[] q = { 0, 0, 0 };

            // TODO use pool for array
            Quad[] mask = new Quad[dims[u] * dims[v]];
            Array.Clear(mask, 0, mask.Length);

            Direction dir1 = (Direction)(2 * d + 1);
            Direction dir2 = (Direction)(2 * d);

            // compute mask for each slice
            q[d] = 1;
            //for (x[d] = -1; x[d] < dims[d]; ++x[d])
            for (x[d] = dims[d] - 1; x[d] >= -1; --x[d])
            {
                // compute mask
                int n = 0;
                int faceCount = 0;
                // TODO handle first and last "column" in own loop
                // this makes it easier to handle border cases
                // and avoids bound tests for "internal" cells
                for (x[v] = 0; x[v] < dims[v]; ++x[v])
                {
                    for (x[u] = 0; x[u] < dims[u]; ++x[u])
                    {
                        VoxelType voxelType1 = (x[d] >= 0) ? F(x[0], x[1], x[2]) : F2(visitor, dir2, x[0], x[1], x[2]);
                        VoxelType voxelType2 = (x[d] < dims[d] - 1) ? F(x[0] + q[0], x[1] + q[1], x[2] + q[2]) : F2(visitor, dir1, x[0] + q[0], x[1] + q[1], x[2] + q[2]);
                        if (voxelType1 != voxelType2)
                        {
                            FaceType faceType1 = VoxelInfo.GetFaceType(voxelType1, dir1);
                            FaceType faceType2 = VoxelInfo.GetFaceType(voxelType2, dir2);
                            if (faceType1 != faceType2)
                            {
                                int compare = FaceInfo.Compare(faceType1, faceType2);
                                // Handle faces that belong to another chunk...
                                if ((compare > 0) && (x[d] >= 0))
                                {
                                    mask[n].Flipped = false;
                                    mask[n].Type = (uint)faceType1;
                                    mask[n].AmbientOcclusion = visitor.ComputeAmbientOcclusion(this, faceType1, dir1, x0 + x[0], y0 + x[1], z0 + x[2]);
                                    faceCount++;
                                }
                                else if ((compare < 0) && (x[d] < dims[d] - 1))
                                {
                                    mask[n].Flipped = true;
                                    mask[n].Type = (uint)faceType2;
                                    mask[n].AmbientOcclusion = visitor.ComputeAmbientOcclusion(this, faceType2, dir2, x0 + x[0] + q[0], y0 + x[1] + q[1], z0 + x[2] + q[2]);
                                    faceCount++;
                                }
                            }
                        }
                        ++n;
                    }
                }

                if (faceCount == 0)
                {
                    continue;
                }

                //totalFaceCount += faceCount;

                // generate mesh for mask using lexicographic ordering
                n = 0;
                int i, j, k;
                for (j = 0; j < dims[v]; ++j)
                {
                    for (i = 0; i < dims[u];)
                    {
                        Quad quad = mask[n];
                        if (quad.Type != 0)
                        {
                            // compute width
                            int w;
                            for (w = 1; (i + w < dims[u]) && (mask[n + w].Equals(quad)); ++w)
                            {
                            }
                            // compute height (this is slightly awkward)
                            var done = false;
                            int h;
                            for (h = 1; j + h < dims[v]; ++h)
                            {
                                for (k = 0; k < w; ++k)
                                {
                                    if (!mask[n + k + h * dims[u]].Equals(quad))
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                if (done)
                                {
                                    break;
                                }
                            }
                            // add quad
                            x[u] = i;
                            x[v] = j;

                            Direction dir = quad.Flipped ? dir2 : dir1;

                            if (!visitor.AddFace((FaceType)quad.Type, dir, x0 + x[0] + q[0], y0 + x[1] + q[1], z0 + x[2] + q[2], w, h, quad.AmbientOcclusion))
                            {
                                return false;
                            }

                            //totalQuadCount++;

                            // zero-out mask
                            k = n;
                            for (int l = 0; l < h; ++l)
                            {
                                Array.Clear(mask, k, w);
                                k += dims[u];
                                /*for (int k = 0; k < w; ++k)
                                {
                                    mask[n + k + l * dims[u]] = 0;
                                }*/
                            }
                            // increment counters and continue
                            i += w;
                            n += w;
                        }
                        else
                        {
                            ++i;
                            ++n;
                        }
                    }
                }
            }
            return true;
        }

        /*private bool GetQuad(ref Quad quad)
        {
            VoxelType voxelType1 = (x[d] >= 0) ? f(x[0], x[1], x[2]) : f2(ite, dir2, x[0], x[1], x[2]);
            VoxelType voxelType2 = (x[d] < dims[d] - 1) ? f(x[0] + q[0], x[1] + q[1], x[2] + q[2]) : f2(ite, dir1, x[0] + q[0], x[1] + q[1], x[2] + q[2]);
            if (voxelType1 != voxelType2)
            {
                FaceType faceType1 = VoxelInfo.GetFaceType(voxelType1, dir1);
                FaceType faceType2 = VoxelInfo.GetFaceType(voxelType2, dir2);
                if (faceType1 != faceType2)
                {
                    int compare = FaceInfo.Compare(faceType1, faceType2);
                    // Handle faces that belong to another chunk...
                    if ((compare > 0) && (x[d] >= 0))
                    {
                        quad.Flipped = false;
                        quad.Type = (uint)faceType1;
                        quad.AmbientOcclusion = visitor.ComputeAmbientOcclusion(ite, faceType1, dir1, x0 + x[0], y0 + x[1], z0 + x[2]);
                        return true;
                    }
                    else if ((compare < 0) && (x[d] < dims[d] - 1))
                    {
                        quad.Flipped = true;
                        quad.Type = (uint)faceType2;
                        quad.AmbientOcclusion = visitor.ComputeAmbientOcclusion(ite, faceType2, dir2, x0 + x[0] + q[0], y0 + x[1] + q[1], z0 + x[2] + q[2]);
                        return true;
                    }
                }
            }
            return false;
        }*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private VoxelType F(int x, int y, int z)
        {
            // FIXME adding x0 and then substract it...
            return (VoxelType)Get(x0 + x, y0 + y, z0 + z);
        }

        // TODO get rid of VoxelMapIterator ite : it used only to get access to neighbours (the ite part is not used anymore)
        private VoxelType F2(EagerVisitor visitor, Direction dir, int x, int y, int z)
        {
            VoxelMap map = visitor.GetNeighbourMap(this, dir);
            Debug.Assert(map.Contains(x0 + x, y0 + y, z0 + z));
            // FIXME adding x0 and then substract it...
            return (VoxelType)map.Get(x0 + x, y0 + y, z0 + z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int ToIndex(int x, int y, int z)
        {
            return (x - x0) + (y - y0) * size + (z - z0) * size2;
        }

        public virtual void Read(BinaryReader reader) { throw new NotImplementedException(); }
        public virtual void Write(BinaryWriter writer) { throw new NotImplementedException(); }

        public override string ToString()
        {
            return base.ToString() + "[" + x0 + ", " + y0 + ", " + z0 + "]";
        }
    }

    public class ArrayVoxelMap : AbstractVoxelMap
    {
        private readonly ushort[] data;

        public ArrayVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
            if (size > 32)
            {
                //throw new Exception("!!!");
            }
            data = new ushort[size3];
        }

        public ArrayVoxelMap(VoxelMap map) : base(map)
        {
            data = new ushort[size3];
        }

        public void InitializeFrom(VoxelMap map)
        {
            if (size != map.Size())
            {
                throw new Exception("!!!");
            }
            x0 = map.X0();
            y0 = map.Y0();
            z0 = map.Z0();

            int index = 0;
            MapIterator iterator = new MapIterator(map);
            while (iterator.Next(out ushort value))
            {
                data[index++] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ushort Get(int x, int y, int z)
        {
            return data[ToIndex(x, y, z)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Set(int x, int y, int z, ushort v)
        {
            data[ToIndex(x, y, z)] = v;
        }

        public override bool Next(ref MapIterator ite, out ushort value)
        {
            if (ite.index >= size3)
            {
                value = 0;
                return false;
            }
            value = data[ite.index];
            ite.index++;
            return true;
        }

    }

    public class RLEVoxelMap : AbstractVoxelMap
    {
        private ushort[] data;

        // TODO use pool
        //private ObjectPool<VoxelMap, ArrayVoxelMap> pool;

        public RLEVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
        }

        public RLEVoxelMap(VoxelMap map) : base(map)
        {
        }

        public override bool IsEmpty()
        {
            return (data.Length <= 0);
        }

#if DEBUG_VOXEL_MAP
        private static int maxSize = -1;
        private static int maxResizeCount = -1;
        private static int resizeTotalCount = 0;
        private static int wastedTotalCount = 0;
#endif

        public void InitializeFrom(VoxelMap map)
        {
            if (size != map.Size())
            {
                throw new Exception("!!!");
            }
            x0 = map.X0();
            y0 = map.Y0();
            z0 = map.Z0();

#if DEBUG_VOXEL_MAP
            int resizeCount = 0;
            int wastedCount = 0;
#endif

            MapIterator iterator = new MapIterator(map);
            if (iterator.Next(out ushort lastValue))
            {
                ushort count = 1;
                ushort pos = 0;
                while (iterator.Next(out ushort value))
                {
                    if (lastValue == value)
                    {
                        count++;
                    }
                    else
                    {
                        if (pos == 0)
                        {
                            data = new ushort[size2 * 2];
                        }
                        else if (pos == data.Length)
                        {
#if DEBUG_VOXEL_MAP
                            resizeCount++;
#endif
                            Array.Resize(ref data, data.Length + size2);
                        }
                        data[pos++] = count;
                        data[pos++] = lastValue;
                        lastValue = value;
                        count = 1;
                    }

                }
                if (pos == 0 && lastValue == 0)
                {
                    data = new ushort[0];
                }
                else
                {
                    if (pos == 0)
                    {
                        data = new ushort[2];
                    }
                    else
                    {
#if DEBUG_VOXEL_MAP
                        resizeCount++;
                        wastedCount = data.Length - (pos + 2);
#endif
                        Array.Resize(ref data, pos + 2);
                    }
                    data[pos++] = count;
                    data[pos++] = lastValue;
                }
            }
#if DEBUG_VOXEL_MAP
            //check();
            maxSize = Math.Max(maxSize, data.Length);
            maxResizeCount = Math.Max(maxResizeCount, resizeCount);
            resizeTotalCount += resizeCount;
            wastedTotalCount += wastedCount;
            if (resizeCount != 0)
            {
                Console.WriteLine("resizes = {0}, wasted = {1}", resizeCount, wastedCount);
                Console.WriteLine("max resizes = {0}, max size = {1}", maxResizeCount, maxSize);
                Console.WriteLine("total resizes = {0}, wasted = {1}", resizeTotalCount, wastedTotalCount);
            }
#endif
        }

#if DEBUG_VOXEL_MAP
        private void check()
        {
            int count = 0;
            for (int i = 0; i < data.Length; i += 2)
            {
                count += data[i];
            }
            Debug.Assert(count == size3);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Get(int index)
        {
            return data[index];
        }

        public override ushort Get(int x, int y, int z)
        {
            int p2 = ToIndex(x, y, z);

            int index = 0;
            int p = data[index];
            while (p <= p2)
            {
                index += 2;
                p += data[index];
            }
            return data[index + 1];
        }

        public override void Set(int x, int y, int z, ushort v)
        {
        }

        public override bool Next(ref MapIterator ite, out ushort value)
        {
            if (ite.index >= data.Length)
            {
                value = 0;
                return false;
            }
            value = data[ite.index + 1];
            ite.count++;
            if (ite.count >= data[ite.index])
            {
                ite.count = 0;
                ite.index += 2;
            }
            return true;
        }

        public override void Read(BinaryReader reader)
        {
            ushort length = reader.ReadUInt16();
            if (length > 0)
            {
                byte[] buffer = new byte[length * 2];
                reader.Read(buffer, 0, buffer.Length);

                data = new ushort[length];

                int p = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    ushort d = (ushort)((buffer[p + 1] << 8) + buffer[p]);
                    data[i] = d;
                    p += 2;
                }
            }
            else
            {
                data = new ushort[0];
            }
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((ushort)data.Length);
            if (data.Length > 0)
            {
                byte[] buffer = new byte[data.Length * 2];
                int p = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    ushort d = data[i];
                    buffer[p] = (byte)(d & 0xFF);
                    buffer[p + 1] = (byte)((d >> 8) & 0xFF);
                    p += 2;
                }
                writer.Write(buffer);
            }
        }
    }

}
