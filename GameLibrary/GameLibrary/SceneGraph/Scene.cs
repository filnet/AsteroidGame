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

namespace GameLibrary.SceneGraph
{
    public class Scene
    {
        //private GameTime gameTime;
        private ICameraComponent cameraComponent;

        //private Effect effect;
        private GroupNode rootNode;

        private Dictionary<int, Effect> renderEffects;

        private Dictionary<int, List<GeometryNode>> renderGroups;
        private Dictionary<int, List<GeometryNode>> collisionGroups;

        private MeshNode geo;

        Dictionary<int, LinkedList<Collision>> collisionCache = new Dictionary<int, LinkedList<Collision>>();
        LinkedList<GameLibrary.Util.Intersection> intersections = new LinkedList<GameLibrary.Util.Intersection>();

        private bool debug = true;

        public GraphicsDevice GraphicsDevice
        {
            get;
            set;
        }

        public GroupNode RootNode
        {
            get { return rootNode; }
        }

        public Boolean ShowBoundingVolumes
        {
            get;
            set;
        }

        public ICameraComponent CameraComponent
        {
            get { return cameraComponent; }
            set { cameraComponent = value; }
        }

        public Scene()
        {
            renderEffects = new Dictionary<int, Effect>();
            renderGroups = new Dictionary<int, List<GeometryNode>>();

            collisionGroups = new Dictionary<int, List<GeometryNode>>();

            rootNode = new TransformNode("ROOT");
            rootNode.Scene = this;

            ShowBoundingVolumes = false;
        }

        public static int THREE_LIGHTS = 0;
        public static int ONE_LIGHT = 1;
        public static int NO_LIGHT = 2;
        public static int WIRE_FRAME = 3;
        public static int VECTOR = 4;
        public static int CLIPPING = 5;
        public static int BULLET = 6;
        public static int ASTEROID = 7;
        public static int BOUNDING = 8;
        public static int HW_INSTANCING = 9;

        public void Initialize()
        {
            //effect = createEffect();
            renderEffects[THREE_LIGHTS] = createBasicEffect1(); // 3 lights
            renderEffects[ONE_LIGHT] = createBasicEffect2(); // 1 light
            renderEffects[NO_LIGHT] = createBasicEffect3(); // no light
            renderEffects[WIRE_FRAME] = createBasicEffect3(); // no light + wire frame
            renderEffects[VECTOR] = createClippingEffect(); // vector
            renderEffects[CLIPPING] = createClippingEffect(); // clipping
            renderEffects[BULLET] = createBulletEffect(); // clipping
            renderEffects[ASTEROID] = createClippingEffect(); // clipping
            renderEffects[BOUNDING] = createBoundingEffect(); // clipping
            //renderEffects[HW_INSTANCING] = createHWInstancingEffect();

            geo = GeometryUtil.CreateGeodesicWF("BOUNDING_SPHERE", 1);
            geo.Scene = this;
            //geo.Scale = new Vector3(1.05f);
            geo.RenderGroupId = Scene.VECTOR;
            geo.Initialize();


            rootNode.Visit(COMMIT_VISITOR);
            rootNode.Initialize();
        }

        public void Dispose()
        {
            rootNode.Visit(DISPOSE_VISITOR);
        }

        public void Update(GameTime gameTime)
        {
            rootNode.Visit(CONTROLLER_VISITOR, gameTime);
            rootNode.Visit(COMMIT_VISITOR);

            rootNode.Visit(UPDATE_VISITOR, null, UPDATE_POST_VISITOR);
            //rootNode.Visit(DUMP_VISITOR);

            collisionGroups.Clear();
            rootNode.Visit(COLLISION_GROUP_VISITOR, this);

            collisionCache.Clear();
            if (collisionGroups.ContainsKey(0))
            {
                checkCollisions(collisionGroups[0], collisionCache);
                if (collisionGroups.ContainsKey(1))
                {
                    checkCollisions(collisionGroups[0], collisionGroups[1], collisionCache);
                }
                if (collisionGroups.ContainsKey(2))
                {
                    checkCollisions(collisionGroups[0], collisionGroups[2], collisionCache);
                }
            }
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
        }

