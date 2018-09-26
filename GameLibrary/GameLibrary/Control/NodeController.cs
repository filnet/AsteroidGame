using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;

namespace GameLibrary.Control
{
    public interface NodeController<T> : Controller where T : Node
    {
        T Node { get; }
    }
}
