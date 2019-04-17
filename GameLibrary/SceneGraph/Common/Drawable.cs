using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Bounding;

namespace GameLibrary.SceneGraph.Common
{
    public interface Drawable
    {
        bool Enabled { get; set; }

        bool Visible { get; set; }

        int RenderGroupId { get; set; }

        bool BoundingVolumeVisible { get; set; }

        /// <summary>
        /// Gets the geometry bounding volume, which contains the entire geometry in model (local) space.
        /// </summary>
        Volume BoundingVolume { get; }

        /// <summary>
        /// Gets the geometry bounding volume, which contains the entire geometry in world space.
        /// </summary>
        Volume WorldBoundingVolume { get; }

        int VertexCount { get; }

        void PreDraw(GraphicsDevice gd);
        void Draw(GraphicsDevice gd);
        void PostDraw(GraphicsDevice gd);

        void PreDrawInstanced(GraphicsDevice gd, VertexBuffer instanceVertexBuffer, int instanceOffset);
        void DrawInstanced(GraphicsDevice gd, int instanceCount);
        void PostDrawInstanced(GraphicsDevice gd);
    }
}
