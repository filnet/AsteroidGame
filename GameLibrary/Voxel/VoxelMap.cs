using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameLibrary.Voxel
{
    enum Neighbour { None = 0, Left = 1 << 0, Right = 1 << 1, Bottom = 1 << 2, Top = 1 << 3, Back = 1 << 4, Front = 1 << 5, All = Left | Right | Bottom | Top | Back | Front }

    public interface Visitor
    {
        bool Begin(int size, int instanceCount, int maxInstanceCount);
        bool Visit(VoxelMapIterator ite);
        bool End();
    }

    public interface VoxelMap
    {
        int Size();

        int Get(int x, int y, int z);
        int GetSafe(int x, int y, int z);

        void Set(int x, int y, int z, int v);
        void SetSafe(int x, int y, int z, int v);


        void Visit(Visitor visitor);
        void Visit(Visitor visitor, VoxelMapIterator ite);
    }

    abstract class AbstractVoxelMap : VoxelMap
    {
        private readonly int size;

        public bool Debug = true;

        public AbstractVoxelMap(int size)
        {
            this.size = size;
        }

        public int Size()
        {
            return size;
        }

        public abstract int Get(int x, int y, int z);

        public int GetSafe(int x, int y, int z)
        {
            if (x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size)
            {
                return Get(x, y, z);
            }
            return 0;
        }

        public virtual void Set(int x, int y, int z, int v)
        {
            throw new Exception("Unsupported operation");
        }

        public void SetSafe(int x, int y, int z, int v)
        {
            if (x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size)
            {
                Set(x, y, z, v);
            }
        }

        public void Visit(Visitor visitor)
        {
            Visit(visitor, new SimpleVoxelMapIterator(this));
        }

        public void Visit(Visitor visitor, VoxelMapIterator ite)
        {
            int instanceCount = size * size * size;

            bool abort = !visitor.Begin(size, instanceCount, instanceCount);
            if (abort)
            {
                visitor.End();
                return;
            }

            int emptyVoxelsCount = 0;
            int culledVoxelsCount = 0;
            int culledFacesCount = 0;

            // TODO do front to back (to benefit from depth culling)
            // do back face culling
            // transparency...
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    ite.Set(0, y, z);
                    for (int x = 0; x < size; x++)
                    {
                        int v = ite.Self();
                        if (v != 0)
                        {
                            if (ite.Neighbours != (int)Neighbour.All)
                            {
                                culledFacesCount += ite.Count;
                                abort = !visitor.Visit(ite);
                            }
                            else
                            {
                                culledVoxelsCount++;
                            }
                        }
                        else
                        {
                            emptyVoxelsCount++;
                        }
                        if (abort) break;
                        if (x < size - 1) ite.TranslateX();
                    }
                    if (abort) break;
                }
                if (abort) break;
            }
            visitor.End();
            int voxelsCount = size * size * size;
            int facesCount = voxelsCount * 6;
            if (Debug)
            {
                Console.Out.WriteLine(String.Format("Empty voxels  {0} / {1} ({2:P0})", emptyVoxelsCount, voxelsCount, emptyVoxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled voxels {0} / {1} ({2:P0})", culledVoxelsCount, voxelsCount, culledVoxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled faces  {0} / {1} ({2:P0})", culledFacesCount, facesCount, culledFacesCount / (float)facesCount));
            }
        }

    }

    public abstract class VoxelMapIterator
    {
        protected readonly VoxelMap map;

        protected int x0;
        protected int y0;
        protected int z0;

        protected int neighbours;
        protected int count;

        public VoxelMapIterator(VoxelMap map)
        {
            this.map = map;
        }

        public int X { get { return x0; } }
        public int Y { get { return y0; } }
        public int Z { get { return z0; } }

        public int Size { get { return map.Size(); } }

        public int Neighbours { get { return neighbours; } }

        public int Count { get { return count; } }

        public abstract void Set(int x0, int y0, int z0);

        public abstract void TranslateX();
        public abstract void TranslateY();
        public abstract void TranslateZ();

        public abstract int Self();
        public abstract int Left();
        public abstract int Right();
        public abstract int Bottom();
        public abstract int Top();
        public abstract int Back();
        public abstract int Front();

        protected void update()
        {
            int f = 0;
            int c = 0;

            if (Left() > 0)
            {
                f |= (int)Neighbour.Left;
                c++;
            }
            if (Right() > 0)
            {
                f |= (int)Neighbour.Right;
                c++;
            }

            if (Bottom() > 0)
            {
                f |= (int)Neighbour.Bottom;
                c++;
            }
            if (Top() > 0)
            {
                f |= (int)Neighbour.Top;
                c++;
            }

            if (Back() > 0)
            {
                f |= (int)Neighbour.Back;
                c++;
            }
            if (Front() > 0)
            {
                f |= (int)Neighbour.Front;
                c++;
            }
            neighbours = f;
            count = c;
        }

    }

    class DefaultVoxelMapIterator : VoxelMapIterator
    {
        private VoxelMap[] neighboursMap;

        public DefaultVoxelMapIterator(VoxelMap map, VoxelMap[] neighboursMap) : base(map)
        {
            this.neighboursMap = neighboursMap;
        }

        public override void Set(int x0, int y0, int z0)
        {
            this.x0 = x0;
            this.y0 = y0;
            this.z0 = z0;
            update();
        }

        public override void TranslateX()
        {
            x0 += 1;
            update();
        }

        public override void TranslateY()
        {
            y0 += 1;
            update();
        }

        public override void TranslateZ()
        {
            z0 += 1;
            update();
        }

        public override int Self()
        {
            return map.Get(x0, y0, z0);
        }

        public override int Left()
        {
            if (x0 > 0)
            {
                return map.Get(x0 - 1, y0, z0);
            }
            VoxelMap nMap = neighboursMap[(int)Direction.Left];
            return (nMap != null) ? nMap.Get(Size - 1, y0, z0) : 0;
        }

        public override int Right()
        {
            if (x0 < Size - 1)
            {
                return map.Get(x0 + 1, y0, z0);
            }
            VoxelMap nMap = neighboursMap[(int)Direction.Right];
            return (nMap != null) ? nMap.Get(0, y0, z0) : 0;
        }

        public override int Bottom()
        {
            if (y0 > 0)
            {
                return map.Get(x0, y0 - 1, z0);
            }
            VoxelMap nMap = neighboursMap[(int)Direction.Bottom];
            return (nMap != null) ? nMap.Get(x0, Size - 1, z0) : 0;
        }

        public override int Top()
        {
            if (y0 < Size - 1)
            {
                return map.Get(x0, y0 + 1, z0);
            }
            VoxelMap nMap = neighboursMap[(int)Direction.Top];
            return (nMap != null) ? nMap.Get(x0, 0, z0) : 0;
        }

        public override int Back()
        {
            if (z0 > 0)
            {
                return map.Get(x0, y0, z0 - 1);
            }
            VoxelMap nMap = neighboursMap[(int)Direction.Back];
            return (nMap != null) ? nMap.Get(x0, y0, Size - 1) : 0;
        }

        public override int Front()
        {
            if (z0 < Size - 1)
            {
                return map.Get(x0, y0, z0 + 1);
            }
            VoxelMap nMap = neighboursMap[(int)Direction.Front];
            return (nMap != null) ? nMap.Get(x0, y0, 0) : 0;
        }
    }

    class CachedVoxelMapIterator : VoxelMapIterator
    {
        private readonly int[,,] n = new int[3, 3, 3];

        public CachedVoxelMapIterator(VoxelMap map) : base(map)
        {
        }

        public override void Set(int x0, int y0, int z0)
        {
            this.x0 = x0;
            this.y0 = y0;
            this.z0 = z0;
            for (int z = 0; z < 3; z++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        int v = map.GetSafe(x0 + x - 1, y0 + y - 1, z0 + z - 1);
                        n[x, y, z] = v;
                    }
                }
            }
            update();
        }

        public override void TranslateX()
        {
            x0 += 1;
            for (int z = 0; z < 3; z++)
            {
                for (int y = 0; y < 3; y++)
                {
                    n[0, y, z] = n[1, y, z];
                    n[1, y, z] = n[2, y, z];
                    n[2, y, z] = map.GetSafe(x0 + 1, y0 + y - 1, z0 + z - 1);
                }
            }
            update();
        }

        public override void TranslateY()
        {
            y0 += 1;
            for (int z = 0; z < 3; z++)
            {
                for (int x = 0; x < 3; x++)
                {
                    n[x, 0, z] = n[x, 1, z];
                    n[x, 1, z] = n[x, 2, z];
                    n[x, 2, z] = map.GetSafe(x0 + x - 1, y0 + 1, z0 + z - 1);
                }
            }
            update();
        }

        public override void TranslateZ()
        {
            z0 += 1;
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    n[x, y, 0] = n[x, y, 1];
                    n[x, y, 1] = n[x, y, 2];
                    n[x, y, 2] = map.GetSafe(x0 + x - 1, y0 + y - 1, z0 + 1);
                }
            }
            update();
        }

        public override int Self()
        {
            return n[1, 1, 1];
        }

        public override int Left()
        {
            return n[0, 1, 1];
        }

        public override int Right()
        {
            return n[2, 1, 1];
        }

        public override int Bottom()
        {
            return n[1, 0, 1];
        }

        public override int Top()
        {
            return n[1, 2, 1];
        }

        public override int Back()
        {
            return n[1, 1, 0];
        }

        public override int Front()
        {
            return n[1, 1, 2];
        }

    }

    class SimpleVoxelMapIterator : VoxelMapIterator
    {
        public SimpleVoxelMapIterator(VoxelMap map) : base(map)
        {
        }

        public override void Set(int x0, int y0, int z0)
        {
            this.x0 = x0;
            this.y0 = y0;
            this.z0 = z0;
            update();
        }

        public override void TranslateX()
        {
            x0 += 1;
            update();
        }

        public override void TranslateY()
        {
            y0 += 1;
            update();
        }

        public override void TranslateZ()
        {
            z0 += 1;
            update();
        }

        public override int Self()
        {
            return map.Get(x0, y0, z0);
        }

        public override int Left()
        {
            return map.GetSafe(x0 - 1, y0, z0);
        }

        public override int Right()
        {
            return map.GetSafe(x0 + 1, y0, z0);
        }

        public override int Bottom()
        {
            return map.GetSafe(x0, y0 - 1, z0);
        }

        public override int Top()
        {
            return map.GetSafe(x0, y0 + 1, z0);
        }

        public override int Back()
        {
            return map.GetSafe(x0, y0, z0 - 1);
        }

        public override int Front()
        {
            return map.GetSafe(x0, y0, z0 + 1);
        }
    }

    class SimpleVoxelMap : AbstractVoxelMap
    {
        int size;
        private int[,,] map;

        public SimpleVoxelMap(int size) : base(size)
        {
            if (size > 16)
            {
                size = 16;
                //throw new Exception("!!!");
            }
            this.size = size;
            map = new int[size, size, size];
        }

        public override int Get(int x, int y, int z)
        {
            return map[x, y, z];
        }

        public override void Set(int x, int y, int z, int v)
        {
            map[x, y, z] = v;
        }

        /*
        public void Visit(Visitor visitor)
        {
            int instanceCount = size * size * size;
            visitor.Begin(size, instanceCount, instanceCount);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        int v = map[x, y, z];
                        visitor.Visit(x, y, z, v, 1);
                    }
                }
            }
            visitor.End();
        }
        */
    }

    class FunctionVoxelMap : AbstractVoxelMap
    {
        public FunctionVoxelMap(int size) : base(size)
        {
        }

        public override int Get(int x, int y, int z)
        {
            return f0(x, y, z);
        }

        private int f0(int x, int y, int z)
        {
            return 1;
        }

        private int f1(int x, int y, int z)
        {
            return (y % 2 == 0 || x % 2 == 0 || z % 2 == 0) ? 0 : 1;
        }

        private int f2(int x, int y, int z)
        {
            return ((x < 3 /*|| x > size - 4*/) || (y < 3 /*|| y > size - 4*/) || (z < 3)) ? 1 : 0;
        }

        private int f3(int x, int y, int z)
        {
            int d2 = (4 * 4) * 2;
            int size = Size();
            int x2 = 2 * x - size;
            int y2 = 2 * y - size;
            int z2 = 2 * z - size;
            int r4 = (x2 * x2) + (y2 * y2) + (z2 * z2);
            if (r4 < d2)
            {
                return 1;
            }
            return 0;
        }

        private int f4(int x, int y, int z)
        {
            return ((x == 0) || (y == 0) || (z == 0)) ? 1 : 0;

        }

        private int f5(int x, int y, int z)
        {
            return ((z == 0)) ? 1 : 0;

        }

    }


    /*
        class Layer
        {
            //private int[] rleData;
        }

        class LayeredVoxelMap : VoxelMap
        {
            //private Layer[] layers;
            public void Visit(Visitor visitor)
            {

            }
            int Value(int x, int y, int z);
            int ValueSafe(int x, int y, int z);
        }
    */
}
