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
        protected int vertexCount;

        protected int[] indices;
        protected int indexCount;

        private bool fixedSize;

        public int VertexCount
        {
            get { return vertexCount; }
        }

        public VertexBufferBuilder(GraphicsDevice gd) : this(gd, 0, 0)
        {
            fixedSize = false;
        }

        public VertexBufferBuilder(GraphicsDevice gd, int expectedVertexCount, int expectedIndexCount)
        {
            this.gd = gd;
            vertexCount = 0;
            indexCount = 0;
            if (expectedVertexCount > 0)
            {
                fixedSize = true;
                vertices = createVertexArray(expectedVertexCount);
            }
            if (expectedIndexCount > 0)
            {
                fixedSize = true;
                indices = createIndexArray(expectedIndexCount);
            }
        }

        public void ExtractBoundingBox(SceneGraph.Bounding.Box box)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            for (int i = 0; i < vertexCount; i++)
            {
                T vertex = vertices[i];
                Vector3 position = VertexPosition(vertex);
                max.X = Math.Max(max.X, position.X);
                max.Y = Math.Max(max.Y, position.Y);
                max.Z = Math.Max(max.Z, position.Z);
                min.X = Math.Min(min.X, position.X);
                min.Y = Math.Min(min.Y, position.Y);
                min.Z = Math.Min(min.Z, position.Z);
            }
            SceneGraph.Bounding.Box.CreateFromMinMax(ref min, ref max, box);
        }

        protected virtual Vector3 VertexPosition(T vertex)
        {
            throw new NotImplementedException();
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
            int index = vertexCount;
            vertices[index] = createVertex(position, normal, color, textureCoordinate, textureIndex, lightTextureIndex);
            vertexCount++;
            return index;
        }

        protected abstract VertexDeclaration getVertexDeclaration();

        protected abstract T createVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex);

        public void AddIndex(int index)
        {
            ensureIndexCapacity();
            indices[indexCount++] = index;
        }

        public void Reset()
        {
            vertexCount = 0;
            indexCount = 0;
        }

        public virtual void SetToMesh(Mesh mesh)
        {
            if (vertexCount > 0 && vertices != null)
            {
                if (mesh.IsDynamic)
                {
                    DynamicVertexBuffer dynamicBuffer = mesh.VertexBuffer as DynamicVertexBuffer;
                    if (dynamicBuffer == null)
                    {
                        dynamicBuffer = new DynamicVertexBuffer(gd, typeof(T), vertexCount, BufferUsage.WriteOnly);
                        mesh.VertexBuffer = dynamicBuffer;
                    }
                    dynamicBuffer.SetData(vertices, 0, vertexCount, SetDataOptions.Discard);
                }
                else
                {
                    if (mesh.VertexBuffer != null)
                    {
                        throw new InvalidOperationException();
                    }
                    mesh.VertexBuffer = new VertexBuffer(gd, typeof(T), vertexCount, BufferUsage.WriteOnly);
                    mesh.VertexBuffer.SetData(vertices, 0, vertexCount);
                }
            }
            if (indexCount > 0 && indices != null)
            {
                if (mesh.IsDynamic)
                {
                    DynamicIndexBuffer dynamicBuffer = mesh.IndexBuffer as DynamicIndexBuffer;
                    if (dynamicBuffer == null)
                    {
                        dynamicBuffer = new DynamicIndexBuffer(gd, typeof(int), indexCount, BufferUsage.WriteOnly);
                        mesh.IndexBuffer = dynamicBuffer;
                    }
                    dynamicBuffer.SetData(indices, 0, indexCount, SetDataOptions.Discard);
                }
                else
                {
                    if (mesh.IndexBuffer != null)
                    {
                        throw new InvalidOperationException();
                    }
                    mesh.IndexBuffer = new IndexBuffer(gd, typeof(int), indexCount, BufferUsage.WriteOnly);
                    mesh.IndexBuffer.SetData(indices, 0, indexCount);
                }
            }
        }

        private void ensureVertexCapacity()
        {
            if (vertices == null)
            {
                vertices = createVertexArray(512);
            }
            if (vertexCount == vertices.Count())
            {
                if (fixedSize)
                {
                    throw new InvalidOperationException();
                }
                int newSize = 2 * vertices.Count();
                Console.WriteLine("Resizing vertex array... {0}", newSize);
                Array.Resize(ref vertices, newSize);
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
            if (indexCount == indices.Count())
            {
                if (fixedSize)
                {
                    throw new InvalidOperationException();
                }
                int newSize = 2 * indices.Count();
                Console.WriteLine("Resizing index array... {0}", newSize);
                Array.Resize(ref indices, newSize);
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

            protected override Vector3 VertexPosition(VertexPositionNormalTexture vertex)
            {
                return vertex.Position;
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

            protected override Vector3 VertexPosition(VertexPositionNormalTextureArray vertex)
            {
                return vertex.Position;
            }
        }

        // VertexPositionColorNormalTexture

        public static VertexBufferBuilder<VertexPositionColorNormalTexture> createVertexPositionColorNormalTextureBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
        {
            return new VertexPositionColorNormalTextureBufferBuilder(gd, vertexCount, indexCount);
        }

        private class VertexPositionColorNormalTextureBufferBuilder : VertexBufferBuilder<VertexPositionColorNormalTexture>
        {
            public VertexPositionColorNormalTextureBufferBuilder(GraphicsDevice gd, int vertexCount, int indexCount)
                : base(gd, vertexCount, indexCount)
            {
            }

            protected override VertexDeclaration getVertexDeclaration()
            {
                return VertexPositionColorNormalTexture.VertexDeclaration;
            }

            protected override VertexPositionColorNormalTexture createVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
            {
                return new VertexPositionColorNormalTexture(position, color, normal, textureCoordinate);
            }

            protected override Vector3 VertexPosition(VertexPositionColorNormalTexture vertex)
            {
                return vertex.Position;
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

            protected override Vector3 VertexPosition(VertexPositionColorNormalTextureArray vertex)
            {
                return vertex.Position;
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

            protected override Vector3 VertexPosition(VertexPositionColor vertex)
            {
                return vertex.Position;
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

            protected override Vector3 VertexPosition(VoxelVertex vertex)
            {
                return vertex.Position;
            }
        }

    }
}



