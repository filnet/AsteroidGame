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

        //[Category("Camera")]
        public override Camera CullCamera
        {
            get { return cullCamera; }
        }

        [Category("Shadows")]
        public bool ShadowsEnabled
        {
            get { return shadowsEnabled; }
            set { shadowsEnabled = value; RequestRedraw(); }
        }

        public override bool ShowFrustum
        {
            get { return cameraFrozen && base.ShowFrustum; }
        }

        public override bool ShowFrustumBoundingSphere
        {
            get { return cameraFrozen && base.ShowFrustumBoundingSphere; }
        }

        public override bool ShowFrustumBoundingBox
        {
            get { return cameraFrozen && base.ShowFrustumBoundingBox; }
        }

        [Category("Debug Light")]
        public bool ShowLightFrustum
        {
            get { return showLightFrustum; }
            set
            {
                showLightFrustum = value;
                foreach (LightRenderContext context in lightRenderContextes)
                {
                    context.ShowFrustum = value;
                }
                RequestRedraw();
            }
        }

        [Category("Debug Light")]
        public bool ShowOccludersBoundingBox
        {
            get { return showOccludersBoundingBox; }
            set
            {
                showOccludersBoundingBox = value;
                foreach (LightRenderContext context in lightRenderContextes)
                {
                    context.ShowSceneBoundingBox = value;
                }
                RequestRedraw();
            }
        }

        [Category("Debug Light")]
        public bool ShowOccluders
        {
            get { return showOccluders; }
            set { showOccluders = value; RequestRedraw(); }
        }

        [Category("Debug Shadows")]
        public bool ShowShadowMap
        {
            get { return showShadowMap; }
            set
            {
                showShadowMap = value;
                foreach (LightRenderContext context in lightRenderContextes)
                {
                    context.ShowShadowMap = value;
                }
                RequestRedraw();
            }
        }

        [Category("Debug Collisions")]
        public bool ShowCollisionVolumes
        {
            get { return showCollisionVolumes; }
            set { showCollisionVolumes = value; RequestRedraw(); }
        }

        #endregion

        private Camera cullCamera;

        // lights
        public readonly List<LightNode> lightNodes;
        public readonly List<LightRenderContext> lightRenderContextes;

        // shadows
        private bool shadowsEnabled;

        // debugging
        private bool showLightFrustum;
        private bool showOccludersBoundingBox;
        private bool showOccluders;
        private bool showShadowMap;
        private bool showCollisionVolumes;

        public bool IsCameraFrozen => cameraFrozen;

        private float savedZFar = 0;
        private bool cameraFrozen = false;

        public SceneRenderContext(GraphicsDevice graphicsDevice, Camera camera) : base(graphicsDevice, camera)
        {
            cullCamera = camera;

            lightNodes = new List<LightNode>(1);
            lightRenderContextes = new List<LightRenderContext>(1);

            shadowsEnabled = true;
            showShadowMap = false;

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

        public void AddLightNode(LightNode lightNode)
        {
            lightNodes.Add(lightNode);

            int index = lightNodes.Count - 1;
            if (index >= lightRenderContextes.Count)
            {
                Console.WriteLine("Creating light render context");
                Camera lightCamera = new LightCamera();
                LightRenderContext rc = new LightRenderContext(GraphicsDevice, lightCamera);
                rc.ShowShadowMap = showShadowMap;
                lightRenderContextes.Add(rc);
            }
        }

        public void CameraFreeze()
        {
            if (cameraFrozen) return;
            cameraFrozen = true;

            cullCamera = new DebugCamera(camera);

            // tweak camera zfar...
            savedZFar = camera.ZFar;
            camera.ZFar = 2000;
        }

        public void CameraUnfreeze()
        {
            if (!cameraFrozen) return;
            cameraFrozen = false;

            cullCamera = camera;
            // TODO restore zfar!
            camera.ZFar = savedZFar;
        }

        // TODO move elsewhere...
        public override void UpdateCamera()
        {
            if (!cameraFrozen)
            {
                base.UpdateCamera();
            }
        }

        public void CaptureFrustum()
        {
            if (!cameraFrozen)
            {
                CameraFreeze();
            }
            else
            {
                CameraUnfreeze();
            }
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
            foreach (RenderContext context in lightRenderContextes)
            {
                context.DebugGeometryAddTo(renderContext);
            }
        }

        protected override internal void DebugGeometryUpdate()
        {
            base.DebugGeometryUpdate();
            /*foreach (RenderContext context in lightRenderContextes)
            {
                context.DebugGeometryUpdate();
            }*/
        }

        protected override internal void DebugGeometryDispose()
        {
            base.DebugGeometryDispose();
        }

    }
}