using GameLibrary.SceneGraph.Common;

namespace GameLibrary.Control
{
    public class NodeInputController<T> : InputController, NodeController<T> where T : Node
    {
        private T node;

        public T Node
        {
            get { return node; }
        }

        public NodeInputController(T node)
            : base()
        {
            this.node = node;
        }

    }
}
