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
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GameLibrary.SceneGraph.Scene;

namespace GameLibrary.Voxel
{

    public enum VoxelChunkState { Null, Loading, Ready }

    public sealed class VoxelChunk
    {
        public VoxelChunkState State;
        public SceneGraph.Bounding.BoundingBox BoundingBox;
        public VoxelMap VoxelMap;
        public Drawable Drawable;
        public Drawable TransparentDrawable;

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

        private bool LoadOnInitialization = false;
        //private bool CreateMeshOnInitialization = false;
        private bool ExitAfterLoad = false;

        private Task loadTask;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private readonly int queueSize = 10;
        private ConcurrentQueue<OctreeNode<VoxelChunk>> queue = new ConcurrentQueue<OctreeNode<VoxelChunk>>();
        private BlockingCollection<OctreeNode<VoxelChunk>> loadQueue;

        public delegate void ObjectLoadedCallback();

        public ObjectLoadedCallback objectLoadedCallback;

        public int Depth { get { return depth; } }

        public VoxelOctree(int size, int chunkSize) : base(Vector3.Zero, new Vector3(size / 2))
        {
            this.size = size;
            this.chunkSize = chunkSize;
            depth = BitUtil.Log2(size / chunkSize);

            objectFactory = createObject;
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
            meshFactory = new VoxelMapMeshFactory(this, graphicsDevice);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            RootNode.obj = createObject(this, RootNode);
            sw.Stop();
            Console.WriteLine("Creating VoxelOctree took {0} ms", sw.Elapsed);

            //fill();
            if (LoadOnInitialization)
            {
                Object d = Vector3.Zero;
                sw.Start();
                Visit(0, loadVisitor, ref d);
                sw.Stop();
                Console.WriteLine("Loading VoxelOctree took {0} ms", sw.Elapsed);
                if (ExitAfterLoad)
                {
                    Environment.Exit(0);
                }
            }
            StartLoadQueue();
        }

        private bool loadVisitor(Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node, ref Object arg)
        {
            node.obj.State = VoxelChunkState.Loading;
            load(node);
            return true;
        }

        public void Dispose()
        {
            DisposeLoadQueue();
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
                //VoxelMap map = new SpongeVoxelMap(chunkSize, x, y, z);
                VoxelMap map = new PerlinNoiseVoxelMap(chunkSize, x, y, z);
                //VoxelMap map = new AOTestVoxelMap(chunkSize, x, y, z);
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

        public override bool LoadNode(OctreeNode<VoxelChunk> node, ref Object arg)
        {
            node.obj.State = VoxelChunkState.Loading;
            bool queued = true;
            loadQueue.Add(node);
            if (!queued)
            {
                node.obj.State = VoxelChunkState.Null;
            }
            return queued;
        }

        public override void ClearLoadQueue()
        {
            // FIXME emptying the queue will cause the consumer thread to take "newer" items early
            // should pause the thread
            // TODO don't clear on each redraw...
            // TODO should be able to add a new batch "in front"...
            // so that the consumer loads "older" chunks if it gets a chance to do so...
            // and there is no need to clear the queue anymore
            //queue.Clear();
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

        private void StartLoadQueue()
        {
            loadQueue = new BlockingCollection<OctreeNode<VoxelChunk>>(queue/*, queueSize*/);
            // A simple blocking consumer with cancellation.
            loadTask = Task.Run(() =>
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
                        node = loadQueue.Take(cts.Token);
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    load(node);

                    if (objectLoadedCallback != null)
                    {
                        objectLoadedCallback();
                    }
                    //Console.WriteLine("Loaded " + node.locCode);
                }
                Console.WriteLine("No more items to take.");
            });
        }

        private void DisposeLoadQueue()
        {
            if (loadTask != null)
            {
                // complete adding and empty queue
                loadQueue.CompleteAdding();
                ClearLoadQueue();
                // cancel load task
                cts.Cancel();
                // wait for load task to end
                Task.WaitAll(loadTask);
            }
            loadTask.Dispose();
            loadQueue.Dispose();
            cts.Dispose();
        }

        private void load(OctreeNode<VoxelChunk> node)
        {
            if (node.obj.State == VoxelChunkState.Loading)
            {
                createMeshes(node);
                node.obj.State = VoxelChunkState.Ready;
            }
        }

        public void createMeshes(OctreeNode<VoxelChunk> node)
        {
            // TODO performance: the depth test is expensive...
            if (node.obj.VoxelMap != null && Octree<VoxelChunk>.GetNodeTreeDepth(node) == Depth)
            {
                VoxelMap voxelMap = node.obj.VoxelMap;

                Mesh mesh = meshFactory.CreateMesh(node);
                if (mesh != null)
                {
                    // FIXME should get bounding box from mesh...
                    SceneGraph.Bounding.BoundingBox boundingBox = node.obj.BoundingBox;
                    node.obj.Drawable = new MeshDrawable(Scene.VOXEL, mesh, boundingBox);
                }
                // FIXME meshFactory API is bad...
                Mesh transparentMesh = meshFactory.CreateTransparentMesh();
                if (transparentMesh != null)
                {
                    // FIXME should get bounding box from transparentMesh...
                    SceneGraph.Bounding.BoundingBox boundingBox = node.obj.BoundingBox;
                    node.obj.TransparentDrawable = new MeshDrawable(Scene.VOXEL_WATER, transparentMesh, boundingBox);
                }
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
                    node.obj.Drawable = new FakeDrawable(Scene.VECTOR, boundingBox);
                }
            }
        }

        class MeshDrawable : Drawable/*, Transform*/
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

            /*
            public Matrix Transform { get; }
            public Matrix WorldTransform { get; }
            */
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
                //Transform = Matrix.CreateTranslation(boundingBox.Center);
                //WorldTransform = Transform;
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
