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

    public class VoxelMapInstancedRenderer : EffectRenderer
    {
        private readonly DrawVisitor drawVisitor;

        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        private DynamicVertexBuffer instanceVertexBuffer;

        private Matrix worldMatrix;

        //private EffectPass pass;

        public VoxelMapInstancedRenderer(Effect effect) : base(effect)
        {
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
            if (vertexBuffer == null)
            {
                VertexPositionNormalTexture[] cubeVertices = createCubeVertices();
                vertexBuffer = new VertexBuffer(
                    gc.GraphicsDevice,
                    StockEffects.InstancedEffect.VertexDeclaration,
                    cubeVertices.Length,
                    BufferUsage.WriteOnly
                );
                vertexBuffer.SetData<VertexPositionNormalTexture>(cubeVertices);

                indexBuffer = new IndexBuffer(gc.GraphicsDevice, typeof(short), cubeVertices.Length, BufferUsage.None);
                short[] indices = new short[cubeVertices.Length];
                for (short i = 0; i < cubeVertices.Length; i++)
                {
                    indices[i] = i;
                }
                indexBuffer.SetData(indices);


                //vertexBuffer.Dispose();
                //instanceVertexBuffer.Dispose();
            }
            /*
            if (cubeGeometry.Scene == null)
            {
                cubeGeometry.Scene = voxelMapGeometry.Scene;
                cubeGeometry.Initialize();
            }
            */

            if (effectMatrices != null)
            {
                effectMatrices.World = voxelMapGeometry.WorldTransform;
            }

            worldMatrix = voxelMapGeometry.WorldTransform;

            effect.CurrentTechnique = effect.Techniques["HardwareInstancing"];

            gc.GraphicsDevice.Indices = indexBuffer;

            //cubeGeometry.preDraw();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                //this.pass = pass;

                pass.Apply();

                voxelMapGeometry.Visit(drawVisitor);

                // If we have more instances than room in our vertex buffer, grow it to the neccessary size.
                if ((instanceVertexBuffer == null) || (drawVisitor.instanceTransforms.Length > instanceVertexBuffer.VertexCount))
                {
                    if (instanceVertexBuffer != null)
                    {
                        instanceVertexBuffer.Dispose();
                    }
                    instanceVertexBuffer = new DynamicVertexBuffer(gc.GraphicsDevice, StockEffects.InstancedEffect.InstanceVertexDeclaration,
                                                                   drawVisitor.instanceTransforms.Length, BufferUsage.WriteOnly);
                }

                // Transfer the latest instance transform matrices into the instanceVertexBuffer.
                instanceVertexBuffer.SetData(drawVisitor.instanceTransforms, 0, drawVisitor.instanceTransforms.Length, SetDataOptions.Discard);

                // Tell the GPU to read from both the model vertex buffer plus our instanceVertexBuffer.
                gc.GraphicsDevice.SetVertexBuffers(
                    new VertexBufferBinding(vertexBuffer, 0, 0),
                    new VertexBufferBinding(instanceVertexBuffer, 0, 1)
                );

                gc.GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 12, instanceVertexBuffer.VertexCount);
                //DrawInstancedPrimitives(PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount, int instanceCount);
            }
        }

        /// <summary>
        /// Initializes the vertices and indices of the 3D model.
        /// </summary>
        private VertexPositionNormalTexture[] createCubeVertices()
        {
            float d = 0.5773502692f; // 1 over the square root of 3

            // positions
            Vector3 topLeftFront = new Vector3(-d, d, d);
            Vector3 bottomLeftFront = new Vector3(-d, -d, d);
            Vector3 topRightFront = new Vector3(d, d, d);
            Vector3 bottomRightFront = new Vector3(d, -d, d);
            Vector3 topLeftBack = new Vector3(-d, d, -d);
            Vector3 topRightBack = new Vector3(d, d, -d);
            Vector3 bottomLeftBack = new Vector3(-d, -d, -d);
            Vector3 bottomRightBack = new Vector3(d, -d, -d);

            // textures
            Vector2 textureTopLeft = new Vector2(0.0f, 0.0f);
            Vector2 textureTopRight = new Vector2(1.0f, 0.0f);
            Vector2 textureBottomLeft = new Vector2(0.0f, 1.0f);
            Vector2 textureBottomRight = new Vector2(1.0f, 1.0f);

            // normals
            Vector3 frontNormal = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 backNormal = new Vector3(0.0f, 0.0f, -1.0f);
            Vector3 topNormal = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 bottomNormal = new Vector3(0.0f, -1.0f, 0.0f);
            Vector3 leftNormal = new Vector3(-1.0f, 0.0f, 0.0f);
            Vector3 rightNormal = new Vector3(1.0f, 0.0f, 0.0f);

            VertexPositionNormalTexture[] cubeVertices = new VertexPositionNormalTexture[36];

            // front face
            cubeVertices[0] = new VertexPositionNormalTexture(topLeftFront, frontNormal, textureTopLeft);
            cubeVertices[1] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            cubeVertices[2] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);
            cubeVertices[3] = new VertexPositionNormalTexture(bottomLeftFront, frontNormal, textureBottomLeft);
            cubeVertices[4] = new VertexPositionNormalTexture(bottomRightFront, frontNormal, textureBottomRight);
            cubeVertices[5] = new VertexPositionNormalTexture(topRightFront, frontNormal, textureTopRight);

            // back face
            cubeVertices[6] = new VertexPositionNormalTexture(topLeftBack, backNormal, textureTopRight);
            cubeVertices[7] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            cubeVertices[8] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            cubeVertices[9] = new VertexPositionNormalTexture(bottomLeftBack, backNormal, textureBottomRight);
            cubeVertices[10] = new VertexPositionNormalTexture(topRightBack, backNormal, textureTopLeft);
            cubeVertices[11] = new VertexPositionNormalTexture(bottomRightBack, backNormal, textureBottomLeft);

            // top face
            cubeVertices[12] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            cubeVertices[13] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);
            cubeVertices[14] = new VertexPositionNormalTexture(topLeftBack, topNormal, textureTopLeft);
            cubeVertices[15] = new VertexPositionNormalTexture(topLeftFront, topNormal, textureBottomLeft);
            cubeVertices[16] = new VertexPositionNormalTexture(topRightFront, topNormal, textureBottomRight);
            cubeVertices[17] = new VertexPositionNormalTexture(topRightBack, topNormal, textureTopRight);

            // bottom face
            cubeVertices[18] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            cubeVertices[19] = new VertexPositionNormalTexture(bottomLeftBack, bottomNormal, textureBottomLeft);
            cubeVertices[20] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            cubeVertices[21] = new VertexPositionNormalTexture(bottomLeftFront, bottomNormal, textureTopLeft);
            cubeVertices[22] = new VertexPositionNormalTexture(bottomRightBack, bottomNormal, textureBottomRight);
            cubeVertices[23] = new VertexPositionNormalTexture(bottomRightFront, bottomNormal, textureTopRight);

            // left face
            cubeVertices[24] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);
            cubeVertices[25] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            cubeVertices[26] = new VertexPositionNormalTexture(bottomLeftFront, leftNormal, textureBottomRight);
            cubeVertices[27] = new VertexPositionNormalTexture(topLeftBack, leftNormal, textureTopLeft);
            cubeVertices[28] = new VertexPositionNormalTexture(bottomLeftBack, leftNormal, textureBottomLeft);
            cubeVertices[29] = new VertexPositionNormalTexture(topLeftFront, leftNormal, textureTopRight);

            // right face
            cubeVertices[30] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            cubeVertices[31] = new VertexPositionNormalTexture(bottomRightFront, rightNormal, textureBottomLeft);
            cubeVertices[32] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);
            cubeVertices[33] = new VertexPositionNormalTexture(topRightBack, rightNormal, textureTopRight);
            cubeVertices[34] = new VertexPositionNormalTexture(topRightFront, rightNormal, textureTopLeft);
            cubeVertices[35] = new VertexPositionNormalTexture(bottomRightBack, rightNormal, textureBottomRight);

            return cubeVertices;
        }

        class DrawVisitor : Voxel.Visitor
        {
            private static float d = 0.5773502692f; // 1 over the square root of 3

            private readonly VoxelMapInstancedRenderer parent;

            public int instances;
            public Matrix[] instanceTransforms;

            public DrawVisitor(VoxelMapInstancedRenderer parent)
            {
                this.parent = parent;
            }

            public bool Begin(int size, int instanceCount, int maxInstanceCount)
            {
                // FIXME remove this resize (bad for performance)
                Array.Resize(ref instanceTransforms, instanceCount);
                instances = 0;
                return true;
            }

            public bool Visit(VoxelMapIterator ite)
            {
                int size = ite.Size - 1;
                Matrix localMatrix = Matrix.CreateScale(0.95f / size) * Matrix.CreateTranslation(
                    d * (2 * ite.X - size) / size, d * (2 * ite.Y - size) / size, d * (2 * ite.Z - size) / size);
                instanceTransforms[instances] = localMatrix;// * parent.worldMatrix;
                instances++;
                return true;
                /*
                                Matrix currentWorld = Matrix.Identity;
                                if (parent.effectMatrices != null)
                                {
                                    Matrix localMatrix = Matrix.CreateScale(0.95f / size) * Matrix.CreateTranslation((2 * x - size) * d / size, (2 * y - size) * d / size, (2 * z - size) * d / size);
                                    //Matrix localMatrix = Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(x - s2, y - s2, z - s2);
                                    parent.effectMatrices.World = localMatrix * parent.worldMatrix;
                                }

                                parent.pass.Apply();

                                //parent.cubeGeometry.Draw();

                                if (parent.effectMatrices != null)
                                {
                                    parent.effectMatrices.World = currentWorld;
                                }
                                */
            }

            public bool End()
            {
                return true;
            }
        }
    }

}
