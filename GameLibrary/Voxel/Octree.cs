using GameLibrary.Component.Util;
using GameLibrary.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GameLibrary.Voxel
{
    public sealed class Mask
    {
        public static readonly ulong LEAF = 0b111;
        public static readonly ulong PARENT = ~LEAF;

        public static readonly int X = 0b001;
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
        public static readonly int BACK = 1 << 1;
        public static readonly int FRONT = 1 << 0;
    }

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

    public enum Direction
    {
        // 6-connected
        Left, Right, Bottom, Top, Back, Front,
        // 18-connected
        BottomLeft, BottomRight, BottomFront, BottomBack,
        LeftFront, RightFront, LeftBack, RightBack,
        TopLeft, TopRight, TopFront, TopBack,
        // 26-connected
        BottomLeftFront, BottomRightFront, BottomLeftBack, BottomRightBack,
        TopLeftFront, TopRightFront, TopLeftBack, TopRightBack,
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

        private static readonly int LEFT = 0b000;
        private static readonly int RIGHT = 0b001;
        private static readonly int BOTTOM = 0b000;
        private static readonly int TOP = 0b100;
        private static readonly int BACK = 0b010;
        private static readonly int FRONT = 0b000;

        public sealed class DirData
        {
            public readonly Direction dir;
            public readonly int mask;
            public readonly int value;
            public readonly int dX;
            public readonly int dY;
            public readonly int dZ;
            public readonly int lookupIndex;
            public DirData(Direction dir, int mask, int value)
            {
                this.dir = dir;
                this.mask = mask;
                this.value = value;
                // x, y, z deltas
                dX = ((mask & Mask.X) != 0) ? ((value & Mask.X) != 0) ? +1 : -1 : 0;
                dY = ((mask & Mask.Y) != 0) ? ((value & Mask.Y) != 0) ? +1 : -1 : 0;
                dZ = ((mask & Mask.Z) != 0) ? ((value & Mask.Z) != 0) ? -1 : +1 : 0;
                // lookup index is the bit sequence LEFT RIGHT BOTTOM TOP BACK FRONT
                // some lookup indices are invalid (0b111111 for example)
                lookupIndex = ((mask & Mask.X) != 0) ? ((value & Mask.X) != 0) ? 0b01 : 0b10 : 0;
                lookupIndex = (lookupIndex << 2) | (((mask & Mask.Y) != 0) ? ((value & Mask.Y) != 0) ? 0b01 : 0b10 : 0);
                lookupIndex = (lookupIndex << 2) | (((mask & Mask.Z) != 0) ? ((value & Mask.Z) != 0) ? 0b10 : 0b01 : 0);
            }
        }

        public static readonly DirData[] DIR_DATA = new DirData[] {
            // 6-connected
            new DirData(Direction.Left, Mask.X, LEFT),
            new DirData(Direction.Right, Mask.X, RIGHT),
            new DirData(Direction.Bottom, Mask.Y, BOTTOM),
            new DirData(Direction.Top, Mask.Y, TOP),
            new DirData(Direction.Back, Mask.Z, BACK),
            new DirData(Direction.Front, Mask.Z, FRONT),
            // 18-connected
            new DirData(Direction.BottomLeft, Mask.XY, BOTTOM | LEFT),
            new DirData(Direction.BottomRight, Mask.XY, BOTTOM | RIGHT),
            new DirData(Direction.BottomFront, Mask.YZ, BOTTOM | FRONT),
            new DirData(Direction.BottomBack, Mask.YZ, BOTTOM | BACK),
            new DirData(Direction.LeftFront, Mask.XZ, LEFT | FRONT),
            new DirData(Direction.RightFront, Mask.XZ, RIGHT | FRONT),
            new DirData(Direction.LeftBack, Mask.XZ, LEFT | BACK),
            new DirData(Direction.RightBack, Mask.XZ, RIGHT | BACK),
            new DirData(Direction.TopLeft, Mask.XY, TOP | LEFT),
            new DirData(Direction.TopRight, Mask.XY, TOP | RIGHT),
            new DirData(Direction.TopFront, Mask.YZ, TOP | FRONT),
            new DirData(Direction.TopBack, Mask.YZ, TOP | BACK),
            // 26-connected
            new DirData(Direction.BottomLeftFront, Mask.XYZ, BOTTOM | LEFT | FRONT),
            new DirData(Direction.BottomRightFront, Mask.XYZ, BOTTOM | RIGHT | FRONT),
            new DirData(Direction.BottomLeftBack, Mask.XYZ, BOTTOM | LEFT | BACK),
            new DirData(Direction.BottomRightBack, Mask.XYZ, BOTTOM | RIGHT | BACK),
            new DirData(Direction.TopLeftFront, Mask.XYZ, TOP | LEFT | FRONT),
            new DirData(Direction.TopRightFront, Mask.XYZ, TOP | RIGHT | FRONT),
            new DirData(Direction.TopLeftBack, Mask.XYZ, TOP | LEFT | BACK),
            new DirData(Direction.TopRightBack, Mask.XYZ, TOP | RIGHT | BACK),
        };

        // all octant visit order permutations
        // for each 48 visit orders gives the 8 octants in appropriate order
        private static readonly Octant[,] OCTANT_VISIT_ORDER = computeVisitOrderPermutations();

        // gives the Direction by lookup index
        // lookup index is the bit sequence LEFT RIGHT BOTTOM TOP BACK FRONT
        // some lookup indices are invalid (0b111111 for example)
        public static readonly Direction[] DIR_LOOKUP_TABLE = computeDirectionLookupTable();

        public OctreeNode<T> RootNode;

        public Vector3 Center;
        public Vector3 HalfSize;

        private readonly Dictionary<ulong, OctreeNode<T>> nodes;

        public delegate T ObjectFactory(Octree<T> octree, OctreeNode<T> node);

        public ObjectFactory objectFactory;

        public Octree(Vector3 center, Vector3 halfSize)
        {
            Center = center;
            HalfSize = halfSize;
            RootNode = new OctreeNode<T>();
            RootNode.locCode = 1;

            nodes = new Dictionary<ulong, OctreeNode<T>>();

            nodes.Add(RootNode.locCode, RootNode);
        }

        public static ulong ParentLocCode(ulong locCode)
        {
            return locCode >> 3;
        }

        public static ulong ChildLocCode(ulong locCode, Octant octant)
        {
            return (locCode << 3) | (ulong)octant;
        }

        public static bool HasChild(OctreeNode<T> node, Octant octant)
        {
            return ((node.childExists & (1 << (int)octant)) != 0);
        }

        public static bool HasChildren(OctreeNode<T> node)
        {
            return (node.childExists != 0);
        }

        public OctreeNode<T> GetChildNode(OctreeNode<T> node, Octant octant)
        {
            ulong childLocCode = ChildLocCode(node.locCode, octant);
            return LookupNode(childLocCode);
        }

        public static String LocCodeToString(ulong locCode)
        {
            return Convert.ToString((long)locCode, 2);
        }

        // TODO does not belong here
        public virtual void LoadNode(OctreeNode<T> node, ref Object arg)
        {
            // NOOP
        }

        // TODO does not belong here
        public virtual void ClearLoadQueue()
        {
            // NOOP
        }

        protected void GetNodeBoundingBox(OctreeNode<T> node, ref SceneGraph.Bounding.BoundingBox boundingBox)
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
                Octant octant = (Octant)((node.locCode >> (d * 3)) & Mask.LEAF);

                halfSize /= 2f;

                Vector3 c = OCTANT_VECTORS[(int)octant];
                c.X *= halfSize.X;
                c.Y *= halfSize.Y;
                c.Z *= halfSize.Z;
                center += c;
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
                Octant octant = (Octant)((node.locCode >> (d * 3)) & Mask.LEAF);

                x += OCTANT_DELTAS[(int)octant, 0] * h2;
                y += OCTANT_DELTAS[(int)octant, 1] * h2;
                z += OCTANT_DELTAS[(int)octant, 2] * h2;
            }
            x -= h2;
            y -= h2;
            z -= h2;
        }

        public static Octant GetOctant(OctreeNode<T> node)
        {
            return GetOctant(node.locCode);
        }

        public static Octant GetOctant(ulong locCode)
        {
            return (Octant)(locCode & Mask.LEAF);
        }

        public void GetNodeHalfSize(OctreeNode<T> node, out Vector3 halfSize)
        {
            int depth = GetNodeTreeDepth(node);
            halfSize = Vector3.Divide(HalfSize, (float)Math.Pow(2, depth));
        }

        public static int GetNodeTreeDepth(OctreeNode<T> node)
        {
            // at least flag bit must be set
            Debug.Assert(node.locCode != 0);

            int msbPosition = BitUtil.BitScanReverse(node.locCode);
            return msbPosition / 3;
        }

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

        public OctreeNode<T> LookupNode(ulong locCode)
        {
            if (locCode == 1)
            {
                return RootNode;
            }
            OctreeNode<T> node;
            if (nodes.TryGetValue(locCode, out node))
            {
                return node;
            }
            return null;
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
                    center += c;
                }
            }
            return parent;
        }

        public static Octant GetOctantForPoint(Vector3 center, Vector3 point)
        {
            Octant octant = 0;
            if (point.X >= center.X) octant = (Octant)(((int)octant) | Mask.X);
            if (point.Y >= center.Y) octant = (Octant)(((int)octant) | Mask.Y);
            if (point.Z < center.Z) octant = (Octant)(((int)octant) | Mask.Z);
            return octant;
        }

        public ulong GetNeighborOfGreaterOrEqualSize(ulong nodeLocCode, Direction direction)
        {
            //String nodeLocCodeString = Convert.ToString((long)nodeLocCode, 2);
            if (nodeLocCode == 1)
            {
                // reached root
                return 0;
            }

            DirData dirData = DIR_DATA[(int)direction];
            Octant nodeOctant = (Octant)(nodeLocCode & Mask.LEAF);

            ulong parentLocCode = ParentLocCode(nodeLocCode);

            // check if neighbour is in same parent node
            Octant neighbourOctant = (Octant)((int)nodeOctant ^ dirData.mask);
            if (((int)neighbourOctant & dirData.mask) == dirData.value)
            {
                OctreeNode<T> parent = LookupNode(parentLocCode);
                return HasChild(parent, neighbourOctant) ? (nodeLocCode & Mask.PARENT) | (ulong)neighbourOctant : 0;
            }
            // not the case, go up...
            // TODO rename l to something more meaningful
            ulong l = GetNeighborOfGreaterOrEqualSize(parentLocCode, direction);
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
            return HasChild(n, neighbourOctant) ? (l << 3) | (ulong)neighbourOctant : 0;
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

        public delegate bool Visitor<K>(Octree<K> octree, OctreeNode<K> node, ref Object arg);

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
            visit(RootNode, ref ctxt, ref arg);
        }

        internal struct VisitContext
        {
            internal int visitOrder;
            internal Visitor<T> preVisitor;
            internal Visitor<T> inVisitor;
            internal Visitor<T> postVisitor;
        }

        internal virtual bool visit(OctreeNode<T> node, ref VisitContext ctxt, ref Object arg)
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
                cont = ctxt.preVisitor(this, node, ref arg);
            }
            if (cont && (node.childExists != 0))
            {
                for (ushort i = 0; i < 8; i++)
                {
                    Octant octant = (Octant)OCTANT_VISIT_ORDER[ctxt.visitOrder, i];
                    if (!HasChild(node, octant))
                    {
                        continue;
                    }
                    ulong childLocCode = (node.locCode << 3) | (ulong)octant;
                    OctreeNode<T> childNode = LookupNode(childLocCode);
                    if (childNode == null)
                    {
                        continue;
                    }
                    visit(childNode, ref ctxt, ref arg);
                    ctxt.inVisitor?.Invoke(this, childNode, ref arg);
                }
            }
            ctxt.postVisitor?.Invoke(this, node, ref arg);
            return true;
        }

        private static Octant[,] computeVisitOrderPermutations()
        {
            Octant[,] permutations = new Octant[48, 8];
            for (int order = 0; order < 48; order++)
            {
                int perm = order >> 3;
                int signs = (order & 0b111);
                for (int i = 0; i < 8; i++)
                {
                    int o = i ^ signs;

                    // TODO there are better ways to permute bits
                    // see http://programming.sirrida.de/calcperm.php
                    // but since we compute permutations only once there is no real need to optimize
                    int x = (o & 0b100) >> 2;
                    int y = (o & 0b010) >> 1;
                    int z = (o & 0b001) >> 0;
                    //Console.Write("{0} {1} {2} --> ", x, y, z);
                    VectorUtil.PERMUTATIONS[perm](x, y, z, out x, out y, out z);
                    //Console.WriteLine("{0} {1} {2}", x, y, z);
                    o = (x * Mask.X) | (y * Mask.Y) | (z * Mask.Z) ^ Mask.Z;
                    permutations[order, i] = (Octant)o;
                }
            }
            return permutations;
        }

        private static Direction[] computeDirectionLookupTable()
        {
            Direction[] directionLookupTable = new Direction[64];
            foreach (Direction direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())
            {
                directionLookupTable[DIR_DATA[(int)direction].lookupIndex] = direction;
            }
            return directionLookupTable;
        }
    }
}
