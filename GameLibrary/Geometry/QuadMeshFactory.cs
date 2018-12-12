using System;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.Geometry
{
    public class QuadMeshFactory : IMeshFactory
    {
        private float width;
        private float height;

        public QuadMeshFactory() : this(336.0f, 210.0f)
        {
        }

        public QuadMeshFactory(float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            VertexBufferBuilder<VertexPositionColorNormalTexture> builder = 
                VertexBufferBuilder<VertexPositionColorNormalTexture>.createVertexPositionColorNormalTextureBufferBuilder(gd, 4, 6);
            //VertexBufferBuilder<VertexPositionColor> builder =
            //    VertexBufferBuilder<VertexPositionColor>.createVertexPositionColorBufferBuilder(gd, 4, 6);

            // front face
            Vector3 bottomLeft = new Vector3(0, 0, 0);
            Vector3 topLeft = new Vector3(0, height, 0);
            Vector3 bottomRight = new Vector3(width, 0, 0);
            Vector3 topRight = new Vector3(width, height, 0);

            // textures
            Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 0.0f);

            // normals
            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            
            int i = builder.AddVertex(bottomLeft, frontNormal, Color.White, textureBottomLeft);
            builder.AddVertex(topLeft, frontNormal, Color.White, textureTopLeft);
            builder.AddVertex(bottomRight, frontNormal, Color.White, textureBottomRight);
            builder.AddVertex(topRight, frontNormal, Color.White, textureTopRight);

            builder.AddIndex(i + 1);
            builder.AddIndex(i);
            builder.AddIndex(i + 3);

            builder.AddIndex(i + 2);
            builder.AddIndex(i + 3);
            builder.AddIndex(i);

            Mesh mesh = new Mesh(PrimitiveType.TriangleList, 2);
            //mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingSphere(Vector3.Zero, 2 * d);
            builder.SetToMesh(mesh);
            return mesh;
        }

    }
}
