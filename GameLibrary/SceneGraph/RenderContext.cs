using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Common;
using GameLibrary.SceneGraph.Bounding;
using System.ComponentModel;
using GameLibrary.Component;
using GameLibrary.Component.Camera;
using System.Linq;

namespace GameLibrary.SceneGraph
{

    public sealed class RenderContext
    {
        #region Properties

        [Category("Camera")]
        public Camera Camera
        {
            get { return camera; }
        }

        [Category("Camera")]
        public Camera RenderCamera
        {
            get { return renderCamera; }
        }

        // Frustum culling
        [Category("Culling")]
        public bool FrustumCullingEnabled
        {
            get { return (frustumCullingEnabled && (frustumCullingOwner == 0)); }
            set { frustumCullingEnabled = value; RequestRedraw(); }
        }

        [Category("Culling")]
        public bool DistanceCullingEnabled
        {
            get { return (distanceCullingEnabled && (distanceCullingOwner == 0)); }
            set { distanceCullingEnabled = value; RequestRedraw(); }
        }

        [Category("Culling")]
        public float CullDistance
        {
            get { return cullDistance; }
            set { cullDistance = value; cullDistanceSquared = cullDistance * cullDistance; RequestRedraw(); }
        }

        [Category("Culling")]
        public bool ScreenSizeCullingEnabled
        {
            get { return (screenSizeCullingEnabled && (screenSizeCullingOwner == 0)); }
            set { screenSizeCullingEnabled = value; RequestRedraw(); }
        }

        // flags
        [Category("Flags")]
        public bool Debug { get; set; }

        [Category("Flags")]
        public bool AddBoundingGeometry
        {
            get { return addBoundingGeometry; }
            set { addBoundingGeometry = value; RequestRedraw(); }
        }

        [Category("Flags")]
        public bool ShowBoundingVolumes
        {
            get { return showBoundingVolumes; }
            set { showBoundingVolumes = value; RequestRedraw(); }
        }

        [Category("Flags")]
        public bool ShowCulledBoundingVolumes
        {
            get { return showCulledBoundingVolumes; }
            set { showCulledBoundingVolumes = value; RequestRedraw(); }
        }

        [Category("Flags")]
        public bool ShowCollisionVolumes
        {
            get { return showCollisionVolumes; }
            set { showCollisionVolumes = value; RequestRedraw(); }
        }

        // stats
        [Category("Stats")]
        [ReadOnly(true), Browsable(true)]
        public int FrustumCullCount { get; set; }

        [Category("Stats")]
        [ReadOnly(true), Browsable(true)]
        public int DistanceCullCount { get; set; }

        [Category("Stats")]
        [ReadOnly(true), Browsable(true)]
        public int ScreenSizeCullCount { get; set; }

        #endregion

        private bool frustumCullingEnabled;
        internal ulong frustumCullingOwner;

        private bool distanceCullingEnabled;
        internal ulong distanceCullingOwner;
        public float cullDistance;
        public float cullDistanceSquared;

        private bool screenSizeCullingEnabled;
        internal ulong screenSizeCullingOwner;

        // flags
        private bool addBoundingGeometry;
        private bool showBoundingVolumes;
        private bool showCulledBoundingVolumes;
        private bool showCollisionVolumes;

        //public bool dirty;
        private bool drawRequested;

        public GameTime GameTime;

        public readonly GraphicsDevice GraphicsDevice;

        private Camera camera;
        private readonly Camera renderCamera;

        // camera
        public int VisitOrder;

        public Vector3 sceneMax;
        public Vector3 sceneMin;

        // render bins
        public readonly SortedDictionary<int, List<Drawable>> renderBins;
        public int DrawableCount;

        // lights
        public readonly List<LightNode> lightNodes;
        public readonly List<RenderContext> lightRenderContextes;
        public bool Light;

        // render target
        public Color ClearColor;
        public readonly RenderTarget2D RenderTarget;

        // stats
        public int DrawCount;
        public int VertexCount;

        public RenderContext(GraphicsDevice graphicsDevice, Camera camera) : this(graphicsDevice, camera, null)
        {
        }

