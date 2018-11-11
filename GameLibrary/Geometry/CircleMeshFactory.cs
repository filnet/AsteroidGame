using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;

namespace GameLibrary.Geometry
{
    public class CircleMeshFactory : IMeshFactory
    {
        private int count;

        protected Vector3[] vertices;

        public CircleMeshFactory(int count)
        {
            this.count = count;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            vertices = new Vector3[count + 1];
            generateGeometry();
            vertices[count] = vertices[0];
            Mesh mesh = generateMesh(gd);
            return mesh;
        }

        protected virtual Mesh generateMesh(GraphicsDevice gd)
        {
            VertexBufferBuilder<VertexPositionColor> builder = VertexBufferBuilder<VertexPositionColor>.createVertexPositionColorBufferBuilder(gd, vertices.Count(), 0);
            foreach (Vector3 vertex in vertices)
            {
                builder.AddVertex(vertex, Vector3.Zero, Color.White, Vector2.Zero);
            }
            Mesh mesh = new Mesh(PrimitiveType.LineStrip, count);
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingSphere(Vector3.Zero, 1.0f);
            builder.setToMesh(mesh);
            return mesh;
        }

        private void generateGeometry()
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 position = new Vector3();
                float angle = (360.0f * (float) i) / (float) count;
                position.X = (float) Math.Cos((double) MathHelper.ToRadians(angle));
                position.Y = (float) Math.Sin((double) MathHelper.ToRadians(angle));
                vertices[i] = position;
            }
        }

    }
}
