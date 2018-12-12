using Microsoft.Xna.Framework;
using System;

namespace GameLibrary.Component.Camera
{
    /// <summary>
    /// A general purpose 6DoF (six degrees of freedom) quaternion based
    /// camera. This camera class supports 4 different behaviors: first
    /// person mode, spectator mode, flight mode, and orbit mode. First
    /// person mode only allows 5DOF (x axis movement, y axis movement,
    /// z axis movement, yaw, and pitch) and movement is always parallel
    /// to the world x-z (ground) plane. Spectator mode is similar to first
    /// person mode only movement is along the direction the camera is
    /// pointing. Flight mode supports 6DoF. This is the camera class'
    /// default behavior. Orbit mode rotates the camera around a target
    /// position. This mode can be used to simulate a third person camera.
    /// </summary>
    public class DefaultCamera : Camera
    {
        public enum Behavior
        {
            FirstPerson,
            Spectator,
            Flight,
            Orbit
        };

        public const float DEFAULT_FOVX = MathHelper.PiOver2;
        public const float DEFAULT_ZNEAR = 0.1f;
        public const float DEFAULT_ZFAR = 1000.0f;

        public const float DEFAULT_ORBIT_MIN_ZOOM = DEFAULT_ZNEAR + 1.0f;
        public const float DEFAULT_ORBIT_MAX_ZOOM = DEFAULT_ZFAR * 0.5f;

        public const float DEFAULT_ORBIT_OFFSET_LENGTH = DEFAULT_ORBIT_MIN_ZOOM +
            (DEFAULT_ORBIT_MAX_ZOOM - DEFAULT_ORBIT_MIN_ZOOM) * 0.25f;

        private static Vector3 WORLD_X_AXIS = new Vector3(1.0f, 0.0f, 0.0f);
        private static Vector3 WORLD_Y_AXIS = new Vector3(0.0f, 1.0f, 0.0f);
        private static Vector3 WORLD_Z_AXIS = new Vector3(0.0f, 0.0f, 1.0f);

        private Behavior behavior;
        private bool preferTargetYAxisOrbiting;

        private float fovx;
        private float aspectRatio;
        private float znear;
        private float zfar;
        private float accumPitch;
        private float orbitMinZoom;
        private float orbitMaxZoom;
        private float orbitOffsetLength;
        private float firstPersonYOffset;

        private Vector3 eye;
        private Vector3 target;
        private Vector3 targetYAxis;
        private Vector3 xAxis;
        private Vector3 yAxis;
        private Vector3 zAxis;
        private Vector3 viewDir;

        private Quaternion orientation;
        private Matrix viewMatrix;
        private Matrix projectionMatrix;
        private Matrix viewProjectionMatrix;
        private Matrix invViewProjectionMatrix;

        private BoundingFrustum boundingFrustum;

        private Quaternion savedOrientation;
        private Vector3 savedEye;
        private float savedAccumPitch;

        private bool viewDirty;
        private bool viewProjectionDirty;
        private bool frustrumDirty;

        #region Public Methods

        /// <summary>
        /// Constructs a new instance of the camera class. The camera will
        /// have a flight behavior, and will be initially positioned at the
        /// world origin looking down the world negative z axis.
        /// </summary>
        public DefaultCamera()
        {
            behavior = Behavior.Flight;
            preferTargetYAxisOrbiting = true;

            fovx = DEFAULT_FOVX;
            znear = DEFAULT_ZNEAR;
            zfar = DEFAULT_ZFAR;

            accumPitch = 0.0f;
            orbitMinZoom = DEFAULT_ORBIT_MIN_ZOOM;
            orbitMaxZoom = DEFAULT_ORBIT_MAX_ZOOM;
            orbitOffsetLength = DEFAULT_ORBIT_OFFSET_LENGTH;
            firstPersonYOffset = 0.0f;

            eye = Vector3.Zero;
            target = Vector3.Zero;
            targetYAxis = Vector3.UnitY;
            xAxis = Vector3.UnitX;
            yAxis = Vector3.UnitY;
            zAxis = Vector3.UnitZ;

            orientation = Quaternion.Identity;
            viewMatrix = Matrix.Identity;

            savedEye = eye;
            savedOrientation = orientation;
            savedAccumPitch = 0.0f;

            viewProjectionDirty = true;
            frustrumDirty = true;
        }

