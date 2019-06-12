using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GameLibrary.Component.Camera
{
    /// <summary>
    /// A general purpose quaternion based camera component for XNA. This
    /// camera component provides the necessary bindings to the XNA framework
    /// to allow the camera to be manipulated by the keyboard, mouse, and game
    /// pad. This camera component is implemented in terms of the Camera class.
    /// As a result the camera component supports all of the features of the
    /// Camera class. The camera component maps input to a series of actions.
    /// These actions are defined by the Actions enumeration. Methods are
    /// provided to remap the camera components default bindings.
    /// </summary>
    /// TODO derive from InputController
    public class DefaultCameraComponent : GameComponent, CameraComponent
    {
        public enum Actions
        {
            FlightYawLeftPrimary,
            FlightYawLeftAlternate,
            FlightYawRightPrimary,
            FlightYawRightAlternate,

            MoveForwardsPrimary,
            MoveForwardsAlternate,
            MoveBackwardsPrimary,
            MoveBackwardsAlternate,

            MoveDownPrimary,
            MoveDownAlternate,
            MoveUpPrimary,
            MoveUpAlternate,

            OrbitRollLeftPrimary,
            OrbitRollLeftAlternate,
            OrbitRollRightPrimary,
            OrbitRollRightAlternate,

            PitchUpPrimary,
            PitchUpAlternate,
            PitchDownPrimary,
            PitchDownAlternate,

            YawLeftPrimary,
            YawLeftAlternate,
            YawRightPrimary,
            YawRightAlternate,

            RollLeftPrimary,
            RollLeftAlternate,
            RollRightPrimary,
            RollRightAlternate,

            StrafeRightPrimary,
            StrafeRightAlternate,
            StrafeLeftPrimary,
            StrafeLeftAlternate
        };

        private const float DEFAULT_ACCELERATION_X = 8.0f;
        private const float DEFAULT_ACCELERATION_Y = 8.0f;
        private const float DEFAULT_ACCELERATION_Z = 8.0f;

        private const float DEFAULT_SPEED_FLIGHT_YAW = 100.0f;
        private const float DEFAULT_SPEED_MOUSE_WHEEL = 1.0f;
        private const float DEFAULT_SPEED_ORBIT_ROLL = 100.0f;
        // rotation speed in radians/s
        private const float DEFAULT_SPEED_ROTATION = (MathHelper.Pi * 2) / 2000;
        private const float DEFAULT_VELOCITY_X = 2.0f;
        private const float DEFAULT_VELOCITY_Y = 2.0f;
        private const float DEFAULT_VELOCITY_Z = 2.0f;

        public const int MOUSE_SMOOTHING_SAMPLE_COUNT = 10;
        public const float DEFAULT_MOUSE_SMOOTHING_PERIOD = (1.0f / 60.0f) * 10f;
        private const float DEFAULT_MOUSE_SMOOTHING_SENSITIVITY = 0.5f;

        private DefaultCamera camera;

        private bool clickAndDragMouseRotation;
        private bool mouseEnabled;

        private bool movingAlongPosX;
        private bool movingAlongNegX;
        private bool movingAlongPosY;
        private bool movingAlongNegY;
        private bool movingAlongPosZ;
        private bool movingAlongNegZ;

        private float rotationSpeed;
        private float orbitRollSpeed;
        private float flightYawSpeed;
        private float mouseWheelSpeed;
        private Vector3 acceleration;
        private Vector3 currentVelocity;
        private Vector3 velocity;
        private Vector3 savedVelocity;

        private GamePadState currentGamePadState;

        private KeyboardState currentKeyboardState;
        private Dictionary<Actions, Keys> actionKeys;

        private MouseCage cage = new MouseCage();
        private MouseFilter filter = new MouseFilter();

        #region Public Methods

        /// <summary>
        /// Constructs a new instance of the CameraComponent class. The
        /// camera will have a spectator behavior, and will be initially
        /// positioned at the world origin looking down the world negative
        /// z axis. An initial perspective projection matrix is created
        /// as well as setting up initial key bindings to the actions.
        /// </summary>
        public DefaultCameraComponent(Game game) : base(game)
        {
            camera = new DefaultCamera();
            camera.CurrentBehavior = DefaultCamera.Behavior.Spectator;

            movingAlongPosX = false;
            movingAlongNegX = false;
            movingAlongPosY = false;
            movingAlongNegY = false;
            movingAlongPosZ = false;
            movingAlongNegZ = false;

            rotationSpeed = DEFAULT_SPEED_ROTATION;
            orbitRollSpeed = DEFAULT_SPEED_ORBIT_ROLL;
            flightYawSpeed = DEFAULT_SPEED_FLIGHT_YAW;
            mouseWheelSpeed = DEFAULT_SPEED_MOUSE_WHEEL;
            filter.mouseSmoothingSensitivity = DEFAULT_MOUSE_SMOOTHING_SENSITIVITY;
            acceleration = new Vector3(DEFAULT_ACCELERATION_X, DEFAULT_ACCELERATION_Y, DEFAULT_ACCELERATION_Z);
            velocity = new Vector3(DEFAULT_VELOCITY_X, DEFAULT_VELOCITY_Y, DEFAULT_VELOCITY_Z);
            savedVelocity = velocity;

            Rectangle clientBounds = game.Window.ClientBounds;
            float aspect = (float)clientBounds.Width / (float)clientBounds.Height;

            Perspective(DefaultCamera.DEFAULT_FOVX, aspect, DefaultCamera.DEFAULT_ZNEAR, DefaultCamera.DEFAULT_ZFAR);

            mouseEnabled = true;
            Game.IsMouseVisible = !mouseEnabled;

            actionKeys = new Dictionary<Actions, Keys>();

            //Keys W_FORWARD = Keys.Z;
            //Keys A_STRAFE_LEFT = Keys.Q;
            //Keys S_BACKWARD = Keys.S;
            //Keys D_STRAFE_RIGHT = Keys.D;

            actionKeys.Add(Actions.FlightYawLeftPrimary, Keys.Left);
            actionKeys.Add(Actions.FlightYawLeftAlternate, Keys.A);
            actionKeys.Add(Actions.FlightYawRightPrimary, Keys.Right);
            actionKeys.Add(Actions.FlightYawRightAlternate, Keys.D);
            actionKeys.Add(Actions.MoveForwardsPrimary, Keys.Up);
            actionKeys.Add(Actions.MoveForwardsAlternate, Keys.Z);
            actionKeys.Add(Actions.MoveBackwardsPrimary, Keys.Down);
            actionKeys.Add(Actions.MoveBackwardsAlternate, Keys.S);
            actionKeys.Add(Actions.MoveDownPrimary, Keys.A);
            actionKeys.Add(Actions.MoveDownAlternate, Keys.PageDown);
            actionKeys.Add(Actions.MoveUpPrimary, Keys.E);
            actionKeys.Add(Actions.MoveUpAlternate, Keys.PageUp);
            actionKeys.Add(Actions.OrbitRollLeftPrimary, Keys.Left);
            actionKeys.Add(Actions.OrbitRollLeftAlternate, Keys.A);
            actionKeys.Add(Actions.OrbitRollRightPrimary, Keys.Right);
            actionKeys.Add(Actions.OrbitRollRightAlternate, Keys.D);
            actionKeys.Add(Actions.StrafeRightPrimary, Keys.Right);
            actionKeys.Add(Actions.StrafeRightAlternate, Keys.D);
            actionKeys.Add(Actions.StrafeLeftPrimary, Keys.Left);
            actionKeys.Add(Actions.StrafeLeftAlternate, Keys.Q);

            Game.Activated += HandleGameActivatedEvent;
            Game.Deactivated += HandleGameDeactivatedEvent;

            UpdateOrder = 1;

            //EnabledChanged += OnEnabledChanged;
        }

        /*
        private void OnEnabledChanged()
        {
            Console.WriteLine("XXXXXXXXXXXXXXX");
        }
        */

        /// <summary>
        /// Initializes the CameraComponent class. This method repositions the
        /// mouse to the center of the game window.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            cage.Reset(Game.Window.ClientBounds);

            GameLibrary.Component.UI.KeyboardInput.KeyPressed += KeyPressed;
        }

        protected override void Dispose(bool disposing)
        {
            GameLibrary.Component.UI.KeyboardInput.KeyPressed -= KeyPressed;

            base.Dispose(disposing);
        }

        private void KeyPressed(object sender, GameLibrary.Component.UI.KeyboardInput.KeyEventArgs e, KeyboardState ks)
        {
            //Console.WriteLine(e.KeyCode.ToString());
        }


        /// <summary>
        /// Builds a look at style viewing matrix.
        /// </summary>
        /// <param name="target">The target position to look at.</param>
        public void LookAt(Vector3 target)
        {
            camera.LookAt(target);
        }

        /// <summary>
        /// Builds a look at style viewing matrix.
        /// </summary>
        /// <param name="eye">The camera position.</param>
        /// <param name="target">The target position to look at.</param>
        /// <param name="up">The up direction.</param>
        public void LookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            camera.LookAt(eye, target, up);
        }

        /// <summary>
        /// Binds an action to a keyboard key.
        /// </summary>
        /// <param name="action">The action to bind.</param>
        /// <param name="key">The key to map the action to.</param>
        public void MapActionToKey(Actions action, Keys key)
        {
            actionKeys[action] = key;
        }

        /// <summary>
        /// Moves the camera by dx world units to the left or right; dy
        /// world units upwards or downwards; and dz world units forwards
        /// or backwards.
        /// </summary>
        /// <param name="dx">Distance to move left or right.</param>
        /// <param name="dy">Distance to move up or down.</param>
        /// <param name="dz">Distance to move forwards or backwards.</param>
        public void Move(float dx, float dy, float dz)
        {
            camera.Move(dx, dy, dz);
        }

        /// <summary>
        /// Moves the camera the specified distance in the specified direction.
        /// </summary>
        /// <param name="direction">Direction to move.</param>
        /// <param name="distance">How far to move.</param>
        public void Move(Vector3 direction, Vector3 distance)
        {
            camera.Move(direction, distance);
        }

        /// <summary>
        /// Builds a perspective projection matrix based on a horizontal field
        /// of view.
        /// </summary>
        /// <param name="fovx">Horizontal field of view in radians.</param>
        /// <param name="aspect">The viewport's aspect ratio.</param>
        /// <param name="znear">The distance to the near clip plane.</param>
        /// <param name="zfar">The distance to the far clip plane.</param>
        public void Perspective(float fovx, float aspect, float znear, float zfar)
        {
            camera.Perspective(fovx, aspect, znear, zfar);
            ResetMouse();
        }

        /// <summary>
        /// Rotates the camera. Positive angles specify counter clockwise
        /// rotations when looking down the axis of rotation towards the
        /// origin.
        /// </summary>
        /// <param name="heading">Y axis rotation in radians.</param>
        /// <param name="pitch">X axis rotation in radians.</param>
        /// <param name="roll">Z axis rotation in radians.</param>
        public void Rotate(float heading, float pitch, float roll)
        {
            camera.Rotate(heading, pitch, roll);
        }

        /// <summary>
        /// Updates the state of the CameraComponent class.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!Game.IsActive) return;
            UpdateInput(gameTime);
            UpdateCamera(gameTime);
        }

        /// <summary>
        /// Undo any camera rolling by leveling the camera. When the camera is
        /// orbiting this method will cause the camera to become level with the
        /// orbit target.
        /// </summary>
        public void UndoRoll()
        {
            camera.UndoRoll();
        }

        /// <summary>
        /// Zooms the camera. This method functions differently depending on
        /// the camera's current behavior. When the camera is orbiting this
        /// method will move the camera closer to or further away from the
        /// orbit target. For the other camera behaviors this method will
        /// change the camera's horizontal field of view.
        /// </summary>
        ///
        /// <param name="zoom">
        /// When orbiting this parameter is how far to move the camera.
        /// For the other behaviors this parameter is the new horizontal
        /// field of view.
        /// </param>
        /// 
        /// <param name="minZoom">
        /// When orbiting this parameter is the min allowed zoom distance to
        /// the orbit target. For the other behaviors this parameter is the
        /// min allowed horizontal field of view.
        /// </param>
        /// 
        /// <param name="maxZoom">
        /// When orbiting this parameter is the max allowed zoom distance to
        /// the orbit target. For the other behaviors this parameter is the max
        /// allowed horizontal field of view.
        /// </param>
        public void Zoom(float zoom, float minZoom, float maxZoom)
        {
            camera.Zoom(zoom, minZoom, maxZoom);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines which way to move the camera based on player input.
        /// The returned values are in the range [-1,1].
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        private void GetMovementDirection(out Vector3 direction)
        {
            direction.X = 0.0f;
            direction.Y = 0.0f;
            direction.Z = 0.0f;

            float dz;
            if ((dz = GetMouseWheelValueDelta()) != 0.0f)
            {
                //direction. = 1 * dz * mouseWheelSpeed;
                //direction.Z = 1 * dz * mouseWheelSpeed;
                direction = ViewMatrix.Forward * dz * 25;
            }

            if (currentKeyboardState.IsKeyDown(actionKeys[Actions.MoveForwardsPrimary]) ||
                currentKeyboardState.IsKeyDown(actionKeys[Actions.MoveForwardsAlternate]))
            {
                if (!movingAlongNegZ)
                {
                    movingAlongNegZ = true;
                    currentVelocity.Z = 0.0f;
                }
                direction.Z += 1.0f;
            }
            else
            {
                movingAlongNegZ = false;
            }

            if (currentKeyboardState.IsKeyDown(actionKeys[Actions.MoveBackwardsPrimary]) ||
                currentKeyboardState.IsKeyDown(actionKeys[Actions.MoveBackwardsAlternate]))
            {
                if (!movingAlongPosZ)
                {
                    movingAlongPosZ = true;
                    currentVelocity.Z = 0.0f;
                }
                direction.Z -= 1.0f;
            }
            else
            {
                movingAlongPosZ = false;
            }

            if (currentKeyboardState.IsKeyDown(actionKeys[Actions.MoveUpPrimary]) ||
                currentKeyboardState.IsKeyDown(actionKeys[Actions.MoveUpAlternate]))
            {
                if (!movingAlongPosY)
                {
                    movingAlongPosY = true;
                    currentVelocity.Y = 0.0f;
                }
                direction.Y += 1.0f;
            }
            else
            {
                movingAlongPosY = false;
            }

            if (currentKeyboardState.IsKeyDown(actionKeys[Actions.MoveDownPrimary]) ||
                currentKeyboardState.IsKeyDown(actionKeys[Actions.MoveDownAlternate]))
            {
                if (!movingAlongNegY)
                {
                    movingAlongNegY = true;
                    currentVelocity.Y = 0.0f;
                }
                direction.Y -= 1.0f;
            }
            else
            {
                movingAlongNegY = false;
            }

            switch (CurrentBehavior)
            {
                case DefaultCamera.Behavior.FirstPerson:
                case DefaultCamera.Behavior.Spectator:
                    if (currentKeyboardState.IsKeyDown(actionKeys[Actions.StrafeRightPrimary]) ||
                        currentKeyboardState.IsKeyDown(actionKeys[Actions.StrafeRightAlternate]))
                    {
                        if (!movingAlongPosX)
                        {
                            movingAlongPosX = true;
                            currentVelocity.X = 0.0f;
                        }
                        direction.X += 1.0f;
                    }
                    else
                    {
                        movingAlongPosX = false;
                    }

                    if (currentKeyboardState.IsKeyDown(actionKeys[Actions.StrafeLeftPrimary]) ||
                        currentKeyboardState.IsKeyDown(actionKeys[Actions.StrafeLeftAlternate]))
                    {
                        if (!movingAlongNegX)
                        {
                            movingAlongNegX = true;
                            currentVelocity.X = 0.0f;
                        }
                        direction.X -= 1.0f;
                    }
                    else
                    {
                        movingAlongNegX = false;
                    }

                    break;

                case DefaultCamera.Behavior.Flight:
                    if (currentKeyboardState.IsKeyDown(actionKeys[Actions.FlightYawLeftPrimary]) ||
                        currentKeyboardState.IsKeyDown(actionKeys[Actions.FlightYawLeftAlternate]))
                    {
                        if (!movingAlongPosX)
                        {
                            movingAlongPosX = true;
                            currentVelocity.X = 0.0f;
                        }
                        direction.X += 1.0f;
                    }
                    else
                    {
                        movingAlongPosX = false;
                    }

                    if (currentKeyboardState.IsKeyDown(actionKeys[Actions.FlightYawRightPrimary]) ||
                        currentKeyboardState.IsKeyDown(actionKeys[Actions.FlightYawRightAlternate]))
                    {
                        if (!movingAlongNegX)
                        {
                            movingAlongNegX = true;
                            currentVelocity.X = 0.0f;
                        }
                        direction.X -= 1.0f;
                    }
                    else
                    {
                        movingAlongNegX = false;
                    }
                    break;

                case DefaultCamera.Behavior.Orbit:
                    if (currentKeyboardState.IsKeyDown(actionKeys[Actions.OrbitRollLeftPrimary]) ||
                        currentKeyboardState.IsKeyDown(actionKeys[Actions.OrbitRollLeftAlternate]))
                    {
                        if (!movingAlongPosX)
                        {
                            movingAlongPosX = true;
                            currentVelocity.X = 0.0f;
                        }
                        direction.X += 1.0f;
                    }
                    else
                    {
                        movingAlongPosX = false;
                    }

                    if (currentKeyboardState.IsKeyDown(actionKeys[Actions.OrbitRollRightPrimary]) ||
                        currentKeyboardState.IsKeyDown(actionKeys[Actions.OrbitRollRightAlternate]))
                    {
                        if (!movingAlongNegX)
                        {
                            movingAlongNegX = true;
                            currentVelocity.X = 0.0f;
                        }
                        direction.X -= 1.0f;
                    }
                    else
                    {
                        movingAlongNegX = false;
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Determines which way the mouse wheel has been rolled.
        /// The returned value is in the range [-1,1].
        /// </summary>
        /// <returns>
        /// A positive value indicates that the mouse wheel has been rolled
        /// towards the player. A negative value indicates that the mouse
        /// wheel has been rolled away from the player.
        /// </returns>
        private float GetMouseWheelDirection()
        {
            return (float)cage.deltaWheel;
        }

        private float GetMouseWheelValueDelta()
        {
            return (float)cage.deltaWheelValue;
        }

        /// <summary>
        /// Event handler for when the game window acquires input focus.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="e">Ignored.</param>
        private void HandleGameActivatedEvent(object sender, EventArgs e)
        {
            if (mouseEnabled)
            {
                cage.Activate();
            }
        }

        /// <summary>
        /// Event hander for when the game window loses input focus.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="e">Ignored.</param>
        private void HandleGameDeactivatedEvent(object sender, EventArgs e)
        {
            if (mouseEnabled)
            {
                cage.Deactivate();
            }
        }


        /// <summary>
        /// Resets all mouse states. This is called whenever the mouse input
        /// behavior switches from click-and-drag mode to real-time mode.
        /// </summary>
        private void ResetMouse()
        {
            filter.Reset();
            cage.Reset(Game.Window.ClientBounds);
        }

        /// <summary>
        /// Dampens the rotation by applying the rotation speed to it.
        /// </summary>
        /// <param name="heading">Y axis rotation in radians.</param>
        /// <param name="pitch">X axis rotation in radians.</param>
        /// <param name="roll">Z axis rotation in radians.</param>
        private void RotateSmoothly(float heading, float pitch, float roll)
        {
            heading *= rotationSpeed;
            pitch *= rotationSpeed;
            roll *= rotationSpeed;

            Rotate(heading, pitch, roll);
        }

        /// <summary>
        /// Gathers and updates input from all supported input devices for use
        /// by the CameraComponent class.
        /// </summary>
        private void UpdateInput(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            currentKeyboardState = Keyboard.GetState();

            cage.Update(Game.Window.ClientBounds);

            if (cage.RightClicked())
            {
                MouseEnabled = !MouseEnabled;
            }

            if (clickAndDragMouseRotation)
            {
                /*
                                int deltaX = 0;
                                int deltaY = 0;

                                if (currentMouseState.LeftButton == ButtonState.Pressed)
                                {
                                    switch (CurrentBehavior)
                                    {
                                        case Camera.Behavior.FirstPerson:
                                        case Camera.Behavior.Spectator:
                                        case Camera.Behavior.Flight:
                                            deltaX = previousMouseState.X - currentMouseState.X;
                                            deltaY = previousMouseState.Y - currentMouseState.Y;
                                            break;

                                        case Camera.Behavior.Orbit:
                                            deltaX = currentMouseState.X - previousMouseState.X;
                                            deltaY = currentMouseState.Y - previousMouseState.Y;
                                            break;
                                    }

                                    PerformMouseFiltering((float)deltaX, (float)deltaY);
                                    PerformMouseSmoothing(smoothedMouseMovement.X, smoothedMouseMovement.Y);
                                }
                */
            }
            else if (mouseEnabled)
            {
                filter.PerformMouseFiltering((float)cage.deltaX, (float)cage.deltaY, elapsed);
                //filter.PerformMouseSmoothing();
                //Console.WriteLine(cage.deltaX + " " + filter.smoothedMouseMovement.X);
            }
        }

        /// <summary>
        /// Updates the camera's velocity based on the supplied movement
        /// direction and the elapsed time (since this method was last
        /// called). The movement direction is the in the range [-1,1].
        /// </summary>
        /// <param name="direction">Direction moved.</param>
        /// <param name="elapsedTimeSec">Elapsed game time.</param>
        private void UpdateVelocity(ref Vector3 direction, float elapsedTimeSec)
        {
            velocity = savedVelocity;
            if (currentKeyboardState.IsKeyDown(Keys.LeftShift) || currentKeyboardState.IsKeyDown(Keys.RightShift))
            {
                savedVelocity = velocity;
                velocity *= 5.0f;
            }

            if (direction.X != 0.0f)
            {
                // Camera is moving along the x axis.
                // Linearly accelerate up to the camera's max speed.
                currentVelocity.X += direction.X * acceleration.X * elapsedTimeSec;

                if (currentVelocity.X > velocity.X)
                    currentVelocity.X = velocity.X;
                else if (currentVelocity.X < -velocity.X)
                    currentVelocity.X = -velocity.X;
            }
            else
            {
                // Camera is no longer moving along the x axis.
                // Linearly decelerate back to stationary state.
                if (currentVelocity.X > 0.0f)
                {
                    if ((currentVelocity.X -= acceleration.X * elapsedTimeSec) < 0.0f)
                        currentVelocity.X = 0.0f;
                }
                else
                {
                    if ((currentVelocity.X += acceleration.X * elapsedTimeSec) > 0.0f)
                        currentVelocity.X = 0.0f;
                }
            }

            if (direction.Y != 0.0f)
            {
                // Camera is moving along the y axis.
                // Linearly accelerate up to the camera's max speed.            
                currentVelocity.Y += direction.Y * acceleration.Y * elapsedTimeSec;

                if (currentVelocity.Y > velocity.Y)
                    currentVelocity.Y = velocity.Y;
                else if (currentVelocity.Y < -velocity.Y)
                    currentVelocity.Y = -velocity.Y;
            }
            else
            {
                // Camera is no longer moving along the y axis.
                // Linearly decelerate back to stationary state.
                if (currentVelocity.Y > 0.0f)
                {
                    if ((currentVelocity.Y -= acceleration.Y * elapsedTimeSec) < 0.0f)
                        currentVelocity.Y = 0.0f;
                }
                else
                {
                    if ((currentVelocity.Y += acceleration.Y * elapsedTimeSec) > 0.0f)
                        currentVelocity.Y = 0.0f;
                }
            }

            if (direction.Z != 0.0f)
            {
                // Camera is moving along the z axis.
                // Linearly accelerate up to the camera's max speed.         
                currentVelocity.Z += direction.Z * acceleration.Z * elapsedTimeSec;

                if (currentVelocity.Z > velocity.Z)
                    currentVelocity.Z = velocity.Z;
                else if (currentVelocity.Z < -velocity.Z)
                    currentVelocity.Z = -velocity.Z;
            }
            else
            {
                // Camera is no longer moving along the z axis.
                // Linearly decelerate back to stationary state.
                if (currentVelocity.Z > 0.0f)
                {
                    if ((currentVelocity.Z -= acceleration.Z * elapsedTimeSec) < 0.0f)
                        currentVelocity.Z = 0.0f;
                }
                else
                {
                    if ((currentVelocity.Z += acceleration.Z * elapsedTimeSec) > 0.0f)
                        currentVelocity.Z = 0.0f;
                }
            }
        }

        /// <summary>
        /// Moves the camera based on player input.
        /// </summary>
        /// <param name="direction">Direction moved.</param>
        /// <param name="elapsedTimeSec">Elapsed game time.</param>
        private void UpdatePosition(ref Vector3 direction, float elapsedTimeSec)
        {
            if (currentVelocity.LengthSquared() != 0.0f)
            {
                // Only move the camera if the velocity vector is not of zero
                // length. Doing this guards against the camera slowly creeping
                // around due to floating point rounding errors.

                Vector3 displacement = (currentVelocity * elapsedTimeSec) +
                    (0.5f * acceleration * elapsedTimeSec * elapsedTimeSec);

                // Floating point rounding errors will slowly accumulate and
                // cause the camera to move along each axis. To prevent any
                // unintended movement the displacement vector is clamped to
                // zero for each direction that the camera isn't moving in.
                // Note that the UpdateVelocity() method will slowly decelerate
                // the camera's velocity back to a stationary state when the
                // camera is no longer moving along that direction. To account
                // for this the camera's current velocity is also checked.

                if (direction.X == 0.0f && (float)Math.Abs(currentVelocity.X) < 1e-6f)
                    displacement.X = 0.0f;

                if (direction.Y == 0.0f && (float)Math.Abs(currentVelocity.Y) < 1e-6f)
                    displacement.Y = 0.0f;

                if (direction.Z == 0.0f && (float)Math.Abs(currentVelocity.Z) < 1e-6f)
                    displacement.Z = 0.0f;

                Move(displacement.X, displacement.Y, displacement.Z);
            }

            // Continuously update the camera's velocity vector even if the
            // camera hasn't moved during this call. When the camera is no
            // longer being moved the camera is decelerating back to its
            // stationary state.

            UpdateVelocity(ref direction, elapsedTimeSec);
        }

        /// <summary>
        /// Updates the state of the camera based on player input.
        /// </summary>
        /// <param name="gameTime">Elapsed game time.</param>
        private void UpdateCamera(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            /*
            GameLibrary.System.KeyboardInput.KeyEventHandler
            if (currentKeyboardState.(Keys.Back))
            {
                switch (CameraComponent.CurrentBehavior)
                {
                    case Camera.Behavior.Flight:
                        camera.UndoRoll();
                        break;

                    case Camera.Behavior.Orbit:
                        if (!camera.PreferTargetYAxisOrbiting)
                            camera.UndoRoll();
                        break;

                    default:
                        break;
                }
            }
            */
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

            //if (KeyJustPressed(Keys.C))
            //    camera.ClickAndDragMouseRotation = !camera.ClickAndDragMouseRotation;

            //if (KeyJustPressed(Keys.Add))
            //{
            //    camera.RotationSpeed += 0.01f;
            //
            //    if (camera.RotationSpeed > 1.0f)
            //        camera.RotationSpeed = 1.0f;
            //}

            //if (KeyJustPressed(Keys.Subtract))
            //{
            //    camera.RotationSpeed -= 0.01f;
            //
            //    if (camera.RotationSpeed <= 0.0f)
            //        camera.RotationSpeed = 0.01f;
            //}


            Vector3 direction = new Vector3();
            GetMovementDirection(out direction);

            // TODO better integrate game pad...
            GamePadThumbSticks thumbSticks = currentGamePadState.ThumbSticks;
            GamePadTriggers triggers = currentGamePadState.Triggers;
            Vector2 v = thumbSticks.Right;
            if (v.X != 0 || v.Y != 0)
            {
                if (triggers.Left > 0)
                {
                    //Zoom += (float)(ZoomSpeed * elapsed * -v.Y);
                    if (v.X != 0)
                    {
                        RotateSmoothly(-v.X * elapsed, 0, 0);
                    }
                }
                else
                {
                    //Console.WriteLine(v.X);
                    RotateSmoothly(-v.X * elapsed, v.Y * elapsed, 0);
                }
            }
            switch (camera.CurrentBehavior)
            {
                case DefaultCamera.Behavior.FirstPerson:
                case DefaultCamera.Behavior.Spectator:
                    {
                        float dx = filter.smoothedMouseMovement.X;
                        float dy = filter.smoothedMouseMovement.Y;
                        //Console.Out.WriteLine(dx + " " + elapsed);
                        if (elapsed != 0 && (dx != 0 || dy != 0))
                        {
                            // FIXME should be constant speed as dx/dy is a distance (elapsed is already included)
                            // but it stutters...
                            //float mouseSpeed = 1 / elapsed;
                            //float mouseSpeed = 0.005f;// elapsed; // 0.1f;

                            //Console.WriteLine(dx + " " + (dx / elapsed) + " " + elapsed);

                            // pixel / seconds ?
                            //dx /= elapsed;
                            //dy /= elapsed;

                            RotateSmoothly(dx, dy, 0.0f);
                        }

                        /*if ((dz = GetMouseWheelValueDelta()) != 0.0f)
                        {
                            camera.Move(ZAxis, new Vector3(dz * mouseWheelSpeed / 25));
                        }*/

                        //UpdatePosition(ref direction, elapsed);
                    }
                    break;
                case DefaultCamera.Behavior.Flight:
                    {
                        float dx = 0;
                        float dy = -filter.smoothedMouseMovement.Y;
                        float dz = filter.smoothedMouseMovement.X;

                        if (dy != 0 || dz != 0)
                        {
                            RotateSmoothly(0.0f, dy, dz);
                        }

                        if ((dx = direction.X * flightYawSpeed * elapsed) != 0.0f)
                        {
                            camera.Rotate(dx, 0.0f, 0.0f);
                        }

                        direction.X = 0.0f; // ignore yaw motion when updating camera's velocity
                                            //UpdatePosition(ref direction, elapsed);
                    }
                    break;
                case DefaultCamera.Behavior.Orbit:
                    {
                        float dx = -filter.smoothedMouseMovement.X;
                        float dy = -filter.smoothedMouseMovement.Y;
                        float dz = 0;

                        if (dx != 0 || dy != 0)
                        {
                            RotateSmoothly(dx, dy, 0.0f);
                        }

                        if (!camera.PreferTargetYAxisOrbiting)
                        {
                            if ((dz = direction.X * orbitRollSpeed * elapsed) != 0.0f)
                            {
                                camera.Rotate(0.0f, 0.0f, dz);
                            }
                        }

                        if ((dz = GetMouseWheelDirection() * mouseWheelSpeed) != 0.0f)
                        {
                            camera.Zoom(dz, camera.OrbitMinZoom, camera.OrbitMaxZoom);
                        }
                    }
                    break;
                default:
                    break;
            }
            // must call update position each because of camera "inertia"
            // camera position and inertia should be handled by avatar and physics...
            // so things like camera inertia should be optional
            UpdatePosition(ref direction, elapsed);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Property to get and set the camera's acceleration.
        /// </summary>
        public Vector3 Acceleration
        {
            get { return acceleration; }
            set { acceleration = value; }
        }

        /// <summary>
        /// Property to get and set the mouse rotation behavior.
        /// The default is false which will immediately rotate the camera
        /// as soon as the mouse is moved. If this property is set to true
        /// camera rotations only occur when the mouse button is held down and
        /// the mouse dragged (i.e., clicking-and-dragging the mouse).
        /// </summary>
        public bool ClickAndDragMouseRotation
        {
            get { return clickAndDragMouseRotation; }

            set
            {
                clickAndDragMouseRotation = value;

                Game.IsMouseVisible = value;
                if (!value)
                    ResetMouse();
            }
        }

        public bool MouseEnabled
        {
            get { return mouseEnabled; }

            set
            {
                mouseEnabled = value;

                Game.IsMouseVisible = !value;
                if (value)
                    cage.Activate();
                else
                    cage.Deactivate();
            }
        }

        /// <summary>
        /// Property to get and set the camera's behavior.
        /// </summary>
        public DefaultCamera.Behavior CurrentBehavior
        {
            get { return camera.CurrentBehavior; }
            set { camera.CurrentBehavior = value; }
        }

        /// <summary>
        /// Property to get the camera's current velocity.
        /// </summary>
        public Vector3 CurrentVelocity
        {
            get { return currentVelocity; }
        }

        /// <summary>
        /// Property to get and set the flight behavior's yaw speed.
        /// </summary>
        public float FlightYawSpeed
        {
            get { return flightYawSpeed; }
            set { flightYawSpeed = value; }
        }

        /// <summary>
        /// Property to get and set the sensitivity value used to smooth
        /// mouse movement.
        /// </summary>
        public float MouseSmoothingSensitivity
        {
            get { return filter.mouseSmoothingSensitivity; }
            set { filter.mouseSmoothingSensitivity = value; }
        }

        /// <summary>
        /// Property to get and set the speed of the mouse wheel.
        /// This is used to zoom in and out when the camera is orbiting.
        /// </summary>
        public float MouseWheelSpeed
        {
            get { return mouseWheelSpeed; }
            set { mouseWheelSpeed = value; }
        }

        /// <summary>
        /// Property to get and set the max orbit zoom distance.
        /// </summary>
        public float OrbitMaxZoom
        {
            get { return camera.OrbitMaxZoom; }
            set { camera.OrbitMaxZoom = value; }
        }

        /// <summary>
        /// Property to get and set the min orbit zoom distance.
        /// </summary>
        public float OrbitMinZoom
        {
            get { return camera.OrbitMinZoom; }
            set { camera.OrbitMinZoom = value; }
        }

        /// <summary>
        /// Property to get and set the distance from the target when orbiting.
        /// </summary>
        public float OrbitOffsetDistance
        {
            get { return camera.OrbitOffsetDistance; }
            set { camera.OrbitOffsetDistance = value; }
        }

        /// <summary>
        /// Property to get and set the orbit behavior's rolling speed.
        /// This only applies when PreferTargetYAxisOrbiting is set to false.
        /// Orbiting with PreferTargetYAxisOrbiting set to true will ignore
        /// any camera rolling.
        /// </summary>
        public float OrbitRollSpeed
        {
            get { return orbitRollSpeed; }
            set { orbitRollSpeed = value; }
        }

        /// <summary>
        /// Property to get and set the camera orbit target position.
        /// </summary>
        public Vector3 OrbitTarget
        {
            get { return camera.OrbitTarget; }
            set { camera.OrbitTarget = value; }
        }

        public float FovX
        {
            get { return camera.FovX; }
            set { camera.FovX = value; }
        }

        public float AspectRatio
        {
            get { return camera.AspectRatio; }
            set { camera.AspectRatio = value; }
        }

        public float ZNear
        {
            get { return camera.ZNear; }
            set { camera.ZNear = value; }
        }

        public float ZFar
        {
            get { return camera.ZFar; }
            set { camera.ZFar = value; }
        }

        /// <summary>
        /// Property to get and set the camera orientation.
        /// </summary>
        public Quaternion Orientation
        {
            get { return camera.Orientation; }
            set { camera.Orientation = value; }
        }

        /// <summary>
        /// Property to get and set the camera position.
        /// </summary>
        public Vector3 Position
        {
            get { return camera.Position; }
            set { camera.Position = value; }
        }

        /// <summary>
        /// Property to get the viewing direction vector.
        /// </summary>
        public Vector3 ViewDirection
        {
            get { return camera.ViewDirection; }
        }

        /// <summary>
        /// Property to get and set the flag to force the camera
        /// to orbit around the orbit target's Y axis rather than the camera's
        /// local Y axis.
        /// </summary>
        public bool PreferTargetYAxisOrbiting
        {
            get { return camera.PreferTargetYAxisOrbiting; }
            set { camera.PreferTargetYAxisOrbiting = value; }
        }

        /// <summary>
        /// Property to get and set the mouse rotation speed.
        /// </summary>
        public float RotationSpeed
        {
            get { return rotationSpeed; }
            set { rotationSpeed = value; }
        }

        /// <summary>
        /// Property to get and set the camera's velocity.
        /// </summary>
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        /// <summary>
        /// Property to get the perspective projection matrix.
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get { return camera.ProjectionMatrix; }
        }

        /// <summary>
        /// Property to get the view matrix.
        /// </summary>
        public Matrix ViewMatrix
        {
            get { return camera.ViewMatrix; }
        }

        /// <summary>
        /// Property to get the concatenated view-projection matrix.
        /// </summary>
        public Matrix ViewProjectionMatrix
        {
            get { return camera.ViewProjectionMatrix; }
        }

        /// <summary>
        /// Property to get the concatenated view-projection matrix.
        /// </summary>
        public Matrix InverseViewProjectionMatrix
        {
            get { return camera.InverseViewProjectionMatrix; }
        }

        public int VisitOrder
        {
            get { return camera.VisitOrder; }
        }

        public SceneGraph.Bounding.Frustum Frustum
        {
            get { return camera.Frustum; }
        }

        public SceneGraph.Bounding.Box BoundingBox
        {
            get { return camera.BoundingBox; }
        }

        public SceneGraph.Bounding.Sphere BoundingSphere
        {
            get { return camera.BoundingSphere; }
        }

        /// <summary>
        /// Property to get the camera's local X axis.
        /// </summary>
        public Vector3 XAxis
        {
            get { return camera.XAxis; }
        }

        /// <summary>
        /// Property to get the camera's local Y axis.
        /// </summary>
        public Vector3 YAxis
        {
            get { return camera.YAxis; }
        }

        /// <summary>
        /// Property to get the camera's local Z axis.
        /// </summary>
        public Vector3 ZAxis
        {
            get { return camera.ZAxis; }
        }

        #endregion
    }

    internal sealed class MouseCage
    {
        private MouseState currentMouseState;
        private MouseState previousMouseState;

        internal int deltaX;
        internal int deltaY;
        internal int deltaWheel;
        internal int deltaWheelValue;

        private int savedMousePosX;
        private int savedMousePosY;

        private int cageSize = 100;

        private bool active;

        internal MouseCage()
        {
            currentMouseState = Mouse.GetState();
            previousMouseState = currentMouseState;
            savedMousePosX = -1;
            savedMousePosY = -1;
            active = true;
        }

        internal void Activate()
        {
            if (savedMousePosX >= 0 && savedMousePosY >= 0)
            {
                Mouse.SetPosition(savedMousePosX, savedMousePosY);
                currentMouseState = Mouse.GetState();
            }
            active = true;
        }

        internal void Deactivate()
        {
            MouseState state = Mouse.GetState();
            savedMousePosX = state.X;
            savedMousePosY = state.Y;
            active = false;
        }

        internal void Update(Rectangle clientBounds)
        {
            previousMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

            int centerX = clientBounds.Width / 2;
            int centerY = clientBounds.Height / 2;
            deltaX = previousMouseState.X - currentMouseState.X;
            deltaY = previousMouseState.Y - currentMouseState.Y;

            //Console.WriteLine(deltaX);

            /*
            if (deltaX != 0 || deltaY != 0)
            {
                Console.WriteLine(deltaX + " " + deltaY);
            }
            */

            int currentWheelValue = currentMouseState.ScrollWheelValue;
            int previousWheelValue = previousMouseState.ScrollWheelValue;
            if (currentWheelValue > previousWheelValue)
                deltaWheel = -1;
            else if (currentWheelValue < previousWheelValue)
                deltaWheel = 1;
            else
                deltaWheel = 0;
            deltaWheelValue = currentWheelValue - previousWheelValue;

            if (active)
            {
                // see https://github.com/MonoGame/MonoGame/issues/6262#issuecomment-417870908
                int dx = Math.Abs(centerX - currentMouseState.X);
                int dy = Math.Abs(centerY - currentMouseState.Y);
                if ((dx >= cageSize) || (dy >= cageSize))
                {
                    Mouse.SetPosition(centerX, centerY);
                    currentMouseState = Mouse.GetState();
                    //Console.WriteLine(currentMouseState.Position);
                }
            }
        }

        public bool RightClicked()
        {
            return ((currentMouseState.RightButton == ButtonState.Pressed) && (previousMouseState.RightButton == ButtonState.Released));
        }

        internal void Reset(Rectangle clientBounds)
        {
            if (active)
            {
                int centerX = clientBounds.Width / 2;
                int centerY = clientBounds.Height / 2;
                Mouse.SetPosition(centerX, centerY);
            }

            currentMouseState = Mouse.GetState();
            previousMouseState = currentMouseState;

            deltaX = 0;
            deltaY = 0;

            savedMousePosX = -1;
            savedMousePosY = -1;
        }
    }

    internal sealed class MouseFilter
    {
        struct Sample
        {
            public Vector2 Position;
            public int Count;
            public float Elapsed;
        }

        private Sample[] mouseSmoothingCache;
        private int writeIndex;

        private Vector2[] mouseMovement;
        private int mouseIndex;

        internal float mouseSmoothingSensitivity;

        internal Vector2 smoothedMouseMovement;

        internal MouseFilter()
        {
            // add one for the write slot
            mouseSmoothingCache = new Sample[DefaultCameraComponent.MOUSE_SMOOTHING_SAMPLE_COUNT + 1];
            writeIndex = 0;

            mouseIndex = 0;
            mouseMovement = new Vector2[2];
            mouseMovement[0].X = 0.0f;
            mouseMovement[0].Y = 0.0f;
            mouseMovement[1].X = 0.0f;
            mouseMovement[1].Y = 0.0f;
        }

        /// <summary>
        /// Filters the mouse movement based on a weighted sum of mouse
        /// movement from previous frames.
        /// <para>
        /// For further details see:
        ///  Nettle, Paul "Smooth Mouse Filtering", flipCode's Ask Midnight column.
        ///  http://www.flipcode.com/cgi-bin/fcarticles.cgi?show=64462
        /// </para>
        /// </summary>
        /// <param name="x">Horizontal mouse distance from window center.</param>
        /// <param name="y">Vertical mouse distance from window center.</param>
        internal void PerformMouseFiltering(float x, float y, float elapsed)
        {
            // TODO should decimate samples at high fps (or average them)

            // Store the current mouse movement entry at the front of cache.
            float dt = DefaultCameraComponent.DEFAULT_MOUSE_SMOOTHING_PERIOD / DefaultCameraComponent.MOUSE_SMOOTHING_SAMPLE_COUNT;
            if (mouseSmoothingCache[writeIndex].Elapsed + elapsed >= dt)
            {
                mouseSmoothingCache[writeIndex].Position.X += x;
                mouseSmoothingCache[writeIndex].Position.Y += y;
                mouseSmoothingCache[writeIndex].Count++;
                mouseSmoothingCache[writeIndex].Elapsed += elapsed;
                //Console.WriteLine("*** " + dt + " " + mouseSmoothingCache[writeIndex].Elapsed);
                // move to next
                writeIndex = (writeIndex + 1) % mouseSmoothingCache.Length;
                mouseSmoothingCache[writeIndex].Position.X = 0;
                mouseSmoothingCache[writeIndex].Position.Y = 0;
                mouseSmoothingCache[writeIndex].Count = 0;
                mouseSmoothingCache[writeIndex].Elapsed = 0;
            }
            else
            {
                mouseSmoothingCache[writeIndex].Position.X += x;
                mouseSmoothingCache[writeIndex].Position.Y += y;
                mouseSmoothingCache[writeIndex].Count++;
                mouseSmoothingCache[writeIndex].Elapsed += elapsed;
                //Console.WriteLine("--- " + elapsed);
            }

            float averageX = 0.0f;
            float averageY = 0.0f;
            float averageTotal = 0.0f;
            //float currentWeight = 1.0f;

            // Filter the mouse movement with the rest of the cache entries.
            // Use a weighted average where newer entries have more effect than
            // older entries (towards the back of the cache).
            float time = 0;
            int count = 0;
            int index = writeIndex;
            if (mouseSmoothingCache[writeIndex].Elapsed == 0)
            {
                index = (writeIndex - 1 + mouseSmoothingCache.Length) % mouseSmoothingCache.Length;
            }
            while (time < DefaultCameraComponent.DEFAULT_MOUSE_SMOOTHING_PERIOD)
            {
                if (mouseSmoothingCache[index].Elapsed == 0)
                {
                    // no more samples...
                    Console.WriteLine("*** NO MORE SAMPLES");
                    break;
                }
                count++;
                // exp 
                // see https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
                // see http://howlingmoonsoftware.com/wordpress/useful-math-snippets/
                // TODO should use 
                //currentWeight *= mouseSmoothingSensitivity;
                float t = time / DefaultCameraComponent.DEFAULT_MOUSE_SMOOTHING_PERIOD;
                //t = t * t;
                t = 1 - (float)Math.Pow(t, mouseSmoothingSensitivity);
                //t = (float) Math.Exp(mouseSmoothingSensitivity * t);
                float weight = t;// MathHelper.LerpPrecise(1.0f, 0.0f, t);
                averageX += mouseSmoothingCache[index].Position.X * weight / mouseSmoothingCache[index].Count;
                averageY += mouseSmoothingCache[index].Position.Y * weight / mouseSmoothingCache[index].Count;
                averageTotal += 1.0f * weight;
                time += mouseSmoothingCache[index].Elapsed;
                index = (index - 1 + mouseSmoothingCache.Length) % mouseSmoothingCache.Length;
                if (index == writeIndex)
                {
                    break;
                }
            }
            if (time < DefaultCameraComponent.DEFAULT_MOUSE_SMOOTHING_PERIOD)
            {
                Console.WriteLine("*** NOT ENOUGH SAMPLES");
            }
            //Console.WriteLine(time + " " + index + " " + writeIndex + " " + ((writeIndex - index + mouseSmoothingCache.Length) % mouseSmoothingCache.Length) + " " + count);

            // Calculate the new smoothed mouse movement.
            smoothedMouseMovement.X = averageX / averageTotal;
            smoothedMouseMovement.Y = averageY / averageTotal;
        }

        //float max = float.MinValue;

        /// <summary>
        /// Averages the mouse movement over a couple of frames to smooth out
        /// the mouse movement.
        /// </summary>
        internal void PerformMouseSmoothing()
        {
            // FIXME divide by 10 is totally arbitrary...
            mouseMovement[mouseIndex].X = smoothedMouseMovement.X;// / 10f;
            mouseMovement[mouseIndex].Y = smoothedMouseMovement.Y;// / 10f;

            smoothedMouseMovement.X = (mouseMovement[0].X + mouseMovement[1].X) * 0.5f;
            smoothedMouseMovement.Y = (mouseMovement[0].Y + mouseMovement[1].Y) * 0.5f;

            /*
            if (smoothedMouseMovement.X > max)
            {
                max = smoothedMouseMovement.X;
                //434,1144
                Console.WriteLine(max);
            }
            */

            mouseIndex ^= 1;
            //mouseMovement[mouseIndex].X = 0.0f;
            //mouseMovement[mouseIndex].Y = 0.0f;
        }

        internal void Reset()
        {
            for (int i = 0; i < mouseMovement.Length; ++i)
                mouseMovement[i] = Vector2.Zero;

            for (int i = 0; i < mouseSmoothingCache.Length; ++i)
            {
                mouseSmoothingCache[i].Position = Vector2.Zero;
                mouseSmoothingCache[i].Elapsed = 0;
            }

            smoothedMouseMovement = Vector2.Zero;
            mouseIndex = 0;
        }
    }
}