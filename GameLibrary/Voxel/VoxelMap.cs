using GameLibrary.Util;
using System;

namespace GameLibrary.Voxel
{
    public interface Visitor
    {
        bool Begin(int size);
        bool Visit(VoxelMapIterator ite);
        bool End();
    }

    public interface VoxelMap
    {
        int Size();

        bool IsEmpty();

        int X0();
        int Y0();
        int Z0();

        int Get(int x, int y, int z);
        int GetSafe(int x, int y, int z);

        void Set(int x, int y, int z, int v);
        void SetSafe(int x, int y, int z, int v);

        void Visit(Visitor visitor, VoxelMapIterator ite);
    }

    class EmptyVoxelMap : VoxelMap
    {
        public EmptyVoxelMap()
        {
        }

        public int X0() { return 0; }
        public int Y0() { return 0; }
        public int Z0() { return 0; }


        public bool IsEmpty()
        {
            return true;
        }

        public int Size()
        {
            return 0;
        }

        public int Get(int x, int y, int z)
        {
            return 0;
        }

        public int GetSafe(int x, int y, int z)
        {
            return 0;
        }

        public void Set(int x, int y, int z, int v)
        {
            // TODO assert if called
        }

        public void SetSafe(int x, int y, int z, int v)
        {
            // TODO assert if called
        }

        public void Visit(Visitor visitor, VoxelMapIterator ite)
        {

        }
    }

    abstract class AbstractVoxelMap : VoxelMap
    {
        public static readonly VoxelMap EMPTY_VOXEL_MAP = new EmptyVoxelMap();

        protected readonly int size;

        protected readonly int x0;
        protected readonly int y0;
        protected readonly int z0;

        public static bool Debug = false;

        public AbstractVoxelMap(int size, int x0, int y0, int z0)
        {
            this.size = size;
            this.x0 = x0;
            this.y0 = y0;
            this.z0 = z0;
        }

        public int X0() { return x0; }
        public int Y0() { return y0; }
        public int Z0() { return z0; }

        public int Size()
        {
            return size;
        }

        public virtual bool IsEmpty()
        {
            return (size == 0);
        }

        public abstract int Get(int x, int y, int z);

        public int GetSafe(int x, int y, int z)
        {
            if (x >= x0 && x < x0 + size && y >= y0 && y < y0 + size && z >= z0 && z < z0 + size)
            {
                return Get(x, y, z);
            }
            return 0;
        }

        public virtual void Set(int x, int y, int z, int v)
        {
            throw new Exception("Unsupported operation");
        }

        public void SetSafe(int x, int y, int z, int v)
        {
            if (x >= x0 && x < x0 + size && y >= y0 && y < y0 + size && z >= z0 && z < z0 + size)
            {
                Set(x, y, z, v);
            }
        }

        public void Visit(Visitor visitor, VoxelMapIterator ite)
        {
            bool abort = !visitor.Begin(size);
            if (abort)
            {
                visitor.End();
                return;
            }

            // TODO do front to back (to benefit from depth culling)
            // do back face culling
            // transparency...
            for (int z = z0; z < z0 + size; z++)
            {
                for (int y = y0; y < y0 + size; y++)
                {
                    ite.Set(x0, y, z);
                    for (int x = x0; x < x0 + size; x++)
                    {
                        //int v = ite.Value();
                        abort = !visitor.Visit(ite);
                        if (abort) break;
                        /*if (x < size - 1)*/
                        ite.TranslateX();
                    }
                    if (abort) break;
                }
                if (abort) break;
            }
            visitor.End();
            if (Debug)
            {
                int voxelsCount = size * size * size;
                int facesCount = voxelsCount * 6;
                Console.Out.WriteLine(String.Format("Empty voxels  {0} / {1} ({2:P0})", ite.emptyVoxelsCount, voxelsCount, ite.emptyVoxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled voxels {0} / {1} ({2:P0})", ite.voxelsCount, voxelsCount, ite.voxelsCount / (float)voxelsCount));
                Console.Out.WriteLine(String.Format("Culled faces  {0} / {1} ({2:P0})", ite.facesCount, facesCount, ite.facesCount / (float)facesCount));
            }
        }

    }

    class ArrayVoxelMap : AbstractVoxelMap
    {
        protected readonly int size2;
        private int[] map;

        public ArrayVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
            if (size > 16)
            {
                //throw new Exception("!!!");
            }
            map = new int[size * size * size];
            size2 = size * size;
        }

        public override int Get(int x, int y, int z)
        {
            int index = (x - x0) + (y - y0) * size + (z - z0) * size2;
            return map[index];
        }

        public override void Set(int x, int y, int z, int v)
        {
            int index = (x - x0) + (y - y0) * size + (z - z0) * size2;
            map[index] = v;
        }

    }

    abstract class FunctionVoxelMap : AbstractVoxelMap
    {
        public FunctionVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
        }

        public override int Get(int x, int y, int z)
        {
            return F(x, y, z);
        }

        public override void Set(int x, int y, int z, int v)
        {
        }

