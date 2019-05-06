using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace GameLibrary.Geometry.Common
{
    public static class VertexBufferBuilderExtensions
    {
        // VertexPositionColor

        public static int AddVertex(this VertexBufferBuilder<VertexPositionColor> builder, Vector3 position, Color color)
        {
            return builder.AddVertex(new VertexPositionColor(position, color));
        }

        public static Vector3 VertexPosition(VertexPositionColor vertex)
        {
            return vertex.Position;
        }

        // VertexPositionNormalTexture

        public static int AddVertex(this VertexBufferBuilder<VertexPositionNormalTexture> builder, Vector3 position, Vector3 normal, Vector2 textureCoordinate)
        {
            return builder.AddVertex(new VertexPositionNormalTexture(position, normal, textureCoordinate));
        }

        public static Vector3 VertexPosition(VertexPositionNormalTexture vertex)
        {
            return vertex.Position;
        }

        // VertexPositionNormalTextureArray

        public static int AddVertex(this VertexBufferBuilder<VertexPositionNormalTextureArray> builder, Vector3 position, Vector3 normal, Vector3 textureCoordinate)
        {
            return builder.AddVertex(new VertexPositionNormalTextureArray(position, normal, textureCoordinate));
        }

        public static Vector3 VertexPosition(VertexPositionNormalTextureArray vertex)
        {
            return vertex.Position;
        }

        // VertexPositionColorNormalTexture

        public static int AddVertex(this VertexBufferBuilder<VertexPositionColorNormalTexture> builder, Vector3 position, Color color, Vector3 normal, Vector2 textureCoordinate)
        {
            return builder.AddVertex(new VertexPositionColorNormalTexture(position, color, normal, textureCoordinate));
        }

        public static Vector3 VertexPosition(VertexPositionColorNormalTexture vertex)
        {
            return vertex.Position;
        }

        // VertexPositionColorNormalTextureArray

        public static int AddVertex(this VertexBufferBuilder<VertexPositionColorNormalTextureArray> builder, Vector3 position, Color color, Vector3 normal, Vector3 textureCoordinate)
        {
            return builder.AddVertex(new VertexPositionColorNormalTextureArray(position, color, normal, textureCoordinate));
        }

        public static Vector3 VertexPosition(VertexPositionColorNormalTextureArray vertex)
        {
            return vertex.Position;
        }
    }

    public class VertexBufferBuilder<T> where T : struct, IVertexType
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

        public delegate Vector3 VertexPosition(T vertex);

        // TODO move out of here...
        public void ExtractBoundingBox(SceneGraph.Bounding.Box box, VertexPosition vertexPosition)
        {
            Vector3 min = new Vector3(float.MaxValue);
            Vector3 max = new Vector3(float.MinValue);
            for (int i = 0; i < vertexCount; i++)
            {
                T vertex = vertices[i];
                Vector3 position = vertexPosition(vertex);
                max.X = Math.Max(max.X, position.X);
                max.Y = Math.Max(max.Y, position.Y);
                max.Z = Math.Max(max.Z, position.Z);
                min.X = Math.Min(min.X, position.X);
                min.Y = Math.Min(min.Y, position.Y);
                min.Z = Math.Min(min.Z, position.Z);
            }
            SceneGraph.Bounding.Box.CreateFromMinMax(ref min, ref max, box);
        }

        public int AddVertex(T vertex)
        {
            ensureVertexCapacity();
            int index = vertexCount;
            vertices[index] = vertex;
            vertexCount++;
            return index;
        }

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

    }
}
