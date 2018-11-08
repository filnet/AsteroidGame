﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.Runtime.InteropServices;

namespace GameLibrary.Geometry.Common
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VoxelVertex : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public byte TextureIndex;
        public byte TextureIndex1;
        public byte TextureIndex2;
        public byte TextureIndex3;
        public static readonly VertexDeclaration VertexDeclaration;
        public VoxelVertex(Vector3 position, Vector3 normal, Vector2 textureCoordinate, int textureIndex, int lightTextureIndex)
        {
            this.Position = position;
            this.Normal = normal;
            this.TextureCoordinate = textureCoordinate;
            this.TextureIndex = (byte)textureIndex;
            this.TextureIndex1 = (byte)lightTextureIndex;
            this.TextureIndex2 = 0;
            this.TextureIndex3 = 0;
            //this.TextureIndex = new Byte4(textureIndex, 0, 0, 0);// textureIndex;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Normal.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureIndex1.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return "{{Position:" + this.Position + " Color:" + " Normal:" + this.Normal + " TextureCoordinate:" + this.TextureCoordinate + " TextureIndex:" + this.TextureIndex + " TextureIndex1:" + this.TextureIndex1 + "}}";
        }

        public static bool operator ==(VoxelVertex left, VoxelVertex right)
        {
            return ((left.Position == right.Position) && (left.Normal == right.Normal) && (left.TextureCoordinate == right.TextureCoordinate) && (left.TextureIndex == right.TextureIndex) && (left.TextureIndex1 == right.TextureIndex1));
        }

        public static bool operator !=(VoxelVertex left, VoxelVertex right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != base.GetType())
            {
                return false;
            }
            return (this == ((VoxelVertex)obj));
        }

        static VoxelVertex()
        {
            VertexElement[] elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(32, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0),
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }
    }
}

