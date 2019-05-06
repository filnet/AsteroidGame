using GameLibrary.Util;

namespace GameLibrary.Voxel
{
    public enum VoxelType { None, Earth, Grass, GrassyEarth, Rock, SnowyRock, Snow, Water, Glass, Test }

    public enum FaceType { None, Earth, Grass, Rock, Snow, Water, Glass, Test, Test_Left, Test_Right, Test_Bottom, Test_Top, Test_Back, Test_Front }

    public sealed class FaceInfo
    {
        private static readonly FaceInfo[] FACES = new FaceInfo[] {
            new FaceInfo(FaceType.None, false, false),
            new FaceInfo(FaceType.Earth),
            new FaceInfo(FaceType.Grass),
            new FaceInfo(FaceType.Rock),
            new FaceInfo(FaceType.Snow),
            new FaceInfo(FaceType.Water, true, false),
            new FaceInfo(FaceType.Glass, true, false),
            new FaceInfo(FaceType.Test),
            new FaceInfo(FaceType.Test_Left),
            new FaceInfo(FaceType.Test_Right),
            new FaceInfo(FaceType.Test_Bottom),
            new FaceInfo(FaceType.Test_Top),
            new FaceInfo(FaceType.Test_Back),
            new FaceInfo(FaceType.Test_Front),
        };

        public readonly FaceType Type;
        public readonly bool IsSolid;
        public readonly bool IsOpaque;
        public readonly int Rank;

        private FaceInfo(FaceType type) : this(type, true, true) { }

        private FaceInfo(FaceType type, bool solid, bool opaque)
        {
            Type = type;
            IsSolid = solid;
            IsOpaque = opaque;
            Rank = (IsSolid ? 1 : 0) + (IsOpaque ? 1 : 0);
        }

        #region Public static methods

        public static FaceInfo Get(FaceType type)
        {
            return FACES[(int)type];
        }

        public static int Compare(FaceType faceType1, FaceType faceType2)
        {
            return Get(faceType1).Rank - Get(faceType2).Rank;
        }

        #endregion

        public int TextureIndex()
        {
            return ((int)Type) - 1;
        }

    }

    // Note that it is possible to define exotic voxels that have a mix of opaque, transparent and missing faces
    // mixing opaque and transparent is discouraged as it will yield strange results or break
    // the only exotic voxel type atm is Water (has only a transprent top face, all other faces are missing)
    public sealed class VoxelInfo
    {
        private static readonly VoxelInfo[] VOXELS = new VoxelInfo[] {
            new VoxelInfo(VoxelType.None, FaceType.None, false, false),
            new VoxelInfo(VoxelType.Earth, FaceType.Earth, FaceType.Earth),
            new VoxelInfo(VoxelType.Grass, FaceType.Grass, FaceType.Grass),
            new VoxelInfo(VoxelType.GrassyEarth, FaceType.Grass, FaceType.Earth),
            new VoxelInfo(VoxelType.Rock, FaceType.Rock, FaceType.Rock),
            new VoxelInfo(VoxelType.SnowyRock, FaceType.Snow, FaceType.Rock),
            new VoxelInfo(VoxelType.Snow, FaceType.Snow, FaceType.Snow),
            new VoxelInfo(VoxelType.Water, FaceType.None, FaceType.None, FaceType.None, FaceType.Water, FaceType.None, FaceType.None, true, false),
            new VoxelInfo(VoxelType.Glass, FaceType.Glass, FaceType.Glass, FaceType.Glass, FaceType.Glass, FaceType.Glass, FaceType.Glass, true, false),
            new VoxelInfo(VoxelType.Test, FaceType.Test_Left, FaceType.Test_Right, FaceType.Test_Bottom, FaceType.Test_Top, FaceType.Test_Back, FaceType.Test_Front),
        };

        public readonly VoxelType Type;
        public readonly bool IsSolid;
        public readonly bool IsTransparent;
        public readonly bool IsOpaque;

        private readonly FaceInfo[] faces = new FaceInfo[6];

        private VoxelInfo(VoxelType type, FaceType face) : this(type, face, true, true) { }

        private VoxelInfo(VoxelType type, FaceType face, bool solid, bool transparent) : this(type, face, face, face, face, face, face, solid, transparent) { }

        private VoxelInfo(VoxelType type, FaceType top, FaceType other) : this(type, other, other, other, top, other, other, true, false) { }

        private VoxelInfo(VoxelType type, FaceType left, FaceType right, FaceType bottom, FaceType top, FaceType back, FaceType front)
            : this(type, left, right, bottom, top, back, front, true, true) { }

        private VoxelInfo(VoxelType type, FaceType left, FaceType right, FaceType bottom, FaceType top, FaceType back, FaceType front, bool solid, bool opaque)
        {
            Type = type;
            IsSolid = false;
            IsOpaque = false;
            IsTransparent = true;
            faces[(int)Direction.Left] = FaceInfo.Get(left);
            faces[(int)Direction.Right] = FaceInfo.Get(right);
            faces[(int)Direction.Bottom] = FaceInfo.Get(bottom);
            faces[(int)Direction.Top] = FaceInfo.Get(top);
            faces[(int)Direction.Back] = FaceInfo.Get(back);
            faces[(int)Direction.Front] = FaceInfo.Get(front);
            for (int i = 0; i < faces.Length; i++)
            {
                IsSolid |= faces[i].IsSolid;
                IsOpaque |= faces[i].IsOpaque;
                IsTransparent &= !faces[i].IsOpaque;
            }
        }

        #region Public static methods

        // TODO remove this method
        public static VoxelInfo Get(int type)
        {
            return VOXELS[type];
        }

        public static VoxelInfo Get(VoxelType type)
        {
            return VOXELS[(int)type];
        }

        public static FaceType GetFaceType(VoxelType type, Direction dir)
        {
            return VOXELS[(int)type].GetFaceType(dir);
        }

        #endregion

        public FaceType GetFaceType(Direction dir)
        {
            return IsSolid ? faces[(int)dir].Type : FaceType.None;
        }

        public bool HasFace(Direction dir)
        {
            return IsSolid && faces[(int)dir].IsSolid;
        }

        public bool HasOpaqueFace(Direction dir)
        {
            return IsSolid && faces[(int)dir].IsOpaque;
        }

        // TODO remove does not belong here
        public int TextureIndex(Direction dir)
        {
            return faces[(int)dir].TextureIndex();
        }

    }
}
