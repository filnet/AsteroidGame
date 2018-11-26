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

namespace GameLibrary.SceneGraph
{
    public class Scene
    {
        private ICameraComponent cameraComponent;

        private Node rootNode;

        private readonly Dictionary<int, Renderer> renderers;

        private Dictionary<int, List<Node>> collisionGroups;

        private MeshNode boundingSphereGeo;
        private MeshNode boundingBoxGeo;

        // debug camera
        private MeshNode frustrumGeo;

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
        public Boolean ShowFrustrum;
        public Boolean CaptureFrustrum;

        public GraphicsDevice GraphicsDevice;

        public RenderContext renderContext;
        private readonly GraphicsContext gc = new GraphicsContext();

        public Node RootNode
        {
            get { return rootNode; }
            set { rootNode = value; }
        }

        public ICameraComponent CameraComponent
        {
            get { return cameraComponent; }
            set { cameraComponent = value; }
        }

        public Scene()
        {
            renderers = new Dictionary<int, Renderer>();
            collisionGroups = new Dictionary<int, List<Node>>();
        }

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

        public static int FRUSTRUM = 20;

        public static int BOUNDING_SPHERE = 30;
        public static int BOUNDING_BOX = 31;

        public static int CULLED_BOUNDING_SPHERE = 32;
        public static int CULLED_BOUNDING_BOX = 33;

        public static int COLLISION_SPHERE = 40;
        public static int COLLISION_BOX = 41;

        public static int HORTO = 45;

        public void Initialize()
        {
            //VectorUtil.CreateAABBAreaLookupTable();

            boundingSphereGeo = GeometryUtil.CreateGeodesicWF("BOUNDING_SPHERE", 1);
            //boundingSphereGeo.Scene = this;
            //boundingGeo.Scale = new Vector3(1.05f);
            //boundingSphereGeo.RenderGroupId = VECTOR;
            boundingSphereGeo.Initialize(GraphicsDevice);

            boundingBoxGeo = GeometryUtil.CreateCubeWF("BOUNDING_BOX", 1);
            //boundingBoxGeo.Scene = this;
            //boundingBoxGeo.Scale = new Vector3(1.05f);
            //boundingBoxGeo.RenderGroupId = VECTOR;
            boundingBoxGeo.Initialize(GraphicsDevice);

            renderers[THREE_LIGHTS] = new EffectRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice)); // 3 lights
            renderers[ONE_LIGHT] = new EffectRenderer(EffectFactory.CreateBasicEffect2(GraphicsDevice)); // 1 light
            renderers[NO_LIGHT] = new EffectRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light

            renderers[WIRE_FRAME] = new WireFrameRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light + wire frame
            renderers[VECTOR] = new EffectRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light
            renderers[CLIPPING] = new EffectRenderer(EffectFactory.CreateClippingEffect(GraphicsDevice));

            renderers[PLAYER] = new EffectRenderer(EffectFactory.CreateClippingEffect(GraphicsDevice));
            renderers[BULLET] = new EffectRenderer(EffectFactory.CreateBulletEffect(GraphicsDevice));
            renderers[ASTEROID] = new EffectRenderer(EffectFactory.CreateClippingEffect(GraphicsDevice));

            //renderers[VOXEL_MAP] = new VoxelMapRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice));
            //renderers[VOXEL_MAP] = new VoxelMapInstancedRenderer(EffectFactory.CreateInstancedEffect(GraphicsDevice));

            renderers[VOXEL] = new ShowTimeRenderer(new VoxelRenderer(VoxelUtil.CreateVoxelEffect(GraphicsDevice)));
            renderers[VOXEL_WATER] = new VoxelRenderer(VoxelUtil.CreateVoxelWaterEffect(GraphicsDevice));
            renderers[VOXEL_WATER].RasterizerState = RasterizerState.CullNone;
            renderers[VOXEL_WATER].BlendState = BlendState.AlphaBlend;


            //renderers[OCTREE] = new OctreeRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light
            //renderers[OCTREE] = new OctreeRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice)); // 3 lights

