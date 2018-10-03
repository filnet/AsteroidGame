using System;
using System.Collections.Generic;
using GameLibrary.Control;
using GameLibrary.Util;

namespace GameLibrary.SceneGraph.Common
{
    public abstract class Node
    {
        private static int nextNodeID;

        internal static int GetNextNodeID()
        {
            nextNodeID++;
            return nextNodeID;
        }

        public enum DirtyFlag : int { Transform, ChildTransform, WorldTransform, ChildWorldTransform };

        #region Fields

        private int id;

        private String name;

        private int dirty;

        private List<Controller> controllers;

        #endregion

        #region Properties

        public int Id { get { return id; } }

        public String Name { get { return name; } }

        public Boolean Enabled { get; set; }

        public Boolean Visible { get; set; }

        public GroupNode ParentNode { get; set; }

        internal Scene Scene { get; set; }

        public List<Controller> Controllers
        {
            get { return controllers; }
        }

        #endregion

        #region Constructors

        protected Node(String name)
        {
            id = GetNextNodeID();
            this.name = name;
            if (this.name == null)
            {
                name = "NODE_" + id;
            }
            Enabled = true;
            Visible = true;
            this.controllers = new List<Controller>();
        }

        protected Node(Node node)
        {
            id = GetNextNodeID();
            if (node.name == null)
            {
                name = "NODE_#" + id;
            }
            else
            {
                name = node.name + "#" + id;
            }
            Enabled = node.Enabled;
            Visible = node.Visible;
            // 
            Scene = node.Scene;
            // don't clone controllers !?
            controllers = new List<Controller>();
        }

        #endregion

        #region Public Methods

        public abstract Node Clone();

        public virtual void Initialize()
        {
            Console.Out.WriteLine("Initializing " + name);
        }

        public virtual void Dispose()
        {
        }

        public bool IsDirty(DirtyFlag dirtyFlag)
        {
            return isDirty(dirtyFlag);
        }

        public void ClearDirty(DirtyFlag dirtyFlag)
        {
            clearDirty(dirtyFlag);
        }

        public String DirtyFlagsAsString()
        {
            return dirty.ToBinaryString();
        }

        protected bool isDirty(DirtyFlag dirtyFlag)
        {
            return dirty.IsBitSet((int) dirtyFlag);
        }

        internal void setDirty(DirtyFlag dirtyFlag)
        {
            dirty = dirty.SetBit((int) dirtyFlag);
        }

        internal void clearDirty(DirtyFlag dirtyFlag)
        {
            dirty = dirty.UnsetBit((int) dirtyFlag);
        }

        protected void setParentDirty(DirtyFlag dirtyFlag)
        {
            if (!Enabled)
            {
                return;
            }
            if ((ParentNode != null) && !ParentNode.isDirty(dirtyFlag))
            {
                ParentNode.setDirty(dirtyFlag);
                ParentNode.setParentDirty(dirtyFlag);
            }
        }

        internal void setChildDirty(DirtyFlag dirtyFlag)
        {
            setChildDirty(dirtyFlag, -1);
        }

        internal virtual void setChildDirty(DirtyFlag dirtyFlag, int depth)
        {
        }

        public virtual void Remove()
        {
            if (ParentNode == null) throw new Exception("no parent");
            ParentNode.Remove(this);
        }

        public void AddController(Controller controller)
        {
            controllers.Add(controller);
        }

        public delegate bool Visitor(Node node, ref Object arg);

        public enum VisitType
        {
            // Preorder traversal (http://en.wikipedia.org/wiki/Tree_traversal#Example)
            PreOrder,
            InOrder,
            PostOrder
        }

        public void Visit(Visitor visitor)
        {
            Visit(visitor, VisitType.PreOrder, null);
        }

        public void Visit(Visitor visitor, Object arg)
        {
            Visit(visitor, VisitType.PreOrder, arg);
        }

        public virtual void Visit(Visitor visitor, VisitType visitType, Object arg)
        {
            switch (visitType)
            {
                case VisitType.PreOrder:
                    visit(visitor, null, null, arg);
                    break;
                case VisitType.InOrder:
                    visit(null, visitor, null, arg);
                    break;
                case VisitType.PostOrder:
                    visit(null, null, visitor, arg);
                    break;
            }
        }

        public void Visit(Visitor preVisitor, Visitor inVisitor, Visitor postVisitor)
        {
            visit(preVisitor, inVisitor, postVisitor, null);
        }

        public void Visit(Visitor preVisitor, Visitor inVisitor, Visitor postVisitor, Object arg)
        {
            visit(preVisitor, inVisitor, postVisitor, arg);
        }

        internal virtual bool visit(Visitor preVisitor, Visitor inVisitor, Visitor postVisitor, Object arg)
        {
            bool cont = true;
            if (preVisitor != null)
            {
                cont &= preVisitor(this, ref arg);
            }
            if (postVisitor != null)
            {
                cont &= postVisitor(this, ref arg);
            }
            return cont;
        }

        #endregion
    }
}
