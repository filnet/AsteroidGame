using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;

namespace GameLibrary.Control
{
    public interface PhysicsController<T> : NodeController<T> where T : GeometryNode
    {
        //GeometryNode.PhysicsStruct Physics { get; } 
    }
}
