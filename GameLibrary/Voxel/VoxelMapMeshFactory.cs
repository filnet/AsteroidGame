using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;
using GameLibrary.Util;
using GameLibrary.Voxel;

namespace GameLibrary.Geometry
{
    public class VoxelMapMeshFactory : IMeshFactory
    {
        private VoxelMap map;

        private readonly DrawVisitor drawVisitor;

        public VoxelMapMeshFactory(VoxelMap map)
        {
            this.map = map;
            drawVisitor = new DrawVisitor(this);
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            drawVisitor.builder = VertexBufferBuilder.createVertexPositionNormalTextureBufferBuilder(gd, 0, 0);

            map.Visit(drawVisitor);

            Mesh mesh = new Mesh(PrimitiveType.TriangleList, drawVisitor.primitiveCount);
            mesh.BoundingVolume = drawVisitor.GetBoundingVolume();
            drawVisitor.builder.setToMesh(mesh);
            return mesh;
        }

        class DrawVisitor : Voxel.Visitor
        {
            //private static float DEFAULT_VOXEL_SIZE = 0.5773502692f; // 1 over the square root of 3

            private readonly VoxelMapMeshFactory parent;
            public VertexBufferBuilder builder;

            private readonly float d = 0.5f;
            private int size;

            private bool scale = true;
            private float s = 0.75f;

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

            public DrawVisitor(VoxelMapMeshFactory parent)
            {
                this.parent = parent;

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
                    s = 1;
                }
                scaleXY = new Vector3(s, s, 1);
                scaleXZ = new Vector3(s, 1, s);
                scaleYZ = new Vector3(1, s, s);

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

            public bool Visit(VoxelMapIterator ite)
            {
                Matrix m = Matrix.Identity; // Matrix.CreateScale(0.95f);
                Vector3 t;
                t.X = 2 * d * (ite.X - (size - 1) / 2f);
                t.Y = 2 * d * (ite.Y - (size - 1) / 2f);
                t.Z = 2 * d * (ite.Z - (size - 1) / 2f);
                m = m * Matrix.CreateTranslation(2 * d * (ite.X - (size - 1) / 2f), 2 * d * (ite.Y - (size - 1) / 2f), 2 * d * (ite.Z - (size - 1) / 2f));
                //m = m * Matrix.CreateTranslation(d * (2 * ite.X - size) / size, d * (2 * ite.Y - size) / size, d * (2 * ite.Z - size) / size);
                //Console.Out.WriteLine(2 * d * (ite.X - (size - 1) / 2f) + " " + 2 * d * (ite.Y - (size - 1) / 2f) + " " + 2 * d * (ite.Z - (size - 1) / 2f));

                /*
                // top face vertices
                Vector3.Add(ref topLeftFront, ref t, out _topLeftFront);
                Vector3.Add(ref topLeftBack, ref t, out _topLeftBack);
                Vector3.Add(ref topRightFront, ref t, out _topRightFront);
                Vector3.Add(ref topRightBack, ref t, out _topRightBack);

                // bottom face vertices
                Vector3.Add(ref bottomLeftFront, ref t, out _bottomLeftFront);
                Vector3.Add(ref bottomLeftBack, ref t, out _bottomLeftBack);
                Vector3.Add(ref bottomRightFront, ref t, out _bottomRightFront);
                Vector3.Add(ref bottomRightBack, ref t, out _bottomRightBack);
                */
                // front face
                if ((ite.Neighbours & (int)Neighbour.Front) == 0)
                {
                    Vector3.Multiply(ref topLeftFront, ref scaleXY, out _topLeftFront);
                    Vector3.Multiply(ref bottomLeftFront, ref scaleXY, out _bottomLeftFront);
                    Vector3.Multiply(ref topRightFront, ref scaleXY, out _topRightFront);
                    Vector3.Multiply(ref bottomRightFront, ref scaleXY, out _bottomRightFront);

                    Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                    Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);

                    builder.AddVertex(_topLeftFront, frontNormal, Color.White, textureTopLeft);
                    builder.AddVertex(_bottomLeftFront, frontNormal, Color.White, textureBottomLeft);
                    builder.AddVertex(_topRightFront, frontNormal, Color.White, textureTopRight);
                    builder.AddVertex(_bottomLeftFront, frontNormal, Color.White, textureBottomLeft);
                    builder.AddVertex(_bottomRightFront, frontNormal, Color.White, textureBottomRight);
                    builder.AddVertex(_topRightFront, frontNormal, Color.White, textureTopRight);
                    primitiveCount += 2;
                }
                // back face
                if ((ite.Neighbours & (int)Neighbour.Back) == 0)
                {
                    Vector3.Multiply(ref topLeftBack, ref scaleXY, out _topLeftBack);
                    Vector3.Multiply(ref topRightBack, ref scaleXY, out _topRightBack);
                    Vector3.Multiply(ref bottomLeftBack, ref scaleXY, out _bottomLeftBack);
                    Vector3.Multiply(ref bottomRightBack, ref scaleXY, out _bottomRightBack);

                    Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);
                    Vector3.Add(ref _topRightBack, ref t, out _topRightBack);
                    Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                    Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);

