﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;

namespace GameLibrary.Geometry
{
    public class FrustumMeshFactory : IMeshFactory
    {
        protected readonly Vector3[] vertices;

        //private Frustum Frustum;

        private VertexBufferBuilder<VertexPositionColor> builder;

        public FrustumMeshFactory(Vector3[] vertices)
        {
            this.vertices = vertices;
        }

        public FrustumMeshFactory(SceneGraph.Bounding.Frustum frustum)
        {
            vertices = frustum.GetCorners();
            //this.frustum = frustum;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            //vertices = Frustum.GetCorners();

            int lineCount = 12;
            builder = VertexBufferBuilder<VertexPositionColor>.createVertexPositionColorBufferBuilder(gd, vertices.Count(), 2 * lineCount);

            generate();

            Mesh mesh = new Mesh(PrimitiveType.LineList, lineCount);
            //mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingSphere(new Vector3(0, 0, 0), (float) Math.Sqrt(2) / 2);
            builder.SetToMesh(mesh);

            return mesh;
        }

        protected virtual void generate()
        {
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
        }

    }
}
