using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

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
            VertexBufferBuilder<VertexPositionColor> builder = new VertexBufferBuilder<VertexPositionColor>(gd, vertices.Count(), 0);
            foreach (Vector3 vertex in vertices)
            {
                builder.AddVertex(vertex, Color.White);
            }
            Mesh mesh = new Mesh(PrimitiveType.LineStrip, count);
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.Sphere(Vector3.Zero, 1.0f);
            builder.SetToMesh(mesh);
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
