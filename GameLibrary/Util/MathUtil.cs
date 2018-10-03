using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameLibrary.Component.Util
{
    public static class MathUtil
    {
        public static int power(int n, int exponent)
        {
            int factor = n;
            int result = 1;
            while (exponent != 0)
            {
                if (exponent % 2 != 0)
                {
                    result *= factor;
                    exponent -= 1;
                }
                else
                {
                    factor *= factor;
                    exponent /= 2;
                }
            }
            return result;
        }

    }
}
