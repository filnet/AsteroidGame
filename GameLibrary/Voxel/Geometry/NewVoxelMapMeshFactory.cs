using GameLibrary.Geometry.Common;
using GameLibrary.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace GameLibrary.Voxel.Geometry
{
    public class NewVoxelMapMeshFactory : IMeshFactory
    {
        private readonly GraphicsDevice graphicsDevice;

        private readonly DrawVisitor drawVisitor;

        private ObjectPool<VoxelMap, ArrayVoxelMap> pool;

        public NewVoxelMapMeshFactory(GraphicsDevice graphicsDevice, ObjectPool<VoxelMap, ArrayVoxelMap> pool)
        {
            this.graphicsDevice = graphicsDevice;

            drawVisitor = new DrawVisitor();
            drawVisitor.opaqueBuilder = new VertexBufferBuilder<VoxelVertex>(graphicsDevice);
            drawVisitor.transparentBuilder = new VertexBufferBuilder<VoxelVertex>(graphicsDevice);

            this.pool = pool;
        }

        public Mesh CreateMesh(GraphicsDevice graphicsDevice)
        {
            // not used...
            throw new NotImplementedException();
        }

        public void CreateMeshes(VoxelChunk voxelChunk, VoxelMapIterator ite)
        {
            drawVisitor.opaqueBuilder.Reset();
            drawVisitor.transparentBuilder.Reset();

            ArrayVoxelMap arrayVoxelMap = pool.Take(voxelChunk.VoxelMap);
            Debug.Assert(arrayVoxelMap != null);
            try
            {
                arrayVoxelMap.Visit(drawVisitor, ite);
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
            public VertexBufferBuilder<VoxelVertex> opaqueBuilder;
            public VertexBufferBuilder<VoxelVertex> transparentBuilder;

            private readonly float d = 0.5f;

            // normals
            static readonly Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            static readonly Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            static readonly Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            static readonly Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            static readonly Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            static readonly Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            private static readonly Vector3[] NORMALS =
            {
                leftNormal, rightNormal, bottomNormal, topNormal, backNormal, frontNormal
            };

            public DrawVisitor()
            {
            }

            public bool Begin(int size)
            {
                return true;
            }

            public bool AddFace(FaceType type, Direction dir, Vector3 p, int w, int h)
            {
                FaceInfo faceInfo = FaceInfo.Get(type);
                int textureIndex = faceInfo.TextureIndex();

                VertexBufferBuilder<VoxelVertex> builder = (faceInfo.IsOpaque) ? opaqueBuilder : transparentBuilder;

                // initialize 
                Vector3 t;
                t.X = 2 * d * p.X;// + d;
                t.Y = 2 * d * p.Y;// + d;
                t.Z = 2 * d * p.Z;// + d;

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

                int ao = 0b11111111;

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
                /*
                int i = builder.AddVertex(v1, n, Color.White, tex00, w, h, textureIndex, ao);
                builder.AddVertex(v2, n, Color.White, tex01, w, h, textureIndex, ao);
                builder.AddVertex(v3, n, Color.White, tex10, w, h, textureIndex, ao);
                builder.AddVertex(v4, n, Color.White, tex11, w, h, textureIndex, ao);

                builder.AddIndex(i + 1);
                builder.AddIndex(i);
                builder.AddIndex(i + 3);

                builder.AddIndex(i + 2);
                builder.AddIndex(i + 3);
                builder.AddIndex(i);
                */
                return true;
            }

            public bool End()
            {
                return true;
            }
        }
    }
}