        /// <summary>
        /// Builds a look at style viewing matrix.
        /// </summary>
        /// <param name="target">The target position to look at.</param>
        public void LookAt(Vector3 target)
        {
            LookAt(eye, target, yAxis);
        }

        /// <summary>
        /// Builds a look at style viewing matrix.
        /// </summary>
        /// <param name="eye">The camera position.</param>
        /// <param name="target">The target position to look at.</param>
        /// <param name="up">The up direction.</param>
        public void LookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            this.eye = eye;
            this.target = target;

            zAxis = eye - target;
            zAxis.Normalize();

            viewDir.X = -zAxis.X;
            viewDir.Y = -zAxis.Y;
            viewDir.Z = -zAxis.Z;

            Vector3.Cross(ref up, ref zAxis, out xAxis);
            xAxis.Normalize();

            Vector3.Cross(ref zAxis, ref xAxis, out yAxis);
            yAxis.Normalize();
            xAxis.Normalize();

            viewMatrix.M11 = xAxis.X;
            viewMatrix.M21 = xAxis.Y;
            viewMatrix.M31 = xAxis.Z;
            Vector3.Dot(ref xAxis, ref eye, out viewMatrix.M41);
            viewMatrix.M41 = -viewMatrix.M41;

            viewMatrix.M12 = yAxis.X;
            viewMatrix.M22 = yAxis.Y;
            viewMatrix.M32 = yAxis.Z;
            Vector3.Dot(ref yAxis, ref eye, out viewMatrix.M42);
            viewMatrix.M42 = -viewMatrix.M42;

            viewMatrix.M13 = zAxis.X;
            viewMatrix.M23 = zAxis.Y;
            viewMatrix.M33 = zAxis.Z;
            Vector3.Dot(ref zAxis, ref eye, out viewMatrix.M43);
            viewMatrix.M43 = -viewMatrix.M43;

            viewMatrix.M14 = 0.0f;
            viewMatrix.M24 = 0.0f;
            viewMatrix.M34 = 0.0f;
            viewMatrix.M44 = 1.0f;

            accumPitch = (float)Math.Asin(viewMatrix.M23);
            Quaternion.CreateFromRotationMatrix(ref viewMatrix, out orientation);

            viewProjectionDirty = true;
            frustrumDirty = true;
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
            if (behavior == Behavior.Orbit)
            {
                // Orbiting camera is always positioned relative to the target
                // position. See UpdateViewMatrix().
                return;
            }

            Vector3 forwards;

            if (behavior == Behavior.FirstPerson)
            {
                // Calculate the forwards direction. Can't just use the
                // camera's view direction as doing so will cause the camera to
                // move more slowly as the camera's view approaches 90 degrees
                // straight up and down.
                forwards = Vector3.Normalize(Vector3.Cross(WORLD_Y_AXIS, xAxis));
            }
            else
            {
                forwards = viewDir;
            }

            eye += xAxis * dx;
            eye += WORLD_Y_AXIS * dy;
            eye += forwards * dz;

            viewDirty = true;
            viewProjectionDirty = true;
            frustrumDirty = true;

            UpdateViewMatrix();
        }

        /// <summary>
        /// Moves the camera the specified distance in the specified direction.
        /// </summary>
        /// <param name="direction">Direction to move.</param>
        /// <param name="distance">How far to move.</param>
        public void Move(Vector3 direction, Vector3 distance)
        {
            if (behavior == Behavior.Orbit)
            {
                // Orbiting camera is always positioned relative to the target
                // position. See UpdateViewMatrix().
                return;
            }

            eye.X += direction.X * distance.X;
            eye.Y += direction.Y * distance.Y;
            eye.Z += direction.Z * distance.Z;

            viewDirty = true;
            viewProjectionDirty = true;
            frustrumDirty = true;

            UpdateViewMatrix();
        }

