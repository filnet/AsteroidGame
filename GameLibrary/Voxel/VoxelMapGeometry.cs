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
    public class VoxelMapGeometry : GeometryNode
    {
        int size;
        VoxelMap voxelMap;

        public VoxelMapGeometry(String name, int size)
            : base(name)
        {
            //number_of_vertices = verticesCount;
            this.size = size;
        }

        public override void Initialize(GraphicsDevice gd)
        {
            //voxelMap = new SimpleVoxelMap(size);
            voxelMap = new FunctionVoxelMap(size);
            base.Initialize(gd);
        }

        public override void Dispose()
        {
        }

        public void Visit(Voxel.Visitor visitor)
        {
            voxelMap.Visit(visitor);
        }

    }

}