        public void Draw(GameTime gameTime)
        {
            renderGroups.Clear();

            rootNode.Visit(RENDER_GROUP_VISITOR, this);

            BlendState oldBlendState = GraphicsDevice.BlendState;
            DepthStencilState oldDepthStencilState = GraphicsDevice.DepthStencilState;
            RasterizerState oldRasterizerState = GraphicsDevice.RasterizerState;
            SamplerState oldSamplerState = GraphicsDevice.SamplerStates[0];

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            //GraphicsDevice.RasterizerState = WireFrameRasterizer;
            GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            GeometryNode.DrawContext dc = new GeometryNode.DrawContext();
            dc.scene = this;
            dc.gameTime = gameTime;

            foreach (KeyValuePair<int, List<GeometryNode>> nodeListKVP in renderGroups)
            {
                int renderGroupId = nodeListKVP.Key;

                if (renderGroupId == 2)
                {
                    GraphicsDevice.RasterizerState = WireFrameRasterizer;
                }
                else
                {
                    GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                }

                IEffectMatrices effectMatrices = null;
                Effect effect = renderEffects[renderGroupId];
                if (effect is IEffectMatrices)
                {
                    effectMatrices = effect as IEffectMatrices;
                }
                if (effectMatrices != null)
                {
                    effectMatrices.Projection = CameraComponent.ProjectionMatrix;
                    effectMatrices.View = CameraComponent.ViewMatrix;
                }
                dc.effect = effect;
                List<GeometryNode> nodeList = nodeListKVP.Value;
                //Console.Out.WriteLine(renderGroupId + " " + nodeList.Count);
                foreach (GeometryNode node in nodeList)
                {
                    if (!node.Visible) continue;

                    //xxxxx
                    //effect.CurrentTechnique = effect.Techniques["HardwareInstancing"];
                    if (effectMatrices != null)
                    {
                        effectMatrices.World = node.WorldTransform;
                    }
                    node.preDraw(dc);
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        dc.pass = pass;
                        node.Draw(dc);
                    }
                    node.postDraw(dc);
                }
            }

            if (ShowBoundingVolumes)
            {
                drawBounding(gameTime);
            }
            drawIntersection(gameTime);

            GraphicsDevice.BlendState = oldBlendState;
            GraphicsDevice.DepthStencilState = oldDepthStencilState;
            GraphicsDevice.RasterizerState = oldRasterizerState;
            GraphicsDevice.SamplerStates[0] = oldSamplerState;
        }