        /// <summary>
        /// Builds a perspective projection matrix based on a horizontal field
        /// of view.
        /// </summary>
        /// <param name="fovx">Horizontal field of view in degrees.</param>
        /// <param name="aspect">The viewport's aspect ratio.</param>
        /// <param name="znear">The distance to the near clip plane.</param>
        /// <param name="zfar">The distance to the far clip plane.</param>
        public void Perspective(float fovx, float aspect, float znear, float zfar)
        {
            this.fovx = fovx;
            this.aspectRatio = aspect;
            this.znear = znear;
            this.zfar = zfar;

            float aspectInv = 1.0f / aspect;
            float e = 1.0f / (float)Math.Tan(fovx / 2.0f);
            float fovy = 2.0f * (float)Math.Atan(aspectInv / e);
            float xScale = 1.0f / (float)Math.Tan(0.5f * fovy);
            float yScale = xScale / aspectInv;

            projectionMatrix.M11 = xScale;
            projectionMatrix.M12 = 0.0f;
            projectionMatrix.M13 = 0.0f;
            projectionMatrix.M14 = 0.0f;

            projectionMatrix.M21 = 0.0f;
            projectionMatrix.M22 = yScale;
            projectionMatrix.M23 = 0.0f;
            projectionMatrix.M24 = 0.0f;

            projectionMatrix.M31 = 0.0f;
            projectionMatrix.M32 = 0.0f;
            projectionMatrix.M33 = (zfar + znear) / (znear - zfar);
            projectionMatrix.M34 = -1.0f;

            projectionMatrix.M41 = 0.0f;
            projectionMatrix.M42 = 0.0f;
            projectionMatrix.M43 = (2.0f * zfar * znear) / (znear - zfar);
            projectionMatrix.M44 = 0.0f;

            viewProjectionDirty = true;
            frustrumDirty = true;
        }

        public void SetAspect(float aspect)
        {
            Perspective(this.fovx, aspect, this.znear, this.zfar);
        }

