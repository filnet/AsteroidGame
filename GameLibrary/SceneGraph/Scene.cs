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
using System.ComponentModel;
using GameLibrary.Component;
using GameLibrary.Component.Camera;
using Voxel;

namespace GameLibrary.SceneGraph
{
    public class Scene
    {
        public static int THREE_LIGHTS = 0;
        public static int ONE_LIGHT = 1;
        public static int NO_LIGHT = 2;
        public static int WIRE_FRAME = 3;
        public static int VECTOR = 4;
        public static int CLIPPING = 5;

        public static int PLAYER = 6;
        public static int BULLET = 7;
        public static int ASTEROID = 8;

        public static int OCTREE = 10;
        //public static int VOXEL_MAP = 11;
        public static int VOXEL = 12;
        public static int VOXEL_WATER = 13;

        public static int FRUSTUM = 20;

        public static int BOUNDING_SPHERE = 30;
        public static int BOUNDING_BOX = 31;

        public static int CULLED_BOUNDING_SPHERE = 32;
        public static int CULLED_BOUNDING_BOX = 33;

        public static int CASTER_BOUNDING_SPHERE = 34;
        public static int OCCLUDER_BOUNDING_BOX = 35;

        public static int COLLISION_SPHERE = 40;
        public static int COLLISION_BOX = 41;

        public static int HUD = 45;
        public static int HORTO = 46;

        private CameraComponent cameraComponent;

        private Node rootNode;

        private readonly Dictionary<int, Renderer> renderers;
        private readonly Dictionary<int, Renderer> renderers2;

        private Dictionary<int, List<Node>> collisionGroups;

        private MeshNode boundingSphereGeo;
        private MeshNode boundingBoxGeo;

        // debug camera
        private MeshNode frustumGeo;
        private MeshNode frustumBoundingBoxGeo;
        private MeshNode frustumBoundingSphereGeo;
        private MeshNode sceneBoundingBoxGeo;

        private MeshNode lightFrustumGeo;
        private MeshNode occluderBoundingBoxGeo;

        private BillboardNode billboardNode;

        private Matrix previousProjectionMatrix = Matrix.Identity;
        private Matrix previousViewMatrix = Matrix.Identity;

        Dictionary<int, LinkedList<Collision>> collisionCache = new Dictionary<int, LinkedList<Collision>>();
        //LinkedList<GameLibrary.Util.Intersection> intersections = new LinkedList<GameLibrary.Util.Intersection>();

        // Settings 
        public Boolean Debug;
        public Boolean CheckCollisions = false;
        public Boolean ShowBoundingVolumes
        {
            get { return renderContext.ShowBoundingVolumes; }
            set { renderContext.ShowBoundingVolumes = value; }
        }
        public Boolean ShowCulledBoundingVolumes
        {
            get { return renderContext.ShowCulledBoundingVolumes; }
            set { renderContext.ShowCulledBoundingVolumes = value; }
        }
        public Boolean ShowCollisionVolumes = true;
        public Boolean ShowFrustum;
        public Boolean CaptureFrustum;

        public GraphicsDevice GraphicsDevice;

        public RenderContext renderContext;

        public Node RootNode
        {
            get { return rootNode; }
            set { rootNode = value; }
        }

        public CameraComponent CameraComponent
        {
            get { return cameraComponent; }
            set { cameraComponent = value; }
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

            sceneBoundingBoxGeo = GeometryUtil.CreateCubeWF("VISIBLE_BOUNDING_BOX", 1);
            sceneBoundingBoxGeo.Initialize(GraphicsDevice);

            frustumBoundingBoxGeo = GeometryUtil.CreateCubeWF("FRUSTUM_BOUNDING_BOX", 1);
            frustumBoundingBoxGeo.Initialize(GraphicsDevice);

            frustumBoundingSphereGeo = GeometryUtil.CreateGeodesicWF("FRUSTUM_BOUNDING_SPHERE", 1);
            frustumBoundingSphereGeo.Initialize(GraphicsDevice);

            occluderBoundingBoxGeo = GeometryUtil.CreateCubeWF("CASTER_BOUNDING_BOX", 1);
            occluderBoundingBoxGeo.Initialize(GraphicsDevice);

            billboardNode = new BillboardNode("SHADOW_MAP");
            billboardNode.Initialize(GraphicsDevice);
            //billboardNode.Translation = new Vector3(10, 40, 0);
            //billboardNode.UpdateTransform();
            //billboardNode.UpdateWorldTransform();

            renderers[THREE_LIGHTS] = new BasicRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice)); // 3 lights
            renderers[ONE_LIGHT] = new BasicRenderer(EffectFactory.CreateBasicEffect2(GraphicsDevice)); // 1 light
            renderers[NO_LIGHT] = new BasicRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light

