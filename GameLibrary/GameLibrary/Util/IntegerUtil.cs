using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameLibrary.Util
{
    public static class IntegerUtil
    {
        public static Int64 createInt64Key(int p1, int p2)
        {
            bool firstIsSmaller = p1 < p2;
            Int64 smallerIndex = firstIsSmaller ? p1 : p2;
            Int64 greaterIndex = firstIsSmaller ? p2 : p1;
            Int64 key = (smallerIndex << 32) + greaterIndex;
            return key;
        }


    }
}
