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
        public Vector3 lightDirection;

        private Matrix viewMatrix;
        private Matrix projectionMatrix;
        private Matrix viewProjectionMatrix;

        private Matrix invViewProjectionMatrix;

        private BoundingFrustum boundingFrustum;

        private readonly Bounding.BoundingBox lightBoundingBox;


        public LightCamera() : base()
        {
            lightBoundingBox = new Bounding.BoundingBox();
        }



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

        // The basic process for a directional light (whose rays are parallel) is as follows:
        // - Calculate the 8 corners of the view frustum in world space.
        //   This can be done by using the inverse view-projection matrix to transform the 8 corners of the NDC cube (which in OpenGL is [‒1, 1] along each axis).
        // - Transform the frustum corners to a space aligned with the shadow map axes.
        //   This would commonly be the directional light object's local space.
        //   (In fact, steps 1 and 2 can be done in one step by combining the inverse view-projection matrix of the camera with the inverse world matrix of the light.)
        // - Calculate the bounding box of the transformed frustum corners. This will be the view frustum for the shadow map.
        // - Pass the bounding box's extents to glOrtho or similar to set up the orthographic projection matrix for the shadow map.
        //
        // There are a couple caveats with this basic approach.
        // First, the Z bounds for the shadow map will be tightly fit around the view frustum, which means that objects outside the view frustum,
        // but between the view frustum and the light, may fall outside the shadow frustum. This could lead to missing shadows.
        // To fix this, depth clamping can be enabled so that objects in front of the shadow frustum will be rendered with clamped Z instead of clipped.
        // Alternatively, the Z-near of the shadow frustum can be pushed out to ensure any possible shadowers are included.
        //
        // The bigger issue is that this produces a shadow frustum that continuously changes size and position as the camera moves around.
        // This leads to shadows "swimming", which is a very distracting artifact.
        // In order to fix this, it's common to do the following additional two steps:
        // - Fix the overall size of the frustum based on the longest diagonal of the camera frustum.
        //   This ensures that the camera frustum can fit into the shadow frustum in any orientation.
        //   Don't allow the shadow frustum to change size as the camera rotates.
        // - Discretize the position of the frustum, based on the size of texels in the shadow map.
        //   In other words, if the shadow map is 1024×1024, then you only allow the frustum to move around in discrete steps of 1/1024th of the frustum size.
        //   (You also need to increase the size of the frustum by a factor of 1024/1023, to give room for the shadow frustum and view frustum to slip against each other.)
        //
        // If you do these, the shadow will remain rock solid in world space as the camera moves around.
        // (It won't remain solid if the camera's FOV, near or far planes are changed, though.)
        //
        // As a bonus, if you do all the above, you're well on your way to implementing cascaded shadow maps,
        // which are "just" a set of shadow maps calculated from the view frustum as above,
        // but using different view frustum near and far plane values to place each shadow map.


        public void FitToScene(BoundingFrustum cameraBoundingFrustrum, float nearBias)
        {
            // Matrix with that will rotate in the direction of the light
            Vector3 lightUpVector = Vector3.Up;
            // avoid case when camera up and light dir are //
            /*if (Math.Abs(Vector3.Dot(lightUpVector, lightDirection)) > 0.9f)
            {
                lightUpVector = Vector3.Forward;
            }*/
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero, -lightDirection, lightUpVector);

            // Transform corners to light space
            Vector3[] corners = new Vector3[BoundingFrustum.CornerCount];
            cameraBoundingFrustrum.GetCorners(corners);
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3.Transform(ref corners[c], ref lightRotation, out corners[c]);
            }
            lightBoundingBox.ComputeFromPoints(corners);

            // light position is in the middle of the bounding box
            // actual position is not relevant for directional lights
            Vector3 lightPosition = lightBoundingBox.Center;

            // convert light position back into world space
            Matrix invLightRotation = Matrix.Invert(lightRotation);
            Vector3.Transform(ref lightPosition, ref invLightRotation, out lightPosition);

            // create light view matrix
            viewMatrix = Matrix.CreateLookAt(lightPosition, lightPosition + lightDirection, lightUpVector);

            // create light projection matrix
            float zNear = -(lightBoundingBox.HalfSize.Z + nearBias);
            projectionMatrix = Matrix.CreateOrthographic(
                lightBoundingBox.HalfSize.X * 2, lightBoundingBox.HalfSize.Y * 2, zNear, lightBoundingBox.HalfSize.Z);

            viewProjectionMatrix = viewMatrix * projectionMatrix;
            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

        public void UpdateZNear(Bounding.BoundingBox sceneBoundingBox)
        {
            Vector3 lightUpVector = Vector3.Up;
            // avoid case when camera up and light dir are //
            /*if (Math.Abs(Vector3.Dot(lightUpVector, lightDirection)) > 0.9f)
            {
                lightUpVector = Vector3.Forward;
            }*/
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero, -lightDirection, lightUpVector);

            // Transform to light space
            Bounding.BoundingBox sceneBoundingBoxLS = new Bounding.BoundingBox();
            Bounding.BoundingVolume boundingVolume = sceneBoundingBoxLS as Bounding.BoundingVolume;
            sceneBoundingBox.Transform(lightRotation, ref boundingVolume);

            // create light projection matrix
            float zNear = sceneBoundingBoxLS.Center.Z - sceneBoundingBoxLS.HalfSize.Z;
            projectionMatrix = Matrix.CreateOrthographic(
                lightBoundingBox.HalfSize.X * 2, lightBoundingBox.HalfSize.Y * 2, zNear, lightBoundingBox.HalfSize.Z);

            viewProjectionMatrix = viewMatrix * projectionMatrix;
            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

        public void Update(Bounding.BoundingBox sceneBoundingBox, bool expand)
        {
            lightPosition = sceneBoundingBox.Center;
            //lightDirection = Vector3.Normalize(new Vector3(-1, -1, -1));

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

            float zNearPlane = -sceneHalfSize.Z;
            float zFarPlane = sceneHalfSize.Z;
            if (expand)
            {
                // move near plane to "minus infinity" to account for all occluders
                //zFarPlane = zFarPlane - (zFarPlane - zNearPlane) * 2; 
                //zFarPlane = zNearPlane;
                zNearPlane = -1000;
            }
            projectionMatrix = Matrix.CreateOrthographic(sceneHalfSize.X * 2, sceneHalfSize.Y * 2, zNearPlane, zFarPlane);

            viewProjectionMatrix = viewMatrix * projectionMatrix;

            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);

            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

    }
}
