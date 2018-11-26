using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.Voxel
{
    public enum VoxelType { Air, Dirt, Grass, GrassyDirt, Rock, SnowyRock, Snow, Water, Test }

    public enum FaceType { Earth, Grass, Rock, Snow, Water, Test, TestLeft, TestRight, TestBottom, TestTop, TestBack, TestFront, None }

    //    [Flags]
    //    public enum Face { Left, Right, Bottom, Top, Back, Front }

    public sealed class TileInfo
    {
        public static readonly TileInfo[] TILES = new TileInfo[] {
            new TileInfo(VoxelType.Air, false, true),
            new TileInfo(VoxelType.Dirt, FaceType.Earth, FaceType.Earth),
            new TileInfo(VoxelType.Grass, FaceType.Grass, FaceType.Grass),
            new TileInfo(VoxelType.GrassyDirt, FaceType.Grass, FaceType.Earth),
            new TileInfo(VoxelType.Rock, FaceType.Rock, FaceType.Rock),
            new TileInfo(VoxelType.SnowyRock, FaceType.Snow, FaceType.Rock),
            new TileInfo(VoxelType.Snow, FaceType.Snow, FaceType.Snow),
            new TileInfo(VoxelType.Water, FaceType.None, FaceType.None, FaceType.None, FaceType.Water, FaceType.None, FaceType.None, true, true),
            new TileInfo(VoxelType.Test, FaceType.TestLeft, FaceType.TestRight, FaceType.TestBottom, FaceType.TestTop, FaceType.TestBack, FaceType.TestFront)
        };

        //public readonly String Name;
        public readonly VoxelType Type;
        public readonly bool IsSolid;
        public readonly bool IsTransparent;
        public readonly bool IsOpaque;

        FaceType[] faces = new FaceType[6];

        public TileInfo(VoxelType type, bool solid, bool transparent) : this(type, FaceType.Test, solid, transparent) { }

        public TileInfo(VoxelType type, FaceType face) : this(type, face, true, false) { }

        public TileInfo(VoxelType type, FaceType face, bool solid, bool transparent) : this(type, face, face, face, face, face, face, solid, transparent) { }

        public TileInfo(VoxelType type, FaceType top, FaceType other) : this(type, other, other, other, top, other, other, true, false) { }

        public TileInfo(VoxelType type, FaceType left, FaceType right, FaceType bottom, FaceType top, FaceType back, FaceType front)
            : this(type, left, right, bottom, top, back, front, true, false)
        {
        }

        public TileInfo(VoxelType type, FaceType left, FaceType right, FaceType bottom, FaceType top, FaceType back, FaceType front, bool solid, bool transparent)
        {
            Type = type;
            IsSolid = solid;
            IsTransparent = transparent;
            IsOpaque = solid && !transparent;
            faces[(int)Direction.Left] = left;
            faces[(int)Direction.Right] = right;
            faces[(int)Direction.Bottom] = bottom;
            faces[(int)Direction.Top] = top;
            faces[(int)Direction.Back] = back;
            faces[(int)Direction.Front] = front;
        }

        public int TextureIndex(Direction face)
        {
            return (int)faces[(int)face];
        }

        public bool HasFace(Direction face)
        {
            return IsSolid && faces[(int)face] != FaceType.None;
        }

        public bool HasOpaqueFace(Direction face)
        {
            return IsOpaque && faces[(int)face] != FaceType.None;
        }

    }
}