        private void drawBounding(GameTime gameTime)
        {
            GeometryNode.DrawContext dc = new GeometryNode.DrawContext();
            dc.scene = this;
            dc.gameTime = gameTime;

            Effect effect = renderEffects[BOUNDING];
            IEffectMatrices effectMatrices = effect as IEffectMatrices;
            if (effectMatrices != null)
            {
                effectMatrices.Projection = CameraComponent.ProjectionMatrix;
                effectMatrices.View = CameraComponent.ViewMatrix;
                effectMatrices.World = Matrix.Identity; // node.WorldMatrix;
            }
            dc.effect = effect;

            GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            //GraphicsDevice.BlendState = BlendState.AlphaBlend;

            foreach (KeyValuePair<int, List<GeometryNode>> nodeListKVP in renderGroups)
            {
                List<GeometryNode> nodeList = nodeListKVP.Value;
                foreach (GeometryNode node in nodeList)
                {
                    if (!node.Visible) continue;
                    if (!node.BoundingVolumeVisible) continue;
                    GameLibrary.SceneGraph.Bounding.BoundingSphere boundingSphere = node.WorldBoundingVolume as GameLibrary.SceneGraph.Bounding.BoundingSphere;
                    if (boundingSphere != null && effectMatrices != null)
                    {
                        //effectMatrices.World = node.WorldMatrix;// *Matrix.CreateScale(boundingSphere.Radius); // node.WorldMatrix;
                        effectMatrices.World = Matrix.CreateScale(boundingSphere.Radius) * Matrix.CreateTranslation(boundingSphere.Center); // node.WorldMatrix;
                    }
                    if (boundingSphere != null)
                    {
                        Boolean collided = collisionCache != null ? collisionCache.ContainsKey(node.Id) : false;
                        StockEffects.ClippingEffect clippingEffect = effect as StockEffects.ClippingEffect;
                        if (collided && clippingEffect != null)
                        {
                            Color c = Color.Red;
                            clippingEffect.Color = new Color(c.R, c.G, c.B, 128);
                        }
                        geo.preDraw(dc);
                        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            dc.pass = pass;
                            geo.Draw(dc);
                        }
                        geo.postDraw(dc);
                        if (collided && clippingEffect != null)
                        {
                            Color c = Color.LightGreen;
                            clippingEffect.Color = new Color(c.R, c.G, c.B, 128);
                        }
                    }
                }
            }
        }

        private void drawIntersection(GameTime gameTime)
        {
            GeometryNode.DrawContext dc = new GeometryNode.DrawContext();
            dc.scene = this;
            dc.gameTime = gameTime;

            Effect effect = renderEffects[BOUNDING];
            IEffectMatrices effectMatrices = effect as IEffectMatrices;
            if (effectMatrices != null)
            {
                effectMatrices.Projection = CameraComponent.ProjectionMatrix;
                effectMatrices.View = CameraComponent.ViewMatrix;
                effectMatrices.World = Matrix.Identity; // node.WorldMatrix;
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
                geo.preDraw(dc);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    dc.pass = pass;
                    geo.Draw(dc);
                }
                geo.postDraw(dc);
            }
        }

        private static Node.Visitor UPDATE_VISITOR = delegate(Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            TransformNode transformNode = node as TransformNode;
            if (transformNode != null)
            {
                transformNode.UpdateTransform();
                transformNode.UpdateWorldTransform(arg as TransformNode);
                // propagate parent transform node
                arg = transformNode;
            }
            // do we need to recurse
            bool childDirty = node.IsDirty(Node.DirtyFlag.ChildTransform);
            if (!childDirty) {
                childDirty = node.IsDirty(Node.DirtyFlag.ChildWorldTransform);
            }
            return childDirty;
        };

        private static Node.Visitor UPDATE_POST_VISITOR = delegate(Node node, ref Object arg)
        {
            if (node.IsDirty(Node.DirtyFlag.ChildTransform))
            {
                node.ClearDirty(Node.DirtyFlag.ChildTransform);
            }
            if (node.IsDirty(Node.DirtyFlag.ChildWorldTransform))
            {
                node.ClearDirty(Node.DirtyFlag.ChildWorldTransform);
            }
            // for non transform nodes
            if (node.IsDirty(Node.DirtyFlag.WorldTransform))
            {
                node.ClearDirty(Node.DirtyFlag.WorldTransform);
            }
            return true;
        };

        private static Node.Visitor DUMP_VISITOR = delegate(Node node, ref Object arg)
        {
            int level = (arg != null) ? (int) arg : 0;
            Console.Out.Write("".PadLeft(level, ' '));
            Console.Out.WriteLine(node.Name.PadRight(30 - level, ' ') + " " + node.DirtyFlagsAsString());
            arg = level + 1;
            return true;
        };

        private static Node.Visitor DISPOSE_VISITOR = delegate(Node node, ref Object arg)
        {
            Console.Out.WriteLine("Disposing " + node.Name);
            node.Dispose();
            return true;
        };

        private static Node.Visitor COMMIT_VISITOR = delegate(Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            //Console.Out.WriteLine("Commiting " + node.Name);
            if (node is GroupNode)
            {
                ((GroupNode) node).Commit();
            }
            return true;
        };

        private static Node.Visitor CONTROLLER_VISITOR = delegate(Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            //Console.Out.WriteLine("Updating " + node.Name);
            GameTime gameTime = arg as GameTime;
            foreach (Controller controller in node.Controllers)
            {
                if (!node.Enabled) break; // no need to continue...
                controller.Update(gameTime);
            }
            return true;
        };

        private static Node.Visitor RENDER_GROUP_VISITOR = delegate(Node node, ref Object arg)
        {
            if (!node.Visible) return false;
            GeometryNode geometryNode = node as GeometryNode;
            if (geometryNode != null)
            {
                bool cull = false;
                Scene scene = arg as Scene;
                GameLibrary.SceneGraph.Bounding.BoundingSphere bs = geometryNode.WorldBoundingVolume as GameLibrary.SceneGraph.Bounding.BoundingSphere;
                if (bs != null)
                {
                    Microsoft.Xna.Framework.BoundingSphere xbs = new Microsoft.Xna.Framework.BoundingSphere(bs.Center, bs.Radius);
                    BoundingFrustum boundingFrustum = scene.CameraComponent.BoundingFrustum;
                    //Console.Out.WriteLine(boundingFrustum);
                    if (scene.CameraComponent.BoundingFrustum.Contains(xbs) == ContainmentType.Disjoint)
                    {
                        //Console.Out.WriteLine("Culling " + node.Name);
                        cull = true;
                    }
                }
                else
                {
                    //Console.Out.WriteLine("No bounds " + node.Name);
                }
                if (!cull)
                {
                    scene.addToRenderGroup(geometryNode);
                }
            }
            return true;
        };

        private static Node.Visitor COLLISION_GROUP_VISITOR = delegate(Node node, ref Object arg)
        {
            if (!node.Enabled) return false;
            GeometryNode geometryNode = node as GeometryNode;
            if (geometryNode != null)
            {
                Scene scene = arg as Scene;
                scene.addToCollisionGroup(geometryNode);
            }
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
                        addCollision(cache, node1, node2);
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
                        addCollision(cache, node1, node2);
                    }
                }
            }
        }

        private static Collision addCollision(Dictionary<int, LinkedList<Collision>> cache, GeometryNode node1, GeometryNode node2)
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

        internal void addToRenderGroup(GeometryNode node)
        {
            int groupId = node.RenderGroupId;
            if (groupId < 0)
            {
                return;
            }
            List<GeometryNode> list;
            if (!renderGroups.TryGetValue(groupId, out list))
            {
                list = new List<GeometryNode>();
                renderGroups[groupId] = list;
            }
            if (debug && list.Contains(node))
            {
                throw new Exception("Node already in render groupkp " + groupId);
            }
            list.Add(node);
        }

        internal void addToCollisionGroup(GeometryNode node)
        {
            int groupId = node.CollisionGroupId;
            if (groupId < 0)
            {
                return;
            }
            List<GeometryNode> list;
            if (!collisionGroups.TryGetValue(groupId, out list))
            {
                list = new List<GeometryNode>();
                collisionGroups[groupId] = list;
            }
            if (debug && list.Contains(node))
            {
                throw new Exception("Node already in collision group " + groupId);
            }
            list.Add(node);
        }

        private void removeFromRenderGroup(int groupId, GeometryNode node)
        {
            List<GeometryNode> list = renderGroups[groupId];
            if (list != null)
            {
                list.Remove(node);
            }
        }

        public static RasterizerState WireFrameRasterizer = new RasterizerState()
        {
            CullMode = CullMode.None,
            FillMode = FillMode.WireFrame,
        };

        private Effect createClippingEffect()
        {
            //StockEffects.ClippingEffect effect = new StockEffects.ClippingEffect(GraphicsDevice);

            //effect.World = Matrix.Identity;

            //// primitive color
            ////effect.VertexColorEnabled = true;

            //
            StockEffects.ClippingEffect effect = new StockEffects.ClippingEffect(GraphicsDevice);

            effect.ClippingPlane1 = VectorUtil.CreatePlane(Vector3.Right, 3.0f);
            effect.ClippingPlane2 = VectorUtil.CreatePlane(Vector3.Left, 3.0f);
            //effect.ClippingPlane2 = new Vector4(1.0f, 0.0f, 0.0f, 3.0f);

            effect.ClippingPlane3 = VectorUtil.CreatePlane(Vector3.Up, 3.0f);
            effect.ClippingPlane4 = VectorUtil.CreatePlane(Vector3.Down, 3.0f);

            effect.Color = Color.White;

            return effect;
        }

        private Effect createBulletEffect()
        {
            //StockEffects.ClippingEffect effect = new StockEffects.ClippingEffect(GraphicsDevice);

            //effect.World = Matrix.Identity;

            //// primitive color
            ////effect.VertexColorEnabled = true;

            //
            StockEffects.ClippingEffect effect = new StockEffects.ClippingEffect(GraphicsDevice);

            effect.ClippingPlane1 = VectorUtil.CreatePlane(Vector3.Right, 3.0f);
            effect.ClippingPlane2 = VectorUtil.CreatePlane(Vector3.Left, 3.0f);
            //effect.ClippingPlane2 = new Vector4(1.0f, 0.0f, 0.0f, 3.0f);

            effect.ClippingPlane3 = VectorUtil.CreatePlane(Vector3.Up, 3.0f);
            effect.ClippingPlane4 = VectorUtil.CreatePlane(Vector3.Down, 3.0f);

            effect.Color = Color.Yellow;

            return effect;
        }

        private Effect createBoundingEffect()
        {
            //StockEffects.ClippingEffect effect = new StockEffects.ClippingEffect(GraphicsDevice);

            //effect.World = Matrix.Identity;

            //// primitive color
            ////effect.VertexColorEnabled = true;

            //
            StockEffects.ClippingEffect effect = new StockEffects.ClippingEffect(GraphicsDevice);

            effect.ClippingPlane1 = VectorUtil.CreatePlane(Vector3.Right, 3.0f);
            effect.ClippingPlane2 = VectorUtil.CreatePlane(Vector3.Left, 3.0f);
            //effect.ClippingPlane2 = new Vector4(1.0f, 0.0f, 0.0f, 3.0f);

            effect.ClippingPlane3 = VectorUtil.CreatePlane(Vector3.Up, 3.0f);
            effect.ClippingPlane4 = VectorUtil.CreatePlane(Vector3.Down, 3.0f);

            Color c = Color.LightGreen;
            effect.Color = new Color(c.R, c.G, c.B, 128);

            return effect;
        }

        private Effect createBasicEffect1()
        {
            BasicEffect basicEffect = new BasicEffect(GraphicsDevice);

            basicEffect.World = Matrix.Identity;

            // primitive color
            basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            basicEffect.SpecularPower = 5.0f;
            basicEffect.Alpha = 1.0f;

            basicEffect.LightingEnabled = true;
            if (basicEffect.LightingEnabled)
            {
                // enable each light individually
                basicEffect.DirectionalLight0.Enabled = true;
                if (basicEffect.DirectionalLight0.Enabled)
                {
                    // x direction is red
                    basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1, 0, 0); // range is 0 to 1
                    basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, 0, 0));
                    // points from the light to the origin of the scene
                    basicEffect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                basicEffect.DirectionalLight1.Enabled = true;
                if (basicEffect.DirectionalLight1.Enabled)
                {
                    // y direction is green
                    basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0, 1, 0);
                    basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0, -1, 0));
                    basicEffect.DirectionalLight1.SpecularColor = Vector3.One;
                }

                basicEffect.DirectionalLight2.Enabled = true;
                if (basicEffect.DirectionalLight2.Enabled)
                {
                    // z direction is blue
                    basicEffect.DirectionalLight2.DiffuseColor = new Vector3(0, 0, 1);
                    basicEffect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0, 0, -1));
                    basicEffect.DirectionalLight2.SpecularColor = Vector3.One;
                }
            }

            return basicEffect;
        }

        private Effect createBasicEffect2()
        {
            StockEffects.BasicEffect basicEffect = new StockEffects.BasicEffect(GraphicsDevice);

            basicEffect.World = Matrix.Identity;

            // primitive color
            basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            basicEffect.SpecularPower = 5.0f;
            basicEffect.Alpha = 1.0f;

            basicEffect.LightingEnabled = true;
            if (basicEffect.LightingEnabled)
            {
                // enable each light individually
                basicEffect.DirectionalLight0.Enabled = true;
                if (basicEffect.DirectionalLight0.Enabled)
                {
                    // x direction is red
                    basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f); // range is 0 to 1
                    basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
                    // points from the light to the origin of the scene
                    //basicEffect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                basicEffect.DirectionalLight1.Enabled = false;
                if (basicEffect.DirectionalLight1.Enabled)
                {
                    // y direction is green
                    basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0, 1, 0);
                    basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0, -1, 0));
                    basicEffect.DirectionalLight1.SpecularColor = Vector3.One;
                }

                basicEffect.DirectionalLight2.Enabled = false;
                if (basicEffect.DirectionalLight2.Enabled)
                {
                    // z direction is blue
                    basicEffect.DirectionalLight2.DiffuseColor = new Vector3(0, 0, 1);
                    basicEffect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0, 0, -1));
                    basicEffect.DirectionalLight2.SpecularColor = Vector3.One;
                }
            }

            return basicEffect;
        }

        private Effect createBasicEffect3()
        {
            BasicEffect basicEffect = new BasicEffect(GraphicsDevice);

            basicEffect.World = Matrix.Identity;

            // primitive color
            basicEffect.AmbientLightColor = new Vector3(1f, 1f, 1f);
            basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            //basicEffect.SpecularPower = 5.0f;
            //basicEffect.Alpha = 1.0f;

            basicEffect.LightingEnabled = false;
            if (basicEffect.LightingEnabled)
            {
                // enable each light individually
                basicEffect.DirectionalLight0.Enabled = true;
                if (basicEffect.DirectionalLight0.Enabled)
                {
                    // x direction is red
                    basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f); // range is 0 to 1
                    basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
                    // points from the light to the origin of the scene
                    basicEffect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                basicEffect.DirectionalLight1.Enabled = false;
                if (basicEffect.DirectionalLight1.Enabled)
                {
                    // y direction is green
                    basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0, 1, 0);
                    basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0, -1, 0));
                    basicEffect.DirectionalLight1.SpecularColor = Vector3.One;
                }

                basicEffect.DirectionalLight2.Enabled = false;
                if (basicEffect.DirectionalLight2.Enabled)
                {
                    // z direction is blue
                    basicEffect.DirectionalLight2.DiffuseColor = new Vector3(0, 0, 1);
                    basicEffect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0, 0, -1));
                    basicEffect.DirectionalLight2.SpecularColor = Vector3.One;
                }
            }

            return basicEffect;
        }
    }

}
