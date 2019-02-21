using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GameLibrary.Component.Camera;
using System;
using GameLibrary.SceneGraph.Common;
using System.ComponentModel;
using GameLibrary.Geometry;
using System.Collections.Generic;

namespace GameLibrary.SceneGraph
{
    public sealed class LightRenderContext : RenderContext
    {
        #region Properties

        [Category("Camera")]
        public bool ShadowsEnabled
        {
            get { return shadowsEnabled; }
            set { shadowsEnabled = value; RequestRedraw(); }
        }

        [Category("Camera")]
        public bool ShowShadowMap
        {
            get { return showShadowMap; }
            set { showShadowMap = value; DebugGeometryUpdate(); }
        }

        [Category("Debug")]
        public bool ShowOccluders
        {
            get { return showOccluders; }
            set { showOccluders = value; RequestRedraw(); }
        }

        #endregion

        // shadows
        private bool shadowsEnabled;

        private int shadowMapSize;
        private int cascadeCount;

        // render target
        public readonly RenderTargetShadowCascade RenderTarget;

        // debugging
        private bool showShadowMap;

        private bool showOccluders;

        private MeshNode frustumGeo;
        private BillboardNode billboardNode;

        public LightRenderContext(GraphicsDevice graphicsDevice, Camera camera) : base(graphicsDevice, camera)
        {
            cullCamera = new LightCamera();

            shadowsEnabled = true;
            shadowMapSize = 2048;
            cascadeCount = 4;

            frustumGeo = null;
            /*RenderTarget = new RenderTargetShadowCascade(
                 GraphicsDevice,
                 renderTargetSize, renderTargetSize,
                 false,
                 SurfaceFormat.Single, DepthFormat.Depth_R32_Typeless,// DepthFormat..Depth24Stencil8,
                 0,
                 RenderTargetUsage.DiscardContents,
                 false,
                 CascadeCount);*/

            RenderTarget = new RenderTargetShadowCascade(GraphicsDevice, shadowMapSize, shadowMapSize, cascadeCount);
        }

        public override void Dispose()
        {
            base.Dispose();
            RenderTarget.Dispose();
        }

        public override void SetupGraphicsDevice()
        {
            // Set the render target
            GraphicsDevice.SetRenderTarget(RenderTarget);

            GraphicsDevice.Clear(ClearOptions.Target, Color.White, 0, 0);
        }

        // see https://www.gamedev.net/forums/topic/591684-xna-40---shimmering-shadow-maps/
        public void FitToViewStable(SceneRenderContext renderContext, float nearClipOffset)
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


            // TODO frustrum/bounds/etc... rendering : use RasterizerState.DepthClipEnable = false; in renderer
            // so we see them even if should be Z clipped (ok for far, but what about near...)

            LightCamera lightCamera = RenderCamera as LightCamera;
            Vector3 lightDirection = lightCamera.lightDirection;

            LightCamera lightCullCamera = CullCamera as LightCamera;

            Bounding.BoundingSphere frustrumBoundingSphere = renderContext.CullCamera.BoundingSphere;
            Vector3 center = frustrumBoundingSphere.Center;
            float radius = frustrumBoundingSphere.Radius;

