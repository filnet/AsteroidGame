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
    public class VoxelMapGeometry : GeometryNode
    {
        int size;
        VoxelMap voxelMap;

        public VoxelMapGeometry(String name, int size)
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
            voxelMap = new SimpleVoxelMap(size);

            VertexPositionNormalTexture[] cubeVertices = createCubeVertices();
            vertexBuffer = new VertexBuffer(
                Scene.GraphicsDevice,
                vertexDeclaration,
                cubeVertices.Length,
                BufferUsage.WriteOnly
            );
            vertexBuffer.SetData<VertexPositionNormalTexture>(cubeVertices);

            base.Initialize();
        }

        public override void Dispose()
        {

            //base.Dispose();
            vertexBuffer.Dispose();
            instanceVertexBuffer.Dispose();
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

        class PreDrawVisitor : Voxel.Visitor
        {
            int instances;
            public Matrix[] instanceTransforms;

            public void begin(int size, int instanceCount, int maxInstanceCount)
            {
                // FIXME remove this resize (bad for performance)
                Array.Resize(ref instanceTransforms, instanceCount);
                instances = 0;
            }

            public void visit(int x, int y, int z, int v, int s)
            {
                instanceTransforms[instances] = Matrix.CreateTranslation(x, y, z);
                instances++;
            }

            public void end()
            {
            }

        }


        PreDrawVisitor v = new PreDrawVisitor();

        VertexBuffer vertexBuffer;

        DynamicVertexBuffer instanceVertexBuffer;

        public override void preDraw(GeometryNode.DrawContext dc)
        {
            voxelMap.visit(v);

            // If we have more instances than room in our vertex buffer, grow it to the neccessary size.
            if ((instanceVertexBuffer == null) || (v.instanceTransforms.Length > instanceVertexBuffer.VertexCount))
            {
                if (instanceVertexBuffer != null)
                {
                    instanceVertexBuffer.Dispose();
                }
                instanceVertexBuffer = new DynamicVertexBuffer(Scene.GraphicsDevice, instanceVertexDeclaration,
                                                               v.instanceTransforms.Length, BufferUsage.WriteOnly);
            }

            // Transfer the latest instance transform matrices into the instanceVertexBuffer.
            instanceVertexBuffer.SetData(v.instanceTransforms, 0, v.instanceTransforms.Length, SetDataOptions.Discard);

            // Tell the GPU to read from both the model vertex buffer plus our instanceVertexBuffer.
            Scene.GraphicsDevice.SetVertexBuffers(
                        new VertexBufferBinding(vertexBuffer, 0, 0),
                        new VertexBufferBinding(instanceVertexBuffer, 0, 1)
                    );
        }

        public override void Draw(GeometryNode.DrawContext dc)
        {
            /*
            Console.WriteLine("XXXX " + vertexBuffer.IsDisposed);
            Console.WriteLine("XXXX " + vertexBuffer.VertexCount);
            Console.WriteLine("XXXX " + instanceVertexBuffer.IsDisposed);
            Console.WriteLine("XXXX " + instanceVertexBuffer.IsContentLost);
            Console.WriteLine("XXXX " + instanceVertexBuffer.VertexCount);
             */
            Scene.GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
                                      vertexBuffer.VertexCount, 0,
                                      12, instanceVertexBuffer.VertexCount);
        }
    }

}
