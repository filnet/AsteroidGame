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
            VertexBufferBuilder<VertexPositionNormalTexture> builder = new VertexBufferBuilder<VertexPositionNormalTexture>(gd, 36, 0);

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
            builder.AddVertex(topLeftFront, frontNormal, textureTopLeft);
            builder.AddVertex(bottomLeftFront, frontNormal, textureBottomLeft);
            builder.AddVertex(topRightFront, frontNormal, textureTopRight);
            builder.AddVertex(bottomLeftFront, frontNormal, textureBottomLeft);
            builder.AddVertex(bottomRightFront, frontNormal, textureBottomRight);
            builder.AddVertex(topRightFront, frontNormal, textureTopRight);

            // back face
            builder.AddVertex(topLeftBack, backNormal, textureTopRight);
            builder.AddVertex(topRightBack, backNormal, textureTopLeft);
            builder.AddVertex(bottomLeftBack, backNormal, textureBottomRight);
            builder.AddVertex(bottomLeftBack, backNormal, textureBottomRight);
            builder.AddVertex(topRightBack, backNormal, textureTopLeft);
            builder.AddVertex(bottomRightBack, backNormal, textureBottomLeft);

            // top face
            builder.AddVertex(topLeftFront, topNormal, textureBottomLeft);
            builder.AddVertex(topRightBack, topNormal, textureTopRight);
            builder.AddVertex(topLeftBack, topNormal, textureTopLeft);
            builder.AddVertex(topLeftFront, topNormal, textureBottomLeft);
            builder.AddVertex(topRightFront, topNormal, textureBottomRight);
            builder.AddVertex(topRightBack, topNormal, textureTopRight);

            // bottom face
            builder.AddVertex(bottomLeftFront, bottomNormal, textureTopLeft);
            builder.AddVertex(bottomLeftBack, bottomNormal, textureBottomLeft);
            builder.AddVertex(bottomRightBack, bottomNormal, textureBottomRight);
            builder.AddVertex(bottomLeftFront, bottomNormal, textureTopLeft);
            builder.AddVertex(bottomRightBack, bottomNormal, textureBottomRight);
            builder.AddVertex(bottomRightFront, bottomNormal, textureTopRight);

            // left face
            builder.AddVertex(topLeftFront, leftNormal, textureTopRight);
            builder.AddVertex(bottomLeftBack, leftNormal, textureBottomLeft);
            builder.AddVertex(bottomLeftFront, leftNormal, textureBottomRight);
            builder.AddVertex(topLeftBack, leftNormal, textureTopLeft);
            builder.AddVertex(bottomLeftBack, leftNormal, textureBottomLeft);
            builder.AddVertex(topLeftFront, leftNormal, textureTopRight);

            // right face
            builder.AddVertex(topRightFront, rightNormal, textureTopLeft);
            builder.AddVertex(bottomRightFront, rightNormal, textureBottomLeft);
            builder.AddVertex(bottomRightBack, rightNormal, textureBottomRight);
            builder.AddVertex(topRightBack, rightNormal, textureTopRight);
            builder.AddVertex(topRightFront, rightNormal, textureTopLeft);
            builder.AddVertex(bottomRightBack, rightNormal, textureBottomRight);

            Mesh mesh = new Mesh(PrimitiveType.TriangleList, 2 * 6);
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.Sphere(Vector3.Zero, 2 * d);
            builder.SetToMesh(mesh);
            return mesh;
        }

    }
}
