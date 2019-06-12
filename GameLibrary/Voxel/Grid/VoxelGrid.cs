#define DEBUG_VOXEL_GRID

using GameLibrary.Util;
using GameLibrary.Util.Grid;
using GameLibrary.Util.Octree;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using static GameLibrary.Util.DirectionConstants;
using static GameLibrary.Voxel.VoxelManager;

namespace GameLibrary.Voxel.Grid
{
    public class VoxelGrid : Grid<VoxelChunk>
    {
        public ObjectLoadedCallback objectLoadedCallback;

        private VoxelManager voxelManager;

        public VoxelGrid(int chunkSize, ObjectLoadedCallback objectLoadedCallback) : base(chunkSize)
        {
            this.objectLoadedCallback = objectLoadedCallback;
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            voxelManager = new VoxelManager(graphicsDevice, objectLoadedCallback, GetNeighbourMap);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            int m = 20;
            Point3 p1 = new Point3(-m, -2, -m);
            Point3 p2 = new Point3(m, 1, m);

            int p = 0;
            int c = (p2.X - p1.X) * (p2.Y - p1.Y) * (p2.Z - p1.Z);
            for (int y = p1.Y; y <= p2.Y; y++)
            {
                for (int z = p1.Z; z <= p2.Z; z++)
                {
                    for (int x = p1.X; x <= p2.X; x++)
                    {
                        VoxelChunk voxelChunk = voxelManager.CreateObject(x, y, z, chunkSize);
                        if (voxelChunk != null)
                        {
                            GridItem<VoxelChunk> item = new GridItem<VoxelChunk>
                            {
                                key = new Point3(x, y, z),
                                obj = voxelChunk
                            };
                            AddItem(item);
                        }
                        p++;
                    }
                    Console.WriteLine("{0}/{1} : {2}", p, c, ((100.0f * p) / c));
                }
            }
            sw.Stop();
            Console.WriteLine("Creating VoxelGrid took {0}", sw.Elapsed);
        }

        private VoxelMap GetNeighbourMap(VoxelMap map, Direction dir)
        {
            // find neighbourg node if any
            DirData dirData = DirData.Get(dir);
            // XXX
            Point3 key = new Point3(map.X0(), map.Y0(), map.Z0());
            int nx = key.X / map.Size() + dirData.dX;
            int ny = key.Y / map.Size() + dirData.dY;
            int nz = key.Z / map.Size() + dirData.dZ;
            GridItem<VoxelChunk> neighbourNode = GetItem(new Point3(nx, ny, nz));
            return neighbourNode?.obj.VoxelMap;
        }

        public override bool LoadItem(GridItem<VoxelChunk> item, ref Object arg)
        {
            return voxelManager.LoadItem(item.obj, ref arg);
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
