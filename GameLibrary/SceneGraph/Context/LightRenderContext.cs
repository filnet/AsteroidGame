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
        public bool Stable
        {
            get { return stable; }
            set { stable = value; RequestRedraw(); }
        }

        [Category("Camera")]
        public bool FitToView
        {
            get { return fitToView; }
            set { fitToView = value; RequestRedraw(); }
        }

        [Category("Camera")]
        public bool FitToViewHull
        {
            get { return fitToViewHull; }
            set { fitToViewHull = value; RequestRedraw(); }
        }

        [Category("Camera")]
        public bool FitToScene
        {
            get { return fitToScene; }
            set { fitToScene = value; RequestRedraw(); }
        }

        [Category("Camera")]
        public bool ScissorEnabled
        {
            get { return scissorEnabled; }
            set { scissorEnabled = value; RequestRedraw(); }
        }

        [Category("Debug")]
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

        [Category("Debug")]
        public bool ShowLightPosition
        {
            get { return showLightPosition; }
            set { showLightPosition = value; DebugGeometryUpdate(); }
        }

        #endregion

        // shadows
        private bool shadowsEnabled;

        private int shadowMapSize;
        private int cascadeCount;

        private bool stable = true;
        private bool fitToView = true;
        private bool fitToViewHull = true;
        private bool fitToScene = false;

        private bool scissorEnabled = true;


        // render target
        public readonly RenderTargetShadowCascade RenderTarget;

        // debugging
        private bool showLightPosition;
        private bool showOccluders;
        private bool showShadowMap;

        private MeshNode frustumGeo;
        private MeshNode frustumHullGeo;
        private MeshNode lightPositionGeo;
        private BillboardNode billboardNode;

        public LightRenderContext(GraphicsDevice graphicsDevice, Camera camera) : base("LIGHT", graphicsDevice, camera)
        {
            cullCamera = new LightCamera();

            LightCamera lightRenderCamera = camera as LightCamera;
            LightCamera lightCullCamera = cullCamera as LightCamera;
            lightCullCamera.lightPosition = lightRenderCamera.lightDirection;

            shadowsEnabled = true;
            shadowMapSize = 2048;
            cascadeCount = 4;

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

        // TODO
        // -  use RasterizerState.SlopeScaleDepthBias when rendering shadow map ? or do it ourself ?
        // - set clip planes when rendering shadows to rasterize only needed pixels in the shadow map
        // - RasterizerState.ScissorTestEnable = ?;
        // - frustrum/bounds/etc... rendering : use RasterizerState.DepthClipEnable = false; in renderer
        //   so we see them even if should be Z clipped (ok for far, but what about near...)
        public void FitToViewStable(SceneRenderContext renderContext, float nearClipOffset)
        {
            LightCamera lightRenderCamera = RenderCamera as LightCamera;
            LightCamera lightCullCamera = CullCamera as LightCamera;

            Vector3 lightDirection = lightRenderCamera.lightDirection;
            Vector3 lightUpVector = Vector3.Up;
            // avoid case when camera up and light dir are //
            /*if (Math.Abs(Vector3.Dot(lightUpVector, lightDirection)) > 0.9f)
            {
                lightUpVector = Vector3.Forward;
            }*/

            Bounding.Sphere viewBoundingSphere = renderContext.CullCamera.BoundingSphere;

            Vector3 center = viewBoundingSphere.Center;
            float bounds = 2 * viewBoundingSphere.Radius;
            if (Stable)
            {
                // avoid shimmering
                // 1 - keep size constant
                // 2 - discretize position

                // light rotation
                Matrix lightRotation;
                Vector3 zero = Vector3.Zero;
                Matrix.CreateLookAt(ref zero, ref lightDirection, ref lightUpVector, out lightRotation);
                // inverse light rotation
                Matrix invLightRotation;
                Matrix.Invert(ref lightRotation, out invLightRotation);

                // convert center position into light space
                Vector3.Transform(ref center, ref lightRotation, out center);

                // increase the size of the ortho bounds so both light and camera frustums can slide!
                bounds *= (float)shadowMapSize / (float)(shadowMapSize - 1);

                // discretize center position
                // TODO apply directly to transformation matrix (removes the need to invert light rotation and convert light position back in WS)
                Vector2 worldUnitsPerPixel = new Vector2(bounds / shadowMapSize);
                center.X -= (float)Math.IEEERemainder(center.X, worldUnitsPerPixel.X);
                center.Y -= (float)Math.IEEERemainder(center.Y, worldUnitsPerPixel.Y);
                // FIXME is it necessary for Z too ?
                //center.Z -= (float)Math.IEEERemainder(center.Z, worldUnitsPerPixel.Z);

                // convert center position back into world space
                Vector3.Transform(ref center, ref invLightRotation, out center);
            }

            // render view matrix
            lightRenderCamera.lightPosition = center - (bounds / 2.0f) * lightDirection;
            Matrix.CreateLookAt(ref lightRenderCamera.lightPosition, ref center, ref lightUpVector, out lightRenderCamera.viewMatrix);

            // cull view matrix
            lightCullCamera.lightPosition = lightRenderCamera.lightPosition;
            lightCullCamera.viewMatrix = lightRenderCamera.viewMatrix;
            // FIXME why do we need to do it again...
            lightCullCamera.lightDirection = lightRenderCamera.lightDirection;

            float nearClip;
            float farClip;
            Vector3 cullMin;
            Vector3 cullMax;

            nearClip = 0;
            farClip = bounds;

            cullMin.X = -bounds / 2.0f;
            cullMin.Y = -bounds / 2.0f;
            cullMin.Z = -bounds;
            cullMax.X = bounds / 2.0f;
            cullMax.Y = bounds / 2.0f;
            cullMax.Z = 0;

            if (FitToView)
            {
                // TODO:
                // - [DONE] use frustum convex hull to get better zfar (i.e. max z of hull)
                // - use open znear bounding region (copy paste and adapt Frustum class)
                // - use frustum convex hull to construct a tight culling bounding region (copy paste and adapt again Frustum class...)
                // - take into account scene : case 1 is when scene is smaller/contained in frustrum
                // - take into account scene : case 2 is when there is no scene => don't draw shadows at all...

                float minZ = float.MaxValue;
                float maxZ = float.MinValue;

                // get world space frustum corners
                Vector3[] frustumCornersWS = new Vector3[SceneGraph.Bounding.Frustum.CornerCount];
                renderContext.CullCamera.BoundingFrustum.GetCorners(frustumCornersWS);

                // project world space frustum corners to light space
                Vector3[] frustumCornersLS = new Vector3[SceneGraph.Bounding.Frustum.CornerCount];
                for (int i = 0; i < frustumCornersWS.Length; i++)
                {
                    Vector3.Transform(ref frustumCornersWS[i], ref lightRenderCamera.viewMatrix, out frustumCornersLS[i]);
                    float z = frustumCornersLS[i].Z;
                    minZ = Math.Min(minZ, z);
                    maxZ = Math.Max(maxZ, z);
                }

                if (FitToViewHull)
                {
                    cullMin.X = float.MaxValue;
                    cullMin.Y = float.MaxValue;
                    cullMin.Z = float.MaxValue;
                    cullMax.X = float.MinValue;
                    cullMax.Y = float.MinValue;
                    cullMax.Z = float.MinValue;

                    int[] hull = VectorUtil.HullFromDirection(renderContext.CullCamera.BoundingFrustum, ref lightDirection);
                    for (int i = 0; i < hull.Length; i++)
                    {
                        int j = hull[i];
                        Vector3 v = frustumCornersLS[j];
                        cullMin.X = Math.Min(cullMin.X, v.X);
                        cullMin.Y = Math.Min(cullMin.Y, v.Y);
                        cullMin.Z = Math.Min(cullMin.Z, v.Z);
                        cullMax.X = Math.Max(cullMax.X, v.X);
                        cullMax.Y = Math.Max(cullMax.Y, v.Y);
                        cullMax.Z = Math.Max(cullMax.Z, v.Z);
                    }
                }

                // fit Z to scene ?
                // TODO
                if (FitToScene)
                {
                    // transform scene bounding box to light space
                    // FIXME we are only interested in the Z component
                    //Bounding.Box sceneBoundingBoxLS = new Bounding.Box();
                    //sceneBoundingBox.Transform(viewMatrix, sceneBoundingBoxLS);

                    //minZ = Math.Max(minZ, sceneBoundingBoxLS.Center.Z - sceneBoundingBoxLS.HalfSize.Z);
                    //maxZ = Math.Min(maxZ, sceneBoundingBoxLS.Center.Z + sceneBoundingBoxLS.HalfSize.Z);
                }

                // ???
                nearClip = -maxZ;
                farClip = -minZ;
            }

            //  render projection matrix
            Matrix.CreateOrthographic(bounds, bounds, nearClip, farClip, out lightRenderCamera.projectionMatrix);

            // compute derived values
            Matrix.Multiply(ref lightRenderCamera.viewMatrix, ref lightRenderCamera.projectionMatrix, out lightRenderCamera.viewProjectionMatrix);
            Matrix.Invert(ref lightRenderCamera.viewProjectionMatrix, out lightRenderCamera.invViewProjectionMatrix);
            lightRenderCamera.boundingFrustum.Matrix = lightRenderCamera.viewProjectionMatrix;
            lightRenderCamera.visitOrder = VectorUtil.visitOrder(lightRenderCamera.ViewDirection);

            // cull projection matrix
            Matrix.CreateOrthographicOffCenter(
                cullMin.X, cullMax.X, cullMin.Y, cullMax.Y, -cullMax.Z - nearClipOffset, -cullMin.Z, out lightCullCamera.projectionMatrix);

            // compute derived values
            Matrix.Multiply(ref lightCullCamera.viewMatrix, ref lightCullCamera.projectionMatrix, out lightCullCamera.viewProjectionMatrix);
            Matrix.Invert(ref lightCullCamera.viewProjectionMatrix, out lightCullCamera.invViewProjectionMatrix);
            lightCullCamera.boundingFrustum.Matrix = lightCullCamera.viewProjectionMatrix;
            lightCullCamera.visitOrder = VectorUtil.visitOrder(lightCullCamera.ViewDirection);

            if (ScissorEnabled)
            {
                Viewport viewport = new Viewport(0, 0, shadowMapSize, shadowMapSize);
                Vector3 min = viewport.Project(cullMin, lightRenderCamera.projectionMatrix, Matrix.Identity, Matrix.Identity);
                Vector3 max = viewport.Project(cullMax, lightRenderCamera.projectionMatrix, Matrix.Identity, Matrix.Identity);
                Vector2 o;
                o.X = min.X;
                o.Y = max.Y;
                Vector2 w;
                w.X = max.X - min.X;
                w.Y = min.Y - max.Y;
                //Console.WriteLine(o.X + " " + o.Y + "    " + w.X + " " + w.Y);

                lightCullCamera.ScissorRectangle.X = (int)o.X;
                lightCullCamera.ScissorRectangle.Y = (int)o.Y;
                lightCullCamera.ScissorRectangle.Width = (int)w.X;
                lightCullCamera.ScissorRectangle.Height = (int)w.Y;
            }
            else
            {
                lightCullCamera.ScissorRectangle.X = 0;
                lightCullCamera.ScissorRectangle.Y = 0;
                lightCullCamera.ScissorRectangle.Width = shadowMapSize;
                lightCullCamera.ScissorRectangle.Height = shadowMapSize;
            }
        }

        public override void DebugGeometryAddTo(RenderContext renderContext)
        {
            base.DebugGeometryAddTo(renderContext);

            if (ShowFrustum)
            {
                // FIXME geometry node is rebuilt on each update!
                // FIXME does not belong here...
                frustumGeo?.Dispose();
                frustumGeo = null;

                frustumGeo = GeometryUtil.CreateFrustum(GeneratedName("RENDER_FRUSTUM"), RenderCamera.BoundingFrustum);
                frustumGeo.Initialize(GraphicsDevice);
            }
            if (ShowFrustumHull)
            {
                // frustum hull
                // FIXME geometry node is rebuilt on each update!
                frustumHullGeo?.Dispose();
                frustumHullGeo = null;

                LightCamera lightCamera = cullCamera as LightCamera;
                Vector3[] hull = VectorUtil.HullCornersFromDirection(renderContext.CullCamera.BoundingFrustum, ref lightCamera.lightDirection);
                if (hull.Length > 0)
                {
                    // FIXME when rendering from light viewpoint the hull flickers badly and disappears because back face is z clipped...
                    frustumHullGeo = new MeshNode(GeneratedName("VIEW_FRUSTUM_HULL"), new LineMeshFactory(hull, true));
                    frustumHullGeo.Initialize(GraphicsDevice);
                }
                else
                {
                    Console.WriteLine("!!! NO HULL !!!");
                }
            }
            if (ShowFrustum && frustumGeo != null)
            {
                // frustum (of renderer...)
                // FIXME does not belong here...
                renderContext.AddToBin(Scene.FRUSTUM, frustumGeo);
            }
            if (ShowFrustumHull && frustumHullGeo != null)
            {
                // frustum hull (of main...) 
                renderContext.AddToBin(Scene.BOUNDING_HULL, frustumHullGeo);
            }
            if (ShowLightPosition && lightPositionGeo != null)
            {
                LightCamera lightCamera = renderCamera as LightCamera;
                Vector3 lightPosition = lightCamera.lightPosition;
                lightPositionGeo.Translation = lightPosition;
                lightPositionGeo.UpdateTransform();
                lightPositionGeo.UpdateWorldTransform();
                renderContext.AddToBin(Scene.ONE_LIGHT, lightPositionGeo);
            }
            if (ShowShadowMap && billboardNode != null)
            {
                renderContext.AddToBin(Scene.HUD, billboardNode);
            }
            if (ShowBoundingVolumes)
            {
                List<Drawable> drawableList = renderBins[Scene.BOUNDING_BOX];
                renderContext.AddToBin(Scene.BOUNDING_BOX, drawableList);
                drawableList = renderBins[Scene.BOUNDING_SPHERE];
                renderContext.AddToBin(Scene.BOUNDING_SPHERE, drawableList);
            }
            if (ShowCulledBoundingVolumes)
            {
                List<Drawable> drawableList = renderBins[Scene.CULLED_BOUNDING_BOX];
                renderContext.AddToBin(Scene.CULLED_BOUNDING_BOX, drawableList);
                drawableList = renderBins[Scene.CULLED_BOUNDING_SPHERE];
                renderContext.AddToBin(Scene.CULLED_BOUNDING_SPHERE, drawableList);
            }
            if (ShowOccluders)
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

            if (ShowLightPosition)
            {
                if (lightPositionGeo == null)
                {
                    lightPositionGeo = GeometryUtil.CreateGeodesic(GeneratedName("POSITION"), 4);
                    lightPositionGeo.Initialize(GraphicsDevice);
                }
            }
            if (ShowShadowMap)
            {
                if (billboardNode == null)
                {
                    billboardNode = new BillboardNode(GeneratedName("SHADOW_MAP"));
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

            frustumHullGeo?.Dispose();
            frustumHullGeo = null;

            lightPositionGeo?.Dispose();
            lightPositionGeo = null;

            billboardNode?.Dispose();
            billboardNode = null;
        }


    }
}
