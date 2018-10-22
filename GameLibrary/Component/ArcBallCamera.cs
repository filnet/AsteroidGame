using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameLibrary.SceneGraph;
using GameLibrary.Control;

namespace GameLibrary
{
    public class ArcBallCamera : InputController, ICameraComponent
    {
        private static readonly float DEFAULT_ZOOM = 8.0f;

        // Set rates in world units per 1/60th second (the default fixed-step interval).
        private float rotationSpeed = 1f / 60f;

        private float zoomSpeed = 1.8f / 60f;
        //float forwardSpeed = 50f / 60f;

        private const float verticalAngleMin = -(MathHelper.PiOver2 - 0.01f);
        private const float verticalAngleMax = MathHelper.PiOver2 - 0.01f;
        private const float zoomMin = 0.1f;
        private const float zoomMax = 50.0f;

        private Matrix rotation = Matrix.Identity;
        private Vector3 position = Vector3.Zero;

        // Simply feed this camera the position of whatever you want its target to be
        private Vector3 targetPosition = Vector3.Zero;

        private Matrix viewMatrix;
        private Matrix projectionMatrix;
        private Matrix viewProjectionMatrix;

        private BoundingFrustum boundingFrustum;

        private float zoom = DEFAULT_ZOOM;
        private float horizontalAngle;// = MathHelper.PiOver4;
        private float verticalAngle;// = MathHelper.PiOver4;

        private bool clampVerticalAngle = true;

        private bool viewMatrixDirty;
        private bool viewProjectionMatrixDirty;
        private bool boundingFrustumDirty;

        #region Properties
        public Matrix ViewMatrix
        {
            get
            {
                if (viewMatrixDirty)
                {
                    updateMatrix();
                }
                return viewMatrix;
            }
        }

        public Matrix ProjectionMatrix
        {
            get
            {
                return projectionMatrix;
            }
        }

        public Matrix ViewProjectionMatrix
        {
            get
            {
                if (viewProjectionMatrixDirty)
                {
                    viewProjectionMatrix = viewMatrix * projectionMatrix;
                    viewProjectionMatrixDirty = false;
                }
                return viewProjectionMatrix;
            }
        }

        public BoundingFrustum BoundingFrustum
        {
            get
            {
                if (boundingFrustumDirty)
                {
                    boundingFrustum = new BoundingFrustum(ViewMatrix * ProjectionMatrix);
                    boundingFrustumDirty = false;
                }
                return boundingFrustum;
            }
        }

        public float Zoom
        {
            get
            {
                return zoom;
            }
            set
            {
                // Keep zoom within range
                value = MathHelper.Clamp(value, zoomMin, zoomMax);
                if (zoom != value)
                {
                    zoom = value;
                    viewMatrixDirty = true;
                    viewProjectionMatrixDirty = true;
                    boundingFrustumDirty = true;
                }
            }
        }

        public float HorizontalAngle
        {
            get
            {
                return horizontalAngle;
            }
            set
            {
                // Keep horizontalAngle between -pi and pi.
                value = value % (MathHelper.Pi * 2);
                if (horizontalAngle != value)
                {
                    horizontalAngle = value;

                    viewMatrixDirty = true;
                    viewProjectionMatrixDirty = true;
                    boundingFrustumDirty = true;
                }
            }
        }

        public float VerticalAngle
        {
            get
            {
                return verticalAngle;
            }
            set
            {
                if (clampVerticalAngle)
                {
                    // keep vertical angle within tolerances
                    value = MathHelper.Clamp(value, verticalAngleMin, verticalAngleMax);
                }
                // Keep horizontalAngle between -pi and pi.
                //verticalAngle = value % (MathHelper.Pi * 2);
                if (verticalAngle != value)
                {
                    verticalAngle = value;

                    viewMatrixDirty = true;
                    viewProjectionMatrixDirty = true;
                    boundingFrustumDirty = true;
                }
            }
        }

        public Vector3 TargetPosition
        {
            get
            {
                return targetPosition;
            }
            set
            {
                if (targetPosition != value)
                {
                    targetPosition = value;

                    viewMatrixDirty = true;
                    viewProjectionMatrixDirty = true;
                    boundingFrustumDirty = true;
                }
            }
        }

        #endregion

        public ArcBallCamera()
            : base()
        {
            viewMatrix = Matrix.Identity;
            projectionMatrix = Matrix.Identity;
            boundingFrustum = new BoundingFrustum(Matrix.Identity);
            viewMatrixDirty = true;
            viewProjectionMatrixDirty = true;
            boundingFrustumDirty = true;
        }

        public void Initialize()
        {
        }

