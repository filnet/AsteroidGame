﻿using GameLibrary.Component.Camera;
using GameLibrary.Geometry;
using GameLibrary.SceneGraph.Bounding;
using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace GameLibrary.SceneGraph
{

    public class RenderBin
    {
        public readonly int Id;
        public readonly List<Drawable> DrawableList;

        public RenderBin(int id)
        {
            this.Id = id;
            DrawableList = new List<Drawable>();
        }

        public virtual void Clear()
        {
            DrawableList.Clear();
        }
    }

    public abstract class RenderContext
    {
        #region Properties

        [Category("Camera")]
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; RequestRedraw(); }
        }

        [Category("Camera")]
        public Camera RenderCamera
        {
            get { return renderCamera; }
        }

        [Category("Camera")]
        public virtual Camera CullCamera
        {
            get { return cullCamera; }
        }

        // Frustum culling
        [Category("Culling")]
        public bool FrustumCullingEnabled
        {
            get { return frustumCullingEnabled; }
            set { frustumCullingEnabled = value; RequestRedraw(); }
        }

        [Category("Culling")]
        public bool DistanceCullingEnabled
        {
            get { return distanceCullingEnabled; }
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
            get { return (screenSizeCullingEnabled /*&& (screenSizeCullingOwner == 0)*/); }
            set { screenSizeCullingEnabled = value; RequestRedraw(); }
        }

        // flags
        //[Category("Debug")]
        //public bool Debug { get; set; }

        [Category("Debug Culling")]
        public bool ShowBoundingVolumes
        {
            get { return showBoundingVolumes; }
            set { showBoundingVolumes = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Culling")]
        public bool ShowCulledBoundingVolumes
        {
            get { return showCulledBoundingVolumes; }
            set { showCulledBoundingVolumes = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Culling")]
        public bool ShowSceneBoundingBox
        {
            get { return showSceneBoundingBox; }
            set { showSceneBoundingBox = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Frustum")]
        public virtual bool ShowFrustum
        {
            get { return showFrustum; }
            set { showFrustum = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Frustum")]
        public virtual bool ShowFrustumHull
        {
            get { return showFrustumHull; }
            set { showFrustumHull = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Frustum")]
        public virtual bool ShowFrustumBoundingSphere
        {
            get { return showFrustumBoundingSphere; }
            set { showFrustumBoundingSphere = value; DebugGeometryUpdate(); }
        }

        [Category("Debug Frustum")]
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

        #region Fields

        private readonly string name;

        // shadows
        private bool enabled;

        internal ulong cullingOwner;

        internal bool frustumCullingEnabled;
        internal ulong frustumCullingOwner;

        internal bool distanceCullingEnabled;
        internal ulong distanceCullingOwner;
        public float cullDistance;
        public float cullDistanceSquared;

        internal bool screenSizeCullingEnabled;
        //internal ulong screenSizeCullingOwner;

        // flags
        private bool showBoundingVolumes;
        private bool showCulledBoundingVolumes;

        private bool showFrustum;
        private bool showFrustumBoundingSphere;
        private bool showFrustumBoundingBox;

        private bool showFrustumHull;

        private bool showSceneBoundingBox;

        private MeshNode frustumGeo;
        private MeshNode frustumHullGeo;
        private MeshNode frustumBoundingBoxGeo;
        private MeshNode frustumBoundingSphereGeo;

        private MeshNode sceneBoundingBoxGeo;

        private bool drawRequested;

        public GameTime GameTime;

        public readonly GraphicsDevice GraphicsDevice;

        // user controllable camera
        protected readonly Camera camera;

        // cameras
        protected Camera renderCamera;
        protected Camera cullCamera;

        public Vector3 sceneMax;
        public Vector3 sceneMin;

        private readonly Bounding.Box sceneBoundingBox = new Bounding.Box();

        // render bins
        public readonly SortedDictionary<int, RenderBin> renderBins;

        // render stats
        public int DrawCount;
        public int VertexCount;

        #endregion

        public RenderContext(string name, GraphicsDevice graphicsDevice, Camera camera)
        {
            this.name = name;
            GraphicsDevice = graphicsDevice;

            enabled = true;

            this.camera = camera;
            renderCamera = camera;
            cullCamera = camera;

            frustumCullingEnabled = true;
            distanceCullingEnabled = false;
            screenSizeCullingEnabled = false;

            frustumCullingOwner = 0;
            distanceCullingOwner = 0;
            //screenSizeCullingOwner = 0;

            // distance culling
            cullDistance = 512;
            cullDistanceSquared = cullDistance * cullDistance;

            // state
            renderBins = new SortedDictionary<int, RenderBin>();

            RequestRedraw();
        }

        public string Name
        {
            get { return name; }
        }

        public virtual void Dispose()
        {
            DebugGeometryDispose();
        }

        public abstract void SetupGraphicsDevice();

        public virtual void Clear()
        {
            ClearBins();
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

        private int previousVisitOrder = 0;

        public bool CameraVisitOrderDirty()
        {
            if (previousVisitOrder != camera.VisitOrder)
            {
                previousVisitOrder = camera.VisitOrder;
                return true;
            }
            return false;
        }

        public virtual bool RedrawRequested()
        {
            return drawRequested;
        }

        public virtual void RequestRedraw()
        {
            drawRequested = true;
        }

        public virtual void ClearRedrawRequested()
        {
            drawRequested = false;
        }

        public virtual void CullBegin()
        {
            sceneMin = new Vector3(float.MaxValue);
            sceneMax = new Vector3(float.MinValue);
        }

        public virtual void CullEnd()
        {
            // visible bounding box includes "whole" chunks
            // so we need to intersect with Frustum to get a tighter visible bounding box
            // FIXME should be done in RenderContext
            Bounding.Box.CreateFromMinMax(ref sceneMin, ref sceneMax, sceneBoundingBox);
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
            Console.WriteLine(name);
            //Console.WriteLine("  Frustum cull count : {0}", FrustumCullCount);
            //Console.WriteLine("  Distance cull count : {0}", FrustumCullCount);
            //Console.WriteLine("  Frustum cull count : {0}", FrustumCullCount);
            Console.WriteLine("  Draw count : {0}", DrawCount);
            Console.WriteLine("  Vertices count : {0}", VertexCount);
        }

        public Vector2 ProjectToScreen2(ref Vector3 vector)
        {
            Vector3 p = GraphicsDevice.Viewport.Project(vector, CullCamera.ProjectionMatrix, CullCamera.ViewMatrix, Matrix.Identity);
            return new Vector2(p.X, p.Y);
        }

        public Vector2 ProjectToScreen(ref Vector3 vector)
        {
            return ProjectToScreen(ref vector, GraphicsDevice.Viewport);
        }

        public Vector2 ProjectToScreen(ref Vector3 vector, Viewport viewport)
        {
            // http://www.scratchapixel.com/lessons/3d-basic-rendering/perspective-and-orthographic-projection-matrix/projection-matrices-what-you-need-to-know-first

            Matrix matrix = CullCamera.ViewProjectionMatrix;

            Vector2 v;
            v.X = (vector.X * matrix.M11) + (vector.Y * matrix.M21) + (vector.Z * matrix.M31) + matrix.M41;
            v.Y = (vector.X * matrix.M12) + (vector.Y * matrix.M22) + (vector.Z * matrix.M32) + matrix.M42;
            float w = (vector.X * matrix.M14) + (vector.Y * matrix.M24) + (vector.Z * matrix.M34) + matrix.M44;
            v.X /= w;
            v.Y /= w;

            // FIXME cache x, y, w, h ?
            int x = viewport.X;
            int y = viewport.Y;
            int width = viewport.Width;
            int height = viewport.Height;
            v.X = (((1.0f + v.X) * 0.5f) * width) + x;
            v.Y = (((1.0f - v.Y) * 0.5f) * height) + y;
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
            RenderBin renderBin;
            if (!renderBins.TryGetValue(binId, out renderBin))
            {
                renderBin = CreateRenderBin(binId);
                renderBins[binId] = renderBin;
            }
            AddToRenderBin(renderBin, drawable);
        }

        // FIXME Slow...!!!
        public void AddToBin(int binId, List<Drawable> drawableList)
        {
            if (binId < 0)
            {
                return;
            }
            RenderBin renderBin;
            if (!renderBins.TryGetValue(binId, out renderBin))
            {
                renderBin = CreateRenderBin(binId);
                renderBins[binId] = renderBin;
            }
            // FIXME SLOW !!!
            foreach (Drawable drawable in drawableList)
            {
                AddToRenderBin(renderBin, drawable);
            }
        }

        protected RenderBin GetRenderBin(int binId)
        {
            RenderBin renderBin;
            renderBins.TryGetValue(binId, out renderBin);
            return renderBin;
        }


        protected virtual RenderBin CreateRenderBin(int binId)
        {
            return new RenderBin(binId);
        }

        protected virtual void AddToRenderBin(RenderBin renderBin, Drawable drawable)
        {
            Debug.Assert(!renderBin.DrawableList.Contains(drawable));
            renderBin.DrawableList.Add(drawable);
        }

        public void ClearBins()
        {
            foreach (KeyValuePair<int, RenderBin> drawableListKVP in renderBins)
            {
                RenderBin renderBin = drawableListKVP.Value;
                renderBin.Clear();
            }
        }

        public void AddBoundingVolume(Drawable drawable, bool culled)
        {
            AddBoundingVolume(drawable, drawable.BoundingVolume.Type(), culled);
        }

        public void AddBoundingVolume(Drawable drawable, VolumeType boundingType, bool culled)
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
                    AddToBin((boundingType == VolumeType.Sphere) ? Scene.COLLISION_SPHERE : Scene.COLLISION_BOX, drawable);
                }
                else if (showBoundingVolumes)
                {
                    AddToBin((boundingType == VolumeType.Sphere) ? Scene.BOUNDING_SPHERE : Scene.BOUNDING_BOX, drawable);
                }
            }
            else
            {
                // handle culled Drawables
                if (showCulledBoundingVolumes)
                {
                    AddToBin((boundingType == VolumeType.Sphere) ? Scene.CULLED_BOUNDING_SPHERE : Scene.CULLED_BOUNDING_BOX, drawable);
                }
            }
        }

        public virtual void DebugGeometryAddTo(RenderContext renderContext)
        {
            if (ShowFrustum)
            {
                // cull frustum
                // FIXME geometry node is rebuilt on each update!
                frustumGeo?.Dispose();
                frustumGeo = null;

                frustumGeo = GeometryUtil.CreateFrustum(GenerateName("CULL_FRUSTUM"), CullCamera.Frustum);
                frustumGeo.Initialize(GraphicsDevice);
            }
            if (ShowFrustumHull)
            {
                // cull frustum hull
                // FIXME geometry node is rebuilt on each update!
                frustumHullGeo?.Dispose();
                frustumHullGeo = null;

                Vector3 cameraPosition = renderCamera.Position;
                Bounding.Frustum frustum = cullCamera.Frustum;
                Vector3[] hull = frustum.HullCorners(ref cameraPosition);
                if (hull.Length > 0)
                {
                    frustumHullGeo = new MeshNode(GenerateName("CULL_FRUSTUM_HULL"), new LineMeshFactory(hull, true));
                    frustumHullGeo.Initialize(GraphicsDevice);
                }
                else
                {
                    Console.WriteLine("!!! NO HULL !!!");
                }
            }
            if (ShowFrustum && frustumGeo != null)
            {
                // cull frustum
                renderContext.AddToBin(Scene.FRUSTUM, frustumGeo);
            }
            if (ShowFrustumHull && frustumHullGeo != null)
            {
                // cull frustum hull
                renderContext.AddToBin(Scene.BOUNDING_HULL, frustumHullGeo);
            }
            if (ShowFrustumBoundingSphere && frustumBoundingSphereGeo != null)
            {
                // cull frustum bounding sphere
                renderContext.AddToBin(Scene.BOUNDING_SPHERE, frustumBoundingSphereGeo);
            }
            if (ShowFrustumBoundingBox && frustumBoundingBoxGeo != null)
            {
                // cull frustum bounding box
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
            if (ShowFrustumBoundingSphere)
            {
                // cull frustum bounding sphere
                if (frustumBoundingSphereGeo == null)
                {
                    frustumBoundingSphereGeo = GeometryUtil.CreateGeodesicWF(GenerateName("CULL_FRUSTUM_BOUNDING_SPHERE"), 1);
                    frustumBoundingSphereGeo.Initialize(GraphicsDevice);
                    frustumBoundingSphereGeo.BoundingVolume = CullCamera.BoundingSphere;
                    frustumBoundingSphereGeo.WorldBoundingVolume = CullCamera.BoundingSphere;
                }
            }
            if (ShowFrustumBoundingBox)
            {
                // cull frustum bounding box
                if (frustumBoundingBoxGeo == null)
                {
                    frustumBoundingBoxGeo = GeometryUtil.CreateCubeWF(GenerateName("CULL_FRUSTUM_BOUNDING_BOX"), 1);
                    frustumBoundingBoxGeo.Initialize(GraphicsDevice);
                    frustumBoundingBoxGeo.BoundingVolume = CullCamera.BoundingBox;
                    frustumBoundingBoxGeo.WorldBoundingVolume = CullCamera.BoundingBox;
                }
            }
            if (ShowSceneBoundingBox)
            {
                // scene bounding box
                if (sceneBoundingBoxGeo == null)
                {
                    sceneBoundingBoxGeo = GeometryUtil.CreateCubeWF(GenerateName("SCENE_BOUNDING_BOX"), 1);
                    sceneBoundingBoxGeo.Initialize(GraphicsDevice);
                    sceneBoundingBoxGeo.BoundingVolume = sceneBoundingBox;
                    sceneBoundingBoxGeo.WorldBoundingVolume = sceneBoundingBox;
                }
            }
            RequestRedraw();
        }

        protected virtual internal void DebugGeometryDispose()
        {
            frustumGeo?.Dispose();
            frustumGeo = null;

            frustumHullGeo?.Dispose();
            frustumHullGeo = null;

            frustumBoundingSphereGeo?.Dispose();
            frustumBoundingSphereGeo = null;

            frustumBoundingBoxGeo?.Dispose();
            frustumBoundingBoxGeo = null;

            sceneBoundingBoxGeo?.Dispose();
            sceneBoundingBoxGeo = null;
        }

        protected string GenerateName(string name)
        {
            return this.name + "_" + name;
        }
    }
}
