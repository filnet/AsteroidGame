using GameLibrary.SceneGraph.Common;
using GameLibrary.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.Voxel
{
    public class VoxelChunk
    {
        public bool Initialized;
        public SceneGraph.Bounding.BoundingBox BoundingBox;
        public VoxelMap VoxelMap;
        public Drawable Drawable;
    }

    public class VoxelOctree : Octree<VoxelChunk>
    {
        private int size;
        private int chunkSize;
        private int depth;

        public VoxelOctree(int size, int chunkSize) : base(Vector3.Zero, new Vector3(size / 2))
        {
            this.size = size;
            this.chunkSize = chunkSize;
            depth = BitUtil.Log2(size / chunkSize);

            objectFactory = createObject;
            RootNode.obj = createObject(this, RootNode);

            //fill();
        }

        private void fill()
        {
            int x0 = -2;
            int x1 = 1;
            int y0 = -2;
            int y1 = 1;
            int z0 = -2;
            int z1 = 1;
            //objectFactory = createObject;
            //RootNode.obj = createObject(this, RootNode);
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

        private VoxelChunk createObject(Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node)
        {
            VoxelChunk voxelChunk = null;

            int d = GetNodeTreeDepth(node);
            if (d < depth)
            {
                octree.AddChild(node, Octant.BottomLeftFront);
                octree.AddChild(node, Octant.BottomRightFront);
                octree.AddChild(node, Octant.BottomLeftBack);
                octree.AddChild(node, Octant.BottomRightBack);
                octree.AddChild(node, Octant.TopLeftFront);
                octree.AddChild(node, Octant.TopRightFront);
                octree.AddChild(node, Octant.TopLeftBack);
                octree.AddChild(node, Octant.TopRightBack);

                if (Octree<VoxelChunk>.HasChildren(node))
                {
                    voxelChunk = new VoxelChunk();

                    Vector3 center;
                    Vector3 halfSize;
                    GetNodeBoundingBox(node, out center, out halfSize);
                    voxelChunk.BoundingBox = new SceneGraph.Bounding.BoundingBox(center, halfSize);
                }
            }
            else
            {
                int x;
                int y;
                int z;
                octree.GetNodeCoordinates(node, out x, out y, out z);
                VoxelMap map = new FunctionVoxelMap(chunkSize, x, y, z);
                if (!map.IsEmpty())
                {
                    voxelChunk = new VoxelChunk();
                    voxelChunk.VoxelMap = map;

                    Vector3 center;
                    Vector3 halfSize;
                    GetNodeBoundingBox(node, out center, out halfSize);
                    voxelChunk.BoundingBox = new SceneGraph.Bounding.BoundingBox(center, halfSize);
                }
            }
            return voxelChunk;
        }

    }

}