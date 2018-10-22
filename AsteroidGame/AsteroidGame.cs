using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using GameLibrary;
using AsteroidGame.Control;
using GameLibrary.SceneGraph;
using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;
using GameLibrary.Geometry;
using GameLibrary.Geometry.Common;
using AsteroidGame.Geometry;
using GameLibrary.Util;
using GameLibrary.Voxel;

namespace AsteroidGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class AsteroidGame : CustomGame
    {
        private static AsteroidGame instance;

        private GroupNode bulletGroupNode;
        private GroupNode asteroidGroupNode;

        public static AsteroidGame Instance()
        {
            return instance;
        }

        public AsteroidGame()
            : base()
        {
            IsFixedTimeStep = false;
            instance = this;
        }

        protected override void Initialize()
        {
            base.Initialize();
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

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override Scene createScene(int mode)
        {
            switch (mode)
            {
                case 0:
                    Scene scene = createAsteroidScene();
                    return scene;
                case 1:
                    return createGeodesicTestScene();
                case 2:
                    scene = createCollisionTestScene();
                    return scene;
                case 3:
                    scene = createCubeTestScene();
                    return scene;
                case 4:
                    scene = createVoxelTestScene();
                    return scene;
                default:
                    return null;
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

        private static readonly int ASTEROID_COLLISION_GROUP = 0;
        private static readonly int PLAYER_COLLISION_GROUP = 1;
        private static readonly int BULLET_COLLISION_GROUP = 2;

        private Scene createAsteroidScene()
        {
            float height = 0.25f;

            Scene scene = new Scene();
            scene.GraphicsDevice = GraphicsDevice;

            scene.CameraComponent = CameraComponent;

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
            GroupNode rootNode = scene.RootNode;
            rootNode.Add(worldNode);

            rootNode.AddFirst(createGridNode());

            return scene;
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
            playerNode.RenderGroupId = Scene.CLIPPING;
            playerNode.CollisionGroupId = PLAYER_COLLISION_GROUP;
            playerNode.Scale = new Vector3(0.25f);

            MeshNode centerNode = GeometryUtil.CreateCircle("CENTER_" + name, 32);
            centerNode.RenderGroupId = Scene.CLIPPING;
            centerNode.Scale = new Vector3(0.10f);

            GroupNode X = new GroupNode("X");
            X.Add(centerNode);
            playerNode.Add(X);//centerNode);

            return playerNode;
        }

        long bulletId = 0;
        long asteroidId = 0;

        public void AddBullet(GameTime gameTime, Vector3 position, Quaternion orientation, Vector3 velocity)
        {
            bulletId++;
            GeometryNode node = GeometryUtil.CreateLine("BULLET_" + bulletId);
            node.RenderGroupId = Scene.BULLET;
            node.CollisionGroupId = BULLET_COLLISION_GROUP;
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
            node.RenderGroupId = Scene.CLIPPING;
            //node.RenderGroupId = Scene.CLIPPING;
            node.CollisionGroupId = ASTEROID_COLLISION_GROUP;
            node.Scale = new Vector3(0.40f);
            node.Rotation = orientation;
            node.Translation = position;

            Controller controller = new AsteroidController(node, velocity, angularVelocity);
            node.AddController(controller);

            Node wrapNode = createWrapNode(node);

            return wrapNode;
        }

        private Scene createCubeTestScene()
        {
            Scene scene = new Scene();
            scene.GraphicsDevice = GraphicsDevice;

            scene.CameraComponent = CameraComponent;

            GeometryNode cube = GeometryUtil.CreateCube("CUBE");
            cube.RenderGroupId = Scene.THREE_LIGHTS;
            cube.Scale = new Vector3(2.0f);

            // root
            GroupNode rootNode = scene.RootNode;
            rootNode.Add(cube);

            return scene;
        }

        private Scene createVoxelTestScene()
        {
            Scene scene = new Scene();
            scene.GraphicsDevice = GraphicsDevice;

            scene.CameraComponent = CameraComponent;

            //Node voxelMapNode = createVoxelMapNode("VOXEL", 16);
            Node octreeNode = createOctreeNode("OCTREE", 1024);

            // root
            TransformNode node = new TransformNode("SCENE");
            node.Scale = new Vector3(0.10f);
            //node.Add(voxelMapNode);
            node.Add(octreeNode);

            GroupNode rootNode = scene.RootNode;
            rootNode.Add(node);

            return scene;
        }

        private Node createOctreeNode(String name, int size)
        {
            int SIZE = 1024;
            int VOXEL_SIZE = 16;
            int DEPTH = BitUtil.Log2(SIZE / VOXEL_SIZE);
            OctreeGeometry node = new OctreeGeometry(name, SIZE);
            node.RenderGroupId = Scene.OCTREE;

            GroupNode groupNode = new GroupNode("GROUP_VOXEL");
            groupNode.Enabled = true;
            groupNode.Visible = false;
            node.Add(groupNode);

            //addOctreeChildren(node.Octree, node.Octree.RootNode);
            //node.Octree.AddChild(Vector3.Zero, 6);

            //node.Octree.AddChild(new Vector3(0,0,-1), 6);
            //node.Octree.AddChild(new Vector3(0, -1, -1), 6);
            //node.Octree.AddChild(new Vector3(-1, -1, -1), 6);

            OctreeNode<GeometryNode> onode;

            onode = node.Octree.AddChild(new Vector3(0, 0, 0), DEPTH);
            onode.obj = createOctreeVoxelMapNode(node.Octree, onode, "VOXEL", VOXEL_SIZE);
            // TODO don't had voxel nodes to scene...
            groupNode.Add(onode.obj);

            onode = node.Octree.AddChild(new Vector3(-16, 0, 0), DEPTH);
            onode.obj = createOctreeVoxelMapNode(node.Octree, onode, "VOXEL", VOXEL_SIZE);
            // TODO don't had voxel nodes to scene...
            groupNode.Add(onode.obj);

            onode = node.Octree.AddChild(new Vector3(16, 0, 0), DEPTH);
            onode.obj = createOctreeVoxelMapNode(node.Octree, onode, "VOXEL", 16);
            // TODO don't had voxel nodes to scene...
            groupNode.Add(onode.obj);

            onode = node.Octree.AddChild(new Vector3(32, 0, 0), DEPTH);
            onode.obj = createOctreeVoxelMapNode(node.Octree, onode, "VOXEL", VOXEL_SIZE);
            // TODO don't had voxel nodes to scene...
            groupNode.Add(onode.obj);

            return node;
        }

        private GeometryNode createOctreeVoxelMapNode(Octree<GeometryNode> octree, OctreeNode<GeometryNode> onode, String name, int size)
        {
            //voxelMap = new SimpleVoxelMap(size);
            VoxelMap voxelMap = new FunctionVoxelMap(size);

            //GeometryNode node = new VoxelMapGeometry(name, size);
            //node.RenderGroupId = Scene.VOXEL_MAP;

            GeometryNode node = new MeshNode("VOXEL", new VoxelMapMeshFactory(voxelMap));
            node.Visible = true;
            node.RenderGroupId = Scene.VOXEL;
            //node.Rotation = Quaternion.CreateFromYawPitchRoll(0.3f, 0, 0);

            Vector3 halfSize;
            Vector3 center;
            octree.GetNodeBoundingBox(onode, out center, out halfSize);

            node.Translation = center;

            return node;
        }

        private Node createVoxelMapNode(String name, int size)
        {
            //voxelMap = new SimpleVoxelMap(size);
            VoxelMap voxelMap = new FunctionVoxelMap(size);

            //GeometryNode node = new VoxelMapGeometry(name, size);
            //node.RenderGroupId = Scene.VOXEL_MAP;

            GeometryNode node = new MeshNode("VOXEL", new VoxelMapMeshFactory(voxelMap));
            node.RenderGroupId = Scene.VOXEL;
            //node.Rotation = Quaternion.CreateFromYawPitchRoll(0.3f, 0, 0);

            return node;
        }

        private void addOctreeChildren(Octree<GeometryNode> octree, OctreeNode<GeometryNode> parentNode)
        {
            if (octree.GetNodeTreeDepth(parentNode) == 6)
            {
                return;
            }
            for (int i = 0; i < 8; i++)
            {
                OctreeNode<GeometryNode> childNode = octree.AddChild(parentNode, (Octant)i);
                if (i == 0)
                {
                    addOctreeChildren(octree, childNode);
                }
            }
        }

        private Scene createCollisionTestScene()
        {
            Scene scene = new Scene();
            scene.GraphicsDevice = GraphicsDevice;

            scene.CameraComponent = CameraComponent;

            GeometryNode node1 = new MeshNode("RECT_1", new SquareMeshFactory());
            node1.RenderGroupId = Scene.CLIPPING;
            node1.CollisionGroupId = ASTEROID_COLLISION_GROUP;
            //node1.Scale = new Vector3(0.40f);
            //node1.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4);
            //node1.Translation = position;

            GeometryNode node2 = new MeshNode("RECT_2", new SquareMeshFactory());
            node2.RenderGroupId = Scene.CLIPPING;
            node2.CollisionGroupId = ASTEROID_COLLISION_GROUP;
            //node2.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4);
            node2.Translation = new Vector3(0.75f, 0.75f, 0);

            GeometryNode node3 = new MeshNode("POLY_1", new PolygonMeshFactory(new Polygon(
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(2f, 2f)
            //new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0)
            )));
            node3.RenderGroupId = Scene.CLIPPING;
            node3.CollisionGroupId = ASTEROID_COLLISION_GROUP;
            //node1.Scale = new Vector3(0.40f);
            //node1.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4);
            //node3.Translation = new Vector3(-2.5f, 0, 0);

            GeometryNode node4 = new MeshNode("POLY_2", new PolygonMeshFactory(new Polygon(
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(2f, 2f)
            //new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0)
            )));
            node4.RenderGroupId = Scene.CLIPPING;
            node4.CollisionGroupId = ASTEROID_COLLISION_GROUP;
            //node1.Scale = new Vector3(0.40f);
            //node1.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4);
            node4.Translation = new Vector3(0f, 0.5f, 0);

            // root
            GroupNode rootNode = scene.RootNode;
            //rootNode.Add(node1);
            //rootNode.Add(node2);
            rootNode.Add(node3);
            rootNode.Add(node4);

            return scene;
        }

        private Scene createGeodesicTestScene()
        {
            Scene scene = new Scene();
            scene.GraphicsDevice = GraphicsDevice;

            scene.CameraComponent = CameraComponent;

            // root
            GroupNode rootNode = scene.RootNode;

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

            return scene;
        }


    }
}