            renderers[FRUSTRUM] = new EffectRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice)); // no light

            bool clip = false;
            renderers[BOUNDING_SPHERE] = new BoundRenderer(EffectFactory.CreateBoundEffect(GraphicsDevice, clip), boundingSphereGeo); // clipping
            renderers[BOUNDING_BOX] = new BoundRenderer(EffectFactory.CreateBoundEffect(GraphicsDevice, clip), boundingBoxGeo); // clipping
            renderers[CULLED_BOUNDING_SPHERE] = new BoundRenderer(EffectFactory.CreateCulledBoundEffect(GraphicsDevice, clip), boundingSphereGeo); // clipping
            renderers[CULLED_BOUNDING_BOX] = new BoundRenderer(EffectFactory.CreateCulledBoundEffect(GraphicsDevice, clip), boundingBoxGeo); // clipping
            renderers[COLLISION_SPHERE] = new BoundRenderer(EffectFactory.CreateCollisionEffect(GraphicsDevice, clip), boundingSphereGeo); // clipping
            renderers[COLLISION_BOX] = new BoundRenderer(EffectFactory.CreateCollisionEffect(GraphicsDevice, clip), boundingBoxGeo); // clipping

            renderers[HORTO] = new HortographicRenderer(EffectFactory.CreateBasicEffect3(GraphicsDevice));

            rootNode.Initialize(GraphicsDevice);
            rootNode.Visit(COMMIT_VISITOR, this);

            renderContext = new RenderContext(GraphicsDevice, CameraComponent);
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

            // do the RENDER_GROUP_VISITOR only if:
            // - camera is dirty
            // - TODO scene is dirty (something has moved in the scene)
            // - scene structure is dirty
            if (cameraDirty || sceneDirty || structureDirty || (renderContext.renderBins.Count == 0))
            {
                if (cameraDirty)
                {
                    previousProjectionMatrix = cameraComponent.ProjectionMatrix;
                    previousViewMatrix = cameraComponent.ViewMatrix;
                    // TODO if frustrumGeo is null the previous camera position/etc will be used...
                    // and not the current one at the time the camera was frozen
                    if (frustrumGeo == null)
                    {
                        renderContext.UpdateCamera();
                    }
                }
                if (structureDirty)
                {
                    if (Debug) Console.WriteLine("Structure Dirty");
                }
                // FIXME most of the time nodes stay in the same render group
                // so doing a clear + full reconstruct is not very efficient
                // but RENDER_GROUP_VISITOR does the culling...
                renderContext.ClearBins();

                rootNode.Visit(RENDER_VISITOR, renderContext);
                if (Debug) Console.WriteLine(renderContext.FrustumCullCount + " / " + renderContext.RenderCount);
            }

            if (CaptureFrustrum)
            {
                CaptureFrustrum = false;
                if (frustrumGeo == null)
                {
                    BoundingFrustum boundingFrustum = CameraComponent.BoundingFrustum;
                    frustrumGeo = GeometryUtil.CreateFrustrum("FRUSTRUM", boundingFrustum);
                    frustrumGeo.RenderGroupId = FRUSTRUM;
                    frustrumGeo.Initialize(GraphicsDevice);
                    frustrumGeo.UpdateTransform();
                    frustrumGeo.UpdateWorldTransform(null);
                }
                else
                {
                    frustrumGeo.Dispose();
                    frustrumGeo = null;
                }
            }

            if (ShowFrustrum && (frustrumGeo != null))
            {
                renderContext.AddToBin(FRUSTRUM, frustrumGeo);
            }
        }

        public void Draw(GameTime gameTime)
        {
            Render(gameTime, renderContext.renderBins, renderers);
        }

        public void Render(GameTime gameTime, SortedDictionary<int, List<Drawable>> renderBins, Dictionary<int, Renderer> renderers)
        {
            BlendState oldBlendState = GraphicsDevice.BlendState;
            DepthStencilState oldDepthStencilState = GraphicsDevice.DepthStencilState;
            RasterizerState oldRasterizerState = GraphicsDevice.RasterizerState;
            SamplerState oldSamplerState = GraphicsDevice.SamplerStates[0];

            // FIXME should iterate over ordered by key...
            Renderer renderer;
            foreach (KeyValuePair<int, List<Drawable>> renderBinKVP in renderBins)
            {
                int renderBinId = renderBinKVP.Key;
                List<Drawable> nodeList = renderBinKVP.Value;
                if (nodeList.Count() == 0)
                {
                    // early exit !!!
                    break;
                }

                if (Debug) Console.WriteLine(renderBinId + " " + nodeList.Count);

                renderers.TryGetValue(renderBinId, out renderer);
                if (renderer != null)
                {
                    gc.GraphicsDevice = GraphicsDevice;
                    gc.Camera = CameraComponent;
                    gc.GameTime = gameTime;
                    renderer.Render(gc, nodeList);
                }
                else
                {
                    if (Debug) Console.WriteLine("No renderer found for render group " + renderBinId);
                }
            }

            GraphicsDevice.BlendState = oldBlendState;
            GraphicsDevice.DepthStencilState = oldDepthStencilState;
            GraphicsDevice.RasterizerState = oldRasterizerState;
            GraphicsDevice.SamplerStates[0] = oldSamplerState;
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
                        effectMatrices.Projection = CameraComponent.ProjectionMatrix;
                        effectMatrices.View = CameraComponent.ViewMatrix;
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

        private static readonly Node.Visitor RENDER_VISITOR = delegate (Node node, ref Object arg)
            {
                if (!node.Visible)
                {
                    return false;
                }

                RenderContext ctxt = arg as RenderContext;

                if (node is VoxelOctreeGeometry voxelOctreeGeometry)
                {
                    voxelOctreeGeometry.voxelOctree.ClearLoadQueue();
                    voxelOctreeGeometry.voxelOctree.Visit(
                        ctxt.VisitOrder, VOXEL_OCTREE_RENDER_VISITOR, null, VOXEL_OCTREE_RENDER_POST_VISITOR, ref arg);
                    return true;
                }
                else if (node is Drawable drawable)
                {
                    BoundingVolume bv = drawable.WorldBoundingVolume;
                    bool cull = false;
                    if (bv != null)
                    {
                        BoundingFrustum boundingFrustum = ctxt.BoundingFrustum;
                        if (bv.IsContained(boundingFrustum) == ContainmentType.Disjoint)
                        {
                            //if (Debug) Console.WriteLine("Culling " + node.Name);
                            cull = true;
                            ctxt.FrustumCullCount++;
                        }
                    }
                    if (!cull)
                    {
                        ctxt.AddToBin(drawable);
                        ctxt.RenderCount++;
                    }
                    if (drawable.BoundingVolumeVisible)
                    {
                        ctxt.AddBoundingVolume(drawable, cull);
                    }
                }
                // TODO should return !cull
                return true;
            };

        static float minA = float.MaxValue;
        private static readonly VoxelOctree.Visitor<VoxelChunk> VOXEL_OCTREE_RENDER_VISITOR = delegate (Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node, ref Object arg)
        {
            RenderContext ctxt = arg as RenderContext;

            bool culled = false;

            // frustrum culling
            if (!culled && ctxt.FrustrumCullingEnabled)
            {
                BoundingFrustum boundingFrustum = ctxt.BoundingFrustum;
                ContainmentType containmentType = node.obj.BoundingBox.IsContained(boundingFrustum);
                if (containmentType == ContainmentType.Disjoint)
                {
                    culled = true;
                    ctxt.FrustumCullCount++;
                    //Console.WriteLine("Frustrum Culling " + node.Name);
                }
                else if (containmentType == ContainmentType.Contains)
                {
                    // contained: no need to cull further down
                    ctxt.FrustrumCullingEnabled = false;
                    ctxt.FrustrumCullingOwner = node.locCode;
                    //Console.WriteLine("Fully contained");
                }
            }

            // distance culling
            if (!culled && ctxt.DistanceCullingEnabled)
            {
                // TODO move to BoundingBox
                // compute min and max distance (squared) of a point to an AABB
                // see http://mcmains.me.berkeley.edu/pubs/TVCG2010finalKrishnamurthyMcMains.pdf
                // the above reference also shows how to compute min and max distance (squared) between two AABBs
                Vector3 centerDist;
                centerDist.X = Math.Abs(node.obj.BoundingBox.Center.X - ctxt.CameraPosition.X);
                centerDist.Y = Math.Abs(node.obj.BoundingBox.Center.Y - ctxt.CameraPosition.Y);
                centerDist.Z = Math.Abs(node.obj.BoundingBox.Center.Z - ctxt.CameraPosition.Z);

                // max distance
                Vector3 dist;
                dist.X = centerDist.X + node.obj.BoundingBox.HalfSize.X;
                dist.Y = centerDist.Y + node.obj.BoundingBox.HalfSize.Y;
                dist.Z = centerDist.Z + node.obj.BoundingBox.HalfSize.Z;
                float maxDistanceSquared = (dist.X * dist.X) + (dist.Y * dist.Y) + (dist.Z * dist.Z);

                // min distance
                dist.X = Math.Max(centerDist.X - node.obj.BoundingBox.HalfSize.X, 0);
                dist.Y = Math.Max(centerDist.Y - node.obj.BoundingBox.HalfSize.Y, 0);
                dist.Z = Math.Max(centerDist.Z - node.obj.BoundingBox.HalfSize.Z, 0);
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
                    ctxt.DistanceCullingEnabled = false;
                    ctxt.DistanceCullingOwner = node.locCode;

                    //Console.WriteLine("Distance Culling: CONTAINED " + Octree<VoxelChunk>.LocCodeToString(node.locCode));
                    //Console.WriteLine("Distance Culling " + minDistanceSquared + ", " + maxDistanceSquared + " / " + ctxt.CullDistanceSquared);
                }
            }

            if (!culled && ctxt.ScreenSizeCullingEnabled)
            {
                //if (node.locCode == 0b1100)
                //{
                    AddBBoxHull(ctxt, ref node.obj.BoundingBox);
                    float a = VectorUtil.BBoxArea(ref ctxt.CameraPosition, ref node.obj.BoundingBox, ctxt.ProjectToScreen);
                    if (a != -1 && a < minA)
                    {
                        minA = a;
                        Console.WriteLine("* " + minA);
                    }
                    Console.WriteLine(a);
                //}
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
                    ctxt.RenderCount++;
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
                    ctxt.RenderCount++;
                }
            }
            return !culled;
        };

        private static readonly VoxelOctree.Visitor<VoxelChunk> VOXEL_OCTREE_RENDER_POST_VISITOR = delegate (Octree<VoxelChunk> octree, OctreeNode<VoxelChunk> node, ref Object arg)
        {
            RenderContext ctxt = arg as RenderContext;
            // restore culling flags
            if (ctxt.FrustrumCullingOwner == node.locCode)
            {
                ctxt.FrustrumCullingEnabled = true;
                ctxt.FrustrumCullingOwner = 0;
            }
            if (ctxt.DistanceCullingOwner == node.locCode)
            {
                ctxt.DistanceCullingEnabled = true;
                ctxt.DistanceCullingOwner = 0;
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
            bool addBorder = true;

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

    }
}