                    builder.AddVertex(_topLeftBack, backNormal, Color.White, textureTopRight);
                    builder.AddVertex(_topRightBack, backNormal, Color.White, textureTopLeft);
                    builder.AddVertex(_bottomLeftBack, backNormal, Color.White, textureBottomRight);
                    builder.AddVertex(_bottomLeftBack, backNormal, Color.White, textureBottomRight);
                    builder.AddVertex(_topRightBack, backNormal, Color.White, textureTopLeft);
                    builder.AddVertex(_bottomRightBack, backNormal, Color.White, textureBottomLeft);
                    primitiveCount += 2;
                }
                // top face
                if ((ite.Neighbours & (int)Neighbour.Top) == 0)
                {
                    Vector3.Multiply(ref topLeftFront, ref scaleXZ, out _topLeftFront);
                    Vector3.Multiply(ref topLeftBack, ref scaleXZ, out _topLeftBack);
                    Vector3.Multiply(ref topRightFront, ref scaleXZ, out _topRightFront);
                    Vector3.Multiply(ref topRightBack, ref scaleXZ, out _topRightBack);

                    Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);
                    Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                    Vector3.Add(ref _topRightBack, ref t, out _topRightBack);

                    builder.AddVertex(_topLeftFront, topNormal, Color.White, textureBottomLeft);
                    builder.AddVertex(_topRightBack, topNormal, Color.White, textureTopRight);
                    builder.AddVertex(_topLeftBack, topNormal, Color.White, textureTopLeft);
                    builder.AddVertex(_topLeftFront, topNormal, Color.White, textureBottomLeft);
                    builder.AddVertex(_topRightFront, topNormal, Color.White, textureBottomRight);
                    builder.AddVertex(_topRightBack, topNormal, Color.White, textureTopRight);
                    primitiveCount += 2;
                }
                // bottom face
                if ((ite.Neighbours & (int)Neighbour.Bottom) == 0)
                {
                    Vector3.Multiply(ref bottomLeftFront, ref scaleXZ, out _bottomLeftFront);
                    Vector3.Multiply(ref bottomLeftBack, ref scaleXZ, out _bottomLeftBack);
                    Vector3.Multiply(ref bottomRightFront, ref scaleXZ, out _bottomRightFront);
                    Vector3.Multiply(ref bottomRightBack, ref scaleXZ, out _bottomRightBack);

                    Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                    Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                    Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);

                    builder.AddVertex(_bottomLeftFront, bottomNormal, Color.White, textureTopLeft);
                    builder.AddVertex(_bottomLeftBack, bottomNormal, Color.White, textureBottomLeft);
                    builder.AddVertex(_bottomRightBack, bottomNormal, Color.White, textureBottomRight);
                    builder.AddVertex(_bottomLeftFront, bottomNormal, Color.White, textureTopLeft);
                    builder.AddVertex(_bottomRightBack, bottomNormal, Color.White, textureBottomRight);
                    builder.AddVertex(_bottomRightFront, bottomNormal, Color.White, textureTopRight);
                    primitiveCount += 2;
                }
                // left face
                if ((ite.Neighbours & (int)Neighbour.Left) == 0)
                {
                    Vector3.Multiply(ref topLeftFront, ref scaleYZ, out _topLeftFront);
                    Vector3.Multiply(ref bottomLeftBack, ref scaleYZ, out _bottomLeftBack);
                    Vector3.Multiply(ref bottomLeftFront, ref scaleYZ, out _bottomLeftFront);
                    Vector3.Multiply(ref topLeftBack, ref scaleYZ, out _topLeftBack);

                    Vector3.Add(ref _topLeftFront, ref t, out _topLeftFront);
                    Vector3.Add(ref _bottomLeftBack, ref t, out _bottomLeftBack);
                    Vector3.Add(ref _bottomLeftFront, ref t, out _bottomLeftFront);
                    Vector3.Add(ref _topLeftBack, ref t, out _topLeftBack);

                    builder.AddVertex(_topLeftFront, leftNormal, Color.White, textureTopRight);
                    builder.AddVertex(_bottomLeftBack, leftNormal, Color.White, textureBottomLeft);
                    builder.AddVertex(_bottomLeftFront, leftNormal, Color.White, textureBottomRight);
                    builder.AddVertex(_topLeftBack, leftNormal, Color.White, textureTopLeft);
                    builder.AddVertex(_bottomLeftBack, leftNormal, Color.White, textureBottomLeft);
                    builder.AddVertex(_topLeftFront, leftNormal, Color.White, textureTopRight);
                    primitiveCount += 2;
                }
                // right face
                if ((ite.Neighbours & (int)Neighbour.Right) == 0)
                {
                    Vector3.Multiply(ref topRightFront, ref scaleYZ, out _topRightFront);
                    Vector3.Multiply(ref bottomRightBack, ref scaleYZ, out _bottomRightBack);
                    Vector3.Multiply(ref bottomRightFront, ref scaleYZ, out _bottomRightFront);
                    Vector3.Multiply(ref topRightBack, ref scaleYZ, out _topRightBack);

                    Vector3.Add(ref _topRightFront, ref t, out _topRightFront);
                    Vector3.Add(ref _bottomRightBack, ref t, out _bottomRightBack);
                    Vector3.Add(ref _bottomRightFront, ref t, out _bottomRightFront);
                    Vector3.Add(ref _topRightBack, ref t, out _topRightBack);

                    builder.AddVertex(_topRightFront, rightNormal, Color.White, textureTopLeft);
                    builder.AddVertex(_bottomRightFront, rightNormal, Color.White, textureBottomLeft);
                    builder.AddVertex(_bottomRightBack, rightNormal, Color.White, textureBottomRight);
                    builder.AddVertex(_topRightBack, rightNormal, Color.White, textureTopRight);
                    builder.AddVertex(_topRightFront, rightNormal, Color.White, textureTopLeft);
                    builder.AddVertex(_bottomRightBack, rightNormal, Color.White, textureBottomRight);
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
