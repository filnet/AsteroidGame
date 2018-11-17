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
        private readonly VoxelOctree octree;

        private readonly GraphicsDevice graphicsDevice;

        private readonly DrawVisitor drawVisitor;

        public VoxelMapMeshFactory(VoxelOctree octree, GraphicsDevice graphicsDevice)
        {
            this.octree = octree;
            this.graphicsDevice = graphicsDevice;

            drawVisitor = new DrawVisitor(this);
            drawVisitor.builder = VertexBufferBuilder<VoxelVertex>.createVoxelVertexBufferBuilder(graphicsDevice);
        }

        public Mesh CreateMesh(GraphicsDevice graphicsDevice)
        {
            // not used...
            return null;
        }

        public Mesh CreateMesh(OctreeNode<VoxelChunk> node)
        {
            drawVisitor.builder.Reset();

            VoxelMapIterator ite = new DefaultVoxelMapIterator(octree, node);

            node.obj.VoxelMap.Visit(drawVisitor, ite);

            if (drawVisitor.primitiveCount <= 0)
            {
                return null;
            }
            Mesh mesh = new Mesh(PrimitiveType.TriangleList, drawVisitor.primitiveCount);
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingBox(octree.Center, octree.HalfSize);

            drawVisitor.builder.SetToMesh(mesh);
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

                scaleXY = new Vector3(s.X, s.Y, 1);
                scaleXZ = new Vector3(s.X, 1, s.Z);
                scaleYZ = new Vector3(1, s.Y, s.Z);
            }

            public bool Begin(int size)
            {
                this.size = size;
                vertexCount = 0;
                primitiveCount = 0;
                return true;
            }


            //static int c = 0;

            public bool Visit(VoxelMapIterator ite)
            {
                int v = ite.Value();
                if (v == 0)
                {
                    ite.emptyVoxelsCount++;
                    return true;
                }

                TileInfo tileInfo = tiles[v];

                // initialize 
                Matrix m = Matrix.Identity; // Matrix.CreateScale(0.95f);
                Vector3 t;
                t.X = 2 * d * (ite.X - (size - 1) / 2f);
                t.Y = 2 * d * (ite.Y - (size - 1) / 2f);
                t.Z = 2 * d * (ite.Z - (size - 1) / 2f);
                //m = m * Matrix.CreateTranslation(2 * d * (ite.X - (size - 1) / 2f), 2 * d * (ite.Y - (size - 1) / 2f), 2 * d * (ite.Z - (size - 1) / 2f));
                //m = m * Matrix.CreateTranslation(d * (2 * ite.X - size) / size, d * (2 * ite.Y - size) / size, d * (2 * ite.Z - size) / size);
                //Console.Out.WriteLine(2 * d * (ite.X - (size - 1) / 2f) + " " + 2 * d * (ite.Y - (size - 1) / 2f) + " " + 2 * d * (ite.Z - (size - 1) / 2f));

                if (!scale)
                {
                    Vector3.Add(ref bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref bottomRightFront, ref t, out _bottomRightFront);
                    Vector3.Add(ref topRightFront, ref t, out _topRightFront);

                    Vector3.Add(ref bottomRightBack, ref t, out _bottomRightBack);
                    Vector3.Add(ref topRightBack, ref t, out _topRightBack);
                    Vector3.Add(ref bottomLeftBack, ref t, out _bottomLeftBack);
                    Vector3.Add(ref topLeftBack, ref t, out _topLeftBack);
                }

                bool showFrontFace = (ite.Value(Direction.Front) == 0);
                if (showFrontFace)
                {
                    if (scale)
                    {
                        Vector3.Multiply(ref bottomLeftFront, ref scaleXY, out _bottomLeftFront);
                        Vector3.Multiply(ref topLeftFront, ref scaleXY, out _topLeftFront);
                        Vector3.Multiply(ref bottomRightFront, ref scaleXY, out _bottomRightFront);
                        Vector3.Multiply(ref topRightFront, ref scaleXY, out _topRightFront);

                        Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                        Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                        Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                        Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                    }

                    int tile = tileInfo.TextureIndex(Face.Front);

                    // BottomLeftFront
                    int a00 = VertexAmbientOcclusion(ite, Direction.LeftFront, Direction.BottomFront, Direction.BottomLeftFront);
                    // TopLeftFront
                    int a01 = VertexAmbientOcclusion(ite, Direction.LeftFront, Direction.TopFront, Direction.TopLeftFront);
                    // BottomRightFront
                    int a10 = VertexAmbientOcclusion(ite, Direction.RightFront, Direction.BottomFront, Direction.BottomRightFront);
                    // TopRightFront
                    int a11 = VertexAmbientOcclusion(ite, Direction.RightFront, Direction.TopFront, Direction.TopRightFront);
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

                bool showBackFace = (ite.Value(Direction.Back) == 0);
                if (showBackFace)
                {
                    if (scale)
                    {
                        Vector3.Multiply(ref bottomRightBack, ref scaleXY, out _bottomRightBack);
                        Vector3.Multiply(ref topRightBack, ref scaleXY, out _topRightBack);
                        Vector3.Multiply(ref bottomLeftBack, ref scaleXY, out _bottomLeftBack);
                        Vector3.Multiply(ref topLeftBack, ref scaleXY, out _topLeftBack);

                        Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);
                        Vector3.Add(ref _topRightBack, ref t, out _topRightBack);
                        Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                        Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);
                    }

                    int tile = tileInfo.TextureIndex(Face.Back);

                    int a00 = VertexAmbientOcclusion(ite, Direction.RightBack, Direction.BottomBack, Direction.BottomRightBack);
                    int a01 = VertexAmbientOcclusion(ite, Direction.RightBack, Direction.TopBack, Direction.TopRightBack);
                    int a10 = VertexAmbientOcclusion(ite, Direction.LeftBack, Direction.BottomBack, Direction.BottomLeftBack);
                    int a11 = VertexAmbientOcclusion(ite, Direction.LeftBack, Direction.TopBack, Direction.TopLeftBack);
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

                bool showTopFace = (ite.Value(Direction.Top) == 0);
                if (showTopFace)
                {
                    if (scale)
                    {
                        Vector3.Multiply(ref topLeftFront, ref scaleXZ, out _topLeftFront);
                        Vector3.Multiply(ref topLeftBack, ref scaleXZ, out _topLeftBack);
                        Vector3.Multiply(ref topRightFront, ref scaleXZ, out _topRightFront);
                        Vector3.Multiply(ref topRightBack, ref scaleXZ, out _topRightBack);

                        Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                        Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);
                        Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                        Vector3.Add(ref _topRightBack, ref t, out _topRightBack);
                    }

                    int tile = tileInfo.TextureIndex(Face.Top);

                    int a00 = VertexAmbientOcclusion(ite, Direction.TopLeft, Direction.TopFront, Direction.TopLeftFront);
                    int a01 = VertexAmbientOcclusion(ite, Direction.TopLeft, Direction.TopBack, Direction.TopLeftBack);
                    int a10 = VertexAmbientOcclusion(ite, Direction.TopRight, Direction.TopFront, Direction.TopRightFront);
                    int a11 = VertexAmbientOcclusion(ite, Direction.TopRight, Direction.TopBack, Direction.TopRightBack);
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

                bool showBottomFace = (ite.Value(Direction.Bottom) == 0);
                if (showBottomFace)
                {
                    if (scale)
                    {
                        Vector3.Multiply(ref bottomRightFront, ref scaleXZ, out _bottomRightFront);
                        Vector3.Multiply(ref bottomRightBack, ref scaleXZ, out _bottomRightBack);
                        Vector3.Multiply(ref bottomLeftFront, ref scaleXZ, out _bottomLeftFront);
                        Vector3.Multiply(ref bottomLeftBack, ref scaleXZ, out _bottomLeftBack);

                        Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                        Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);
                        Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                        Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                    }

                    int tile = tileInfo.TextureIndex(Face.Bottom);

                    int a00 = VertexAmbientOcclusion(ite, Direction.BottomRight, Direction.BottomFront, Direction.BottomRightFront);
                    int a01 = VertexAmbientOcclusion(ite, Direction.BottomRight, Direction.BottomBack, Direction.BottomRightBack);
                    int a10 = VertexAmbientOcclusion(ite, Direction.BottomLeft, Direction.BottomFront, Direction.BottomLeftFront);
                    int a11 = VertexAmbientOcclusion(ite, Direction.BottomLeft, Direction.BottomBack, Direction.BottomLeftBack);
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

                bool showLeftFace = (ite.Value(Direction.Left) == 0);
                if (showLeftFace)
                {
                    if (scale)
                    {
                        Vector3.Multiply(ref bottomLeftBack, ref scaleYZ, out _bottomLeftBack);
                        Vector3.Multiply(ref topLeftFront, ref scaleYZ, out _topLeftFront);
                        Vector3.Multiply(ref bottomLeftFront, ref scaleYZ, out _bottomLeftFront);
                        Vector3.Multiply(ref topLeftBack, ref scaleYZ, out _topLeftBack);

                        Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                        Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                        Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                        Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);
                    }

                    int tile = tileInfo.TextureIndex(Face.Left);

                    int a00 = VertexAmbientOcclusion(ite, Direction.BottomLeft, Direction.LeftBack, Direction.BottomLeftBack);
                    int a01 = VertexAmbientOcclusion(ite, Direction.TopLeft, Direction.LeftBack, Direction.TopLeftBack);
                    int a10 = VertexAmbientOcclusion(ite, Direction.BottomLeft, Direction.LeftFront, Direction.BottomLeftFront);
                    int a11 = VertexAmbientOcclusion(ite, Direction.TopLeft, Direction.LeftFront, Direction.TopLeftFront);
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

                bool showRightFace = (ite.Value(Direction.Right) == 0);
                if (showRightFace)
                {
                    if (scale)
                    {
                        Vector3.Multiply(ref topRightFront, ref scaleYZ, out _topRightFront);
                        Vector3.Multiply(ref bottomRightBack, ref scaleYZ, out _bottomRightBack);
                        Vector3.Multiply(ref bottomRightFront, ref scaleYZ, out _bottomRightFront);
                        Vector3.Multiply(ref topRightBack, ref scaleYZ, out _topRightBack);

                        Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                        Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);
                        Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                        Vector3.Add(ref _topRightBack, ref t, out _topRightBack);
                    }

                    int tile = tileInfo.TextureIndex(Face.Right);

                    int a00 = VertexAmbientOcclusion(ite, Direction.BottomRight, Direction.RightFront, Direction.BottomRightFront);
                    int a01 = VertexAmbientOcclusion(ite, Direction.TopRight, Direction.RightFront, Direction.TopRightFront);
                    int a10 = VertexAmbientOcclusion(ite, Direction.BottomRight, Direction.RightBack, Direction.BottomRightBack);
                    int a11 = VertexAmbientOcclusion(ite, Direction.TopRight, Direction.RightBack, Direction.TopRightBack);
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
                ite.voxelsCount += (primitiveCount > 0) ? 1 : 0;
                ite.facesCount += primitiveCount / 2;
                return true;
            }

            public int VertexAmbientOcclusion(VoxelMapIterator ite, Direction side1, Direction side2, Direction corner)
            {
                // TODO cache values...
                return VoxelUtil.VertexAmbientOcclusion(ite.Value(side1) != 0, ite.Value(side2) != 0, ite.Value(corner) != 0);
            }

            public bool End()
            {
                return true;
            }
        }
    }
}