        public RenderContext(GraphicsDevice graphicsDevice, Camera camera, RenderTarget2D renderTarget)
        {
            GraphicsDevice = graphicsDevice;

            this.camera = camera;
            this.renderCamera = camera;

            RenderTarget = renderTarget;

            frustumCullingEnabled = true;
            distanceCullingEnabled = false;
            screenSizeCullingEnabled = false;

            frustumCullingOwner = 0;
            distanceCullingOwner = 0;
            screenSizeCullingOwner = 0;

            // distance culling
            cullDistance = 512;
            cullDistanceSquared = cullDistance * cullDistance;

            // flags
            Debug = false;
            addBoundingGeometry = false;
            showBoundingVolumes = false;
            showCulledBoundingVolumes = false;
            showCollisionVolumes = false;

            // state
            renderBins = new SortedDictionary<int, List<Drawable>>();

            lightNodes = new List<LightNode>(1);
            lightRenderContextes = new List<RenderContext>(1);

            Light = false;

            ClearColor = Color.CornflowerBlue;

            renderTarget = null;
        }

        public bool RedrawRequested()
        {
            return (drawRequested || renderBins.Count == 0);
        }

        public void RequestRedraw()
        {
            drawRequested = true;
        }

        public void ClearRedrawRequested()
        {
            drawRequested = false;
        }

        public void Clear()
        {
            ClearBins();

            lightNodes.Clear();
            foreach (RenderContext context in lightRenderContextes)
            {
                context.Clear();
            }

            sceneMax = new Vector3(float.MinValue);
            sceneMin = new Vector3(float.MaxValue);
        }

        public void AddLightNode(LightNode lightNode)
        {
            lightNodes.Add(lightNode);

            int index = lightNodes.Count - 1;
            if (index >= lightRenderContextes.Count)
            {
                Console.WriteLine("Creating light render context");
                int renderTargetSize = 2048;
                int cascadeCount = 1;
                RenderTarget2D renderTarget = new RenderTarget2D(
                    GraphicsDevice,
                    renderTargetSize, renderTargetSize,
                    false,
                    SurfaceFormat.Single, DepthFormat.Depth24,
                    0,
                    RenderTargetUsage.DiscardContents,
                    false,
                    cascadeCount);

                Camera lightCamera = new LightCamera();
                RenderContext rc = new RenderContext(GraphicsDevice, lightCamera, renderTarget);
                rc.Light = true;
                rc.ClearColor = Color.White;
                lightRenderContextes.Add(rc);
            }
        }

        private float savedZFar = 0;
        private bool cameraFrozen = false;

        public bool IsCameraFrozen => cameraFrozen;

        public void FreezeCamera()
        {
            if (cameraFrozen) return;
            cameraFrozen = true;

            camera = new DebugCamera(camera);

            // tweak camera zfar...
            // TODO restore zfar later...
            savedZFar = renderCamera.ZFar;
            renderCamera.ZFar = 2000;
        }

        public void UnfreezeCamera()
        {
            if (!cameraFrozen) return;
            cameraFrozen = false;

            camera = renderCamera;
            // TODO restore zfar!
            renderCamera.ZFar = savedZFar;
        }

        // TODO move elsewhere...
        public void UpdateCamera()
        {
            if (cameraFrozen) return;

            // compute visit order based on view direction
            Vector3 dir = Camera.ViewDirection;
            VisitOrder = VectorUtil.visitOrder(dir);
        }

        public void ResetStats()
        {
            // culli
            FrustumCullCount = 0;
            DistanceCullCount = 0;
            ScreenSizeCullCount = 0;

            // draw
            DrawCount = 0;
            VertexCount = 0;

            foreach (RenderContext context in lightRenderContextes)
            {
                context.ResetStats();
            }
        }

        public void ShowStats(String name)
        {
            Console.WriteLine(name + ": " + DrawCount + " (" + VertexCount + ")");
            int i = 1;
            foreach (RenderContext context in lightRenderContextes)
            {
                context.ShowStats("Light #" + i++);
            }
        }

