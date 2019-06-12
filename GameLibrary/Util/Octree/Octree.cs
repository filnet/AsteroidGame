using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static GameLibrary.Util.DirectionConstants;

namespace GameLibrary.Util.Octree
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

    public sealed class OctreeNode<T>
    {
        internal T obj;
        internal ulong locCode;
        internal uint childExists; // optional
    }

    // https://geidav.wordpress.com/2014/08/18/advanced-octrees-2-node-representations/
    // When sorting the nodes by locational code the resulting order is the same as the pre-order traversal of the Octree,
    // which in turn is equivalent to the Morton Code (also known as Z-Order Curve).
    // The Morton Code linearly indexes multi-dimensional data, preserving data locality on multiple levels.
    // https://en.wikipedia.org/wiki/Z-order_curve
    public class Octree<T>
    {
        private sealed class Constants
        {
            public static readonly ulong LEAF = 0b111;
            public static readonly ulong PARENT = ~LEAF;

            /*public static readonly int X = 0b001;
            public static readonly int Y = 0b100;
            public static readonly int Z = 0b010;
            public static readonly int XY = X | Y;
            public static readonly int YZ = Y | Z;
            public static readonly int XZ = X | Z;
            public static readonly int XYZ = X | Y | Z;

            public static readonly int LEFT = 1 << 5;
            public static readonly int RIGHT = 1 << 4;
            public static readonly int BOTTOM = 1 << 3;
            public static readonly int TOP = 1 << 2;
            public static readonly int BACK = 1 << 1; // !!!
            public static readonly int FRONT = 1 << 0; // !!!*/
        }

        private static readonly Vector3[] OCTANT_VECTORS = new Vector3[] {
            new Vector3(-1, -1, +1), // BottomLeftFront
            new Vector3(+1, -1, +1), // BottomRightFront
            new Vector3(-1, -1, -1), // BottomLeftBack
            new Vector3(+1, -1, -1), // BottomRightBack
            new Vector3(-1, +1, +1), // TopLeftFront
            new Vector3(+1, +1, +1), // TopRightFront
            new Vector3(-1, +1, -1), // TopLeftBack
            new Vector3(+1, +1, -1), // TopRightBack
        };

        private static readonly int[,] OCTANT_DELTAS = new int[,] {
            { -1, -1, +1 }, // BottomLeftFront
            { +1, -1, +1 }, // BottomRightFront
            { -1, -1, -1 }, // BottomLeftBack
            { +1, -1, -1 }, // BottomRightBack
            { -1, +1, +1 }, // TopLeftFront
            { +1, +1, +1 }, // TopRightFront
            { -1, +1, -1 }, // TopLeftBack
            { +1, +1, -1 }, // TopRightBack
        };

        // all octant visit order permutations
        // for each 48 visit orders gives the 8 octants in appropriate order
        private static readonly Octant[] OCTANT_VISIT_ORDER = computeVisitOrderPermutations();

        public delegate T ObjectFactory(Octree<T> octree, OctreeNode<T> node);

        public delegate bool Visitor<K>(Octree<K> octree, OctreeNode<K> node, Object arg);

        public enum VisitType
        {
            // Preorder traversal (http://en.wikipedia.org/wiki/Tree_traversal#Example)
            PreOrder,
            InOrder,
            PostOrder
        }

        public Vector3 Center;
        public Vector3 HalfSize;

        public OctreeNode<T> RootNode { get; }

        public ObjectFactory objectFactory;

        private readonly Dictionary<ulong, OctreeNode<T>> nodes;

        public Octree(Vector3 center, Vector3 halfSize)
        {
            Center = center;
            HalfSize = halfSize;
            RootNode = new OctreeNode<T>();
            RootNode.locCode = 1;

            nodes = new Dictionary<ulong, OctreeNode<T>>();

            nodes.Add(RootNode.locCode, RootNode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ParentLocCode(ulong locCode)
        {
            return locCode >> 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ChildLocCode(ulong locCode, Octant octant)
        {
            return (locCode << 3) | (ulong)octant;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SiblingLocCode(ulong locCode, Octant octant)
        {
            return (locCode & Constants.PARENT) | (ulong)octant;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasChild(OctreeNode<T> node, Octant octant)
        {
            return ((node.childExists & (1 << (int)octant)) != 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasChildren(OctreeNode<T> node)
        {
            return (node.childExists != 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OctreeNode<T> GetChildNode(OctreeNode<T> node, Octant octant)
        {
            ulong childLocCode = ChildLocCode(node.locCode, octant);
            return LookupNode(childLocCode);
        }

        public static String LocCodeToString(ulong locCode)
        {
            return Convert.ToString((long)locCode, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Octant GetOctant(ulong locCode)
        {
            return (Octant)(locCode & Constants.LEAF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetNodeTreeDepth(OctreeNode<T> node)
        {
            // at least flag bit must be set
            Debug.Assert(node.locCode != 0);

            int msbPosition = BitUtil.BitScanReverse(node.locCode);
            return msbPosition / 3;
        }

        /*
                public static int GetNodeTreeDepth2(OctreeNode<T> node)
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
        */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OctreeNode<T> LookupNode(ulong locCode)
        {
            nodes.TryGetValue(locCode, out OctreeNode<T> node);
            return node;
        }

        // TODO does not belong here
        public virtual bool LoadNode(OctreeNode<T> node, ref Object arg)
        {
            throw new NotImplementedException();
        }

        // TODO does not belong here
        public virtual void ClearLoadQueue()
        {
            throw new NotImplementedException();
        }

        public void GetNodeHalfSize(OctreeNode<T> node, out Vector3 halfSize)
        {
            int depth = GetNodeTreeDepth(node);
            halfSize = Vector3.Divide(HalfSize, (float)Math.Pow(2, depth));
        }

        protected void GetNodeBoundingBox(OctreeNode<T> node, ref SceneGraph.Bounding.Box boundingBox)
        {
            Vector3 center;
            Vector3 halfSize;
            GetNodeBoundingBox(node, out center, out halfSize);
            boundingBox.Center = center;
            boundingBox.HalfSize = halfSize;
        }

        protected void GetNodeBoundingBox(OctreeNode<T> node, out Vector3 center, out Vector3 halfSize)
        {
            center = Center;
            halfSize = HalfSize;

            // TODO compute x,y,z (in voxel space) by de-interlacing loc code
            int depth = GetNodeTreeDepth(node);
            ulong locCode = node.locCode;
            for (int d = depth - 1; d >= 0; d--)
            {
                Octant octant = GetOctant(node.locCode >> (d * 3));

                halfSize /= 2f;

                Vector3 c = OCTANT_VECTORS[(int)octant];
                c.X *= halfSize.X;
                c.Y *= halfSize.Y;
                c.Z *= halfSize.Z;
                center.X += c.X;
                center.Y += c.Y;
                center.Z += c.Z;
            }
        }

        public void GetNodeCoordinates(OctreeNode<T> node, out int x, out int y, out int z)
        {
            // TODO compute x,y,z (in voxel space) by de-interlacing loc code
            int depth = GetNodeTreeDepth(node);
            // FIX should use max depth... the following will compute the "local" coordinates at the depth of the node
            int h2 = (int)HalfSize.X; // MathUtil.Pow(2, depth);
            x = 0;
            y = 0;
            z = 0;
            ulong locCode = node.locCode;
            for (int d = depth - 1; d >= 0; d--)
            {
                h2 /= 2;
                Octant octant = GetOctant(node.locCode >> (d * 3));

                x += OCTANT_DELTAS[(int)octant, 0] * h2;
                y += OCTANT_DELTAS[(int)octant, 1] * h2;
                z += OCTANT_DELTAS[(int)octant, 2] * h2;
            }
            x -= h2;
            y -= h2;
            z -= h2;
        }

        public OctreeNode<T> AddChild(OctreeNode<T> parent, Octant octant)
        {
            // TODO check if max depth is attained...
            if (HasChild(parent, octant))
            {
                Debug.Assert(false);
                ulong locCode = ChildLocCode(parent.locCode, octant);
                return LookupNode(locCode);
            }
            // create octree node
            OctreeNode<T> node = new OctreeNode<T>();

            node.locCode = ChildLocCode(parent.locCode, octant);
            node.childExists = 0;

            // create node object
            T obj = default(T);
            if (objectFactory != null)
            {
                obj = objectFactory(this, node);
                node.obj = obj;
            }
            if (obj == null)
            {
                return null;
            }

            // add new node to parent
            nodes.Add(node.locCode, node);
            parent.childExists |= (uint)(1 << (int)octant);

            return node;
        }

        public OctreeNode<T> AddChild(Vector3 point, int depth)
        {
            OctreeNode<T> parent = RootNode;

            Vector3 center = Center;
            Vector3 halfSize = HalfSize;

            // TODO compute loc code by interlacing x, y, z bits (in voxel world)
            ulong locCode = 1;
            for (int d = 0; d < depth; d++)
            {
                Octant octant = GetOctantForPoint(center, point);

                locCode = ChildLocCode(locCode, octant);
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
                    halfSize.X /= 2f;
                    halfSize.Y /= 2f;
                    halfSize.Z /= 2f;

                    Vector3 c = OCTANT_VECTORS[(int)octant];
                    c.X *= halfSize.X;
                    c.Y *= halfSize.Y;
                    c.Z *= halfSize.Z;
                    center.X += c.X;
                    center.Y += c.Y;
                    center.Z += c.Z;
                }
            }
            return parent;
        }

        public static Octant GetOctantForPoint(Vector3 center, Vector3 point)
        {
            Octant octant = 0;
            if (point.X >= center.X) octant = (Octant)(((int)octant) | Mask.X);
            if (point.Y >= center.Y) octant = (Octant)(((int)octant) | Mask.Y);
            if (point.Z <= center.Z) octant = (Octant)(((int)octant) | Mask.Z);
            return octant;
        }

        public ulong GetNeighborOfGreaterOrEqualSize(ulong nodeLocCode, Direction direction)
        {
            DirData dirData = DirData.Get(direction);
            return getNeighborOfGreaterOrEqualSize(nodeLocCode, dirData.mask, dirData.value);
        }

        // https://geidav.wordpress.com/2017/12/02/advanced-octrees-4-finding-neighbor-nodes/
        private ulong getNeighborOfGreaterOrEqualSize(ulong nodeLocCode, int mask, int value)
        {
            if (nodeLocCode == 1)
            {
                // reached root
                return 0;
            }

            Octant nodeOctant = GetOctant(nodeLocCode);

            ulong parentLocCode = ParentLocCode(nodeLocCode);

            // check if neighbour is in same parent node
            Octant neighbourOctant = (Octant)((int)nodeOctant ^ mask);
            if (((int)neighbourOctant & mask) == value)
            {
                OctreeNode<T> parent = LookupNode(parentLocCode);
                return HasChild(parent, neighbourOctant) ? ChildLocCode(parentLocCode, neighbourOctant) : 0;
            }

            // not the case, go up...
            // TODO rename l to something more meaningful
            // TODO explain logic / give an example
            // find direction of neighbour search in parent
            int v = ((int)neighbourOctant & mask) ^ value;
            int n_mask = mask & v;
            int n_value = value & n_mask;
            ulong l = getNeighborOfGreaterOrEqualSize(parentLocCode, n_mask, n_value);
            if (l == 0)
            {
                return 0;
            }
            // node l is guaranteed to contain the neighbourg...
            OctreeNode<T> n = LookupNode(l);
            if (n.childExists == 0)
            {
                // leaf node
                return l;
            }
            return HasChild(n, neighbourOctant) ? ChildLocCode(l, neighbourOctant) : l;
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

        public void Visit(int visitOrder, Visitor<T> visitor, Object arg)
        {
            Visit(visitOrder, visitor, VisitType.PreOrder, arg);
        }

        public virtual void Visit(int visitOrder, Visitor<T> visitor, VisitType visitType, Object arg)
        {
            switch (visitType)
            {
                case VisitType.PreOrder:
                    visit(visitOrder, visitor, null, null, arg);
                    break;
                case VisitType.InOrder:
                    visit(visitOrder, null, visitor, null, arg);
                    break;
                case VisitType.PostOrder:
                    visit(visitOrder, null, null, visitor, arg);
                    break;
            }
        }

        public void Visit(int visitOrder, Visitor<T> preVisitor, Visitor<T> inVisitor, Visitor<T> postVisitor, Object arg)
        {
            visit(visitOrder, preVisitor, inVisitor, postVisitor, arg);
        }

        internal void visit(int visitOrder, Visitor<T> preVisitor, Visitor<T> inVisitor, Visitor<T> postVisitor, Object arg)
        {
            VisitContext ctxt = new VisitContext();
            ctxt.visitOrder = visitOrder;
            ctxt.preVisitor = preVisitor;
            ctxt.inVisitor = inVisitor;
            ctxt.postVisitor = postVisitor;
            visit(RootNode, ref ctxt, arg);
        }

        internal struct VisitContext
        {
            internal int visitOrder;
            internal Visitor<T> preVisitor;
            internal Visitor<T> inVisitor;
            internal Visitor<T> postVisitor;
        }

        internal virtual bool visit(OctreeNode<T> node, ref VisitContext ctxt, Object arg)
        {
            if ((node.childExists == 0) && (node.obj == null))
            {
                // empty node
                return false;
            }
            // TODO iterate over sorted location codes
            bool cont = true;
            if (ctxt.preVisitor != null)
            {
                cont = ctxt.preVisitor(this, node, arg);
            }
            if (cont && (node.childExists != 0))
            {
                int permutationIndex = ctxt.visitOrder * 8;
                for (ushort i = 0; i < 8; i++)
                {
                    Octant octant = (Octant)OCTANT_VISIT_ORDER[permutationIndex++];
                    if (!HasChild(node, octant))
                    {
                        continue;
                    }
                    //ulong childLocCode = (node.locCode << 3) | (ulong)octant;
                    //OctreeNode<T> childNode = LookupNode(childLocCode);
                    OctreeNode<T> childNode = GetChildNode(node, octant);
                    /*
                    if (childNode == null)
                    {
                        continue;
                    }*/
                    visit(childNode, ref ctxt, arg);
                    ctxt.inVisitor?.Invoke(this, childNode, arg);
                }
            }
            ctxt.postVisitor?.Invoke(this, node, arg);
            return true;
        }

        private static Octant[] computeVisitOrderPermutations()
        {
            // for each 48 visit order, list the 8 octants in the corresponding visit order
            Octant[] permutations = new Octant[48 * 8];
            int index = 0;
            for (int order = 0; order < 48; order++)
            {
                // extract permutation and signs from visit order
                int perm = order >> 3;
                // signs are pre-permuted and, so must be applied after permutating
                int signs = (order & 0b111);

                // loop over all 8 octants in +x +y +z order
                for (int i = 0; i < 8; i++)
                {
                    int x = (i & 0b100) >> 2;
                    int y = (i & 0b010) >> 1;
                    int z = (i & 0b001) >> 0;

                    // handle signs
                    x = x ^ ((signs & 0b100) >> 2);
                    y = y ^ ((signs & 0b010) >> 1);
                    z = z ^ ((signs & 0b001) >> 0);

                    // handle permutation
                    VectorUtil.INV_PERMUTATIONS[perm](x, y, z, out x, out y, out z);


                    //FAILS when camera pointing +/-z (perm 4 and 5)
                    //check if y is ok (x is...)


                    // reconstruct octant and store it
                    // octant are coded as such 0b +y -z +x
                    int o = (y << 2) | ((1 - z) << 1) | x;
                    permutations[index++] = (Octant)o;
                }
            }
            return permutations;
        }

    }
}
