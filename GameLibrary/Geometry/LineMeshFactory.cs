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

        public LineMeshFactory()
        {
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            vertices = new Vector3[2];
            generateGeometry();
            Mesh mesh = generateMesh(gd);
            return mesh;
        }

        protected virtual Mesh generateMesh(GraphicsDevice gd)
        {
            VertexBufferBuilder builder = VertexBufferBuilder.createVertexPositionColorBufferBuilder(gd, vertices.Count(), 0);
            foreach (Vector3 vertex in vertices)
            {
                builder.AddVertex(vertex, Vector3.Zero, Color.White, Vector2.Zero);
            }
            Mesh mesh = new Mesh(PrimitiveType.LineList, vertices.Count());
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingSphere(new Vector3(0.5f, 0, 0), 0.5f);
            builder.setToMesh(mesh);
            return mesh;
        }

        private void generateGeometry()
        {
            vertices[0] = Vector3.Zero;
            vertices[1] = Vector3.Right;
        }

    }
}
