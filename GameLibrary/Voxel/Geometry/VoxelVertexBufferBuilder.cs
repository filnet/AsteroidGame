using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GameLibrary.Geometry.Common;

namespace GameLibrary.Voxel.Geometry
{
    public static class VoxelVertexBufferBuilderExtensions
    {
        public static int AddVertex(this VertexBufferBuilder<VoxelVertex> builder, Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
        {
            VoxelVertex vertex = new VoxelVertex(position, normal, textureCoordinate, 1, 1, textureIndex, lightTextureIndex);
            return builder.AddVertex(vertex);
        }

        public static int AddVertex(this VertexBufferBuilder<VoxelVertex> builder, Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int w, int h, int textureIndex, int lightTextureIndex)
        {
            VoxelVertex vertex = new VoxelVertex(position, normal, textureCoordinate, w, h, textureIndex, lightTextureIndex);
            return builder.AddVertex(vertex);
        }

        public static Vector3 VertexPosition(VoxelVertex vertex)
        {
            return vertex.Position;
        }
    }

    public class VoxelVertexBufferBuilder : VertexBufferBuilder<VoxelVertex>
    {
        public VoxelVertexBufferBuilder(GraphicsDevice gd) : base(gd)
        {
        }

        public VoxelVertexBufferBuilder(GraphicsDevice gd, int expectedVertexCount, int expectedIndexCount) : base(gd, expectedVertexCount, expectedIndexCount)
        {
        }

    }
}
