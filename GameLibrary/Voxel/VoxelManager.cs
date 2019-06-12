#define NEW_FACTORY
#define DEBUG_VOXEL_MANAGER

using GameLibrary.Geometry.Common;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Util;
using GameLibrary.Util.Grid;
using GameLibrary.Voxel.Geometry;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GameLibrary.Voxel.Geometry.NewVoxelMapMeshFactory;

namespace GameLibrary.Voxel
{
    public class VoxelManager
    {
        private GraphicsDevice graphicsDevice;

#if NEW_FACTORY
        private NewVoxelMapMeshFactory meshFactory;
#else
        private VoxelMapMeshFactory meshFactory;
#endif

        private bool CompressAtInitialization = true;
        private bool LoadFromDisk = true;
        private bool WriteToDisk = true;
        //private bool LoadAtInitialization = false;
        //private bool CreateMeshOnInitialization = false;
        //private bool ExitAfterLoad = false;

        private Task loadTask;
        private CancellationTokenSource cts = new CancellationTokenSource();
        //private readonly int queueSize = 10;
        private ConcurrentQueue<GridItem<VoxelChunk>> queue = new ConcurrentQueue<GridItem<VoxelChunk>>();
        private BlockingCollection<GridItem<VoxelChunk>> loadQueue;

        //public delegate T ObjectFactory(Grid<T> grid, GridItem<T> item);

        public delegate void ObjectLoadedCallback();

        //public ObjectFactory objectFactory;
        //private GetNeighbourMapCallback getNeighbourMapCallback;

        private ObjectLoadedCallback objectLoadedCallback;

        public VoxelManager(GraphicsDevice graphicsDevice, ObjectLoadedCallback objectLoadedCallback, GetNeighbourMapCallback getNeighbourMapCallback)
        {
            this.graphicsDevice = graphicsDevice;
            this.objectLoadedCallback = objectLoadedCallback;
#if NEW_FACTORY
            meshFactory = new NewVoxelMapMeshFactory(graphicsDevice, getNeighbourMapCallback);
#else
            meshFactory = new VoxelMapMeshFactory(graphicsDevice, pool);
#endif

            StartLoadQueue();
        }

        public void Dispose()
        {
            DisposeLoadQueue();
        }

        // TODO this should be done asynchronously
        public VoxelChunk CreateObject(int x, int y, int z, int chunkSize)
        {
            VoxelChunk voxelChunk = null;

            /*int x, y, z;
            x = item.key.X * chunkSize;
            y = item.key.Y * chunkSize;
            z = item.key.Z * chunkSize;*/
            x *= chunkSize;
            y *= chunkSize;
            z *= chunkSize;

            //String name = item.ToString();
            StringBuilder sb = new StringBuilder(32);
            sb.Append(x.ToString("+0;-0"));
            sb.Append(y.ToString("+0;-0"));
            sb.Append(z.ToString("+0;-0"));
            String name = sb.ToString();

            VoxelMap map = EmptyVoxelMap.INSTANCE;
            bool loaded = false;
            if (LoadFromDisk && !loaded)
            {
                using (Stats.Use("VoxelManager.ReadMap"))
                {
                    // FIXME get RLEVoxelMap from pool
                    RLEVoxelMap rleMap = new RLEVoxelMap(chunkSize, x, y, z);
                    if (ReadVoxelMap(name, rleMap))
                    {
                        map = rleMap;
                        loaded = true;
#if DEBUG_VOXEL_GRID
                           Console.WriteLine("Loaded map for {0},{1},{2} ({3})", x, y, z, map.IsEmpty());
#endif
                    }
                }
            }
            if (!loaded)
            {
                using (Stats.Use("VoxelManager.CreateMap"))
                {
                    //VoxelMap map = new AOTestVoxelMap(chunkSize, x, y, z);
                    //VoxelMap map = new SpongeVoxelMap(chunkSize, x, y, z);
                    // TODO this generates garbage (need a size less perlin noise map
                    VoxelMap perlinNoiseMap = new PerlinNoiseVoxelMap(chunkSize, x, y, z);

                    map = perlinNoiseMap;

                    if (CompressAtInitialization)
                    {
                        RLEVoxelMap rleMap = new RLEVoxelMap(map);
                        rleMap.InitializeFrom(map);
                        map = rleMap;
                        if (WriteToDisk)
                        {
                            using (Stats.Use("VoxelManager.WriteMap"))
                            {
                                WriteVoxelMap(name, map);
                            }
                        }
                    }

                    loaded = true;
#if DEBUG_VOXEL_GRID
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
#if DEBUG_VOXEL_GRID
                    Console.WriteLine("Empty map for {0},{1},{2}", x, y, z);
#endif
            }

            if (voxelChunk != null)
            {
                // voxel chunk bounding box (indenpendant of content)
                // FIXME should be merge of opaque+transparent...
                Vector3 center;
                Vector3 halfSize;
                halfSize.X = (float)chunkSize / 2.0f;
                halfSize.Y = (float)chunkSize / 2.0f;
                halfSize.Z = (float)chunkSize / 2.0f;
                center.X = (float)x + halfSize.X;
                center.Y = (float)y + halfSize.Y;
                center.Z = (float)z + halfSize.Z;
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

            return voxelChunk;
        }

        public bool ReadVoxelMap(String name, VoxelMap map)
        {
            String path = "C:\\Projects\\XNA\\Save\\" + name;

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
            String path = "C:\\Projects\\XNA\\Save\\" + name;
            using (var writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate)))
            {
                map.Write(writer);
            }
            return true;
        }

