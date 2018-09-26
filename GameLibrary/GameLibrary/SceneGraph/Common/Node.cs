using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Control;

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

        public delegate Boolean Visitor(Node node, Object arg);

        #region Fields

        private int id;

        private String name;

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

        public virtual void Remove()
        {
            if (ParentNode == null) throw new Exception("no parent");
            ParentNode.Remove(this);
        }
        
        public void AddController(Controller controller)
        {
            controllers.Add(controller);
        }

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
            visitor(this, arg);
        }

        #endregion
    }
}
