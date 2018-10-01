using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameLibrary.Voxel
{
    public interface Visitor
    {
        void begin(int size, int instanceCount, int maxInstanceCount);
        void visit(int x, int y, int z, int v, int s);
        void end();
    }

    interface VoxelMap
    {
        void visit(Visitor visitor);
    }

    class FunctionVoxelMap : VoxelMap
    {
        int size;

        public FunctionVoxelMap(int size)
        {
            this.size = size;
        }

        private int f(int x, int y, int z)
        {
                        //int v = (((x * y * z) % 7) == 0) ? 0 : 1;
                        int v = (y == 2 || x == 2 || z == 2) ? 0 : 1;
            /*
                        int x2 = 2 * x - size;
                        int y2 = 2 * y - size;
                        int z2 = 2 * z - size;
                        int r4 = (x2 * x2) + (y2 * y2) + (z2 * z2);
                        if (r4 < size * size && r4 > (size * size) - size)
             */
                        return v;
        }

        public void visit(Visitor visitor)
        {
            int instanceCount = size * size * size;
            visitor.begin(size, instanceCount, instanceCount);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        int v = f(x, y, z);
                        if (v != 0)
                        {
                            visitor.visit(x, y, z, v, 1);
                        }
                    }
                }
            }
            visitor.end();
        }
    }

    class SimpleVoxelMap : VoxelMap
    {
        int size;
        private int[, ,] map;

        public SimpleVoxelMap(int size)
        {
            if (size > 16)
            {
                size = 16;
                //throw new Exception("!!!");
            }
            this.size = size;
            map = new int[size, size, size];
        }

        public void visit(Visitor visitor)
        {
            int instanceCount = size * size * size;
            visitor.begin(size, instanceCount, instanceCount);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        int v = map[x, y, z];
                        visitor.visit(x, y, z, v, 1);
                    }
                }
            }
            visitor.end();
        }
    }

    class Layer
    {
        private int[] rleData;
    }

    class LayeredVoxelMap : VoxelMap
    {
        private Layer[] layers;
        public void visit(Visitor visitor)
        {

        }
    }
}