        public bool LoadItem(GridItem<VoxelChunk> item, ref Object arg)
        {
            item.obj.State = VoxelChunkState.Queued;
            //Console.WriteLine("Queuing " + item.locCode);
            loadQueue.Add(item);
            return true;
        }

        public void ClearLoadQueue()
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
                GridItem<VoxelChunk> item;
                bool success = loadQueue.TryTake(out item);
                if (success)
                {
                    if (item.obj.State == VoxelChunkState.Queued)
                    {
                        item.obj.State = VoxelChunkState.Null;
                    }
                    //obj.Dispose();
                }
            }
        }

        private void StartLoadQueue()
        {
            loadQueue = new BlockingCollection<GridItem<VoxelChunk>>(queue/*, queueSize*/);
            // A simple blocking consumer with cancellation.
            loadTask = Task.Run(() =>
            {
                while (!loadQueue.IsCompleted)
                {

                    GridItem<VoxelChunk> item = null;
                    // Blocks if number.Count == 0
                    // IOE means that Take() was called on a completed collection.
                    // Some other thread can call CompleteAdding after we pass the
                    // IsCompleted check but before we call Take. 
                    // In this example, we can simply catch the exception since the 
                    // loop will break on the next iteration.
                    try
                    {
                        item = loadQueue.Take(cts.Token);
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    load(item);

                    if (objectLoadedCallback != null)
                    {
                        objectLoadedCallback();
                    }
                    //Console.WriteLine("Loaded " + item.locCode);
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

        private void load(GridItem<VoxelChunk> item)
        {
            VoxelChunk voxelChunk = item.obj;
            // FIXME works because there is a single loader thread
            if (voxelChunk.State == VoxelChunkState.Queued)
            {
                voxelChunk.State = VoxelChunkState.Loading;
                //Console.WriteLine("Loading item " + item.key);
                // HACK...
                //mapIterator.Key = item.key;

                createMeshes(voxelChunk);
                voxelChunk.State = VoxelChunkState.Ready;
            }
        }

        private void createMeshes(VoxelChunk voxelChunk)
        {
            Debug.Assert(voxelChunk.VoxelMap != null);

            using (Stats.Use("VoxelManager.CreateMeshes"))
            {
                meshFactory.CreateMeshes(voxelChunk);
            }

            Mesh opaqueMesh = meshFactory.CreateOpaqueMesh();
            if (opaqueMesh != null)
            {
                using (Stats.Use("VoxelManager.CreateOpaqueMesh"))
                {
                    voxelChunk.OpaqueDrawable = new MeshDrawable(Scene.VOXEL, opaqueMesh);
                }
            }

            // FIXME meshFactory API is bad...
            Mesh transparentMesh = meshFactory.CreateTransparentMesh();
            if (transparentMesh != null)
            {
                using (Stats.Use("VoxelManager.CreateTransparentMesh"))
                {
                    voxelChunk.TransparentDrawable = new MeshDrawable(Scene.VOXEL_TRANSPARENT, transparentMesh);
                }
            }
        }

    }
}
