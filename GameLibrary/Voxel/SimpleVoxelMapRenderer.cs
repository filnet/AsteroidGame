using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;
using GameLibrary.SceneGraph.Bounding;
using GameLibrary.Geometry.Common;
using GameLibrary.Geometry;
using GameLibrary.Voxel;

namespace GameLibrary.SceneGraph
{

    public class SimpleVoxelMapRenderer : EffectRenderer
    {
        private GeometryNode cubeGeometry;

        private readonly DrawVisitor drawVisitor;

        private Matrix worldMatrix;

        private EffectPass pass;

        public SimpleVoxelMapRenderer(Effect effect) : base(effect)
        {
            // FIXME need to call cubeGeometry.Dispose() at some point...
            cubeGeometry = GeometryUtil.CreateCube("VOXEL_CUBE");
            drawVisitor = new DrawVisitor(this);
        }

        public override void Render(GraphicsContext gc, List<GeometryNode> nodeList)
        {
            gc.GraphicsDevice.BlendState = BlendState;
            gc.GraphicsDevice.DepthStencilState = DepthStencilState;
            gc.GraphicsDevice.RasterizerState = RasterizerState;
            gc.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = gc.Camera.ProjectionMatrix;
                effectMatrices.View = gc.Camera.ViewMatrix;
            }

            foreach (GeometryNode node in nodeList)
            {
                VoxelMapGeometry voxelMapGeometry = node as VoxelMapGeometry;
                if (voxelMapGeometry != null)
                {
                    render(gc, voxelMapGeometry);
                }
            }
        }

        private void render(GraphicsContext gc, VoxelMapGeometry voxelMapGeometry)
        {
            if (cubeGeometry.Scene == null)
            {
                cubeGeometry.Scene = voxelMapGeometry.Scene;
                cubeGeometry.Initialize();
            }

            worldMatrix = voxelMapGeometry.WorldTransform;

            cubeGeometry.preDraw();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                this.pass = pass;
                voxelMapGeometry.Visit(drawVisitor);
            }
        }

        class DrawVisitor : Voxel.Visitor
        {
            private static float d = 0.5773502692f; // 1 over the square root of 3

            private readonly SimpleVoxelMapRenderer parent;

            public DrawVisitor(SimpleVoxelMapRenderer parent)
            {
                this.parent = parent;
            }

            public bool Begin(int size, int instanceCount, int maxInstanceCount)
            {
                return true;
            }

            public bool Visit(VoxelMapIterator ite)
            {
                Matrix currentWorld = Matrix.Identity;
                if (parent.effectMatrices != null)
                {
                    int size = ite.Size - 1;
                    Matrix localMatrix = Matrix.CreateScale(0.95f / size) * Matrix.CreateTranslation(
                        d * (2 * ite.X - size) / size, d * (2 * ite.Y - size) / size, d * (2 * ite.Z - size) / size);
                    parent.effectMatrices.World = localMatrix * parent.worldMatrix;
                }
                parent.pass.Apply();

                parent.cubeGeometry.Draw();

                return true;
            }

            public bool End()
            {
                return true;
            }
        }
    }

}
