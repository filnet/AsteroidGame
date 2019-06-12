using GameLibrary.Geometry.Common;
using GameLibrary.Util;
using GameLibrary.Util.Octree;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace GameLibrary.Voxel.Geometry
{
    public class NewVoxelMapMeshFactory : IMeshFactory
    {
        public delegate VoxelMap GetNeighbourMapCallback(VoxelMap map, Direction dir);

        private readonly GraphicsDevice graphicsDevice;

        private readonly DrawVisitor drawVisitor;

        private ObjectPool<VoxelMap, ArrayVoxelMap> pool;

        private static ArrayVoxelMap ArrayVoxelMapFactory(VoxelMap map, ArrayVoxelMap arrayMap)
        {
            if (arrayMap == null)
            {
                arrayMap = new ArrayVoxelMap(map);
            }
            arrayMap.InitializeFrom(map);
            return arrayMap;
        }

        public NewVoxelMapMeshFactory(GraphicsDevice graphicsDevice, GetNeighbourMapCallback getNeighbourMapCallback)
        {
            this.graphicsDevice = graphicsDevice;

            pool = new ObjectPool<VoxelMap, ArrayVoxelMap>(ArrayVoxelMapFactory, AbstractVoxelMap.EqualityComparerInstance);

            drawVisitor = new DrawVisitor
            {
                getNeighbourMapCallback = getNeighbourMapCallback,
                pool = pool,
                opaqueBuilder = new VoxelVertexBufferBuilder(graphicsDevice),
                transparentBuilder = new VoxelVertexBufferBuilder(graphicsDevice)
            };
        }

        public Mesh CreateMesh(GraphicsDevice graphicsDevice)
        {
            // not used...
            throw new NotImplementedException();
        }

        public void CreateMeshes(VoxelChunk voxelChunk)
        {
            drawVisitor.Reset();

            ArrayVoxelMap arrayVoxelMap = pool.Take(voxelChunk.VoxelMap);
            Debug.Assert(arrayVoxelMap != null);
            try
            {
                arrayVoxelMap.VisitEager(drawVisitor);
            }
            finally
            {
                pool.Give(voxelChunk.VoxelMap);
            }
        }

        public Mesh CreateOpaqueMesh()
        {
            return CreateMesh(drawVisitor.opaqueBuilder);
        }

        public Mesh CreateTransparentMesh()
        {
            return CreateMesh(drawVisitor.transparentBuilder);
        }

        private static Mesh CreateMesh(VertexBufferBuilder<VoxelVertex> builder)
        {
            if (builder.VertexCount <= 0)
            {
                return null;
            }
            Mesh mesh = new Mesh(PrimitiveType.TriangleList, builder.VertexCount / 2);
            builder.SetToMesh(mesh);

            SceneGraph.Bounding.Box box = new SceneGraph.Bounding.Box();
            builder.ExtractBoundingBox(box, VoxelVertexBufferBuilderExtensions.VertexPosition);
            mesh.BoundingVolume = box;

            return mesh;
        }

        class DrawVisitor : Voxel.EagerVisitor
        {
            internal VoxelVertexBufferBuilder opaqueBuilder;
            internal VoxelVertexBufferBuilder transparentBuilder;

            internal GetNeighbourMapCallback getNeighbourMapCallback;

            internal ObjectPool<VoxelMap, ArrayVoxelMap> pool;
            
            private readonly VoxelMap[] neighbourMaps;
            private readonly VoxelMap[] neighbourMapKeys;

            private readonly float d = 0.5f;

            // normals
            private static readonly Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            private static readonly Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            private static readonly Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            private static readonly Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            private static readonly Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            private static readonly Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            private static readonly Vector3[] NORMALS =
            {
                leftNormal, rightNormal, bottomNormal, topNormal, backNormal, frontNormal
            };

            internal DrawVisitor()
            {
                int n = Enum.GetNames(typeof(Direction)).Length;
                neighbourMaps = new VoxelMap[n];
                neighbourMapKeys = new VoxelMap[n];
            }

            internal void Reset()
            {
                opaqueBuilder.Reset();
                transparentBuilder.Reset();
            }

            public bool Begin()
            {
                return true;
            }

            public bool End()
            {
                for (int i = 0; i < neighbourMaps.Length; i++)
                {
                    if (neighbourMaps[i] != null && neighbourMapKeys[i] != null)
                    {
                        pool.Give(neighbourMapKeys[i]);
                    }
                    neighbourMaps[i] = null;
                    neighbourMapKeys[i] = null;
                }
                return true;
            }

            public VoxelMap GetNeighbourMap(VoxelMap map, Direction dir)
            {
                VoxelMap neighbourMap = neighbourMaps[(int)dir];
                if (neighbourMap == null)
                {
                    neighbourMap = getNeighbourMapCallback(map, dir);
                    if (neighbourMap == null)
                    {
                        neighbourMap = EmptyVoxelMap.INSTANCE;
                    }
                    else
                    {
                        // remember key used in pool
                        neighbourMapKeys[(int)dir] = neighbourMap;
                        neighbourMap = pool.Take(neighbourMap);
                    }
                    neighbourMaps[(int)dir] = neighbourMap;
                }
                return neighbourMap;
            }

            public bool AddFace(FaceType type, Direction dir, int x, int y, int z, int w, int h, byte ao)
            {
                FaceInfo faceInfo = FaceInfo.Get(type);
                int textureIndex = faceInfo.TextureIndex();

                VoxelVertexBufferBuilder builder = (faceInfo.IsOpaque) ? opaqueBuilder : transparentBuilder;

                // initialize 
                Vector3 t;
                t.X = 2 * d * x;// + d;
                t.Y = 2 * d * y;// + d;
                t.Z = 2 * d * z;// + d;

                // FIXME hack
                if (type == FaceType.Water)
                {
                    t.Y -= 0.33f;
                }

                //Vector3 n = NORMALS[(int)dir];

                Vector2 tex00 = new Vector2(0, 0);
                Vector2 tex01 = new Vector2(0, h);
                Vector2 tex10 = new Vector2(w, 0);
                Vector2 tex11 = new Vector2(w, h);

                switch (dir)
                {
                    case Direction.Front:
                        {
                            Vector3 right = new Vector3(w, 0, 0);
                            Vector3 top = new Vector3(0, h, 0);

                            Vector3 bottomLeft = t;
                            Vector3 topLeft = t + top;
                            Vector3 bottomRight = t + right;
                            Vector3 topRight = t + top + right;

                            int i = builder.AddVertex(bottomLeft, frontNormal, Color.White, tex00, w, h, textureIndex, ao);
                            builder.AddVertex(topLeft, frontNormal, Color.White, tex01, w, h, textureIndex, ao);
                            builder.AddVertex(bottomRight, frontNormal, Color.White, tex10, w, h, textureIndex, ao);
                            builder.AddVertex(topRight, frontNormal, Color.White, tex11, w, h, textureIndex, ao);

                            builder.AddIndex(i + 1);
                            builder.AddIndex(i);
                            builder.AddIndex(i + 3);

                            builder.AddIndex(i + 2);
                            builder.AddIndex(i + 3);
                            builder.AddIndex(i);
                        }
                        break;
                    case Direction.Back:
                        {
                            Vector3 right = new Vector3(w, 0, 0);
                            Vector3 top = new Vector3(0, h, 0);

                            Vector3 bottomLeft = t;
                            Vector3 topLeft = t + top;
                            Vector3 bottomRight = t + right;
                            Vector3 topRight = t + top + right;

                            int i = builder.AddVertex(bottomRight, backNormal, Color.White, tex00, w, h, textureIndex, ao);
                            builder.AddVertex(topRight, backNormal, Color.White, tex01, w, h, textureIndex, ao);
                            builder.AddVertex(bottomLeft, backNormal, Color.White, tex10, w, h, textureIndex, ao);
                            builder.AddVertex(topLeft, backNormal, Color.White, tex11, w, h, textureIndex, ao);

                            builder.AddIndex(i + 1);
                            builder.AddIndex(i);
                            builder.AddIndex(i + 3);

                            builder.AddIndex(i + 2);
                            builder.AddIndex(i + 3);
                            builder.AddIndex(i);
                        }
                        break;
                    case Direction.Top:
                        {
                            Vector3 right = new Vector3(w, 0, 0);
                            Vector3 front = new Vector3(0, 0, h);

                            Vector3 leftBack = t;
                            Vector3 leftFront = t + front;
                            Vector3 rightBack = t + right;
                            Vector3 rightFront = t + front + right;

                            int i = builder.AddVertex(leftFront, topNormal, Color.White, tex00, w, h, textureIndex, ao);
                            builder.AddVertex(leftBack, topNormal, Color.White, tex01, w, h, textureIndex, ao);
                            builder.AddVertex(rightFront, topNormal, Color.White, tex10, w, h, textureIndex, ao);
                            builder.AddVertex(rightBack, topNormal, Color.White, tex11, w, h, textureIndex, ao);

                            builder.AddIndex(i + 1);
                            builder.AddIndex(i);
                            builder.AddIndex(i + 3);

                            builder.AddIndex(i + 2);
                            builder.AddIndex(i + 3);
                            builder.AddIndex(i);
                        }
                        break;
                    case Direction.Bottom:
                        {
                            Vector3 right = new Vector3(w, 0, 0);
                            Vector3 front = new Vector3(0, 0, h);

                            Vector3 leftBack = t;
                            Vector3 leftFront = t + front;
                            Vector3 rightBack = t + right;
                            Vector3 rightFront = t + front + right;

                            int i = builder.AddVertex(rightFront, bottomNormal, Color.White, tex00, w, h, textureIndex, ao);
                            builder.AddVertex(rightBack, bottomNormal, Color.White, tex01, w, h, textureIndex, ao);
                            builder.AddVertex(leftFront, bottomNormal, Color.White, tex10, w, h, textureIndex, ao);
                            builder.AddVertex(leftBack, bottomNormal, Color.White, tex11, w, h, textureIndex, ao);

                            builder.AddIndex(i + 1);
                            builder.AddIndex(i);
                            builder.AddIndex(i + 3);

                            builder.AddIndex(i + 2);
                            builder.AddIndex(i + 3);
                            builder.AddIndex(i);
                        }
                        break;
                    case Direction.Right:
                        {
                            Vector3 top = new Vector3(0, h, 0);
                            Vector3 front = new Vector3(0, 0, w);

                            Vector3 bottomBack = t;
                            Vector3 topBack = t + top;
                            Vector3 bottomFront = t + front;
                            Vector3 topFront = t + top + front;

                            int i = builder.AddVertex(bottomFront, rightNormal, Color.White, tex00, w, h, textureIndex, ao);
                            builder.AddVertex(topFront, rightNormal, Color.White, tex01, w, h, textureIndex, ao);
                            builder.AddVertex(bottomBack, rightNormal, Color.White, tex10, w, h, textureIndex, ao);
                            builder.AddVertex(topBack, rightNormal, Color.White, tex11, w, h, textureIndex, ao);

                            builder.AddIndex(i + 1);
                            builder.AddIndex(i);
                            builder.AddIndex(i + 3);

                            builder.AddIndex(i + 2);
                            builder.AddIndex(i + 3);
                            builder.AddIndex(i);
                        }
                        break;
                    case Direction.Left:
                        {
                            Vector3 top = new Vector3(0, h, 0);
                            Vector3 front = new Vector3(0, 0, w);

                            Vector3 bottomBack = t;
                            Vector3 topBack = t + top;
                            Vector3 bottomFront = t + front;
                            Vector3 topFront = t + top + front;

                            int i = builder.AddVertex(bottomBack, leftNormal, Color.White, tex00, w, h, textureIndex, ao);
                            builder.AddVertex(topBack, leftNormal, Color.White, tex01, w, h, textureIndex, ao);
                            builder.AddVertex(bottomFront, leftNormal, Color.White, tex10, w, h, textureIndex, ao);
                            builder.AddVertex(topFront, leftNormal, Color.White, tex11, w, h, textureIndex, ao);

                            builder.AddIndex(i + 1);
                            builder.AddIndex(i);
                            builder.AddIndex(i + 3);

                            builder.AddIndex(i + 2);
                            builder.AddIndex(i + 3);
                            builder.AddIndex(i);
                        }
                        break;
                }
                return true;
            }

            private readonly bool AmbientOcclusion = true;

            public byte ComputeAmbientOcclusion(VoxelMapIterator ite, FaceType type, Direction dir, int x, int y, int z)
            {
                return 0;
            }

            public byte ComputeAmbientOcclusion(VoxelMap map, FaceType type, Direction dir, int x, int y, int z)
            {
                byte ao = 0b11111111;
                if (!AmbientOcclusion || !FaceInfo.Get(type).IsOpaque)
                {
                    return ao;
                }
                switch (dir)
                {
                    case Direction.Front:
                        {
                            byte a00 = ComputeAmbientOcclusion(map, x, y, z, Direction.LeftFront, Direction.BottomFront, Direction.BottomLeftFront);
                            byte a01 = ComputeAmbientOcclusion(map, x, y, z, Direction.LeftFront, Direction.TopFront, Direction.TopLeftFront);
                            byte a10 = ComputeAmbientOcclusion(map, x, y, z, Direction.RightFront, Direction.BottomFront, Direction.BottomRightFront);
                            byte a11 = ComputeAmbientOcclusion(map, x, y, z, Direction.RightFront, Direction.TopFront, Direction.TopRightFront);
                            ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                        }
                        break;
                    case Direction.Back:
                        {
                            byte a00 = ComputeAmbientOcclusion(map, x, y, z, Direction.RightBack, Direction.BottomBack, Direction.BottomRightBack);
                            byte a01 = ComputeAmbientOcclusion(map, x, y, z, Direction.RightBack, Direction.TopBack, Direction.TopRightBack);
                            byte a10 = ComputeAmbientOcclusion(map, x, y, z, Direction.LeftBack, Direction.BottomBack, Direction.BottomLeftBack);
                            byte a11 = ComputeAmbientOcclusion(map, x, y, z, Direction.LeftBack, Direction.TopBack, Direction.TopLeftBack);
                            ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                        }
                        break;
                    case Direction.Top:
                        {
                            byte a00 = ComputeAmbientOcclusion(map, x, y, z, Direction.TopLeft, Direction.TopFront, Direction.TopLeftFront);
                            byte a01 = ComputeAmbientOcclusion(map, x, y, z, Direction.TopLeft, Direction.TopBack, Direction.TopLeftBack);
                            byte a10 = ComputeAmbientOcclusion(map, x, y, z, Direction.TopRight, Direction.TopFront, Direction.TopRightFront);
                            byte a11 = ComputeAmbientOcclusion(map, x, y, z, Direction.TopRight, Direction.TopBack, Direction.TopRightBack);
                            ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                        }
                        break;
                    case Direction.Bottom:
                        {
                            byte a00 = ComputeAmbientOcclusion(map, x, y, z, Direction.BottomRight, Direction.BottomFront, Direction.BottomRightFront);
                            byte a01 = ComputeAmbientOcclusion(map, x, y, z, Direction.BottomRight, Direction.BottomBack, Direction.BottomRightBack);
                            byte a10 = ComputeAmbientOcclusion(map, x, y, z, Direction.BottomLeft, Direction.BottomFront, Direction.BottomLeftFront);
                            byte a11 = ComputeAmbientOcclusion(map, x, y, z, Direction.BottomLeft, Direction.BottomBack, Direction.BottomLeftBack);
                            ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                        }
                        break;
                    case Direction.Right:
                        {
                            byte a00 = ComputeAmbientOcclusion(map, x, y, z, Direction.BottomRight, Direction.RightFront, Direction.BottomRightFront);
                            byte a01 = ComputeAmbientOcclusion(map, x, y, z, Direction.TopRight, Direction.RightFront, Direction.TopRightFront);
                            byte a10 = ComputeAmbientOcclusion(map, x, y, z, Direction.BottomRight, Direction.RightBack, Direction.BottomRightBack);
                            byte a11 = ComputeAmbientOcclusion(map, x, y, z, Direction.TopRight, Direction.RightBack, Direction.TopRightBack);
                            ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                        }
                        break;
                    case Direction.Left:
                        {
                            byte a00 = ComputeAmbientOcclusion(map, x, y, z, Direction.BottomLeft, Direction.LeftBack, Direction.BottomLeftBack);
                            byte a01 = ComputeAmbientOcclusion(map, x, y, z, Direction.TopLeft, Direction.LeftBack, Direction.TopLeftBack);
                            byte a10 = ComputeAmbientOcclusion(map, x, y, z, Direction.BottomLeft, Direction.LeftFront, Direction.BottomLeftFront);
                            byte a11 = ComputeAmbientOcclusion(map, x, y, z, Direction.TopLeft, Direction.LeftFront, Direction.TopLeftFront);
                            ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                        }
                        break;
                }
                return ao;
            }

            private byte ComputeAmbientOcclusion(VoxelMap map, int x, int y, int z, Direction dir1, Direction dir2, Direction dirCorner)
            {
                // TODO cache values...
                // TODO use FaceInfo not VoxelType
                bool side1 = !VoxelInfo.Get(Value(map, x, y, z, dir1)).IsTransparent;
                bool side2 = !VoxelInfo.Get(Value(map, x, y, z, dir2)).IsTransparent;
                if (side1 && side2)
                {
                    return 0;
                }
                bool corner = !VoxelInfo.Get(Value(map, x, y, z, dirCorner)).IsTransparent;
                return (byte)(3 - (Convert.ToByte(side1) + Convert.ToByte(side2) + Convert.ToByte(corner)));
            }

            private static byte CombineAmbientOcclusion(byte a00, byte a01, byte a10, byte a11)
            {
                return (byte)(a00 | (a01 << 2) | (a10 << 4) | (a11 << 6));
            }

            private int Value(VoxelMap map, int x, int y, int z, Direction dir)
            {
                Octree<VoxelChunk>.DirData dirData = Octree<VoxelChunk>.DIR_DATA[(int)dir];
                int nx = x + dirData.dX;
                int ny = y + dirData.dY;
                int nz = z + dirData.dZ;

                int lookupIndex = 0;
                if (nx < map.X0())
                {
                    lookupIndex |= Mask.LEFT;
                }
                else if (nx >= map.X0() + map.Size())
                {
                    lookupIndex |= Mask.RIGHT;
                }
                if (ny < map.Y0())
                {
                    lookupIndex |= Mask.BOTTOM;
                }
                else if (ny >= map.Y0() + map.Size())
                {
                    lookupIndex |= Mask.TOP;
                }
                if (nz < map.Z0())
                {
                    lookupIndex |= Mask.BACK;
                }
                else if (nz >= map.Z0() + map.Size())
                {
                    lookupIndex |= Mask.FRONT;
                }

                if (lookupIndex == 0)
                {
                    // inside
                    return map.Get(nx, ny, nz);
                }

                // outside
                Direction neighbourDir = Octree<VoxelChunk>.DIR_LOOKUP_TABLE[lookupIndex];
                VoxelMap neighbourMap = GetNeighbourMap(map, neighbourDir);
                Debug.Assert(neighbourMap.Contains(nx, ny, nz));
                return neighbourMap.Get(nx, ny, nz);
            }
                       
        }

    }
}