        public void SetZFar(float zfar)
        {
            Perspective(this.fovx, this.aspectRatio, this.znear, zfar);
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
            heading = -heading;
            pitch = -pitch;
            roll = -roll;

            switch (behavior)
            {
                case Behavior.FirstPerson:
                case Behavior.Spectator:
                    RotateFirstPerson(heading, pitch);
                    break;

                case Behavior.Flight:
                    RotateFlight(heading, pitch, roll);
                    break;

                case Behavior.Orbit:
                    RotateOrbit(heading, pitch, roll);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Undo any camera rolling by leveling the camera. When the camera is
        /// orbiting this method will cause the camera to become level with the
        /// orbit target.
        /// </summary>
        public void UndoRoll()
        {
            if (behavior == Behavior.Orbit)
                LookAt(eye, target, targetYAxis);
            else
                LookAt(eye, eye + ViewDirection, WORLD_Y_AXIS);
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
            if (behavior == Behavior.Orbit)
            {
                Vector3 offset = eye - target;

                orbitOffsetLength = offset.Length();
                offset.Normalize();
                orbitOffsetLength += zoom;
                orbitOffsetLength = Math.Min(Math.Max(orbitOffsetLength, minZoom), maxZoom);
                offset *= orbitOffsetLength;
                eye = offset + target;

                viewDirty = true;
                viewProjectionDirty = true;
                frustrumDirty = true;
            }
            else
            {
                zoom = Math.Min(Math.Max(zoom, minZoom), maxZoom);
                Perspective(zoom, aspectRatio, znear, zfar);

                viewProjectionDirty = true;
                frustrumDirty = true;
            }
            UpdateViewMatrix();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Change to a new camera behavior.
        /// </summary>
        /// <param name="newBehavior">The new camera behavior.</param>
        private void ChangeBehavior(Behavior newBehavior)
        {
            Behavior prevBehavior = behavior;

            if (prevBehavior == newBehavior)
                return;

            behavior = newBehavior;

            switch (newBehavior)
            {
                case Behavior.FirstPerson:
                    switch (prevBehavior)
                    {
                        case Behavior.Flight:
                        case Behavior.Spectator:
                            eye.Y = firstPersonYOffset;

                            viewDirty = true;
                            viewProjectionDirty = true;
                            frustrumDirty = true;
                            break;

                        case Behavior.Orbit:
                            eye.X = savedEye.X;
                            eye.Z = savedEye.Z;
                            eye.Y = firstPersonYOffset;
                            orientation = savedOrientation;
                            accumPitch = savedAccumPitch;

                            viewDirty = true;
                            viewProjectionDirty = true;
                            frustrumDirty = true;
                            break;

                        default:
                            break;
                    }

                    UndoRoll();
                    break;

                case Behavior.Spectator:
                    switch (prevBehavior)
                    {
                        case Behavior.Flight:
                            viewDirty = true;
                            viewProjectionDirty = true;
                            frustrumDirty = true;
                            break;

                        case Behavior.Orbit:
                            eye = savedEye;
                            orientation = savedOrientation;
                            accumPitch = savedAccumPitch;

                            viewDirty = true;
                            viewProjectionDirty = true;
                            frustrumDirty = true;
                            break;

                        default:
                            break;
                    }

                    UndoRoll();
                    break;

                case Behavior.Flight:
                    if (prevBehavior == Behavior.Orbit)
                    {
                        eye = savedEye;
                        orientation = savedOrientation;
                        accumPitch = savedAccumPitch;

                        viewDirty = true;
                        viewProjectionDirty = true;
                        frustrumDirty = true;
                    }
                    else
                    {
                        savedEye = eye;

                        viewDirty = true;
                        viewProjectionDirty = true;
                        frustrumDirty = true;
                    }
                    break;

                case Behavior.Orbit:
                    if (prevBehavior == Behavior.FirstPerson)
                        firstPersonYOffset = eye.Y;

                    savedEye = eye;
                    savedOrientation = orientation;
                    savedAccumPitch = accumPitch;

                    targetYAxis = yAxis;

                    Vector3 newEye = eye + zAxis * orbitOffsetLength;

                    LookAt(newEye, eye, targetYAxis);
                    break;

                default:
                    break;
            }
            UpdateViewMatrix();
        }

        /// <summary>
        /// Sets a new camera orientation.
        /// </summary>
        /// <param name="newOrientation">The new orientation.</param>
        private void ChangeOrientation(Quaternion newOrientation)
        {
            Matrix m = Matrix.CreateFromQuaternion(newOrientation);

            // Store the pitch for this new orientation.
            // First person and spectator behaviors limit pitching to
            // 90 degrees straight up and down.

            float pitch = (float)Math.Asin(m.M23);

            accumPitch = pitch;

            orientation = newOrientation;

            // First person and spectator behaviors don't allow rolling.
            // Negate any rolling that might be encoded in the new orientation.
            if (behavior == Behavior.FirstPerson || behavior == Behavior.Spectator)
            {
                LookAt(eye, eye + Vector3.Negate(zAxis), WORLD_Y_AXIS);
            }
            else
            {
                viewDirty = true;
                viewProjectionDirty = true;
                frustrumDirty = true;
            }
            UpdateViewMatrix();
        }

        private void ChangeEye(Vector3 eye)
        {
            this.eye = eye;

            viewDirty = true;
            viewProjectionDirty = true;
            frustrumDirty = true;

            UpdateViewMatrix();
        }

        /// <summary>
        /// Rotates the camera for first person and spectator behaviors.
        /// Pitching is limited to 90 degrees straight up and down.
        /// </summary>
        /// <param name="heading">Y axis rotation angle.</param>
        /// <param name="pitch">X axis rotation angle.</param>
        private void RotateFirstPerson(float heading, float pitch)
        {
            accumPitch += pitch;

            if (accumPitch > MathHelper.PiOver2)
            {
                pitch = MathHelper.PiOver2 - (accumPitch - pitch);
                accumPitch = MathHelper.PiOver2;
            }

            if (accumPitch < -MathHelper.PiOver2)
            {
                pitch = -MathHelper.PiOver2 - (accumPitch - pitch);
                accumPitch = -MathHelper.PiOver2;
            }

            //float heading = MathHelper.ToRadians(heading);
            //float pitch = MathHelper.ToRadians(pitch);
            Quaternion rotation = Quaternion.Identity;

            // Rotate the camera about the world Y axis.
            if (heading != 0.0f)
            {
                Quaternion.CreateFromAxisAngle(ref WORLD_Y_AXIS, heading, out rotation);
                Quaternion.Concatenate(ref rotation, ref orientation, out orientation);

                viewDirty = true;
                viewProjectionDirty = true;
                frustrumDirty = true;
            }

            // Rotate the camera about its local X axis.
            if (pitch != 0.0f)
            {
                Quaternion.CreateFromAxisAngle(ref WORLD_X_AXIS, pitch, out rotation);
                Quaternion.Concatenate(ref orientation, ref rotation, out orientation);

                viewDirty = true;
                viewProjectionDirty = true;
                frustrumDirty = true;
            }
            UpdateViewMatrix();
        }

        /// <summary>
        /// Rotates the camera for flight behavior.
        /// </summary>
        /// <param name="heading">Y axis rotation angle.</param>
        /// <param name="pitch">X axis rotation angle.</param>
        /// <param name="roll">Z axis rotation angle.</param>
        private void RotateFlight(float heading, float pitch, float roll)
        {
            accumPitch += pitch;

            if (accumPitch > 2f * MathHelper.Pi)
                accumPitch -= 2f * MathHelper.Pi;

            if (accumPitch < -2f * MathHelper.Pi)
                accumPitch += 2f * MathHelper.Pi;

            //float heading = MathHelper.ToRadians(heading);
            //float pitch = MathHelper.ToRadians(pitch);
            //float roll = MathHelper.ToRadians(roll);

            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(heading, pitch, roll);
            Quaternion.Concatenate(ref orientation, ref rotation, out orientation);

            viewDirty = true;
            viewProjectionDirty = true;
            frustrumDirty = true;

            UpdateViewMatrix();
        }

        /// <summary>
        /// Rotates the camera for orbit behavior. Rotations are either about
        /// the camera's local y axis or the orbit target's y axis. The property
        /// PreferTargetYAxisOrbiting controls which rotation method to use.
        /// </summary>
        /// <param name="heading">Y axis rotation angle.</param>
        /// <param name="pitch">X axis rotation angle.</param>
        /// <param name="roll">Z axis rotation angle.</param>
        private void RotateOrbit(float heading, float pitch, float roll)
        {
            //float heading = MathHelper.ToRadians(heading);
            //float pitch = MathHelper.ToRadians(pitch);

            if (preferTargetYAxisOrbiting)
            {
                Quaternion rotation = Quaternion.Identity;

                if (heading != 0.0f)
                {
                    Quaternion.CreateFromAxisAngle(ref targetYAxis, heading, out rotation);
                    Quaternion.Concatenate(ref rotation, ref orientation, out orientation);

                    viewDirty = true;
                    viewProjectionDirty = true;
                    frustrumDirty = true;
                }

                if (pitch != 0.0f)
                {
                    Quaternion.CreateFromAxisAngle(ref WORLD_X_AXIS, pitch, out rotation);
                    Quaternion.Concatenate(ref orientation, ref rotation, out orientation);

                    viewDirty = true;
                    viewProjectionDirty = true;
                    frustrumDirty = true;
                }
            }
            else
            {
                //float roll = MathHelper.ToRadians(roll);
                Quaternion rotation = Quaternion.CreateFromYawPitchRoll(heading, pitch, roll);
                Quaternion.Concatenate(ref orientation, ref rotation, out orientation);

                viewDirty = true;
                viewProjectionDirty = true;
                frustrumDirty = true;
            }
            UpdateViewMatrix();
        }

        /// <summary>
        /// Rebuild the view matrix.
        /// </summary>
        private void UpdateViewMatrix()
        {
            if (!viewDirty)
            {
                return;
            }
            Matrix.CreateFromQuaternion(ref orientation, out viewMatrix);

            xAxis.X = viewMatrix.M11;
            xAxis.Y = viewMatrix.M21;
            xAxis.Z = viewMatrix.M31;

            yAxis.X = viewMatrix.M12;
            yAxis.Y = viewMatrix.M22;
            yAxis.Z = viewMatrix.M32;

            zAxis.X = viewMatrix.M13;
            zAxis.Y = viewMatrix.M23;
            zAxis.Z = viewMatrix.M33;

            if (behavior == Behavior.Orbit)
            {
                // Calculate the new camera position based on the current
                // orientation. The camera must always maintain the same
                // distance from the target. Use the current offset vector
                // to determine the correct distance from the target.

                eye = target + zAxis * orbitOffsetLength;
            }

            viewMatrix.M41 = -Vector3.Dot(xAxis, eye);
            viewMatrix.M42 = -Vector3.Dot(yAxis, eye);
            viewMatrix.M43 = -Vector3.Dot(zAxis, eye);

            viewDir.X = -zAxis.X;
            viewDir.Y = -zAxis.Y;
            viewDir.Z = -zAxis.Z;

            viewDirty = false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Property to get and set the camera's behavior.
        /// </summary>
        public Behavior CurrentBehavior
        {
            get { return behavior; }
            set { ChangeBehavior(value); }
        }

        /// <summary>
        /// Property to get and set the max orbit zoom distance.
        /// </summary>
        public float OrbitMaxZoom
        {
            get { return orbitMaxZoom; }
            set { orbitMaxZoom = value; }
        }

        /// <summary>
        /// Property to get and set the min orbit zoom distance.
        /// </summary>
        public float OrbitMinZoom
        {
            get { return orbitMinZoom; }
            set { orbitMinZoom = value; }
        }

        /// <summary>
        /// Property to get and set the distance from the target when orbiting.
        /// </summary>
        public float OrbitOffsetDistance
        {
            get { return orbitOffsetLength; }
            set { orbitOffsetLength = value; }
        }

        /// <summary>
        /// Property to get and set the camera orbit target position.
        /// </summary>
        public Vector3 OrbitTarget
        {
            get { return target; }
            set { target = value; }
        }

        /// <summary>
        /// Property to get and set the camera orientation.
        /// </summary>
        public Quaternion Orientation
        {
            get { return orientation; }
            set { ChangeOrientation(value); }
        }

        /// <summary>
        /// Property to get and set the camera position.
        /// </summary>
        public Vector3 Position
        {
            get { return eye; }
            set { ChangeEye(eye); }
        }

        /// <summary>
        /// Property to get the viewing direction vector.
        /// </summary>
        public Vector3 ViewDirection
        {
            get
            {
                if (viewDirty)
                {
                    UpdateViewMatrix();
                }
                return viewDir;
            }
        }

        /// <summary>
        /// Property to get and set the flag to force the camera
        /// to orbit around the orbit target's Y axis rather than the camera's
        /// local Y axis.
        /// </summary>
        public bool PreferTargetYAxisOrbiting
        {
            get { return preferTargetYAxisOrbiting; }

            set
            {
                preferTargetYAxisOrbiting = value;

                if (preferTargetYAxisOrbiting)
                    UndoRoll();
            }
        }

        /// <summary>
        /// Property to get the perspective projection matrix.
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get { return projectionMatrix; }
        }

        /// <summary>
        /// Property to get the view matrix.
        /// </summary>
        public Matrix ViewMatrix
        {
            get
            {
                if (viewDirty)
                {
                    UpdateViewMatrix();
                }
                return viewMatrix;
            }
        }

        /// <summary>
        /// Property to get the concatenated view-projection matrix.
        /// </summary>
        public Matrix ViewProjectionMatrix
        {
            get
            {
                if (viewProjectionDirty)
                {
                    viewProjectionDirty = false;
                    viewProjectionMatrix = viewMatrix * projectionMatrix;
                    invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
                }
                return viewProjectionMatrix;
            }
        }

        /// <summary>
        /// Property to get the concatenated view-projection matrix.
        /// </summary>
        public Matrix InverseViewProjectionMatrix
        {
            get
            {
                if (viewProjectionDirty)
                {
                    viewProjectionDirty = false;
                    viewProjectionMatrix = viewMatrix * projectionMatrix;
                    invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
                }
                return invViewProjectionMatrix;
            }
        }

        /// <summary>
        /// Property to get the camera's local X axis.
        /// </summary>
        public Vector3 XAxis
        {
            get
            {
                if (viewDirty)
                {
                    UpdateViewMatrix();
                }
                return xAxis;
            }
        }

        /// <summary>
        /// Property to get the camera's local Y axis.
        /// </summary>
        public Vector3 YAxis
        {
            get
            {
                if (viewDirty)
                {
                    UpdateViewMatrix();
                }
                return yAxis;
            }
        }

        /// <summary>
        /// Property to get the camera's local Z axis.
        /// </summary>
        public Vector3 ZAxis
        {
            get
            {
                if (viewDirty)
                {
                    UpdateViewMatrix();
                }
                return zAxis;
            }
        }

        public BoundingFrustum BoundingFrustum
        {
            get
            {
                if (frustrumDirty)
                {
                    frustrumDirty = false;
                    boundingFrustum = new BoundingFrustum(ViewProjectionMatrix);
                }
                return boundingFrustum;
            }
        }

        #endregion
    }

}
