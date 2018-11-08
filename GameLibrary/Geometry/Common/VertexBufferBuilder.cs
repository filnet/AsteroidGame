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

        public VertexBufferBuilder(GraphicsDevice gd, int indexCount)
        {
            this.gd = gd;
            vIndex = 0;
            iIndex = 0;
            if (indexCount > 0)
            {
                indices = new short[indexCount];
            }
        }

        public void AddVertex(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
        {
            AddVertex(position, normal, Color.White, textureCoordinate);
        }

        public void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate)
        {
            AddVertex(position, normal, color, textureCoordinate, 0, 0);
        }

        public void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex)
        {
            AddVertex(position, normal, color, textureCoordinate, textureIndex, 0);
        }

        public abstract void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex);

        public void AddIndex(int index)
        {
            if (indices == null)
            {
                indices = new short[32];
            }
            if (iIndex == indices.Count())
            {
                Array.Resize(ref indices, 2 * indices.Count());
            }
            indices[iIndex++] = (short)index;
        }

        public virtual void setToMesh(Mesh mesh)
        {
            if (indices != null && iIndex > 0)
            {
                mesh.IndexBuffer = new IndexBuffer(gd, typeof(short), iIndex, BufferUsage.None);
                mesh.IndexBuffer.SetData(indices);
            }
        }

        public static VertexBufferBuilder createVertexPositionNormalTextureBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionNormalTextureBufferBuilder(gd, vertexCount, indexCount);
        }

        public static VertexBufferBuilder createVertexPositionNormalTextureArrayBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionNormalTextureArrayBufferBuilder(gd, vertexCount, indexCount);
        }

        public static VertexBufferBuilder createVertexPositionColorNormalTextureArrayBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionColorNormalTextureArrayBufferBuilder(gd, vertexCount, indexCount);
        }

        public static VertexBufferBuilder createVertexPositionColorBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionColorBufferBuilder(gd, vertexCount, indexCount);
        }

        public static VertexBufferBuilder createVoxelVertexBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VoxelVertexBufferBuilder(gd, vertexCount, indexCount);
        }

        private class VertexPositionNormalTextureBufferBuilder : VertexBufferBuilder
        {
            private VertexPositionNormalTexture[] vertices;

            public VertexPositionNormalTextureBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, indexCount)
            {
                if (vertexCount > 0)
                {
                    vertices = new VertexPositionNormalTexture[vertexCount];
                }
            }

            public override void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                if (vertices == null)
                {
                    vertices = new VertexPositionNormalTexture[32];
                }
                if (vIndex == vertices.Count())
                {
                    Array.Resize(ref vertices, 2 * vertices.Count());
                }
                vertices[vIndex++] = new VertexPositionNormalTexture(position, normal, textureCoordinate);
            }

            public override void setToMesh(Mesh mesh)
            {
                base.setToMesh(mesh);
                if (vertices != null && vIndex > 0)
                {
                    mesh.VertexDeclaration = VertexPositionNormalTexture.VertexDeclaration;
                    mesh.VertexBuffer = new VertexBuffer(gd, typeof(VertexPositionNormalTexture), vIndex, BufferUsage.None);
                    mesh.VertexBuffer.SetData(vertices, 0, vIndex);
                    mesh.VertexCount = vIndex;
                }
            }
        }

        private class VertexPositionNormalTextureArrayBufferBuilder : VertexBufferBuilder
        {
            private VertexPositionNormalTextureArray[] vertices;

            public VertexPositionNormalTextureArrayBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, indexCount)
            {
                if (vertexCount > 0)
                {
                    vertices = new VertexPositionNormalTextureArray[vertexCount];
                }
            }

            public override void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                if (vertices == null)
                {
                    vertices = new VertexPositionNormalTextureArray[32];
                }
                if (vIndex == vertices.Count())
                {
                    Array.Resize(ref vertices, 2 * vertices.Count());
                }
                vertices[vIndex++] = new VertexPositionNormalTextureArray(position, normal, new Vector3(textureCoordinate, textureIndex));
            }

            public override void setToMesh(Mesh mesh)
            {
                base.setToMesh(mesh);
                if (vertices != null && vIndex > 0)
                {
                    mesh.VertexDeclaration = VertexPositionNormalTextureArray.VertexDeclaration;
                    mesh.VertexBuffer = new VertexBuffer(gd, typeof(VertexPositionNormalTextureArray), vIndex, BufferUsage.None);
                    mesh.VertexBuffer.SetData(vertices, 0, vIndex);
                    mesh.VertexCount = vIndex;
                }
            }
        }

        private class VertexPositionColorNormalTextureArrayBufferBuilder : VertexBufferBuilder
        {
            private VertexPositionColorNormalTextureArray[] vertices;

            public VertexPositionColorNormalTextureArrayBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, indexCount)
            {
                if (vertexCount > 0)
                {
                    vertices = new VertexPositionColorNormalTextureArray[vertexCount];
                }
            }

            public override void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                if (vertices == null)
                {
                    vertices = new VertexPositionColorNormalTextureArray[32];
                }
                if (vIndex == vertices.Count())
                {
                    Array.Resize(ref vertices, 2 * vertices.Count());
                }
                vertices[vIndex++] = new VertexPositionColorNormalTextureArray(position, color, normal, new Vector3(textureCoordinate, textureIndex));
            }

            public override void setToMesh(Mesh mesh)
            {
                base.setToMesh(mesh);
                if (vertices != null && vIndex > 0)
                {
                    mesh.VertexDeclaration = VertexPositionColorNormalTextureArray.VertexDeclaration;
                    mesh.VertexBuffer = new VertexBuffer(gd, typeof(VertexPositionColorNormalTextureArray), vIndex, BufferUsage.None);
                    mesh.VertexBuffer.SetData(vertices, 0, vIndex);
                    mesh.VertexCount = vIndex;
                }
            }
        }

        private class VoxelVertexBufferBuilder : VertexBufferBuilder
        {
            private VoxelVertex[] vertices;

            public VoxelVertexBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, indexCount)
            {
                if (vertexCount > 0)
                {
                    vertices = new VoxelVertex[vertexCount];
                }
            }

            public override void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                if (vertices == null)
                {
                    vertices = new VoxelVertex[32];
                }
                if (vIndex == vertices.Count())
                {
                    Array.Resize(ref vertices, 2 * vertices.Count());
                }
                vertices[vIndex++] = new VoxelVertex(position, normal, textureCoordinate, textureIndex, lightTextureIndex);
                //Console.WriteLine(vertices[vIndex-1].ToString());
            }

            public override void setToMesh(Mesh mesh)
            {
                base.setToMesh(mesh);
                if (vertices != null && vIndex > 0)
                {
                    mesh.VertexDeclaration = VoxelVertex.VertexDeclaration;
                    mesh.VertexBuffer = new VertexBuffer(gd, typeof(VoxelVertex), vIndex, BufferUsage.None);
                    mesh.VertexBuffer.SetData(vertices, 0, vIndex);
                    mesh.VertexCount = vIndex;
                }
            }
        }

        private class VertexPositionColorBufferBuilder : VertexBufferBuilder
        {
            private VertexPositionColor[] vertices;
            public VertexPositionColorBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, indexCount)
            {
                if (vertexCount > 0)
                {
                    vertices = new VertexPositionColor[vertexCount];
                }
            }

            public override void AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                if (vertices == null)
                {
                    vertices = new VertexPositionColor[32];
                }
                if (vIndex == vertices.Count())
                {
                    Array.Resize(ref vertices, 2 * vertices.Count());
                }
                vertices[vIndex++] = new VertexPositionColor(position, color);
            }

            public override void setToMesh(Mesh mesh)
            {
                base.setToMesh(mesh);
                if (vertices != null && vIndex > 0)
                {
                    mesh.VertexDeclaration = VertexPositionColor.VertexDeclaration;
                    mesh.VertexBuffer = new VertexBuffer(gd, typeof(VertexPositionColor), vIndex, BufferUsage.None);
                    mesh.VertexBuffer.SetData(vertices, 0, vIndex);
                    mesh.VertexCount = vIndex;
                }
            }
        }


    }
}


