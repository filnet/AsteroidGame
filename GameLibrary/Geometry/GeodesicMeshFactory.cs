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
    public class GeodesicMeshFactory : IMeshFactory
    {
        protected struct TriangleIndices
        {
            public short v1;
            public short v2;
            public short v3;

            public TriangleIndices(short v1, short v2, short v3)
            {
                this.v1 = v1;
                this.v2 = v2;
                this.v3 = v3;
            }
        }

        private int recursionLevel;
        protected Boolean flat;
        protected Boolean facetted;


        private int frequency;
        private int frequency2;

        protected int vertexCount;
        protected int facesCount;
        protected int edgesCount;

        protected Vector3[] vertices;
        private TriangleIndices[] faces;

        private short index;

        private Dictionary<Int64, short> middlePointIndexCache;

        public GeodesicMeshFactory(int depth)
            : this(depth, false, false)
        {
        }

        public GeodesicMeshFactory(int depth, Boolean facetted)
            : this(depth, facetted, false)
        {
        }

        public GeodesicMeshFactory(int depth, Boolean facetted, Boolean flat)
        {
            this.recursionLevel = depth;
            this.facetted = flat || facetted;
            this.flat = flat;
        }

        public Mesh CreateMesh(GraphicsDevice gd)
        {
            frequency = MathUtil.Pow(2, recursionLevel);
            frequency2 = frequency * frequency;

            vertexCount = 10 * frequency2 + 2;
            facesCount = 20 * frequency2;
            edgesCount = 30 * frequency2;

            vertices = new Vector3[vertexCount];

            middlePointIndexCache = new Dictionary<long, short>(vertexCount);
            index = 0;

            Console.WriteLine("Generating geodesic: vertices = {0}, faces = {1}, edges = {2}", vertexCount, facesCount, edgesCount);
            generateGeometry();

            Mesh mesh = generateMesh(gd, faces);
            return mesh;
        }

        protected virtual Mesh generateMesh(GraphicsDevice gd, TriangleIndices[] faces)
        {
            Mesh mesh;
            VertexBufferBuilder<VertexPositionNormalTexture> builder;
            if (!facetted)
            {
                builder = VertexBufferBuilder<VertexPositionNormalTexture>.createVertexPositionNormalTextureBufferBuilder(gd, vertices.Count(), faces.Count() * 3);
                foreach (Vector3 vertex in vertices)
                {
                    Vector3 n = Vector3.Normalize(vertex);
                    builder.AddVertex(vertex, n, Color.White, Vector2.Zero);
                }
                foreach (TriangleIndices tri in faces)
                {
                    builder.AddIndex(tri.v1);
                    builder.AddIndex(tri.v2);
                    builder.AddIndex(tri.v3);
                }
                mesh = new Mesh(PrimitiveType.TriangleList, faces.Count());
            }
            else
            {
                builder = VertexBufferBuilder<VertexPositionNormalTexture>.createVertexPositionNormalTextureBufferBuilder(gd, faces.Count() * 3, 0);
                foreach (TriangleIndices tri in faces)
                {
                    Vector3 v1 = vertices[tri.v1];
                    Vector3 v2 = vertices[tri.v2];
                    Vector3 v3 = vertices[tri.v3];
                    Vector3 n = Vector3.Normalize(Vector3.Cross(v2 - v1, v3 - v1));
                    builder.AddVertex(v1, n, Color.White, Vector2.Zero);
                    builder.AddVertex(v2, n, Color.White, Vector2.Zero);
                    builder.AddVertex(v3, n, Color.White, Vector2.Zero);
                }
                mesh = new Mesh(PrimitiveType.TriangleList, faces.Count());
            }
            mesh.BoundingVolume = new GameLibrary.SceneGraph.Bounding.Sphere(Vector3.Zero, 1.0f);
            builder.SetToMesh(mesh);
            return mesh;
        }

        // icosahedron
        //
        // V = 10 * u2 + 2
        // F = 20 * u2
        // E = 30 * u2
        //
        // frequency 0  vertexCount= 12    faceCount= 20    edgeCount= 30
        // frequency 1  vertexCount= 42    faceCount= 80    edgeCount= 120
        // frequency 2  vertexCount= 162   faceCount= 320   edgeCount= 480
        // frequency 3  vertexCount= 642   faceCount= 1280  edgeCount= 1920
        //

        // Faces: 4, 16, 64, 256, 1024
        // Edges: 6, 24, 96, 384, 1536
        // Vertices: 4, 10, 34, 130, 514
        private void generateGeometry()
        {
            // initialize triangles
            faces = createIcosahedron();
            int currentFacesCount = faces.Count();

            // refine triangles
            for (int i = 0; i < recursionLevel; i++)
            {
                currentFacesCount *= 4;
                TriangleIndices[] faces2 = new TriangleIndices[currentFacesCount];
                int f = 0;
                foreach (TriangleIndices tri in faces)
                {
                    // replace triangle by 4 triangles
                    short a = getMiddlePoint(tri.v1, tri.v2);
                    short b = getMiddlePoint(tri.v2, tri.v3);
                    short c = getMiddlePoint(tri.v3, tri.v1);

                    faces2[f++] = new TriangleIndices(a, b, c);
                    faces2[f++] = new TriangleIndices(tri.v1, a, c);
                    faces2[f++] = new TriangleIndices(tri.v2, b, a);
                    faces2[f++] = new TriangleIndices(tri.v3, c, b);
                }
                faces = faces2;
            }
        }

        // add vertex to mesh, fix position to be on unit sphere, return index
        private short addVertex(Vector3 p)
        {
            if (!flat)
            {
                p.Normalize();
            }
            vertices[index] = p;
            return index++;
        }

        // return index of point in the middle of p1 and p2
        private short getMiddlePoint(short p1, short p2)
        {
            // first check if we have it already
            Int64 key = IntegerUtil.createInt64Key(p1, p2);
            short ret;
            if (middlePointIndexCache.TryGetValue(key, out ret))
            {
                return ret;
            }

            // not in cache, calculate it
            Vector3 v1 = vertices[p1];
            Vector3 v2 = vertices[p2];
            Vector3 middle = (v1 + v2) / 2;

            // add vertex makes sure point is on unit sphere
            short index = addVertex(middle);

            // store it, return index
            middlePointIndexCache.Add(key, index);
            return index;
        }

        private TriangleIndices[] createIcosahedron()
        {
            float t = (float)((1.0 + Math.Sqrt(5.0)) / 2.0);

            // create 12 vertices of a icosahedron
            addVertex(Vector3.Normalize(new Vector3(-1, t, 0)));
            addVertex(Vector3.Normalize(new Vector3(1, t, 0)));
            addVertex(Vector3.Normalize(new Vector3(-1, -t, 0)));
            addVertex(Vector3.Normalize(new Vector3(1, -t, 0)));

            addVertex(Vector3.Normalize(new Vector3(0, -1, t)));
            addVertex(Vector3.Normalize(new Vector3(0, 1, t)));
            addVertex(Vector3.Normalize(new Vector3(0, -1, -t)));
            addVertex(Vector3.Normalize(new Vector3(0, 1, -t)));

            addVertex(Vector3.Normalize(new Vector3(t, 0, -1)));
            addVertex(Vector3.Normalize(new Vector3(t, 0, 1)));
            addVertex(Vector3.Normalize(new Vector3(-t, 0, -1)));
            addVertex(Vector3.Normalize(new Vector3(-t, 0, 1)));

            // create 20 triangles of the icosahedron
            TriangleIndices[] faces = {
            // 5 faces around point 0
            new TriangleIndices(0, 11, 5),
            new TriangleIndices(0, 5, 1),
            new TriangleIndices(0, 1, 7),
            new TriangleIndices(0, 7, 10),
            new TriangleIndices(0, 10, 11),
            // 5 adjacent faces
            new TriangleIndices(1, 5, 9),
            new TriangleIndices(5, 11, 4),
            new TriangleIndices(11, 10, 2),
            new TriangleIndices(10, 7, 6),
            new TriangleIndices(7, 1, 8),
            // 5 faces around point 3
            new TriangleIndices(3, 9, 4),
            new TriangleIndices(3, 4, 2),
            new TriangleIndices(3, 2, 6),
            new TriangleIndices(3, 6, 8),
            new TriangleIndices(3, 8, 9),
            // 5 adjacent faces
            new TriangleIndices(4, 9, 5),
            new TriangleIndices(2, 4, 11),
            new TriangleIndices(6, 2, 10),
            new TriangleIndices(8, 6, 7),
            new TriangleIndices(9, 8, 1),
        };

            return faces;
        }

        private TriangleIndices[] createHexahedron()
        {
            float d = (float)(1.0 / Math.Sqrt(3));

            // 0 topLeftFront 
            addVertex(new Vector3(-d, d, d));
            // 1 bottomLeftFront
            addVertex(new Vector3(-d, -d, d));
            // 2 topRightFront
            addVertex(new Vector3(d, d, d));
            // 3 bottomRightFront
            addVertex(new Vector3(d, -d, d));
            // 4 topLeftBack
            addVertex(new Vector3(-d, d, -d));
            // 5 topRightBack
            addVertex(new Vector3(d, d, -d));
            // 6 bottomLeftBack
            addVertex(new Vector3(-d, -d, -d));
            // 7 bottomRightBack
            addVertex(new Vector3(d, -d, -d));

            // create 12 triangles of the hexahedron
            TriangleIndices[] faces = {
            // front face
            new TriangleIndices(0, 1, 2),
            new TriangleIndices(3, 2, 1),
            // back face
            new TriangleIndices(5, 7, 4),
            new TriangleIndices(6, 4, 7),
            // top face
            new TriangleIndices(4, 0, 5),
            new TriangleIndices(2, 5, 0),
            // bottom face
            new TriangleIndices(1, 6, 3),
            new TriangleIndices(7, 3, 6),
            // left face
            new TriangleIndices(4, 6, 0),
            new TriangleIndices(1, 0, 6),
            // right face
            new TriangleIndices(2, 3, 5),
            new TriangleIndices(7, 5, 3),
        };

            return faces;
        }

    }
}
