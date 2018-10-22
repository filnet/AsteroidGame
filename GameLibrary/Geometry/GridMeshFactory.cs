using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;

namespace GameLibrary.Geometry
{
    public class GridMeshFactory : IMeshFactory
    {
        private int count;

        private int lineCount;
        private int verticeCount;

        protected Vector3[] vertices;

        public GridMeshFactory(int count)
        {
            this.count = count;
            // multiply by two take into the X and Y axis
            lineCount = 2 * (2 * count + 1);
            verticeCount = 2 * lineCount;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            //((number_of_vertices + 1) * 2 * 2) + ((number_of_vertices + 1) * 2 * 2)
            vertices = new Vector3[verticeCount];
            generateGeometry();
            Mesh mesh = generateMesh(gd);
            return mesh;
        }

        protected virtual Mesh generateMesh(GraphicsDevice gd)
        {
            VertexBufferBuilder builder = VertexBufferBuilder.createVertexPositionColorBufferBuilder(gd, vertices.Count(), 0);
            foreach (Vector3 vertex in vertices)
            {
                builder.AddVertex(vertex, Vector3.Zero, Color.LightGray, Vector2.Zero);
            }
            Mesh mesh = new Mesh(PrimitiveType.LineList, lineCount);
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingSphere(Vector3.Zero, (float) Math.Sqrt(3 * 3 + 3 * 3));
            builder.setToMesh(mesh);
            return mesh;
        }

        private void generateGeometry()
        {
            int v = 0;
            for (int i = -count; i <= count; i++)
            {
                Vector3 position1 = new Vector3();
                position1.X = (float) i;
                position1.Y = (float) -count;
                vertices[v++] = position1;
                Vector3 position2 = new Vector3();
                position2.X = (float) i;
                position2.Y = (float) count;
                vertices[v++] = position2;
            }
            for (int i = -count; i <= count; i++)
            {
                Vector3 position1 = new Vector3();
                position1.X = (float) -count;
                position1.Y = (float) i;
                vertices[v++] = position1;
                Vector3 position2 = new Vector3();
                position2.X = (float) count;
                position2.Y = (float) i;
                vertices[v++] = position2;
            }
        }

    }
}
