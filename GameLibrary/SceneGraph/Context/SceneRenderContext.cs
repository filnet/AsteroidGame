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

        public enum FreezeMode { None, Render, Cull }

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
        public FreezeMode CameraFreezeMode
        {
            get { return cameraFreezeMode; }
            set { FreezeCamera(value); }
        }

        public override bool ShowFrustum
        {
            get { return (cameraFreezeMode != FreezeMode.None) && base.ShowFrustum; }
        }

        public override bool ShowFrustumBoundingSphere
        {
            get { return (cameraFreezeMode != FreezeMode.None) && base.ShowFrustumBoundingSphere; }
        }

        public override bool ShowFrustumBoundingBox
        {
            get { return (cameraFreezeMode != FreezeMode.None) && base.ShowFrustumBoundingBox; }
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

        private FreezeMode cameraFreezeMode = FreezeMode.None;
        private float savedZFar = 0;

        public SceneRenderContext(GraphicsDevice graphicsDevice, Camera camera) : base(graphicsDevice, camera)
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
            // FIXME performance: most fequent case (i.e. no request) is the worst case scenario...
            if (base.RedrawRequested())
            {
                return true;
            }
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

        public void FreezeCamera(FreezeMode mode)
        {
            if (mode == cameraFreezeMode)
            {
                return;
            }
            switch (mode)
            {
                case FreezeMode.Cull:
                    // freeze cull camera
                    cullCamera = new DebugCamera(camera);

                    // tweak camera zfar...
                    // this is needed to make sure the render camera "sees" the cull camera
                    savedZFar = camera.ZFar;
                    camera.ZFar = 2000;
                    renderCamera = camera;
                    break;
                case FreezeMode.Render:
                    // freeze render camera
                    renderCamera = new DebugCamera(camera);

                    // unfreeze cull camera
                    if (cameraFreezeMode == FreezeMode.Cull)
                    {
                        // restore zfar
                        camera.ZFar = savedZFar;
                        // move camera to cull camera
                        camera.LookAt(cullCamera.Position, cullCamera.Position + cullCamera.ViewDirection, Vector3.Up);
                    }
                    cullCamera = camera;
                    break;
                case FreezeMode.None:
                default:
                    if (cameraFreezeMode == FreezeMode.Cull)
                    {
                        // restore zfar
                        camera.ZFar = savedZFar;
                    }

                    renderCamera = camera;
                    cullCamera = camera;
                    break;
            }
            cameraFreezeMode = mode;
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