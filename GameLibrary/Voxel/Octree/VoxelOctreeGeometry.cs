using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GameLibrary.Voxel.Octree
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
            // FIXME not thread safe
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
