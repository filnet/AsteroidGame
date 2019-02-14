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
using GameLibrary.Geometry;

namespace GameLibrary.SceneGraph
{

    public abstract class RenderContext
    {
        #region Properties

        [Category("Camera")]
        public Camera Camera
        {
            get { return camera; }
        }

        [Category("Camera")]
        public virtual Camera CullCamera
        {
            get { return camera; }
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
        [Category("Debug Camera")]
        public bool Debug { get; set; }

        [Category("Debug Camera")]
        public bool AddBoundingGeometry
        {
            get { return addBoundingGeometry; }
            set { addBoundingGeometry = value; DebugGeometryUpdate(); } 
        }

        [Category("Debug Camera")]
        public bool ShowBoundingVolumes
        {
            get { return showBoundingVolumes; }
            set { showBoundingVolumes = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Camera")]
        public bool ShowCulledBoundingVolumes
        {
            get { return showCulledBoundingVolumes; }
            set { showCulledBoundingVolumes = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Camera")]
        public bool ShowSceneBoundingBox
        {
            get { return showSceneBoundingBox; }
            set { showSceneBoundingBox = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Camera")]
        public virtual bool ShowFrustum
        {
            get { return showFrustum; }
            set { showFrustum = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Camera")]
        public virtual bool ShowFrustumBoundingSphere
        {
            get { return showFrustumBoundingSphere; }
            set { showFrustumBoundingSphere = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Camera")]
        public virtual bool ShowFrustumBoundingBox
        {
            get { return showFrustumBoundingBox; }
            set { showFrustumBoundingBox = value; DebugGeometryUpdate(); }
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

        private bool showFrustum;
        private bool showFrustumBoundingSphere;
        private bool showFrustumBoundingBox;

        private bool showSceneBoundingBox;

        private MeshNode frustumGeo;
        private MeshNode frustumBoundingBoxGeo;
        private MeshNode frustumBoundingSphereGeo;

        private MeshNode sceneBoundingBoxGeo;

        private bool drawRequested;

        public GameTime GameTime;

        public readonly GraphicsDevice GraphicsDevice;

        protected readonly Camera camera;

        // camera
        public int VisitOrder;

        public Vector3 sceneMax;
        public Vector3 sceneMin;

        public readonly Bounding.BoundingBox sceneBoundingBox = new Bounding.BoundingBox();

        // render bins
        public readonly SortedDictionary<int, List<Drawable>> renderBins;
        private int DrawableCount;

        // render stats
        public int DrawCount;
        public int VertexCount;

        public RenderContext(GraphicsDevice graphicsDevice, Camera camera)
        {
            GraphicsDevice = graphicsDevice;

            this.camera = camera;

            frustumCullingEnabled = true;
            distanceCullingEnabled = false;
            screenSizeCullingEnabled = false;

            frustumCullingOwner = 0;
            distanceCullingOwner = 0;
            screenSizeCullingOwner = 0;

            // distance culling
            cullDistance = 512;
            cullDistanceSquared = cullDistance * cullDistance;

            // state
            renderBins = new SortedDictionary<int, List<Drawable>>();
        }

        public virtual void Dispose()
        {
            DebugGeometryDispose();
        }

        public abstract void SetupGraphicsDevice();

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

        public virtual void Clear()
        {
            ClearBins();
        }

        // TODO move elsewhere...
        public virtual void UpdateCamera()
        {
            // compute visit order based on view direction
            Vector3 dir = CullCamera.ViewDirection;
            VisitOrder = VectorUtil.visitOrder(dir);
        }

        public void CullBegin()
        {
            sceneMin = new Vector3(float.MaxValue);
            sceneMax = new Vector3(float.MinValue);
        }

        public void CullEnd()
        {
            // visible bounding box includes "whole" chunks
            // so we need to intersect with Frustum to get a tighter visible bounding box
            // FIXME should be done in RenderContext
            Bounding.BoundingBox.CreateFromMinMax(sceneMin, sceneMax, sceneBoundingBox);
        }

        public virtual void ResetStats()
        {
            // culling
            FrustumCullCount = 0;
            DistanceCullCount = 0;
            ScreenSizeCullCount = 0;

            // draw
            DrawCount = 0;
            VertexCount = 0;
        }

        protected internal virtual void ShowStats(String name)
        {
            Console.WriteLine(name + ": " + DrawCount + " (" + VertexCount + ")");
        }

        public Vector2 ProjectToScreen2(ref Vector3 vector)
        {
            Vector3 p = GraphicsDevice.Viewport.Project(vector, CullCamera.ProjectionMatrix, CullCamera.ViewMatrix, Matrix.Identity);
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
            if (false /*showCollisionVolumes*/)
            {
                // collided = (collisionCache != null) ? collisionCache.ContainsKey(drawable.Id) : false;
            }
            if (!culled)
            {
                if (collided)
                {
                    AddToBin((boundingType == BoundingType.Sphere) ? Scene.COLLISION_SPHERE : Scene.COLLISION_BOX, drawable);
                }
                else if (showBoundingVolumes)
                {
                    AddToBin((boundingType == BoundingType.Sphere) ? Scene.BOUNDING_SPHERE : Scene.BOUNDING_BOX, drawable);
                }
            }
            else
            {
                // handle culled Drawables
                if (showCulledBoundingVolumes)
                {
                    AddToBin((boundingType == BoundingType.Sphere) ? Scene.CULLED_BOUNDING_SPHERE : Scene.CULLED_BOUNDING_BOX, drawable);
                }
            }
        }

        public virtual void DebugGeometryAddTo(RenderContext renderContext)
        {
            if (ShowFrustum && frustumGeo != null)
            {
                // frustum
                renderContext.AddToBin(Scene.FRUSTUM, frustumGeo);
            }
            if (ShowFrustumBoundingSphere && frustumBoundingSphereGeo != null)
            {
                // frustum bounding sphere
                renderContext.AddToBin(Scene.BOUNDING_SPHERE, frustumBoundingSphereGeo);
            }
            if (ShowFrustumBoundingBox && frustumBoundingBoxGeo != null)
            {
                // frustum bounding box
                renderContext.AddToBin(Scene.BOUNDING_BOX, frustumBoundingBoxGeo);
            }

            if (ShowSceneBoundingBox && sceneBoundingBoxGeo != null)
            {
                // scene bounding box
                renderContext.AddToBin(Scene.BOUNDING_BOX, sceneBoundingBoxGeo);
            }           
        }

        protected virtual internal void DebugGeometryUpdate()
        {
            if (ShowFrustum)
            {
                // camera frustum 
                // geometry node is rebuilt on each update!
                frustumGeo?.Dispose();
                frustumGeo = GeometryUtil.CreateFrustum("FRUSTUM", CullCamera.BoundingFrustum);
                frustumGeo.RenderGroupId = Scene.FRUSTUM;
                frustumGeo.Initialize(GraphicsDevice);
            }
            if (ShowFrustumBoundingSphere)
            {
                // camera frustum bounding sphere
                if (frustumBoundingSphereGeo == null)
                {
                    frustumBoundingSphereGeo = GeometryUtil.CreateGeodesicWF("FRUSTUM_BOUNDING_SPHERE", 1);
                    frustumBoundingSphereGeo.Initialize(GraphicsDevice);
                }
                frustumBoundingSphereGeo.BoundingVolume = CullCamera.BoundingSphere;
                frustumBoundingSphereGeo.WorldBoundingVolume = CullCamera.BoundingSphere;
            }
            if (ShowFrustumBoundingBox)
            {
                // camera frustum bounding box
                if (frustumBoundingBoxGeo == null)
                {
                    frustumBoundingBoxGeo = GeometryUtil.CreateCubeWF("FRUSTUM_BOUNDING_BOX", 1);
                    frustumBoundingBoxGeo.Initialize(GraphicsDevice);
                }

                // FIXME garbage (need to manage bb in camera (like bs)
                Bounding.BoundingBox frustumBoundingBox = new Bounding.BoundingBox();
                Vector3[] corners = new Vector3[BoundingFrustum.CornerCount];
                CullCamera.BoundingFrustum.GetCorners(corners);
                frustumBoundingBox.ComputeFromPoints(corners);

                frustumBoundingBoxGeo.BoundingVolume = frustumBoundingBox;
                frustumBoundingBoxGeo.WorldBoundingVolume = frustumBoundingBox;
            }
            if (ShowSceneBoundingBox)
            {
                // scene bounding box
                if (sceneBoundingBoxGeo == null)
                {
                    sceneBoundingBoxGeo = GeometryUtil.CreateCubeWF("SCENE_BOUNDING_BOX", 1);
                    sceneBoundingBoxGeo.Initialize(GraphicsDevice);
                }

                sceneBoundingBoxGeo.BoundingVolume = sceneBoundingBox;
                sceneBoundingBoxGeo.WorldBoundingVolume = sceneBoundingBox;
            }
            RequestRedraw();
        }

        protected virtual internal void DebugGeometryDispose()
        {
            frustumGeo?.Dispose();
            frustumGeo = null;

            frustumBoundingSphereGeo?.Dispose();
            frustumBoundingSphereGeo = null;

            frustumBoundingBoxGeo?.Dispose();
            frustumBoundingBoxGeo = null;

            sceneBoundingBoxGeo?.Dispose();
            sceneBoundingBoxGeo = null;
        }

    }
}
