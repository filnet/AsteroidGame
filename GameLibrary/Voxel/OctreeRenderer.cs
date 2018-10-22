using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;
using GameLibrary.SceneGraph.Bounding;
using GameLibrary.Geometry.Common;
using GameLibrary.Geometry;
using GameLibrary.Voxel;

namespace GameLibrary.SceneGraph
{

    public class OctreeRenderer : EffectRenderer
    {
        private GeometryNode boundingBoxGeo;

        private OctreeGeometry octreeGeometry;
        private Matrix worldMatrix;

        private EffectPass pass;

        public static RasterizerState OctreeRasterizer = new RasterizerState()
        {
            CullMode = CullMode.None,
            //FillMode = FillMode.WireFrame,
        };

        public OctreeRenderer(Effect effect) : base(effect)
        {
            // FIXME need to call cubeGeometry.Dispose() at some point...
            //cubeGeometry = GeometryUtil.CreateCube("VOXEL_CUBE");

            boundingBoxGeo = GeometryUtil.CreateCubeWF("BOUNDING_BOX", 1);
            //boundingBoxGeo.Scene = this;
            //boundingBoxGeo.Scale = new Vector3(1.05f);
            //boundingBoxGeo.RenderGroupId = VECTOR;
            //boundingBoxGeo.Initialize();


            RasterizerState = OctreeRasterizer;
        }

        public override void Render(GraphicsContext gc, List<GeometryNode> nodeList)
        {
            gc.GraphicsDevice.BlendState = BlendState;
            gc.GraphicsDevice.DepthStencilState = DepthStencilState;
            gc.GraphicsDevice.RasterizerState = RasterizerState;
            gc.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = gc.Camera.ProjectionMatrix;
                effectMatrices.View = gc.Camera.ViewMatrix;
            }

            foreach (GeometryNode node in nodeList)
            {
                OctreeGeometry octreeGeometry = node as OctreeGeometry;
                if (octreeGeometry != null)
                {
                    render(gc, octreeGeometry);
                }
            }
        }

        private void render(GraphicsContext gc, OctreeGeometry octreeGeometry)
        {
            if (boundingBoxGeo.Scene == null)
            {
                boundingBoxGeo.Scene = octreeGeometry.Scene;
                boundingBoxGeo.Initialize();
            }

            this.octreeGeometry = octreeGeometry;
            worldMatrix = octreeGeometry.WorldTransform;

            boundingBoxGeo.preDraw();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                this.pass = pass;
                octreeGeometry.Octree.Visit(_GROUP_VISITOR, this);
            }
        }

        private static Octree<GeometryNode>.Visitor _GROUP_VISITOR = delegate (OctreeNode<GeometryNode> octreeNode, ref Object arg)
        {
            OctreeRenderer renderer = arg as OctreeRenderer;
            /*
            if (!node.Enabled) return false;
            GeometryNode geometryNode = node as GeometryNode;
            if (geometryNode != null)
            {
                Scene scene = arg as Scene;
                scene.AddToGroups(scene.collisionGroups, geometryNode.CollisionGroupId, geometryNode);
            }
            */

            if (renderer.effectMatrices != null)
            {
                // TOOO move bbox rendering to elsewhere !!!
                if (false && octreeNode.obj != null)
                {
                    Vector3 halfSize;
                    Vector3 center;
                    renderer.octreeGeometry.Octree.GetNodeBoundingBox(octreeNode, out center, out halfSize);

                    Matrix localMatrix = Matrix.CreateTranslation(center);
                    renderer.effectMatrices.World = localMatrix * renderer.worldMatrix;
                    renderer.pass.Apply();
                    octreeNode.obj.preDraw();
                    octreeNode.obj.Draw();
                    octreeNode.obj.postDraw();
                }
                if (true)
                {
                    Vector3 halfSize;
                    Vector3 center;
                    renderer.octreeGeometry.Octree.GetNodeBoundingBox(octreeNode, out center, out halfSize);

                    Matrix localMatrix = Matrix.CreateScale(halfSize) * Matrix.CreateTranslation(center);
                    renderer.effectMatrices.World = localMatrix * renderer.worldMatrix;
                    renderer.pass.Apply();
                    renderer.boundingBoxGeo.Draw();
                }
            }

            return true;
        };
        /*
                class DrawVisitor : Octree.Visitor
                {
                    private static float d = 0.5773502692f; // 1 over the square root of 3

                    private readonly OctreeRenderer parent;

                    public DrawVisitor(OctreeRenderer parent)
                    {
                        this.parent = parent;
                    }

                    public bool Begin(int size, int instanceCount, int maxInstanceCount)
                    {
                        return true;
                    }

                    public bool Visit(OctreeIterator ite)
                    {
                        Matrix currentWorld = Matrix.Identity;
                        if (parent.effectMatrices != null)
                        {
                            int size = ite.Size;
                            Matrix localMatrix = Matrix.CreateScale(0.750f / size) * Matrix.CreateTranslation(
                                (2 * ite.X - size) * d / size, (2 * ite.Y - size) * d / size, (2 * ite.Z - size) * d / size);
                            parent.effectMatrices.World = localMatrix * parent.worldMatrix;
                        }
                        parent.pass.Apply();

                        parent.cubeGeometry.Draw();

                        return true;
                    }

                    public bool End()
                    {
                        return true;
                    }
                }
                */
    }

}
