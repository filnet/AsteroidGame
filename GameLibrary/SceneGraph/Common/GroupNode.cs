using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Bounding;

namespace GameLibrary.SceneGraph.Common
{
    public class GroupNode : Node
    {
        #region Fields

        //private BoundingVolume boundingVolume;

        private LinkedList<Node> nodes;

        //internal enum ChangeType { Add, Remove }

        //internal struct NodeChangeInfo
        //{
        //    public Node Node;
        //    public ChangeType Type;
        //}

        public enum EventType
        {
            ADDED,
            ADDED_FIRST,
            REMOVED
        }

        internal class NodeEvent
        {

            public EventType Type;
            public Node Node;
            public NodeEvent(EventType Type, Node Node)
            {
                this.Type = Type;
                this.Node = Node;
            }

        }

        private LinkedList<NodeEvent> nodeEvents;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a sphere that encloses the contents of all the nodes below the current one.
        /// </summary>
        /// <remarks>
        /// This node itself is not included in this bounding sphere
        /// </remarks>
        /// 
        /*
                public virtual BoundingVolume BoundingVolume
                {
                    get { return boundingVolume; }
                    internal set { boundingVolume = value; }
                }
                */

        public bool BoundingVolumeVisible
        {
            get;
            set;
        }
        public LinkedList<Node> Nodes
        {
            get { return nodes; }
        }

        #endregion

        #region Constructor

        public GroupNode(String name)
            : base(name)
        {
            BoundingVolumeVisible = true;
            nodes = new LinkedList<Node>();
            setStructureDirty();
        }

        public GroupNode(GroupNode node)
            : base(node)
        {
            //boundingVolume = node.boundingVolume != null ? node.boundingVolume.Clone() : null;
            BoundingVolumeVisible = node.BoundingVolumeVisible;
            nodes = new LinkedList<Node>();
            for (LinkedListNode<Node> it = node.Nodes.First; it != null; it = it.Next)
            {
                Add(it.Value.Clone());
            }
            setStructureDirty();
        }

        #endregion

        #region Public Methods

        public override Node Clone()
        {
            return new GroupNode(this);
        }

        public void Add(Node node)
        {
            if (nodeEvents == null)
            {
                nodeEvents = new LinkedList<NodeEvent>();
            }
            // TODO performance: call AddFrist
            nodeEvents.AddLast(new NodeEvent(EventType.ADDED, node));
            setStructureDirty();
        }

        public void AddFirst(Node node)
        {
            if (nodeEvents == null)
            {
                nodeEvents = new LinkedList<NodeEvent>();
            }
            // TODO performance: call AddFrist
            nodeEvents.AddLast(new NodeEvent(EventType.ADDED_FIRST, node));
            setStructureDirty();
        }

        public void Remove(Node node)
        {
            if (nodeEvents == null)
            {
                nodeEvents = new LinkedList<NodeEvent>();
            }
            // TODO performance: call AddFrist
            nodeEvents.AddLast(new NodeEvent(EventType.REMOVED, node));
            setStructureDirty();
        }

        public void Commit(GraphicsDevice graphicsDevice)
        {
            if (!isDirty(DirtyFlag.Structure))
            {
                return;
            }
            if (nodeEvents != null)
            {
                bool added = false;
                for (LinkedListNode<NodeEvent> it = nodeEvents.First; it != null; it = it.Next)
                {
                    NodeEvent evt = it.Value;
                    Node node = evt.Node;
                    switch (evt.Type)
                    {
                        case EventType.ADDED:
                            added = true;
                            node.ParentNode = this;
                            //setSceneRecursive(node);
                            nodes.AddLast(node);
                            node.Initialize(graphicsDevice);
                            if (node.IsDirty(DirtyFlag.Transform))
                            {
                                node.setDirty(DirtyFlag.ChildTransform);
                                setParentDirty(DirtyFlag.ChildTransform);
                            }
                            // FIXME this will cause every node in the scene to recompute their world transform
                            node.setDirty(DirtyFlag.ChildWorldTransform);
                            setParentDirty(DirtyFlag.ChildWorldTransform);
                            break;
                        case EventType.ADDED_FIRST:
                            added = true;
                            node.ParentNode = this;
                            //setSceneRecursive(node);
                            nodes.AddFirst(node);
                            node.Initialize(graphicsDevice);
                            if (node.IsDirty(DirtyFlag.Transform))
                            {
                                node.setDirty(DirtyFlag.ChildTransform);
                                setParentDirty(DirtyFlag.ChildTransform);
                            }
                            // FIXME this will cause every node in the scene to recompute their world transform
                            node.setDirty(DirtyFlag.ChildWorldTransform);
                            setParentDirty(DirtyFlag.ChildWorldTransform);
                            break;
                        case EventType.REMOVED:
                            node.ParentNode = null;
                            nodes.Remove(node);
                            node.Dispose();
                            break;
                    }
                }
                if (added)
                {
                    setDirty(DirtyFlag.ChildStructure);
                }
                nodeEvents.Clear();
                nodeEvents = null;
            }
            clearDirty(DirtyFlag.Structure);
        }

        // http://en.wikipedia.org/wiki/Tree_traversal#Example
        internal override bool visit(Visitor preVisitor, Visitor inVisitor, Visitor postVisitor, Object arg)
        {
            bool cont = true;
            if (preVisitor != null)
            {
                cont = cont && preVisitor(this, ref arg);
            }
            if (cont)
            {
                for (LinkedListNode<Node> it = nodes.First; it != null; it = it.Next)
                {
                    Node childNode = it.Value;
                    childNode.visit(preVisitor, inVisitor, postVisitor, arg);
                    if (inVisitor != null)
                    {
                        inVisitor(childNode, ref arg);
                    }
                }
            }
            if (postVisitor != null)
            {
                postVisitor(this, ref arg);
            }
            return cont;
        }

        #endregion

        private void setStructureDirty()
        {
            setDirty(DirtyFlag.Structure);
            setParentDirty(DirtyFlag.ChildStructure);
        }

        /*
                internal override void setChildDirty(DirtyFlag dirtyFlag, int depth)
                {
                    for (LinkedListNode<Node> it = nodes.First; it != null; it = it.Next)
                    {
                        Node node = it.Value;
                        if (!node.IsDirty(dirtyFlag))
                        {
                            node.setDirty(dirtyFlag);
                            if (depth > 0 || depth == -1)
                            {
                                node.setChildDirty(dirtyFlag, (depth > 0) ? depth - 1 : depth);
                            }
                        }
                    }
                }
        */

        #region Private Methods

            /*
        private void setSceneRecursive(Node node)
        {
            node.Scene = node.ParentNode.Scene;
            GroupNode groupNode = node as GroupNode;
            if (groupNode != null)
            {
                for (LinkedListNode<Node> it = groupNode.Nodes.First; it != null; it = it.Next)
                {
                    setSceneRecursive(it.Value);
                }
            }
        }
        */
        #endregion
    }

}
