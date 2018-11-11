using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameLibrary.Util
{
    public static class IntegerUtil
    {
        public static Int64 createInt64Key(short p1, short p2)
        {
            bool firstIsSmaller = p1 < p2;
            Int64 smallerIndex = firstIsSmaller ? p1 : p2;
            Int64 greaterIndex = firstIsSmaller ? p2 : p1;
            Int64 key = (smallerIndex << 16) + greaterIndex;
            return key;
        }

        public static Int64 createInt64Key(int p1, int p2)
        {
            bool firstIsSmaller = p1 < p2;
            Int64 smallerIndex = firstIsSmaller ? p1 : p2;
            Int64 greaterIndex = firstIsSmaller ? p2 : p1;
            Int64 key = (smallerIndex << 32) + greaterIndex;
            return key;
        }
    }

    public static class IntExtensions
    {
        public static bool IsBitSet(this int v, int index)
        {
            checkIndex(index);
            return (v & (1 << index)) != 0;
        }

        public static int SetBit(this int v, int index)
        {
            checkIndex(index);
            return (int) (v | (1 << index));
        }

        public static int SetBit(this int v, int index, bool b)
        {
            checkIndex(index);
            if (b)
            {
                return v.SetBit(index);                  
            }
            else
            {
                return v.UnsetBit(index);
            }
        }

        public static int UnsetBit(this int v, int index)
        {
            checkIndex(index);
            return (int) (v & ~(1 << index));
        }

        public static int ToggleBit(this int v, int index)
        {
            checkIndex(index);
            return (int) (v ^ (1 << index));
        }

        public static string ToBinaryString(this int v)
        {
            return Convert.ToString(v, 2).PadLeft(32, '0');
        }

        private static void checkIndex(int index)
        {
            if (index < 0 || index > 31)
            {
                throw new ArgumentOutOfRangeException("index", "Index must be in the range of 0-31.");
            }
        }

    }
}
