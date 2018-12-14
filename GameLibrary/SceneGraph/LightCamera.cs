using GameLibrary.Component.Camera;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.SceneGraph
{
    public class LightCamera : AbstractCamera
    {
        public override Vector3 Position { get { return lightPosition; } set => throw new NotImplementedException(); }

        public override Vector3 ViewDirection { get { return lightDirection; } }

        public override Matrix ProjectionMatrix { get { return projectionMatrix; } }

        public override Matrix ViewMatrix { get { return viewMatrix; } }

        public override Matrix ViewProjectionMatrix { get { return viewProjectionMatrix; } }

        public override Matrix InverseViewProjectionMatrix { get { return invViewProjectionMatrix; } }

        public override BoundingFrustum BoundingFrustum { get { return boundingFrustum; } }

        private Vector3 lightPosition;
        private Vector3 lightDirection;

        private Matrix viewMatrix;
        private Matrix projectionMatrix;
        private Matrix viewProjectionMatrix;

        private Matrix invViewProjectionMatrix;

        private BoundingFrustum boundingFrustum;

        public LightCamera() : base()
        {
        }

        public void Update(Bounding.BoundingBox sceneBoundingBox, bool expandNearZ)
        {
            // Think of light's orthographic frustum as a bounding box that encloses all objects visible by the camera,
            // plus objects not visible but potentially casting shadows. For the simplicity let's disregard the latter.
            // So to find this frustum:
            // - find all objects that are inside the current camera frustum
            // - find minimal aa bounding box that encloses them all
            // - transform corners of that bounding box to the light's space (using light's view matrix)
            // - find aa bounding box in light's space of the transformed (now obb) bounding box
            // - this aa bounding box is your directional light's orthographic frustum.
            //
            // Note that actual translation component in light view matrix doesn't really matter as you'll
            // only get different Z values for the frustum but the boundaries will be the same in world space.
            // For the convenience, when building light view matrix, you can assume the light "position" is at
            // the center of the bounding box enclosing all visible objects.

            lightPosition = sceneBoundingBox.Center;
            lightDirection = Vector3.Normalize(new Vector3(-1, -1, -1));

            viewMatrix = Matrix.CreateLookAt(lightPosition, lightPosition + lightDirection, Vector3.Up);

            // transform bounding box
            ref Matrix m = ref viewMatrix;

            //Vector3 newCenter;
            //Vector3 v = sceneBoundingBox.Center;
            //newCenter.X = (v.X * m.M11) + (v.Y * m.M21) + (v.Z * m.M31) + m.M41;
            //newCenter.Y = (v.X * m.M12) + (v.Y * m.M22) + (v.Z * m.M32) + m.M42;
            //newCenter.Z = (v.X * m.M13) + (v.Y * m.M23) + (v.Z * m.M33) + m.M43;

            Vector3 sceneHalfSize;
            Vector3 v = sceneBoundingBox.HalfSize;
            sceneHalfSize.X = (v.X * Math.Abs(m.M11)) + (v.Y * Math.Abs(m.M21)) + (v.Z * Math.Abs(m.M31));
            sceneHalfSize.Y = (v.X * Math.Abs(m.M12)) + (v.Y * Math.Abs(m.M22)) + (v.Z * Math.Abs(m.M32));
            sceneHalfSize.Z = (v.X * Math.Abs(m.M13)) + (v.Y * Math.Abs(m.M23)) + (v.Z * Math.Abs(m.M33));

            //Bounding.BoundingBox bb = new Bounding.BoundingBox(newCenter, newHalfSize);

            // move near plane to "minus infinity" to account for all occluders
            float zNearPlane = expandNearZ ? - 1000 : -sceneHalfSize.Z;
            projectionMatrix = Matrix.CreateOrthographic(sceneHalfSize.X * 2, sceneHalfSize.Y * 2, zNearPlane, sceneHalfSize.Z);

            viewProjectionMatrix = viewMatrix * projectionMatrix;

            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);

            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

        public void UpdateZNear(Bounding.BoundingBox sceneBoundingBox, Bounding.BoundingBox occluderBoundingBox)
        {
            lightPosition = sceneBoundingBox.Center;
            lightDirection = Vector3.Normalize(new Vector3(-1, -1, -1));

            viewMatrix = Matrix.CreateLookAt(lightPosition, lightPosition + lightDirection, Vector3.Up);

            // transform bounding box
            ref Matrix m = ref viewMatrix;

            Vector3 sceneHalfSize;
            Vector3 v = sceneBoundingBox.HalfSize;
            sceneHalfSize.X = (v.X * Math.Abs(m.M11)) + (v.Y * Math.Abs(m.M21)) + (v.Z * Math.Abs(m.M31));
            sceneHalfSize.Y = (v.X * Math.Abs(m.M12)) + (v.Y * Math.Abs(m.M22)) + (v.Z * Math.Abs(m.M32));
            sceneHalfSize.Z = (v.X * Math.Abs(m.M13)) + (v.Y * Math.Abs(m.M23)) + (v.Z * Math.Abs(m.M33));

            Vector3 occluderHalfSize;
            v = occluderBoundingBox.HalfSize;
            occluderHalfSize.X = (v.X * Math.Abs(m.M11)) + (v.Y * Math.Abs(m.M21)) + (v.Z * Math.Abs(m.M31));
            occluderHalfSize.Y = (v.X * Math.Abs(m.M12)) + (v.Y * Math.Abs(m.M22)) + (v.Z * Math.Abs(m.M32));
            occluderHalfSize.Z = (v.X * Math.Abs(m.M13)) + (v.Y * Math.Abs(m.M23)) + (v.Z * Math.Abs(m.M33));

            projectionMatrix = Matrix.CreateOrthographic(sceneHalfSize.X * 2, sceneHalfSize.Y * 2, -occluderHalfSize.Z, sceneHalfSize.Z);

            viewProjectionMatrix = viewMatrix * projectionMatrix;

            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);

            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

    }
}
