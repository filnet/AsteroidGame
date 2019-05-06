//#define NEW_FACTORY
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

namespace GameLibrary.Voxel.Octree
{
    public class VoxelOctree : Octree<VoxelChunk>
    {
        private readonly int size;
        private readonly int chunkSize;
        private readonly int depth;

        private GraphicsDevice graphicsDevice;

        private OctreeVoxelMapIterator mapIterator;
#if NEW_FACTORY
        private NewVoxelMapMeshFactory meshFactory;
#else
        private VoxelMapMeshFactory meshFactory;
#endif

        private bool CompressAtInitialization = true;
        private bool LoadFromDisk = false;
        private bool WriteToDisk = true;
        private bool LoadAtInitialization = false;
        //private bool CreateMeshOnInitialization = false;
        private bool ExitAfterLoad = false;

        private Task loadTask;
        private CancellationTokenSource cts = new CancellationTokenSource();
        //private readonly int queueSize = 10;
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

            ObjectPool<VoxelMap, ArrayVoxelMap> pool = new ObjectPool<VoxelMap, ArrayVoxelMap>(ArrayVoxelMapFactory, AbstractVoxelMap.EqualityComparerInstance);

            mapIterator = new OctreeVoxelMapIterator(this, pool);
#if NEW_FACTORY
            meshFactory = new NewVoxelMapMeshFactory(graphicsDevice, pool);
#else
            meshFactory = new VoxelMapMeshFactory(graphicsDevice, pool);
#endif

            using (Stats.Use("VoxelOctree.Create"))
            {
                RootNode.obj = createObject(this, RootNode);
            }

            //fill();
            if (LoadAtInitialization)
            {
                Object d = Vector3.Zero;
                using (Stats.Use("VoxelOctree.Load"))
                {
                    Visit(0, loadVisitor, ref d);
                }
            }
            if (ExitAfterLoad)
            {
                Environment.Exit(0);
            }

            Stats.Log("VoxelOctree.");

            StartLoadQueue();
        }

        private static ArrayVoxelMap ArrayVoxelMapFactory(VoxelMap map, ArrayVoxelMap arrayMap)
        {
            if (arrayMap == null)
            {
                arrayMap = new ArrayVoxelMap(map);
            }
            arrayMap.InitializeFrom(map);
            return arrayMap;
        }

        private bool loadVisitor(Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node, ref Object arg)
        {
            node.obj.State = VoxelChunkState.Queued;
            load(node);
            return true;
        }

        public void Dispose()
        {
            DisposeLoadQueue();
        }
        /*
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
        */

