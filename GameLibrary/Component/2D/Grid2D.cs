using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary
{
    public class Grid2D : DrawableGameComponent
    {

        int number_of_vertices = 4;

        VertexBuffer vertexBuffer;

        public Grid2D(Game game, int verticesCount)
            : base(game)
        {
            number_of_vertices = verticesCount;
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
            int verticesCount = ((number_of_vertices + 1) * 2 * 2) + ((number_of_vertices + 1) * 2 * 2);
            VertexPositionColor[] vertices = new VertexPositionColor[verticesCount];
            Initialize(vertices, number_of_vertices);

            // Initialize the vertex buffer, allocating memory for each vertex.
            vertexBuffer = new VertexBuffer(
                GraphicsDevice,
                vertexDeclaration,
                verticesCount,
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
        private void Initialize(VertexPositionColor[] vertices, int count)
        {
            int v = 0;
            for (int i = -count; i <= count; i++)
            {
                Vector3 position1 = new Vector3();
                position1.X = (float) i;
                position1.Y = (float) -count;
                vertices[v++] = new VertexPositionColor(position1, Color.White);
                Vector3 position2 = new Vector3();
                position2.X = (float) i;
                position2.Y = (float) count;
                vertices[v++] = new VertexPositionColor(position2, Color.White);
            }
            for (int i = -count; i <= count; i++)
            {
                Vector3 position1 = new Vector3();
                position1.X = (float) -count;
                position1.Y = (float) i;
                vertices[v++] = new VertexPositionColor(position1, Color.White);
                Vector3 position2 = new Vector3();
                position2.X = (float) count;
                position2.Y = (float) i;
                vertices[v++] = new VertexPositionColor(position2, Color.White);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, ((number_of_vertices + 1) * 2) + ((number_of_vertices + 1) * 2));
        }
    }

}
