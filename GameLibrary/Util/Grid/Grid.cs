using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace GameLibrary.Util.Grid
{
    // See http://hhoppe.com/perfecthash.pdf
    public struct Key
    {
        public static readonly IEqualityComparer<Key> KeyEqualityComparerInstance = new KeyEqualityComparer();

        public int X;
        public int Y;
        public int Z;

        public Key(int X, int Y, int Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        private sealed class KeyEqualityComparer : IEqualityComparer<Key>
        {
            public bool Equals(Key key1, Key key2)
            {
                return ((key1.X == key2.X) && (key1.Y == key2.Y) && (key1.Z == key2.Z));
            }

            public int GetHashCode(Key key)
            {
                int hash = key.X;
                hash = hash * 31 + key.Y;
                hash = hash * 31 + key.Z;
                return hash;
            }
        }
    }

    public sealed class GridItem<T>
    {
        internal T obj;
    }

    public class Grid<T>
    {
        //public OctreeItem<T> RootItem;

        //public Vector3 Center;
        //public Vector3 HalfSize;

        private readonly Dictionary<Key, GridItem<T>> items;

        //public delegate T ObjectFactory(Octree<T> octree, OctreeItem<T> item);

        //public ObjectFactory objectFactory;

        public Grid()
        {
            /*Center = center;
            HalfSize = halfSize;*/
            /*RootItem = new OctreeItem<T>();
            RootItem.locCode = 1;

            items = new Dictionary<ulong, OctreeItem<T>>();

            items.Add(RootItem.locCode, RootItem);
            */
            items = new Dictionary<Key, GridItem<T>>();
        }

        // TODO does not belong here
        public virtual bool LoadItem(GridItem<T> item, ref Object arg)
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
            //visit(RootItem, ref ctxt, ref arg);
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