        // NOTE this method is called recursvily (via Octree.AddChild() calls)
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
                    // no need to load this internal node
                    voxelChunk.State = VoxelChunkState.Ready;
                }
            }
            else
            {
                int x, y, z;
                octree.GetNodeCoordinates(node, out x, out y, out z);
                String name = Octree<VoxelChunk>.LocCodeToString(node.locCode);
                VoxelMap map = EmptyVoxelMap.INSTANCE;
                bool loaded = false;
                if (LoadFromDisk && !loaded)
                {
                    using (Stats.Use("VoxelOctree.ReadMap"))
                    {
                        RLEVoxelMap rleMap = new RLEVoxelMap(chunkSize, x, y, z);
                        if (ReadVoxelMap(name, rleMap))
                        {
                            map = rleMap;
                            loaded = true;
#if DEBUG_VOXEL_OCTREE
                           Console.WriteLine("Loaded map for {0},{1},{2} ({3})", x, y, z, map.IsEmpty());
#endif
                        }
                    }
                }
                if (!loaded)
                {
                    using (Stats.Use("VoxelOctree.CreateMap"))
                    {
                        //VoxelMap map = new AOTestVoxelMap(chunkSize, x, y, z);
                        //VoxelMap map = new SpongeVoxelMap(chunkSize, x, y, z);
                        VoxelMap perlinNoiseMap = new PerlinNoiseVoxelMap(chunkSize, x, y, z);

                        map = perlinNoiseMap;

                        if (CompressAtInitialization)
                        {
                            RLEVoxelMap rleMap = new RLEVoxelMap(map);
                            rleMap.InitializeFrom(map);
                            map = rleMap;
                            if (WriteToDisk)
                            {
                                using (Stats.Use("VoxelOctree.WriteMap"))
                                {
                                    WriteVoxelMap(name, map);
                                }
                            }
                        }

                        loaded = true;
#if DEBUG_VOXEL_OCTREE
                        Console.WriteLine("Created map for {0},{1},{2} ({3})", x, y, z, map.IsEmpty());
#endif
                    }
                }
                if (!map.IsEmpty())
                {
                    voxelChunk = new VoxelChunk();
                    voxelChunk.VoxelMap = map;

                    /*Vector3 center;
                    Vector3 halfSize;
                    GetNodeBoundingBox(node, out center, out halfSize);
                    voxelChunk.BoundingBox = new SceneGraph.Bounding.Box(center, halfSize);*/
                }
                else
                {
                    // no geometry to create
#if DEBUG_VOXEL_OCTREE
                    Console.WriteLine("Empty map for {0},{1},{2}", x, y, z);
#endif
                }
            }

            if (voxelChunk != null)
            {
                // voxel chunk bounding box (indenpendant of content)
                // FIXME should be merge of opaque+transparent...
                Vector3 center;
                Vector3 halfSize;
                GetNodeBoundingBox(node, out center, out halfSize);
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
                    voxelChunk.NodeDrawable = new FakeDrawable(Scene.VECTOR, voxelChunk.BoundingBox);
                }
            }
            return voxelChunk;
        }

        private string GetFilePath(string name)
        {
            String path = "C:\\Projects\\XNA\\Save\\" + name;
            return path;
        }

        public bool ReadVoxelMap(String name, VoxelMap map)
        {
            String path = GetFilePath(name);
            try
            {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    map.Read(reader);
                }
            }
            catch (IOException ex)
            {
                //Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public bool WriteVoxelMap(String name, VoxelMap map)
        {
            String path = GetFilePath(name);
            using (var writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate)))
            {
                map.Write(writer);
            }
            return true;
        }

        public override bool LoadNode(OctreeNode<VoxelChunk> node, ref Object arg)
        {
            //Console.WriteLine("Queuing " + node.locCode + " " + node.obj.State);
            node.obj.State = VoxelChunkState.Queued;
            loadQueue.Add(node);
            return true;
        }

        public override void ClearLoadQueue()
        {
            //Console.WriteLine("ClearLoadQueue");
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
                    if (node.obj.State == VoxelChunkState.Queued)
                    {
                        node.obj.State = VoxelChunkState.Null;
                    }
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
                loadTask.Dispose();
            }
            loadQueue?.Dispose();
            cts.Dispose();
        }

        private void load(OctreeNode<VoxelChunk> node)
        {
            VoxelChunk voxelChunk = node.obj;
            // FIXME works because there is a single loader thread
            if (voxelChunk.State == VoxelChunkState.Queued)
            {
                voxelChunk.State = VoxelChunkState.Loading;
                //Console.WriteLine("Loading item " + item.locCode);
                // TODO performance: the depth test is expensive...
                if (true /*&& Octree<VoxelChunk>.GetNodeTreeDepth(node) == Depth*/)
                {
                    // HACK...
                    mapIterator.NodeLocCode = node.locCode;
                    createMeshes(voxelChunk);
                }
                voxelChunk.State = VoxelChunkState.Ready;
            }
        }

        private void createMeshes(VoxelChunk voxelChunk)
        {
            Debug.Assert(voxelChunk.VoxelMap != null);

            using (Stats.Use("VoxelOctree.CreateMeshes"))
            {
                meshFactory.CreateMeshes(voxelChunk, mapIterator);
            }

            Mesh opaqueMesh = meshFactory.CreateOpaqueMesh();
            if (opaqueMesh != null)
            {
                using (Stats.Use("VoxelOctree.CreateOpaqueMesh"))
                {
                    voxelChunk.Drawable = new MeshDrawable(Scene.VOXEL, opaqueMesh);
                }
            }

            // FIXME meshFactory API is bad...
            Mesh transparentMesh = meshFactory.CreateTransparentMesh();
            if (transparentMesh != null)
            {
                using (Stats.Use("VoxelOctree.CreateTransparentMesh"))
                {
                    voxelChunk.TransparentDrawable = new MeshDrawable(Scene.VOXEL_TRANSPARENT, transparentMesh);
                }
            }
        }

    }
}
