using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;

namespace AsteroidGame.Geometry
{
    public class AsteroidMeshFactory : IMeshFactory
    {
        private int count;

        protected Vector3[] vertices;

        public AsteroidMeshFactory(int count)
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
            VertexBufferBuilder builder = VertexBufferBuilder.createVertexPositionColorBufferBuilder(gd, vertices.Count(), 0);
            foreach (Vector3 vertex in vertices)
            {
                builder.AddVertex(vertex, Vector3.Zero, Color.LightGray, Vector2.Zero);
            }
            Mesh mesh = new Mesh(PrimitiveType.LineStrip, count);
            builder.setToMesh(mesh);
            GameLibrary.SceneGraph.Bounding.BoundingSphere boundingSphere = new GameLibrary.SceneGraph.Bounding.BoundingSphere();
            boundingSphere.ComputeFromPoints(vertices);
            mesh.BoundingVolume = boundingSphere;
            return mesh;
        }

        private void generateGeometry()
        {
            Random random = new Random();

            //int index = 0;

            float increment = MathHelper.TwoPi / count;

            float minSeparation = increment / 3;

            float totalIncrement = 0;
            float angle = 0;

            angle = totalIncrement + increment * (float) random.NextDouble();
            totalIncrement += increment;


            float maxIncrement = MathHelper.TwoPi;
            if (angle < minSeparation)
            {
                maxIncrement -= minSeparation - angle;
            }

            double delta = 0.4;

            Vector3 position = new Vector3();
            for (int i = 0; i < count; i++)
            {
                //double angle = (MathHelper.TwoPi * (double) i) / (double) count;
                //angle += MathHelper.PiOver4 * (random.NextDouble() - 0.5);
                double length = 1.0 - delta + random.NextDouble() * delta;
                position.X = (float) (Math.Cos(angle) * length);
                position.Y = (float) (Math.Sin(angle) * length);
                vertices[i] = position;

                if (i < count - 1)
                {
                    float minIncrement = angle + minSeparation;
                    if (maxIncrement - minIncrement < minSeparation)
                    {
                        //angle = minIncrement - minSeparation;
                        Console.Out.WriteLine(i + " " + minIncrement + " " + maxIncrement);
                    }
                    else
                    {
                        angle = totalIncrement + increment * (float) random.NextDouble();
                        angle = MathHelper.Max(angle, minIncrement);
                        angle = MathHelper.Min(angle, maxIncrement);
                        totalIncrement += increment;
                    }
                }
                //if (showInternalVertices)
                //{
                //    indices[index++] = originIndex;
                //    indices[index++] = i;
                //}
            }
        }

    }
}