        protected abstract int F(int x, int y, int z);
    }

    class PlaneVoxelMap : FunctionVoxelMap
    {
        public PlaneVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
        }

        public override bool IsEmpty()
        {
            return !(y0 == 0);
        }
        protected override int F(int x, int y, int z)
        {
            if (y == 0)
            {
                if (x == 0 || z == 0) return 3;
                return 2;
            }
            return 0;
        }
    }

    class AOTestVoxelMap : FunctionVoxelMap
    {
        public AOTestVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
        }

        public override bool IsEmpty()
        {
            return !((x0 == 0 || x0 == size) && y0 == 0 && z0 == 0);
        }

        protected override int F(int x, int y, int z)
        {
            int xx = x - x0;
            if (z < 1 && y < 2) return 1;
            if (xx < 1 && y < 2) return 1;
            //if (z < 1) return 0;
            if (y < 1) return 2;
            //if (y > 14) return 2;
            // if (y < 5 && x > 3 && x < size - 3 && z > 3 && z < size - 3) return 2;
            //if (y < 7 && x > 6 && x < size - 6 && z > 6 && z < size - 6) return 3;
            // (y < 8 && x > 7 && x < size - 7 && z > 7 && z < size - 7) return 4;
            //if (y == 12 && x > 7 && x < size - 7 && z > 7 && z < size - 7) return 4;
            //if (y == 13 && x > 6 && x < size - 6 && z > 6 && z < size - 6) return 3;
            //if (y == 14 && x > 4 && x < size - 4 && z > 4 && z < size - 4) return 3;
            //if (y == 9 && x == 8 && z == 8) return 5;
            return 0;
        }
    }

    class SpongeVoxelMap : FunctionVoxelMap
    {
        private FastNoise fn = new FastNoise();

        public SpongeVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
        }

        protected override int F(int x, int y, int z)
        {
            float scale = 0.50f;
            float n = fn.GetSimplex(x / scale, y / scale, z / scale);
            return (n > 0.5f) ? 2 : 0;
        }

        /*
        for (int x = 0; x<Width) 
        {
          for (int y = 0; y<Depth) 
          {
            for (int z = 0; z<Height) 
            {
              if(z<Noise2D(x, y) * Height) 
              {
                Array[x][y][z] = Noise3D(x, y, z)
              } else {
                Array[x][y][z] = 0
              }
            } 
          } 
        } 
        */

    }
    class PerlinNoiseVoxelMap : FunctionVoxelMap
    {
        //private SimplexNoiseGenerator g = new SimplexNoiseGenerator();
        private NoiseGen g = new NoiseGen();
        private FastNoise fn = new FastNoise();

        private static float max = float.MinValue;
        private static float min = float.MaxValue;
        private static int maxX = int.MinValue;
        private static int minX = int.MaxValue;
        private static int maxY = int.MinValue;
        private static int minY = int.MaxValue;
        private static int maxZ = int.MinValue;
        private static int minZ = int.MaxValue;

        public PerlinNoiseVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
        }


        public override bool IsEmpty()
        {
            return (y0 >= 0);
        }

        protected override int F(int x, int y, int z)
        {
            //float n = SimplexNoise.Generate(x, y, z, 0.5f, 1);
            //float n = SimplexNoise.CalcPixel3D(x, y, z,  1 / 10f);
            float scale = 0.50f;
            float n = fn.GetSimplex(x / scale, y / scale, z / scale);
            //float n = SimplexNoise.Generate(x / scale, y / scale, z / scale);
            //float n = g.noise(x / scale, y / scale, z / scale);
            //float n = (float) Perlin.perlin(x / scale, y / scale, z / scale);
            //float n = g.GetNoise(x / scale, y / scale, z / scale);

            bool b = false;
            if (n > max) { max = n; b = true; }
            if (n < min) { min = n; b = true; }
            /*
            if (x > maxX) { maxX = x; b = true; }
            if (x < minX) { minX = x; b = true; }
            if (y > maxY) { maxY = y; b = true; }
            if (y < minY) { minY = y; b = true; }
            if (z > maxZ) { maxZ = z; b = true; }
            if (z < minZ) { minZ = z; b = true; }
            */
            if (b)
            {
                Console.WriteLine((max - min) + " " + min + " " + max);
                Console.WriteLine(x + " " + y + " " + z);
                //Console.WriteLine(minX + " " + minY + " " + minZ);
                //Console.WriteLine(maxX + " " + maxY + " " + maxZ);
            }

            if (y >= 0)
            {
                float f = (float)y / 256f;
                n = -2;
            }
            return (n > 0.5f) ? 1 : (n >= -1) ? 4 : 0;
        }

        /*
        for (int x = 0; x<Width) 
        {
          for (int y = 0; y<Depth) 
          {
            for (int z = 0; z<Height) 
            {
              if(z<Noise2D(x, y) * Height) 
              {
                Array[x][y][z] = Noise3D(x, y, z)
              } else {
                Array[x][y][z] = 0
              }
            } 
          } 
        } 
        */

    }
}
