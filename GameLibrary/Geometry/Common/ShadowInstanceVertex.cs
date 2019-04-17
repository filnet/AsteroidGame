using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.Runtime.InteropServices;

namespace GameLibrary.Geometry.Common
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ShadowInstanceVertex : IVertexType
    {
        public byte SplitIndex;
        public byte SplitIndex1;
        public byte SplitIndex2;
        public byte SplitIndex3;

        public static readonly VertexDeclaration VertexDeclaration;

        public ShadowInstanceVertex(int splitIndex)
        {
            this.SplitIndex = (byte)splitIndex;
            this.SplitIndex1 = 0;
            this.SplitIndex2 = 0;
            this.SplitIndex3 = 0;
            //this.SplitIndex = new Byte4(SplitIndex, 0, 0, 0);// SplitIndex;
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
                var hashCode = SplitIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ SplitIndex1.GetHashCode();
                hashCode = (hashCode * 397) ^ SplitIndex2.GetHashCode();
                hashCode = (hashCode * 397) ^ SplitIndex3.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return "{{SplitIndex:" + this.SplitIndex + " SplitIndex1:" + this.SplitIndex1 + "}}";
        }

        public static bool operator ==(ShadowInstanceVertex left, ShadowInstanceVertex right)
        {
            return ((left.SplitIndex == right.SplitIndex) &&
                (left.SplitIndex1 == right.SplitIndex1) &&
                (left.SplitIndex2 == right.SplitIndex2) &&
                (left.SplitIndex3 == right.SplitIndex3));
        }

        public static bool operator !=(ShadowInstanceVertex left, ShadowInstanceVertex right)
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
            return (this == ((ShadowInstanceVertex)obj));
        }

        static ShadowInstanceVertex()
        {
            VertexElement[] elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 1),
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }
    }
}

