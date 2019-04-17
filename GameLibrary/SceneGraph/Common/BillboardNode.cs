using System;
using GameLibrary.Geometry;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.SceneGraph.Common
{
    public class BillboardNode : MeshNode
    {
        // Hack to handle texture arrays...
        public int Mode
        {
            get;
            set;
        }

        public Texture2D Texture
        {
            get;
            set;
        }

        // TODO BillboardNode should be able to share mesh
        public BillboardNode(String name) : base(name, new QuadMeshFactory())
        {
        }

        public BillboardNode(BillboardNode node) : base(node)
        {
        }

    }
}