            float bounds = 2 * radius;
            bool stable = true;
            if (stable)
            {
                // light rotation
                Matrix lightRotation;
                Vector3 zero = Vector3.Zero;
                Matrix.CreateLookAt(ref zero, ref lightDirection, ref lightUpVector, out lightRotation);

                // convert center position into light space
                Matrix invLightRotation;
                Vector3.Transform(ref center, ref lightRotation, out center);
                Matrix.Invert(ref lightRotation, out invLightRotation);

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
            lightCamera.lightPosition = center - (float)radius * lightDirection;
            Matrix.CreateLookAt(ref lightCamera.lightPosition, ref center, ref lightUpVector, out lightCamera.viewMatrix);
            lightCullCamera.viewMatrix = lightCamera.viewMatrix;

            float nearClip;
            float farClip;
            bool fitToView = true;
            bool fitToScene = false;
            if (fitToView)
            {
                // get camera frustum corners
                // FIXME : garbage...
                Vector3[] frustumCornersWS = new Vector3[BoundingFrustum.CornerCount];
                renderContext.CullCamera.BoundingFrustum.GetCorners(frustumCornersWS);

                // transform view frustum corners to light space
                // FIXME we are only interested in the Z component
                Vector3[] frustumCornersLS = new Vector3[BoundingFrustum.CornerCount];
                float minZ = float.MaxValue;
                float maxZ = float.MinValue;
                for (int c = 0; c < frustumCornersWS.Length; c++)
                {
                    Vector3.Transform(ref frustumCornersWS[c], ref lightCamera.viewMatrix, out frustumCornersLS[c]);
                    float z = frustumCornersLS[c].Z;
                    minZ = Math.Min(minZ, z);
                    maxZ = Math.Max(maxZ, z);
                }
                // FIXME..
                lightCamera.frustumBoundingBoxLS.ComputeFromPoints(frustumCornersLS);
                //Console.WriteLine(frustumBoundingBoxLS);


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
            Matrix.CreateOrthographic(bounds, bounds, nearClip - nearClipOffset, farClip, out lightCamera.projectionMatrix);

            // compute derived values
            Matrix.Multiply(ref lightCamera.viewMatrix, ref lightCamera.projectionMatrix, out lightCamera.viewProjectionMatrix);
            Matrix.Invert(ref lightCamera.viewProjectionMatrix, out lightCamera.invViewProjectionMatrix);
            // FIXME garbage
            lightCamera.boundingFrustum = new BoundingFrustum(lightCamera.viewProjectionMatrix);
            lightCamera.visitOrder = VectorUtil.visitOrder(lightCamera.ViewDirection);

            // light cull camera
            // TODO:
            // - use frustum convex hull to get better zfar (i.e. max z of hull)
            // - use open znear bounding region (copy paste and adapt BoundingFrustum class)
            // - use frustum convex hull to construct a tight culling bounding region (copy paste and adapt again BoundingFrustum class...)
            // - take into account scene : case 1 is when scene is smaller/contained in frustrum
            // - take into account scene : case 2 is when scene there is no scene => don't draw shadows at all...

            Bounding.BoundingBox bb = lightCamera.frustumBoundingBoxLS;
            Matrix.CreateOrthographicOffCenter(
                bb.Center.X - bb.HalfSize.X, bb.Center.X + bb.HalfSize.X,
                bb.Center.Y - bb.HalfSize.Y, bb.Center.Y + bb.HalfSize.Y,
                nearClip - nearClipOffset, farClip,
                out lightCullCamera.projectionMatrix);

            // compute derived values
            Matrix.Multiply(ref lightCullCamera.viewMatrix, ref lightCullCamera.projectionMatrix, out lightCullCamera.viewProjectionMatrix);
            Matrix.Invert(ref lightCullCamera.viewProjectionMatrix, out lightCullCamera.invViewProjectionMatrix);
            // FIXME garbage
            lightCullCamera.boundingFrustum = new BoundingFrustum(lightCullCamera.viewProjectionMatrix);
            lightCullCamera.visitOrder = VectorUtil.visitOrder(lightCullCamera.ViewDirection);

            /*
            lightCullCamera.ScissorRectangle.X = (int)((1.0f + ((bb.Center.X - bb.HalfSize.X) / bounds)) * 1024f);
            lightCullCamera.ScissorRectangle.Y = (int)((1.0f - ((bb.Center.Y - bb.HalfSize.Y) / bounds)) * 1024f);
            lightCullCamera.ScissorRectangle.Width = (int)(2f * bb.HalfSize.X * 2048f / bounds);
            lightCullCamera.ScissorRectangle.Height = (int)(2f * bb.HalfSize.Y * 2048f / bounds);
            */
            lightCullCamera.ScissorRectangle.X = 0;
            lightCullCamera.ScissorRectangle.Y = 0;
            lightCullCamera.ScissorRectangle.Width = 2048;
            lightCullCamera.ScissorRectangle.Height = 2048;
        }

        public override void DebugGeometryAddTo(RenderContext renderContext)
        {
            base.DebugGeometryAddTo(renderContext);

            if (ShowFrustum && frustumGeo != null)
            {
                // tight frustum bounding box
                renderContext.AddToBin(Scene.FRUSTUM, frustumGeo);
            }

            // shadow map texture
            if (showShadowMap && billboardNode != null)
            {
                renderContext.AddToBin(Scene.HUD, billboardNode);
            }

            if (ShowBoundingVolumes)
            {
                List<Drawable> drawableList = renderBins[Scene.BOUNDING_BOX];
                renderContext.AddToBin(Scene.BOUNDING_BOX, drawableList);
                // TODO bounding spheres...
            }

            if (ShowCulledBoundingVolumes)
            {
                List<Drawable> drawableList = renderBins[Scene.CULLED_BOUNDING_BOX];
                renderContext.AddToBin(Scene.CULLED_BOUNDING_BOX, drawableList);
                // TODO bounding spheres...
            }

            if (showOccluders)
            {
                // TODO this a better approach than the one used for ShowBoundingVolumes (done in the CULL callback)
                // use this approach there too...
                foreach (KeyValuePair<int, List<Drawable>> renderBinKVP in renderBins)
                {
                    int renderBinId = renderBinKVP.Key;
                    // HACK...
                    if (renderBinId >= Scene.DEBUG)
                    {
                        break;
                    }
                    List<Drawable> drawableList = renderBinKVP.Value;
                    renderContext.AddToBin(Scene.OCCLUDER_BOUNDING_BOX, drawableList);
                }
            }
        }

        protected override internal void DebugGeometryUpdate()
        {
            base.DebugGeometryUpdate();

            if (ShowFrustum)
            {
                // FIXME geometry node is rebuilt on each update!
                frustumGeo?.Dispose();
                if (RenderCamera.BoundingFrustum != null)
                {
                    frustumGeo = GeometryUtil.CreateFrustum("FRUSTUM", RenderCamera.BoundingFrustum);
                    frustumGeo.RenderGroupId = Scene.FRUSTUM;
                    frustumGeo.Initialize(GraphicsDevice);
                }
            }

            if (showShadowMap)
            {
                if (billboardNode == null)
                {
                    billboardNode = new BillboardNode("SHADOW_MAP");
                    billboardNode.Initialize(GraphicsDevice);
                    billboardNode.Texture = RenderTarget;
                }
            }
        }

        protected override internal void DebugGeometryDispose()
        {
            base.DebugGeometryDispose();

            frustumGeo?.Dispose();
            frustumGeo = null;

            billboardNode?.Dispose();
            billboardNode = null;
        }


    }
}
