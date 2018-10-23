using GameLibrary.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GameLibrary.Voxel
{
    // 0b +Y -Z +X
    public enum Octant
    {
        BottomLeftFront = 0b000,
        BottomRightFront = 0b001,
        BottomLeftBack = 0b010,
        BottomRightBack = 0b011,
        TopLeftFront = 0b100,
        TopRightFront = 0b101,
        TopLeftBack = 0b110,
        TopRightBack = 0b111
    }

    public class OctreeNode<T>
    {
        internal T obj;
        internal ulong locCode;
        internal uint childExists; // optional
    }

    // https://geidav.wordpress.com/2014/08/18/advanced-octrees-2-node-representations/
    public class Octree<T>
    {
        private const int V = 1;

        //private static readonly OctreeNode<T> NULL_OCTREE_NODE = new OctreeNode<T>();

        private readonly Vector3[] OCTANT_TRANSLATIONS = new Vector3[8];

        public OctreeNode<T> RootNode;

        public Vector3 Center;
        public Vector3 HalfSize;

        private readonly Dictionary<ulong, OctreeNode<T>> nodes;

        public delegate T ObjectFactory(Octree<T> octree, OctreeNode<T> node);

        public ObjectFactory objectFactory;

        public Octree() : this(Vector3.Zero, Vector3.Zero)
        {

        }

        public Octree(Vector3 center, Vector3 halfSize)
        {
            Center = center;
            HalfSize = halfSize;
            RootNode = new OctreeNode<T>();
            //RootNode.map = new FunctionVoxelMap(32);
            RootNode.locCode = 1;
            RootNode.childExists = 0;
            //RootNode.center = Vector3.Zero;
            //GeometryNode node = new MeshNode("VOXEL", new VoxelMapMeshFactory(voxelMap));
            //node.RenderGroupId = Scene.VOXEL;

            nodes = new Dictionary<ulong, OctreeNode<T>>();

            OCTANT_TRANSLATIONS[(int)Octant.BottomLeftFront] = new Vector3(-1, -1, 1);
            OCTANT_TRANSLATIONS[(int)Octant.BottomRightFront] = new Vector3(1, -1, 1);
            OCTANT_TRANSLATIONS[(int)Octant.BottomLeftBack] = new Vector3(-1, -1, -1);
            OCTANT_TRANSLATIONS[(int)Octant.BottomRightBack] = new Vector3(1, -1, -1);
            OCTANT_TRANSLATIONS[(int)Octant.TopLeftFront] = new Vector3(-1, 1, 1);
            OCTANT_TRANSLATIONS[(int)Octant.TopRightFront] = new Vector3(1, 1, 1);
            OCTANT_TRANSLATIONS[(int)Octant.TopLeftBack] = new Vector3(-1, 1, -1);
            OCTANT_TRANSLATIONS[(int)Octant.TopRightBack] = new Vector3(1, 1, -1);
        }

        public OctreeNode<T> GetParentNode(OctreeNode<T> node)
        {
            ulong locCodeParent = node.locCode >> 3;
            return LookupNode(locCodeParent);
        }

        public OctreeNode<T> GetChildNode(OctreeNode<T> node, Octant octant)
        {
            ulong locCodeChild = (node.locCode << 3) | (ulong)octant;
            return LookupNode(locCodeChild);
        }

        public void GetNodeBoundingBox(OctreeNode<T> node, ref SceneGraph.Bounding.BoundingBox boundingBox)
        {
            Vector3 center;
            Vector3 halfSize;
            GetNodeBoundingBox(node, out center, out halfSize);
            boundingBox.Center = center;
            boundingBox.HalfSize = halfSize;
        }

        public void GetNodeBoundingBox(OctreeNode<T> node, out Vector3 center, out Vector3 halfSize)
        {
            center = Center;
            halfSize = HalfSize;

            int depth = GetNodeTreeDepth(node);
            for (int d = depth - 1; d >= 0; d--)
            {
                Octant octant = (Octant)((node.locCode >> (d * 3)) & 0b111);

                halfSize /= 2f;

                Vector3 c = OCTANT_TRANSLATIONS[(int)octant];
                c.X *= halfSize.X;
                c.Y *= halfSize.Y;
                c.Z *= halfSize.Z;
                center += c;
            }
        }

        public void GetNodeHalfSize(OctreeNode<T> node, out Vector3 halfSize)
        {
            int depth = GetNodeTreeDepth(node);
            halfSize = Vector3.Divide(HalfSize, (float)Math.Pow(2, depth));
        }

        public int GetNodeTreeDepth(OctreeNode<T> node)
        {
            // at least flag bit must be set
            Debug.Assert(node.locCode != 0);

            int msbPosition = BitUtil.BitScanReverse(node.locCode);
            return msbPosition / 3;
        }

        public int GetNodeTreeDepthSlow(OctreeNode<T> node)
        {
            // at least flag bit must be set
            Debug.Assert(node.locCode != 0);

            int depth = 0;
            for (ulong lc = node.locCode; lc != 1; lc >>= 3)
            {
                depth++;
            }
            return depth;
        }

        private OctreeNode<T> LookupNode(ulong locCode)
        {
            OctreeNode<T> node;
            if (nodes.TryGetValue(locCode, out node))
            {
                return node;
            }
            return null;
        }

        public bool HasChild(OctreeNode<T> node, Octant octant)
        {
            return ((node.childExists & (1 << (int)octant)) != 0);
        }

        public Octant GetOctant(Vector3 center, Vector3 point)
        {
            Octant octant = 0;
            if (point.X >= center.X) octant = (Octant)(((int)octant) | 0b001);
            if (point.Y >= center.Y) octant = (Octant)(((int)octant) | 0b100);
            if (point.Z < center.Z) octant = (Octant)(((int)octant) | 0b010);
            return octant;
        }

        public OctreeNode<T> AddChild(Vector3 point, int depth)
        {
            // from root down
            // compute center
            // flip bit Front/back if z > 0 etc
            OctreeNode<T> parent = RootNode;

            Vector3 center = Center;
            Vector3 halfSize = HalfSize;

            ulong locCode = 1;
            for (int d = 0; d < depth; d++)
            {
                Octant octant = GetOctant(center, point);

                locCode = (locCode << 3) | (ulong)octant;
                if (HasChild(parent, octant))
                {
                    parent = LookupNode(locCode);
                }
                else
                {
                    parent = AddChild(parent, octant);
                }
                if (d < depth - 1)
                {
                    halfSize /= 2f;

                    Vector3 c = OCTANT_TRANSLATIONS[(int)octant];
                    c.X *= halfSize.X;
                    c.Y *= halfSize.Y;
                    c.Z *= halfSize.Z;
                    center += c;
                }
            }
            return parent;
        }

        public OctreeNode<T> AddChild(OctreeNode<T> parent, Octant octant)
        {
            // TODO check if max depth is attained...
            if (HasChild(parent, octant))
            {
                Debug.Assert(false);
                ulong locCode = (parent.locCode << 3) | (ulong)octant;
                return LookupNode(locCode);
            }
            // create new node
            OctreeNode<T> node = new OctreeNode<T>();
            node.locCode = (parent.locCode << 3) | (ulong)octant;
            node.childExists = 0;

            // create node object
            if (objectFactory != null)
            {
                node.obj = objectFactory(this, node);
            }

            // add new node to parent
            nodes.Add(node.locCode, node);
            parent.childExists |= (uint)(1 << (int)octant);

            return node;
        }

        /*
        def get_neighbor_of_greater_or_equal_size(self, direction):   
  if direction == self.Direction.N:       
    if self.parent is None: # Reached root?
      return None
    if self.parent.children[self.Child.SW] == self: # Is 'self' SW child?
      return self.parent.children[self.Child.NW]
    if self.parent.children[self.Child.SE] == self: # Is 'self' SE child?
      return self.parent.children[self.Child.NE]

    node = self.parent.get_neighbor_of_greater_or_same_size(direction)
    if node is None or node.is_leaf():
      return node
 
    # 'self' is guaranteed to be a north child
    return (node.children[self.Child.SW]
            if self.parent.children[self.Child.NW] == self # Is 'self' NW child?
            else node.children[self.Child.SE])
  else:
    # TODO: implement symmetric to NORTH case


            def find_neighbors_of_smaller_size(self, neighbor, direction):   
  candidates = [] if neighbor is None else [neighbor]
  neighbors = []
 
  if direction == self.Direction.N:
    while len(candidates) > 0:
      if candidates[0].is_leaf():
        neighbors.append(candidates[0])
      else:
        candidates.append(candidates[0].children[self.Child.SW])
        candidates.append(candidates[0].children[self.Child.SE])
 
      candidates.remove(candidates[0])
 
    return neighbors
  else:
    # TODO: implement other directions symmetric to NORTH case




    def get_neighbors(self, direction):   
  neighbor = self.get_neighbor_of_greater_or_equal_size(direction)
  neighbors = self.find_neighbors_of_smaller_size(neighbor, direction)
  return neighbors
            */
        public delegate bool Visitor(Octree<T> octree, OctreeNode<T> node, ref Object arg);

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

        internal void visit(Visitor preVisitor, Visitor inVisitor, Visitor postVisitor, Object arg)
        {
            visit(RootNode, preVisitor, inVisitor, postVisitor, arg);
        }

        internal virtual bool visit(OctreeNode<T> node, Visitor preVisitor, Visitor inVisitor, Visitor postVisitor, Object arg)
        {
            if ((node.childExists == 0) && (node.obj == null))
            {
                // empty node
                return false;
            }
            // TODO iterate over sorted location codes
            bool cont = true;
            if (preVisitor != null)
            {
                cont = cont && preVisitor(this, node, ref arg);
            }
            if (cont && (node.childExists != 0))
            {
                for (ushort i = 0; i < 8; i++)
                {
                    if ((node.childExists & (1L << i)) == 0)
                    {
                        continue;
                    }
                    ulong childLocCode = (node.locCode << 3) | (ulong)i;
                    OctreeNode<T> childNode = LookupNode(childLocCode);
                    if (childNode == null)
                    {
                        continue;
                    }
                    visit(childNode, preVisitor, inVisitor, postVisitor, arg);
                    if (inVisitor != null)
                    {
                        inVisitor(this, childNode, ref arg);
                    }
                }
            }
            if (postVisitor != null)
            {
                postVisitor(this, node, ref arg);
            }
            return cont;
        }

    }
}
