using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;

namespace GameLibrary.Geometry
{
    public class SquareMeshFactory : IMeshFactory
    {
        protected Vector3[] vertices;

        public SquareMeshFactory()
        {
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            vertices = new Vector3[4];
            generateGeometry();
            Mesh mesh = generateMesh(gd);
            return mesh;
        }

        protected virtual Mesh generateMesh(GraphicsDevice gd)
        {
            VertexBufferBuilder<VertexPositionColor> builder = VertexBufferBuilder<VertexPositionColor>.createVertexPositionColorBufferBuilder(gd, vertices.Count(), vertices.Count() + 1);
            short i = 0;
            foreach (Vector3 vertex in vertices)
            {
                builder.AddVertex(vertex, Vector3.Zero, Color.White, Vector2.Zero);
                builder.AddIndex(i++);
            }
            builder.AddIndex(0);
            Mesh mesh = new Mesh(PrimitiveType.LineStrip, vertices.Count());
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingSphere(new Vector3(0, 0, 0), (float)Math.Sqrt(2) / 2);
            builder.SetToMesh(mesh);
            return mesh;
        }

        private void generateGeometry()
        {
            vertices[0] = new Vector3(0.5f, 0.5f, 0);
            vertices[1] = new Vector3(0.5f, -0.5f, 0);
            vertices[2] = new Vector3(-0.5f, -0.5f, 0);
            vertices[3] = new Vector3(-0.5f, 0.5f, 0);
        }

    }
}
