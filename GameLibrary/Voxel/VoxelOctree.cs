using GameLibrary.SceneGraph.Common;
using GameLibrary.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.Voxel
{
    public class VoxelObject
    {
        public VoxelMap VoxelMap;
        public GeometryNode GeometryNode;
    }

    public class VoxelOctree : Octree<VoxelObject>
    {
        private int size;
        private int chunkSize;
        private int depth;

        public VoxelOctree(int size, int chunkSize) : base(Vector3.Zero, new Vector3(size / 2))
        {
            this.size = size;
            this.chunkSize = chunkSize;
            depth = BitUtil.Log2(size / chunkSize);

            int x0 = -6;
            int x1 = 6;
            int y0 = -6;
            int y1 = 6;
            int z0 = 0;
            int z1 = 0;
            objectFactory = createObject;
            RootNode.obj = createObject(this, RootNode);
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    for (int z = z0; z <= z1; z++)
                    {
                        AddChild(new Vector3(chunkSize * x, chunkSize * y, chunkSize * z), depth);
                    }
                }
            }
        }

        private VoxelObject createObject(Octree<VoxelObject> octree, OctreeNode<VoxelObject> node)
        {
            VoxelObject voxelObject = new VoxelObject();
            int d = octree.GetNodeTreeDepth(node);
            if (d == depth)
            {
                voxelObject.VoxelMap = new FunctionVoxelMap(chunkSize);
            }
            return voxelObject;
        }

    }

}