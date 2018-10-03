using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace GameLibrary.Geometry.Common
{
    public abstract class VertexBufferBuilder
    {
        protected GraphicsDevice gd;

        protected int vIndex;

        protected short[] indices;
        protected int iIndex;

        public VertexBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            this.gd = gd;
            vIndex = 0;
            if (indexCount > 0)
            {
                indices = new short[indexCount];
            }
        }

        public abstract void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate);

        public void AddIndex(int index)
        {
            indices[iIndex++] = (short) index;
        }

        public virtual void setToMesh(Mesh mesh)
        {
            if (indices != null)
            {
                mesh.IndexBuffer = new IndexBuffer(gd, typeof(short), indices.Count(), BufferUsage.None);
                mesh.IndexBuffer.SetData(indices);
            }
        }

        private class VertexPositionNormalTextureBufferBuilder : VertexBufferBuilder
        {
            private VertexPositionNormalTexture[] vertices;
            public VertexPositionNormalTextureBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, vertexCount, indexCount)
            {
                if (vertexCount > 0)
                {
                    vertices = new VertexPositionNormalTexture[vertexCount];
                }
            }
            public override void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate)
            {
                vertices[vIndex++] = new VertexPositionNormalTexture(position, normal, textureCoordinate);
            }
            public override void setToMesh(Mesh mesh)
            {
                base.setToMesh(mesh);
                mesh.VertexDeclaration = VertexPositionNormalTexture.VertexDeclaration;
                mesh.VertexBuffer = new VertexBuffer(gd, typeof(VertexPositionNormalTexture), vertices.Count(), BufferUsage.None);
                mesh.VertexBuffer.SetData(vertices);
                mesh.VertexCount = vertices.Count();
            }
        }

        public static VertexBufferBuilder createVertexPositionNormalTextureBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionNormalTextureBufferBuilder(gd, vertexCount, indexCount);
        }

        private class VertexPositionColorBufferBuilder : VertexBufferBuilder
        {
            private VertexPositionColor[] vertices;
            public VertexPositionColorBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, vertexCount, indexCount)
            {
                if (vertexCount > 0)
                {
                    vertices = new VertexPositionColor[vertexCount];
                }
            }
            public override void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate)
            {
                //Color color = new Color(normal);
                //color.A = 128;
                vertices[vIndex++] = new VertexPositionColor(position, color);
            }
            public override void setToMesh(Mesh mesh)
            {
                base.setToMesh(mesh);
                mesh.VertexDeclaration = VertexPositionColor.VertexDeclaration;
                mesh.VertexBuffer = new VertexBuffer(gd, typeof(VertexPositionColor), vertices.Count(), BufferUsage.None);
                mesh.VertexBuffer.SetData(vertices);
                mesh.VertexCount = vertices.Count();
            }
        }

        public static VertexBufferBuilder createVertexPositionColorBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionColorBufferBuilder(gd, vertexCount, indexCount);
        }
    }
}
