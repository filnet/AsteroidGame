using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Voxel;

namespace GameLibrary
{
    public class VoxelMapGeometry2 : GeometryNode
    {
        int size;
        VoxelMap voxelMap;
        CubeGeometry cube;

        public VoxelMapGeometry2(String name, int size)
            : base(name)
        {

            //number_of_vertices = verticesCount;
            this.size = size;
        }

        static VertexDeclaration vertexDeclaration = new VertexDeclaration(new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            }
        );

        // To store instance transform matrices in a vertex buffer, we use this custom
        // vertex type which encodes 4x4 matrices as a set of four Vector4 values.
        static VertexDeclaration instanceVertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
        );

        public override void Initialize()
        {
            voxelMap = new FunctionVoxelMap(size);
            //voxelMap = new SimpleVoxelMap(size);

            cube = new CubeGeometry("CUBE");
            //cube.RenderGroupId = Scene.THREE_LIGHTS;
            //cube.Scale = new Vector3(2.0f);

            base.Initialize();
        }

        public override void Dispose()
        {

            //base.Dispose();
            cube.Dispose();
        }

        class DrawVisitor : Voxel.Visitor
        {
            public CubeGeometry cube;
            public GeometryNode.DrawContext dc;
            public int size;

            private static float d = 0.5773502692f; // 1 over the square root of 3

            public void begin(int size, int instanceCount, int maxInstanceCount)
            {
                this.size = size;
            }

            public void visit(int x, int y, int z, int v, int s)
            {
                Matrix currentWorld = Matrix.Identity;
                IEffectMatrices effectMatrices = null;
                if (dc.effect is IEffectMatrices)
                {
                    effectMatrices = dc.effect as IEffectMatrices;
                }
                if (effectMatrices != null)
                {
                    currentWorld = effectMatrices.World;
                    Matrix localMatrix = Matrix.CreateScale(0.95f / size) * Matrix.CreateTranslation((2 * x - size) * d / size, (2 * y - size) * d / size, (2 * z - size) * d / size);
                    //Matrix localMatrix = Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(x - s2, y - s2, z - s2);
                    effectMatrices.World = localMatrix * currentWorld;
                    dc.pass.Apply();                    
                }

                cube.Draw(dc);

                if (effectMatrices != null)
                {
                    effectMatrices.World = currentWorld;
                }

            }

            public void end()
            {
            }

        }


        DrawVisitor v = new DrawVisitor();


        public override void preDraw(GeometryNode.DrawContext dc)
        {
            if (cube.Scene == null)
            {
                cube.Scene = Scene;
                cube.Initialize();
            }
            cube.preDraw(dc);
        }

        public override void Draw(GeometryNode.DrawContext dc)
        {
            v.dc = dc;
            v.cube = cube;
            voxelMap.visit(v);
        }
    }

}
