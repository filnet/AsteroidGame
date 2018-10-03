using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary
{
    public class Line2D: DrawableGameComponent
    {

        private VertexBuffer vertexBuffer;

        public Line2D(Game game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            VertexDeclaration vertexDeclaration = new VertexDeclaration(new VertexElement[]
                {
                    new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                    new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0)
                }
            );

            VertexPositionColor[] vertices = new VertexPositionColor[2];
            Initialize(vertices);

            // Initialize the vertex buffer, allocating memory for each vertex.
            vertexBuffer = new VertexBuffer(
                GraphicsDevice,
                vertexDeclaration,
                2,
                BufferUsage.WriteOnly
            );

            // Set the vertex buffer data to the array of vertices.
            vertexBuffer.SetData<VertexPositionColor>(vertices);
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
            vertexBuffer.Dispose();
        }

        /// <summary>
        /// Initializes the vertices and indices of the 3D model.
        /// </summary>
        private void Initialize(VertexPositionColor[] vertices)
        {
            vertices[0] = new VertexPositionColor(Vector3.Zero, Color.Yellow);
            vertices[1] = new VertexPositionColor(Vector3.Right, Color.Yellow);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, 1);
        }
    }

}
