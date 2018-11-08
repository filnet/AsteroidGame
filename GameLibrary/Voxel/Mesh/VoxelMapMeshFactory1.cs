using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;
using GameLibrary.Util;
using GameLibrary.Voxel;

namespace GameLibrary.Voxel
{
    public class VoxelMapMeshFactory1 : IMeshFactory
    {
        private VoxelMap map;
        private VoxelMap[] neighbours;

        private readonly DrawVisitor drawVisitor;

        public VoxelMapMeshFactory1(VoxelMap map) : this(map, null)
        {
        }

        public VoxelMapMeshFactory1(VoxelMap map, VoxelMap[] neighbours)
        {
            this.map = map;
            this.neighbours = neighbours;
            drawVisitor = new DrawVisitor(this);
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            drawVisitor.builder = VertexBufferBuilder.createVertexPositionColorNormalTextureArrayBufferBuilder(gd, 0, 0);

            VoxelMapIterator ite;
            if (neighbours == null)
            {
                ite = new SimpleVoxelMapIterator(map);
            }
            else
            {
                ite = new DefaultVoxelMapIterator(map, neighbours);
            }

            map.Visit(drawVisitor, ite);

            Mesh mesh = new Mesh(PrimitiveType.TriangleList, drawVisitor.primitiveCount);
            mesh.BoundingVolume = drawVisitor.GetBoundingVolume();
            drawVisitor.builder.setToMesh(mesh);
            return mesh;
        }

        class DrawVisitor : Voxel.Visitor
        {
            //private static float DEFAULT_VOXEL_SIZE = 0.5773502692f; // 1 over the square root of 3

            private readonly VoxelMapMeshFactory1 factory;
            public VertexBufferBuilder builder;

            private readonly float d = 0.5f;
            private int size;

            private bool scale = false;
            private Vector3 s = new Vector3(0.75f, 0.75f, 0.75f);

            public int primitiveCount;

            // top face vertices
            Vector3 topLeftFront;
            Vector3 topLeftBack;
            Vector3 topRightFront;
            Vector3 topRightBack;

            Vector3 _topLeftFront;
            Vector3 _topLeftBack;
            Vector3 _topRightFront;
            Vector3 _topRightBack;

            // bottom face vertices
            Vector3 bottomLeftFront;
            Vector3 bottomLeftBack;
            Vector3 bottomRightFront;
            Vector3 bottomRightBack;

            Vector3 _bottomLeftFront;
            Vector3 _bottomLeftBack;
            Vector3 _bottomRightFront;
            Vector3 _bottomRightBack;

            Vector3 scaleXY;
            Vector3 scaleXZ;
            Vector3 scaleYZ;

            // FIXME texture coordinates can be computed from xyz and surface normals
            // see https://0fps.net/2013/07/09/texture-atlases-wrapping-and-mip-mapping/
            // vec2 tileUV = vec2(dot(normal.zxy, position), 
            // dot(normal.yzx, position))
            // vec2 texCoord = tileOffset + tileSize * fract(tileUV)
            // textures
            Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 0.0f);
            Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);

            // normals
            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            public DrawVisitor(VoxelMapMeshFactory1 factory)
            {
                this.factory = factory;

                // top face vertices
                topLeftFront = new Vector3(-d, d, d);
                topLeftBack = new Vector3(-d, d, -d);
                topRightFront = new Vector3(d, d, d);
                topRightBack = new Vector3(d, d, -d);

                // bottom face vertices
                bottomLeftFront = new Vector3(-d, -d, d);
                bottomLeftBack = new Vector3(-d, -d, -d);
                bottomRightFront = new Vector3(d, -d, d);
                bottomRightBack = new Vector3(d, -d, -d);

                if (!scale)
                {
                    s = Vector3.One;
                }
                scaleXY = new Vector3(s.X, s.Y, 1);
                scaleXZ = new Vector3(s.X, 1, s.Z);
                scaleYZ = new Vector3(1, s.Y, s.Z);

            }

