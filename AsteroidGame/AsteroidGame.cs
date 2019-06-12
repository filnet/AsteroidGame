using AsteroidGame.Control;
using AsteroidGame.Geometry;
using BepuPhysics;
using GameLibrary;
using GameLibrary.Component.Camera;
using GameLibrary.Control;
using GameLibrary.Geometry;
using GameLibrary.Physics.Bepu;
using GameLibrary.SceneGraph;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Util;
using GameLibrary.Voxel.Grid;
using GameLibrary.Voxel.Octree;
using Microsoft.Xna.Framework;
using System;
using System.Threading;
using WpfLibrary;

namespace AsteroidGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class AsteroidGame : CustomGame
    {
        private static AsteroidGame instance;

        private readonly bool DebugWindowEnabled = true;
        private WpfControlWindow wnd;

        public static AsteroidGame Instance()
        {
            return instance;
        }

        public AsteroidGame() : base()
        {
            //IsFixedTimeStep = false;
            instance = this;
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (DebugWindowEnabled)
            {
                openWpf();
            }
        }

        protected override void Dispose(bool disposing)
        {
            closeWpf();
            base.Dispose(disposing);
        }

        private void openWpf()
        {
            Thread t = new Thread((ThreadStart)delegate
            {
                wnd = new WpfControlWindow();
                // Do stuff here, e.g. to the window
                //wnd.Title = "Something else";
                //wnd.setSelected(Scene.CameraComponent);
                wnd.SetSelected1(Scene.RenderContext);
                wnd.SetSelected2(Scene.RenderContext.LightRenderContext(0));
                wnd.SetSelected3(Scene.RenderContext.ReflectionRenderContext(0));
                wnd.SetRendererMap(Scene.GetRendererMap());
                // Show the window
                wnd.ShowDialog();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        private void closeWpf()
        {
            if (wnd != null)
            {
                wnd.Dispatcher.BeginInvoke(new ThreadStart(() => wnd.Close()));
            }
        }

        //int c = 0;
        private void refreshWpf()
        {
            if (wnd != null)
            {
                /*c++;
                //Console.WriteLine((c % 1000));
                if ((c % 33) == 0)
                {
                    Console.WriteLine("XXXXXXXXXXXXXXXXXXXXXXXXXXX");
                    wnd.Dispatcher.BeginInvoke(new ThreadStart(() => wnd.Refresh()));
                }*/
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        protected override void UpdateScene(GameTime gameTime)
        {
            base.UpdateScene(gameTime);
            refreshWpf();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void createScene(int mode)
        {
            Scene = new Scene();
            Scene.GraphicsDevice = GraphicsDevice;

            DefaultCameraComponent cam = CameraComponent as DefaultCameraComponent;
            if (cam != null)
            {
                cam.CurrentBehavior = DefaultCamera.Behavior.Orbit;
                cam.OrbitOffsetDistance = 9.0f;
                cam.LookAt(new Vector3(0, 1, 0), Vector3.Forward, Vector3.Up);
            }

            /*
            CameraComponent = new ArcBallCamera();
            Scene.CameraComponent = CameraComponent;
            // TODO call Reset() instead...
            ArcBallCamera camera = Scene.CameraComponent as ArcBallCamera;
            camera.Zoom = ArcBallCamera.DEFAULT_ZOOM;
            camera.ZoomMin = ArcBallCamera.DEFAULT_ZOOM_MIN;
            camera.ZoomMax = ArcBallCamera.DEFAULT_ZOOM_MAX;
            camera.ZoomSpeed = ArcBallCamera.DEFAULT_ZOOM_SPEED;
            */

            switch (mode)
            {
                case 0:
                    Scene.RootNode = createAsteroidScene();
                    break;
                case 1:
                    Scene.RootNode = createGeodesicTestScene();
                    break;
                case 2:
                    Scene.RootNode = createCubeTestScene();
                    break;
                case 3:
                    Scene.RootNode = createBoundingBoxHullSceneTest();
                    break;
                case 4:
                    //cam = CameraComponent as DefaultCamera;// new DefaultCamera(this);
                    if (cam != null)
                    {
                        cam.CurrentBehavior = DefaultCamera.Behavior.FirstPerson;
                        Vector3 eye = new Vector3(0, 2, 0);
                        cam.LookAt(eye, eye + Vector3.Forward, Vector3.Up);
                        cam.Velocity = new Vector3(2.5f);
                        //cam.ClickAndDragMouseRotation = true;
                    }
                    Scene.RootNode = createVoxelTestScene();
                    break;
                case 5:
                    Scene.RootNode = createCollisionTestScene();
                    break;
            }
        }

        private Node createGridNode()
        {
            // grid
            GeometryNode grid = GeometryUtil.CreateGrid("GRID", 3);
            grid.BoundingVolumeVisible = false;
            grid.RenderGroupId = Scene.VECTOR;
            GeometryNode circle = GeometryUtil.CreateCircle("GRID_CENTER", 16);
            circle.BoundingVolumeVisible = false;
            circle.RenderGroupId = Scene.VECTOR;
            grid.Add(circle);
            return grid;
        }

        // asteroid stuff (also used by voxel...)
        long bulletId = 0;
        long asteroidId = 0;

        private GroupNode bulletGroupNode;
        private GroupNode asteroidGroupNode;

        private Node createAsteroidScene()
        {
            float height = 0.25f;

            // player
            Node playerNode = createPlayerNode();

            // bullets
            bulletGroupNode = new GroupNode("GROUP_BULLET");

            // asteroids
            asteroidGroupNode = new GroupNode("GROUP_ASTEROID");

            // world
            TransformNode worldNode = new TransformNode("TRANSFORM_PLAYER");
            worldNode.Translation = new Vector3(0, 0, height);
            //worldNode.Scale = new Vector3(0.5f);

            worldNode.Add(playerNode);

            worldNode.Add(bulletGroupNode);
            worldNode.Add(asteroidGroupNode);

            // root
            GroupNode rootNode = new GroupNode("ROOT_NODE");
            rootNode.Add(worldNode);

            rootNode.AddFirst(createGridNode());

            return rootNode;
        }

        private Node createPlayerNode()
        {
            // player
            GeometryNode playerNode = createPlayerGeometry("PLAYER");
            PlayerController playerController = new PlayerController(playerNode);
            playerNode.AddController(playerController);

            // gun
            GeometryNode gunNode = createGunGeometry("GUN");
            float gunSize = 0.25f;
            gunNode.Scale = new Vector3(gunSize);
            gunNode.Translation = new Vector3(1 - gunSize, 0, 0);

            GunController gunController = new GunController(gunNode, playerNode, new Vector3(1.0f, 0, 0));
            gunNode.AddController(gunController);

            playerNode.Add(gunNode);

            Node wrapNode = createWrapNode(playerNode);

            return wrapNode;
        }

        private static GroupNode createWrapNode(Node node)
        {
            GroupNode groupNode = new GroupNode("WRAP_" + node.Name);
            groupNode.Add(node);
            Controller wrapController = new WrapController(groupNode);
            groupNode.AddController(wrapController);
            return groupNode;
        }

        private static GeometryNode createGunGeometry(String name)
        {
            MeshNode gunNode = GeometryUtil.CreateCircle(name, 3);
            gunNode.RenderGroupId = Scene.CLIPPING;

            return gunNode;
        }

        private static GeometryNode createPlayerGeometry(String name)
        {
            // player
            MeshNode playerNode = GeometryUtil.CreateCircle(name, 3);
            playerNode.RenderGroupId = Scene.PLAYER;
            playerNode.CollisionGroupId = Scene.PLAYER;
            playerNode.Scale = new Vector3(0.25f);

            MeshNode centerNode = GeometryUtil.CreateCircle("CENTER_" + name, 32);
            centerNode.RenderGroupId = Scene.CLIPPING;
            centerNode.Scale = new Vector3(0.10f);

            GroupNode X = new GroupNode("X");
            X.Add(centerNode);
            playerNode.Add(X);//centerNode);

            return playerNode;
        }

        public void AddBullet(GameTime gameTime, Vector3 position, Quaternion orientation, Vector3 velocity)
        {
            bulletId++;
            GeometryNode node = GeometryUtil.CreateLine("BULLET_" + bulletId);
            node.RenderGroupId = Scene.BULLET;
            node.CollisionGroupId = Scene.BULLET;
            node.Scale = new Vector3(0.10f);
            node.Rotation = orientation;
            node.Translation = position;

            RIPController ripController = new RIPController(node, 1.5f);
            node.AddController(ripController);

            Controller controller = new BulletController(node, velocity);
            node.AddController(controller);
            //bulletController.Update(gameTime);

            bulletGroupNode.Add(node);
        }

        public void AddAsteroid(GameTime gameTime, Vector3 position, Quaternion orientation, Vector3 velocity, Vector3 angularVelocity)
        {
            asteroidId++;
            Node node = createAsteroidNode("ASTEROID", asteroidId, ref position, ref orientation, velocity, angularVelocity);
            asteroidGroupNode.Add(node);
        }

        private Node createAsteroidNode(String name, long id, ref Vector3 position, ref Quaternion orientation, Vector3 velocity, Vector3 angularVelocity)
        {
            GeometryNode node = new MeshNode(name + "_" + asteroidId, new AsteroidMeshFactory(7));
            node.RenderGroupId = Scene.ASTEROID;
            node.CollisionGroupId = Scene.ASTEROID;
            node.Scale = new Vector3(0.40f);
            node.Rotation = orientation;
            node.Translation = position;

            Controller controller = new AsteroidController(node, velocity, angularVelocity);
            node.AddController(controller);

            Node wrapNode = createWrapNode(node);

            return wrapNode;
        }

        private Node createCubeTestScene()
        {
            GeometryNode cube = GeometryUtil.CreateCube("CUBE");
            cube.RenderGroupId = Scene.THREE_LIGHTS;
            cube.Scale = new Vector3(2.0f);

            return cube;
        }

        //public void FireBullet(GameTime gameTime, Vector3 position, Quaternion orientation, Vector3 velocity)
        public void FireBullet(GameTime gameTime)
        {
            Vector3 position = Scene.CameraComponent.Position + Scene.CameraComponent.ViewDirection;// Vector3.Up * 3;
            Vector3 velocity = Scene.CameraComponent.ViewDirection * 10;

            bulletId++;
            GeometryNode node = GeometryUtil.CreateSphere("BULLET_" + bulletId, 3);
            node.RenderGroupId = Scene.ONE_LIGHT;
            //node.RenderGroupId = Scene.BULLET;
            //node.CollisionGroupId = Scene.BULLET;
            node.Scale = new Vector3(BepuHelper.bulletSphere.Radius);
            node.Rotation = Quaternion.Identity;
            node.Translation = position;

            var bulletHandle = BepuHelper.AddBullet(Scene.Simulation, position, velocity);
            BodyReference bodyReference = new BodyReference(bulletHandle, Scene.Simulation.Bodies);

            // TODO should pass bulletHandle to controller ?
            // TODO creating/destroying controller with bullets creates GARBAGE...
            // need to use the same controller for all entities/node ?
            Controller controller = new Voxel.Control.BulletController(node, bodyReference);
            // TODO node should not have controller list...
            // should have controller (i.e. components) lists at scene level (or some manager)
            node.AddController(controller);
            //bulletController.Update(gameTime);

            bool rip = false;
            if (rip)
            {
                Voxel.Control.RIPController ripController = new Voxel.Control.RIPController(node, bulletHandle, 6.0f);
                node.AddController(ripController);
            }

            bulletGroupNode.Add(node);
        }

        public void RemoveBullet(int bulletHandle)
        {
            BepuHelper.RemoveBullet(Scene.Simulation, bulletHandle);
        }

        private Node createVoxelTestScene()
        {
            TransformNode sceneNode = new TransformNode("SCENE");

            Node groundNode = new VoxelGridGeometry("GRID", 32);

            VoxelOctreeGeometry voxelOctreeNode = new VoxelOctreeGeometry("OCTREE", 256, 32);
            voxelOctreeNode.voxelOctree.LoadFromDisk = false;
            TransformNode structureNode = new TransformNode("SCENE");
            structureNode.Add(voxelOctreeNode);
            structureNode.Translation = new Vector3(200, 64 + 0.001f, 200);

            GeometryNode sphereGeo = GeometryUtil.CreateSphere("SPHERE", 2);
            sphereGeo.RenderGroupId = Scene.ONE_LIGHT;
            sphereGeo.Translation = new Vector3(2, 6, 2);

            LightNode sunNode = new LightNode("LIGHT_SUN");
            sunNode.Translation = new Vector3(1, 1, 1);

            //sunNode.Add(structureNode);
            sunNode.Add(groundNode);
            sunNode.Add(sphereGeo);

            sceneNode.Add(sunNode);

            bulletGroupNode = new GroupNode("GROUP_BULLET");
            Voxel.Control.GunController gunController = new Voxel.Control.GunController(bulletGroupNode);
            bulletGroupNode.AddController(gunController);
            sceneNode.Add(bulletGroupNode);

            return sceneNode;
        }

        private Node createBoundingBoxHullSceneTest()
        {
            // root
            GroupNode rootNode = new GroupNode("ROOT_NODE");

            for (int z = -1; z <= 1; z++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float x0 = (x + 0) * 2.50f;
                        float y0 = (y + 0) * 1.50f;
                        float z0 = (z + 0) * 1.50f;
                        Vector3 v = new Vector3(x, y, z);
                        int p = ((v.X < 0) ? 1 : 0)      //  1 = left
                            + ((v.X > 0 ? 1 : 0) << 1)      //  2 = right
                            + ((v.Y < 0 ? 1 : 0) << 2)      //  4 = bottom
                            + ((v.Y > 0 ? 1 : 0) << 3)      //  8 = top
                            + ((v.Z < 0 ? 1 : 0) << 5)      // 32 = back !!!
                            + ((v.Z > 0 ? 1 : 0) << 4);     // 16 = front !!!

                        //Hull hull = VectorUtil.AABB_HULLS[p];

                        p *= 7;
                        int verticeCount = VolumeUtil.HULL_LOOKUP_TABLE[p];
                        if (verticeCount == 0)
                        {
                            continue;
                        }

                        Vector3[] vertices = new Vector3[verticeCount];
                        for (int i = 0; i < verticeCount; i++)
                        {
                            vertices[i] = VolumeUtil.BB_HULL_VERTICES[VolumeUtil.HULL_LOOKUP_TABLE[++p]];
                        }
                        //Console.WriteLine(verticeCount + " " + vertices);

                        GeometryNode geo = new MeshNode("X", new LineMeshFactory(vertices));
                        geo.RenderGroupId = Scene.VECTOR;
                        //node.CollisionGroupId = Scene.ASTEROID;

                        TransformNode node = new TransformNode("TRANSFORM");
                        node.Scale = new Vector3(0.45f);
                        node.Translation = new Vector3(x0, y0, z0);
                        node.Add(geo);

                        rootNode.Add(node);
                    }
                }
            }
            return rootNode;
        }

        private Node createGeodesicTestScene()
        {
            // root
            GroupNode rootNode = new GroupNode("ROOT_NODE");

            bool WF = false;

            int depth = 4;
            int types = 5;
            for (int i = 0; i <= depth; i++)
            {
                for (int j = 0; j < types; j++)
                {
                    float x = -((i - depth / 2));
                    float y = -((j - types / 2));
                    GeometryNode geo;
                    switch (j)
                    {
                        case 0:
                            geo = GeometryUtil.CreateSphere("SPHERE_" + i + "_" + j, i);
                            geo.RenderGroupId = Scene.ONE_LIGHT;
                            break;
                        case 1:
                            geo = GeometryUtil.CreateGeodesic("GEODESIC_" + i + "_" + j, i);
                            geo.RenderGroupId = Scene.ONE_LIGHT;
                            break;
                        case 2:
                            geo = GeometryUtil.CreateGeodesic("GEODESIC2_" + i + "_" + j, i);
                            geo.RenderGroupId = Scene.WIRE_FRAME;
                            break;
                        case 3:
                            geo = GeometryUtil.CreateGeodesicWF("GEODESIC2_WF_" + i + "_" + j, i);
                            geo.RenderGroupId = Scene.VECTOR;
                            break;
                        case 4:
                            geo = GeometryUtil.CreateGeodesic("GEODESIC2" + i + "_" + j, i);
                            geo.RenderGroupId = Scene.ONE_LIGHT;
                            if (WF)
                            {
                                GeometryNode geoWF = GeometryUtil.CreateGeodesic("GEODESIC2_" + i + "_" + j, i);
                                geoWF.Scale = new Vector3(1.05f);
                                geoWF.RenderGroupId = Scene.WIRE_FRAME;
                                geo.Add(geoWF);
                            }
                            else
                            {
                                GeometryNode geoWF = GeometryUtil.CreateGeodesicWF("GEODESIC2_WF_" + i + "_" + j, i);
                                geoWF.Scale = new Vector3(1.05f);
                                geoWF.RenderGroupId = Scene.VECTOR;
                                geo.Add(geoWF);
                            }
                            break;
                        default:
                            geo = GeometryUtil.CreateGeodesic("GEODESIC_DEFAULT_" + i + "_" + j, i);
                            geo.RenderGroupId = Scene.ONE_LIGHT;
                            break;
                    }
                    TransformNode node = new TransformNode("TRANSFORM" + i + "_" + j);
                    node.Scale = new Vector3(0.45f);
                    node.Translation = new Vector3(x, y, 0);
                    node.Add(geo);
                    rootNode.Add(node);
                }
            }
            return rootNode;
        }

        private Node createCollisionTestScene()
        {
            GeometryNode node1 = new MeshNode("RECT_1", new SquareMeshFactory());
            node1.RenderGroupId = Scene.ASTEROID;
            node1.CollisionGroupId = Scene.ASTEROID;
            //node1.Scale = new Vector3(0.40f);
            //node1.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4);
            //node1.Translation = position;

            GeometryNode node2 = new MeshNode("RECT_2", new SquareMeshFactory());
            node2.RenderGroupId = Scene.ASTEROID;
            node2.CollisionGroupId = Scene.ASTEROID;
            //node2.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4);
            node2.Translation = new Vector3(0.75f, 0.75f, 0);

            GeometryNode node3 = new MeshNode("POLY_1", new PolygonMeshFactory(new Polygon(
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(2f, 2f)
            //new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0)
            )));
            node3.RenderGroupId = Scene.ASTEROID;
            node3.CollisionGroupId = Scene.ASTEROID;
            //node1.Scale = new Vector3(0.40f);
            //node1.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4);
            //node3.Translation = new Vector3(-2.5f, 0, 0);

            GeometryNode node4 = new MeshNode("POLY_2", new PolygonMeshFactory(new Polygon(
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(2f, 2f)
            //new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0)
            )));
            node4.RenderGroupId = Scene.ASTEROID;
            node4.CollisionGroupId = Scene.ASTEROID;
            //node1.Scale = new Vector3(0.40f);
            //node1.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4);
            node4.Translation = new Vector3(0f, 0.5f, 0);

            // root
            GroupNode rootNode = new GroupNode("ROOT_NODE");
            //rootNode.Add(node1);
            //rootNode.Add(node2);
            rootNode.Add(node3);
            rootNode.Add(node4);

            return rootNode;
        }

    }
}
