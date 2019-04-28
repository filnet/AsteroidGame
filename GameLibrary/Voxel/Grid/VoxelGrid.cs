using GameLibrary.Geometry.Common;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Bounding;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Util;
using GameLibrary.Util.Grid;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameLibrary.Voxel.Grid
{
    public class VoxelGrid : Grid<VoxelChunk>
    {
        private readonly int chunkSize;

        private GraphicsDevice graphicsDevice;

        //private VoxelMapMeshFactory meshFactory;

        //private bool CompressAtInitialization = true;
        //private bool LoadAtInitialization = false;
        //private bool CreateMeshOnInitialization = false;
        //private bool ExitAfterLoad = false;

        private Task loadTask;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private readonly int queueSize = 10;
        private ConcurrentQueue<GridItem<VoxelChunk>> queue = new ConcurrentQueue<GridItem<VoxelChunk>>();
        private BlockingCollection<GridItem<VoxelChunk>> loadQueue;

        public delegate void ObjectLoadedCallback();

        public ObjectLoadedCallback objectLoadedCallback;

        public VoxelGrid(int chunkSize)
        {
            this.chunkSize = chunkSize;
            //objectFactory = createObject;
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
            //meshFactory = new VoxelMapMeshFactory(this, graphicsDevice);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            //RootItem.obj = createObject(this, RootItem);
            sw.Stop();
            Console.WriteLine("Creating VoxelGrid took {0}", sw.Elapsed);

            StartLoadQueue();
        }

        private bool loadVisitor(Grid<VoxelChunk> grid, GridItem<VoxelChunk> item, ref Object arg)
        {
            item.obj.State = VoxelChunkState.Queued;
            load(item);
            return true;
        }

        public void Dispose()
        {
            DisposeLoadQueue();
        }

        // TODO this should be done asynchronously
        private VoxelChunk createObject(Grid<VoxelChunk> grid, GridItem<VoxelChunk> item)
        {
            VoxelChunk voxelChunk = null;
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

        public override bool LoadItem(GridItem<VoxelChunk> item, ref Object arg)
        {
            item.obj.State = VoxelChunkState.Queued;
            //Console.WriteLine("Queuing " + item.locCode);
            loadQueue.Add(item);
            return true;
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
            // FIXME works because there is a single loader thread
            if (item.obj.State == VoxelChunkState.Queued)
            {
                item.obj.State = VoxelChunkState.Loading;
                //Console.WriteLine("Loading item " + item.locCode);
                createMeshes(item);
                item.obj.State = VoxelChunkState.Ready;
            }
        }

        private void createMeshes(GridItem<VoxelChunk> item)
        {
            // TODO performance: the depth test is expensive...
            if (item.obj.VoxelMap != null /*&& Grid<VoxelChunk>.GetItemTreeDepth(item) == Depth*/)
            {
                Mesh mesh = null;// meshFactory.CreateMesh(item);
                if (mesh != null)
                {
                    item.obj.Drawable = new MeshDrawable(Scene.VOXEL, mesh);
                }
                // FIXME meshFactory API is bad...
                Mesh transparentMesh = null;// meshFactory.CreateTransparentMesh();
                if (transparentMesh != null)
                {
                    item.obj.TransparentDrawable = new MeshDrawable(Scene.VOXEL_WATER, transparentMesh);
                }
            }
        }

        class MeshDrawable : AbstractDrawable
        {
            public override Volume BoundingVolume
            {
                get;
            }

            public override Volume WorldBoundingVolume
            {
                get
                {
                    return BoundingVolume;
                }
            }

            public override int VertexCount
            {
                get { return mesh.VertexCount; }
            }

            private readonly Mesh mesh;

            public MeshDrawable(int renderGroupId, Mesh mesh)
            {
                this.mesh = mesh;
                Enabled = true;
                Visible = true;
                RenderGroupId = renderGroupId;
                BoundingVolume = mesh.BoundingVolume;
                BoundingVolumeVisible = true;
            }

            public override void PreDraw(GraphicsDevice gd)
            {
                gd.SetVertexBuffer(mesh.VertexBuffer);
                if (mesh.IndexBuffer != null)
                {
                    gd.Indices = mesh.IndexBuffer;
                }
            }

            public override void Draw(GraphicsDevice gd)
            {
                if (mesh.IndexBuffer != null)
                {
                    gd.DrawIndexedPrimitives(mesh.PrimitiveType, 0, 0, mesh.PrimitiveCount);
                }
                else
                {
                    gd.DrawPrimitives(mesh.PrimitiveType, 0, mesh.PrimitiveCount);
                }
            }

            public override void PostDraw(GraphicsDevice gd)
            {
            }

            public override void PreDrawInstanced(GraphicsDevice gd, VertexBuffer instanceVertexBuffer, int instanceOffset)
            {
                VertexBufferBinding[] vertexBufferBindings = {
                    new VertexBufferBinding(mesh.VertexBuffer, 0, 0),
                    new VertexBufferBinding(instanceVertexBuffer, instanceOffset, 1)
                };
                gd.SetVertexBuffers(vertexBufferBindings);
                if (mesh.IndexBuffer != null)
                {
                    gd.Indices = mesh.IndexBuffer;
                }
            }

            public override void DrawInstanced(GraphicsDevice gd, int instanceCount)
            {
                gd.DrawInstancedPrimitives(mesh.PrimitiveType, 0, 0, mesh.PrimitiveCount, instanceCount);
            }

            public override void PostDrawInstanced(GraphicsDevice gd)
            {
            }

        }

        // Fake is always invisible (but not its bounds...)
        class FakeDrawable : AbstractDrawable
        {
            public override Volume BoundingVolume
            {
                get;
            }

            public override Volume WorldBoundingVolume
            {
                get
                {
                    return BoundingVolume;
                }
            }

            public FakeDrawable(int renderGroupId, Volume boundingVolume)
            {
                Enabled = true;
                Visible = false;
                RenderGroupId = renderGroupId;
                BoundingVolume = boundingVolume;
                BoundingVolumeVisible = true;
            }
        }
    }
}
