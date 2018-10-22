using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;

namespace GameLibrary.Geometry
{
    public class FrustrumMeshFactory : IMeshFactory
    {
        protected Vector3[] vertices;

        private BoundingFrustum boundingFrustum;

        public FrustrumMeshFactory(BoundingFrustum boundingFrustum)
        {
            this.boundingFrustum = boundingFrustum;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            vertices = boundingFrustum.GetCorners();
            Mesh mesh = generateMesh(gd);
            return mesh;
        }

        protected virtual Mesh generateMesh(GraphicsDevice gd)
        {
            int lineCount = 12; 
            VertexBufferBuilder builder = VertexBufferBuilder.createVertexPositionColorBufferBuilder(gd, vertices.Count(), 2 * lineCount);
            foreach (Vector3 vertex in vertices)
            {
                builder.AddVertex(vertex, Vector3.Zero, Color.White, Vector2.Zero);
            }
            // front
            builder.AddIndex(0);
            builder.AddIndex(1);
            builder.AddIndex(1);
            builder.AddIndex(2);
            builder.AddIndex(2);
            builder.AddIndex(3);
            builder.AddIndex(3);
            builder.AddIndex(0);
            // back
            builder.AddIndex(4);
            builder.AddIndex(5);
            builder.AddIndex(5);
            builder.AddIndex(6);
            builder.AddIndex(6);
            builder.AddIndex(7);
            builder.AddIndex(7);
            builder.AddIndex(4);
            // ???
            builder.AddIndex(0);
            builder.AddIndex(4);
            // ???
            builder.AddIndex(1);
            builder.AddIndex(5);
            // ???
            builder.AddIndex(2);
            builder.AddIndex(6);
            // ???
            builder.AddIndex(3);
            builder.AddIndex(7);

            Mesh mesh = new Mesh(PrimitiveType.LineList, lineCount);
            //mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingSphere(new Vector3(0, 0, 0), (float) Math.Sqrt(2) / 2);
            builder.setToMesh(mesh);
            return mesh;
        }

    }
}
