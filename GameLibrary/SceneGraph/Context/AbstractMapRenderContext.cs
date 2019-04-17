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
    public abstract class AbstractMapRenderContext : RenderContext
    {
        #region Properties

        [Category("Debug")]
        public bool ShowMap
        {
            get { return showMap; }
            set { showMap = value; DebugGeometryUpdate(); }
        }

        #endregion

        // render target
        public readonly RenderTarget2D MapRenderTarget;

        // debugging
        private bool showMap;

        private MeshNode frustumGeo;
        private BillboardNode mapBillboardNode;

        public AbstractMapRenderContext(String name, GraphicsDevice graphicsDevice, Camera camera) : base(name, graphicsDevice, camera)
        {
            PresentationParameters pp = GraphicsDevice.PresentationParameters;
            if (Enabled)
            {
                MapRenderTarget = new RenderTarget2D(GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, GraphicsDevice.DisplayMode.Format, DepthFormat.Depth24);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            MapRenderTarget?.Dispose();
        }

        public override void SetupGraphicsDevice()
        {
            // Set the render target
            GraphicsDevice.SetRenderTarget(MapRenderTarget);
            GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 0, 0);
        }

        public override void DebugGeometryAddTo(RenderContext renderContext)
        {
            base.DebugGeometryAddTo(renderContext);

            if (false && ShowFrustum)
            {
                // FIXME geometry node is rebuilt on each update!
                // FIXME does not belong here...
                frustumGeo?.Dispose();
                frustumGeo = null;

                frustumGeo = GeometryUtil.CreateFrustum(GenerateName("RENDER_FRUSTUM"), RenderCamera.Frustum);
                frustumGeo.Initialize(GraphicsDevice);
            }
            if (false && ShowFrustum && frustumGeo != null)
            {
                // frustum (of renderer...)
                // FIXME does not belong here...
                renderContext.AddToBin(Scene.FRUSTUM, frustumGeo);
            }
            if (ShowMap && mapBillboardNode != null)
            {
                renderContext.AddToBin(Scene.HUD, mapBillboardNode);
            }
            if (ShowBoundingVolumes)
            {
                List<Drawable> drawableList = renderBins[Scene.BOUNDING_BOX].DrawableList;
                renderContext.AddToBin(Scene.BOUNDING_BOX, drawableList);
                drawableList = renderBins[Scene.BOUNDING_SPHERE].DrawableList;
                renderContext.AddToBin(Scene.BOUNDING_SPHERE, drawableList);
            }
            if (ShowCulledBoundingVolumes)
            {
                List<Drawable> drawableList = renderBins[Scene.CULLED_BOUNDING_BOX].DrawableList;
                renderContext.AddToBin(Scene.CULLED_BOUNDING_BOX, drawableList);
                drawableList = renderBins[Scene.CULLED_BOUNDING_SPHERE].DrawableList;
                renderContext.AddToBin(Scene.CULLED_BOUNDING_SPHERE, drawableList);
            }
        }

        protected override internal void DebugGeometryUpdate()
        {
            base.DebugGeometryUpdate();

            if (ShowMap)
            {
                if (mapBillboardNode == null)
                {
                    mapBillboardNode = new BillboardNode(GenerateName("MAP"));
                    mapBillboardNode.Initialize(GraphicsDevice);
                    mapBillboardNode.Mode = 0;
                    mapBillboardNode.Texture = MapRenderTarget;
                }
            }
        }

        protected override internal void DebugGeometryDispose()
        {
            base.DebugGeometryDispose();

            frustumGeo?.Dispose();
            frustumGeo = null;

            mapBillboardNode?.Dispose();
            mapBillboardNode = null;
        }

    }
}
