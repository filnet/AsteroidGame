using GameLibrary.Util;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameLibrary.Voxel
{
    public interface Visitor
    {
        bool Begin(int size);
        bool Visit(VoxelMapIterator ite);
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
    }

    class EmptyVoxelMap : VoxelMap
    {
        public static readonly VoxelMap INSTANCE = new EmptyVoxelMap();

        public EmptyVoxelMap() { }

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
    }

    public abstract class AbstractVoxelMap : VoxelMap
    {
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
            ushort value;
            while (iterator.Next(out value))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int toIndex(int x, int y, int z)
        {
            return (x - x0) + (y - y0) * size + (z - z0) * size2;
        }
    }

    public class ArrayVoxelMap : AbstractVoxelMap
    {
        private ushort[] data;

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
            ushort value;
            while (iterator.Next(out value))
            {
                data[index++] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ushort Get(int x, int y, int z)
        {
            return data[toIndex(x, y, z)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Set(int x, int y, int z, ushort v)
        {
            data[toIndex(x, y, z)] = v;
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
        private bool empty;

        public RLEVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
        }

        public RLEVoxelMap(VoxelMap map) : base(map)
        {
        }

        public override bool IsEmpty()
        {
            return empty;
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

            int totalValue = 0;

            MapIterator iterator = new MapIterator(map);
            ushort lastValue;
            if (iterator.Next(out lastValue))
            {
                data = new ushort[size2];

                ushort count = 1;
                int pos = 0;
                ushort value;
                while (iterator.Next(out value))
                {
                    if (lastValue == value)
                    {
                        count++;
                    }
                    else
                    {
                        if (pos == data.Length)
                        {
                            Array.Resize(ref data, data.Length * 2);
                        }
                        data[pos++] = count;
                        data[pos++] = lastValue;
                        totalValue += lastValue;
                        lastValue = value;
                        count = 1;
                    }

                }
                Array.Resize(ref data, pos + 2);
                data[pos++] = count;
                data[pos++] = lastValue;
                totalValue += lastValue;
            }
            //check();

            empty = (totalValue == 0);
        }

        private void check()
        {
            int count = 0;
            for (int i = 0; i < data.Length; i += 2)
            {
                count += data[i];
            }
            Debug.Assert(count == size3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Get(int index)
        {
            return data[index];
        }

        public override ushort Get(int x, int y, int z)
        {
            int p2 = toIndex(x, y, z);

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
    }

}
