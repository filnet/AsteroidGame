using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GameLibrary.Component.Camera;
using System;
using GameLibrary.SceneGraph.Common;
using System.ComponentModel;

namespace GameLibrary.SceneGraph
{
    public sealed class LightRenderContext : RenderContext
    {
        #region Properties

        [Category("Debug Shadows")]
        public bool ShowShadowMap
        {
            get { return showShadowMap; }
            set { showShadowMap = value; DebugGeometryUpdate(); }
        }

        #endregion

        // render target
        public readonly RenderTargetShadowCascade RenderTarget;
        private readonly int CascadeCount;

        private bool showShadowMap;

        private BillboardNode billboardNode;

        public LightRenderContext(GraphicsDevice graphicsDevice, Camera camera) : base(graphicsDevice, camera)
        {
            int renderTargetSize = 2048;
            CascadeCount = 4;

            /*RenderTarget = new RenderTargetShadowCascade(
                 GraphicsDevice,
                 renderTargetSize, renderTargetSize,
                 false,
                 SurfaceFormat.Single, DepthFormat.Depth_R32_Typeless,// DepthFormat..Depth24Stencil8,
                 0,
                 RenderTargetUsage.DiscardContents,
                 false,
                 CascadeCount);*/

            RenderTarget = new RenderTargetShadowCascade(GraphicsDevice, renderTargetSize, renderTargetSize, CascadeCount);
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

        public override void DebugGeometryAddTo(RenderContext renderContext)
        {
            base.DebugGeometryAddTo(renderContext);

            // shadow map texture
            if (showShadowMap && billboardNode != null)
            {
                billboardNode.Texture = RenderTarget;

                renderContext.AddToBin(Scene.HUD, billboardNode);
            }
        }

        protected override internal void DebugGeometryUpdate()
        {
            base.DebugGeometryUpdate();

            if (showShadowMap)
            {
                if (billboardNode == null)
                {
                    billboardNode = new BillboardNode("SHADOW_MAP");
                    billboardNode.Initialize(GraphicsDevice);
                }
            }
        }

        protected override internal void DebugGeometryDispose()
        {
            base.DebugGeometryDispose();

            billboardNode?.Dispose();
            billboardNode = null;
        }


    }
}
