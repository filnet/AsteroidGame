using Microsoft.Xna.Framework;
using System;

namespace GameLibrary.Util.Grid
{
    public sealed class GridItem<T>
    {
        internal T obj;
    }

    public class Grid<T>
    {
        //public OctreeNode<T> RootNode;

        //public Vector3 Center;
        //public Vector3 HalfSize;

        //private readonly Dictionary<ulong, OctreeNode<T>> nodes;

        //public delegate T ObjectFactory(Octree<T> octree, OctreeNode<T> node);

        //public ObjectFactory objectFactory;

        public Grid(Vector3 center, Vector3 halfSize)
        {
            /*Center = center;
            HalfSize = halfSize;
            RootNode = new OctreeNode<T>();
            RootNode.locCode = 1;

            nodes = new Dictionary<ulong, OctreeNode<T>>();

            nodes.Add(RootNode.locCode, RootNode);
            */
        }

        // TODO does not belong here
        public virtual bool LoadNode(GridItem<T> node, ref Object arg)
        {
            // NOOP
            return true;
        }

        // TODO does not belong here
        public virtual void ClearLoadQueue()
        {
            // NOOP
        }


        public delegate bool Visitor<K>(Grid<K> grid, GridItem<K> item, ref Object arg);

        public enum VisitType
        {
            // Preorder traversal (http://en.wikipedia.org/wiki/Tree_traversal#Example)
            PreOrder,
            InOrder,
            PostOrder
        }

        public void Visit(int visitOrder, Visitor<T> visitor, ref Object arg)
        {
            Visit(visitOrder, visitor, VisitType.PreOrder, ref arg);
        }

        public virtual void Visit(int visitOrder, Visitor<T> visitor, VisitType visitType, ref Object arg)
        {
            switch (visitType)
            {
                case VisitType.PreOrder:
                    visit(visitOrder, visitor, null, null, ref arg);
                    break;
                case VisitType.InOrder:
                    visit(visitOrder, null, visitor, null, ref arg);
                    break;
                case VisitType.PostOrder:
                    visit(visitOrder, null, null, visitor, ref arg);
                    break;
            }
        }

        public void Visit(int visitOrder, Visitor<T> preVisitor, Visitor<T> inVisitor, Visitor<T> postVisitor, ref Object arg)
        {
            visit(visitOrder, preVisitor, inVisitor, postVisitor, ref arg);
        }

        internal void visit(int visitOrder, Visitor<T> preVisitor, Visitor<T> inVisitor, Visitor<T> postVisitor, ref Object arg)
        {
            VisitContext ctxt = new VisitContext();
            ctxt.visitOrder = visitOrder;
            ctxt.preVisitor = preVisitor;
            ctxt.inVisitor = inVisitor;
            ctxt.postVisitor = postVisitor;
            //visit(RootNode, ref ctxt, ref arg);
        }

        internal struct VisitContext
        {
            internal int visitOrder;
            internal Visitor<T> preVisitor;
            internal Visitor<T> inVisitor;
            internal Visitor<T> postVisitor;
        }

        internal virtual bool visit(GridItem<T> item, ref VisitContext ctxt, ref Object arg)
        {
            return true;
        }

    }
}
