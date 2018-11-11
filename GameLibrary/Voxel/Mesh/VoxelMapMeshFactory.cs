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
    public enum FaceType { Earth, Grass, Rock, Snow, Test, TestLeft, TestRight, TestBottom, TestTop, TestBack, TestFront }

    [Flags]
    public enum Face { Left, Right, Bottom, Top, Back, Front }

    public class TileInfo
    {
        String Name;
        bool IsSolid;

        FaceType[] faces = new FaceType[6];

        public TileInfo(String name, bool solid) : this(name, FaceType.Test, solid) { }

        public TileInfo(String name, FaceType face) : this(name, face, true) { }

        public TileInfo(String name, FaceType face, bool solid) : this(name, face, face, face, face, face, face, solid) { }

        public TileInfo(String name, FaceType top, FaceType other) : this(name, other, other, other, top, other, other, true) { }

        public TileInfo(String name, FaceType left, FaceType right, FaceType bottom, FaceType top, FaceType back, FaceType front)
            : this(name, left, right, bottom, top, back, front, true)
        {
        }

        public TileInfo(String name, FaceType left, FaceType right, FaceType bottom, FaceType top, FaceType back, FaceType front, bool solid)
        {
            Name = name;
            IsSolid = solid;
            faces[(int)Face.Left] = left;
            faces[(int)Face.Right] = right;
            faces[(int)Face.Bottom] = bottom;
            faces[(int)Face.Top] = top;
            faces[(int)Face.Back] = back;
            faces[(int)Face.Front] = front;
        }

        public int TextureIndex(Face face)
        {
            return (int)faces[(int)face];
        }
    }

    public class VoxelMapMeshFactory : IMeshFactory
    {
        private VoxelMap map;
        private VoxelMap[] neighbours;

        private readonly DrawVisitor drawVisitor;

        public VoxelMapMeshFactory(VoxelMap map) : this(map, null)
        {
        }

        public VoxelMapMeshFactory(VoxelMap map, VoxelMap[] neighbours)
        {
            this.map = map;
            this.neighbours = neighbours;
            drawVisitor = new DrawVisitor(this);
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            drawVisitor.builder = VertexBufferBuilder<VoxelVertex>.createVoxelVertexBufferBuilder(gd, 0, 0);

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

            private readonly VoxelMapMeshFactory factory;
            public VertexBufferBuilder<VoxelVertex> builder;

            private readonly float d = 0.5f;
            private int size;

            private bool scale = false;
            private Vector3 s = new Vector3(0.75f, 0.75f, 0.75f);

            public short vertexCount;
            public short primitiveCount;

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

            private static readonly TileInfo[] tiles = new TileInfo[] {
                new TileInfo("Air",false),
                new TileInfo("Grass", FaceType.Grass, FaceType.Grass),
                new TileInfo("GrassyEarth", FaceType.Grass, FaceType.Earth),
                new TileInfo("Rock", FaceType.Rock, FaceType.Rock),
                new TileInfo("SnowyRock", FaceType.Snow, FaceType.Rock),
                new TileInfo("Test", FaceType.TestLeft, FaceType.TestRight, FaceType.TestBottom, FaceType.TestTop, FaceType.TestBack, FaceType.TestFront)
            };

            public DrawVisitor(VoxelMapMeshFactory factory)
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
                TileInfo tileInfo = tiles[ite.Value()];

                // initialize 
                Matrix m = Matrix.Identity; // Matrix.CreateScale(0.95f);
                Vector3 t;
                t.X = 2 * d * (ite.X - (size - 1) / 2f);
                t.Y = 2 * d * (ite.Y - (size - 1) / 2f);
                t.Z = 2 * d * (ite.Z - (size - 1) / 2f);
                m = m * Matrix.CreateTranslation(2 * d * (ite.X - (size - 1) / 2f), 2 * d * (ite.Y - (size - 1) / 2f), 2 * d * (ite.Z - (size - 1) / 2f));
                //m = m * Matrix.CreateTranslation(d * (2 * ite.X - size) / size, d * (2 * ite.Y - size) / size, d * (2 * ite.Z - size) / size);
                //Console.Out.WriteLine(2 * d * (ite.X - (size - 1) / 2f) + " " + 2 * d * (ite.Y - (size - 1) / 2f) + " " + 2 * d * (ite.Z - (size - 1) / 2f));

                if (true && (ite.Neighbours & (int)Neighbour.Front) == 0)
                {
                    // front face

                    Vector3.Multiply(ref bottomLeftFront, ref scaleXY, out _bottomLeftFront);
                    Vector3.Multiply(ref topLeftFront, ref scaleXY, out _topLeftFront);
                    Vector3.Multiply(ref bottomRightFront, ref scaleXY, out _bottomRightFront);
                    Vector3.Multiply(ref topRightFront, ref scaleXY, out _topRightFront);

                    Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                    Vector3.Add(ref _topRightFront, ref t, out _topRightFront);

                    int tile = tileInfo.TextureIndex(Face.Front);

                    // BottomLeftFront
                    int a00 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.LeftFront) != 0, ite.Value(Direction.BottomFront) != 0, ite.Value(Direction.BottomLeftFront) != 0);
                    // TopLeftFront
                    int a01 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.LeftFront) != 0, ite.Value(Direction.TopFront) != 0, ite.Value(Direction.TopLeftFront) != 0);
                    // BottomRightFront
                    int a10 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.RightFront) != 0, ite.Value(Direction.BottomFront) != 0, ite.Value(Direction.BottomRightFront) != 0);
                    // TopRightFront
                    int a11 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.RightFront) != 0, ite.Value(Direction.TopFront) != 0, ite.Value(Direction.TopRightFront) != 0); ;
                    int ao = VoxelUtil.CombineVertexAmbientOcclusion(a00, a01, a10, a11);

                    builder.AddVertex(_bottomLeftFront, frontNormal, Color.White, tex00, tile, ao);
                    builder.AddVertex(_topLeftFront, frontNormal, Color.White, tex01, tile, ao);
                    builder.AddVertex(_bottomRightFront, frontNormal, Color.White, tex10, tile, ao);
                    builder.AddVertex(_topRightFront, frontNormal, Color.White, tex11, tile, ao);

                    builder.AddIndex((short)(vertexCount + 1));
                    builder.AddIndex((short)(vertexCount + 0));
                    builder.AddIndex((short)(vertexCount + 3));

                    builder.AddIndex((short)(vertexCount + 2));
                    builder.AddIndex((short)(vertexCount + 3));
                    builder.AddIndex((short)(vertexCount + 0));

                    vertexCount += 4;
                    primitiveCount += 2;
                }
                if (true && (ite.Neighbours & (int)Neighbour.Back) == 0)
                {
                    // back face

                    Vector3.Multiply(ref bottomRightBack, ref scaleXY, out _bottomRightBack);
                    Vector3.Multiply(ref topRightBack, ref scaleXY, out _topRightBack);
                    Vector3.Multiply(ref bottomLeftBack, ref scaleXY, out _bottomLeftBack);
                    Vector3.Multiply(ref topLeftBack, ref scaleXY, out _topLeftBack);

                    Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);
                    Vector3.Add(ref _topRightBack, ref t, out _topRightBack);
                    Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                    Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);

                    int tile = tileInfo.TextureIndex(Face.Back);

                    // BottomRightBack
                    int a00 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.RightBack) != 0, ite.Value(Direction.BottomBack) != 0, ite.Value(Direction.BottomRightBack) != 0);
                    // TopRightBack
                    int a01 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.RightBack) != 0, ite.Value(Direction.TopBack) != 0, ite.Value(Direction.TopRightBack) != 0);
                    // BottomLeftBack
                    int a10 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.LeftBack) != 0, ite.Value(Direction.BottomBack) != 0, ite.Value(Direction.BottomLeftBack) != 0);
                    // TopLeftBack
                    int a11 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.LeftBack) != 0, ite.Value(Direction.TopBack) != 0, ite.Value(Direction.TopLeftBack) != 0); ;
                    int ao = VoxelUtil.CombineVertexAmbientOcclusion(a00, a01, a10, a11);

                    builder.AddVertex(_bottomRightBack, backNormal, Color.White, tex00, tile, ao);
                    builder.AddVertex(_topRightBack, backNormal, Color.White, tex01, tile, ao);
                    builder.AddVertex(_bottomLeftBack, backNormal, Color.White, tex10, tile, ao);
                    builder.AddVertex(_topLeftBack, backNormal, Color.White, tex11, tile, ao);

                    builder.AddIndex((short)(vertexCount + 1));
                    builder.AddIndex((short)(vertexCount + 0));
                    builder.AddIndex((short)(vertexCount + 3));

                    builder.AddIndex((short)(vertexCount + 2));
                    builder.AddIndex((short)(vertexCount + 3));
                    builder.AddIndex((short)(vertexCount + 0));

                    vertexCount += 4;
                    primitiveCount += 2;
                }
                if (true && (ite.Neighbours & (int)Neighbour.Top) == 0)
                {
                    // top face

                    Vector3.Multiply(ref topLeftFront, ref scaleXY, out _topLeftFront);
                    Vector3.Multiply(ref topLeftBack, ref scaleXY, out _topLeftBack);
                    Vector3.Multiply(ref topRightFront, ref scaleXY, out _topRightFront);
                    Vector3.Multiply(ref topRightBack, ref scaleXY, out _topRightBack);

                    Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);
                    Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                    Vector3.Add(ref _topRightBack, ref t, out _topRightBack);

                    int tile = tileInfo.TextureIndex(Face.Top);

                    // TopLeftFront
                    int a00 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.TopLeft) != 0, ite.Value(Direction.TopFront) != 0, ite.Value(Direction.TopLeftFront) != 0);
                    // TopLeftBack
                    int a01 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.TopLeft) != 0, ite.Value(Direction.TopBack) != 0, ite.Value(Direction.TopLeftBack) != 0);
                    // TopRightFront
                    int a10 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.TopRight) != 0, ite.Value(Direction.TopFront) != 0, ite.Value(Direction.TopRightFront) != 0);
                    // TopRightBack
                    int a11 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.TopRight) != 0, ite.Value(Direction.TopBack) != 0, ite.Value(Direction.TopRightBack) != 0); ;
                    int ao = VoxelUtil.CombineVertexAmbientOcclusion(a00, a01, a10, a11);

                    builder.AddVertex(_topLeftFront, topNormal, Color.White, tex00, tile, ao);
                    builder.AddVertex(_topLeftBack, topNormal, Color.White, tex01, tile, ao);
                    builder.AddVertex(_topRightFront, topNormal, Color.White, tex10, tile, ao);
                    builder.AddVertex(_topRightBack, topNormal, Color.White, tex11, tile, ao);

                    builder.AddIndex((short)(vertexCount + 1));
                    builder.AddIndex((short)(vertexCount + 0));
                    builder.AddIndex((short)(vertexCount + 3));

                    builder.AddIndex((short)(vertexCount + 2));
                    builder.AddIndex((short)(vertexCount + 3));
                    builder.AddIndex((short)(vertexCount + 0));

                    vertexCount += 4;
                    primitiveCount += 2;
                }
                if (true && (ite.Neighbours & (int)Neighbour.Bottom) == 0)
                {
                    // bottom face

                    Vector3.Multiply(ref bottomRightFront, ref scaleXY, out _bottomRightFront);
                    Vector3.Multiply(ref bottomRightBack, ref scaleXY, out _bottomRightBack);
                    Vector3.Multiply(ref bottomLeftFront, ref scaleXY, out _bottomLeftFront);
                    Vector3.Multiply(ref bottomLeftBack, ref scaleXY, out _bottomLeftBack);

                    Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                    Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);
                    Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);

                    int tile = tileInfo.TextureIndex(Face.Bottom);

                    // BottomRightFront
                    int a00 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.BottomRight) != 0, ite.Value(Direction.BottomFront) != 0, ite.Value(Direction.BottomRightFront) != 0);
                    // BottomRightBack
                    int a01 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.BottomRight) != 0, ite.Value(Direction.BottomBack) != 0, ite.Value(Direction.BottomRightBack) != 0);
                    // BottomLeftFront
                    int a10 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.BottomLeft) != 0, ite.Value(Direction.BottomFront) != 0, ite.Value(Direction.BottomLeftFront) != 0);
                    // BottomLeftBack
                    int a11 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.BottomLeft) != 0, ite.Value(Direction.BottomBack) != 0, ite.Value(Direction.BottomLeftBack) != 0); ;
                    int ao = VoxelUtil.CombineVertexAmbientOcclusion(a00, a01, a10, a11);

                    builder.AddVertex(_bottomRightFront, bottomNormal, Color.White, tex00, tile, ao);
                    builder.AddVertex(_bottomRightBack, bottomNormal, Color.White, tex01, tile, ao);
                    builder.AddVertex(_bottomLeftFront, bottomNormal, Color.White, tex10, tile, ao);
                    builder.AddVertex(_bottomLeftBack, bottomNormal, Color.White, tex11, tile, ao);

                    builder.AddIndex((short)(vertexCount + 1));
                    builder.AddIndex((short)(vertexCount + 0));
                    builder.AddIndex((short)(vertexCount + 3));

                    builder.AddIndex((short)(vertexCount + 2));
                    builder.AddIndex((short)(vertexCount + 3));
                    builder.AddIndex((short)(vertexCount + 0));

                    vertexCount += 4;
                    primitiveCount += 2;
                }
                if (true && (ite.Neighbours & (int)Neighbour.Left) == 0)
                {
                    // left face

                    Vector3.Multiply(ref bottomLeftBack, ref scaleYZ, out _bottomLeftBack);
                    Vector3.Multiply(ref topLeftFront, ref scaleYZ, out _topLeftFront);
                    Vector3.Multiply(ref bottomLeftFront, ref scaleYZ, out _bottomLeftFront);
                    Vector3.Multiply(ref topLeftBack, ref scaleYZ, out _topLeftBack);

                    Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                    Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);

                    int tile = tileInfo.TextureIndex(Face.Left);

                    // BottomLeftBack
                    int a00 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.BottomLeft) != 0, ite.Value(Direction.LeftBack) != 0, ite.Value(Direction.BottomLeftBack) != 0);
                    // TopLeftBack
                    int a01 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.TopLeft) != 0, ite.Value(Direction.LeftBack) != 0, ite.Value(Direction.TopLeftBack) != 0);
                    // BottomLeftFront
                    int a10 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.BottomLeft) != 0, ite.Value(Direction.LeftFront) != 0, ite.Value(Direction.BottomLeftFront) != 0);
                    // TopLeftFront
                    int a11 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.TopLeft) != 0, ite.Value(Direction.LeftFront) != 0, ite.Value(Direction.TopLeftFront) != 0); ;
                    int ao = VoxelUtil.CombineVertexAmbientOcclusion(a00, a01, a10, a11);

                    builder.AddVertex(_bottomLeftBack, leftNormal, Color.White, tex00, tile, ao);
                    builder.AddVertex(_topLeftBack, leftNormal, Color.White, tex01, tile, ao);
                    builder.AddVertex(_bottomLeftFront, leftNormal, Color.White, tex10, tile, ao);
                    builder.AddVertex(_topLeftFront, leftNormal, Color.White, tex11, tile, ao);

                    builder.AddIndex((short)(vertexCount + 1));
                    builder.AddIndex((short)(vertexCount + 0));
                    builder.AddIndex((short)(vertexCount + 3));

                    builder.AddIndex((short)(vertexCount + 2));
                    builder.AddIndex((short)(vertexCount + 3));
                    builder.AddIndex((short)(vertexCount + 0));

                    vertexCount += 4;
                    primitiveCount += 2;
                }
                if (true && (ite.Neighbours & (int)Neighbour.Right) == 0)
                {
                    // right face

                    Vector3.Multiply(ref topRightFront, ref scaleYZ, out _topRightFront);
                    Vector3.Multiply(ref bottomRightBack, ref scaleYZ, out _bottomRightBack);
                    Vector3.Multiply(ref bottomRightFront, ref scaleYZ, out _bottomRightFront);
                    Vector3.Multiply(ref topRightBack, ref scaleYZ, out _topRightBack);

                    Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                    Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);
                    Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                    Vector3.Add(ref _topRightBack, ref t, out _topRightBack);

                    int tile = tileInfo.TextureIndex(Face.Right);

                    // BottomRightFront
                    int a00 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.BottomRight) != 0, ite.Value(Direction.RightFront) != 0, ite.Value(Direction.BottomRightFront) != 0);
                    // TopRightFront
                    int a01 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.TopRight) != 0, ite.Value(Direction.RightFront) != 0, ite.Value(Direction.TopRightFront) != 0);
                    // BottomRightBack
                    int a10 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.BottomRight) != 0, ite.Value(Direction.RightBack) != 0, ite.Value(Direction.BottomRightBack) != 0);
                    // TopRightBack
                    int a11 = VoxelUtil.VertexAmbientOcclusion(
                        ite.Value(Direction.TopRight) != 0, ite.Value(Direction.RightBack) != 0, ite.Value(Direction.TopRightBack) != 0); ;
                    int ao = VoxelUtil.CombineVertexAmbientOcclusion(a00, a01, a10, a11);

                    builder.AddVertex(_bottomRightFront, rightNormal, Color.White, tex00, tile, ao);
                    builder.AddVertex(_topRightFront, rightNormal, Color.White, tex01, tile, ao);
                    builder.AddVertex(_bottomRightBack, rightNormal, Color.White, tex10, tile, ao);
                    builder.AddVertex(_topRightBack, rightNormal, Color.White, tex11, tile, ao);

                    builder.AddIndex((short)(vertexCount + 1));
                    builder.AddIndex((short)(vertexCount + 0));
                    builder.AddIndex((short)(vertexCount + 3));

                    builder.AddIndex((short)(vertexCount + 2));
                    builder.AddIndex((short)(vertexCount + 3));
                    builder.AddIndex((short)(vertexCount + 0));

                    vertexCount += 4;
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