            public GameLibrary.SceneGraph.Bounding.BoundingVolume GetBoundingVolume()
            {
                return new GameLibrary.SceneGraph.Bounding.BoundingBox(Vector3.Zero, new Vector3(d * size, d * size, d * size));
            }

            public bool Begin(int size, int instanceCount, int maxInstanceCount)
            {
                this.size = size;
                return true;
            }

            //static int c = 0;

            public bool Visit(VoxelMapIterator ite)
            {
                //c++;
                //if (c > 6) return false;
                int tile = ite.Value() - 1;
                int topTile = tile;
                int sideTile = tile;
                if (tile == 1) sideTile = 0;
                if (tile == 3) sideTile = 2;
                int bottomTile = tile;

                Color topColor = Color.White;
                Color bottomColor = Color.White;

                Matrix m = Matrix.Identity; // Matrix.CreateScale(0.95f);
                Vector3 t;
                t.X = 2 * d * (ite.X - (size - 1) / 2f);
                t.Y = 2 * d * (ite.Y - (size - 1) / 2f);
                t.Z = 2 * d * (ite.Z - (size - 1) / 2f);
                m = m * Matrix.CreateTranslation(2 * d * (ite.X - (size - 1) / 2f), 2 * d * (ite.Y - (size - 1) / 2f), 2 * d * (ite.Z - (size - 1) / 2f));
                //m = m * Matrix.CreateTranslation(d * (2 * ite.X - size) / size, d * (2 * ite.Y - size) / size, d * (2 * ite.Z - size) / size);
                //Console.Out.WriteLine(2 * d * (ite.X - (size - 1) / 2f) + " " + 2 * d * (ite.Y - (size - 1) / 2f) + " " + 2 * d * (ite.Z - (size - 1) / 2f));

                // front face
                if (false && (ite.Neighbours & (int)Neighbour.Front) == 0)
                {
                    Vector3.Multiply(ref topLeftFront, ref scaleXY, out _topLeftFront);
                    Vector3.Multiply(ref bottomLeftFront, ref scaleXY, out _bottomLeftFront);
                    Vector3.Multiply(ref topRightFront, ref scaleXY, out _topRightFront);
                    Vector3.Multiply(ref bottomRightFront, ref scaleXY, out _bottomRightFront);

                    Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                    Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);

                    builder.AddVertex(_topLeftFront, frontNormal, topColor, textureTopLeft, sideTile);
                    builder.AddVertex(_bottomLeftFront, frontNormal, bottomColor, textureBottomLeft, sideTile);
                    builder.AddVertex(_topRightFront, frontNormal, topColor, textureTopRight, sideTile);
                    builder.AddVertex(_bottomLeftFront, frontNormal, bottomColor, textureBottomLeft, sideTile);
                    builder.AddVertex(_bottomRightFront, frontNormal, bottomColor, textureBottomRight, sideTile);
                    builder.AddVertex(_topRightFront, frontNormal, topColor, textureTopRight, sideTile);
                    primitiveCount += 2;
                }
                // back face
                if (false && (ite.Neighbours & (int)Neighbour.Back) == 0)
                {
                    Vector3.Multiply(ref topLeftBack, ref scaleXY, out _topLeftBack);
                    Vector3.Multiply(ref topRightBack, ref scaleXY, out _topRightBack);
                    Vector3.Multiply(ref bottomLeftBack, ref scaleXY, out _bottomLeftBack);
                    Vector3.Multiply(ref bottomRightBack, ref scaleXY, out _bottomRightBack);

                    Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);
                    Vector3.Add(ref _topRightBack, ref t, out _topRightBack);
                    Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                    Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);

                    builder.AddVertex(_topLeftBack, backNormal, topColor, textureTopRight, sideTile);
                    builder.AddVertex(_topRightBack, backNormal, topColor, textureTopLeft, sideTile);
                    builder.AddVertex(_bottomLeftBack, backNormal, bottomColor, textureBottomRight, sideTile);
                    builder.AddVertex(_bottomLeftBack, backNormal, bottomColor, textureBottomRight, sideTile);
                    builder.AddVertex(_topRightBack, backNormal, topColor, textureTopLeft, sideTile);
                    builder.AddVertex(_bottomRightBack, backNormal, bottomColor, textureBottomLeft, sideTile);
                    primitiveCount += 2;
                }
                // top face
                if (true && (ite.Neighbours & (int)Neighbour.Top) == 0)
                {
                    Vector3.Multiply(ref topLeftFront, ref scaleXZ, out _topLeftFront);
                    Vector3.Multiply(ref topLeftBack, ref scaleXZ, out _topLeftBack);
                    Vector3.Multiply(ref topRightFront, ref scaleXZ, out _topRightFront);
                    Vector3.Multiply(ref topRightBack, ref scaleXZ, out _topRightBack);

                    Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);
                    Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                    Vector3.Add(ref _topRightBack, ref t, out _topRightBack);

                    // TopLeftFront
                    int a00 = VoxelUtil.VertexAmbientOcclusion(ite.Value(Direction.TopLeft) != 0, ite.Value(Direction.TopFront) != 0, ite.Value(Direction.TopLeftFront) != 0);
                    // TopLeftBack
                    int a01 = VoxelUtil.VertexAmbientOcclusion(ite.Value(Direction.TopLeft) != 0, ite.Value(Direction.TopBack) != 0, ite.Value(Direction.TopLeftBack) != 0);
                    // TopRightFront
                    int a10 = VoxelUtil.VertexAmbientOcclusion(ite.Value(Direction.TopRight) != 0, ite.Value(Direction.TopFront) != 0, ite.Value(Direction.TopRightFront) != 0);
                    // TopRightBack
                    int a11 = VoxelUtil.VertexAmbientOcclusion(ite.Value(Direction.TopRight) != 0, ite.Value(Direction.TopBack) != 0, ite.Value(Direction.TopRightBack) != 0); ;

                    Color c00 = VoxelUtil.AmbientOcclusionColor(a00);
                    Color c01 = VoxelUtil.AmbientOcclusionColor(a01);
                    Color c10 = VoxelUtil.AmbientOcclusionColor(a10);
                    Color c11 = VoxelUtil.AmbientOcclusionColor(a11);

                    if (a00 + a11 > a01 + a10)
                    {
                        // generate flipped quad
                        builder.AddVertex(_topLeftBack, topNormal, c01, textureTopLeft, topTile);
                        builder.AddVertex(_topLeftFront, topNormal, c00, textureBottomLeft, topTile);
                        builder.AddVertex(_topRightBack, topNormal, c11, textureTopRight, topTile);

                        builder.AddVertex(_topRightFront, topNormal, c10, textureBottomRight, topTile);
                        builder.AddVertex(_topRightBack, topNormal, c11, textureTopRight, topTile);
                        builder.AddVertex(_topLeftFront, topNormal, c00, textureBottomLeft, topTile);
                    }
                    else
                    {
                        // generate normal quad
                        builder.AddVertex(_topLeftFront, topNormal, c00, textureBottomLeft, topTile);
                        builder.AddVertex(_topRightFront, topNormal, c10, textureBottomRight, topTile);
                        builder.AddVertex(_topLeftBack, topNormal, c01, textureTopLeft, topTile);

                        builder.AddVertex(_topRightBack, topNormal, c11, textureTopRight, topTile);
                        builder.AddVertex(_topLeftBack, topNormal, c01, textureTopLeft, topTile);
                        builder.AddVertex(_topRightFront, topNormal, c10, textureBottomRight, topTile);
                    }
                    primitiveCount += 2;
                }
                // bottom face
                if (false && (ite.Neighbours & (int)Neighbour.Bottom) == 0)
                {
                    Vector3.Multiply(ref bottomLeftFront, ref scaleXZ, out _bottomLeftFront);
                    Vector3.Multiply(ref bottomLeftBack, ref scaleXZ, out _bottomLeftBack);
                    Vector3.Multiply(ref bottomRightFront, ref scaleXZ, out _bottomRightFront);
                    Vector3.Multiply(ref bottomRightBack, ref scaleXZ, out _bottomRightBack);

                    Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                    Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                    Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);

                    builder.AddVertex(_bottomLeftFront, bottomNormal, bottomColor, textureTopLeft, bottomTile);
                    builder.AddVertex(_bottomLeftBack, bottomNormal, bottomColor, textureBottomLeft, bottomTile);
                    builder.AddVertex(_bottomRightBack, bottomNormal, bottomColor, textureBottomRight, bottomTile);
                    builder.AddVertex(_bottomLeftFront, bottomNormal, bottomColor, textureTopLeft, bottomTile);
                    builder.AddVertex(_bottomRightBack, bottomNormal, bottomColor, textureBottomRight, bottomTile);
                    builder.AddVertex(_bottomRightFront, bottomNormal, bottomColor, textureTopRight, bottomTile);
                    primitiveCount += 2;
                }
                // left face
                if (false && (ite.Neighbours & (int)Neighbour.Left) == 0)
                {
                    Vector3.Multiply(ref topLeftFront, ref scaleYZ, out _topLeftFront);
                    Vector3.Multiply(ref bottomLeftBack, ref scaleYZ, out _bottomLeftBack);
                    Vector3.Multiply(ref bottomLeftFront, ref scaleYZ, out _bottomLeftFront);
                    Vector3.Multiply(ref topLeftBack, ref scaleYZ, out _topLeftBack);

                    Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                    Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);

                    builder.AddVertex(_topLeftFront, leftNormal, topColor, textureTopRight, sideTile);
                    builder.AddVertex(_bottomLeftBack, leftNormal, bottomColor, textureBottomLeft, sideTile);
                    builder.AddVertex(_bottomLeftFront, leftNormal, bottomColor, textureBottomRight, sideTile);
                    builder.AddVertex(_topLeftBack, leftNormal, topColor, textureTopLeft, sideTile);
                    builder.AddVertex(_bottomLeftBack, leftNormal, bottomColor, textureBottomLeft, sideTile);
                    builder.AddVertex(_topLeftFront, leftNormal, topColor, textureTopRight, sideTile);
                    primitiveCount += 2;
                }
                // right face
                if (false && (ite.Neighbours & (int)Neighbour.Right) == 0)
                {
                    Vector3.Multiply(ref topRightFront, ref scaleYZ, out _topRightFront);
                    Vector3.Multiply(ref bottomRightBack, ref scaleYZ, out _bottomRightBack);
                    Vector3.Multiply(ref bottomRightFront, ref scaleYZ, out _bottomRightFront);
                    Vector3.Multiply(ref topRightBack, ref scaleYZ, out _topRightBack);

                    Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                    Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);
                    Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                    Vector3.Add(ref _topRightBack, ref t, out _topRightBack);

                    builder.AddVertex(_topRightFront, rightNormal, topColor, textureTopLeft, sideTile);
                    builder.AddVertex(_bottomRightFront, rightNormal, bottomColor, textureBottomLeft, sideTile);
                    builder.AddVertex(_bottomRightBack, rightNormal, bottomColor, textureBottomRight, sideTile);
                    builder.AddVertex(_topRightBack, rightNormal, topColor, textureTopRight, sideTile);
                    builder.AddVertex(_topRightFront, rightNormal, topColor, textureTopLeft, sideTile);
                    builder.AddVertex(_bottomRightBack, rightNormal, bottomColor, textureBottomRight, sideTile);
                    primitiveCount += 2;
                }
                return true;
            }

            public bool End()
            {
                return true;
            }
        }
    }
}
