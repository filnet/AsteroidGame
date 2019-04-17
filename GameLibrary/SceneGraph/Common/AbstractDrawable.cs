using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Bounding;

namespace GameLibrary.SceneGraph.Common
{
    public abstract class AbstractDrawable : Drawable
    {
        public bool Enabled { get; set; }

        public bool Visible { get; set; }

        public int RenderGroupId { get; set; }

        public bool BoundingVolumeVisible { get; set; }

        /// <summary>
        /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
        /// </summary>
        public abstract Volume BoundingVolume { get; }

        /// <summary>
        /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
        /// </summary>
        public abstract Volume WorldBoundingVolume { get; }

        public virtual int VertexCount { get { throw new NotSupportedException(); } }

        public virtual void PreDraw(GraphicsDevice gd) { throw new NotSupportedException(); }
        public virtual void Draw(GraphicsDevice gd) { throw new NotSupportedException(); }
        public virtual void PostDraw(GraphicsDevice gd) { throw new NotSupportedException(); }

        public virtual void PreDrawInstanced(GraphicsDevice gd, VertexBuffer instanceVertexBuffer, int instanceOffset) { throw new NotSupportedException(); }
        public virtual void DrawInstanced(GraphicsDevice gd, int instanceCount) { throw new NotSupportedException(); }
        public virtual void PostDrawInstanced(GraphicsDevice gd) { throw new NotSupportedException(); }

    }
}
