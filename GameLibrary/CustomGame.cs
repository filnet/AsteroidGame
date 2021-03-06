﻿
using GameLibrary.Component;
using GameLibrary.Component.Camera;
using GameLibrary.Component.Debug;
using GameLibrary.Control;
using GameLibrary.SceneGraph;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GameLibrary
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class CustomGame : Microsoft.Xna.Framework.Game
    {
        //private const string FontAssetName = @"Arial-Bold-12pt-OuterGlow-3px";
        //private const string FontAssetName = @"DemoFont12pt";
        //private const string FontAssetName = @"Fonts/Arial-12pt";
        //private const string FontAssetName = @"Fonts/Font";
        private const string FontAssetName = @"DemoFont12pt";

        // Set distance from the camera of the near and far clipping planes.
        //private static float nearClip = 1.0f;
        //private static float farClip = 2000.0f;

        // Set field of view of the camera in radians (pi/4 is 45 degrees).
        //static float viewAngle = MathHelper.PiOver4;

        private GraphicsDeviceManager graphics;

        private Controller controlComponent;
        private CameraComponent cameraComponent;
        private FPSComponent fpsComponent;

        private int mode = 4;
        private Scene scene;

        public GraphicsDeviceManager Graphics
        {
            get { return graphics; }
        }

        public Scene Scene
        {
            get { return scene; }
            set { scene = value; }
        }

        public CameraComponent CameraComponent
        {
            get { return cameraComponent; }
            set { cameraComponent = value; }
        }

        public CustomGame() : base()
        {
            Content.RootDirectory = "Content";

            graphics = createGraphicsDeviceManager();

            IsFixedTimeStep = true;
            IsMouseVisible = true;

            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += new EventHandler<EventArgs>(WindowClientSizeChanged);

            Exiting += new EventHandler<EventArgs>(GameExiting);

            // Initialize the keyboard-event handler.
            GameLibrary.Component.UI.KeyboardInput.Initialize(this, 500f, 20);
        }

        protected virtual void createScene(int mode)
        {
        }


        private GraphicsDeviceManager createCustomGraphicsDeviceManager()
        {
            CustomGraphicsDeviceManager graphics = new CustomGraphicsDeviceManager(this);

#if WINDOWS
            graphics.PreferredBackBufferWidth = 1680;
            graphics.PreferredBackBufferHeight = 1050;
#endif
#if WINDOWS_PHONE
            graphics.PreferredBackBufferWidth = 400;
            graphics.PreferredBackBufferHeight = 600;
#endif

            graphics.IsFullScreen = true;
            graphics.IsWideScreenOnly = true;
            graphics.SynchronizeWithVerticalRetrace = true;
            graphics.PreferMultiSampling = false;
            graphics.ApplyChanges();

            return graphics;
        }

        private GraphicsDeviceManager createGraphicsDeviceManager()
        {
            GraphicsDeviceManager graphics = new GraphicsDeviceManager(this);

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;

            //graphics.SynchronizeWithVerticalRetrace = true;
            graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // Setup the window to be a quarter the size of the desktop.
            //int windowWidth = GraphicsDevice.DisplayMode.Width / 2;
            //int windowHeight = GraphicsDevice.DisplayMode.Height / 2;
            //graphics.PreferredBackBufferWidth = windowWidth;
            //graphics.PreferredBackBufferHeight = windowHeight;

            graphics.PreferMultiSampling = true;
            graphics.SynchronizeWithVerticalRetrace = IsFixedTimeStep;

            graphics.ApplyChanges();
            return graphics;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            controlComponent = new CustomControlComponent(this);
            //Components.Add(controlComponent);

            cameraComponent = new DefaultCameraComponent(this);
            cameraComponent.Initialize();
            //Components.Add(cameraComponent);

            fpsComponent = new FPSComponent(this);
            //Components.Add(fpsComponent);

            //float aspectRatio = (float)graphics.PreferredBackBufferWidth / (float)graphics.PreferredBackBufferHeight;
            //CameraComponent.SetAspect(aspectRatio);

            createScene(mode);
            Scene.CameraComponent = CameraComponent;
            Scene.Initialize();

            InitializeTransform();

            // Setup the camera.
            InitCamera();

            // Initialize the DebugSystem
            DebugSystem.Initialize(this, debugFont: FontAssetName);
            DebugSystem.Instance.FpsCounter.Visible = true;
            DebugSystem.Instance.TimeRuler.Visible = true;
            DebugSystem.Instance.TimeRuler.ShowLog = false;

            // Add "tr" command if DebugCommandHost is registered.
            IDebugCommandHost host = Services.GetService(typeof(IDebugCommandHost)) as IDebugCommandHost;
            if (host != null)
            {
                host.RegisterCommand("t", "Toggle an option", this.ToggleOptionCommandExecute);
                host.RegisterCommand("v", "Viewpoint", this.ViewpointCommandExecute);
                host.RegisterCommand("d", "Dump scene", this.DumpSceneCommandExecute);
                host.RegisterCommand("s", "SwitchScene", this.SwitchSceneCommandExecute);
                //this.Visible = true;
            }

            fpsComponent.Initialize();

            // initialize self. will initialize all added components
            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (scene != null)
            {
                scene.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload all content.
        /// </summary>
        protected override void UnloadContent()
        {
            base.UnloadContent();
            //Components.Remove(controlComponent);
            //Components.Remove(cameraComponent);
        }

        public void NextScene()
        {
            if (scene != null)
            {
                scene.Dispose();
            }
            mode = (mode + 1) % 5;
            createScene(mode);
            Scene.CameraComponent = CameraComponent;
            Scene.Initialize();

            InitializeTransform();
            InitCamera();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected sealed override void Update(GameTime gameTime)
        {
            // call StartFrame at the begining of Update to indicate that new frame has started.
            DebugSystem.Instance.TimeRuler.StartFrame();

            // begin measuring the Update method
            DebugSystem.Instance.TimeRuler.BeginMark("Update", Color.Yellow);

            // start update stopwatch
            fpsComponent.UpdateStopwatch.Start();

            base.Update(gameTime);
            controlComponent.Update(gameTime);
            cameraComponent.Update(gameTime);

            UpdateScene(gameTime);

            fpsComponent.UpdateStopwatch.Stop();
            fpsComponent.Update(gameTime);

            // end measuring the Update method
            DebugSystem.Instance.TimeRuler.EndMark("Update");

            // TODO show GC and stuff 
            // see https://github.com/willmotil/MonoGameUtilityClasses
        }

        protected virtual void UpdateScene(GameTime gameTime)
        {
            scene.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected sealed override void Draw(GameTime gameTime)
        {
            // Begin measuring our Draw method
            DebugSystem.Instance.TimeRuler.BeginMark("Draw", Color.Red);

            fpsComponent.DrawStopwatch.Start();

            GraphicsDevice.Clear(Color.SteelBlue);
            scene.Draw(gameTime);
            base.Draw(gameTime);

            fpsComponent.DrawStopwatch.Stop();
            fpsComponent.Draw(gameTime);

            // End measuring our Draw method
            DebugSystem.Instance.TimeRuler.EndMark("Draw");
        }

        protected void DrawScene(GameTime gameTime)
        {
        }

        void GameExiting(object sender, EventArgs e)
        {
            Console.WriteLine("Exiting!");
        }

        void WindowClientSizeChanged(object sender, EventArgs e)
        {
            Console.WriteLine("ClientSizeChanged!");
        }

        /// <summary>
        /// Initializes the transforms used for the 3D model.
        /// </summary>
        private void InitializeTransform()
        {
            //worldMatrix = Matrix.Identity; // CreateRotationX(tilt) * Matrix.CreateRotationY(tilt);
        }

        private void InitCamera()
        {
            /*
            int windowWidth = GraphicsDevice.DisplayMode.Width;
            int windowHeight = GraphicsDevice.DisplayMode.Height;
            float aspectRatio = (float)windowWidth / (float)windowHeight;
            cameraComponent.Perspective(viewAngle, aspectRatio, nearClip, farClip);
            */

            //GraphicsDevice device = graphics.GraphicsDevice;
            //float aspectRatio = (float)windowWidth / (float)windowHeight;

            //camera.Perspective(CAMERA_FOV, aspectRatio, CAMERA_ZNEAR, CAMERA_ZFAR);
            //camera.Position = new Vector3(0.0f, CAMERA_OFFSET, 0.0f);
            //camera.Acceleration = new Vector3(4.0f, 4.0f, 4.0f);
            //camera.Velocity = new Vector3(1.0f, 1.0f, 1.0f);
            //camera.OrbitMinZoom = 1.5f;
            //camera.OrbitMaxZoom = 5.0f;
            //camera.OrbitOffsetDistance = camera.OrbitMinZoom;

            //ChangeCameraBehavior(Camera.Behavior.Orbit);
        }

        /// <summary>
        /// 'tr' command execution.
        /// </summary>
        void ToggleOptionCommandExecute(IDebugCommandHost host, string command, IList<string> arguments)
        {
            if (arguments.Count == 0) return;

            char[] subArgSeparator = new[] { ':' };
            foreach (string orgArg in arguments)
            {
                string arg = orgArg.ToLower();
                string[] subargs = arg.Split(subArgSeparator);
                switch (subargs[0])
                {
                    case "d":
                        //Scene.Debug = !Scene.Debug;
                        break;
                    case "bv":
                        Scene.RenderContext.ShowBoundingVolumes = !Scene.RenderContext.ShowBoundingVolumes;
                        break;
                    case "cbv":
                        Scene.RenderContext.ShowCulledBoundingVolumes = !Scene.RenderContext.ShowCulledBoundingVolumes;
                        break;
                    case "c":
                        Scene.RenderContext.ShowCollisionVolumes = !Scene.RenderContext.ShowCollisionVolumes;
                        break;
                    case "f":
                        Scene.RenderContext.ShowFrustum = !Scene.RenderContext.ShowFrustum;
                        break;
                    case "s":
                        Scene.ShowStats = !Scene.ShowStats;
                        break;
                    case "cf":
                        Scene.CaptureFrustum = !Scene.CaptureFrustum;
                        break;
                    default:
                        break;
                }
            }
        }

        void ViewpointCommandExecute(IDebugCommandHost host, string command, IList<string> arguments)
        {
            char[] subArgSeparator = new[] { ':' };
            foreach (string orgArg in arguments)
            {
                string arg = orgArg.ToLower();
                string[] subargs = arg.Split(subArgSeparator);
                switch (subargs[0])
                {
                    case "o":
                        Scene.ViewpointOrigin();
                        break;
                    case "s":
                        Scene.ViewpointScene();
                        break;
                    case "l":
                        Scene.ViewpointLight();
                        break;
                    case "/?":
                    case "--help":
                        host.Echo("bv [on|off]");
                        host.Echo("Options:");
                        host.Echo("       on     Show bounding Frustum.");
                        host.Echo("       off    Hide bounding Frustum.");
                        break;
                    default:
                        break;
                }
            }
        }

        void DumpSceneCommandExecute(IDebugCommandHost host, string command, IList<string> arguments)
        {
            Scene.Dump();
        }

        void SwitchSceneCommandExecute(IDebugCommandHost host, string command, IList<string> arguments)
        {
            NextScene();
        }

    }

    class CustomControlComponent : InputController
    {
        private int windowWidth;
        private int windowHeight;

        private CustomGame game;

        public CustomControlComponent(CustomGame game) : base()
        {
            this.game = game;
        }

        public void Initialize()
        {
            //base.Initialize();
            // Setup the window to be a quarter the size of the desktop.
            windowWidth = game.GraphicsDevice.DisplayMode.Width / 2;
            windowHeight = game.GraphicsDevice.DisplayMode.Height / 2;

            //// Setup the initial input states.
            //currentKeyboardState = Keyboard.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Allows the game to exit
            if (checkExitKey())
            {
                game.Exit();
                return;
            }


            if ((IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt)) && KeyJustPressed(Keys.Enter))
            {
                ToggleFullScreen();
            }


            if ((IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt)) && KeyJustPressed(Keys.V))
            {
                game.IsFixedTimeStep = !game.IsFixedTimeStep;
                game.Graphics.SynchronizeWithVerticalRetrace = game.IsFixedTimeStep;
                game.Graphics.ApplyChanges();
            }

            //if (KeyJustPressed(Keys.H))
            //    displayHelp = !displayHelp;

            //if (KeyJustPressed(Keys.P))
            //    disableParallax = !disableParallax;

            //if (KeyJustPressed(Keys.T))
            //    disableColorMap = !disableColorMap;
        }

        private void ToggleFullScreen()
        {
            int newWidth = 0;
            int newHeight = 0;

            GraphicsDeviceManager graphics = game.Graphics;
            GraphicsDevice graphicsDevice = game.GraphicsDevice;

            graphics.IsFullScreen = !graphics.IsFullScreen;

            if (game.Graphics.IsFullScreen)
            {
                windowWidth = graphics.PreferredBackBufferWidth;
                windowHeight = graphics.PreferredBackBufferHeight;
                newWidth = graphicsDevice.DisplayMode.Width;
                newHeight = graphicsDevice.DisplayMode.Height;
            }
            else
            {
                newWidth = windowWidth;
                newHeight = windowHeight;
                if (newWidth <= 0 || newHeight <= 0)
                {
                    newWidth = graphicsDevice.DisplayMode.Width / 2;
                    newHeight = graphicsDevice.DisplayMode.Height / 2;
                }
            }

            graphics.PreferredBackBufferWidth = newWidth;
            graphics.PreferredBackBufferHeight = newHeight;
            graphics.PreferMultiSampling = true;
            graphics.ApplyChanges();

            float aspectRatio = (float)newWidth / (float)newHeight;
            game.CameraComponent.AspectRatio = aspectRatio;
        }

    }

}
