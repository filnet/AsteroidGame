using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Common;
using System.ComponentModel;
using GameLibrary.Component.Camera;

namespace GameLibrary.SceneGraph
{
    public sealed class SceneRenderContext : RenderContext
    {
        #region Properties

        public enum CameraMode { Default, FreezeRender, FreezeCull, LightRender, LightCull }

        public int LightCount
        {
            get { return lightNodes.Count; }
        }

        public LightRenderContext LightRenderContext(int index)
        {
            return lightRenderContextes[index];
        }

        public RefractionRenderContext RefractionRenderContext(int index)
        {
            return refractionRenderContextes[index];
        }

        public ReflectionRenderContext ReflectionRenderContext(int index)
        {
            return reflectionRenderContextes[index];
        }

        [Category("Camera")]
        public bool ShadowsEnabled
        {
            get { return shadowsEnabled; }
            set { shadowsEnabled = value; RequestRedraw(); }
        }

        [Category("Debug")]
        public CameraMode Mode
        {
            get { return cameraMode; }
            set { SetCameraMode(value); }
        }

        [Category("Debug Physics")]
        public bool ShowCollisionVolumes
        {
            get { return showCollisionVolumes; }
            set { showCollisionVolumes = value; RequestRedraw(); }
        }

        #endregion

        // lights
        private bool shadowsEnabled;
        private readonly List<LightNode> lightNodes;
        private readonly List<LightRenderContext> lightRenderContextes;

        // refraction
        private readonly List<RefractionRenderContext> refractionRenderContextes;

        // reflection
        private readonly List<ReflectionRenderContext> reflectionRenderContextes;

        // debugging
        private bool showCollisionVolumes;

        private CameraMode cameraMode = CameraMode.Default;
        private float savedZFar = 0;

        public SceneRenderContext(GraphicsDevice graphicsDevice, Camera camera) : base("VIEW", graphicsDevice, camera)
        {
            shadowsEnabled = true;
            lightNodes = new List<LightNode>(1);
            lightRenderContextes = new List<LightRenderContext>(1);

            refractionRenderContextes = new List<RefractionRenderContext>(1);

            reflectionRenderContextes = new List<ReflectionRenderContext>(1);

            DebugGeometryUpdate();
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (RenderContext context in lightRenderContextes)
            {
                context.Dispose();
            }
            foreach (RenderContext context in refractionRenderContextes)
            {
                context.Dispose();
            }
            foreach (RenderContext context in reflectionRenderContextes)
            {
                context.Dispose();
            }
            DebugGeometryDispose();
        }

        public override void SetupGraphicsDevice()
        {
            GraphicsDevice.SetRenderTargets(null);
            GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);
        }

        public override void Clear()
        {
            base.Clear();

            lightNodes.Clear();
            foreach (RenderContext context in lightRenderContextes)
            {
                context.Clear();
            }
            foreach (RenderContext context in refractionRenderContextes)
            {
                context.Clear();
            }
            foreach (RenderContext context in reflectionRenderContextes)
            {
                context.Clear();
            }
        }

        public override bool RedrawRequested()
        {
            if (base.RedrawRequested())
            {
                return true;
            }
            // FIXME performance: most fequent case (i.e. no request) is the worst case scenario...
            foreach (RenderContext context in lightRenderContextes)
            {
                if (context.RedrawRequested())
                {
                    return true;
                }
            }
            foreach (RenderContext context in refractionRenderContextes)
            {
                if (context.RedrawRequested())
                {
                    return true;
                }
            }
            foreach (RenderContext context in reflectionRenderContextes)
            {
                if (context.RedrawRequested())
                {
                    return true;
                }
            }
            return false;
        }

        public override void ClearRedrawRequested()
        {
            base.ClearRedrawRequested();
            foreach (RenderContext context in lightRenderContextes)
            {
                context.ClearRedrawRequested();
            }
            foreach (RenderContext context in refractionRenderContextes)
            {
                context.ClearRedrawRequested();
            }
            foreach (RenderContext context in reflectionRenderContextes)
            {
                context.ClearRedrawRequested();
            }
        }

        public void AddLightNode(LightNode lightNode)
        {
            lightNodes.Add(lightNode);

            LightRenderContext lightRenderContext;
            int index = lightNodes.Count - 1;
            if (index >= lightRenderContextes.Count)
            {
                Console.WriteLine("Creating light render context");
                Vector3 lightDirection = -lightNode.Translation;
                lightDirection.Normalize();
                LightCamera lightCamera = new LightCamera(lightDirection);

                lightRenderContext = new LightRenderContext(GraphicsDevice, lightCamera);
                lightRenderContext.ShowShadowMap = true;

                lightRenderContextes.Add(lightRenderContext);

            }
            else
            {
                lightRenderContext = lightRenderContextes[index];
            }
            lightRenderContext.FitToViewStable(this);
        }

        public void AddRefraction()
        {
            if (0 >= refractionRenderContextes.Count)
            {
                Console.WriteLine("Creating refraction render context");

                RefractionRenderContext refractionRenderContext = new RefractionRenderContext(GraphicsDevice, CullCamera);
                refractionRenderContext.Enabled = false;
                //refractionRenderContext.ShowMap = true;

                refractionRenderContextes.Add(refractionRenderContext);
            }
        }

