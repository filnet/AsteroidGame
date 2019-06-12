using GameLibrary.Util;
using GameLibrary.Util.Octree;
using System;
using System.Diagnostics;

namespace GameLibrary.Voxel.Octree
{
    public class OctreeVoxelMapIterator : VoxelMapIterator
    {
        private readonly Octree<VoxelChunk> octree;

        private readonly VoxelMap[] neighbourMaps;
        private readonly VoxelMap[] neighbourMapKeys;

        private readonly ObjectPool<VoxelMap, ArrayVoxelMap> pool;

        public ulong NodeLocCode { get; set; }

        public OctreeVoxelMapIterator(Octree<VoxelChunk> octree, ObjectPool<VoxelMap, ArrayVoxelMap> pool)
        {
            this.octree = octree;
            this.pool = pool;
            int n = Enum.GetNames(typeof(Direction)).Length;
            neighbourMaps = new VoxelMap[n];
            neighbourMapKeys = new VoxelMap[n];
        }

        internal override void Begin(VoxelMap map)
        {
            base.Begin(map);
        }

        internal override void End()
        {
            base.End();
            for (int i = 0; i < neighbourMaps.Length; i++)
            {
                if (neighbourMaps[i] != null && neighbourMapKeys[i] != null)
                {
                    pool.Give(neighbourMapKeys[i]);
                }
                neighbourMaps[i] = null;
                neighbourMapKeys[i] = null;
            }
        }

        public override int Value()
        {
            return v;// map.Get(x, y, z);
        }

        public override int Value(Direction dir)
        {
            return Value(x, y, z, dir);
        }

        public override int Value(int x, int y, int z, Direction dir)
        {
            Octree<VoxelChunk>.DirData dirData = Octree<VoxelChunk>.DIR_DATA[(int)dir];
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
            Direction neighbourDir = Octree<VoxelChunk>.DIR_LOOKUP_TABLE[lookupIndex];
            VoxelMap neighbourMap = GetNeighbourMap(neighbourDir);
            Debug.Assert(neighbourMap.Contains(nx, ny, nz));
            return neighbourMap.Get(nx, ny, nz);
        }

        public override void Set(int x, int y, int z, ushort v)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.v = v;
        }

        public override VoxelMap GetNeighbourMap(Direction dir)
        {
            VoxelMap neighbourMap = neighbourMaps[(int)dir];
            if (neighbourMap == null)
            {
                // find neighbourg node if any
                OctreeNode<VoxelChunk> neighbourNode = null;
                ulong neighbourLocCode = octree.GetNeighborOfGreaterOrEqualSize(NodeLocCode, dir);
                // TODO check that node is a leaf
                if (neighbourLocCode > 0)
                {
                    neighbourNode = octree.LookupNode(neighbourLocCode);
                }
                neighbourMap = (neighbourNode != null) ? neighbourNode.obj.VoxelMap : null;
                if (neighbourMap == null)
                {
                    neighbourMap = EmptyVoxelMap.INSTANCE;
                }
                else
                {
                    // remember key used in pool
                    neighbourMapKeys[(int)dir] = neighbourMap;
                    neighbourMap = pool.Take(neighbourMap);
                }
                neighbourMaps[(int)dir] = neighbourMap;
            }
            return neighbourMap;
        }
    }


}
