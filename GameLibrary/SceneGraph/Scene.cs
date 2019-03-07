using GameLibrary.Component;
using GameLibrary.Component.Camera;
using GameLibrary.Control;
using GameLibrary.Geometry;
using GameLibrary.SceneGraph.Bounding;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Voxel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Voxel;
using static GameLibrary.SceneGraph.SceneRenderContext;

namespace GameLibrary.SceneGraph
{
    public class Scene
    {
        public static readonly int THREE_LIGHTS = 0;
        public static readonly int ONE_LIGHT = 1;
        public static readonly int NO_LIGHT = 2;
        public static readonly int WIRE_FRAME = 3;
        public static readonly int VECTOR = 4;
        public static readonly int CLIPPING = 5;

        public static readonly int PLAYER = 6;
        public static readonly int BULLET = 7;
        public static readonly int ASTEROID = 8;

        public static readonly int OCTREE = 10;
        //public static readonly int VOXEL_MAP = 11;
        public static readonly int VOXEL = 12;
        public static readonly int VOXEL_WATER = 13;

        // DEBUG STARTS HERE
        public static readonly int DEBUG = 20;
        public static readonly int FRUSTUM = 21;

        public static readonly int BOUNDING_SPHERE = 30;
        public static readonly int BOUNDING_BOX = 31;

        public static readonly int CULLED_BOUNDING_SPHERE = 32;
        public static readonly int CULLED_BOUNDING_BOX = 33;

        public static readonly int OCCLUDER_BOUNDING_SPHERE = 34;
        public static readonly int OCCLUDER_BOUNDING_BOX = 35;

        public static readonly int COLLISION_SPHERE = 40;
        public static readonly int COLLISION_BOX = 41;

        public static readonly int BOUNDING_HULL = 43;

        public static readonly int HUD = 45;
        public static readonly int HORTO = 46;

        private static readonly Color GREEN = new Color(Color.Green, 255);
        private static readonly Color BLUE = new Color(Color.Blue, 255);
        private static readonly Color YELLOW = new Color(Color.Yellow, 255);
        private static readonly Color RED = new Color(Color.Red, 255);

        private CameraComponent cameraComponent;

        private Node rootNode;

        private readonly Dictionary<int, Renderer> renderers;
        private readonly Dictionary<int, Renderer> renderers2;

        private Dictionary<int, List<Node>> collisionGroups;

        private MeshNode boundingSphereGeo;
        private MeshNode boundingBoxGeo;

        private Matrix previousProjectionMatrix = Matrix.Identity;
        private Matrix previousViewMatrix = Matrix.Identity;

        Dictionary<int, LinkedList<Collision>> collisionCache = new Dictionary<int, LinkedList<Collision>>();
        //LinkedList<GameLibrary.Util.Intersection> intersections = new LinkedList<GameLibrary.Util.Intersection>();

        // Settings 
        public bool Debug = false;
        public bool ShowStats = false;
        public bool CaptureFrustum;
        public bool CheckCollisions = false;

        public GraphicsDevice GraphicsDevice;

        private SceneRenderContext renderContext;

        public SceneRenderContext RenderContext
        {
            get { return renderContext; }
        }

        public CameraComponent CameraComponent
        {
            get { return cameraComponent; }
            set { cameraComponent = value; }
        }

        public Node RootNode
        {
            get { return rootNode; }
            set { rootNode = value; }
        }

        public Scene()
        {
            renderers = new Dictionary<int, Renderer>();
            renderers2 = new Dictionary<int, Renderer>();
            collisionGroups = new Dictionary<int, List<Node>>();
        }

        public void Initialize()
        {
            //VectorUtil.CreateAABBAreaLookupTable();

            boundingSphereGeo = GeometryUtil.CreateGeodesicWF("BOUNDING_SPHERE", 4);
            boundingSphereGeo.Initialize(GraphicsDevice);

            boundingBoxGeo = GeometryUtil.CreateCubeWF("BOUNDING_BOX", 1);
            boundingBoxGeo.Initialize(GraphicsDevice);

            renderers[THREE_LIGHTS] = new BasicRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice)); // 3 lights
            renderers[ONE_LIGHT] = new BasicRenderer(EffectFactory.CreateBasicEffect2(GraphicsDevice)); // 1 light
            renderers[NO_LIGHT] = new BasicRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light

