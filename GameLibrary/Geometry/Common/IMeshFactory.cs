using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace GameLibrary.Geometry.Common
{
    public interface IMeshFactory
    {
        Mesh CreateMesh(GraphicsDevice gd);
    }
}
