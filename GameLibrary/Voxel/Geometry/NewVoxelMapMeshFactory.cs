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

            //private readonly float d = 0.5f;

            // FIXME texture coordinates can be computed from xyz and surface normals
            // see https://0fps.net/2013/07/09/texture-atlases-wrapping-and-mip-mapping/
            // vec2 tileUV = vec2(dot(normal.zxy, position), 
            // dot(normal.yzx, position))
            // vec2 texCoord = tileOffset + tileSize * fract(tileUV)
            // textures

            // BottomLeft   (0,0)
            // TopLeft      (0,1)
            // BottomRight  (1,0)
            // TopRight     (1,1)
            // Note that in the texture coordinates below the Y axis is inversed (for bitmaps...)
            Vector2 tex00 = new Vector2(0.0f, 1.0f);
            Vector2 tex01 = new Vector2(0.0f, 0.0f);
            Vector2 tex10 = new Vector2(1.0f, 1.0f);
            Vector2 tex11 = new Vector2(1.0f, 0.0f);

            // normals

            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            private static readonly Vector3[] NORMALS =
            {
                new Vector3(-1.0f, 0.0f, 0.0f), // Left
                new Vector3(1.0f, 0.0f, 0.0f), // Right
                new Vector3(0.0f, -1.0f, 0.0f), // Bottom
                new Vector3(0.0f, 1.0f, 0.0f), // Top
                new Vector3(0.0f, 0.0f, -1.0f), // Back
                new Vector3(0.0f, 0.0f, 1.0f), // Front
            };

            public DrawVisitor()
            {
            }

            public bool Begin(int size)
            {
                return true;
            }

            public bool AddFace(FaceType type, Direction dir, int w, int h, ref Vector3 v1, ref Vector3 v2, ref Vector3 v3, ref Vector3 v4)
            {
                // !!!
                VertexBufferBuilder<VoxelVertex> builder;
                FaceInfo faceInfo = FaceInfo.Get(type);
                if (faceInfo.IsOpaque)
                {
                    builder = opaqueBuilder;
                }
                else
                {
                    builder = transparentBuilder;
                }
                int textureIndex = faceInfo.TextureIndex();

                if (type == FaceType.Water)
                {
                    float dy = -0.33f;
                    v1.Y += dy;
                    v2.Y += dy;
                    v3.Y += dy;
                    v4.Y += dy;
                }

                // TODO use precomputed normals...
                Vector3 n = NORMALS[(int)dir];

                Vector2 tex00 = new Vector2(0, 0);
                Vector2 tex01 = new Vector2(0, h);
                Vector2 tex10 = new Vector2(w, 0);
                Vector2 tex11 = new Vector2(w, h);

                int ao = 0b11111111;

                int i = builder.AddVertex(v1, n, Color.White, tex00, textureIndex, ao);
                builder.AddVertex(v2, n, Color.White, tex01, textureIndex, ao);
                builder.AddVertex(v3, n, Color.White, tex10, textureIndex, ao);
                builder.AddVertex(v4, n, Color.White, tex11, textureIndex, ao);

                builder.AddIndex(i + 1);
                builder.AddIndex(i);
                builder.AddIndex(i + 3);

                builder.AddIndex(i + 2);
                builder.AddIndex(i + 3);
                builder.AddIndex(i);

                return true;
            }

            public bool End()
            {
                return true;
            }
        }
    }
}
