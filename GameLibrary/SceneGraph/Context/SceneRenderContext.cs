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

        public Camera LightCamera(int index)
        {
            return LightRenderContext(index).RenderCamera;
        }

        public LightRenderContext LightRenderContext(int index)
        {
            return lightRenderContextes[index];
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
        private readonly List<LightNode> lightNodes;
        private readonly List<LightRenderContext> lightRenderContextes;

        // shadows
        private bool shadowsEnabled;

        // debugging
        private bool showCollisionVolumes;

        private CameraMode cameraMode = CameraMode.Default;
        private float savedZFar = 0;

        public SceneRenderContext(GraphicsDevice graphicsDevice, Camera camera) : base("VIEW", graphicsDevice, camera)
        {
            lightNodes = new List<LightNode>(1);
            lightRenderContextes = new List<LightRenderContext>(1);

            shadowsEnabled = true;

            DebugGeometryUpdate();
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (LightRenderContext context in lightRenderContextes)
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
            foreach (LightRenderContext context in lightRenderContextes)
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
            foreach (LightRenderContext context in lightRenderContextes)
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
            foreach (LightRenderContext context in lightRenderContextes)
            {
                context.ClearRedrawRequested();
            }
        }

        public void AddLightNode(LightNode lightNode)
        {
            lightNodes.Add(lightNode);

            int index = lightNodes.Count - 1;
            if (index >= lightRenderContextes.Count)
            {
                Console.WriteLine("Creating light render context");
                LightCamera lightCamera = new LightCamera();
                lightCamera.lightDirection = -lightNode.Translation;
                lightCamera.lightDirection.Normalize();

                LightRenderContext lightRenderContext = new LightRenderContext(GraphicsDevice, lightCamera);
                lightRenderContext.ShowShadowMap = true;

                lightRenderContextes.Add(lightRenderContext);
            }
        }


        private Matrix previousViewProjectionMatrix = Matrix.Identity;

        public bool CameraDirty()
        {
            if (!previousViewProjectionMatrix.Equals(Camera.ViewProjectionMatrix))
            {
                previousViewProjectionMatrix = Camera.ViewProjectionMatrix;
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
        }

        public override void DebugGeometryAddTo(RenderContext renderContext)
        {
            base.DebugGeometryAddTo(renderContext);
            foreach (LightRenderContext context in lightRenderContextes)
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
        }

        protected override internal void DebugGeometryDispose()
        {
            base.DebugGeometryDispose();
        }

    }
}