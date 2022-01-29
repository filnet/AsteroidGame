using GameLibrary.SceneGraph.Common;
using System.Runtime.InteropServices;

namespace GameLibrary.Voxel
{
    public enum VoxelChunkState
    {
        Null, Queued, Loading, Ready
    }

    public sealed class VoxelChunk
    {
        public ulong LocCode;
        public int X;
        public int Y;
        public int Z;

        public VoxelChunkState State;
        public VoxelMap VoxelMap;

        public SceneGraph.Bounding.Box BoundingBox;

        public Drawable OpaqueDrawable;
        public Drawable TransparentDrawable;

        // debug
        public Drawable ItemDrawable;

        public VoxelChunk()
        {
            State = VoxelChunkState.Null;
        }
    }
}