        public Vector2 ProjectToScreen2(ref Vector3 vector)
        {
            Vector3 p = GraphicsDevice.Viewport.Project(vector, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);
            return new Vector2(p.X, p.Y);
        }

        public Vector2 ProjectToScreen(ref Vector3 vector)
        {
            // http://www.scratchapixel.com/lessons/3d-basic-rendering/perspective-and-orthographic-projection-matrix/projection-matrices-what-you-need-to-know-first

            Matrix matrix = camera.ViewProjectionMatrix;

            Vector2 v;
            v.X = (vector.X * matrix.M11) + (vector.Y * matrix.M21) + (vector.Z * matrix.M31) + matrix.M41;
            v.Y = (vector.X * matrix.M12) + (vector.Y * matrix.M22) + (vector.Z * matrix.M32) + matrix.M42;
            float W = (vector.X * matrix.M14) + (vector.Y * matrix.M24) + (vector.Z * matrix.M34) + matrix.M44;
            v.X /= W;
            v.Y /= W;

            // FIXME cache x, y, w, h ?
            int x = GraphicsDevice.Viewport.X;
            int y = GraphicsDevice.Viewport.Y;
            int width = GraphicsDevice.Viewport.Width;
            int height = GraphicsDevice.Viewport.Height;
            v.X = (((v.X + 1f) * 0.5f) * width) + x;
            v.Y = (((-v.Y + 1f) * 0.5f) * height) + y;
            return v;
        }

        public void AddToBin(Drawable drawable)
        {
            AddToBin(drawable.RenderGroupId, drawable);
        }

        public void AddToBin(int binId, Drawable drawable)
        {
            if (binId < 0)
            {
                return;
            }
            List<Drawable> list;
            if (!renderBins.TryGetValue(binId, out list))
            {
                list = new List<Drawable>();
                renderBins[binId] = list;
                list.Add(drawable);
                DrawableCount += list.Count;
                return;
            }
            if (Debug && list.Contains(drawable))
            {
                throw new Exception("Drawable already in group " + binId);
            }
            list.Add(drawable);
            DrawableCount += list.Count;
        }

        // FIXME Slow...!!!
        public void AddToBin(int binId, List<Drawable> drawableList)
        {
            if (binId < 0)
            {
                return;
            }
            List<Drawable> list;
            if (!renderBins.TryGetValue(binId, out list))
            {
                list = new List<Drawable>(drawableList);
                renderBins[binId] = list;
                DrawableCount += list.Count;
                return;
            }
            foreach (Drawable drawable in drawableList)
            {
                // FIXME SLOW !!!
                if (!list.Contains(drawable))
                {
                    list.Add(drawable);
                    DrawableCount++;
                }
            }
        }

        public void ClearBins()
        {
            foreach (KeyValuePair<int, List<Drawable>> drawableListKVP in renderBins)
            {
                List<Drawable> drawableList = drawableListKVP.Value;
                drawableList.Clear();
            }
            DrawableCount = 0;
        }

        public void AddBoundingVolume(Drawable drawable, bool culled)
        {
            AddBoundingVolume(drawable, drawable.BoundingVolume.GetBoundingType(), culled);
        }

        public void AddBoundingVolume(Drawable drawable, BoundingType boundingType, bool culled)
        {
            Boolean collided = false;
            if (ShowCollisionVolumes)
            {
                // collided = (collisionCache != null) ? collisionCache.ContainsKey(drawable.Id) : false;
            }
            if (!culled)
            {
                if (collided)
                {
                    AddToBin((boundingType == BoundingType.Sphere) ? Scene.COLLISION_SPHERE : Scene.COLLISION_BOX, drawable);
                }
                else if (ShowBoundingVolumes)
                {
                    AddToBin((boundingType == BoundingType.Sphere) ? Scene.BOUNDING_SPHERE : Scene.BOUNDING_BOX, drawable);
                }
            }
            else
            {
                // handle culled Drawables
                if (ShowCulledBoundingVolumes)
                {
                    AddToBin((boundingType == BoundingType.Sphere) ? Scene.CULLED_BOUNDING_SPHERE : Scene.CULLED_BOUNDING_BOX, drawable);
                }
            }
        }

    }
}
