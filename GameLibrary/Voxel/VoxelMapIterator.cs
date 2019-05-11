using GameLibrary.Util;
using GameLibrary.Voxel.Octree;

namespace GameLibrary.Voxel
{
    public abstract class VoxelMapIterator
    {
        protected VoxelMap map;

        protected int x0;
        protected int y0;
        protected int z0;

        protected int size;

        protected int x;
        protected int y;
        protected int z;
        protected ushort v;

        public int emptyVoxelsCount;
        public int voxelsCount;
        public int facesCount;

        public VoxelMapIterator()
        {
        }

        internal virtual void Begin(VoxelMap map)
        {
            this.map = map;
            size = map.Size();
            this.x0 = map.X0();
            this.y0 = map.Y0();
            this.z0 = map.Z0();
        }

        internal virtual void End()
        {
        }

        public int X { get { return x; } }
        public int Y { get { return y; } }
        public int Z { get { return z; } }
        public ushort V { get { return v; } }

        public int X0 { get { return x0; } }
        public int Y0 { get { return y0; } }
        public int Z0 { get { return z0; } }

        public abstract void Set(int x, int y, int z, ushort v);

        public abstract int Value();
        public abstract int Value(Direction dir);

        public abstract int Value(int x, int y, int z, Direction dir);

        public abstract VoxelMap GetNeighbourMap(Direction dir);
    }

    class SimpleVoxelMapIterator : VoxelMapIterator
    {
        public SimpleVoxelMapIterator()
        {
        }

        public override int Value()
        {
            return map.Get(x, y, z);
        }

        public override int Value(Direction dir)
        {
            return Value(x, y, z, dir);
        }

        public override int Value(int x, int y, int z, Direction dir)
        {
            VoxelOctree.DirData dirData = VoxelOctree.DIR_DATA[(int)dir];
            int nx = x + dirData.dX;
            int ny = y + dirData.dY;
            int nz = z + dirData.dZ;
            return map.GetSafe(nx, ny, nz);
        }

        public override void Set(int x, int y, int z, ushort v)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.v = v;
        }

        public override VoxelMap GetNeighbourMap(Direction dir)
        {
            return EmptyVoxelMap.INSTANCE;
        }

    }
}
