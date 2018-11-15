using GameLibrary.Geometry.Common;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Bounding;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GameLibrary.SceneGraph.Scene;

namespace GameLibrary.Voxel
{

    public enum VoxelChunkState { Null, Loading, Ready }

    public class VoxelChunk
    {
        public VoxelChunkState State;
        public SceneGraph.Bounding.BoundingBox BoundingBox;
        public VoxelMap VoxelMap;
        public Drawable Drawable;

        public VoxelChunk()
        {
            State = VoxelChunkState.Null;
        }
    }

    public class VoxelOctree : Octree<VoxelChunk>
    {
        private readonly int size;
        private readonly int chunkSize;
        private readonly int depth;

        private GraphicsDevice graphicsDevice;

        private VoxelMapMeshFactory meshFactory;

        public int Depth { get { return depth; } }

        public VoxelOctree(int size, int chunkSize) : base(Vector3.Zero, new Vector3(size / 2))
        {
            this.size = size;
            this.chunkSize = chunkSize;
            depth = BitUtil.Log2(size / chunkSize);

            objectFactory = createObject;
            RootNode.obj = createObject(this, RootNode);

            //fill();
            start();
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
            meshFactory = new VoxelMapMeshFactory(this, graphicsDevice);
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

        // TODO this should be done asynchronously
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

        private BlockingCollection<OctreeNode<VoxelChunk>> loadQueue = new BlockingCollection<OctreeNode<VoxelChunk>>();

        // TODO get it through OctreeGeometry.Initialize()
        // and build MeshFactory in init...
        //private GraphicsDevice gd;

        public override void LoadNode(OctreeNode<VoxelChunk> node, ref Object arg)
        {
            node.obj.State = VoxelChunkState.Loading;
            loadQueue.Add(node);
        }

        public override void ClearLoadQueue()
        {
            while (loadQueue.Count > 0)
            {
                OctreeNode<VoxelChunk> node;
                bool success = loadQueue.TryTake(out node);
                if (success)
                {
                    node.obj.State = VoxelChunkState.Null;
                    //obj.Dispose();
                }
            }
        }

        private void start()
        {
            // A simple blocking consumer with no cancellation.
            Task.Run(() =>
            {
                while (!loadQueue.IsCompleted)
                {

                    OctreeNode<VoxelChunk> node = null;
                    // Blocks if number.Count == 0
                    // IOE means that Take() was called on a completed collection.
                    // Some other thread can call CompleteAdding after we pass the
                    // IsCompleted check but before we call Take. 
                    // In this example, we can simply catch the exception since the 
                    // loop will break on the next iteration.
                    try
                    {
                        node = loadQueue.Take();
                    }
                    catch (InvalidOperationException) { }

                    if (node != null)
                    {
                        load(node);
                    }
                }
                Console.WriteLine("\r\nNo more items to take.");
            });
        }

        private void load(OctreeNode<VoxelChunk> node)
        {
            createMesh(node);
            node.obj.State = VoxelChunkState.Ready;
        }

        public bool createMesh(OctreeNode<VoxelChunk> node)
        {
            Drawable drawable = null;
            // TODO performance: the depth test is expensive...
            if (node.obj.VoxelMap != null && Octree<VoxelChunk>.GetNodeTreeDepth(node) == Depth)
            {
                VoxelMap voxelMap = node.obj.VoxelMap;

                // TODO performance: derive from a simpler geometry node (no child, no physics, etc...)
                Mesh mesh = meshFactory.CreateMesh(node);
                if (mesh == null)
                {
                    return false;
                }
                SceneGraph.Bounding.BoundingBox boundingBox = node.obj.BoundingBox;
                drawable = new MeshDrawable(Scene.VOXEL, mesh, boundingBox);
            }
            else
            {
                if (true /*ctxt.AddBoundingGeometry*/)
                {
                    // No geometry, add fake geometry for displaying bounding boxes
                    // do only displaying bounding boxes...
                    // the place holder geometry should be as simple as possible:
                    // - only bounding volume
                    // - no mesh
                    // - no physics
                    // - no child, etc...
                    SceneGraph.Bounding.BoundingBox boundingBox = node.obj.BoundingBox;
                    drawable = new FakeDrawable(Scene.VECTOR, boundingBox);
                }
            }
            node.obj.Drawable = drawable;
            return true;
        }

        class MeshDrawable : Drawable, Transform
        {
            public bool Enabled { get; set; }

            // Fake is always invisible (but not its bounds...)
            public bool Visible { get; set; }

            public int RenderGroupId { get; set; }

            public bool BoundingVolumeVisible { get; set; }

            /// <summary>
            /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
            /// </summary>
            public BoundingVolume BoundingVolume { get; }

            /// <summary>
            /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
            /// </summary>
            public BoundingVolume WorldBoundingVolume { get; }

            public Matrix Transform { get; }
            public Matrix WorldTransform { get; }

            public void PreDraw(GraphicsDevice gd)
            {
                if (mesh.IndexBuffer != null)
                {
                    gd.SetVertexBuffer(mesh.VertexBuffer);
                    gd.Indices = mesh.IndexBuffer;
                }
                else
                {
                    gd.SetVertexBuffer(mesh.VertexBuffer);
                }
            }

            public void Draw(GraphicsDevice gd)
            {
                if (mesh.IndexBuffer != null)
                {
                    gd.DrawIndexedPrimitives(mesh.PrimitiveType, 0, 0, mesh.PrimitiveCount);
                }
                else if (mesh.PrimitiveCount > 0)
                {
                    gd.DrawPrimitives(mesh.PrimitiveType, 0, mesh.PrimitiveCount);
                }
            }

            public void PostDraw(GraphicsDevice gd)
            {
            }

            private readonly Mesh mesh;

            public MeshDrawable(int renderGroupId, Mesh mesh, SceneGraph.Bounding.BoundingBox boundingBox)
            {
                this.mesh = mesh;
                RenderGroupId = renderGroupId;
                // TODO keep only one bv
                BoundingVolume = boundingBox;
                WorldBoundingVolume = BoundingVolume;
                Enabled = true;
                Visible = true;
                BoundingVolumeVisible = true;
                // TODO keep only one transform
                Transform = Matrix.CreateTranslation(boundingBox.Center);
                WorldTransform = Transform;
            }
        }

        class FakeDrawable : Drawable
        {
            public bool Enabled { get; set; }

            // Fake is always invisible (but not its bounds...)
            public bool Visible { get; set; }

            public int RenderGroupId { get; set; }

            public bool BoundingVolumeVisible { get; set; }

            /// <summary>
            /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
            /// </summary>
            public BoundingVolume BoundingVolume { get; }

            /// <summary>
            /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
            /// </summary>
            public BoundingVolume WorldBoundingVolume { get; }

            public void PreDraw(GraphicsDevice gd)
            { }
            public void Draw(GraphicsDevice gd)
            { }
            public void PostDraw(GraphicsDevice gd)
            { }

            public FakeDrawable(int renderGroupId, BoundingVolume boundingVolume) : this(renderGroupId, boundingVolume, boundingVolume)
            {
            }
            public FakeDrawable(int renderGroupId, BoundingVolume boundingVolume, BoundingVolume worldBoundingVolume)
            {
                RenderGroupId = renderGroupId;
                // TODO keep only one bv
                BoundingVolume = boundingVolume;
                WorldBoundingVolume = worldBoundingVolume;
                Enabled = true;
                Visible = false;
                BoundingVolumeVisible = true;
            }
        }
    }
}
