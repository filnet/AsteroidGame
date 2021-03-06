﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;
using GameLibrary.Util;

namespace GameLibrary.Geometry
{
    public class CubeWFMeshFactory : IMeshFactory
    {
        private float size;

        private static float DEFAULT_SIZE = 0.5773502692f; // 1 over the square root of 3

        public CubeWFMeshFactory() : this(DEFAULT_SIZE)
        {
        }

        public CubeWFMeshFactory(float size)
        {
            this.size = size;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            int verticeCount = 8;
            int lineCount = 12;

            VertexBufferBuilder<VertexPositionColor> builder = new VertexBufferBuilder<VertexPositionColor>(gd, verticeCount, lineCount * 2);

            float d = size;

            // top face
            Vector3 topLeftFront = new Vector3(-d, d, d);
            Vector3 topLeftBack = new Vector3(-d, d, -d);
            Vector3 topRightFront = new Vector3(d, d, d);
            Vector3 topRightBack = new Vector3(d, d, -d);

            // bottom face
            Vector3 bottomLeftFront = new Vector3(-d, -d, d);
            Vector3 bottomLeftBack = new Vector3(-d, -d, -d);
            Vector3 bottomRightFront = new Vector3(d, -d, d);
            Vector3 bottomRightBack = new Vector3(d, -d, -d);

            // front face
            builder.AddVertex(topLeftFront, Color.White);
            builder.AddVertex(bottomLeftFront, Color.White);
            builder.AddVertex(bottomRightFront, Color.White);
            builder.AddVertex(topRightFront, Color.White);

            // back face
            builder.AddVertex(topLeftBack, Color.White);
            builder.AddVertex(topRightBack, Color.White);
            builder.AddVertex(bottomRightBack, Color.White);
            builder.AddVertex(bottomLeftBack, Color.White);

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
            builder.AddIndex(3);
            builder.AddIndex(5);
            // ???
            builder.AddIndex(2);
            builder.AddIndex(6);
            // ???
            builder.AddIndex(1);
            builder.AddIndex(7);


            Mesh mesh = new Mesh(PrimitiveType.LineList, lineCount);
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.Sphere(Vector3.Zero, 2 * d);
            builder.SetToMesh(mesh);
            return mesh;
        }

    }
}
