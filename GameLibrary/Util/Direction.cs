using System;
using System.Diagnostics;
using System.Linq;

namespace GameLibrary.Util
{
    public enum Direction
    {
        // 6-connected
        Left, Right, Bottom, Top, Back, Front,
        // 18-connected
        BottomLeft, BottomRight, BottomFront, BottomBack,
        LeftFront, RightFront, LeftBack, RightBack,
        TopLeft, TopRight, TopFront, TopBack,
        // 26-connected
        BottomLeftFront, BottomRightFront, BottomLeftBack, BottomRightBack,
        TopLeftFront, TopRightFront, TopLeftBack, TopRightBack,
    }

    public static class Mask
    {
        public static readonly int X = 0b001;
        public static readonly int Y = 0b100;
        public static readonly int Z = 0b010;
        public static readonly int XY = X | Y;
        public static readonly int YZ = Y | Z;
        public static readonly int XZ = X | Z;
        public static readonly int XYZ = X | Y | Z;

        public static readonly int LEFT = 1 << 5;
        public static readonly int RIGHT = 1 << 4;
        public static readonly int BOTTOM = 1 << 3;
        public static readonly int TOP = 1 << 2;
        public static readonly int BACK = 1 << 1; // !!!
        public static readonly int FRONT = 1 << 0; // !!!
    }

    public static class DirectionConstants
    {
        private static readonly int LEFT = 0b000;
        private static readonly int RIGHT = 0b001;
        private static readonly int BOTTOM = 0b000;
        private static readonly int TOP = 0b100;
        private static readonly int BACK = 0b010;
        private static readonly int FRONT = 0b000;

        public sealed class DirData
        {
            public readonly Direction dir;
            public readonly int mask;
            public readonly int value;
            public readonly int dX;
            public readonly int dY;
            public readonly int dZ;
            public readonly int lookupIndex;

            public DirData(Direction dir, int mask, int value)
            {
                this.dir = dir;
                this.mask = mask;
                this.value = value;
                // x, y, z deltas
                // note again the Z inversion !!!
                dX = ((mask & Mask.X) != 0) ? ((value & Mask.X) != 0) ? +1 : -1 : 0;
                dY = ((mask & Mask.Y) != 0) ? ((value & Mask.Y) != 0) ? +1 : -1 : 0;
                dZ = ((mask & Mask.Z) != 0) ? ((value & Mask.Z) != 0) ? -1 : +1 : 0;
                // lookup index for octrees
                // index is the bit sequence LEFT RIGHT BOTTOM TOP BACK FRONT
                // some lookup indices are invalid (0b111111 for example)
                // note yet again the Z inversion !!!
                lookupIndex = ((mask & Mask.X) != 0) ? ((value & Mask.X) != 0) ? 0b01 : 0b10 : 0;
                lookupIndex = (lookupIndex << 2) | (((mask & Mask.Y) != 0) ? ((value & Mask.Y) != 0) ? 0b01 : 0b10 : 0);
                lookupIndex = (lookupIndex << 2) | (((mask & Mask.Z) != 0) ? ((value & Mask.Z) != 0) ? 0b10 : 0b01 : 0);
            }

            public static DirData Get(Direction dir)
            {
                return DIR_DATA[(int)dir];
            }

            public static int ComputeLookupIndex(int x, int y, int z, int x0, int y0, int z0, int size)
            {
                int lookupIndex = 0;
                if (x < x0)
                {
                    lookupIndex |= Mask.LEFT;
                }
                else if (x >= x0 + size)
                {
                    lookupIndex |= Mask.RIGHT;
                }
                if (y < y0)
                {
                    lookupIndex |= Mask.BOTTOM;
                }
                else if (y >= y0 + size)
                {
                    lookupIndex |= Mask.TOP;
                }
                if (z < z0)
                {
                    lookupIndex |= Mask.BACK;
                }
                else if (z >= z0 + size)
                {
                    lookupIndex |= Mask.FRONT;
                }
                return lookupIndex;
            }

            public static Direction LookupDirection(int lookupIndex)
            {
                Debug.Assert(lookupIndex >= 0 && lookupIndex < DIR_LOOKUP_TABLE.Length);
                return DIR_LOOKUP_TABLE[lookupIndex];
            }
        }

        private static readonly DirData[] DIR_DATA = new DirData[] {
            // 6-connected
            new DirData(Direction.Left, Mask.X, LEFT),
            new DirData(Direction.Right, Mask.X, RIGHT),
            new DirData(Direction.Bottom, Mask.Y, BOTTOM),
            new DirData(Direction.Top, Mask.Y, TOP),
            new DirData(Direction.Back, Mask.Z, BACK),
            new DirData(Direction.Front, Mask.Z, FRONT),
            // 18-connected
            new DirData(Direction.BottomLeft, Mask.XY, BOTTOM | LEFT),
            new DirData(Direction.BottomRight, Mask.XY, BOTTOM | RIGHT),
            new DirData(Direction.BottomFront, Mask.YZ, BOTTOM | FRONT),
            new DirData(Direction.BottomBack, Mask.YZ, BOTTOM | BACK),
            new DirData(Direction.LeftFront, Mask.XZ, LEFT | FRONT),
            new DirData(Direction.RightFront, Mask.XZ, RIGHT | FRONT),
            new DirData(Direction.LeftBack, Mask.XZ, LEFT | BACK),
            new DirData(Direction.RightBack, Mask.XZ, RIGHT | BACK),
            new DirData(Direction.TopLeft, Mask.XY, TOP | LEFT),
            new DirData(Direction.TopRight, Mask.XY, TOP | RIGHT),
            new DirData(Direction.TopFront, Mask.YZ, TOP | FRONT),
            new DirData(Direction.TopBack, Mask.YZ, TOP | BACK),
            // 26-connected
            new DirData(Direction.BottomLeftFront, Mask.XYZ, BOTTOM | LEFT | FRONT),
            new DirData(Direction.BottomRightFront, Mask.XYZ, BOTTOM | RIGHT | FRONT),
            new DirData(Direction.BottomLeftBack, Mask.XYZ, BOTTOM | LEFT | BACK),
            new DirData(Direction.BottomRightBack, Mask.XYZ, BOTTOM | RIGHT | BACK),
            new DirData(Direction.TopLeftFront, Mask.XYZ, TOP | LEFT | FRONT),
            new DirData(Direction.TopRightFront, Mask.XYZ, TOP | RIGHT | FRONT),
            new DirData(Direction.TopLeftBack, Mask.XYZ, TOP | LEFT | BACK),
            new DirData(Direction.TopRightBack, Mask.XYZ, TOP | RIGHT | BACK),
        };

        // gives the Direction by lookup index
        // lookup index is the bit sequence LEFT RIGHT BOTTOM TOP BACK FRONT
        // some lookup indices are invalid (0b111111 for example)
        private static readonly Direction[] DIR_LOOKUP_TABLE = ComputeDirectionLookupTable();

        private static Direction[] ComputeDirectionLookupTable()
        {
            Direction[] directionLookupTable = new Direction[64];
            foreach (Direction dir in Enum.GetValues(typeof(Direction)).Cast<Direction>())
            {
                int lookupIndex = DirData.Get(dir).lookupIndex;
                directionLookupTable[lookupIndex] = dir;
            }
            return directionLookupTable;
        }
    }
}
