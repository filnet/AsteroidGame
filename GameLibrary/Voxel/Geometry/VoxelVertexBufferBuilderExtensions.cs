using GameLibrary.Geometry.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.Voxel.Geometry
{
    public static class VoxelVertexBufferBuilderExtensions
    {
        public static int AddVertex(this VertexBufferBuilder<VoxelVertex> builder, Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
        {
            VoxelVertex vertex = new VoxelVertex(position, normal, textureCoordinate, textureIndex, lightTextureIndex);
            return builder.AddVertex(vertex);
        }

        public static Vector3 VertexPosition(VoxelVertex vertex)
        {
            return vertex.Position;
        }
    }

}
