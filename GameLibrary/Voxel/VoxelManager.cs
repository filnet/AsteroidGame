#define DEBUG_VOXEL_MANAGER

using GameLibrary.Geometry.Common;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Util;
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
using static GameLibrary.Voxel.Geometry.VoxelMapMeshFactory;

namespace GameLibrary.Voxel
{
    public class VoxelManager
    {
        private GraphicsDevice graphicsDevice;

        private VoxelMapMeshFactory meshFactory1;
        private VoxelMapMeshFactory meshFactory2;
        private VoxelMapMeshFactory meshFactory3;

        public bool CompressAtInitialization = true;
        public bool LoadFromDisk = true;
        public bool WriteToDisk = true;
        //private bool LoadAtInitialization = false;
        //private bool CreateMeshOnInitialization = false;
        //private bool ExitAfterLoad = false;

        private Task loadTask1;
        private Task loadTask2;
        private Task loadTask3;
        private CancellationTokenSource cts = new CancellationTokenSource();
        //private readonly int queueSize = 10;
        private ConcurrentQueue<VoxelChunk> queue = new ConcurrentQueue<VoxelChunk>();
        private BlockingCollection<VoxelChunk> loadQueue;

        //public delegate T ObjectFactory(Grid<T> grid, GridItem<T> item);

        public delegate void ObjectLoadedCallback();

        public delegate VoxelMap VoxelMapFactory(int x, int y, int z, int size);

        //public ObjectFactory objectFactory;
        //private GetNeighbourMapCallback getNeighbourMapCallback;

        private ObjectLoadedCallback objectLoadedCallback;

        public string Name;

        public VoxelManager(GraphicsDevice graphicsDevice, ObjectLoadedCallback objectLoadedCallback, GetNeighbourMapCallback getNeighbourMapCallback)
        {
            this.graphicsDevice = graphicsDevice;
            this.objectLoadedCallback = objectLoadedCallback;

            var pool1 = new ObjectPool<VoxelMap, ArrayVoxelMap>(ArrayVoxelMapFactory, AbstractVoxelMap.EqualityComparerInstance);
            var pool2 = new ObjectPool<VoxelMap, ArrayVoxelMap>(ArrayVoxelMapFactory, AbstractVoxelMap.EqualityComparerInstance);
            var pool3 = new ObjectPool<VoxelMap, ArrayVoxelMap>(ArrayVoxelMapFactory, AbstractVoxelMap.EqualityComparerInstance);

            meshFactory1 = new VoxelMapMeshFactory(graphicsDevice, pool1, getNeighbourMapCallback);
            meshFactory2 = new VoxelMapMeshFactory(graphicsDevice, pool2, getNeighbourMapCallback);
            meshFactory3 = new VoxelMapMeshFactory(graphicsDevice, pool3, getNeighbourMapCallback);

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

        public void Dispose()
        {
            DisposeLoadQueue();
        }

        // TODO this should be done asynchronously
        public VoxelChunk CreateChunk(int x, int y, int z, int size, VoxelMapFactory voxelMapFactory)
        {
            VoxelChunk voxelChunk = null;

            /*int x, y, z;
            x = item.key.X * size;
            y = item.key.Y * size;
            z = item.key.Z * size;*/
            x *= size;
            y *= size;
            z *= size;

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
                    RLEVoxelMap rleMap = new RLEVoxelMap(size, x, y, z);
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
                    map = voxelMapFactory(x, y, z, size);
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
                halfSize.X = (float)size / 2.0f;
                halfSize.Y = (float)size / 2.0f;
                halfSize.Z = (float)size / 2.0f;
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
            String path = "C:\\Projects\\XNA\\Save\\" + Name + name;

            try
            {
                using (var reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    map.Read(reader);
                }
            }
            catch (IOException)
            {
                //Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public bool WriteVoxelMap(String name, VoxelMap map)
        {
            String path = "C:\\Projects\\XNA\\Save\\" + Name + name;
            using (var writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate)))
            {
                map.Write(writer);
            }
            return true;
        }

        public bool LoadChunk(VoxelChunk chunk, ref Object arg)
        {
            chunk.State = VoxelChunkState.Queued;
            //Console.WriteLine("Queuing " + item.locCode);
            loadQueue.Add(chunk);
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
                bool success = loadQueue.TryTake(out VoxelChunk chunk);
                if (success)
                {
                    if (chunk.State == VoxelChunkState.Queued)
                    {
                        chunk.State = VoxelChunkState.Null;
                    }
                    //obj.Dispose();
                }
            }
        }

        private void StartLoadQueue()
        {
            loadQueue = new BlockingCollection<VoxelChunk>(queue/*, queueSize*/);
            // A simple blocking consumer with cancellation.
            loadTask1 = Task.Run(() => { LoadQueueRun(meshFactory1); });
            loadTask2 = Task.Run(() => { LoadQueueRun(meshFactory2); });
            loadTask3 = Task.Run(() => { LoadQueueRun(meshFactory3); });
        }

        private void LoadQueueRun(VoxelMapMeshFactory meshFactory)
        {
            while (!loadQueue.IsCompleted)
            {

                VoxelChunk chunk = null;
                // Blocks if number.Count == 0
                // IOE means that Take() was called on a completed collection.
                // Some other thread can call CompleteAdding after we pass the
                // IsCompleted check but before we call Take. 
                // In this example, we can simply catch the exception since the 
                // loop will break on the next iteration.
                try
                {
                    chunk = loadQueue.Take(cts.Token);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                Load(meshFactory, chunk);

                if (objectLoadedCallback != null)
                {
                    objectLoadedCallback();
                }
                //Console.WriteLine("Loaded " + item.locCode);
            }
            Console.WriteLine("No more items to take.");
        }

        private void DisposeLoadQueue()
        {
            // complete adding and empty queue
            loadQueue.CompleteAdding();
            ClearLoadQueue();

            // cancel load tasks
            cts.Cancel();

            StopLoadTask(loadTask1);
            StopLoadTask(loadTask2);
            StopLoadTask(loadTask3);

            loadQueue.Dispose();
            cts.Dispose();
        }

        private void StopLoadTask(Task task)
        {
            Debug.Assert(task != null);
            // wait for load task to end
            Task.WaitAll(task);
            task.Dispose();
        }

        private static void Load(VoxelMapMeshFactory meshFactory, VoxelChunk chunk)
        {
            // FIXME works because there is a single loader thread
            if (chunk.State == VoxelChunkState.Queued)
            {
                chunk.State = VoxelChunkState.Loading;
                //Console.WriteLine("Loading item " + item.key);
                // HACK...
                //mapIterator.Key = item.key;

                CreateMeshes(meshFactory, chunk);
                chunk.State = VoxelChunkState.Ready;
            }
        }

        private static void CreateMeshes(VoxelMapMeshFactory meshFactory, VoxelChunk voxelChunk)
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
