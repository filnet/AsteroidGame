using GameLibrary.Util;
using System;

namespace GameLibrary.Voxel
{
    /*
    enum Neighbour
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Bottom = 1 << 2,
        Top = 1 << 3,
        Back = 1 << 4,
        Front = 1 << 5,
        All = Left | Right | Bottom | Top | Back | Front
    }
    */

    public interface Visitor
    {
        bool Begin(int size);
        bool Visit(VoxelMapIterator ite);
        bool End();
    }

    public interface VoxelMap
    {
        int Size();

        bool IsEmpty();

        int Get(int x, int y, int z);
        int GetSafe(int x, int y, int z);

        void Set(int x, int y, int z, int v);
        void SetSafe(int x, int y, int z, int v);

        void Visit(Visitor visitor, VoxelMapIterator ite);
    }

    abstract class AbstractVoxelMap : VoxelMap
    {
        public static readonly VoxelMap EMPTY_VOXEL_MAP = new EmptyVoxelMap();

        protected readonly int size;

        public bool Debug = false;

        public AbstractVoxelMap(int size)
        {
            this.size = size;
        }

        public int Size()
        {
            return size;
        }

        public virtual bool IsEmpty()
        {
            return (size == 0);
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
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    ite.Set(0, y, z);
                    for (int x = 0; x < size; x++)
                    {
                        //int v = ite.Value();
                        abort = !visitor.Visit(ite);
                        /*
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
                        */
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
                Console.Out.WriteLine(String.Format("Empty voxels  {0} / {1} ({2:P0})", ite.emptyVoxelsCount, voxelsCount, ite.emptyVoxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled voxels {0} / {1} ({2:P0})", ite.voxelsCount, voxelsCount, ite.voxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled faces  {0} / {1} ({2:P0})", ite.facesCount, facesCount, ite.facesCount / (float)facesCount));
            }
        }

    }

    public abstract class VoxelMapIterator
    {
        protected readonly VoxelMap map;

        protected int x;
        protected int y;
        protected int z;

        protected readonly int size;

        //protected int neighbours;
        //protected int count;

        public int emptyVoxelsCount;
        public int voxelsCount;
        public int facesCount;

        public VoxelMapIterator(VoxelMap map)
        {
            this.map = map;
            size = map.Size();
        }

        public int X { get { return x; } }
        public int Y { get { return y; } }
        public int Z { get { return z; } }

        //public int Neighbours { get { return neighbours; } }

        //public int Count { get { return count; } }

        public abstract void Set(int x, int y, int z);

        public abstract void TranslateX();
        //public abstract void TranslateY();
        //public abstract void TranslateZ();

        public abstract int Value();
        public abstract int Value(Direction direction);

        protected void update()
        {
            /*
            int f = 0;
            int c = 0;

            if (Value(Direction.Left) > 0)
            {
                f |= (int)Neighbour.Left;
                c++;
            }
            if (Value(Direction.Right) > 0)
            {
                f |= (int)Neighbour.Right;
                c++;
            }

            if (Value(Direction.Bottom) > 0)
            {
                f |= (int)Neighbour.Bottom;
                c++;
            }
            if (Value(Direction.Top) > 0)
            {
                f |= (int)Neighbour.Top;
                c++;
            }

            if (Value(Direction.Back) > 0)
            {
                f |= (int)Neighbour.Back;
                c++;
            }
            if (Value(Direction.Front) > 0)
            {
                f |= (int)Neighbour.Front;
                c++;
            }
            neighbours = f;
            count = c;
            */
        }

    }

    class DefaultVoxelMapIterator : VoxelMapIterator
    {
        private readonly VoxelOctree octree;
        private readonly OctreeNode<VoxelChunk> node;

        private VoxelMap[] neighboursMap;

        public DefaultVoxelMapIterator(VoxelOctree octree, OctreeNode<VoxelChunk> node) : base(node.obj.VoxelMap)
        {
            this.octree = octree;
            this.node = node;
        }

        public override int Value()
        {
            return map.Get(x, y, z);
        }

        public override int Value(Direction direction)
        {
            VoxelOctree.DirData dirData = VoxelOctree.DIR_DATA[(int)direction];
            int nx = x + dirData.dX;
            int ny = y + dirData.dY;
            int nz = z + dirData.dZ;
            if (nx >= 0 && nx < size && ny >= 0 && ny < size && nz >= 0 && nz < size)
            {
                return map.Get(nx, ny, nz);
            }
            VoxelMap nMap = getNeighbourMap(direction);
            if (nMap == null)
            {
                return 0;
            }
            nx = (nx + size) % size;
            ny = (ny + size) % size;
            nz = (nz + size) % size;
            return nMap.Get(nx, ny, nz);
        }

        public override void Set(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            update();
        }

        public override void TranslateX()
        {
            x += 1;
            update();
        }

        private VoxelMap getNeighbourMap(Direction direction)
        {
            // FIXME garbage generation here...
            if (neighboursMap == null)
            {
                int n = Enum.GetNames(typeof(Direction)).Length;
                neighboursMap = new VoxelMap[n];
            }
            VoxelMap map = neighboursMap[(int)direction];
            if (map == null)
            {
                //foreach (Direction direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())

                ulong l = octree.GetNeighborOfGreaterOrEqualSize(node.locCode, direction);
                OctreeNode<VoxelChunk> neighbourNode = octree.LookupNode(l);
                map = (neighbourNode != null) ? neighbourNode.obj.VoxelMap : AbstractVoxelMap.EMPTY_VOXEL_MAP;
                neighboursMap[(int)direction] = map;
            }

            return map;
        }
    }

