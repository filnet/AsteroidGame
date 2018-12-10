using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace GameLibrary.Geometry.Common
{
    public abstract class VertexBufferBuilder<T> where T : struct, IVertexType
    {
        protected GraphicsDevice gd;

        private T[] vertices;
        protected int vIndex;

        protected int[] indices;
        protected int iIndex;

        public int VertexCount
        {
            get { return vIndex; }
        }

        public VertexBufferBuilder(GraphicsDevice gd) : this(gd, 0, 0)
        {
        }

        public VertexBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            this.gd = gd;
            vIndex = 0;
            iIndex = 0;
            if (vertexCount > 0)
            {
                vertices = createVertexArray(vertexCount);
            }
            if (indexCount > 0)
            {
                indices = createIndexArray(indexCount);
            }
        }

        public int AddVertex(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
        {
            return AddVertex(position, normal, Color.White, textureCoordinate);
        }

        public int AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate)
        {
            return AddVertex(position, normal, color, textureCoordinate, 0, 0);
        }

        public int AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex)
        {
            return AddVertex(position, normal, color, textureCoordinate, textureIndex, 0);
        }

        public int AddVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
        {
            ensureVertexCapacity();
            int index = vIndex;
            vertices[index] = createVertex(position, normal, color, textureCoordinate, textureIndex, lightTextureIndex);
            vIndex++;
            return index;
        }

        protected abstract VertexDeclaration getVertexDeclaration();

        protected abstract T createVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex);

        public void AddIndex(int index)
        {
            ensureIndexCapacity();
            indices[iIndex++] = index;
        }

        public void Reset()
        {
            vIndex = 0;
            iIndex = 0;
        }

        public virtual void SetToMesh(Mesh mesh)
        {
            if (vertices != null && vIndex > 0)
            {
                //mesh.VertexDeclaration = getVertexDeclaration();
                mesh.VertexBuffer = new VertexBuffer(gd, typeof(T), vIndex, BufferUsage.WriteOnly);
                mesh.VertexBuffer.SetData(vertices, 0, vIndex);
            }
            if (indices != null && iIndex > 0)
            {
                mesh.IndexBuffer = new IndexBuffer(gd, typeof(int), iIndex, BufferUsage.WriteOnly);
                mesh.IndexBuffer.SetData(indices);
            }
        }

        private void ensureVertexCapacity()
        {
            if (vertices == null)
            {
                vertices = createVertexArray(512);
            }
            if (vIndex == vertices.Count())
            {
                //Console.WriteLine("Resizing vertex array... {0}", vertices.Count());
                Array.Resize(ref vertices, 2 * vertices.Count());
            }
        }

        private T[] createVertexArray(int size)
        {
            return new T[size];
        }

        private void ensureIndexCapacity()
        {
            if (indices == null)
            {
                indices = createIndexArray(512);
            }
            if (iIndex == indices.Count())
            {
                //Console.WriteLine("Resizing index array... {0}", vertices.Count());
                Array.Resize(ref indices, 2 * indices.Count());
            }
        }

        private int[] createIndexArray(int size)
        {
            return new int[size];
        }

        // VertexPositionNormalTexture

        public static VertexBufferBuilder<VertexPositionNormalTexture> createVertexPositionNormalTextureBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionNormalTextureBufferBuilder(gd, vertexCount, indexCount);
        }

        private class VertexPositionNormalTextureBufferBuilder : VertexBufferBuilder<VertexPositionNormalTexture>
        {
            public VertexPositionNormalTextureBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, vertexCount, indexCount)
            {
            }

            protected override VertexDeclaration getVertexDeclaration()
            {
                return VertexPositionNormalTexture.VertexDeclaration;
            }

            protected override VertexPositionNormalTexture createVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                return new VertexPositionNormalTexture(position, normal, textureCoordinate);
            }
        }

        // VertexPositionNormalTextureArray

        public static VertexBufferBuilder<VertexPositionNormalTextureArray> createVertexPositionNormalTextureArrayBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionNormalTextureArrayBufferBuilder(gd, vertexCount, indexCount);
        }

        private class VertexPositionNormalTextureArrayBufferBuilder : VertexBufferBuilder<VertexPositionNormalTextureArray>
        {
            public VertexPositionNormalTextureArrayBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, vertexCount, indexCount)
            {
            }

            protected override VertexDeclaration getVertexDeclaration()
            {
                return VertexPositionNormalTextureArray.VertexDeclaration;
            }

            protected override VertexPositionNormalTextureArray createVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                return new VertexPositionNormalTextureArray(position, normal, new Vector3(textureCoordinate, textureIndex));
            }
        }

        // VertexPositionColorNormalTextureArray

        public static VertexBufferBuilder<VertexPositionColorNormalTextureArray> createVertexPositionColorNormalTextureArrayBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionColorNormalTextureArrayBufferBuilder(gd, vertexCount, indexCount);
        }

        private class VertexPositionColorNormalTextureArrayBufferBuilder : VertexBufferBuilder<VertexPositionColorNormalTextureArray>
        {
            public VertexPositionColorNormalTextureArrayBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, vertexCount, indexCount)
            {
            }

            protected override VertexDeclaration getVertexDeclaration()
            {
                return VertexPositionColorNormalTextureArray.VertexDeclaration;
            }

            protected override VertexPositionColorNormalTextureArray createVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                return new VertexPositionColorNormalTextureArray(position, color, normal, new Vector3(textureCoordinate, textureIndex));
            }

        }

        // VertexPositionColor

        public static VertexBufferBuilder<VertexPositionColor> createVertexPositionColorBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionColorBufferBuilder(gd, vertexCount, indexCount);
        }

        private class VertexPositionColorBufferBuilder : VertexBufferBuilder<VertexPositionColor>
        {
            public VertexPositionColorBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, vertexCount, indexCount)
            {
            }

            protected override VertexDeclaration getVertexDeclaration()
            {
                return VertexPositionColor.VertexDeclaration;
            }

            protected override VertexPositionColor createVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                return new VertexPositionColor(position, color);
            }

        }

        // VoxelVertex

        public static VertexBufferBuilder<VoxelVertex> createVoxelVertexBufferBuilder(GraphicsDevice gd)
        {
            return new VoxelVertexBufferBuilder(gd);
        }

        private class VoxelVertexBufferBuilder : VertexBufferBuilder<VoxelVertex>
        {
            public VoxelVertexBufferBuilder(GraphicsDevice gd)
                : base(gd)
            {
            }

            protected override VertexDeclaration getVertexDeclaration()
            {
                return VoxelVertex.VertexDeclaration;
            }

            protected override VoxelVertex createVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                return new VoxelVertex(position, normal, textureCoordinate, textureIndex, lightTextureIndex);
            }
        }

    }
}