        public void AddReflection()
        {
            if (0 >= reflectionRenderContextes.Count)
            {
                Console.WriteLine("Creating reflection render context");

                ReflectionRenderContext reflectionRenderContext = new ReflectionRenderContext(GraphicsDevice, CullCamera);
                reflectionRenderContext.Enabled = true;
                //reflectionRenderContext.ShowMap = true;

                reflectionRenderContextes.Add(reflectionRenderContext);
            }
        }

        private Matrix previousViewProjectionMatrix = Matrix.Identity;

        public bool CameraDirty()
        {
            if (!previousViewProjectionMatrix.Equals(camera.ViewProjectionMatrix))
            {
                previousViewProjectionMatrix = camera.ViewProjectionMatrix;
                return true;
            }
            return false;
        }

        public void SetCameraMode(CameraMode mode)
        {
            if (mode == cameraMode)
            {
                return;
            }
            // FIXME some mode switches are buggy and won't restore zfae
            switch (mode)
            {
                case CameraMode.FreezeCull:
                    // freeze cull camera
                    cullCamera = new DebugCamera(camera);

                    // tweak camera zfar...
                    // this is needed to make sure the render camera "sees" the cull camera
                    savedZFar = camera.ZFar;
                    camera.ZFar = 2000;
                    renderCamera = camera;
                    break;
                case CameraMode.FreezeRender:
                    // freeze render camera
                    renderCamera = new DebugCamera(camera);

                    // unfreeze cull camera
                    if (cameraMode == CameraMode.FreezeCull)
                    {
                        // restore zfar
                        camera.ZFar = savedZFar;
                        // move camera to cull camera
                        camera.LookAt(cullCamera.Position, cullCamera.Position + cullCamera.ViewDirection, Vector3.Up);
                    }
                    cullCamera = camera;
                    break;
                case CameraMode.LightRender:
                    cullCamera = camera;
                    renderCamera = LightRenderContext(0).RenderCamera;
                    ShowFrustum = true;
                    LightRenderContext(0).ShowFrustum = true;
                    LightRenderContext(0).ShowViewFrustumHull = true;
                    LightRenderContext(0).ViewSplitsMode = ViewSplitsMode.None;
                    //savedZFar = camera.ZFar;
                    //renderCamera.ZFar = 2000;
                    break;
                case CameraMode.LightCull:
                    cullCamera = camera;
                    renderCamera = LightRenderContext(0).CullCamera;
                    break;
                case CameraMode.Default:
                default:
                    if (cameraMode == CameraMode.FreezeCull)
                    {
                        // restore zfar
                        camera.ZFar = savedZFar;
                    }

                    renderCamera = camera;
                    cullCamera = camera;
                    break;
            }
            cameraMode = mode;
            DebugGeometryUpdate();
        }

        public override void ResetStats()
        {
            base.ResetStats();
            foreach (RenderContext context in lightRenderContextes)
            {
                context.ResetStats();
            }
            foreach (RenderContext context in refractionRenderContextes)
            {
                context.ResetStats();
            }
            foreach (RenderContext context in reflectionRenderContextes)
            {
                context.ResetStats();
            }
        }

        public void ShowStats()
        {
            ShowStats("Scene");
        }

        protected internal override void ShowStats(String name)
        {
            base.ShowStats(name);
            int i = 1;
            foreach (RenderContext context in lightRenderContextes)
            {
                context.ShowStats("Light #" + i++);
            }
            i = 1;
            foreach (RenderContext context in refractionRenderContextes)
            {
                context.ShowStats("Refraction #" + i++);
            }
            i = 1;
            foreach (RenderContext context in reflectionRenderContextes)
            {
                context.ShowStats("Reflection #" + i++);
            }
        }

        protected override void AddToRenderBin(RenderBin renderBin, Drawable drawable)
        {
            base.AddToRenderBin(renderBin, drawable);
            // HACK
            if (renderBin.Id >= Scene.DEBUG)
            {
                return;
            }
            if (ShadowsEnabled)
            {
                for (int i = 0; i < LightCount; i++)
                {
                    LightRenderContext lightRenderContext = LightRenderContext(i);
                    if (lightRenderContext.Enabled)
                    {
                        lightRenderContext.AddShadowReceiver(renderBin, drawable);
                    }
                }
            }
        }

        public override void DebugGeometryAddTo(RenderContext renderContext)
        {
            base.DebugGeometryAddTo(renderContext);
            foreach (RenderContext context in lightRenderContextes)
            {
                context.DebugGeometryAddTo(renderContext);
            }
            foreach (RenderContext context in refractionRenderContextes)
            {
                context.DebugGeometryAddTo(renderContext);
            }
            foreach (RenderContext context in reflectionRenderContextes)
            {
                context.DebugGeometryAddTo(renderContext);
            }
        }

        protected override internal void DebugGeometryUpdate()
        {
            base.DebugGeometryUpdate();
            foreach (RenderContext context in lightRenderContextes)
            {
                context.DebugGeometryUpdate();
            }
            foreach (RenderContext context in refractionRenderContextes)
            {
                context.DebugGeometryUpdate();
            }
            foreach (RenderContext context in reflectionRenderContextes)
            {
                context.DebugGeometryUpdate();
            }
        }

        protected override internal void DebugGeometryDispose()
        {
            base.DebugGeometryDispose();
        }

    }
}