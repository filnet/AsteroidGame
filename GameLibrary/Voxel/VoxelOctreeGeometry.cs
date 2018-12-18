﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Voxel;
using GameLibrary.Geometry.Common;
using GameLibrary.Geometry;
using GameLibrary.SceneGraph.Bounding;
using static GameLibrary.SceneGraph.Scene;

namespace GameLibrary.Voxel
{
    public class VoxelOctreeGeometry : GeometryNode
    {
        public VoxelOctree voxelOctree;

        public VoxelOctreeGeometry(String name, int size, int chunkSize) : base(name)
        {
            voxelOctree = new VoxelOctree(size, chunkSize);
            voxelOctree.objectLoadedCallback = CB;

        }

        private void CB()
        {
            // TODO not thread safe
            setDirty(DirtyFlag.Structure);
            setParentDirty(DirtyFlag.ChildStructure);
        }

        public override void Initialize(GraphicsDevice gd)
        {
            base.Initialize(gd);

            //BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingBox(voxelOctree.Center, voxelOctree.HalfSize);

            voxelOctree.Initialize(gd);
        }

        public override void Dispose()
        {
            voxelOctree.Dispose();

            base.Dispose();
        }

        public override int VertexCount { get { throw new NotSupportedException(); } }
    }

}
