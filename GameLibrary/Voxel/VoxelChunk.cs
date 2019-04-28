using GameLibrary.SceneGraph.Common;

namespace GameLibrary.Voxel
{
    public enum VoxelChunkState
    {
        Null, Queued, Loading, Ready
    }

    public sealed class VoxelChunk
    {
        public VoxelChunkState State;
        public VoxelMap VoxelMap;

        public SceneGraph.Bounding.Box BoundingBox;

        public Drawable NodeDrawable;
        public Drawable Drawable;
        public Drawable TransparentDrawable;

        public VoxelChunk()
        {
            State = VoxelChunkState.Null;
        }
    }
}
