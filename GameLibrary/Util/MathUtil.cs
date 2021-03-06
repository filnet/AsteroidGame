﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameLibrary.Component.Util
{
    public static class MathUtil
    {
        public static int Pow(int x, int pow)
        {
            int ret = 1;
            while (pow != 0)
            {
                if ((pow & 1) == 1)
                    ret *= x;
                x *= x;
                pow >>= 1;
            }
            return ret;
        }

        public static float Lerp(float u, float v, float s)
        {
            return u * (1 - s) + v * s;
        }

        public static double Lerp(double u, double v, double s)
        {
            return u * (1 - s) + v * s;
        }

    }
}
