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

        private Bounding.BoundingBox frustumBoundingBoxLS;

        public Bounding.BoundingSphere bs;

        public LightCamera() : base()
        {
            frustumBoundingBoxLS = new Bounding.BoundingBox();
            bs = new Bounding.BoundingSphere();
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

        public void FitToView(BoundingFrustum cameraBoundingFrustum, float nearClipOffset)
        {
            // matrix that will rotate in the direction of the light
            Vector3 lightUpVector = Vector3.Up;
            // avoid case when camera up and light dir are //
            /*if (Math.Abs(Vector3.Dot(lightUpVector, lightDirection)) > 0.9f)
            {
                lightUpVector = Vector3.Forward;
            }*/
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero, lightDirection, lightUpVector);

            // transform view Frustum corners to light space
            Vector3[] corners = new Vector3[BoundingFrustum.CornerCount];
            cameraBoundingFrustum.GetCorners(corners);
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3.Transform(ref corners[c], ref lightRotation, out corners[c]);
            }
            frustumBoundingBoxLS.ComputeFromPoints(corners);

            // light position is in the middle of the bounding box
            // actual position is not relevant for directional lights
            lightPosition = frustumBoundingBoxLS.Center;

            // convert light position back into world space
            Matrix invLightRotation = Matrix.Invert(lightRotation);
            Vector3.Transform(ref lightPosition, ref invLightRotation, out lightPosition);

            // create light view matrix
            viewMatrix = Matrix.CreateLookAt(lightPosition, lightPosition + lightDirection, lightUpVector);

            // create light projection matrix
            float nearClip = -frustumBoundingBoxLS.HalfSize.Z - nearClipOffset;
            float farClip = frustumBoundingBoxLS.HalfSize.Z;
            projectionMatrix = Matrix.CreateOrthographic(
                frustumBoundingBoxLS.HalfSize.X * 2, frustumBoundingBoxLS.HalfSize.Y * 2, nearClip, farClip);

            viewProjectionMatrix = viewMatrix * projectionMatrix;
            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

        // see https://www.gamedev.net/forums/topic/591684-xna-40---shimmering-shadow-maps/
        public void FitToViewStable(RenderContext renderContext, float nearClipOffset)
        {
            int shadowMapSize = 2048;

            // matrix that will rotate in the direction of the light
            Vector3 lightUpVector = Vector3.Up;
            // avoid case when camera up and light dir are //
            /*if (Math.Abs(Vector3.Dot(lightUpVector, lightDirection)) > 0.9f)
            {
                lightUpVector = Vector3.Forward;
            }*/

            // TODO
            // 
            // use RasterizerState.SlopeScaleDepthBias when rendering shadow map ?
            // or do it ourself ?
            // 
            // why does it seem that changing SamplerState (Point filtering, etc...=
            // has no effect (FIXED was using wrong texture index...)
            //
            // set clip planes when rendering shadows to rasterize only needed pixels in the shadow map
            // RasterizerState.ScissorTestEnable = ?;
            // 
            // TODO
            // TODO


            // TODO frustrum/bounds/etc... rendering : use RasterizerState.DepthClipEnable = false; in renderer
            // so we see them even if should be Z clipped (ok for far, but what about near...)

            Bounding.BoundingSphere frustrumBoundingSphere = renderContext.Camera.BoundingSphere;

            Vector3 center = frustrumBoundingSphere.Center;
            float radius = frustrumBoundingSphere.Radius;

            float bounds = 2 * radius;
            bool stable = true;
            if (stable)
            {
                Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero, lightDirection, lightUpVector);

                // convert center position into light space
                Vector3.Transform(ref center, ref lightRotation, out center);
                Matrix invLightRotation = Matrix.Invert(lightRotation);

                // discretize center position
                // don't forget to increase the size of the ortho bounds so both light and camera frustums can slide!
                // TODO apply directly to transformation matrix (removes the need to invert light rotation and convert light position back in WS)

                bounds *= shadowMapSize / (shadowMapSize - 1);

                Vector2 worldUnitsPerPixel = new Vector2(bounds / shadowMapSize);
                center.X -= (float)Math.IEEERemainder(center.X, worldUnitsPerPixel.X);
                center.Y -= (float)Math.IEEERemainder(center.Y, worldUnitsPerPixel.Y);
                // FIXME is it necessary for Z too ?
                //center.Z -= (float)Math.IEEERemainder(center.Z, worldUnitsPerPixel.Z);

                // convert center position back into world space
                Vector3.Transform(ref center, ref invLightRotation, out center);
            }

            // create light view matrix
            lightPosition = center - (float)radius * lightDirection;
            viewMatrix = Matrix.CreateLookAt(lightPosition, center, lightUpVector);

            float nearClip;
            float farClip;
            bool fitToView = true;
            bool fitToScene = false;
            if (fitToView)
            {
                // get camera frustum corners
                // FIXME : garbage...
                Vector3[] frustumCornersWS = new Vector3[BoundingFrustum.CornerCount];
                renderContext.Camera.BoundingFrustum.GetCorners(frustumCornersWS);

                // transform view frustum corners to light space
                // FIXME we are only interested in the Z component
                Vector3[] frustumCornersLS = new Vector3[BoundingFrustum.CornerCount];
                float minZ = float.MaxValue;
                float maxZ = float.MinValue;
                for (int c = 0; c < frustumCornersWS.Length; c++)
                {
                    Vector3.Transform(ref frustumCornersWS[c], ref viewMatrix, out frustumCornersLS[c]);
                    float z = frustumCornersLS[c].Z;
                    minZ = Math.Min(minZ, z);
                    maxZ = Math.Max(maxZ, z);
                }

                // fit Z to scene ?
                if (fitToScene)
                {
                    // transform scene bounding box to light space
                    // FIXME we are only interested in the Z component
                    Bounding.BoundingBox sceneBoundingBoxLS = new Bounding.BoundingBox();
                    //sceneBoundingBox.Transform(viewMatrix, sceneBoundingBoxLS);

                    minZ = Math.Max(minZ, sceneBoundingBoxLS.Center.Z - sceneBoundingBoxLS.HalfSize.Z);
                    maxZ = Math.Min(maxZ, sceneBoundingBoxLS.Center.Z + sceneBoundingBoxLS.HalfSize.Z);
                }

                // ???
                nearClip = -maxZ;
                farClip = -minZ;
            }
            else
            {
                nearClip = 0;
                farClip = 2 * radius;
            }
            //Console.WriteLine(nearClip + " / " + farClip + " (" + radius + ")");

            // create light projection matrix
            if (stable)
            {
                // increase bounds to account for position discretization
                // the light frustum position moves in worldUnitsPerPixel steps (discretized)
                // so we need some leeway so the light and camera frustums can slide
                //bounds *= shadowMapSize / (shadowMapSize - 1);

                //bounds = (float)Math.Ceiling(bounds);
                // FIXME bounds += worldUnitsPerPixel.Length(); ???
                // or bounds += Math.Max(worldUnitsPerPixel.X, worldUnitsPerPixel.Y);
                // or bounds.X += worldUnitsPerPixel.X ???
                //Vector2 worldUnitsPerPixel = new Vector2(diagonalLength / 2048);
                //bounds += worldUnitsPerPixel.X;
            }

            // create light projection matrix
            projectionMatrix = Matrix.CreateOrthographic(bounds, bounds, nearClip - nearClipOffset, farClip);

            // compute derived values
            viewProjectionMatrix = viewMatrix * projectionMatrix;
            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);

            bs.Center = center;
            bs.Radius = radius;
            //bs.Radius /= 100;
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
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero, lightDirection, lightUpVector);

            // transform scene bounding box to light space
            Bounding.BoundingBox sceneBoundingBoxLS = new Bounding.BoundingBox();
            //Bounding.BoundingVolume boundingVolume = sceneBoundingBoxLS as Bounding.BoundingVolume;
            sceneBoundingBox.Transform(lightRotation, sceneBoundingBoxLS);

            // transform view Frustum corners to light space
            Vector3[] corners = new Vector3[BoundingFrustum.CornerCount];
            cameraBoundingFrustum.GetCorners(corners);
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3.Transform(ref corners[c], ref lightRotation, out corners[c]);
            }
            // create light bounding box
            frustumBoundingBoxLS.ComputeFromPoints(corners);

            // take minimum of view bounding box and light bounding box
            Vector3 min;
            Vector3 max;
            min = Vector3.Max(frustumBoundingBoxLS.Center - frustumBoundingBoxLS.HalfSize, sceneBoundingBoxLS.Center - sceneBoundingBoxLS.HalfSize);
            max = Vector3.Min(frustumBoundingBoxLS.Center + frustumBoundingBoxLS.HalfSize, sceneBoundingBoxLS.Center + sceneBoundingBoxLS.HalfSize);
            frustumBoundingBoxLS = Bounding.BoundingBox.CreateFromMinMax(min, max);

            if (nearPlaneOffset != 0)
            {
                // apply near plane offset
                min = frustumBoundingBoxLS.Center - frustumBoundingBoxLS.HalfSize;
                max = frustumBoundingBoxLS.Center + frustumBoundingBoxLS.HalfSize;
                min.Z = min.Z - nearPlaneOffset;
                frustumBoundingBoxLS = Bounding.BoundingBox.CreateFromMinMax(min, max);
            }

            // light position is in the middle of the bounding box
            // actual position is not relevant for directional lights
            Vector3 lightPosition = frustumBoundingBoxLS.Center;

            // convert light position back into world space
            Matrix invLightRotation = Matrix.Invert(lightRotation);
            Vector3.Transform(ref lightPosition, ref invLightRotation, out lightPosition);

            // create light view matrix
            viewMatrix = Matrix.CreateLookAt(lightPosition, lightPosition + lightDirection, lightUpVector);

            // create light projection matrix
            float zNear = -(frustumBoundingBoxLS.HalfSize.Z /*+ nearBias*/);
            projectionMatrix = Matrix.CreateOrthographic(
                frustumBoundingBoxLS.HalfSize.X * 2, frustumBoundingBoxLS.HalfSize.Y * 2, zNear, frustumBoundingBoxLS.HalfSize.Z);

            viewProjectionMatrix = viewMatrix * projectionMatrix;
            invViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
            boundingFrustum = new BoundingFrustum(viewProjectionMatrix);
        }

    }
}
