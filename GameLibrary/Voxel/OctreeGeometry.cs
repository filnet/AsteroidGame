using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Voxel;

namespace GameLibrary
{
    public class OctreeGeometry : GeometryNode
    {
        private int halfSize;
        public Octree<GeometryNode> Octree;

        public OctreeGeometry(String name, int size)
            : base(name)
        {
            //number_of_vertices = verticesCount;
            halfSize = size / 2;
            Octree = new Octree<GeometryNode>(Vector3.Zero, new Vector3(halfSize));
        }

        public override void Initialize()
        {
            //Octree = new SimpleOctree(size);
            BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingBox(Vector3.Zero, new Vector3(halfSize));
            base.Initialize();
        }

        public override void Dispose()
        {
        }

    }

}
