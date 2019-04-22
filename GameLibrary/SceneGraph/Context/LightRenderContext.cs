using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GameLibrary.Component.Camera;
using System;
using GameLibrary.SceneGraph.Common;
using System.ComponentModel;
using GameLibrary.Geometry;
using System.Collections.Generic;
using GameLibrary.Component.Util;

namespace GameLibrary.SceneGraph
{
    public enum ViewSplitsMode { None, All, Single }

    public struct CascadeSplitInfo
    {
        public int StartSplit;
        public int EndSplit;
    }

    public class CascadeRenderBin : RenderBin
    {
        public List<CascadeSplitInfo> CascadeSplitInfoList
        {
            get { return cascadeSplitInfoList; }
        }

        private readonly List<CascadeSplitInfo> cascadeSplitInfoList;

        private readonly int cascadeCount;
        private readonly int[] splitDrawCount;
        private readonly int[] splitVertexCount;

        public CascadeRenderBin(long id, int cascadeCount) : base(id)
        {
            this.cascadeCount = cascadeCount;
            cascadeSplitInfoList = new List<CascadeSplitInfo>();
            splitDrawCount = new int[cascadeCount];
            splitVertexCount = new int[cascadeCount];
        }

        public void AddSplitInfo(CascadeSplitInfo info, Drawable drawable)
        {
            if (info.StartSplit < 0) throw new InvalidOperationException("StartSplit < 0");
            if (info.StartSplit > info.EndSplit) throw new InvalidOperationException("StartSplit > EndSplit");
            if (info.EndSplit >= cascadeCount) throw new InvalidOperationException("EndSplit >= cascadeCount");
            for (int i = info.StartSplit; i <= info.EndSplit; i++)
            {
                splitDrawCount[i] += 1;
                splitVertexCount[i] += drawable.VertexCount;
            }
            CascadeSplitInfoList.Add(info);
        }

        public override void Clear()
        {
            base.Clear();
            cascadeSplitInfoList.Clear();
            Array.Clear(splitDrawCount, 0, splitDrawCount.Length);
            Array.Clear(splitVertexCount, 0, splitVertexCount.Length);
        }
    }


    public sealed class LightRenderContext : RenderContext
    {
        #region Properties

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

        [Category("Camera")]
        public bool SplitCull
        {
            get { return splitCull; }
            set { splitCull = value; RequestRedraw(); }

        }

        [Category("Debug")]
        public bool ShowShadowMap
        {
            get { return showShadowMap; }
            set { showShadowMap = value; DebugGeometryUpdate(); }
        }

