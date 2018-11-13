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
        /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
        /// </summary>
        BoundingVolume BoundingVolume { get; }

        /// <summary>
        /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
        /// </summary>
        BoundingVolume WorldBoundingVolume { get; }

        void PreDraw(GraphicsDevice gd);
        void Draw(GraphicsDevice gd);
        void PostDraw(GraphicsDevice gd);
    }
}
