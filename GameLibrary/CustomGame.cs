using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph;
using GameLibrary.Control;
using System.Diagnostics;
using GameLibrary.System;

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
        private static float nearClip = 1.0f;
        private static float farClip = 2000.0f;

        // Set field of view of the camera in radians (pi/4 is 45 degrees).
        static float viewAngle = MathHelper.PiOver4;

        private GraphicsDeviceManager graphics;

        private Controller controlComponent;
        private ICameraComponent cameraComponent;
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

        public ICameraComponent CameraComponent
        {
            get { return cameraComponent; }
        }

        public CustomGame()
            : base()
        {
            graphics = createGraphicsDeviceManager();

            Content.RootDirectory = "Content";

            //graphics.GraphicsProfile = GraphicsProfile.HiDef;

            Exiting += new EventHandler<EventArgs>(Game_Exiting);

            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);

            IsFixedTimeStep = false;
            IsMouseVisible = false;
        }

        protected virtual Scene createScene(int mode)
        {
            return null;
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
            graphics.SynchronizeWithVerticalRetrace = true;
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
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

            cameraComponent = new ArcBallCamera();
            //Components.Add(cameraComponent);

            fpsComponent = new FPSComponent(this);
            //Components.Add(fpsComponent);


            // Setup the window to be a quarter the size of the desktop.
            int windowWidth = GraphicsDevice.DisplayMode.Width / 2;
            int windowHeight = GraphicsDevice.DisplayMode.Height / 2;

            graphics.PreferredBackBufferWidth = windowWidth;
            graphics.PreferredBackBufferHeight = windowHeight;
            graphics.PreferMultiSampling = true;
            graphics.SynchronizeWithVerticalRetrace = IsFixedTimeStep;
            graphics.ApplyChanges();

            InitializeTransform();

            // Setup the camera.
            InitCamera();

            // Initialize the DebugSystem
            System.DebugSystem.Initialize(this, debugFont: FontAssetName);
            System.DebugSystem.Instance.FpsCounter.Visible = true;
            System.DebugSystem.Instance.TimeRuler.Visible = true;
            System.DebugSystem.Instance.TimeRuler.ShowLog = false;


            // Add "tr" command if DebugCommandHost is registered.
            IDebugCommandHost host = Services.GetService(typeof(IDebugCommandHost)) as IDebugCommandHost;
            if (host != null)
            {
                host.RegisterCommand("t", "Toggle an option", this.ToggleOptionCommandExecute);
                host.RegisterCommand("s", "Show options", this.ShowOptionsCommandExecute);
                host.RegisterCommand("d", "Dump scene", this.SceneCommandExecute);
                //this.Visible = true;
            }

            fpsComponent.Initialize();

            scene = createScene(mode);
            scene.Initialize();

            // initialize self. will initialize all added components
            base.Initialize();
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
            scene = createScene(mode);
            scene.Initialize();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected sealed override void Update(GameTime gameTime)
        {
            // call StartFrame at the begining of Update to indicate that new frame has started.
            System.DebugSystem.Instance.TimeRuler.StartFrame();

            // begin measuring the Update method
            System.DebugSystem.Instance.TimeRuler.BeginMark("Update", Color.Yellow);

            fpsComponent.UpdateStopwatch.Start();

            UpdateScene(gameTime);
            base.Update(gameTime);
            controlComponent.Update(gameTime);
            cameraComponent.Update(gameTime);
            fpsComponent.UpdateStopwatch.Stop();
            fpsComponent.Update(gameTime);
            //fpsCounter.Update(gameTime);

            // end measuring the Update method
            System.DebugSystem.Instance.TimeRuler.EndMark("Update");
        }

        protected void UpdateScene(GameTime gameTime)
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
            System.DebugSystem.Instance.TimeRuler.BeginMark("Draw", Color.Red);

            fpsComponent.DrawStopwatch.Start();

            GraphicsDevice.Clear(Color.SteelBlue);
            DrawScene(gameTime);
            fpsComponent.DrawStopwatch.Stop();
            //fpsComponent.Draw(gameTime);
            base.Draw(gameTime);
            fpsComponent.Draw(gameTime);

            // End measuring our Draw method
            System.DebugSystem.Instance.TimeRuler.EndMark("Draw");
        }

        protected void DrawScene(GameTime gameTime)
        {
            scene.Draw(gameTime);
        }

        void Game_Exiting(object sender, EventArgs e)
        {
            Console.WriteLine("Exiting!");
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
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
            int windowWidth = GraphicsDevice.DisplayMode.Width;
            int windowHeight = GraphicsDevice.DisplayMode.Height;
            float aspectRatio = (float)windowWidth / (float)windowHeight;
            cameraComponent.Perspective(viewAngle, aspectRatio, nearClip, farClip);

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
                        Scene.Debug = !Scene.Debug;
                        break;
                    case "bv":
                        Scene.ShowBoundingVolumes = !Scene.ShowBoundingVolumes;
                        break;
                    case "cbv":
                        Scene.ShowCulledBoundingVolumes = !Scene.ShowCulledBoundingVolumes;
                        break;
                    case "c":
                        Scene.ShowCollisionVolumes = !Scene.ShowCollisionVolumes;
                        break;
                    case "f":
                        Scene.ShowFrustrum = !Scene.ShowFrustrum;
                        break;
                    case "cf":
                        Scene.CaptureFrustrum = !Scene.CaptureFrustrum;
                        break;
                    default:
                        break;
                }
            }
        }

        void ShowOptionsCommandExecute(IDebugCommandHost host, string command, IList<string> arguments)
        {
            char[] subArgSeparator = new[] { ':' };
            foreach (string orgArg in arguments)
            {
                string arg = orgArg.ToLower();
                string[] subargs = arg.Split(subArgSeparator);
                switch (subargs[0])
                {
                    case "on":
                        Scene.ShowFrustrum = true;
                        break;
                    case "off":
                        Scene.ShowFrustrum = false;
                        break;
                    case "reset":
                        //ResetLog();
                        break;
                    case "log":
                        //if (subargs.Length > 1)
                        //{
                        //    if (String.Compare(subargs[1], "on") == 0)
                        //        ShowLog = true;
                        //    if (String.Compare(subargs[1], "off") == 0)
                        //        ShowLog = false;
                        //}
                        //else
                        //{
                        //    ShowLog = !ShowLog;
                        //}
                        break;
                    case "frame":
                        //int a = Int32.Parse(subargs[1]);
                        //a = Math.Max(a, 1);
                        //a = Math.Min(a, MaxSampleFrames);
                        //TargetSampleFrames = a;
                        break;
                    case "/?":
                    case "--help":
                        host.Echo("bv [on|off]");
                        host.Echo("Options:");
                        host.Echo("       on     Show bounding frustrum.");
                        host.Echo("       off    Hide bounding frustrum.");
                        break;
                    default:
                        break;
                }
            }
            Scene.CaptureFrustrum = true;
        }


        void SceneCommandExecute(IDebugCommandHost host, string command, IList<string> arguments)
        {
            if (arguments.Count == 0)
            {
                Scene.Dump();
                NextScene();
                Scene.Dump();
                return;
            }

            char[] subArgSeparator = new[] { ':' };
            foreach (string orgArg in arguments)
            {
                string arg = orgArg.ToLower();
                string[] subargs = arg.Split(subArgSeparator);
                switch (subargs[0])
                {
                    case "d":
                        Scene.Dump();
                        break;
                }
            }
            Scene.Dump();
        }
    }
    class CustomControlComponent : InputController
    {

        private int windowWidth;
        private int windowHeight;


        private CustomGame game;
        //public CustomGame CustomGame
        //{
        //    get
        //    {
        //        return (CustomGame) Game;
        //    }
        //}

        public CustomControlComponent(CustomGame game)
            : base()
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


            //if (KeyJustPressed(Keys.V))
            //{
            //    Game.IsFixedTimeStep = !Game.IsFixedTimeStep;
            //}

            //if (KeyJustPressed(Keys.Back))
            //{
            //    switch (camera.CurrentBehavior)
            //    {
            //    case Camera.Behavior.Flight:
            //        camera.UndoRoll();
            //        break;

            //    case Camera.Behavior.Orbit:
            //        if (!camera.PreferTargetYAxisOrbiting)
            //            camera.UndoRoll();
            //        break;

            //    default:
            //        break;
            //    }
            //}

            //if (KeyJustPressed(Keys.Space))
            //{
            //    if (camera.CurrentBehavior == Camera.Behavior.Orbit)
            //        camera.PreferTargetYAxisOrbiting = !camera.PreferTargetYAxisOrbiting;
            //}

            //if (KeyJustPressed(Keys.D1))
            //    ChangeCameraBehavior(Camera.Behavior.FirstPerson);

            //if (KeyJustPressed(Keys.D2))
            //    ChangeCameraBehavior(Camera.Behavior.Spectator);

            //if (KeyJustPressed(Keys.D3))
            //    ChangeCameraBehavior(Camera.Behavior.Flight);

            //if (KeyJustPressed(Keys.D4))
            //    ChangeCameraBehavior(Camera.Behavior.Orbit);

            //if (KeyJustPressed(Keys.H))
            //    displayHelp = !displayHelp;

            //if (KeyJustPressed(Keys.P))
            //    disableParallax = !disableParallax;

            //if (KeyJustPressed(Keys.T))
            //    disableColorMap = !disableColorMap;

            //if (KeyJustPressed(Keys.C))
            //    camera.ClickAndDragMouseRotation = !camera.ClickAndDragMouseRotation;

            //if (KeyJustPressed(Keys.Add))
            //{
            //    camera.RotationSpeed += 0.01f;

            //    if (camera.RotationSpeed > 1.0f)
            //        camera.RotationSpeed = 1.0f;
            //}

            //if (KeyJustPressed(Keys.Subtract))
            //{
            //    camera.RotationSpeed -= 0.01f;

            //    if (camera.RotationSpeed <= 0.0f)
            //        camera.RotationSpeed = 0.01f;
            //}
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

            //camera.Perspective(CAMERA_FOV, aspectRatio, CAMERA_ZNEAR, CAMERA_ZFAR);
        }

    }

}
