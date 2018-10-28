using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Voxel;
using GameLibrary.Geometry.Common;
using GameLibrary.Geometry;

namespace GameLibrary
{
    public class VoxelOctreeGeometry : GeometryNode
    {
        private Vector3 center;
        private Vector3 halfSize;

        public VoxelOctree voxelOctree;

        public VoxelOctreeGeometry(String name, int size) : base(name)
        {
            center = Vector3.Zero;
            halfSize = new Vector3(size / 2);
            voxelOctree = new VoxelOctree(size, 16);
        }

        public override void Initialize(GraphicsDevice gd)
        {
            BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingBox(center, halfSize);

            voxelOctree.Visit(CREATE_GEOMETRY_VISITOR, gd);

            base.Initialize(gd);
        }

        public override void Dispose()
        {
        }

        private static VoxelOctree.Visitor CREATE_GEOMETRY_VISITOR = delegate (Octree<VoxelObject> octree, OctreeNode<VoxelObject> node, ref Object arg)
        {
            GraphicsDevice gd = arg as GraphicsDevice;

            GeometryNode geometryNode = null;
            //int depth = octree.GetNodeTreeDepth(node);
            if (node.obj.VoxelMap != null)
            {
                VoxelMap voxelMap = node.obj.VoxelMap;

                // FIXME garbage generation here...
                VoxelMap[] neighbours = new VoxelMap[6];
                foreach (Direction direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())
                {
                    ulong l = octree.GetNeighborOfGreaterOrEqualSize(node.locCode, direction);
                    OctreeNode<VoxelObject> n = octree.LookupNode(l);
                    neighbours[(int)direction] = (n != null) ? n.obj.VoxelMap : null;
                }

                geometryNode = new MeshNode("VOXEL", new VoxelMapMeshFactory(voxelMap, neighbours));
                geometryNode.Initialize(gd);
                geometryNode.Visible = true;
                geometryNode.RenderGroupId = Scene.VOXEL;
                //node.Rotation = Quaternion.CreateFromYawPitchRoll(0.3f, 0, 0);

                Vector3 halfSize;
                Vector3 center;
                octree.GetNodeBoundingBox(node, out center, out halfSize);

                geometryNode.Translation = center;
            }
            else
            {
                geometryNode = GeometryUtil.CreateCubeWF("BOUNDING_BOX", 0.5f);
                geometryNode.Initialize(gd);
                geometryNode.Visible = false;
                geometryNode.RenderGroupId = Scene.VECTOR;

                Vector3 halfSize;
                Vector3 center;
                octree.GetNodeBoundingBox(node, out center, out halfSize);

                geometryNode.Scale = halfSize;
                geometryNode.Translation = center;
            }
            if (geometryNode != null)
            {
                geometryNode.UpdateTransform();
                geometryNode.UpdateWorldTransform(null);
            }
            node.obj.GeometryNode = geometryNode;
            return true;
        };


    }

}
