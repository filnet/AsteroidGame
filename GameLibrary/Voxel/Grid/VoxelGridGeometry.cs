using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GameLibrary.Voxel.Grid
{
    public class VoxelGridGeometry : GeometryNode
    {
        public VoxelGrid voxelGrid;

        public VoxelGridGeometry(String name, int chunkSize) : base(name)
        {
            voxelGrid = new VoxelGrid(chunkSize);
            voxelGrid.objectLoadedCallback = CB;
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

            //BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingBox(voxelGrid.Center, voxelGrid.HalfSize);

            voxelGrid.Initialize(gd);
        }

        public override void Dispose()
        {
            voxelGrid.Dispose();

            base.Dispose();
        }

        public override int VertexCount { get { throw new NotSupportedException(); } }
    }

}
