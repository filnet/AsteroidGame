using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.Voxel
{
    public abstract class VoxelMapIterator
    {
        protected readonly VoxelMap map;

        protected int x;
        protected int y;
        protected int z;
        protected int v;

        protected readonly int x0;
        protected readonly int y0;
        protected readonly int z0;

        protected readonly int size;

        public int emptyVoxelsCount;
        public int voxelsCount;
        public int facesCount;

        public VoxelMapIterator(VoxelMap map)
        {
            this.map = map;
            size = map.Size();
            this.x0 = map.X0();
            this.y0 = map.Y0();
            this.z0 = map.Z0(); 
        }

        public int X { get { return x; } }
        public int Y { get { return y; } }
        public int Z { get { return z; } }
        public int V { get { return v; } }

        public int X0 { get { return x0; } }
        public int Y0 { get { return y0; } }
        public int Z0 { get { return z0; } }

        public abstract void Set(int x, int y, int z, int v);

        public abstract void TranslateX(int v);
        //public abstract void TranslateY();
        //public abstract void TranslateZ();

        public abstract int Value();
        public abstract int Value(Direction direction);
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
            return v;// map.Get(x, y, z);
        }

        public override int Value(Direction direction)
        {
            VoxelOctree.DirData dirData = VoxelOctree.DIR_DATA[(int)direction];
            int nx = x + dirData.dX;
            int ny = y + dirData.dY;
            int nz = z + dirData.dZ;

            bool inside = true;
            int lookupIndex = 0;
            if (nx < x0)
            {
                lookupIndex |= Mask.LEFT;
                inside = false;
            }
            else if (nx >= x0 + size)
            {
                lookupIndex |= Mask.RIGHT;
                inside = false;
            }
            if (ny < y0)
            {
                lookupIndex |= Mask.BOTTOM;
                inside = false;
            }
            else if (ny >= y0 + size)
            {
                lookupIndex |= Mask.TOP;
                inside = false;
            }
            if (nz < z0)
            {
                lookupIndex |= Mask.BACK;
                inside = false;
            }
            else if (nz >= z0 + size)
            {
                lookupIndex |= Mask.FRONT;
                inside = false;
            }

            if (inside)
            {
                return map.Get(nx, ny, nz);
            }

            Direction nDirection = Octree<VoxelChunk>.DIR_LOOKUP_TABLE[lookupIndex];
            VoxelMap nMap = getNeighbourMap(nDirection);
            if (nMap.IsEmpty())
            {
                return 0;
            }
            return nMap.Get(nx, ny, nz);
        }

        public override void Set(int x, int y, int z, int v)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.v = v;
        }

        public override void TranslateX(int v)
        {
            x += 1;
            this.v = v;
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
                ulong l = octree.GetNeighborOfGreaterOrEqualSize(node.locCode, direction);
                OctreeNode<VoxelChunk> neighbourNode = octree.LookupNode(l);
                map = (neighbourNode != null) ? neighbourNode.obj.VoxelMap : AbstractVoxelMap.EMPTY_VOXEL_MAP;
                neighboursMap[(int)direction] = map;
            }

            return map;
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

        public override void Set(int x, int y, int z, int v)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.v = v;
        }

        public override void TranslateX(int v)
        {
            x += 1;
            this.v = v;
        }

    }
}
