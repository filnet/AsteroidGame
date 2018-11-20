using System;
using System.Collections.Generic;
using System.Text;
using GameLibrary.SceneGraph.Bounding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.SceneGraph.Common
{
    public class TextureNode : Node, Spritable
    {
        private int renderGroupId;

        protected Texture2D texture2D;

        public int RenderGroupId { get { return renderGroupId; } set { renderGroupId = value; } }
        public bool BoundingVolumeVisible { get; set; }

        public BoundingVolume BoundingVolume { get; set; }

        public BoundingVolume WorldBoundingVolume { get; set; }

        public TextureNode(string name) : base(name)
        {
        }

        public TextureNode(Node node) : base(node)
        {
        }

        public override Node Clone()
        {
            throw new NotImplementedException();
        }

        public void PreDraw(GraphicsDevice gd)
        {
        }

        public void Draw(GraphicsDevice gd)
        {
        }

        public void PostDraw(GraphicsDevice gd)
        {
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture2D, Vector2.Zero);
        }

    }
}