    class CachedVoxelMapIterator : VoxelMapIterator
    {
        private readonly int[,,] n = new int[3, 3, 3];

        public CachedVoxelMapIterator(VoxelMap map) : base(map)
        {
        }

        public override int Value()
        {
            return n[1, 1, 1];
        }

        public override int Value(Direction direction)
        {
            VoxelOctree.DirData dirData = VoxelOctree.DIR_DATA[(int)direction];
            return n[1 + dirData.dX, 1 + dirData.dY, 1 + dirData.dZ];
        }

        public override void Set(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int v = map.GetSafe(x + i - 1, y + j - 1, z + k - 1);
                        n[i, j, k] = v;
                    }
                }
            }
            update();
        }

        public override void TranslateX()
        {
            for (int j = 0; j < 3; z++)
            {
                for (int k = 0; k < 3; k++)
                {
                    n[0, j, k] = n[1, j, k];
                    n[1, j, k] = n[2, j, k];
                    n[2, j, k] = map.GetSafe(x + 1, y + j, z + k);
                }
            }
            x += 1;
            update();
        }

    }

    class SimpleVoxelMapIterator : VoxelMapIterator
    {
        public SimpleVoxelMapIterator(VoxelMap map) : base(map)
        {
        }

        public override int Value()
        {
            return map.Get(x, y, z);
        }

        public override int Value(Direction direction)
        {
            VoxelOctree.DirData dirData = VoxelOctree.DIR_DATA[(int)direction];
            int nx = x + dirData.dX;
            int ny = y + dirData.dY;
            int nz = z + dirData.dZ;
            return map.GetSafe(nx, ny, nz);
        }

        public override void Set(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            update();
        }

        public override void TranslateX()
        {
            x += 1;
            update();
        }

    }

    class EmptyVoxelMap : AbstractVoxelMap
    {
        public EmptyVoxelMap() : base(0)
        {
        }

        public override bool IsEmpty()
        {
            return true;
        }

        public override int Get(int x, int y, int z)
        {
            return 0;
        }

        public override void Set(int x, int y, int z, int v)
        {
            // TODO assert if called
        }

    }

    class SimpleVoxelMap : AbstractVoxelMap
    {
        private int[,,] map;

        public SimpleVoxelMap(int size) : base(size)
        {
            if (size > 16)
            {
                //throw new Exception("!!!");
            }
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

    }

    class FunctionVoxelMap : AbstractVoxelMap
    {
        private readonly int x0;
        private readonly int y0;
        private readonly int z0;

        public FunctionVoxelMap(int size, int x0, int y0, int z0) : base(size)
        {
            this.x0 = x0;
            this.y0 = y0;
            this.z0 = z0;
        }

        public override int Get(int x, int y, int z)
        {
            return f9(x0 + x, y0 + y, z0 + z);
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

        private int f6(int x, int y, int z)
        {
            if (y == 0 && x >= 5 && x <= 8 && z >= 2 && z <= 3) return 1;
            if (y == 1 && x >= 6 && x <= 7 && z >= 2 && z <= 2) return 1;
            return 0;
        }

        private int f7(int x, int y, int z)
        {
            //if (x < 1) return 0;
            //if (z < 1) return 0;
            if (y < 4) return 2;
            if (y > 14) return 2;
            if (y < 5 && x > 3 && x < size - 3 && z > 3 && z < size - 3) return 2;
            if (y < 7 && x > 6 && x < size - 6 && z > 6 && z < size - 6) return 3;
            if (y < 8 && x > 7 && x < size - 7 && z > 7 && z < size - 7) return 4;
            if (y == 12 && x > 7 && x < size - 7 && z > 7 && z < size - 7) return 4;
            if (y == 13 && x > 6 && x < size - 6 && z > 6 && z < size - 6) return 3;
            if (y == 14 && x > 4 && x < size - 4 && z > 4 && z < size - 4) return 3;
            if (y == 9 && x == 8 && z == 8) return 5;
            return 0;
        }
/*
        public override bool IsEmpty()
        {
            return !(y0 == 0);
        }
*/
        private int f8(int x, int y, int z)
        {
            if (y == 0)
            {
                if (x - x0 == 0 || z - z0 == 0) return 3;
                return 2;
            }
            return 0;
        }

        private int f9(int x, int y, int z)
        {
            float n = SimplexNoise.Generate(z / 10.0f, x / 10.0f, y / 10.0f);
            return (n > 0.5f) ? 1 : 0;
        }

        /*
        for (int x = 0; x<Width) 
        {
          for (int y = 0; y<Depth) 
          {
            for (int z = 0; z<Height) 
            {
              if(z<Noise2D(x, y) * Height) 
              {
                Array[x][y][z] = Noise3D(x, y, z)
              } else {
                Array[x][y][z] = 0
              }
            } 
          } 
        } 
        */

    }

}
