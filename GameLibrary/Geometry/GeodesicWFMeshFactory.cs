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
    public class GeodesicWFMeshFactory : GeodesicMeshFactory
    {
        private Dictionary<Int64, int> edgeCache;

        public GeodesicWFMeshFactory(int depth)
            : base(depth)
        {
        }

        public GeodesicWFMeshFactory(int depth, Boolean facetted)
            : base(depth, facetted)
        {
        }

        public GeodesicWFMeshFactory(int depth, Boolean facetted, Boolean flat)
            : base(depth, facetted, flat)
        {
        }

        protected override Mesh generateMesh(GraphicsDevice gd, TriangleIndices[] faces)
        {
            edgeCache = new Dictionary<Int64, int>(edgesCount);
            VertexBufferBuilder<VertexPositionColor> builder = new VertexBufferBuilder<VertexPositionColor>(gd, vertices.Count(), edgesCount * 2);
            foreach (Vector3 vertex in vertices)
            {
                Vector3 n = Vector3.Normalize(vertex);
                builder.AddVertex(vertex, Color.White);
            }
            foreach (TriangleIndices tri in faces)
            {
                addEdge(builder, tri.v1, tri.v2);
                addEdge(builder, tri.v2, tri.v3);
                addEdge(builder, tri.v3, tri.v1);
            }
            Mesh mesh = new Mesh(PrimitiveType.LineList, faces.Count() * 3);
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.Sphere(Vector3.Zero, 1.0f);
            builder.SetToMesh(mesh);
            return mesh;
        }

        private void addEdge(VertexBufferBuilder<VertexPositionColor> builder, short p1, short p2)
        {
            // first check if we have it already
            Int64 key = IntegerUtil.createInt64Key(p1, p2);
            int ret;
            if (edgeCache.TryGetValue(key, out ret))
            {
                return;
            }
            edgeCache.Add(key, 1);
            bool firstIsSmaller = p1 < p2;
            short smallerIndex = firstIsSmaller ? p1 : p2;
            short greaterIndex = firstIsSmaller ? p2 : p1;
            builder.AddIndex(smallerIndex);
            builder.AddIndex(greaterIndex);
        }
    }
}
