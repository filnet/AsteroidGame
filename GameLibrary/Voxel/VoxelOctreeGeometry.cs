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
using GameLibrary.SceneGraph.Bounding;

namespace GameLibrary.Voxel
{
    public class VoxelOctreeGeometry : GeometryNode
    {
        private Vector3 center;
        private Vector3 halfSize;

        public VoxelOctree voxelOctree;

        public VoxelOctreeGeometry(String name, int size, int chunkSize) : base(name)
        {
            center = Vector3.Zero;
            halfSize = new Vector3((size * chunkSize) / 2);
            voxelOctree = new VoxelOctree(size, chunkSize);
        }

        public override void Initialize(GraphicsDevice gd)
        {
            BoundingVolume = new GameLibrary.SceneGraph.Bounding.BoundingBox(center, halfSize);

            //voxelOctree.Visit(0, CREATE_GEOMETRY_VISITOR, gd);

            base.Initialize(gd);
        }

        public override void Dispose()
        {
        }

        public class FakeDrawable : Drawable
        {
            public bool Enabled { get; set; }

            // Fake is always invisible (but not its bounds...)
            public bool Visible { get; set; }

            public int RenderGroupId { get; set; }

            public bool BoundingVolumeVisible { get; set; }

            /// <summary>
            /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
            /// </summary>
            public BoundingVolume BoundingVolume { get; }

            /// <summary>
            /// Gets or sets the geometry bounding volume, which contains the entire geometry in model (local) space.
            /// </summary>
            public BoundingVolume WorldBoundingVolume { get; }

            public void PreDraw(GraphicsDevice gd)
            { }
            public void Draw(GraphicsDevice gd)
            { }
            public void PostDraw(GraphicsDevice gd)
            { }

            public FakeDrawable(int renderGroupId, BoundingVolume boundingVolume) : this(renderGroupId, boundingVolume, boundingVolume)
            {
            }
            public FakeDrawable(int renderGroupId, BoundingVolume boundingVolume, BoundingVolume worldBoundingVolume)
            {
                RenderGroupId = renderGroupId;
                BoundingVolume = boundingVolume;
                WorldBoundingVolume = worldBoundingVolume;
                Enabled = true;
                Visible = false;
                BoundingVolumeVisible = true;
            }
        }

        public static VoxelOctree.Visitor<VoxelChunk> CREATE_GEOMETRY_VISITOR = delegate (Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node, bool cull, ref Object arg)
    {
        GraphicsDevice gd = arg as GraphicsDevice;

        Drawable drawable = null;
        // TODO remove magic number 4... that's why we are stuck at size 512
        if (node.obj.VoxelMap != null && Octree<VoxelChunk>.GetNodeTreeDepth(node) == 6)
        {
            VoxelMap voxelMap = node.obj.VoxelMap;

            // TODO performance: derive from a simpler geometry node (no child, no physics, etc...)
            IMeshFactory f = new VoxelMapMeshFactory(octree as VoxelOctree, node);
            Mesh mesh = f.CreateMesh(gd);
            if (mesh == null)
            {
                return VoxelOctree.VisitReturn.Abort;
            }
            GeometryNode geometryNode = new MeshNode("VOXEL", mesh);
            geometryNode.Initialize(gd);
            geometryNode.Visible = true;
            geometryNode.RenderGroupId = Scene.VOXEL;
            //node.Rotation = Quaternion.CreateFromYawPitchRoll(0.3f, 0, 0);

            SceneGraph.Bounding.BoundingBox boundingBox = node.obj.BoundingBox;
            /*
            Vector3 halfSize;
            Vector3 center;
            octree.GetNodeBoundingBox(node, out center, out halfSize);
            */

            geometryNode.Translation = boundingBox.Center;

            geometryNode.BoundingVolume = boundingBox;
            geometryNode.BoundingVolumeVisible = false;

            geometryNode.UpdateTransform();
            geometryNode.UpdateWorldTransform(null);

            drawable = geometryNode;
        }
        else 
        {
            SceneGraph.Bounding.BoundingBox boundingBox = node.obj.BoundingBox;
            drawable = new FakeDrawable(Scene.VECTOR, boundingBox);

            // TODO performance: LAZILY create somple place holder geometry
            // the place holder geometry should be as simple as possible:
            // - only bounding volume
            // - no mesh
            // - no child, etc...
/*
            GeometryNode geometryNode = GeometryUtil.CreateCubeWF("BOUNDING_BOX", 0.5f);
            geometryNode.Initialize(gd);
            geometryNode.Visible = false;
            geometryNode.RenderGroupId = Scene.VECTOR;

            Vector3 halfSize;
            Vector3 center;
            octree.GetNodeBoundingBox(node, out center, out halfSize);

            geometryNode.Scale = halfSize;
            geometryNode.Translation = center;
*/
        }
        node.obj.Drawable = drawable;
        return VoxelOctree.VisitReturn.Continue;
    };


    }

}
