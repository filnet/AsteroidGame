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
        private readonly ulong nodeLocCode;

        private VoxelMap[] neighboursMap;

        public OctreeVoxelMapIterator(Octree<VoxelChunk> octree, ulong nodeLocCode)
        {
            this.octree = octree;
            this.nodeLocCode = nodeLocCode;
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
            VoxelMap nMap = getNeighbourMap(nDirection);
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
                ulong l = octree.GetNeighborOfGreaterOrEqualSize(nodeLocCode, direction);
                OctreeNode<VoxelChunk> neighbourNode = octree.LookupNode(l);
                map = (neighbourNode != null) ? neighbourNode.obj.VoxelMap : null;
                if (map == null)
                {
                    map = EmptyVoxelMap.INSTANCE;
                }
                neighboursMap[(int)direction] = map;
            }
            return map;
        }
    }


}
