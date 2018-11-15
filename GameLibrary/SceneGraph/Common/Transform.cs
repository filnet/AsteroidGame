using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Bounding;

namespace GameLibrary.SceneGraph.Common
{
    public interface Transform
    {
        /*
        Vector3 Scale { get; set;  }
        Quaternion Rotation { get; set; }
        Vector3 Translation { get; set; }
        */
        Matrix Transform { get; }
        Matrix WorldTransform { get; }
    }
}
