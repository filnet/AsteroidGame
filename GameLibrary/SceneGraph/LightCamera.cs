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

        private Bounding.BoundingBox lightBoundingBox;

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

        public void FitToView(BoundingFrustum cameraBoundingFrustum, float nearPlaneOffset)
        {
            // matrix that will rotate in the direction of the light
            Vector3 lightUpVector = Vector3.Up;
            // avoid case when camera up and light dir are //
            /*if (Math.Abs(Vector3.Dot(lightUpVector, lightDirection)) > 0.9f)
            {
                lightUpVector = Vector3.Forward;
            }*/
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero, -lightDirection, lightUpVector);

            // transform view Frustum corners to light space
            Vector3[] corners = new Vector3[BoundingFrustum.CornerCount];
            cameraBoundingFrustum.GetCorners(corners);
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3.Transform(ref corners[c], ref lightRotation, out corners[c]);
            }
            lightBoundingBox.ComputeFromPoints(corners);

            if (nearPlaneOffset != 0)
            {
                // apply near plane offset
                Vector3 min = lightBoundingBox.Center - lightBoundingBox.HalfSize;
                Vector3 max = lightBoundingBox.Center + lightBoundingBox.HalfSize;
                min.Z = min.Z - nearPlaneOffset;
                lightBoundingBox = Bounding.BoundingBox.CreateFromMinMax(min, max);
            }

            // light position is in the middle of the bounding box
            // actual position is not relevant for directional lights
            lightPosition = lightBoundingBox.Center;

            Vector2 worldUnitsPerPixel = new Vector2(lightBoundingBox.HalfSize.X / 2048, lightBoundingBox.HalfSize.Y / 2048) * 2.0f;
            lightPosition.X -= (float)Math.IEEERemainder(lightPosition.X, worldUnitsPerPixel.X);
            lightPosition.Y -= (float)Math.IEEERemainder(lightPosition.Y, worldUnitsPerPixel.Y);

            // convert light position back into world space
            Matrix invLightRotation = Matrix.Invert(lightRotation);
            Vector3.Transform(ref lightPosition, ref invLightRotation, out lightPosition);

            // create light view matrix
            viewMatrix = Matrix.CreateLookAt(lightPosition, lightPosition + lightDirection, lightUpVector);

            // create light projection matrix
            float zNear = -(lightBoundingBox.HalfSize.Z /*+ nearBias*/);
            projectionMatrix = Matrix.CreateOrthographic(
                lightBoundingBox.HalfSize.X * 2, lightBoundingBox.HalfSize.Y * 2, zNear, lightBoundingBox.HalfSize.Z);

            viewProjectionMatrix = viewMatrix * projectionMatrix;
            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

        // see https://www.gamedev.net/forums/topic/591684-xna-40---shimmering-shadow-maps/
        public void FitToViewStable(BoundingFrustum cameraBoundingFrustum, float nearPlaneOffset)
        {
            // matrix that will rotate in the direction of the light
            Vector3 lightUpVector = Vector3.Up;
            // avoid case when camera up and light dir are //
            /*if (Math.Abs(Vector3.Dot(lightUpVector, lightDirection)) > 0.9f)
            {
                lightUpVector = Vector3.Forward;
            }*/
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero, -lightDirection, lightUpVector);

            // get camera frustum corners
            Vector3[] frustumCornersWS = new Vector3[8];
            cameraBoundingFrustum.GetCorners(frustumCornersWS);

            // get length of largest diagonal
            float diagonalLength = (frustumCornersWS[0] - frustumCornersWS[6]).Length();
            float radius = diagonalLength / 2.0f;

            // compute frustrum centroid
            //Vector3 center = (frustumCornersWS[0] + frustumCornersWS[6] + frustumCornersWS[3] + frustumCornersWS[5]) / 4.0f;
            Vector3 center = frustumCornersWS[0];
            for (int i = 1; i < frustumCornersWS.Length; i++)
            {
                center += frustumCornersWS[i];
            }
            center /= 8;

            // TODO need Z in view space ...
            //float nearClip = frustumCornersWS[0].Z;
            //float farClip = frustumCornersWS[6].Z;
            // for now use radius (not as tight as possible... can do better)
            float nearClip = -(radius + nearPlaneOffset);
            float farClip = radius;


            // TODO frustrum/bounds/etc... rendering : use RasterizerState.DepthClipEnable = false; in renderer
            // so we see them even if should be Z clipped (ok for far, but what about near...)

            if (true)
            {
                // convert center position into light space
                Vector3.Transform(ref center, ref lightRotation, out center);

                // discretize center position
                // don't forget to increase the size of the ortho bounds so both light and camera frustums can slide!
                // FIXME is it to be done in light space ?
                Vector2 worldUnitsPerPixel = new Vector2(diagonalLength / 2048);
                center.X -= (float)Math.IEEERemainder(center.X, worldUnitsPerPixel.X);
                center.Y -= (float)Math.IEEERemainder(center.Y, worldUnitsPerPixel.Y);
                // FIXME is it necessary for Z too ?
                //center.Z -= (float)Math.IEEERemainder(center.Z, worldUnitsPerPixel.Z);

                // convert center position back into world space
                Matrix invLightRotation = Matrix.Invert(lightRotation);
                Vector3.Transform(ref center, ref invLightRotation, out center);
            }

            // create light view matrix
            lightPosition = center;// - lightDirection * backupDist;
            viewMatrix = Matrix.CreateLookAt(lightPosition, lightPosition + lightDirection, lightUpVector);

            // create light projection matrix
            float bounds = diagonalLength;
            if (true)
            {
                // increase bounds to account for position discretization
                // the light frustum position moves in worldUnitsPerPixel steps (discretized)
                // so we need some leeway so the light and camera frustums can slide
                bounds *= 2048 / (2048 - 1);
                // FIXME bounds += worldUnitsPerPixel.Length(); ???
                // or bounds += Math.Max(worldUnitsPerPixel.X, worldUnitsPerPixel.Y);
                // or bounds.X += worldUnitsPerPixel.X ???
                //Vector2 worldUnitsPerPixel = new Vector2(diagonalLength / 2048);
                //bounds += worldUnitsPerPixel.X;
            }

            //float backupDist = nearPlaneOffset + nearClip + radius;
            //nearClip = 0;
            //farClip = backupDist + radius;
            projectionMatrix = Matrix.CreateOrthographic(bounds, bounds, nearClip, farClip);

            // compute derived values
            viewProjectionMatrix = viewMatrix * projectionMatrix;
            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

        public void FitToScene(BoundingFrustum cameraBoundingFrustum, Bounding.BoundingBox sceneBoundingBox, float nearPlaneOffset)
        {
            // matrix that will rotate in the direction of the light
            Vector3 lightUpVector = Vector3.Up;
            // avoid case when camera up and light dir are //
            /*if (Math.Abs(Vector3.Dot(lightUpVector, lightDirection)) > 0.9f)
            {
                lightUpVector = Vector3.Forward;
            }*/
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero, -lightDirection, lightUpVector);

            // transform scene bounding box to light space
            Bounding.BoundingBox sceneBoundingBoxLS = new Bounding.BoundingBox();
            Bounding.BoundingVolume boundingVolume = sceneBoundingBoxLS as Bounding.BoundingVolume;
            sceneBoundingBox.Transform(lightRotation, ref boundingVolume);

            // transform view Frustum corners to light space
            Vector3[] corners = new Vector3[BoundingFrustum.CornerCount];
            cameraBoundingFrustum.GetCorners(corners);
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3.Transform(ref corners[c], ref lightRotation, out corners[c]);
            }
            lightBoundingBox.ComputeFromPoints(corners);

            // take minimum of view bounding box and light bounding box
            Vector3 min;
            Vector3 max;
            min = Vector3.Max(lightBoundingBox.Center - lightBoundingBox.HalfSize, sceneBoundingBoxLS.Center - sceneBoundingBoxLS.HalfSize);
            max = Vector3.Min(lightBoundingBox.Center + lightBoundingBox.HalfSize, sceneBoundingBoxLS.Center + sceneBoundingBoxLS.HalfSize);
            lightBoundingBox = Bounding.BoundingBox.CreateFromMinMax(min, max);

            if (nearPlaneOffset != 0)
            {
                // apply near plane offset
                min = lightBoundingBox.Center - lightBoundingBox.HalfSize;
                max = lightBoundingBox.Center + lightBoundingBox.HalfSize;
                min.Z = min.Z - nearPlaneOffset;
                lightBoundingBox = Bounding.BoundingBox.CreateFromMinMax(min, max);
            }

            // light position is in the middle of the bounding box
            // actual position is not relevant for directional lights
            Vector3 lightPosition = lightBoundingBox.Center;

            // convert light position back into world space
            Matrix invLightRotation = Matrix.Invert(lightRotation);
            Vector3.Transform(ref lightPosition, ref invLightRotation, out lightPosition);

            // create light view matrix
            viewMatrix = Matrix.CreateLookAt(lightPosition, lightPosition + lightDirection, lightUpVector);

            // create light projection matrix
            float zNear = -(lightBoundingBox.HalfSize.Z /*+ nearBias*/);
            projectionMatrix = Matrix.CreateOrthographic(
                lightBoundingBox.HalfSize.X * 2, lightBoundingBox.HalfSize.Y * 2, zNear, lightBoundingBox.HalfSize.Z);

            viewProjectionMatrix = viewMatrix * projectionMatrix;
            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

    }
}
