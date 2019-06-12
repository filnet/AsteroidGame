using GameLibrary.Util;
using GameLibrary.Util.Grid;
using GameLibrary.Util.Octree;
using System;
using System.Diagnostics;

namespace GameLibrary.Voxel.Grid
{
    public class GridVoxelMapIterator : VoxelMapIterator
    {
        private readonly Grid<VoxelChunk> grid;

        private readonly VoxelMap[] neighbourMaps;
        private readonly VoxelMap[] neighbourMapKeys;

        private readonly ObjectPool<VoxelMap, ArrayVoxelMap> pool;

        public Point3 Key { get; set; }

        public GridVoxelMapIterator(Grid<VoxelChunk> grid, ObjectPool<VoxelMap, ArrayVoxelMap> pool)
        {
            this.grid = grid;
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
                GridItem<VoxelChunk> neighbourNode = null;
                Octree<VoxelChunk>.DirData dirData = Octree<VoxelChunk>.DIR_DATA[(int)dir];
                int nx = Key.X + dirData.dX;
                int ny = Key.Y + dirData.dY;
                int nz = Key.Z + dirData.dZ;
                // TODO check that node is a leaf
                neighbourNode = grid.GetItem(new Point3(nx, ny, nz));
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
