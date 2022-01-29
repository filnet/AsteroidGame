//#define DEBUG_VOXEL_OCTREE

using GameLibrary.Geometry.Common;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Util;
using GameLibrary.Util.Octree;
using GameLibrary.Voxel.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static GameLibrary.Voxel.VoxelManager;

namespace GameLibrary.Voxel.Octree
{
    public class VoxelOctree : Octree<VoxelChunk>
    {
        private readonly int size;
        private readonly int chunkSize;
        private readonly int depth;

        public ObjectLoadedCallback objectLoadedCallback;

        private VoxelManager voxelManager;

        public VoxelOctree(int size, int chunkSize) : base(Vector3.Zero, new Vector3(size / 2))
        {
            this.size = size;
            this.chunkSize = chunkSize;
            depth = BitUtil.Log2(size / chunkSize);

            objectFactory = CreateObject;
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            voxelManager = new VoxelManager(graphicsDevice, objectLoadedCallback, GetNeighbourMap);
            voxelManager.LoadFromDisk = false;
            voxelManager.Name = "Octree";

            using (Stats.Use("VoxelOctree.Create"))
            {
                Create();
            }
            Console.WriteLine("Creating VoxelOctree took {0}ms", Stats.Elapsed("VoxelOctree.Create"));
        }

        private void Create()
        {
            RootNode.obj = CreateObject(this, RootNode);
        }

        // NOTE this method is called recursivly (via Octree.AddChild() calls)
        // TODO this should be done asynchronously
        private VoxelChunk CreateObject(Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node)
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
                    voxelChunk = new VoxelChunk
                    {
                        // no need to load this internal node
                        State = VoxelChunkState.Ready
                    };

                    // FIXME below code duplicates code in VoxelManage.CreateChuk()

                    GetNodeBoundingBox(node, out Vector3 center, out Vector3 halfSize);
                    voxelChunk.BoundingBox = new SceneGraph.Bounding.Box(center, halfSize);

                    // FIXME performance
                    // HACK and bad for performance
                    if (true /*ctxt.AddBoundingGeometry*/)
                    {
                        // No geometry, add fake geometry for displaying bounding boxes
                        // do only displaying bounding boxes...
                        // the place holder geometry should be as simple as possible:
                        // - only bounding volume
                        // - no mesh
                        // - no physics
                        // - no child, etc...
                        voxelChunk.ItemDrawable = new FakeDrawable(Scene.VECTOR, voxelChunk.BoundingBox);
                    }
                }
            }
            else
            {
                octree.GetNodeCoordinates(node, out int x, out int y, out int z);
                x /= chunkSize;
                y /= chunkSize;
                z /= chunkSize;
                voxelChunk = voxelManager.CreateChunk(x, y, z, chunkSize, CreateVoxelMap);
            }
            return voxelChunk;
        }

        private static VoxelMap CreateVoxelMap(int x, int y, int z, int size)
        {
            //VoxelMap map = new ConstantVoxelMap(size, x, y, z);
            VoxelMap map = new SierpinskiCarpetVoxelMap(size, x, y, z);
            return map;
        }

        private VoxelMap GetNeighbourMap(VoxelMap map, Direction dir)
        {
            // FIXME
            ulong locCode = GetLocCode(map.X0() + chunkSize / 2, map.Y0() + chunkSize / 2, map.Z0() + chunkSize / 2, depth);
            // find neighbourg node if any
            ulong neighbourLocCode = GetNeighborOfGreaterOrEqualSize(locCode, dir);

            // TODO check that node is a leaf
            if (neighbourLocCode > 0)
            {
                OctreeNode<VoxelChunk> neighbourNode = LookupNode(neighbourLocCode);
                return neighbourNode?.obj.VoxelMap;
            }
            return null;
        }

        public override bool LoadNode(OctreeNode<VoxelChunk> node, ref Object arg)
        {
            return voxelManager.LoadChunk(node.obj, ref arg);
        }

        public void ClearLoadQueue()
        {
            voxelManager.ClearLoadQueue();
        }

        public void Dispose()
        {
            voxelManager.Dispose();
        }

    }
}