        [Category("Debug")]
        public bool ShowSplits
        {
            get { return showSplits; }
            set { showSplits = value; RequestRedraw(); }
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

        [Category("Debug View Frustum")]
        public bool ShowViewFrustumHull
        {
            get { return showViewFrustumHull; }
            set { showViewFrustumHull = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Cascade")]
        public ViewSplitsMode ViewSplitsMode
        {
            get { return viewSplitsMode; }
            set { viewSplitsMode = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Cascade")]
        public int ShowSplitIndex
        {
            get { return showSplitIndex; }
            set { showSplitIndex = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Cascade")]
        public bool ShowSplitFrustums
        {
            get { return showSplitFrustums; }
            set { showSplitFrustums = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Cascade")]
        public bool ShowSplitSpheres
        {
            get { return showSplitSpheres; }
            set { showSplitSpheres = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Cascade")]
        public bool ShowSplitLightRegions
        {
            get { return showSplitLightRegions; }
            set { showSplitLightRegions = value; DebugGeometryUpdate(); }
        }

        #endregion

        #region Fields

        // shadow map
        private int shadowMapSize;

        // cascade
        private int cascadeCount;

        private bool splitCull;

        // shadow map rendering
        public Matrix[] viewProjectionMatrices;

        private readonly int[] shadowReceiverCount;
        private readonly int[] shadowCasterCount;

        private int activeSplitStart;
        private int activeSplitEnd;

        // shadow rendering
        public Matrix[] viewMatrices;

        public Vector4[] splitDistances;
        public Vector4[] splitOffsets;
        public Vector4[] splitScales;

        private Bounding.Frustum[] splitFrustums;
        private Bounding.Sphere[] splitBoundingSpheres;
        private Bounding.Region[] splitLightCullRegions;

        // light
        private Vector3 lightDirection;
        private Vector3 lightUpVector;
        private Matrix lightRotation;
        private Matrix invLightRotation;

        // flags
        private bool stable = true;
        private bool fitToView = true;
        private bool fitToScene = false;

        private bool scissorEnabled = false;

        // render target
        public readonly RenderTargetShadowCascade ShadowRenderTarget;

        // debugging
        private bool showLightPosition;
        private bool showOccluders;
        private bool showShadowMap;
        private bool showSplits;

        private bool showViewFrustumHull;

        private ViewSplitsMode viewSplitsMode = ViewSplitsMode.None;
        private int showSplitIndex = 0;
        private bool showSplitFrustums = false;
        private bool showSplitSpheres = false;
        private bool showSplitLightRegions = false;

        private MeshNode frustumGeo;
        private MeshNode lightPositionGeo;
        private BillboardNode billboardNode;
        private MeshNode viewFrustumHullGeo;
        private MeshNode[] viewFrustumSplitGeos;
        private MeshNode[] viewFrustumSphereSplitGeos;
        private MeshNode[] lightRegionSplitGeos;

        #endregion

        public LightRenderContext(GraphicsDevice graphicsDevice, LightCamera lightCamera) : base("LIGHT", graphicsDevice, lightCamera)
        {
            cullCamera = new LightCamera(lightCamera);

            shadowMapSize = 2048;
            cascadeCount = 4;

            splitCull = true;

            // shadow map rendering
            viewProjectionMatrices = new Matrix[cascadeCount];

            shadowReceiverCount = new int[cascadeCount];
            shadowCasterCount = new int[cascadeCount];

            // shadow rendering
            viewMatrices = new Matrix[cascadeCount];
            splitDistances = new Vector4[cascadeCount];
            splitOffsets = new Vector4[cascadeCount];
            splitScales = new Vector4[cascadeCount];

            splitFrustums = new Bounding.Frustum[cascadeCount];
            for (int i = 0; i < cascadeCount; i++)
            {
                splitFrustums[i] = new Bounding.Frustum();
            }

            splitBoundingSpheres = new Bounding.Sphere[cascadeCount];
            for (int i = 0; i < cascadeCount; i++)
            {
                splitBoundingSpheres[i] = new Bounding.Sphere();
            }

            splitLightCullRegions = new Bounding.Region[cascadeCount];
            for (int i = 0; i < cascadeCount; i++)
            {
                splitLightCullRegions[i] = new Bounding.Region();
            }

            // TODO do it when light direction changed
            Update();

            ShadowRenderTarget = new RenderTargetShadowCascade(GraphicsDevice, shadowMapSize, shadowMapSize, cascadeCount);
        }

        public override void Dispose()
        {
            base.Dispose();
            ShadowRenderTarget.Dispose();
        }

        protected override RenderBin CreateRenderBin(long id)
        {
            return new CascadeRenderBin(id, cascadeCount);
        }

        public void AddShadowReceiver(RenderBin renderBin, Drawable drawable)
        {
            int start = -1;
            int end = -1;
            bool done = false;
            for (int splitIndex = 0; !done && splitIndex < cascadeCount; splitIndex++)
            {
                ContainmentType containmentType = splitFrustums[splitIndex].Contains(drawable.WorldBoundingVolume, Bounding.ContainmentHint.Precise);
                switch (containmentType)
                {
                    case ContainmentType.Contains:
                        // split frustums don't overlap
                        // so if a frustum contains the drawable then we are done
                        start = splitIndex;
                        end = splitIndex;
                        // done
                        done = true;
                        break;
                    case ContainmentType.Intersects:
                        if (start == -1)
                        {
                            // this is the first slit for this drawable
                            start = splitIndex;
                        }
                        // and also the current last
                        end = splitIndex;
                        break;
                    case ContainmentType.Disjoint:
                        if (end != -1)
                        {
                            // done because further split frustum won't contain the drawable
                            done = true;
                        }
                        // can't say, just continue
                        break;
                }
            }
            // should not be necessary to test but the view frustum culling
            // sends some false positives that get caught here
            // TOOD could provide this information back to the view culling...
            if (start != -1 && end != -1)
            {
                for (int i = start; i <= end; i++)
                {
                    shadowReceiverCount[i] += 1;
                }
            }
            //else
            //{
            //Console.WriteLine("XXXXX");
            //}
        }

        public override void CullBegin()
        {
            base.CullBegin();

            // get first and last splits that have shadow receivers
            // holes in between are ignored
            activeSplitStart = -1;
            activeSplitEnd = -1;
            for (int splitIndex = 0; splitIndex < cascadeCount; splitIndex++)
            {
                if (shadowReceiverCount[splitIndex] != 0)
                {
                    if (activeSplitStart == -1)
                    {
                        activeSplitStart = splitIndex;
                    }
                    activeSplitEnd = splitIndex;
                }
            }
            //Console.WriteLine(activeSplitStart + " - " + activeSplitEnd)
        }

        public override void CullEnd()
        {
            base.CullEnd();
        }

        protected override void AddToRenderBin(RenderBin renderBin, Drawable drawable)
        {
            // HACK
            if (renderBin.Id >= Scene.DEBUG)
            {
                base.AddToRenderBin(renderBin, drawable);
                return;
            }

            int start = -1;
            int end = -1;
            if (activeSplitStart != -1 && activeSplitEnd != -1 && splitCull)
            {
                // we check only the active splits
                bool done = false;
                for (int splitIndex = activeSplitStart; !done && splitIndex <= activeSplitEnd; splitIndex++)
                {
                    if (shadowReceiverCount[splitIndex] == 0)
                    {
                        continue;
                    }
                    Bounding.ContainmentHint hint = Bounding.ContainmentHint.Precise;
                    ContainmentType containmentType = splitLightCullRegions[splitIndex].Contains(drawable.WorldBoundingVolume, hint);
                    switch (containmentType)
                    {
                        case ContainmentType.Contains:
                            // further split regions can contain earlier ones
                            // so we can't assume we are done (contrary to view frustum case)
                            if (start == -1)
                            {
                                start = splitIndex;
                            }
                            end = splitIndex;
                            break;
                        case ContainmentType.Intersects:
                            if (start == -1)
                            {
                                // this is the first slit for this drawable
                                start = splitIndex;
                            }
                            // and also the current last
                            end = splitIndex;
                            break;
                        case ContainmentType.Disjoint:
                            if (end != -1)
                            {
                                // done
                                done = true;
                            }
                            // can't say, just continue
                            break;
                    }
                }
            }
            else
            {
                start = activeSplitStart;
                end = activeSplitEnd;
            }
            // there are a few false positives from view frustum culling...
            if (start != -1 && end != -1)
            {
                for (int i = start; i <= end; i++)
                {
                    shadowCasterCount[i] += 1;
                }
                CascadeSplitInfo cascadeSplitInfo = new CascadeSplitInfo();
                cascadeSplitInfo.StartSplit = start;
                cascadeSplitInfo.EndSplit = end;

                CascadeRenderBin cascadeRenderBin = renderBin as CascadeRenderBin;
                base.AddToRenderBin(renderBin, drawable);
                cascadeRenderBin.AddSplitInfo(cascadeSplitInfo, drawable);
            }
        }

        public override void SetupGraphicsDevice()
        {
            // Set the render target
            GraphicsDevice.SetRenderTarget(ShadowRenderTarget);

            GraphicsDevice.Clear(ClearOptions.Target, Color.White, 0, 0);
        }

        public override void Clear()
        {
            base.Clear();
            Array.Clear(shadowReceiverCount, 0, shadowReceiverCount.Length);
            Array.Clear(shadowCasterCount, 0, shadowCasterCount.Length);
        }

        private void Update()
        {
            LightCamera lightRenderCamera = RenderCamera as LightCamera;
            LightCamera lightCullCamera = CullCamera as LightCamera;

            lightDirection = lightRenderCamera.lightDirection;
            lightUpVector = Vector3.Up;
            // avoid case when camera up and light dir are //
            /*if (Math.Abs(Vector3.Dot(lightUpVector, lightDirection)) > 0.9f)
            {
                lightUpVector = Vector3.Forward;
            }*/

            // light rotation
            Vector3 zero = Vector3.Zero;
            Matrix.CreateLookAt(ref zero, ref lightDirection, ref lightUpVector, out lightRotation);
            // inverse light rotation
            Matrix.Invert(ref lightRotation, out invLightRotation);
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
        public void FitToViewStable(SceneRenderContext renderContext)
        {
            Camera viewCamera = renderContext.CullCamera;

            Vector3[] frustumCornersLS = new Vector3[SceneGraph.Bounding.Frustum.CornerCount];

            //Matrix m = viewCamera.ProjectionMatrix;
            //Bounding.Frustum frustum = new Bounding.Frustum();

            int splitCount = splitDistances.Length;
            float lastSplitDistance = viewCamera.ZNear;
            for (int i = 0; i < splitCount; ++i)
            {
                float splitDistance = ComputeSplitDistance(i + 1, splitCount, viewCamera.ZNear, viewCamera.ZFar, 0.22f);
                //Console.Write(lastSplitDistance + " - " + splitDistance + ": ");

                // split frustum
                // TODO use view frustum to copy corners and planes as many are shared by splits
                // TODO similartly, modify copy of view frustum matrix (see commented out lines below)

                float aspectInv = 1.0f / viewCamera.AspectRatio;
                float e = 1.0f / (float)Math.Tan(viewCamera.FovX / 2.0f);
                float fovY = 2.0f * (float)Math.Atan(aspectInv / e);
                Matrix m = Matrix.CreatePerspectiveFieldOfView(fovY, viewCamera.AspectRatio, lastSplitDistance, splitDistance);
                //m.M33 = 1.0f / (lastSplitDistance - splitDistance);
                //m.M43 = lastSplitDistance / (lastSplitDistance - splitDistance);

                Bounding.Frustum frustum = splitFrustums[i];
                frustum.Matrix = viewCamera.ViewMatrix * m;

                // light cull region for split
                frustum.RegionFromDirection(ref lightDirection, splitLightCullRegions[i]);

                // best fitting bounding sphere for slice
                double dz;
                double radius;
                VolumeUtil.ComputeFrustumBestFittingSphere(
                    viewCamera.FovX, viewCamera.AspectRatio, lastSplitDistance, splitDistance,
                    out dz, out radius);

                float bounds = 2.0f * (float)radius;
                //Console.Write(dz + ", " + bounds + " ");

                // compute actual bounding sphere center
                Vector3 center;
                frustum.NearFaceCenter((float)dz - lastSplitDistance, out center);

                if (Stable)
                {
                    StabilizeSphere(ref center, ref bounds);
                }
                Bounding.Sphere sphere = splitBoundingSpheres[i];
                sphere.Center = center;
                sphere.Radius = (float)radius;

                // light view matrix
                Matrix viewMatrix;
                Vector3 lightPosition = center - (bounds / 2.0f) * lightDirection;
                Matrix.CreateLookAt(ref lightPosition, ref center, ref lightUpVector, out viewMatrix);

                Vector3 frustumMinLS;
                Vector3 frustumMaxLS;
                if (FitToView)
                {
                    frustum.GetTransformedCorners(frustumCornersLS, ref viewMatrix);

                    // TODO:
                    // - [DONE] use open znear bounding region (copy paste and adapt Frustum class)
                    // - [DONE] use frustum convex hull to construct a tight culling bounding region (copy paste and adapt again Frustum class...)
                    // - take into account scene : case 1 is when scene is smaller/contained in frustrum
                    // - take into account scene : case 2 is when there is no scene => don't draw shadows at all...
                    frustumMinLS.X = float.MaxValue;
                    frustumMinLS.Y = float.MaxValue;
                    frustumMinLS.Z = float.MaxValue;
                    frustumMaxLS.X = float.MinValue;
                    frustumMaxLS.Y = float.MinValue;
                    frustumMaxLS.Z = float.MinValue;

                    // get light space frustum corners

                    // compute light space frustum bounding box...
                    for (int j = 0; j < frustumCornersLS.Length; j++)
                    {
                        Vector3 v = frustumCornersLS[j];
                        frustumMinLS.X = Math.Min(frustumMinLS.X, v.X);
                        frustumMinLS.Y = Math.Min(frustumMinLS.Y, v.Y);
                        frustumMinLS.Z = Math.Min(frustumMinLS.Z, v.Z);
                        frustumMaxLS.X = Math.Max(frustumMaxLS.X, v.X);
                        frustumMaxLS.Y = Math.Max(frustumMaxLS.Y, v.Y);
                        frustumMaxLS.Z = Math.Max(frustumMaxLS.Z, v.Z);
                    }
                }
                else
                {
                    frustumMinLS.X = -bounds / 2.0f;
                    frustumMinLS.Y = -bounds / 2.0f;
                    frustumMinLS.Z = -bounds;
                    frustumMaxLS.X = bounds / 2.0f;
                    frustumMaxLS.Y = bounds / 2.0f;
                    frustumMaxLS.Z = 0;
                }

                //  light projection matrix
                Matrix projectionMatrix;
                Matrix.CreateOrthographic(bounds, bounds, -frustumMaxLS.Z, -frustumMinLS.Z, out projectionMatrix);

                //  light view+projection matrix
                Matrix viewProjectionMatrix;
                Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out viewProjectionMatrix);

                // for rendering the shadow map
                viewProjectionMatrices[i] = viewProjectionMatrix;

                // for rendering the shadows
                Matrix textureScaleMatrix = Matrix.CreateScale(0.5f, -0.5f, 1.0f);
                Matrix textureTranslationMatrix = Matrix.CreateTranslation(0.5f, 0.5f, 0.0f);
                Matrix shadowProjection = projectionMatrix * textureScaleMatrix * textureTranslationMatrix;
                Vector4 scale = new Vector4(shadowProjection.M11, shadowProjection.M22, shadowProjection.M33, 1.0f);
                Vector4 offset = new Vector4(shadowProjection.M41, shadowProjection.M42, shadowProjection.M43, 0.0f);

                viewMatrices[i] = viewMatrix;
                // TODO express distances in clip space (to avoid a multiplication by world view in vertex shader)
                splitDistances[i] = new Vector4(splitDistance);
                splitScales[i] = scale;
                splitOffsets[i] = offset;

                if (i == ShowSplitIndex)
                {
                    // render camera
                    LightCamera lightRenderCamera = RenderCamera as LightCamera;
                    lightRenderCamera.lightPosition = lightPosition;
                    lightRenderCamera.viewMatrix = viewMatrix;
                    lightRenderCamera.projectionMatrix = projectionMatrix;
                    lightRenderCamera.viewProjectionMatrix = viewProjectionMatrix;
                    // compute derived values
                    Matrix.Invert(ref lightRenderCamera.viewProjectionMatrix, out lightRenderCamera.inverseViewProjectionMatrix);
                    lightRenderCamera.boundingFrustum.Matrix = lightRenderCamera.viewProjectionMatrix;
                    lightRenderCamera.visitOrder = VectorUtil.visitOrder(lightRenderCamera.ViewDirection);

                    // cull camera
                    LightCamera lightCullCamera = CullCamera as LightCamera;
                    lightCullCamera.lightPosition = lightPosition;
                    lightCullCamera.viewMatrix = viewMatrix;
                    Matrix.CreateOrthographicOffCenter(frustumMinLS.X, frustumMaxLS.X, frustumMinLS.Y, frustumMaxLS.Y, -frustumMaxLS.Z, -frustumMinLS.Z, out lightCullCamera.projectionMatrix);
                    Matrix.Multiply(ref lightCullCamera.viewMatrix, ref lightCullCamera.projectionMatrix, out lightCullCamera.viewProjectionMatrix);
                    // compute derived values
                    Matrix.Invert(ref lightCullCamera.viewProjectionMatrix, out lightCullCamera.inverseViewProjectionMatrix);
                    lightCullCamera.boundingFrustum.Matrix = lightCullCamera.viewProjectionMatrix;
                    lightCullCamera.visitOrder = VectorUtil.visitOrder(lightCullCamera.ViewDirection);

                    if (ScissorEnabled)
                    {
                        Viewport viewport = new Viewport(0, 0, shadowMapSize, shadowMapSize);
                        Vector3 min = viewport.Project(frustumMinLS, lightRenderCamera.projectionMatrix, Matrix.Identity, Matrix.Identity);
                        Vector3 max = viewport.Project(frustumMaxLS, lightRenderCamera.projectionMatrix, Matrix.Identity, Matrix.Identity);
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

                lastSplitDistance = splitDistance;
                //Console.WriteLine();
            }

            // cull region
            if (true /*FitToView*/)
            {
                LightCamera lightCullCamera = CullCamera as LightCamera;
                // TODO cull region should be restricted to splits that contain shadow receivers
                //      could be done in CullBegin after view culling and all shadow receivers are known
                viewCamera.Frustum.RegionFromDirection(ref lightDirection, lightCullCamera.cullRegion);
            }

        }

        private static float ComputeSplitDistance(int splitIndex, int splitCount, float znear, float zfar, float lerpWeight)
        {
            double n = znear;
            double f = zfar;
            double w = lerpWeight; // 0.22f;

            double p = (double)splitIndex / (double)splitCount;
            double dLog = n * Math.Pow(f / n, p);
            double dLin = MathUtil.Lerp(n, f, p);
            double d = MathUtil.Lerp(dLog, dLin, w);

            return (float)d;
        }

        private void StabilizeSphere(ref Vector3 center, ref float bounds)
        {
            // avoid shimmering
            // 1 - keep size constant
            // 2 - discretize position

            // convert center position into light space
            Vector3 centerLS;
            Vector3.Transform(ref center, ref lightRotation, out centerLS);

            // increase the size of the ortho bounds so both light and camera frustums can slide!
            bounds *= (float)shadowMapSize / (float)(shadowMapSize - 1);

            // discretize center position
            // TODO apply directly to transformation matrix (removes the need to invert light rotation and convert light position back in WS)
            Vector2 worldUnitsPerPixel = new Vector2(bounds / shadowMapSize);
            centerLS.X -= (float)Math.IEEERemainder(centerLS.X, worldUnitsPerPixel.X);
            centerLS.Y -= (float)Math.IEEERemainder(centerLS.Y, worldUnitsPerPixel.Y);
            // FIXME is it necessary for Z too ?
            //center.Z -= (float)Math.IEEERemainder(centerLS.Z, worldUnitsPerPixel.Z);

            // convert center position back into world space
            Vector3.Transform(ref centerLS, ref invLightRotation, out center);
        }

        protected internal override void ShowStats(String name)
        {
            base.ShowStats(name);
            Console.WriteLine("  Split count : {0}", cascadeCount);
            Console.WriteLine("  Active split start / end : {0} / {1}", activeSplitStart, activeSplitEnd);
            for (int splitIndex = 0; splitIndex < cascadeCount; splitIndex++)
            {
                Console.WriteLine("  Split #{0} reveiver/caster : {1}/{2}", splitIndex, shadowReceiverCount[splitIndex], shadowCasterCount[splitIndex]);
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

                frustumGeo = GeometryUtil.CreateFrustum(GenerateName("RENDER_FRUSTUM"), RenderCamera.Frustum);
                frustumGeo.Initialize(GraphicsDevice);
            }
            if (ShowViewFrustumHull)
            {
                // frustum hull
                // FIXME geometry node is rebuilt on each update!
                viewFrustumHullGeo?.Dispose();
                viewFrustumHullGeo = null;

                LightCamera lightCamera = cullCamera as LightCamera;
                Bounding.Frustum frustum = renderContext.CullCamera.Frustum;
                Vector3[] hull = frustum.HullCornersFromDirection(ref lightCamera.lightDirection);
                if (hull.Length > 0)
                {
                    // FIXME when rendering from light viewpoint the hull flickers badly and disappears because back face is z clipped...
                    viewFrustumHullGeo = new MeshNode(GenerateName("VIEW_FRUSTUM_HULL"), new LineMeshFactory(hull, true));
                    viewFrustumHullGeo.Initialize(GraphicsDevice);
                }
                else
                {
                    Console.WriteLine("!!! NO HULL !!!");
                }
            }
            if (ViewSplitsMode != ViewSplitsMode.None)
            {
                // FIXME geometry node is rebuilt on each update!
                // FIXME does not belong here...

                if (viewFrustumSplitGeos is null)
                {
                    viewFrustumSplitGeos = new MeshNode[cascadeCount];
                }
                for (int i = 0; i < cascadeCount; i++)
                {
                    MeshNode frustumGeo = viewFrustumSplitGeos[i];

                    frustumGeo?.Dispose();
                    frustumGeo = null;

                    if ((ViewSplitsMode == ViewSplitsMode.Single) && (i != ShowSplitIndex))
                    {
                        continue;
                    }
                    frustumGeo = GeometryUtil.CreateFrustum(GenerateName("RENDER_FRUSTUM_SPLIT#" + i), splitFrustums[i]);
                    frustumGeo.Initialize(GraphicsDevice);

                    viewFrustumSplitGeos[i] = frustumGeo;
                }
                if (lightRegionSplitGeos is null)
                {
                    lightRegionSplitGeos = new MeshNode[cascadeCount];
                }
                for (int i = 0; i < cascadeCount; i++)
                {
                    MeshNode regionGeo = lightRegionSplitGeos[i];

                    regionGeo?.Dispose();
                    regionGeo = null;

                    if ((ViewSplitsMode == ViewSplitsMode.Single) && (i != ShowSplitIndex))
                    {
                        continue;
                    }
                    regionGeo = GeometryUtil.CreateRegion(GenerateName("LIGHT_REGION_SPLIT#" + i), splitLightCullRegions[i]);
                    regionGeo.Initialize(GraphicsDevice);

                    lightRegionSplitGeos[i] = regionGeo;
                }
            }
            if (ShowFrustum && frustumGeo != null)
            {
                // frustum (of renderer...)
                // FIXME does not belong here...
                renderContext.AddToBin(Scene.FRUSTUM, frustumGeo);
            }
            if (ShowViewFrustumHull && viewFrustumHullGeo != null)
            {
                // frustum hull (of main...) 
                renderContext.AddToBin(Scene.BOUNDING_HULL, viewFrustumHullGeo);
            }
            if (ViewSplitsMode != ViewSplitsMode.None)
            {
                if (ShowSplitFrustums && viewFrustumSplitGeos != null)
                {
                    for (int i = 0; i < cascadeCount; i++)
                    {
                        if ((ViewSplitsMode == ViewSplitsMode.Single) && (i != ShowSplitIndex))
                        {
                            continue;
                        }
                        MeshNode frustumGeo = viewFrustumSplitGeos[i];
                        if (frustumGeo != null)
                        {
                            renderContext.AddToBin(Scene.FRUSTUM, frustumGeo);
                        }
                    }
                }
                if (ShowSplitLightRegions && lightRegionSplitGeos != null)
                {
                    for (int i = 0; i < cascadeCount; i++)
                    {
                        if ((ViewSplitsMode == ViewSplitsMode.Single) && (i != ShowSplitIndex))
                        {
                            continue;
                        }
                        MeshNode regionGeo = lightRegionSplitGeos[i];
                        if (regionGeo != null)
                        {
                            renderContext.AddToBin(Scene.REGION, regionGeo);
                        }
                    }
                }
                if (ShowSplitSpheres && viewFrustumSphereSplitGeos != null)
                {
                    for (int i = 0; i < cascadeCount; i++)
                    {
                        MeshNode frustumSphereGeo = viewFrustumSphereSplitGeos[i];
                        if ((ViewSplitsMode == ViewSplitsMode.Single) && (i != ShowSplitIndex))
                        {
                            continue;
                        }
                        if (frustumSphereGeo != null)
                        {
                            Bounding.Sphere sphere = splitBoundingSpheres[i];
                            frustumSphereGeo.BoundingVolume = sphere;
                            frustumSphereGeo.WorldBoundingVolume = sphere;
                            renderContext.AddToBin(Scene.BOUNDING_SPHERE, frustumSphereGeo);
                        }
                    }
                }
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
                if (renderBins.ContainsKey(Scene.BOUNDING_BOX))
                {
                    List<Drawable> drawableList = renderBins[Scene.BOUNDING_BOX].DrawableList;
                    renderContext.AddToBin(Scene.BOUNDING_BOX, drawableList);
                }
                if (renderBins.ContainsKey(Scene.BOUNDING_SPHERE))
                {
                    List<Drawable> drawableList = renderBins[Scene.BOUNDING_SPHERE].DrawableList;
                    renderContext.AddToBin(Scene.BOUNDING_SPHERE, drawableList);
                }
            }
            if (ShowCulledBoundingVolumes)
            {
                if (renderBins.ContainsKey(Scene.CULLED_BOUNDING_BOX))
                {
                    List<Drawable> drawableList = renderBins[Scene.CULLED_BOUNDING_BOX].DrawableList;
                    renderContext.AddToBin(Scene.CULLED_BOUNDING_BOX, drawableList);
                }
                if (renderBins.ContainsKey(Scene.CULLED_BOUNDING_SPHERE))
                {
                    List<Drawable> drawableList = renderBins[Scene.CULLED_BOUNDING_SPHERE].DrawableList;
                    renderContext.AddToBin(Scene.CULLED_BOUNDING_SPHERE, drawableList);
                }
            }
            if (ShowOccluders)
            {
                // TODO this a better approach than the one used for ShowBoundingVolumes (done in the CULL callback)
                // use this approach there too...
                foreach (KeyValuePair<int, RenderBin> renderBinKVP in renderBins)
                {
                    int renderBinId = renderBinKVP.Key;
                    // HACK...
                    if (renderBinId >= Scene.DEBUG)
                    {
                        break;
                    }
                    RenderBin renderBin = renderBinKVP.Value;
                    List<Drawable> drawableList = renderBin.DrawableList;
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
                    lightPositionGeo = GeometryUtil.CreateGeodesic(GenerateName("POSITION"), 4);
                    lightPositionGeo.Initialize(GraphicsDevice);
                }
            }
            if (ShowShadowMap)
            {
                if (billboardNode == null)
                {
                    billboardNode = new BillboardNode(GenerateName("SHADOW_MAP"));
                    billboardNode.Initialize(GraphicsDevice);
                    billboardNode.Mode = 5; // 2x2 (for 4 cascades...)
                    billboardNode.Texture = ShadowRenderTarget;
                }
            }
            if (ViewSplitsMode != ViewSplitsMode.None)
            {
                if (viewFrustumSphereSplitGeos is null)
                {
                    viewFrustumSphereSplitGeos = new MeshNode[cascadeCount];
                }
                for (int i = 0; i < cascadeCount; i++)
                {
                    MeshNode frustumSphereGeo = GeometryUtil.CreateGeodesicWF(GenerateName("VIEW_FRUSTUM_SPHERE_SPLIT#" + i), 1);
                    frustumSphereGeo.Initialize(GraphicsDevice);
                    //frustumSphereGeo.BoundingVolume = CullCamera.BoundingSphere;
                    //frustumSphereGeo.WorldBoundingVolume = CullCamera.BoundingSphere;
                    viewFrustumSphereSplitGeos[i] = frustumSphereGeo;
                }
            }
        }

        protected override internal void DebugGeometryDispose()
        {
            base.DebugGeometryDispose();

            frustumGeo?.Dispose();
            frustumGeo = null;

            viewFrustumHullGeo?.Dispose();
            viewFrustumHullGeo = null;

            lightPositionGeo?.Dispose();
            lightPositionGeo = null;

            billboardNode?.Dispose();
            billboardNode = null;

            if (viewFrustumSplitGeos != null)
            {
                for (int i = 0; i < cascadeCount; i++)
                {
                    viewFrustumSplitGeos[i]?.Dispose();
                }
            }
            if (viewFrustumSphereSplitGeos != null)
            {
                for (int i = 0; i < cascadeCount; i++)
                {
                    viewFrustumSphereSplitGeos[i]?.Dispose();
                }
            }
            if (lightRegionSplitGeos != null)
            {
                for (int i = 0; i < cascadeCount; i++)
                {
                    lightRegionSplitGeos[i]?.Dispose();
                }
            }
        }

    }
}
