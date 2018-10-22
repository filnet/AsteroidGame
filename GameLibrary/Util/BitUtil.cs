using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GameLibrary.Util
{
    class BitUtil
    {

        private const ulong DeBruijnSequence = 0x022fdd63cc95386dUL;

        // table taken from http://chessprogramming.wikispaces.com/De+Bruijn+Sequence+Generator
        static readonly int[] MultiplyDeBruijnBitPosition = new int[64] {
            0, // change to 1 if you want bitSize(0) = 1
            1,  2, 53,  3,  7, 54, 27, 4, 38, 41,  8, 34, 55, 48, 28,
            62,  5, 39, 46, 44, 42, 22,  9, 24, 35, 59, 56, 49, 18, 29, 11,
            63, 52,  6, 26, 37, 40, 33, 47, 61, 45, 43, 21, 23, 58, 17, 10,
            51, 25, 36, 32, 60, 20, 57, 16, 50, 31, 19, 15, 30, 14, 13, 12
        };

        // see https://stackoverflow.com/questions/21888140/de-bruijn-algorithm-binary-digit-count-64bits-c-sharp
        public static int BitScanReverse(ulong v)
        {
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v |= v >> 32;
            // at this point you could also use popcount to find the number of set bits.
            // That might well be faster than a lookup table because you prevent a 
            // potential cache miss
            //if (v == (ulong)-1) return 64;
            v++;
            return MultiplyDeBruijnBitPosition[(ulong)(v * DeBruijnSequence) >> 58];
        }
        // see https://graphics.stanford.edu/~seander/bithacks.html#IntegerLogDeBruijn
        static readonly int[] MultiplyDeBruijnBitPosition2 = new int[] {
            0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
            31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
        };

        public static int Log2(int v)
        {
            // TODO assert if not a power of two !
            int r = MultiplyDeBruijnBitPosition2[((uint) v * 0x077CB531U) >> 27];
            return r;
        }

    }

}
