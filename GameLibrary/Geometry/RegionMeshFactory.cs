using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Component.Util;

namespace GameLibrary.Geometry
{
    public class RegionMeshFactory : IMeshFactory
    {
        private SceneGraph.Bounding.Region region;

        private VertexBufferBuilder<VertexPositionColor> builder;

        public RegionMeshFactory(SceneGraph.Bounding.Region region)
        {
            this.region = region;
            //vertices = region.s();
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            builder = new VertexBufferBuilder<VertexPositionColor>(gd, 0, 0);

            generate();

            // FIXME LineCount is actually the number of vertices (so LineCount / 2)
            Mesh mesh = new Mesh(PrimitiveType.LineList, region.LineCount / 2);
            //mesh.BoundingVolume = new SceneGraph.Bounding.Sphere(new Vector3(0, 0, 0), (float) Math.Sqrt(2) / 2);
            builder.SetToMesh(mesh);

            return mesh;
        }

        protected virtual void generate()
        {
            for (int i = 0; i < region.LineCount; i++)
            {
                Vector3 vertex = region.lines[i];
                builder.AddVertex(vertex, Color.White);
            }
        }

    }
}
