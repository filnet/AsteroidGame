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

        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> UpdateOrderChanged;

        public T Node
        {
            get { return node; }
        }

        public bool Enabled => throw new NotImplementedException();

        public int UpdateOrder => throw new NotImplementedException();

        protected BaseNodeController(T node)
            : base()
        {
            this.node = node;
        }

        public virtual void Update(GameTime gameTime)
        {
            //throw new NotImplementedException();
        }

    }
}
