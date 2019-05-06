using GameLibrary.Util;

namespace GameLibrary.Voxel
{
    abstract class FunctionVoxelMap : AbstractVoxelMap
    {
        public FunctionVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
        }

        protected abstract ushort F(int x, int y, int z);

        public override ushort Get(int x, int y, int z)
        {
            /*
                        if (!Contains(x, y, z))
                        {
                            Console.WriteLine("!!!");
                        }
            */
            return F(x, y, z);
        }

        public override void Set(int x, int y, int z, ushort v)
        {
        }

        public override bool Next(ref MapIterator ite, out ushort value)
        {
            if (ite.z >= size)
            {
                value = 0;
                return false;
            }
            value = F(x0 + ite.x, y0 + ite.y, z0 + ite.z);
            ite.x++;
            if (ite.x >= size)
            {
                ite.x = 0;
                ite.y++;
                if (ite.y >= size)
                {
                    ite.y = 0;
                    ite.z++;
                }
            }
            return true;
        }
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

        protected override ushort F(int x, int y, int z)
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

        protected override ushort F(int x, int y, int z)
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

        protected override ushort F(int x, int y, int z)
        {
            float scale = 0.50f;
            float n = fn.GetSimplex(x / scale, y / scale, z / scale);
            return (n > 0.5f) ? (ushort)2 : (ushort)0;
        }
    }

    class PerlinNoiseVoxelMap : FunctionVoxelMap
    {
        //private static SimplexNoiseGenerator g = new SimplexNoiseGenerator();
        private static readonly NoiseGen g = new NoiseGen();
        //private static readonly FastNoise fn = new FastNoise(37);
        private static readonly FastNoise fn = new FastNoise();

        private static float max = float.MinValue;
        private static float min = float.MaxValue;

        public PerlinNoiseVoxelMap(int size, int x0, int y0, int z0) : base(size, x0, y0, z0)
        {
        }

        public override bool IsEmpty()
        {
            return false;
        }

        protected override ushort F(int x, int y, int z)
        {
            /*if ((x0 != -32) || (y0 != -64) || (z0 != -32))
            {
                return 0;
            }*/

            /*
            if (x == 0 || z == 0)
            {
                return 0;
            }
            */

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
            if (b)
            {
                //Console.WriteLine((max - min) + " " + min + " " + max);
                //Console.WriteLine(x + " " + y + " " + z);
            }

            if (y >= 0)
            {
                //if (n < 0.001f) return 3;
                int lx, ly, lz;
                if (y < 5)
                {
                    lx = (x >= 0) ? x % size : size + (x % size) - 1;
                    ly = (y >= 0) ? y % size : size + (y % size) - 1;
                    lz = (z >= 0) ? z % size : size + (z % size) - 1;
                    /*lx = x;
                    ly = y;
                    lz = z;*/

                    if ((lx == 2 || lx == 3) && (lz == 2 || lz == 3))
                    {
                        return (y == 2) ? (ushort)VoxelType.Test : (ushort)VoxelType.Rock;
                    }
                }
                /*if (y == 0)
                {
                    lx = (x >= 0) ? x % size : size + (x % size) - 1;
                    ly = (y >= 0) ? y % size : size + (y % size) - 1;
                    lz = (z >= 0) ? z % size : size + (z % size) - 1;

                    if ((lx == 0) && (lz == 16))
                    {
                        return (ushort)VoxelType.Rock;
                    }
                }*/
                //if (y < 5)
                //{
                if (x == -1)
                {
                    //return (ushort)VoxelType.Rock;
                }
                //}
                return 0;
            }
            if (n < 0.05f)
            {
                if (y == -1)
                {
                    return (ushort)VoxelType.GrassyEarth;
                }
                return (ushort)VoxelType.Rock;
            }
            if (n < 0.2f)
            {
                return (ushort)VoxelType.GrassyEarth;
            }
            if (y >= -10)
            {
                return (n > 0.5f) ? (ushort)VoxelType.Water : (ushort)VoxelType.Earth;
            }
            return (n > 0.5f) ? (ushort)VoxelType.Water : (ushort)VoxelType.Earth;
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
