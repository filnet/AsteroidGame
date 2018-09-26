﻿using System;
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

        private BoundingVolume boundingVolume;

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
        public virtual BoundingVolume BoundingVolume
        {
            get { return boundingVolume; }
            internal set { boundingVolume = value; }
        }

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
            nodes = new LinkedList<Node>();
            BoundingVolumeVisible = true;
        }

        public GroupNode(GroupNode node)
            : base(node)
        {
            boundingVolume = node.boundingVolume != null ? node.boundingVolume.Clone() : null;
            BoundingVolumeVisible = node.BoundingVolumeVisible;
            nodes = new LinkedList<Node>();
            for (LinkedListNode<Node> it = node.Nodes.First; it != null; it = it.Next)
            {
                Add(it.Value.Clone());
            }
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
            nodeEvents.AddLast(new NodeEvent(EventType.ADDED, node));
        }

        public void AddFirst(Node node)
        {
            if (nodeEvents == null)
            {
                nodeEvents = new LinkedList<NodeEvent>();
            }
            nodeEvents.AddLast(new NodeEvent(EventType.ADDED_FIRST, node));
        }

        public void Remove(Node node)
        {
            if (nodeEvents == null)
            {
                nodeEvents = new LinkedList<NodeEvent>();
            }
            nodeEvents.AddLast(new NodeEvent(EventType.REMOVED, node));
        }

        public void Commit()
        {
            if (nodeEvents != null)
            {
                for (LinkedListNode<NodeEvent> it = nodeEvents.First; it != null; it = it.Next)
                {
                    NodeEvent evt = it.Value;
                    Node node = evt.Node;
                    switch (evt.Type)
                    {
                        case EventType.ADDED:
                            node.ParentNode = this;
                            setSceneRecursive(node);
                            nodes.AddLast(node);
                            node.Initialize();
                            break;
                        case EventType.ADDED_FIRST:
                            node.ParentNode = this;
                            setSceneRecursive(node);
                            nodes.AddFirst(node);
                            node.Initialize();
                            break;
                        case EventType.REMOVED:
                            node.ParentNode = null;
                            nodes.Remove(node);
                            node.Dispose();
                            break;
                    }
                }
                nodeEvents.Clear();
                nodeEvents = null;
            }
        }

        public override void Visit(Visitor visitor, VisitType visitType, Object arg)
        {
            if (visitType == VisitType.PreOrder)
            {
                Boolean propagate = visitor(this, arg);
                if (!propagate) return;
            }
            for (LinkedListNode<Node> it = nodes.First; it != null; it = it.Next)
            {
                Node node = it.Value;
                node.Visit(visitor, visitType, arg);
            }
            if (visitType == VisitType.PostOrder)
            {
                Boolean propagate = visitor(this, arg);
                if (!propagate) return;
            }
        }

        #endregion

        #region Private Methods

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

        #endregion
    }

}
