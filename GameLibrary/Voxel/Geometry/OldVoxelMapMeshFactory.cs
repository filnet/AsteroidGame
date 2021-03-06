﻿using GameLibrary.Geometry.Common;
using GameLibrary.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace GameLibrary.Voxel.Geometry
{
    public class OldVoxelMapMeshFactory : IMeshFactory
    {
        private readonly GraphicsDevice graphicsDevice;

        private readonly DrawVisitor drawVisitor;

        private ObjectPool<VoxelMap, ArrayVoxelMap> pool;

        public OldVoxelMapMeshFactory(GraphicsDevice graphicsDevice, ObjectPool<VoxelMap, ArrayVoxelMap> pool)
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

        class DrawVisitor : Voxel.Visitor
        {
            //private static float DEFAULT_VOXEL_SIZE = 0.5773502692f; // 1 over the square root of 3

            public VertexBufferBuilder<VoxelVertex> opaqueBuilder;
            public VertexBufferBuilder<VoxelVertex> transparentBuilder;

            private readonly float d = 0.5f;

            private bool scale = false;
            private Vector3 s = new Vector3(0.75f, 0.75f, 0.75f);

            // front face vertices
            Vector3 bottomLeftFront;
            Vector3 topLeftFront;
            Vector3 bottomRightFront;
            Vector3 topRightFront;

            // back face vertices
            Vector3 bottomRightBack;
            Vector3 topRightBack;
            Vector3 bottomLeftBack;
            Vector3 topLeftBack;

            Vector3 _bottomLeftFront;
            Vector3 _topLeftFront;
            Vector3 _bottomRightFront;
            Vector3 _topRightFront;

            Vector3 _bottomRightBack;
            Vector3 _topRightBack;
            Vector3 _bottomLeftBack;
            Vector3 _topLeftBack;

            Vector3 scaleXY;
            Vector3 scaleXZ;
            Vector3 scaleYZ;

            // FIXME texture coordinates can be computed from xyz and surface normals
            // see https://0fps.net/2013/07/09/texture-atlases-wrapping-and-mip-mapping/
            // vec2 textureIndexUV = vec2(dot(normal.zxy, position), 
            // dot(normal.yzx, position))
            // vec2 texCoord = textureIndexOffset + textureIndexSize * fract(textureIndexUV)
            // textures

            // BottomLeft   (0,0)
            // TopLeft      (0,1)
            // BottomRight  (1,0)
            // TopRight     (1,1)
            // [NOT TRUE ANYMORE] Note that in the texture coordinates below the Y axis is inversed (for bitmaps...)
            // The above is not true anymore and Y flipping is done in the shader (not good...)
            Vector2 tex00 = new Vector2(0.0f, 0.0f);
            Vector2 tex01 = new Vector2(0.0f, 1.0f);
            Vector2 tex10 = new Vector2(1.0f, 0.0f);
            Vector2 tex11 = new Vector2(1.0f, 1.0f);

            // normals
            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            public DrawVisitor()
            {
                // front face vertices
                bottomLeftFront = new Vector3(-d, -d, d);
                topLeftFront = new Vector3(-d, d, d);
                bottomRightFront = new Vector3(d, -d, d);
                topRightFront = new Vector3(d, d, d);

                // back face vertices
                bottomRightBack = new Vector3(d, -d, -d);
                topRightBack = new Vector3(d, d, -d);
                bottomLeftBack = new Vector3(-d, -d, -d);
                topLeftBack = new Vector3(-d, d, -d);

                scaleXY = new Vector3(s.X, s.Y, 1);
                scaleXZ = new Vector3(s.X, 1, s.Z);
                scaleYZ = new Vector3(1, s.Y, s.Z);
            }

            public bool Begin(int size)
            {
                //this.size = size;
                return true;
            }

            public bool Visit(VoxelMapIterator ite)
            {
                int v = ite.Value();
                if (v == 0)
                {
                    ite.emptyVoxelsCount++;
                    return true;
                }

                VoxelInfo voxelInfo = VoxelInfo.Get(v);

                VertexBufferBuilder<VoxelVertex> builder = (voxelInfo.IsOpaque) ? opaqueBuilder : transparentBuilder;

                // initialize 
                Vector3 t;
                t.X = 2 * d * ite.X + d;
                t.Y = 2 * d * ite.Y + d;
                t.Z = 2 * d * ite.Z + d;

                // FIXME hack
                if (voxelInfo.Type == VoxelType.Water)
                {
                    t.Y -= 0.33f;
                }

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

                int faceCount = 0;

                // Front
                if (ShowFace(ite, voxelInfo, Direction.Front, Direction.Back))
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

                    int textureIndex = voxelInfo.TextureIndex(Direction.Front);

                    byte ao = 0b11111111;
                    if (!voxelInfo.IsTransparent)
                    {
                        byte a00 = ComputeAmbientOcclusion(ite, Direction.LeftFront, Direction.BottomFront, Direction.BottomLeftFront);
                        byte a01 = ComputeAmbientOcclusion(ite, Direction.LeftFront, Direction.TopFront, Direction.TopLeftFront);
                        byte a10 = ComputeAmbientOcclusion(ite, Direction.RightFront, Direction.BottomFront, Direction.BottomRightFront);
                        byte a11 = ComputeAmbientOcclusion(ite, Direction.RightFront, Direction.TopFront, Direction.TopRightFront);
                        ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                    }

                    int i = builder.AddVertex(_bottomLeftFront, frontNormal, Color.White, tex00, textureIndex, ao);
                    builder.AddVertex(_topLeftFront, frontNormal, Color.White, tex01, textureIndex, ao);
                    builder.AddVertex(_bottomRightFront, frontNormal, Color.White, tex10, textureIndex, ao);
                    builder.AddVertex(_topRightFront, frontNormal, Color.White, tex11, textureIndex, ao);

                    builder.AddIndex(i + 1);
                    builder.AddIndex(i);
                    builder.AddIndex(i + 3);

                    builder.AddIndex(i + 2);
                    builder.AddIndex(i + 3);
                    builder.AddIndex(i);

                    faceCount++;
                }
                // Back
                if (ShowFace(ite, voxelInfo, Direction.Back, Direction.Front))
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

                    int textureIndex = voxelInfo.TextureIndex(Direction.Back);

                    byte ao = 0b11111111;
                    if (!voxelInfo.IsTransparent)
                    {
                        byte a00 = ComputeAmbientOcclusion(ite, Direction.RightBack, Direction.BottomBack, Direction.BottomRightBack);
                        byte a01 = ComputeAmbientOcclusion(ite, Direction.RightBack, Direction.TopBack, Direction.TopRightBack);
                        byte a10 = ComputeAmbientOcclusion(ite, Direction.LeftBack, Direction.BottomBack, Direction.BottomLeftBack);
                        byte a11 = ComputeAmbientOcclusion(ite, Direction.LeftBack, Direction.TopBack, Direction.TopLeftBack);
                        ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                    }

                    int i = builder.AddVertex(_bottomRightBack, backNormal, Color.White, tex00, textureIndex, ao);
                    builder.AddVertex(_topRightBack, backNormal, Color.White, tex01, textureIndex, ao);
                    builder.AddVertex(_bottomLeftBack, backNormal, Color.White, tex10, textureIndex, ao);
                    builder.AddVertex(_topLeftBack, backNormal, Color.White, tex11, textureIndex, ao);

                    builder.AddIndex(i + 1);
                    builder.AddIndex(i);
                    builder.AddIndex(i + 3);

                    builder.AddIndex(i + 2);
                    builder.AddIndex(i + 3);
                    builder.AddIndex(i);

                    faceCount++;
                }
                // Top
                if (ShowFace(ite, voxelInfo, Direction.Top, Direction.Bottom))
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

                    int textureIndex = voxelInfo.TextureIndex(Direction.Top);

                    byte ao = 0b11111111;
                    if (!voxelInfo.IsTransparent)
                    {
                        byte a00 = ComputeAmbientOcclusion(ite, Direction.TopLeft, Direction.TopFront, Direction.TopLeftFront);
                        byte a01 = ComputeAmbientOcclusion(ite, Direction.TopLeft, Direction.TopBack, Direction.TopLeftBack);
                        byte a10 = ComputeAmbientOcclusion(ite, Direction.TopRight, Direction.TopFront, Direction.TopRightFront);
                        byte a11 = ComputeAmbientOcclusion(ite, Direction.TopRight, Direction.TopBack, Direction.TopRightBack);
                        ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                    }

                    int i = builder.AddVertex(_topLeftFront, topNormal, Color.White, tex00, textureIndex, ao);
                    builder.AddVertex(_topLeftBack, topNormal, Color.White, tex01, textureIndex, ao);
                    builder.AddVertex(_topRightFront, topNormal, Color.White, tex10, textureIndex, ao);
                    builder.AddVertex(_topRightBack, topNormal, Color.White, tex11, textureIndex, ao);

                    builder.AddIndex(i + 1);
                    builder.AddIndex(i);
                    builder.AddIndex(i + 3);

                    builder.AddIndex(i + 2);
                    builder.AddIndex(i + 3);
                    builder.AddIndex(i);

                    faceCount++;
                }
                // Bottom
                if (ShowFace(ite, voxelInfo, Direction.Bottom, Direction.Top))
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

                    int textureIndex = voxelInfo.TextureIndex(Direction.Bottom);

                    byte ao = 0b11111111;
                    if (!voxelInfo.IsTransparent)
                    {
                        byte a00 = ComputeAmbientOcclusion(ite, Direction.BottomRight, Direction.BottomFront, Direction.BottomRightFront);
                        byte a01 = ComputeAmbientOcclusion(ite, Direction.BottomRight, Direction.BottomBack, Direction.BottomRightBack);
                        byte a10 = ComputeAmbientOcclusion(ite, Direction.BottomLeft, Direction.BottomFront, Direction.BottomLeftFront);
                        byte a11 = ComputeAmbientOcclusion(ite, Direction.BottomLeft, Direction.BottomBack, Direction.BottomLeftBack);
                        ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                    }

                    int i = builder.AddVertex(_bottomRightFront, bottomNormal, Color.White, tex00, textureIndex, ao);
                    builder.AddVertex(_bottomRightBack, bottomNormal, Color.White, tex01, textureIndex, ao);
                    builder.AddVertex(_bottomLeftFront, bottomNormal, Color.White, tex10, textureIndex, ao);
                    builder.AddVertex(_bottomLeftBack, bottomNormal, Color.White, tex11, textureIndex, ao);

                    builder.AddIndex(i + 1);
                    builder.AddIndex(i);
                    builder.AddIndex(i + 3);

                    builder.AddIndex(i + 2);
                    builder.AddIndex(i + 3);
                    builder.AddIndex(i);

                    faceCount++;
                }
                // Right
                if (ShowFace(ite, voxelInfo, Direction.Right, Direction.Left))
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

                    int textureIndex = voxelInfo.TextureIndex(Direction.Right);

                    byte ao = 0b11111111;
                    if (!voxelInfo.IsTransparent)
                    {
                        byte a00 = ComputeAmbientOcclusion(ite, Direction.BottomRight, Direction.RightFront, Direction.BottomRightFront);
                        byte a01 = ComputeAmbientOcclusion(ite, Direction.TopRight, Direction.RightFront, Direction.TopRightFront);
                        byte a10 = ComputeAmbientOcclusion(ite, Direction.BottomRight, Direction.RightBack, Direction.BottomRightBack);
                        byte a11 = ComputeAmbientOcclusion(ite, Direction.TopRight, Direction.RightBack, Direction.TopRightBack);
                        ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                    }

                    int i = builder.AddVertex(_bottomRightFront, rightNormal, Color.White, tex00, textureIndex, ao);
                    builder.AddVertex(_topRightFront, rightNormal, Color.White, tex01, textureIndex, ao);
                    builder.AddVertex(_bottomRightBack, rightNormal, Color.White, tex10, textureIndex, ao);
                    builder.AddVertex(_topRightBack, rightNormal, Color.White, tex11, textureIndex, ao);

                    builder.AddIndex(i + 1);
                    builder.AddIndex(i);
                    builder.AddIndex(i + 3);

                    builder.AddIndex(i + 2);
                    builder.AddIndex(i + 3);
                    builder.AddIndex(i);

                    faceCount++;
                }
                // Left
                if (ShowFace(ite, voxelInfo, Direction.Left, Direction.Right))
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

                    int textureIndex = voxelInfo.TextureIndex(Direction.Left);

                    byte ao = 0b11111111;
                    if (!voxelInfo.IsTransparent)
                    {
                        byte a00 = ComputeAmbientOcclusion(ite, Direction.BottomLeft, Direction.LeftBack, Direction.BottomLeftBack);
                        byte a01 = ComputeAmbientOcclusion(ite, Direction.TopLeft, Direction.LeftBack, Direction.TopLeftBack);
                        byte a10 = ComputeAmbientOcclusion(ite, Direction.BottomLeft, Direction.LeftFront, Direction.BottomLeftFront);
                        byte a11 = ComputeAmbientOcclusion(ite, Direction.TopLeft, Direction.LeftFront, Direction.TopLeftFront);
                        ao = CombineAmbientOcclusion(a00, a01, a10, a11);
                    }

                    int i = builder.AddVertex(_bottomLeftBack, leftNormal, Color.White, tex00, textureIndex, ao);
                    builder.AddVertex(_topLeftBack, leftNormal, Color.White, tex01, textureIndex, ao);
                    builder.AddVertex(_bottomLeftFront, leftNormal, Color.White, tex10, textureIndex, ao);
                    builder.AddVertex(_topLeftFront, leftNormal, Color.White, tex11, textureIndex, ao);

                    builder.AddIndex(i + 1);
                    builder.AddIndex(i);
                    builder.AddIndex(i + 3);

                    builder.AddIndex(i + 2);
                    builder.AddIndex(i + 3);
                    builder.AddIndex(i);

                    faceCount++;
                }
                ite.voxelsCount += (faceCount > 0) ? 1 : 0;
                ite.facesCount += faceCount;
                return true;
            }

            public bool ShowFace(VoxelMapIterator ite, VoxelInfo voxelInfo, Direction face, Direction oppositeFace)
            {
                VoxelInfo oppositeVoxelInfo = VoxelInfo.Get(ite.Value(face));
                // check if voxel has a face in given direction
                bool show = voxelInfo.HasFace(face);
                // check if neighbour opposite face is opaque and, if yes, don't show this face
                show = show && !oppositeVoxelInfo.HasOpaqueFace(oppositeFace);
                // check that both are not transparent (transparent cancels out...)
                show = show && !(voxelInfo.IsTransparent && oppositeVoxelInfo.IsSolid && oppositeVoxelInfo.IsTransparent);
                return show;
            }

            // TODO remove (should be handled by VoxelMap)
            public byte ComputeAmbientOcclusion(VoxelMapIterator ite, Direction dir1, Direction dir2, Direction dirCorner)
            {
                // TODO cache values...
                bool side1 = !VoxelInfo.Get(ite.Value(dir1)).IsTransparent;
                bool side2 = !VoxelInfo.Get(ite.Value(dir2)).IsTransparent;
                if (side1 && side2)
                {
                    return 0;
                }
                bool corner = !VoxelInfo.Get(ite.Value(dirCorner)).IsTransparent;
                return (byte)(3 - (Convert.ToByte(side1) + Convert.ToByte(side2) + Convert.ToByte(corner)));
            }

            // TODO remove (should be handled by VoxelMap)
            public static byte CombineAmbientOcclusion(byte a00, byte a01, byte a10, byte a11)
            {
                return (byte)(a00 | (a01 << 2) | (a10 << 4) | (a11 << 6));
            }

            public bool End()
            {
                return true;
            }
        }
    }
}
