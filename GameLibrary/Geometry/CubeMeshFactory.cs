using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;
using GameLibrary.Util;

namespace GameLibrary.Geometry
{
    public class CubeMeshFactory : IMeshFactory
    {
        static VertexDeclaration vertexDeclaration = new VertexDeclaration(new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        });

        private float size;

        private static float DEFAULT_SIZE = 0.5773502692f; // 1 over the square root of 3

        public CubeMeshFactory() : this(DEFAULT_SIZE)
        {
        }

        public CubeMeshFactory(float size)
        {
            this.size = size;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            VertexBufferBuilder<VertexPositionNormalTexture> builder = VertexBufferBuilder<VertexPositionNormalTexture>.createVertexPositionNormalTextureBufferBuilder(gd, 36, 0);

            float d = size;

            // top face
            Vector3 topLeftFront = new Vector3(-d, d, d);
            Vector3 topLeftBack = new Vector3(-d, d, -d);
            Vector3 topRightFront = new Vector3(d, d, d);
            Vector3 topRightBack = new Vector3(d, d, -d);

            // bottom face
            Vector3 bottomLeftFront = new Vector3(-d, -d, d);
            Vector3 bottomLeftBack = new Vector3(-d, -d, -d);
            Vector3 bottomRightFront = new Vector3(d, -d, d);
            Vector3 bottomRightBack = new Vector3(d, -d, -d);

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

            // front face
            builder.AddVertex(topLeftFront, frontNormal, Color.White, textureTopLeft);
            builder.AddVertex(bottomLeftFront, frontNormal, Color.White, textureBottomLeft);
            builder.AddVertex(topRightFront, frontNormal, Color.White, textureTopRight);
            builder.AddVertex(bottomLeftFront, frontNormal, Color.White, textureBottomLeft);
            builder.AddVertex(bottomRightFront, frontNormal, Color.White, textureBottomRight);
            builder.AddVertex(topRightFront, frontNormal, Color.White, textureTopRight);

            // back face
            builder.AddVertex(topLeftBack, backNormal, Color.White, textureTopRight);
            builder.AddVertex(topRightBack, backNormal, Color.White, textureTopLeft);
            builder.AddVertex(bottomLeftBack, backNormal, Color.White, textureBottomRight);
            builder.AddVertex(bottomLeftBack, backNormal, Color.White, textureBottomRight);
            builder.AddVertex(topRightBack, backNormal, Color.White, textureTopLeft);
            builder.AddVertex(bottomRightBack, backNormal, Color.White, textureBottomLeft);

            // top face
            builder.AddVertex(topLeftFront, topNormal, Color.White, textureBottomLeft);
            builder.AddVertex(topRightBack, topNormal, Color.White, textureTopRight);
            builder.AddVertex(topLeftBack, topNormal, Color.White, textureTopLeft);
            builder.AddVertex(topLeftFront, topNormal, Color.White, textureBottomLeft);
            builder.AddVertex(topRightFront, topNormal, Color.White, textureBottomRight);
            builder.AddVertex(topRightBack, topNormal, Color.White, textureTopRight);

            // bottom face
            builder.AddVertex(bottomLeftFront, bottomNormal, Color.White, textureTopLeft);
            builder.AddVertex(bottomLeftBack, bottomNormal, Color.White, textureBottomLeft);
            builder.AddVertex(bottomRightBack, bottomNormal, Color.White, textureBottomRight);
            builder.AddVertex(bottomLeftFront, bottomNormal, Color.White, textureTopLeft);
            builder.AddVertex(bottomRightBack, bottomNormal, Color.White, textureBottomRight);
            builder.AddVertex(bottomRightFront, bottomNormal, Color.White, textureTopRight);

            // left face
            builder.AddVertex(topLeftFront, leftNormal, Color.White, textureTopRight);
            builder.AddVertex(bottomLeftBack, leftNormal, Color.White, textureBottomLeft);
            builder.AddVertex(bottomLeftFront, leftNormal, Color.White, textureBottomRight);
            builder.AddVertex(topLeftBack, leftNormal, Color.White, textureTopLeft);
            builder.AddVertex(bottomLeftBack, leftNormal, Color.White, textureBottomLeft);
            builder.AddVertex(topLeftFront, leftNormal, Color.White, textureTopRight);

            // right face
            builder.AddVertex(topRightFront, rightNormal, Color.White, textureTopLeft);
            builder.AddVertex(bottomRightFront, rightNormal, Color.White, textureBottomLeft);
            builder.AddVertex(bottomRightBack, rightNormal, Color.White, textureBottomRight);
            builder.AddVertex(topRightBack, rightNormal, Color.White, textureTopRight);
            builder.AddVertex(topRightFront, rightNormal, Color.White, textureTopLeft);
            builder.AddVertex(bottomRightBack, rightNormal, Color.White, textureBottomRight);

            Mesh mesh = new Mesh(PrimitiveType.TriangleList, 2 * 6);
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingSphere(Vector3.Zero, 2 * d);
            builder.setToMesh(mesh);
            return mesh;
        }

    }
}