            renderers[WIRE_FRAME] = new WireFrameRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light + wire frame
            renderers[VECTOR] = new BasicRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light
            renderers[CLIPPING] = new BasicRenderer(EffectFactory.CreateClippingEffect(GraphicsDevice));

            renderers[PLAYER] = new BasicRenderer(EffectFactory.CreateClippingEffect(GraphicsDevice));
            renderers[BULLET] = new BasicRenderer(EffectFactory.CreateBulletEffect(GraphicsDevice));
            renderers[ASTEROID] = new BasicRenderer(EffectFactory.CreateClippingEffect(GraphicsDevice));

            //renderers[VOXEL_MAP] = new VoxelMapRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice));
            //renderers[VOXEL_MAP] = new VoxelMapInstancedRenderer(EffectFactory.CreateInstancedEffect(GraphicsDevice));

            renderers[VOXEL] = new VoxelRenderer(VoxelUtil.CreateVoxelEffect(GraphicsDevice));
            renderers[VOXEL_WATER] = new VoxelWaterRenderer(VoxelUtil.CreateVoxelWaterEffect(GraphicsDevice));

            renderers2[VOXEL] = new VoxelShadowRenderer(VoxelUtil.CreateVoxelShadowEffect(GraphicsDevice));

            //renderers[OCTREE] = new OctreeRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light
            //renderers[OCTREE] = new OctreeRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice)); // 3 lights

            renderers[FRUSTUM] = new FrustumRenderer(EffectFactory.CreateFrustumEffect(GraphicsDevice));

            bool clip = false; // clipping
            renderers[BOUNDING_SPHERE] = new BoundRenderer(EffectFactory.CreateBoundEffect(GraphicsDevice, clip), boundingSphereGeo);
            renderers[BOUNDING_BOX] = new BoundRenderer(EffectFactory.CreateBoundEffect(GraphicsDevice, clip), boundingBoxGeo);
            renderers[CULLED_BOUNDING_SPHERE] = new BoundRenderer(EffectFactory.CreateCulledBoundEffect(GraphicsDevice, clip), boundingSphereGeo);
            renderers[CULLED_BOUNDING_BOX] = new BoundRenderer(EffectFactory.CreateCulledBoundEffect(GraphicsDevice, clip), boundingBoxGeo);
            renderers[CASTER_BOUNDING_SPHERE] = new BoundRenderer(EffectFactory.CreateCasterBoundEffect(GraphicsDevice, clip), boundingSphereGeo);
            renderers[OCCLUDER_BOUNDING_BOX] = new BoundRenderer(EffectFactory.CreateCasterBoundEffect(GraphicsDevice, clip), boundingBoxGeo);
            renderers[COLLISION_SPHERE] = new BoundRenderer(EffectFactory.CreateCollisionEffect(GraphicsDevice, clip), boundingSphereGeo);
            renderers[COLLISION_BOX] = new BoundRenderer(EffectFactory.CreateCollisionEffect(GraphicsDevice, clip), boundingBoxGeo);

