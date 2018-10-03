using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLibrary.SceneGraph;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph.Common;

namespace GameLibrary.Control
{
    public abstract class BaseNodeController<T> : NodeController<T> where T : Node
    {
        private T node;

        public T Node
        {
            get { return node; }
        }

        protected BaseNodeController(T node)
            : base()
        {
            this.node = node;
        }

        public virtual void Update(GameTime gameTime)
        {
        }

    }
}
