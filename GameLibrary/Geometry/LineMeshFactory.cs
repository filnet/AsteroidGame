using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;

namespace GameLibrary.Geometry
{
    public class LineMeshFactory : IMeshFactory
    {
        protected Vector3[] vertices;
        protected bool close;

        public LineMeshFactory()
        {
        }

        public LineMeshFactory(Vector3[] vertices) : this(vertices, false)
        {
        }

        public LineMeshFactory(Vector3[] vertices, bool close)
        {
            this.vertices = vertices;
            this.close = close;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            if (vertices == null)
            {
                vertices = new Vector3[2];
                generateGeometry();
            }
            Mesh mesh = generateMesh(gd);
            return mesh;
        }

        protected virtual Mesh generateMesh(GraphicsDevice gd)
        {
            int vertexCount = close ? vertices.Count() + 1 : vertices.Count();
            VertexBufferBuilder<VertexPositionColor> builder = VertexBufferBuilder<VertexPositionColor>.createVertexPositionColorBufferBuilder(gd, vertexCount, 0);
            foreach (Vector3 vertex in vertices)
            {
                builder.AddVertex(vertex, Vector3.Zero, Color.White, Vector2.Zero);
            }
            if (close)
            {
                builder.AddVertex(vertices[0], Vector3.Zero, Color.White, Vector2.Zero);
            }
            Mesh mesh = new Mesh(PrimitiveType.LineStrip, vertexCount - 1);
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingSphere(new Vector3(0.5f, 0, 0), 0.5f);
            builder.SetToMesh(mesh);
            return mesh;
        }

        private void generateGeometry()
        {
            vertices[0] = Vector3.Zero;
            vertices[1] = Vector3.Right;
        }

    }
}
