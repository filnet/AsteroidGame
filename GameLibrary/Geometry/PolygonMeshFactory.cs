using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;
using GameLibrary.Util;

namespace GameLibrary.Geometry
{
    public class PolygonMeshFactory : IMeshFactory
    {
        private Polygon poly;

        private int vertexCount;

        public PolygonMeshFactory(Polygon poly)
        {
            this.poly = poly;
            vertexCount = poly.Count;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            generateGeometry();
            Mesh mesh = generateMesh(gd);
            return mesh;
        }

        protected virtual Mesh generateMesh(GraphicsDevice gd)
        {
            VertexBufferBuilder<VertexPositionColor> builder = VertexBufferBuilder<VertexPositionColor>.createVertexPositionColorBufferBuilder(gd, vertexCount, vertexCount + 1);
            for (short i = 0; i < vertexCount; i++)
            {
                Vector3 v = new Vector3(poly[i], 0);
                builder.AddVertex(v, Vector3.Zero, Color.White, Vector2.Zero);
                builder.AddIndex(i);
            }
            builder.AddIndex(0);
            Mesh mesh = new Mesh(PrimitiveType.LineStrip, vertexCount);
            builder.SetToMesh(mesh);
            GameLibrary.SceneGraph.Bounding.BoundingSphere boundingSphere = new GameLibrary.SceneGraph.Bounding.BoundingSphere(2);
            //boundingSphere.ComputeFromPoints(vertices);
            mesh.BoundingVolume = boundingSphere;
            builder.SetToMesh(mesh);
            return mesh;
        }

        private void generateGeometry()
        {
        }

    }
}