            renderers[WIRE_FRAME] = new WireFrameRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, false)); // no light + wire frame
            renderers[VECTOR] = new BasicRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, false)); // no light
            renderers[CLIPPING] = new BasicRenderer(EffectFactory.CreateClippingEffect(GraphicsDevice));

            renderers[PLAYER] = new BasicRenderer(EffectFactory.CreateClippingEffect(GraphicsDevice));
            renderers[BULLET] = new BasicRenderer(EffectFactory.CreateBulletEffect(GraphicsDevice));
            renderers[ASTEROID] = new BasicRenderer(EffectFactory.CreateClippingEffect(GraphicsDevice));

            //renderers[VOXEL_MAP] = new VoxelMapRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice));
            //renderers[VOXEL_MAP] = new VoxelMapInstancedRenderer(EffectFactory.CreateInstancedEffect(GraphicsDevice));

            renderers[VOXEL] = new VoxelRenderer(VoxelUtil.CreateVoxelEffect(GraphicsDevice));
            renderers[VOXEL_WATER] = new VoxelWaterRenderer(VoxelUtil.CreateVoxelWaterEffect(GraphicsDevice));

            //renderers[OCTREE] = new OctreeRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light
            //renderers[OCTREE] = new OctreeRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice)); // 3 lights

            renderers[FRUSTUM] = new FrustumRenderer(EffectFactory.CreateFrustumEffect(GraphicsDevice));

            bool clip = false; // clipping
            renderers[BOUNDING_SPHERE] = new BoundRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, clip, GREEN), boundingSphereGeo);
            renderers[BOUNDING_BOX] = new BoundRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, clip, GREEN), boundingBoxGeo);
            renderers[BOUNDING_HULL] = new BasicRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, clip, RED));

            renderers[CULLED_BOUNDING_SPHERE] = new BoundRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, clip, BLUE), boundingSphereGeo);
            renderers[CULLED_BOUNDING_BOX] = new BoundRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, clip, BLUE), boundingBoxGeo);

            renderers[OCCLUDER_BOUNDING_SPHERE] = new BoundRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, clip, YELLOW), boundingSphereGeo);
            renderers[OCCLUDER_BOUNDING_BOX] = new BoundRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, clip, YELLOW), boundingBoxGeo);

            renderers[COLLISION_SPHERE] = new BoundRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, clip, RED), boundingSphereGeo);
            renderers[COLLISION_BOX] = new BoundRenderer(EffectFactory.CreateVectorEffect(GraphicsDevice, clip, RED), boundingBoxGeo);

            //renderers[HUD] = new HortographicRenderer(EffectFactory.CreateBillboardEffect(GraphicsDevice));
            renderers[HUD] = new BillboardRenderer(EffectFactory.CreateBillboardEffect(GraphicsDevice));
            renderers[HORTO] = new HortographicRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice));

            // shadow renderers
            renderers2[ONE_LIGHT] = new ShadowCascadeRenderer(EffectFactory.CreateShadowCascadeEffect(GraphicsDevice));
            renderers2[VOXEL] = new VoxelShadowCascadeRenderer(EffectFactory.CreateShadowCascadeEffect(GraphicsDevice));
            //renderers2[VOXEL] = new ShadowRenderer(EffectFactory.CreateShadowEffect(GraphicsDevice));

            rootNode.Initialize(GraphicsDevice);
            rootNode.Visit(COMMIT_VISITOR, this);

            renderContext = new SceneRenderContext(GraphicsDevice, cameraComponent);
        }

        public void Dispose()
        {
            rootNode.Visit(DISPOSE_VISITOR);

            renderContext.Dispose();
        }

        public void Dump()
        {
            rootNode.Visit(DUMP_VISITOR);
        }

        public void ViewpointScene()
        {
            renderContext.RenderCamera.LookAt(
                renderContext.CullCamera.Position,
                renderContext.CullCamera.Position + renderContext.CullCamera.ViewDirection,
                Vector3.Up /*renderContext.CullCamera.YAxis*/);
        }

        public void ViewpointLight()
        {
            if (renderContext.LightCount > 0)
            {
                Camera lightCamera = renderContext.LightCamera(0);
                renderContext.RenderCamera.LookAt(
                    lightCamera.Position - lightCamera.ViewDirection * 100,
                    lightCamera.Position + lightCamera.ViewDirection,
                    Vector3.Up);
            }
        }

        public void Update(GameTime gameTime)
        {
            // FIXME WrapController use the node's WorldBoundingVolume but it might be
            // invalidated after by other controllers (like the PlayerController or AsteroidController)
            // is it enough (for now...) to change the visit order ?
            // Controllers should not be registered in nodes...
            // FIXME some controllers need to run before update (those that move nodes) others after (warp)
            // some controllers should  run early (rip)
            rootNode.Visit(CONTROLLER_VISITOR, gameTime);

            bool structureDirty = RootNode.IsDirty(Node.DirtyFlag.Structure) || RootNode.IsDirty(Node.DirtyFlag.ChildStructure);
            if (structureDirty)
            {
                rootNode.Visit(COMMIT_VISITOR, this);
            }

            // update
            rootNode.Visit(UPDATE_VISITOR, null, UPDATE_POST_VISITOR);

            if (CheckCollisions)
            {
                handleCollisions();
            }

            bool cameraDirty = false;
            cameraDirty = cameraDirty || CaptureFrustum;
            cameraDirty = cameraDirty || (!previousProjectionMatrix.Equals(renderContext.Camera.ProjectionMatrix));
            cameraDirty = cameraDirty || (!previousViewMatrix.Equals(renderContext.Camera.ViewMatrix));

            // TODO implement sceneDirty (easy)
            // Octree should set it when needed
            // LIMIT LOAD QUEUE SIZE (100?)
            // !!! and flush only when ADDING a 1st new chunk !!!!!!
            // but it won't work
            bool sceneDirty = false;

            renderContext.GameTime = gameTime;
            renderContext.ResetStats();

            // do the RENDER_GROUP_VISITOR only if:
            // - camera is dirty
            // - TODO scene is dirty (something has moved in the scene)
            // - scene structure is dirty
            if (cameraDirty || sceneDirty || structureDirty || renderContext.RedrawRequested())
            {
                if (CaptureFrustum)
                {
                    CaptureFrustum = false;
                    switch (renderContext.CameraFreezeMode)
                    {
                        case FreezeMode.None:
                            renderContext.FreezeCamera(FreezeMode.Cull);
                            break;
                        case FreezeMode.Cull:
                            renderContext.FreezeCamera(FreezeMode.Render);
                            break;
                        case FreezeMode.Render:
                            renderContext.FreezeCamera(FreezeMode.None);
                            break;
                    }
                }

                if (cameraDirty || renderContext.RedrawRequested())
                {
                    renderContext.ClearRedrawRequested();
                    if (cameraDirty)
                    {
                        previousProjectionMatrix = renderContext.RenderCamera.ProjectionMatrix;
                        previousViewMatrix = renderContext.RenderCamera.ViewMatrix;
                    }
                }
                if (structureDirty)
                {
                    if (Debug) Console.WriteLine("Structure Dirty");
                }

                bool FitToView = true;
                bool Stable = true;
                bool FindOccluders = true;

                // FIXME most of the time nodes stay in the same render group
                // so doing a clear + full reconstruct is not very efficient
                // but RENDER_GROUP_VISITOR does the culling...
                renderContext.Clear();

                // view camera culling
                renderContext.CullBegin();
                rootNode.Visit(CULL_VISITOR, renderContext);
                renderContext.CullEnd();

                // light camera culling
                if (renderContext.ShadowsEnabled)
                {
                    for (int i = 0; i < renderContext.LightCount; i++)
                    {
                        LightRenderContext lightRenderContext = renderContext.LightRenderContext(i);
                        if (!lightRenderContext.ShadowsEnabled)
                        {
                            continue;
                        }

                        // create initial light frustum
                        // if we are looking for occluders then push near plane outwards
                        float nearClipOffset = FindOccluders ? 1000.0f : 0.0f;
                        if (FitToView)
                        {
                            if (Stable)
                            {
                                lightRenderContext.FitToViewStable(renderContext, nearClipOffset);
                            }
                            else
                            {
                                //lightCamera.FitToView(renderContext.CullCamera.BoundingFrustum, nearClipOffset);
                            }
                        }
                        else
                        {
                            //lightCamera.FitToScene(renderContext.CullCamera.BoundingFrustum, renderContext.sceneBoundingBox, nearClipOffset);
                        }

                        if (FindOccluders)
                        {
                            // FIXME we get spurious occluders that are too far away to really
                            // contribute shadows... no idea atm on how to ignore them
                            // FIXME this will trigger chunk loading in conflict with main camera
                            // TODO should load occluders at a lower priority (but will cause shadow pops)
                            // find occluders
                            // TODO directional light frustrum is an OOBB and culling could be optimized for that
                            lightRenderContext.CullBegin();
                            rootNode.Visit(CULL_VISITOR, lightRenderContext);
                            lightRenderContext.CullEnd();

                            // revert to tight light frustum
                            // occluders beyond near/far planes will be depth clamped (see shadow renderer rasterizer state)
                            if (FitToView)
                            {
                                if (Stable)
                                {
                                    lightRenderContext.FitToViewStable(renderContext, 0.0f);
                                }
                                else
                                {
                                    //lightCamera.FitToView(renderContext.CullCamera.BoundingFrustum, 0.0f);
                                }
                            }
                            else
                            {
                                //lightCamera.FitToScene(renderContext.CullCamera.BoundingFrustum, renderContext.sceneBoundingBox, 0.0f);
                            }
                        }
                        else
                        {
                            // no culling was done to find occluders: just copy occluders from main context
                            foreach (KeyValuePair<int, List<Drawable>> renderBinKVP in renderContext.renderBins)
                            {
                                int renderBinId = renderBinKVP.Key;
                                // HACK HACK HACK
                                if (renderBinId == VOXEL)
                                {
                                    List<Drawable> drawableList = renderBinKVP.Value;
                                    lightRenderContext.AddToBin(renderBinId, drawableList);
                                }
                            }
                        }
                    }

                    // debug geometry
                    renderContext.DebugGeometryAddTo(renderContext);
                }
            }

        }

        public void Draw(GameTime gameTime)
        {
            if (renderContext.ShadowsEnabled)
            {
                LightRenderContext lightRenderContext = null;
                for (int i = 0; i < renderContext.LightCount; i++)
                {
                    //LightNode lightNode = renderContext.lightNodes[i];
                    lightRenderContext = renderContext.LightRenderContext(i);

                    Render(lightRenderContext, lightRenderContext.renderBins, renderers2);

                    // HACK
                    VoxelEffect voxelEffect = ((VoxelRenderer)renderers[VOXEL]).effect;
                    voxelEffect.DirectionalLight0.Direction = lightRenderContext.RenderCamera.ViewDirection;
                    voxelEffect.LightWorldViewProj = lightRenderContext.RenderCamera.ViewProjectionMatrix;
                    voxelEffect.ShadowMapTexture = lightRenderContext.RenderTarget;
                }
            }

            Render(renderContext, renderContext.renderBins, renderers);

            if (ShowStats)
            {
                renderContext.ShowStats();
                ShowStats = false;
            }
        }

        public void Render(RenderContext renderContext, SortedDictionary<int, List<Drawable>> renderBins, Dictionary<int, Renderer> renderers)
        {
            BlendState oldBlendState = GraphicsDevice.BlendState;
            DepthStencilState oldDepthStencilState = GraphicsDevice.DepthStencilState;
            RasterizerState oldRasterizerState = GraphicsDevice.RasterizerState;
            SamplerState oldSamplerState = GraphicsDevice.SamplerStates[0];

            renderContext.SetupGraphicsDevice();

            // FIXME should iterate over ordered by key...
            foreach (KeyValuePair<int, List<Drawable>> renderBinKVP in renderBins)
            {
                int renderBinId = renderBinKVP.Key;
                List<Drawable> drawableList = renderBinKVP.Value;
                if (drawableList.Count() == 0) continue;

                if (Debug) Console.WriteLine(renderBinId + " " + drawableList.Count);

                Renderer renderer;
                renderers.TryGetValue(renderBinId, out renderer);
                if (renderer != null)
                {
                    renderer.Render(renderContext, drawableList);
                }
                else
                {
                    if (Debug)
                    {
                        Console.WriteLine("No renderer found for render group " + renderBinId);
                    }
                }
            }

            // Drop the render target
            GraphicsDevice.SetRenderTargets(null);

            GraphicsDevice.BlendState = oldBlendState;
            GraphicsDevice.DepthStencilState = oldDepthStencilState;
            GraphicsDevice.RasterizerState = oldRasterizerState;
            GraphicsDevice.SamplerStates[0] = oldSamplerState;
        }

        private static readonly Node.Visitor UPDATE_VISITOR = delegate (Node node, ref object arg)
        {
            if (!node.Enabled) return false;
            TransformNode parentTransformNode = arg as TransformNode;
            if (node is TransformNode transformNode)
            {
                transformNode.UpdateTransform();
                transformNode.UpdateWorldTransform(parentTransformNode);
                // propagate parent transform node
                arg = transformNode;
            }
            // do we need to recurse
            bool recurse = false;
            // recurse if we are part of a "deep" world transform update
            recurse = recurse || ((parentTransformNode != null) && parentTransformNode.IsDirty(Node.DirtyFlag.ChildWorldTransform));
            // recurse if we have dirty children
            recurse = recurse || node.IsDirty(Node.DirtyFlag.ChildTransform);
            // recurse if we are triggering a "deep" world transform update ?
            recurse = recurse || node.IsDirty(Node.DirtyFlag.ChildWorldTransform);
            return recurse;
        };

        private static readonly Node.Visitor UPDATE_POST_VISITOR = delegate (Node node, ref Object arg)
        {
            if (node.IsDirty(Node.DirtyFlag.ChildTransform))
            {
                node.ClearDirty(Node.DirtyFlag.ChildTransform);
            }
            if (node.IsDirty(Node.DirtyFlag.ChildWorldTransform))
            {
                node.ClearDirty(Node.DirtyFlag.ChildWorldTransform);
            }
            return true;
        };

        private static readonly Node.Visitor COMMIT_VISITOR = delegate (Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            if (node is GroupNode groupNode)
            {
                //if (Debug) Console.WriteLine("Commiting " + node.Name);
                Scene scene = arg as Scene;
                groupNode.Commit(scene.GraphicsDevice);
            }
            // do we need to recurse
            bool recurse = false;
            recurse = recurse || node.IsDirty(Node.DirtyFlag.ChildStructure);
            node.ClearDirty(Node.DirtyFlag.ChildStructure);
            return recurse;
        };

        private static readonly Node.Visitor CONTROLLER_VISITOR = delegate (Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            GameTime gameTime = arg as GameTime;
            foreach (Controller controller in node.Controllers)
            {
                // a controller can disable the node...
                if (!node.Enabled)
                {
                    // no need to continue...
                    break;
                }
                controller.Update(gameTime);
            }
            return true;
        };

        private static readonly Node.Visitor CULL_VISITOR = delegate (Node node, ref Object arg)
            {
                if (!node.Visible)
                {
                    return false;
                }

                RenderContext ctxt = arg as RenderContext;

                // FIXME get rid this endless if/else if/else if/...
                if (node is VoxelOctreeGeometry voxelOctreeGeometry)
                {
                    voxelOctreeGeometry.voxelOctree.ClearLoadQueue();
                    voxelOctreeGeometry.voxelOctree.Visit(
                        ctxt.CullCamera.VisitOrder, VOXEL_OCTREE_CULL_VISITOR, null, VOXEL_OCTREE_CULL_POST_VISITOR, ref arg);
                    return true;
                }
                else if (node is Drawable drawable)
                {
                    Volume bv = drawable.WorldBoundingVolume;
                    bool cull = false;
                    if (bv != null)
                    {
                        BoundingFrustum boundingFrustum = ctxt.CullCamera.BoundingFrustum;
                        // TODO use a more accurate test for leaf nodes (but still not 100% accurate...)
                        bool slow = false;
                        if (bv.IsContained(boundingFrustum, !slow) == ContainmentType.Disjoint)
                        {
                            //if (Debug) Console.WriteLine("Culling " + node.Name);
                            cull = true;
                            ctxt.FrustumCullCount++;
                        }
                    }
                    if (!cull)
                    {
                        ctxt.AddToBin(drawable);
                    }
                    if (drawable.BoundingVolumeVisible)
                    {
                        ctxt.AddBoundingVolume(drawable, cull);
                    }
                }
                else if (node is LightNode lightNode && ctxt is SceneRenderContext sceneCtxt)
                {
                    sceneCtxt.AddLightNode(lightNode);
                }
                // TODO should return !cull
                return true;
            };

        static float minA = float.MaxValue;

        private static readonly VoxelOctree.Visitor<VoxelChunk> VOXEL_OCTREE_CULL_VISITOR = delegate (Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node, ref Object arg)
        {
            RenderContext ctxt = arg as RenderContext;

            bool culled = false;

            Bounding.Box boundingBox = (node.obj.Drawable != null) ?
                node.obj.Drawable.BoundingVolume as Bounding.Box : node.obj.BoundingBox;

            // frustum culling
            if (!culled && ctxt.FrustumCullingEnabled)
            {
                // TODO start from node that fully encompass the frustum (no need to recurse from root)
                // when recursing down some collision tests are not needed (possible to extend that from frame to frame)
                // see TODO.txt for links that show how to do that

                // http://www.cse.chalmers.se/~uffe/vfc.pdf
                // https://cesium.com/blog/2017/02/02/tighter-frustum-culling-and-why-you-may-want-to-disregard-it/
                // https://github.com/labnation/MonoGame/blob/master/MonoGame.Framework/BoundingFrustum.cs
                // https://github.com/labnation/MonoGame/blob/master/MonoGame.Framework/BoundingBox.cs

                BoundingFrustum boundingFrustum = ctxt.CullCamera.BoundingFrustum;

                // use a more accurate test for leaf nodes (but still not 100% accurate...)
                bool slow = (node.obj.Drawable != null);
                ContainmentType containmentType = boundingBox.IsContained(boundingFrustum, !slow);
                if (containmentType == ContainmentType.Disjoint)
                {
                    culled = true;
                    ctxt.FrustumCullCount++;
                    //Console.WriteLine("Frustum Culling " + node.Name);
                }
                else if (containmentType == ContainmentType.Contains)
                {
                    // contained: no need to cull further down
                    // take ownership of frustum culling
                    ctxt.frustumCullingOwner = node.locCode;
                    //Console.WriteLine("Fully contained");
                }
            }

            // distance culling
            if (!culled && ctxt.DistanceCullingEnabled)
            {
                Vector3 center = boundingBox.Center;
                Vector3 halfSize = boundingBox.HalfSize;
                // TODO move to BoundingBox
                // compute min and max distance (squared) of a point to an AABB
                // see http://mcmains.me.berkeley.edu/pubs/TVCG2010finalKrishnamurthyMcMains.pdf
                // the above reference also shows how to compute min and max distance (squared) between two AABBs
                Vector3 cameraPosition = ctxt.CullCamera.Position;
                Vector3 centerDist;
                centerDist.X = Math.Abs(center.X - cameraPosition.X);
                centerDist.Y = Math.Abs(center.Y - cameraPosition.Y);
                centerDist.Z = Math.Abs(center.Z - cameraPosition.Z);

                Vector3 dist;

                // max distance
                dist.X = centerDist.X + halfSize.X;
                dist.Y = centerDist.Y + halfSize.Y;
                dist.Z = centerDist.Z + halfSize.Z;
                float maxDistanceSquared = (dist.X * dist.X) + (dist.Y * dist.Y) + (dist.Z * dist.Z);

                // min distance
                dist.X = Math.Max(centerDist.X - halfSize.X, 0);
                dist.Y = Math.Max(centerDist.Y - halfSize.Y, 0);
                dist.Z = Math.Max(centerDist.Z - halfSize.Z, 0);
                float minDistanceSquared = (dist.X * dist.X) + (dist.Y * dist.Y) + (dist.Z * dist.Z);

                if (minDistanceSquared > ctxt.cullDistanceSquared && maxDistanceSquared > ctxt.cullDistanceSquared)
                {
                    // disjoint
                    culled = true;
                    ctxt.DistanceCullCount++;

                    //Console.WriteLine("Distance Culling: DISJOINT");
                    //Console.WriteLine("Distance Culling " + minDistanceSquared + ", " + maxDistanceSquared + " / " + ctxt.CullDistanceSquared);
                }
                else if (minDistanceSquared < ctxt.cullDistanceSquared && maxDistanceSquared < ctxt.cullDistanceSquared)
                {
                    // contained: no need to cull further down
                    // take ownership of frustum culling
                    ctxt.distanceCullingOwner = node.locCode;

                    //Console.WriteLine("Distance Culling: CONTAINED " + Octree<VoxelChunk>.LocCodeToString(node.locCode));
                    //Console.WriteLine("Distance Culling " + minDistanceSquared + ", " + maxDistanceSquared + " / " + ctxt.CullDistanceSquared);
                }
            }

            if (!culled && ctxt.ScreenSizeCullingEnabled)
            {
                // https://gdbooks.gitbooks.io/3dcollisions/content/Chapter2/static_aabb_plane.html

                Vector3 center = boundingBox.Center;
                Vector3 halfSize = boundingBox.HalfSize;

                // plane normal
                Vector3 n = ctxt.CullCamera.ViewDirection;

                // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
                float r = halfSize.X * Math.Abs(n.X) + halfSize.Y * Math.Abs(n.Y) + halfSize.Z * Math.Abs(n.Z);

                // FIXME performance no need to compute d each time...
                Vector3 p = ctxt.CullCamera.Position;
                float d = n.X * p.X + n.Y * p.Y + n.Z * p.Z; ;

                // Compute distance of box center from plane
                float s = Vector3.Dot(n, center) - d;

                // Intersection occurs when distance s falls within [-r,+r] interval
                if (Math.Abs(s) > r)
                {
                    if (node.locCode == 0b1000)
                    {
                        // TODO use dynamic vertex buffer
                        // then use dynamic vertex buffer to plot graph of mouse dx / dy (using line strip)
                        // and for camera Frustums...
                        AddBBoxHull(ctxt, ref boundingBox);
                    }
                    Vector3 cameraPosition = ctxt.CullCamera.Position;
                    float a = VectorUtil.ProjectedArea(node.obj.BoundingBox, ref cameraPosition, ctxt.ProjectToScreen);
                    if (a != -1 && a < minA)
                    {
                        minA = a;
                        Console.WriteLine("* " + minA);
                    }
                    //ctxt.ScreenSizeCullCount++;
                }
            }

            if (culled && !ctxt.ShowCulledBoundingVolumes)
            {
                // early exit !
                return false;
            }

            if (node.obj.State == VoxelChunkState.Null)
            {
                bool queued = octree.LoadNode(node, ref arg);
                if (!queued)
                {
                    // queue is full, bail out...
                    return false;
                }
            }
            if (node.obj.State != VoxelChunkState.Ready)
            {
                // FIXME because we bail out, the chunk will be available on next frame only
                return false;
            }

            Drawable drawable = node.obj.Drawable;
            if (drawable != null)
            {
                if (drawable.Visible && !culled)
                {
                    ctxt.AddToBin(drawable);

                    ctxt.sceneMin.X = Math.Min(ctxt.sceneMin.X, boundingBox.Center.X - boundingBox.HalfSize.X);
                    ctxt.sceneMin.Y = Math.Min(ctxt.sceneMin.Y, boundingBox.Center.Y - boundingBox.HalfSize.Y);
                    ctxt.sceneMin.Z = Math.Min(ctxt.sceneMin.Z, boundingBox.Center.Z - boundingBox.HalfSize.Z);
                    ctxt.sceneMax.X = Math.Max(ctxt.sceneMax.X, boundingBox.Center.X + boundingBox.HalfSize.X);
                    ctxt.sceneMax.Y = Math.Max(ctxt.sceneMax.Y, boundingBox.Center.Y + boundingBox.HalfSize.Y);
                    ctxt.sceneMax.Z = Math.Max(ctxt.sceneMax.Z, boundingBox.Center.Z + boundingBox.HalfSize.Z);
                }
                if (drawable.BoundingVolumeVisible)
                {
                    ctxt.AddBoundingVolume(drawable, BoundingType.AABB, culled);
                }
            }
            Drawable transparentDrawable = node.obj.TransparentDrawable;
            if (transparentDrawable != null)
            {
                if (transparentDrawable.Visible && !culled)
                {
                    ctxt.AddToBin(transparentDrawable);
                }
            }
            return !culled;
        };

        private static readonly VoxelOctree.Visitor<VoxelChunk> VOXEL_OCTREE_CULL_POST_VISITOR = delegate (Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node, ref Object arg)
        {
            RenderContext ctxt = arg as RenderContext;
            // restore culling flags
            if (ctxt.frustumCullingOwner == node.locCode)
            {
                ctxt.frustumCullingOwner = 0;
            }
            if (ctxt.distanceCullingOwner == node.locCode)
            {
                ctxt.distanceCullingOwner = 0;
            }
            if (ctxt.screenSizeCullingOwner == node.locCode)
            {
                ctxt.screenSizeCullingOwner = 0;
            }
            return true;
        };

        private static readonly Node.Visitor COLLISION_GROUP_VISITOR = delegate (Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            if (node is Physical physical)
            {
                Scene scene = arg as Scene;
                //scene.AddToGroups(scene.collisionGroups, physical.CollisionGroupId, drawable);
            }
            return true;
        };

        private static readonly Node.Visitor DISPOSE_VISITOR = delegate (Node node, ref Object arg)
        {
            Console.WriteLine("Disposing " + node.Name);
            node.Dispose();
            return true;
        };

        private static readonly Node.Visitor DUMP_VISITOR = delegate (Node node, ref Object arg)
        {
            int level = (arg != null) ? (int)arg : 0;
            Console.Write("".PadLeft(level, ' '));
            Console.Write(node.Name.PadRight(30 - level, ' '));
            Console.Write(" " + node.Controllers.Count);
            Console.Write(" " + node.DirtyFlagsAsString());
            Console.WriteLine();
            arg = level + 1;
            return true;
        };

        public static void AddBBoxHull(RenderContext ctxt, ref Bounding.Box boundingBox)
        {
            bool addHull = false;
            bool addProjectedHull = !addHull;
            bool addBorder = false;

            if (addBorder)
            {
                Vector3[] borderVertices = new Vector3[4]
                {
                    new Vector3(10, 10, 0),
                    new Vector3(950, 10, 0),
                    new Vector3(950, 530, 0),
                    new Vector3(10, 530, 0),
                };

                GeometryNode borderNode = new MeshNode("HORTO_CORDER", new LineMeshFactory(borderVertices, true));
                borderNode.RenderGroupId = Scene.HORTO;
                borderNode.Initialize(ctxt.GraphicsDevice);
                ctxt.AddToBin(borderNode);
            }

            if (addHull)
            {
                Vector3 cameraPosition = ctxt.CullCamera.Position;
                Vector3[] hull = VectorUtil.Hull(boundingBox, ref cameraPosition);
                if (hull.Length > 0)
                {
                    GeometryNode n = new MeshNode("BBOX_HULL", new LineMeshFactory(hull, true));
                    n.RenderGroupId = Scene.VECTOR;
                    n.Initialize(ctxt.GraphicsDevice);
                    ctxt.AddToBin(n);
                }
            }
            if (addProjectedHull)
            {
                Vector3 cameraPosition = ctxt.CullCamera.Position;
                Vector3[] hull = VectorUtil.ProjectedHull(boundingBox, ref cameraPosition, ctxt.ProjectToScreen);
                if (hull.Length > 0)
                {
                    GeometryNode n = new MeshNode("BBOX_PROJECTED_HULL", new LineMeshFactory(hull, true));
                    n.RenderGroupId = Scene.HORTO;
                    n.Initialize(ctxt.GraphicsDevice);
                    ctxt.AddToBin(n);
                }
            }
        }

        struct Collision
        {
            public Node node1;
            public Node node2;
        }

        private void handleCollisions()
        {
            // handle collisions
            ClearCollisionGroups(collisionGroups);
            rootNode.Visit(COLLISION_GROUP_VISITOR, this);

            collisionCache.Clear();
            if (collisionGroups.ContainsKey(ASTEROID))
            {
                checkCollisions(collisionGroups[ASTEROID], collisionCache);
                if (collisionGroups.ContainsKey(PLAYER))
                {
                    checkCollisions(collisionGroups[ASTEROID], collisionGroups[PLAYER], collisionCache);
                }
                if (collisionGroups.ContainsKey(BULLET))
                {
                    checkCollisions(collisionGroups[ASTEROID], collisionGroups[BULLET], collisionCache);
                }
            }
            /*
            intersections.Clear();
            foreach (LinkedList<Collision> list in collisionCache.Values)
            {
                foreach (Collision c in list)
                {
                    MeshNode n1 = c.node1 as MeshNode;
                    MeshNode n2 = c.node2 as MeshNode;
                    if (n1 != null && n2 != null)
                    {
                        //PolygonUtil.clip(n1, n2, intersections);
                        //PolygonUtil.findIntersections(n1, n2, intersections);
                    }
                }
            }
            */
        }

        private void checkCollisions(List<Node> nodes, Dictionary<int, LinkedList<Collision>> cache)
        {
            //int n = 0;
            for (int i = 0; i < nodes.Count() - 1; i++)
            {
                for (int j = i + 1; j < nodes.Count(); j++)
                {
                    Node node1 = nodes[i];
                    Node node2 = nodes[j];
                    /*
                    if (node1.WorldBoundingVolume != null && node2.WorldBoundingVolume != null && node1.WorldBoundingVolume.Intersects(node2.WorldBoundingVolume))
                    {
                        n++;
                        AddCollision(cache, node1, node2);
                    }
                    */
                }
            }
        }

        private void checkCollisions(List<Node> nodes1, List<Node> nodes2, Dictionary<int, LinkedList<Collision>> cache)
        {
            //int n = 0;
            for (int i = 0; i < nodes1.Count(); i++)
            {
                Node node1 = nodes1[i];
                for (int j = 0; j < nodes2.Count(); j++)
                {
                    Node node2 = nodes2[j];
                    /*
                    if (node1.WorldBoundingVolume.Intersects(node2.WorldBoundingVolume))
                    {
                        n++;
                        AddCollision(cache, node1, node2);
                    }
                    */
                }
            }
        }

        private static Collision AddCollision(Dictionary<int, LinkedList<Collision>> cache, Node node1, Node node2)
        {
            Collision c;
            c.node1 = node1;
            c.node2 = node2;
            LinkedList<Collision> list1;
            if (!cache.TryGetValue(node1.Id, out list1))
            {
                list1 = new LinkedList<Collision>();
                cache[node1.Id] = list1;
            }
            list1.AddLast(c);

            LinkedList<Collision> list2;
            if (!cache.TryGetValue(node2.Id, out list2))
            {
                list2 = new LinkedList<Collision>();
                cache[node2.Id] = list2;
            }
            list2.AddLast(c);
            return c;
        }

        internal void ClearCollisionGroups(Dictionary<int, List<Node>> groups)
        {
            foreach (KeyValuePair<int, List<Node>> nodeListKVP in groups)
            {
                List<Node> nodeList = nodeListKVP.Value;
                nodeList.Clear();
            }
        }

        /*
        private void drawIntersection(GameTime gameTime)
        {
            DrawContext dc = new DrawContext();
            dc.scene = this;
            dc.gameTime = gameTime;

            Effect effect = renderEffects[BOUND];
            IEffectMatrices effectMatrices = effect as IEffectMatrices;
            if (effectMatrices != null)
            {
                effectMatrices.Projection = cameraComponent.ProjectionMatrix;
                effectMatrices.View = cameraComponent.ViewMatrix;
            }
            dc.effect = effect;

            GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            //GraphicsDevice.BlendState = BlendState.AlphaBlend;

            foreach (GameLibrary.Util.Intersection intersection in intersections)
            {
                if (effectMatrices != null)
                {
                    //effectMatrices.World = node.WorldMatrix;// *Matrix.CreateScale(boundingSphere.Radius); // node.WorldMatrix;
                    effectMatrices.World = Matrix.CreateScale(0.04f) * Matrix.CreateTranslation(intersection.vertex); // node.WorldMatrix;
                }
                boundingGeo.preDraw(dc);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    dc.pass = pass;
                    boundingGeo.Draw(dc);
                }
                boundingGeo.postDraw(dc);
            }
        }
    */

    }
}
