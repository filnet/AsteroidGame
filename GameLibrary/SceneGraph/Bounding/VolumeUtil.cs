using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameLibrary
{
    public static class VolumeUtil
    {
        public delegate Vector2 ProjectToScreen(ref Vector3 vector);

        // see https://pdfs.semanticscholar.org/1f59/8266e387cf367702d16acf5a4e02cc72cb99.pdf
        // or http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.43.1845&rep=rep1&type=pdf
        // 1st column is the number of vertices, then up to 6 vertices
        public static readonly int[] HULL_LOOKUP_TABLE = new int[] {
            0, 0, 0, 0, 0, 0, 0, //  0 - inside
            4, 0, 4, 7, 3, 0, 0, //  1 - left
            4, 1, 2, 6, 5, 0, 0, //  2 - right
            0, 0, 0, 0, 0, 0, 0, //  3 -
            4, 0, 1, 5, 4, 0, 0, //  4 - bottom
            6, 0, 1, 5, 4, 7, 3, //  5 - bottom, left
            6, 0, 1, 2, 6, 5, 4, //  6 - bottom, right
            0, 0, 0, 0, 0, 0, 0, //  7 -
            4, 2, 3, 7, 6, 0, 0, //  8 - top
            6, 4, 7, 6, 2, 3, 0, //  9 - top, left
            6, 2, 3, 7, 6, 5, 1, // 10 - top, right
            0, 0, 0, 0, 0, 0, 0, // 11 -
            0, 0, 0, 0, 0, 0, 0, // 12 -
            0, 0, 0, 0, 0, 0, 0, // 13 -
            0, 0, 0, 0, 0, 0, 0, // 14 -
            0, 0, 0, 0, 0, 0, 0, // 15 -
            4, 0, 3, 2, 1, 0, 0, // 16 - front
            6, 0, 4, 7, 3, 2, 1, // 17 - front, left
            6, 0, 3, 2, 6, 5, 1, // 18 - front, right
            0, 0, 0, 0, 0, 0, 0, // 19 -
            6, 0, 3, 2, 1, 5, 4, // 20 - front, bottom
            6, 1, 5, 4, 7, 3, 2, // 21 - front, bottom, left
            6, 0, 3, 2, 6, 5, 4, // 22 - front, bottom, right
            6, 3, 2, 6, 5, 4, 7, // 23 - front, left, right, bottom (frustum)
            6, 0, 3, 7, 6, 2, 1, // 24 - front, top
            6, 0, 4, 7, 6, 2, 1, // 25 - front, top, left
            6, 0, 3, 7, 6, 5, 1, // 26 - front, top, right
            6, 1, 0, 4, 7, 6, 5, // 27 - front, left, right, top (frustum)
            0, 0, 0, 0, 0, 0, 0, // 28 -
            6, 2, 1, 5, 4, 7, 6, // 29 - front, left, top, bottom (frustum)
            6, 0, 3, 7, 6, 5, 4, // 30 - front, right, top, bottom (frustum)
            4, 4, 5, 6, 7, 0, 0, // 31 - front, left, right, top, bottom (frustum)
            4, 4, 5, 6, 7, 0, 0, // 32 - back
            6, 4, 5, 6, 7, 3, 0, // 33 - back, left
            6, 1, 2, 6, 7, 4, 5, // 34 - back, right
            0, 0, 0, 0, 0, 0, 0, // 35 -
            6, 0, 1, 5, 6, 7, 4, // 36 - back, bottom
            6, 0, 1, 5, 6, 7, 3, // 37 - back, bottom, left
            6, 0, 1, 2, 6, 7, 4, // 38 - back, bottom, right
            0, 0, 0, 0, 0, 0, 0, // 39 -
            6, 2, 3, 7, 4, 5, 6, // 40 - back, top
            6, 0, 4, 5, 6, 2, 3, // 41 - back, top, left
            6, 1, 2, 3, 7, 4, 5, // 42 - back, top, right
        };

        public static readonly Vector3[] BB_HULL_VERTICES = new Vector3[] {
                new Vector3(-1, -1, +1),
                new Vector3(+1, -1, +1),
                new Vector3(+1, +1, +1),
                new Vector3(-1, +1, +1),
                new Vector3(-1, -1, -1),
                new Vector3(+1, -1, -1),
                new Vector3(+1, +1, -1),
                new Vector3(-1, +1, -1),
            };

    }

}
