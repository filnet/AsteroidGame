using System;
using System.Diagnostics;

namespace GameLibrary.Util.Raster
{
    public static class LineRasterizerUtil
    {
        public interface LineRasterizer
        {
            bool HasNext();
            void Next(out Point3 p);
        }

        // Bresenham 3D 
        public static LineRasterizer CreateLineRasterizer(Point3 p1, Point3 p2)
        {
            // Bresenham 3D 
            int dx, dy, dz;
            int l, m, n;
            int x_inc, y_inc, z_inc;
            int dx2, dy2, dz2;

            dx = p2.X - p1.X;
            dy = p2.Y - p1.Y;
            dz = p2.Z - p1.Z;

            x_inc = (dx < 0) ? -1 : 1;
            l = Math.Abs(dx);
            y_inc = (dy < 0) ? -1 : 1;
            m = Math.Abs(dy);
            z_inc = (dz < 0) ? -1 : 1;
            n = Math.Abs(dz);

            dx2 = l << 1;
            dy2 = m << 1;
            dz2 = n << 1;

            if ((l >= m) && (l >= n))
            {
                return new LineRasterizerX(p1, l, dx2, dy2, dz2, x_inc, y_inc, z_inc);
            }
            else if ((m >= l) && (m >= n))
            {
                return new LineRasterizerY(p1, m, dx2, dy2, dz2, x_inc, y_inc, z_inc);
            }
            return new LineRasterizerZ(p1, n, dx2, dy2, dz2, x_inc, y_inc, z_inc);
        }


        internal struct LineRasterizerX : LineRasterizer
        {
            Point3 p;
            int l;
            int dx2, dy2, dz2;
            int x_inc, y_inc, z_inc;

            int i;
            int err_1, err_2;

            public LineRasterizerX(Point3 p, int l, int dx2, int dy2, int dz2, int x_inc, int y_inc, int z_inc)
            {
                this.p = p;
                this.l = l;
                this.dx2 = dx2;
                this.dy2 = dy2;
                this.dz2 = dz2;
                this.x_inc = x_inc;
                this.y_inc = y_inc;
                this.z_inc = z_inc;
                i = 0;
                err_1 = dy2 - l;
                err_2 = dz2 - l;
            }

            public bool HasNext()
            {
                return (i <= l);
            }

            public void Next(out Point3 point)
            {
                //Debug.Assert(i <= l);
                point = p;
                if (err_1 > 0)
                {
                    p.Y += y_inc;
                    err_1 -= dx2;
                }
                if (err_2 > 0)
                {
                    p.Z += z_inc;
                    err_2 -= dx2;
                }
                err_1 += dy2;
                err_2 += dz2;
                p.X += x_inc;
                i++;
            }
        }

        internal struct LineRasterizerY : LineRasterizer
        {
            Point3 p;
            int m;
            int dx2, dy2, dz2;
            int x_inc, y_inc, z_inc;

            int i;
            int err_1, err_2;

            public LineRasterizerY(Point3 p, int m, int dx2, int dy2, int dz2, int x_inc, int y_inc, int z_inc)
            {
                this.p = p;
                this.m = m;
                this.dx2 = dx2;
                this.dy2 = dy2;
                this.dz2 = dz2;
                this.x_inc = x_inc;
                this.y_inc = y_inc;
                this.z_inc = z_inc;
                i = 0;
                err_1 = dx2 - m;
                err_2 = dz2 - m;
            }

            public bool HasNext()
            {
                return (i <= m);
            }

            public void Next(out Point3 point)
            {
                //Debug.Assert(i <= m);
                point = p;
                if (err_1 > 0)
                {
                    p.X += x_inc;
                    err_1 -= dy2;
                }
                if (err_2 > 0)
                {
                    p.Z += z_inc;
                    err_2 -= dy2;
                }
                err_1 += dx2;
                err_2 += dz2;
                p.Y += y_inc;
                i++;
            }
        }

        internal struct LineRasterizerZ : LineRasterizer
        {
            Point3 p;
            int n;
            int dx2, dy2, dz2;
            int x_inc, y_inc, z_inc;

            int i;
            int err_1, err_2;

            public LineRasterizerZ(Point3 p, int n, int dx2, int dy2, int dz2, int x_inc, int y_inc, int z_inc)
            {
                this.p = p;
                this.n = n;
                this.dx2 = dx2;
                this.dy2 = dy2;
                this.dz2 = dz2;
                this.x_inc = x_inc;
                this.y_inc = y_inc;
                this.z_inc = z_inc;
                i = 0;
                err_1 = dy2 - n;
                err_2 = dx2 - n;
            }

            public bool HasNext()
            {
                return (i <= n);
            }

            public void Next(out Point3 point)
            {
                //Debug.Assert(i <= n);
                point = p;
                if (err_1 > 0)
                {
                    p.Y += y_inc;
                    err_1 -= dz2;
                }
                if (err_2 > 0)
                {
                    p.X += x_inc;
                    err_2 -= dz2;
                }
                err_1 += dy2;
                err_2 += dx2;
                p.Z += z_inc;
                i++;
            }
        }

    }
}
