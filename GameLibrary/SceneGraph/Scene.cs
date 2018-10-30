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
    public class Scene
    {
        private ICameraComponent cameraComponent;

        private Node rootNode;

        private readonly Dictionary<int, Renderer> renderers;

        private Dictionary<int, List<GeometryNode>> renderGroups;
        private Dictionary<int, List<GeometryNode>> collisionGroups;

        private MeshNode boundingSphereGeo;
        private MeshNode boundingBoxGeo;

        private BoundingFrustum boundingFrustum;
        private MeshNode frustrumGeo;

        private Matrix previousProjectionMatrix = Matrix.Identity;
        private Matrix previousViewMatrix = Matrix.Identity;

        Dictionary<int, LinkedList<Collision>> collisionCache = new Dictionary<int, LinkedList<Collision>>();
        //LinkedList<GameLibrary.Util.Intersection> intersections = new LinkedList<GameLibrary.Util.Intersection>();

        // Settings 
        public Boolean Debug;
        public Boolean ShowBoundingVolumes = false;
        public Boolean ShowCulledBoundingVolumes = false;
        public Boolean ShowCollisionVolumes = true;
        public Boolean ShowFrustrum;
        public Boolean CaptureFrustrum;

        // Stats
        public int cullCount;
        public int renderCount;

        public GraphicsDevice GraphicsDevice;

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

        public BoundingFrustum BoundingFrustum
        {
            get
            {
                if (boundingFrustum != null)
                {
                    return boundingFrustum;
                }
                return cameraComponent.BoundingFrustum;
            }
        }

        public Scene()
        {
            renderers = new Dictionary<int, Renderer>();

            renderGroups = new Dictionary<int, List<GeometryNode>>();

            collisionGroups = new Dictionary<int, List<GeometryNode>>();

            //rootNode = new TransformNode("ROOT");
            //rootNode.Scene = this;
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

        //public static int VOXEL_MAP = 10;
        public static int VOXEL = 12;
        public static int OCTREE = 15;

        public static int FRUSTRUM = 20;

        public static int BOUNDING_SPHERE = 30;
        public static int BOUNDING_BOX = 31;

        public static int CULLED_BOUNDING_SPHERE = 32;
        public static int CULLED_BOUNDING_BOX = 33;

        public static int COLLISION_SPHERE = 40;
        public static int COLLISION_BOX = 41;

        public void Initialize()
        {
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

            renderers[VOXEL] = new EffectRenderer(EffectFactory.CreateVoxelEffect(GraphicsDevice)); // 1 light + texture
            //renderers[VOXEL] = new EffectRenderer(EffectFactory.CreateBasicEffect1(GraphicsDevice)); // 3 lights
            //renderers[VOXEL].RasterizerState = RasterizerState.CullNone;
            //renderers[VOXEL].RasterizerState = Renderer.WireFrameRasterizer;

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

            rootNode.Visit(COMMIT_VISITOR, this);
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

            // handle collisions
            ClearGroups(collisionGroups);
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

            bool cameraDirty = false;
            cameraDirty = cameraDirty || (!previousProjectionMatrix.Equals(cameraComponent.ProjectionMatrix));
            cameraDirty = cameraDirty || (!previousViewMatrix.Equals(cameraComponent.ViewMatrix));

            bool sceneDirty = true;

            // do the RENDER_GROUP_VISITOR only if:
            // - camera is dirty
            // - TODO scene is dirty (something has moved in the scene)
            // - scene structure is dirty
            if (cameraDirty || sceneDirty || structureDirty || (renderGroups.Count == 0))
            {
                if (cameraDirty)
                {
                    //Console.WriteLine("Camera Dirty");
                    previousProjectionMatrix = cameraComponent.ProjectionMatrix;
                    previousViewMatrix = cameraComponent.ViewMatrix;
                }
                if (structureDirty)
                {
                    Console.WriteLine("Structure Dirty");
                }
                ClearGroups(renderGroups);

                cullCount = 0;
                renderCount = 0;
                // FIXME most of the time nodes stay in the same render group
                // so doing a clear + full reconstruct is not very efficient
                // but RENDER_GROUP_VISITOR does the culling...
                rootNode.Visit(RENDER_GROUP_VISITOR, this);
                if (Debug)
                {
                    Console.WriteLine(cullCount + " / " + renderCount);
                }
            }

            if (CaptureFrustrum)
            {
                // TODO use captured frustrum for culling (using a dummy camera?)
                CaptureFrustrum = false;
                boundingFrustum = CameraComponent.BoundingFrustum;
                frustrumGeo = GeometryUtil.CreateFrustrum("FRUSTRUM", boundingFrustum);
                //frustrumGeo.Scene = this;
                frustrumGeo.RenderGroupId = FRUSTRUM;
                //frustrumGeo.Scale = new Vector3(0.9f);
                //frustrumGeo.Translation = new Vector3(0, -1f, 0);
                frustrumGeo.Initialize(GraphicsDevice);
                frustrumGeo.UpdateTransform();
                frustrumGeo.UpdateWorldTransform(null);
            }
            if (ShowFrustrum && (frustrumGeo != null))
            {
                AddToGroups(renderGroups, FRUSTRUM, frustrumGeo);
            }
        }

        public void Draw(GameTime gameTime)
        {
            Render(renderGroups, renderers);
        }

        private GraphicsContext gc = new GraphicsContext();

        public void Render(Dictionary<int, List<GeometryNode>> renderGroups, Dictionary<int, Renderer> renderers)
        {
            BlendState oldBlendState = GraphicsDevice.BlendState;
            DepthStencilState oldDepthStencilState = GraphicsDevice.DepthStencilState;
            RasterizerState oldRasterizerState = GraphicsDevice.RasterizerState;
            SamplerState oldSamplerState = GraphicsDevice.SamplerStates[0];

            // FIXME should iterate over ordered by key...
            Renderer renderer;
            foreach (KeyValuePair<int, List<GeometryNode>> nodeListKVP in renderGroups)
            {
                int renderGroupId = nodeListKVP.Key;
                List<GeometryNode> nodeList = nodeListKVP.Value;

                if (Debug) Console.WriteLine(renderGroupId + " " + nodeList.Count);

                renderers.TryGetValue(renderGroupId, out renderer);
                if (renderer != null)
                {
                    gc.GraphicsDevice = GraphicsDevice;
                    gc.Camera = CameraComponent;

                    renderer.Render(gc, nodeList);
                }
                else
                {
                    if (Debug) Console.WriteLine("No renderer found for render group " + renderGroupId);
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

        private static Node.Visitor UPDATE_POST_VISITOR = delegate (Node node, ref Object arg)
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

        private static Node.Visitor COMMIT_VISITOR = delegate (Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            //Console.WriteLine("Commiting " + node.Name);
            if (node is GroupNode groupNode)
            {
                Scene scene = arg as Scene;
                groupNode.Commit(scene.GraphicsDevice);
            }
            // do we need to recurse
            bool recurse = false;
            recurse = recurse || node.IsDirty(Node.DirtyFlag.ChildStructure);
            node.ClearDirty(Node.DirtyFlag.ChildStructure);
            return recurse;
        };

        private static Node.Visitor CONTROLLER_VISITOR = delegate (Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            //Console.WriteLine("Updating " + node.Name);
            GameTime gameTime = arg as GameTime;
            foreach (Controller controller in node.Controllers)
            {
                if (!node.Enabled) break; // no need to continue...
                controller.Update(gameTime);
            }
            return true;
        };

        private static Node.Visitor RENDER_GROUP_VISITOR = delegate (Node node, ref Object arg)
        {
            if (!node.Visible) return false;
            if (node is VoxelOctreeGeometry voxelOctreeGeometry)
            {
                voxelOctreeGeometry.voxelOctree.Visit(VOXEL_OCTREE_RENDER_GROUP_VISITOR, arg);
                return true;
            }
            else if (node is GeometryNode geometryNode)
            {
                Scene scene = arg as Scene;

                BoundingVolume bv = geometryNode.WorldBoundingVolume;
                bool cull = false;
                if (bv != null)
                {
                    BoundingFrustum boundingFrustum = scene.BoundingFrustum;
                    //Console.WriteLine(boundingFrustum);
                    if (bv.IsContained(boundingFrustum) == ContainmentType.Disjoint)
                    {
                        //Console.WriteLine("Culling " + node.Name);
                        cull = true;
                        scene.cullCount++;
                    }
                }
                if (!cull)
                {
                    scene.AddToGroups(scene.renderGroups, geometryNode);
                    scene.renderCount++;

                    if (geometryNode.BoundingVolumeVisible)
                    {
                        Boolean collided = (scene.collisionCache != null) ? scene.collisionCache.ContainsKey(geometryNode.Id) : false;
                        if (collided && scene.ShowCollisionVolumes)
                        {
                            if (geometryNode.BoundingVolume.GetBoundingType() == BoundingType.Sphere)
                            {
                                scene.AddToGroups(scene.renderGroups, COLLISION_SPHERE, geometryNode);
                            }
                            else
                            {
                                scene.AddToGroups(scene.renderGroups, COLLISION_BOX, geometryNode);
                            }
                        }
                        else if (scene.ShowBoundingVolumes)
                        {
                            if (geometryNode.BoundingVolume.GetBoundingType() == BoundingType.Sphere)
                            {
                                scene.AddToGroups(scene.renderGroups, BOUNDING_SPHERE, geometryNode);
                            }
                            else
                            {
                                scene.AddToGroups(scene.renderGroups, BOUNDING_BOX, geometryNode);
                            }
                        }
                    }
                }
                else
                {
                    if (geometryNode.BoundingVolumeVisible)
                    {
                        if (scene.ShowCulledBoundingVolumes)
                        {
                            if (geometryNode.BoundingVolume.GetBoundingType() == BoundingType.Sphere)
                            {
                                scene.AddToGroups(scene.renderGroups, CULLED_BOUNDING_SPHERE, geometryNode);
                            }
                            else
                            {
                                scene.AddToGroups(scene.renderGroups, CULLED_BOUNDING_BOX, geometryNode);
                            }
                        }
                    }
                }
            }
            // TODO should return !cull
            return true;
        };

        private static VoxelOctree.Visitor VOXEL_OCTREE_RENDER_GROUP_VISITOR = delegate (Octree<VoxelObject> octree, OctreeNode<VoxelObject> node, ref Object arg)
        {
            Scene scene = arg as Scene;

            // FIXME this is a performance bottleneck
            Bounding.BoundingBox bv = new Bounding.BoundingBox();
            octree.GetNodeBoundingBox(node, ref bv);

            BoundingFrustum boundingFrustum = scene.BoundingFrustum;
            //Console.WriteLine(boundingFrustum);
            bool cull = false;
            if (bv.IsContained(boundingFrustum) == ContainmentType.Disjoint)
            {
                //Console.WriteLine("Culling " + node.Name);
                cull = true;
                scene.cullCount++;
            }
            GeometryNode geometryNode = node.obj.GeometryNode;
            if (geometryNode != null)
            {
                if (!cull)
                {
                    if (geometryNode.Visible)
                    {
                        scene.AddToGroups(scene.renderGroups, geometryNode);
                        scene.renderCount++;
                    }
                    if (geometryNode.BoundingVolumeVisible)
                    {
                        Boolean collided = (scene.collisionCache != null) ? scene.collisionCache.ContainsKey(geometryNode.Id) : false;
                        if (collided && scene.ShowCollisionVolumes)
                        {
                            scene.AddToGroups(scene.renderGroups, COLLISION_BOX, geometryNode);
                        }
                        else if (scene.ShowBoundingVolumes)
                        {
                            scene.AddToGroups(scene.renderGroups, BOUNDING_BOX, geometryNode);
                        }
                    }
                }
                else
                {
                    // handle culled nodes
                    if (geometryNode.BoundingVolumeVisible)
                    {
                        if (scene.ShowCulledBoundingVolumes)
                        {
                            scene.AddToGroups(scene.renderGroups, CULLED_BOUNDING_BOX, geometryNode);
                        }
                    }
                }
            }
            return !cull;
        };

        private static Node.Visitor COLLISION_GROUP_VISITOR = delegate (Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            if (node is GeometryNode geometryNode)
            {
                Scene scene = arg as Scene;
                scene.AddToGroups(scene.collisionGroups, geometryNode.CollisionGroupId, geometryNode);
            }
            return true;
        };

        private static Node.Visitor DISPOSE_VISITOR = delegate (Node node, ref Object arg)
        {
            Console.WriteLine("Disposing " + node.Name);
            node.Dispose();
            return true;
        };

        private static Node.Visitor DUMP_VISITOR = delegate (Node node, ref Object arg)
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

        struct Collision
        {
            public GeometryNode node1;
            public GeometryNode node2;
        }

        private void checkCollisions(List<GeometryNode> nodes, Dictionary<int, LinkedList<Collision>> cache)
        {
            int n = 0;
            for (int i = 0; i < nodes.Count() - 1; i++)
            {
                for (int j = i + 1; j < nodes.Count(); j++)
                {
                    GeometryNode node1 = nodes[i];
                    GeometryNode node2 = nodes[j];
                    if (node1.WorldBoundingVolume != null && node2.WorldBoundingVolume != null && node1.WorldBoundingVolume.Intersects(node2.WorldBoundingVolume))
                    {
                        n++;
                        AddCollision(cache, node1, node2);
                    }
                }
            }
        }

        private void checkCollisions(List<GeometryNode> nodes1, List<GeometryNode> nodes2, Dictionary<int, LinkedList<Collision>> cache)
        {
            int n = 0;
            for (int i = 0; i < nodes1.Count(); i++)
            {
                GeometryNode node1 = nodes1[i];
                for (int j = 0; j < nodes2.Count(); j++)
                {
                    GeometryNode node2 = nodes2[j];
                    if (node1.WorldBoundingVolume.Intersects(node2.WorldBoundingVolume))
                    {
                        n++;
                        AddCollision(cache, node1, node2);
                    }
                }
            }
        }

        private static Collision AddCollision(Dictionary<int, LinkedList<Collision>> cache, GeometryNode node1, GeometryNode node2)
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

        internal void ClearGroups(Dictionary<int, List<GeometryNode>> groups)
        {
            foreach (KeyValuePair<int, List<GeometryNode>> nodeListKVP in groups)
            {
                List<GeometryNode> nodeList = nodeListKVP.Value;
                nodeList.Clear();
            }
        }

        internal void AddToGroups(Dictionary<int, List<GeometryNode>> groups, GeometryNode node)
        {
            AddToGroups(groups, node.RenderGroupId, node);
        }

        internal void AddToGroups(Dictionary<int, List<GeometryNode>> groups, int groupId, GeometryNode node)
        {
            if (groupId < 0)
            {
                return;
            }
            List<GeometryNode> list;
            if (!groups.TryGetValue(groupId, out list))
            {
                list = new List<GeometryNode>();
                groups[groupId] = list;
            }
            if (Debug && list.Contains(node))
            {
                throw new Exception("Node already in group " + groupId);
            }
            list.Add(node);
        }

    }

}
