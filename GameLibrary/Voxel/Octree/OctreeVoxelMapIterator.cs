using GameLibrary.Util;
using GameLibrary.Util.Octree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GameLibrary.Voxel.Octree
{
    public class OctreeVoxelMapIterator : VoxelMapIterator
    {
        private readonly Octree<VoxelChunk> octree;

        private readonly VoxelMap[] neighbourMap;
        private readonly VoxelMap[] neighbourKeyMap;

        private readonly ObjectPool<VoxelMap, ArrayVoxelMap> pool;

        public ulong NodeLocCode { get; set; }

        public OctreeVoxelMapIterator(Octree<VoxelChunk> octree, ObjectPool<VoxelMap, ArrayVoxelMap> pool)
        {
            this.octree = octree;
            this.pool = pool;
            int n = Enum.GetNames(typeof(Direction)).Length;
            neighbourMap = new VoxelMap[n];
            neighbourKeyMap = new VoxelMap[n];
        }

        internal override void Begin(VoxelMap map)
        {
            base.Begin(map);
        }

        internal override void End()
        {
            base.End();
            for (int i = 0; i < neighbourMap.Length; i++)
            {
                if (neighbourMap[i] != null && neighbourKeyMap[i] != null)
                {
                    pool.Give(neighbourKeyMap[i]);
                }
                neighbourMap[i] = null;
                neighbourKeyMap[i] = null;
            }
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

            int lookupIndex = 0;
            if (nx < x0)
            {
                lookupIndex |= Mask.LEFT;
            }
            else if (nx >= x0 + size)
            {
                lookupIndex |= Mask.RIGHT;
            }
            if (ny < y0)
            {
                lookupIndex |= Mask.BOTTOM;
            }
            else if (ny >= y0 + size)
            {
                lookupIndex |= Mask.TOP;
            }
            if (nz < z0)
            {
                lookupIndex |= Mask.BACK;
            }
            else if (nz >= z0 + size)
            {
                lookupIndex |= Mask.FRONT;
            }

            if (lookupIndex == 0)
            {
                // inside
                return map.Get(nx, ny, nz);
            }

            // outside
            Direction nDirection = Octree<VoxelChunk>.DIR_LOOKUP_TABLE[lookupIndex];
            VoxelMap nMap = GetNeighbourMap(nDirection);
            Debug.Assert(nMap.Contains(nx, ny, nz));
            return nMap.Get(nx, ny, nz);
        }

        public override void Set(int x, int y, int z, ushort v)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.v = v;
        }

        public override VoxelMap GetNeighbourMap(Direction direction)
        {
            VoxelMap map = neighbourMap[(int)direction];
            if (map == null)
            {
                // find neighbourg node if any
                OctreeNode<VoxelChunk> neighbourNode = null;
                ulong l = octree.GetNeighborOfGreaterOrEqualSize(NodeLocCode, direction);
                // TODO check that node is a leaf
                if (l > 0)
                {
                    neighbourNode = octree.LookupNode(l);
                }
                map = (neighbourNode != null) ? neighbourNode.obj.VoxelMap : null;
                if (map == null)
                {
                    map = EmptyVoxelMap.INSTANCE;
                }
                else
                {
                    // remember key used in pool
                    neighbourKeyMap[(int)direction] = map;
                    map = pool.Take(map);
                }
                neighbourMap[(int)direction] = map;
            }
            return map;
        }
    }


}
