using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary
{
    public class Circle2D: DrawableGameComponent
    {

        private int number_of_vertices = 4;
        
        private VertexBuffer vertexBuffer;

        //private BasicEffect basicEffect;

        //public BasicEffect Effect
        //{
        //    get
        //    {
        //        return basicEffect;
        //    }
        //}

        public Circle2D(Game game, int verticesCount)
            : base(game)
        {
            number_of_vertices = verticesCount;
        }

        public override void Initialize()
        {
            base.Initialize();
            //InitializeEffect();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            //VertexDeclaration vertexDeclaration = new VertexDeclaration(new VertexElement[]
            //    {
            //        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            //        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            //        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            //    }
            //);
            VertexDeclaration vertexDeclaration = new VertexDeclaration(new VertexElement[]
                {
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                    new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0)
                }
            );


            
            //VertexPositionNormalTexture[] cubeVertices = new VertexPositionNormalTexture[number_of_vertices];
            int verticesCount = number_of_vertices + 1;
            VertexPositionColor[] vertices = new VertexPositionColor[verticesCount];
            Initialize(vertices, number_of_vertices);
            vertices[verticesCount - 1] = vertices[0];

            // Initialize the vertex buffer, allocating memory for each vertex.
            vertexBuffer = new VertexBuffer(
                GraphicsDevice,
                vertexDeclaration,
                verticesCount,
                BufferUsage.WriteOnly
            );

            // Set the vertex buffer data to the array of vertices.
            vertexBuffer.SetData<VertexPositionColor>(vertices);

            //graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);
            //return vertexBuffer;
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
            vertexBuffer.Dispose();
            //vertexBuffer.SetData(null);
        }

        /// <summary>
        /// Initializes the vertices and indices of the 3D model.
        /// </summary>
        private void Initialize(VertexPositionColor[] vertices, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 position = new Vector3();
                float angle = (360.0f * (float) i) / (float) count;
                position.X = (float) Math.Cos((double) MathHelper.ToRadians(angle));
                position.Y = (float) Math.Sin((double) MathHelper.ToRadians(angle));
                vertices[i] = new VertexPositionColor(position, Color.White);
            }
        }

        //private void InitializeEffect()
        //{
        //    basicEffect = new BasicEffect(GraphicsDevice);

        //    basicEffect.World = Matrix.Identity;

        //    // primitive color
        //    basicEffect = new BasicEffect(GraphicsDevice);
        //    basicEffect.VertexColorEnabled = true;
        //}

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            GraphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, number_of_vertices);
            
            //RasterizerState rasterizerState = new RasterizerState();
            //rasterizerState.CullMode = CullMode.None;
            //GraphicsDevice.RasterizerState = rasterizerState;

            //GraphicsDevice.BlendState = BlendState.Opaque;
            //GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            //GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            //GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            //BasicEffect basicEffect = cube.Effect;
            //basicEffect.Projection = cameraComponent.ProjectionMatrix;
            //basicEffect.View = cameraComponent.ViewMatrix;
            //basicEffect.World = Matrix.Identity;

            //foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            //{
            //    pass.Apply();
                //GraphicsDevice.DrawPrimitives(PrimitiveType.LineStrip, 0, number_of_vertices);
            //}
        }
    }

}