        // FOV is in radians
        // screenWidth and screenHeight are pixel values. They're floats because we need to divide them to get an aspect ratio.
        public void Perspective(float FOV, float aspect, float nearPlane, float farPlane)
        {
            //if (screenHeight < float.Epsilon)
            //    throw new Exception("screenHeight cannot be zero or a negative value");

            //if (screenWidth < float.Epsilon)
            //    throw new Exception("screenWidth cannot be zero or a negative value");

            //if (nearPlane < 0.1f)
            //    throw new Exception("nearPlane must be greater than 0.1");

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(FOV, aspect, nearPlane, farPlane);

            boundingFrustumDirty = true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            /*
            if (dirty)
            {
                updateMatrix();
            }
             */

            TimeSpan elapsedSpan = gameTime.ElapsedGameTime;
            double elapsed = elapsedSpan.TotalSeconds * 60;

            if (IsKeyDown(Keys.R) || IsButtonDown(Buttons.Start))
            {
                VerticalAngle = 0;
                HorizontalAngle = 0;
                Zoom = DEFAULT_ZOOM;
                TargetPosition = Vector3.Zero;
            }
            if (/*IsKeyDown(Keys.Left) ||*/ (GamePadState.DPad.Left == ButtonState.Pressed))
            {
                // Rotate left.
                //RotateAroundY((float) ((double) rotationSpeed * elapsed));
                TargetPosition += new Vector3(-.01f, 0, 0);
            }
            if (/*IsKeyDown(Keys.Right) ||*/ (GamePadState.DPad.Right == ButtonState.Pressed))
            {
                // Rotate right.
                //RotateAroundY(-(float) ((double) rotationSpeed * elapsed));
                TargetPosition += new Vector3(.01f, 0, 0);
            }
            if (/*IsKeyDown(Keys.Up) ||*/ (GamePadState.DPad.Up == ButtonState.Pressed))
            {
                //RotateAroundX((float) ((double) rotationSpeed * elapsed));
                TargetPosition += new Vector3(0, .01f, 0);

                //Matrix forwardMovement = Matrix.CreateRotationY(yaw);
                //Vector3 v = new Vector3(0, 0, forwardSpeed);
                //v = Vector3.Transform(v, forwardMovement);
                //avatarPosition.Z += v.Z;
                //avatarPosition.X += v.X;
            }
            if (/*IsKeyDown(Keys.Down) ||*/ (GamePadState.DPad.Down == ButtonState.Pressed))
            {
                //RotateAroundX(-(float) ((double) rotationSpeed * elapsed));
                TargetPosition += new Vector3(0, -.01f, 0);

                //Matrix forwardMovement = Matrix.CreateRotationY(yaw);
                //Vector3 v = new Vector3(0, 0, -forwardSpeed);
                //v = Vector3.Transform(v, forwardMovement);
                //avatarPosition.Z += v.Z;
                //avatarPosition.X += v.X;
            }
            GamePadThumbSticks thumbSticks = GamePadState.ThumbSticks;
            GamePadTriggers triggers = GamePadState.Triggers;

            Vector2 v = thumbSticks.Right;
            if (v.X != 0)
            {
                if (triggers.Left > 0)
                {
                    RotateAroundY((float)((double)rotationSpeed * elapsed * -v.X));
                }
                else
                {
                    RotateAroundY((float)((double)rotationSpeed * elapsed * -v.X));
                }
            }
            if (v.Y != 0)
            {
                if (triggers.Left > 0)
                {
                    Zoom += (float)(zoomSpeed * elapsed * -v.Y);
                }
                else
                {
                    RotateAroundX((float)((double)rotationSpeed * elapsed * v.Y));
                }
            }

        }

        private void updateMatrix()
        {
            // Start with an initial offset
            Vector3 cameraPosition = new Vector3(0.0f, 0.0f, zoom);

            // Rotate vertically
            cameraPosition = Vector3.Transform(cameraPosition, Matrix.CreateRotationX(verticalAngle));

            // Rotate horizontally
            cameraPosition = Vector3.Transform(cameraPosition, Matrix.CreateRotationY(horizontalAngle));

            position = cameraPosition + targetPosition;
            this.LookAt(targetPosition);

            // Compute view matrix
            this.viewMatrix = Matrix.CreateLookAt(this.position,
                                                  this.position + this.rotation.Forward,
                                                  this.rotation.Up);
            viewMatrixDirty = false;
        }

        /// <summary>
        /// Points camera in direction of any position.
        /// </summary>
        /// <param name="targetPos">Target position for camera to face.</param>
        public void LookAt(Vector3 targetPos)
        {
            Vector3 newForward = targetPos - this.position;
            newForward.Normalize();
            this.rotation.Forward = newForward;

            Vector3 referenceVector = Vector3.UnitY;

            // On the slim chance that the camera is pointer perfectly parallel with the Y Axis, we cannot
            // use cross product with a parallel axis, so we change the reference vector to the forward axis (Z).
            if (this.rotation.Forward.Y == referenceVector.Y || this.rotation.Forward.Y == -referenceVector.Y)
            {
                referenceVector = Vector3.UnitZ;
            }

            this.rotation.Right = Vector3.Cross(this.rotation.Forward, referenceVector);
            this.rotation.Up = Vector3.Cross(this.rotation.Right, this.rotation.Forward);

            viewMatrixDirty = true;
            viewProjectionMatrixDirty = true;
            boundingFrustumDirty = true;
        }

        // Y axis rotation - also known as Yaw
        private void RotateAroundY(float angle)
        {
            //yaw += angle;

            //// keep the value in the range 0-360 (0 - 2 PI radians)
            //if (yaw > Math.PI * 2)
            //    yaw -= MathHelper.Pi * 2;
            //else if (yaw < 0)
            //    yaw += MathHelper.Pi * 2;

            HorizontalAngle += angle;
        }

        // X axis rotation - also known as Pitch
        private void RotateAroundX(float angle)
        {
            //pitch += angle;

            //// keep the value in the range 0-360 (0 - 2 PI radians)
            //if (pitch > Math.PI * 2)
            //    pitch -= MathHelper.Pi * 2;
            //else if (pitch < 0)
            //    pitch += MathHelper.Pi * 2;

            VerticalAngle += angle;
        }

    }
}