            //renderers[HUD] = new HortographicRenderer(EffectFactory.CreateBillboardEffect(GraphicsDevice));
            renderers[HUD] = new BillboardRenderer(EffectFactory.CreateBillboardEffect(GraphicsDevice));
            renderers[HORTO] = new HortographicRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice));

            rootNode.Initialize(GraphicsDevice);
            rootNode.Visit(COMMIT_VISITOR, this);

            renderContext = new RenderContext(GraphicsDevice, cameraComponent);
        }

        public void Dispose()
        {
            rootNode.Visit(DISPOSE_VISITOR);
        }

        public void Dump()
        {
            rootNode.Visit(DUMP_VISITOR);
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

            bool cameraDirty = false;
            cameraDirty = cameraDirty || (!previousProjectionMatrix.Equals(cameraComponent.ProjectionMatrix));
            cameraDirty = cameraDirty || (!previousViewMatrix.Equals(cameraComponent.ViewMatrix));

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
                if (cameraDirty || renderContext.RedrawRequested())
                {
                    renderContext.ClearRedrawRequested();
                    if (cameraDirty)
                    {
                        previousProjectionMatrix = cameraComponent.ProjectionMatrix;
                        previousViewMatrix = cameraComponent.ViewMatrix;
                    }
                    // TODO if FrustumGeo is null the previous camera position/etc will be used...
                    // and not the current one at the time the camera was frozen
                    if (frustumGeo == null)
                    {
                        renderContext.UpdateCamera();
                    }
                }
                if (structureDirty)
                {
                    if (Debug) Console.WriteLine("Structure Dirty");
                }

                bool FitToView = true;
                bool Stable = true;
                bool FindOccluders = true;

                bool ShowSceneBoundingBox = false;
                bool ShowFrustumBoundingBox = false;
                bool ShowFrustumBoundingSphere = false;

                bool ShowLightFrustum = true;
                bool ShowOccluderBoundingBox = false;
                bool ShowOccluders = false;
                bool ShowShadowMap = true;

                // main camera culling

                // FIXME most of the time nodes stay in the same render group
                // so doing a clear + full reconstruct is not very efficient
                // but RENDER_GROUP_VISITOR does the culling...
                renderContext.Clear();

                rootNode.Visit(CULL_VISITOR, renderContext);

                // light camera culling

                Bounding.BoundingBox FrustumBoundingBox = null;
                Bounding.BoundingSphere FrustumBoundingSphere = null;
                if (ShowFrustumBoundingBox)
                {
                    // FIXME garbage
                    FrustumBoundingBox = new Bounding.BoundingBox();
                    Vector3[] corners = new Vector3[BoundingFrustum.CornerCount];
                    renderContext.BoundingFrustum.GetCorners(corners);
                    FrustumBoundingBox.ComputeFromPoints(corners);
                }

                // visible bounding box includes "whole" chunks
                // so we need to intersect with Frustum to get a tighter visible bounding box
                Bounding.BoundingBox sceneBoundingBox = Bounding.BoundingBox.CreateFromMinMax(renderContext.sceneMin, renderContext.sceneMax);

                // FIXME only last occluders BB and light Frustum will be shown...
                RenderContext shadowRenderContext = null;
                LightCamera lightCamera = null;
                Bounding.BoundingBox occluderBoundingBox = null;

                for (int i = 0; i < renderContext.lightNodes.Count; i++)
                {
                    LightNode lightNode = renderContext.lightNodes[i];
                    shadowRenderContext = renderContext.lightRenderContextes[i];
                    lightCamera = shadowRenderContext.Camera as LightCamera;

                    lightCamera.lightDirection = -lightNode.Translation;
                    lightCamera.lightDirection.Normalize();

                    // create initial light Frustum 
                    // if we are looking for occluders then push near plane outwards
                    if (FitToView)
                    {
                        if (Stable)
                        {
                            lightCamera.FitToViewStable(renderContext.BoundingFrustum, sceneBoundingBox, FindOccluders ? 1000.0f : 0.0f);
                        }
                        else
                        {
                            lightCamera.FitToView(renderContext.BoundingFrustum, FindOccluders ? 1000.0f : 0.0f);
                        }
                    }
                    else
                    {
                        lightCamera.FitToScene(renderContext.BoundingFrustum, sceneBoundingBox, FindOccluders ? 1000.0f : 0.0f);
                    }
                    shadowRenderContext.UpdateCamera();

                    if (FindOccluders)
                    {
                        shadowRenderContext.sceneMin = new Vector3(float.MaxValue);
                        shadowRenderContext.sceneMax = new Vector3(float.MinValue);

                        // FIXME we get spurious occluders that are too far away to really
                        // contribute shadows... no idea atm on how to ignore them
                        // FIXME this will trigger chunk loading in conflict with main camera
                        // TODO should load occluders at a lower priority (but will cause shadow pops)
                        // find occluders
                        rootNode.Visit(CULL_VISITOR, shadowRenderContext);

                        // occluder bounding box (used for debugging)
                        if (ShowOccluderBoundingBox)
                        {
                            occluderBoundingBox = Bounding.BoundingBox.CreateFromMinMax(
                                shadowRenderContext.sceneMin, shadowRenderContext.sceneMax);
                        }

                        // revert to tight light Frustum 
                        // occluders beyond near/far planes will be depth clamped (see shadow renderer rasterizer state)
                        if (FitToView)
                        {
                            if (Stable)
                            {
                                lightCamera.FitToViewStable(renderContext.BoundingFrustum, sceneBoundingBox, 0.0f);
                            }
                            else
                            {
                                lightCamera.FitToView(renderContext.BoundingFrustum, 0.0f);
                            }
                        }
                        else
                        {
                            lightCamera.FitToScene(renderContext.BoundingFrustum, sceneBoundingBox, 0.0f);
                        }
                        shadowRenderContext.UpdateCamera();
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
                                shadowRenderContext.AddToBin(renderBinId, drawableList);
                            }
                        }
                    }

                    // show occluder bounding boxes
                    if (ShowOccluders)
                    {
                        foreach (KeyValuePair<int, List<Drawable>> renderBinKVP in shadowRenderContext.renderBins)
                        {
                            int renderBinId = renderBinKVP.Key;
                            List<Drawable> drawableList = renderBinKVP.Value;
                            renderContext.AddToBin(OCCLUDER_BOUNDING_BOX, drawableList);
                        }
                    }

                    FrustumBoundingSphere = lightCamera.bs;
                }

                // DEBUG STUFF

                // camera Frustum
                if (ShowFrustum && (frustumGeo != null))
                {
                    renderContext.AddToBin(FRUSTUM, frustumGeo);
                }

                // scene bounding box
                if (ShowSceneBoundingBox)
                {
                    sceneBoundingBoxGeo.BoundingVolume = sceneBoundingBox;
                    sceneBoundingBoxGeo.WorldBoundingVolume = sceneBoundingBox;
                    renderContext.AddToBin(BOUNDING_BOX, sceneBoundingBoxGeo);
                }

                // Frustum bounding box
                if (ShowFrustumBoundingBox)
                {
                    if (false)
                    {
                        Vector3[] corners = new Vector3[8];
                        renderContext.BoundingFrustum.GetCorners(corners);
                        Matrix m = shadowRenderContext.Camera.ViewMatrix;
                        Vector3.Transform(corners, ref m, corners);
                        //corners[0] = FrustumBoundingBox.Center + Vector3[];
                        //FrustumBoundingBox.Transform(lightCamera.ViewMatrix, FrustumBoundingBox);
                        frustumBoundingBoxGeo = GeometryUtil.CreateFrustum("FRUSTUM_BOUNDING_BOX", corners);
                        frustumBoundingBoxGeo.RenderGroupId = VECTOR;
                        frustumBoundingBoxGeo.Initialize(GraphicsDevice);

                        renderContext.AddToBin(frustumBoundingBoxGeo);
                    }
                    else
                    {
                        frustumBoundingBoxGeo.BoundingVolume = FrustumBoundingBox;
                        frustumBoundingBoxGeo.WorldBoundingVolume = FrustumBoundingBox;
                        renderContext.AddToBin(BOUNDING_BOX, frustumBoundingBoxGeo);
                    }
                }
                if (ShowFrustumBoundingSphere && FrustumBoundingSphere != null)
                {
                    frustumBoundingSphereGeo.BoundingVolume = FrustumBoundingSphere;
                    frustumBoundingSphereGeo.WorldBoundingVolume = FrustumBoundingSphere;
                    renderContext.AddToBin(BOUNDING_SPHERE, frustumBoundingSphereGeo);
                }

                // occluder bounding box
                if (ShowOccluderBoundingBox)
                {
                    occluderBoundingBoxGeo.BoundingVolume = occluderBoundingBox;
                    occluderBoundingBoxGeo.WorldBoundingVolume = occluderBoundingBox;
                    renderContext.AddToBin(BOUNDING_BOX, occluderBoundingBoxGeo);
                }

                // light Frustum
                if (ShowLightFrustum && lightCamera != null)
                {
                    BoundingFrustum lightFrustum = lightCamera.BoundingFrustum;
                    lightFrustumGeo = GeometryUtil.CreateFrustum("Frustum", lightFrustum);
                    lightFrustumGeo.RenderGroupId = FRUSTUM;
                    lightFrustumGeo.Initialize(GraphicsDevice);

                    renderContext.AddToBin(FRUSTUM, lightFrustumGeo);
                }

                // shadow map texture
                if (ShowShadowMap)
                {
                    // TODO move away..
                    renderContext.AddToBin(HUD, billboardNode);
                }
            }

            if (CaptureFrustum)
            {
                CaptureFrustum = false;
                if (frustumGeo == null)
                {
                    BoundingFrustum boundingFrustum = cameraComponent.BoundingFrustum;

                    frustumGeo = GeometryUtil.CreateFrustum("Frustum", boundingFrustum);
                    frustumGeo.RenderGroupId = FRUSTUM;
                    frustumGeo.Initialize(GraphicsDevice);
                    //FrustumGeo.UpdateTransform();
                    //FrustumGeo.UpdateWorldTransform(null);

                    // tweak camera zfar...
                    // TODO restore zfar later...
                    cameraComponent.SetZFar(2000);
                }
                else
                {
                    // TODO restore zfar!
                    frustumGeo.Dispose();
                    frustumGeo = null;
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            RenderContext shadowRenderContext = null;
            if (true)
            {
                for (int i = 0; i < renderContext.lightNodes.Count; i++)
                {
                    LightNode lightNode = renderContext.lightNodes[i];
                    shadowRenderContext = renderContext.lightRenderContextes[i];

                    Render(shadowRenderContext, shadowRenderContext.renderBins, renderers2);

                    billboardNode.Texture = shadowRenderContext.RenderTarget;

                    // HACK
                    VoxelEffect voxelEffect = ((VoxelRenderer)renderers[VOXEL]).effect;
                    voxelEffect.DirectionalLight0.Direction = shadowRenderContext.Camera.ViewDirection;
                    voxelEffect.LightWorldViewProj = shadowRenderContext.Camera.ViewProjectionMatrix;
                    voxelEffect.ShadowMapTexture = shadowRenderContext.RenderTarget;
                }
            }

            Render(renderContext, renderContext.renderBins, renderers);

            renderContext.ShowStats("Scene");
        }

        public void Render(RenderContext renderContext, SortedDictionary<int, List<Drawable>> renderBins, Dictionary<int, Renderer> renderers)
        {
            BlendState oldBlendState = GraphicsDevice.BlendState;
            DepthStencilState oldDepthStencilState = GraphicsDevice.DepthStencilState;
            RasterizerState oldRasterizerState = GraphicsDevice.RasterizerState;
            SamplerState oldSamplerState = GraphicsDevice.SamplerStates[0];

            // Set the render target
            GraphicsDevice.SetRenderTarget(renderContext.RenderTarget);

            GraphicsDevice.Clear(ClearOptions.Target, renderContext.ClearColor, 0, 0);

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
                    Console.WriteLine("No renderer found for render group " + renderBinId);
                }
            }

            // Drop the render target
            GraphicsDevice.SetRenderTarget(null);

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
                        ctxt.VisitOrder, VOXEL_OCTREE_CULL_VISITOR, null, VOXEL_OCTREE_CULL_POST_VISITOR, ref arg);
                    return true;
                }
                else if (node is Drawable drawable)
                {
                    BoundingVolume bv = drawable.WorldBoundingVolume;
                    bool cull = false;
                    if (bv != null)
                    {
                        BoundingFrustum boundingFrustum = ctxt.BoundingFrustum;
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
                else if (!ctxt.Light && node is LightNode lightNode)
                {
                    ctxt.AddLightNode(lightNode);
                }
                // TODO should return !cull
                return true;
            };

        static float minA = float.MaxValue;

        private static readonly VoxelOctree.Visitor<VoxelChunk> VOXEL_OCTREE_CULL_VISITOR = delegate (Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node, ref Object arg)
        {
            RenderContext ctxt = arg as RenderContext;

            bool culled = false;

            Bounding.BoundingBox boundingBox = (node.obj.Drawable != null) ?
                node.obj.Drawable.BoundingVolume as Bounding.BoundingBox : node.obj.BoundingBox;

            // Frustum culling
            if (!culled && ctxt.FrustumCullingEnabled)
            {
                // TODO start from node that fully encompass the Frustum (no need to recurse from root)
                // when recursing down some collision tests are not needed (possible to extend that from frame to frame)
                // see TODO.txt for links that show how to do that

                // http://www.cse.chalmers.se/~uffe/vfc.pdf
                // https://cesium.com/blog/2017/02/02/tighter-frustum-culling-and-why-you-may-want-to-disregard-it/
                // https://github.com/labnation/MonoGame/blob/master/MonoGame.Framework/BoundingFrustum.cs
                // https://github.com/labnation/MonoGame/blob/master/MonoGame.Framework/BoundingBox.cs

                BoundingFrustum boundingFrustum = ctxt.BoundingFrustum;

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
                    // take ownership of Frustum culling
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
                Vector3 centerDist;
                centerDist.X = Math.Abs(center.X - ctxt.CameraPosition.X);
                centerDist.Y = Math.Abs(center.Y - ctxt.CameraPosition.Y);
                centerDist.Z = Math.Abs(center.Z - ctxt.CameraPosition.Z);

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
                    // take ownership of Frustum culling
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
                Vector3 n = ctxt.Camera.ViewDirection;

                // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
                float r = halfSize.X * Math.Abs(n.X) + halfSize.Y * Math.Abs(n.Y) + halfSize.Z * Math.Abs(n.Z);

                // FIXME performance no need to compute d each time...
                Vector3 p = ctxt.Camera.Position;
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
                    float a = VectorUtil.BBoxArea(ref ctxt.CameraPosition, ref node.obj.BoundingBox, ctxt.ProjectToScreen);
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

                    ctxt.sceneMax.X = Math.Max(ctxt.sceneMax.X, boundingBox.Center.X + boundingBox.HalfSize.X);
                    ctxt.sceneMax.Y = Math.Max(ctxt.sceneMax.Y, boundingBox.Center.Y + boundingBox.HalfSize.Y);
                    ctxt.sceneMax.Z = Math.Max(ctxt.sceneMax.Z, boundingBox.Center.Z + boundingBox.HalfSize.Z);
                    ctxt.sceneMin.X = Math.Min(ctxt.sceneMin.X, boundingBox.Center.X - boundingBox.HalfSize.X);
                    ctxt.sceneMin.Y = Math.Min(ctxt.sceneMin.Y, boundingBox.Center.Y - boundingBox.HalfSize.Y);
                    ctxt.sceneMin.Z = Math.Min(ctxt.sceneMin.Z, boundingBox.Center.Z - boundingBox.HalfSize.Z);
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

        public static void AddBBoxHull(RenderContext ctxt, ref Bounding.BoundingBox boundingBox)
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
                Vector3[] hull = VectorUtil.BBoxHull(ref ctxt.CameraPosition, ref boundingBox);
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
                Vector3[] hull = VectorUtil.BBoxProjectedHull(ref ctxt.CameraPosition, ref boundingBox, ctxt.ProjectToScreen);
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
