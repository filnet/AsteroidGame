using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph.Common;
using GameLibrary.SceneGraph.Bounding;
using System.ComponentModel;
using GameLibrary.Component;
using GameLibrary.Component.Camera;

namespace GameLibrary.SceneGraph
{

    public sealed class RenderContext
    {
        #region Properties

        // frustrum culling
        [Category("Culling")]
        public bool FrustrumCullingEnabled
        {
            get { return (frustrumCullingEnabled && (frustrumCullingOwner == 0)); }
            set { frustrumCullingEnabled = value; RequestRedraw(); }
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

        [Category("State")]
        [ReadOnly(true), Browsable(true)]
        public int VisitOrder { get; set; }

        // stats
        [Category("Stats")]
        [ReadOnly(true), Browsable(true)]
        public int DistanceCullCount { get; set; }
        [Category("Stats")]
        [ReadOnly(true), Browsable(true)]
        public int FrustumCullCount { get; set; }
        [Category("Stats")]
        [ReadOnly(true), Browsable(true)]
        public int RenderCount { get; set; }

        #endregion

        private bool frustrumCullingEnabled;
        internal ulong frustrumCullingOwner;
        public BoundingFrustum BoundingFrustum;

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

        public readonly ICamera Camera;
        //public readonly CameraComponent Camera;

        // camera
        public Matrix ViewProjectionMatrix;
        public Vector3 CameraPosition;

        public readonly SortedDictionary<int, List<Drawable>> renderBins;

        public RenderContext(GraphicsDevice graphicsDevice, ICamera camera)
        {
            GraphicsDevice = graphicsDevice;
            Camera = camera;

            frustrumCullingEnabled = true;
            distanceCullingEnabled = false;
            screenSizeCullingEnabled = false;

            frustrumCullingOwner = 0;
            distanceCullingOwner = 0;
            screenSizeCullingOwner = 0;

            // distance culling
            cullDistance = 4 * 512;
            cullDistanceSquared = cullDistance * cullDistance;

            // flags
            Debug = false;
            addBoundingGeometry = false;
            showBoundingVolumes = false;
            showCulledBoundingVolumes = false;
            showCollisionVolumes = false;

            // stats
            DistanceCullCount = 0;
            FrustumCullCount = 0;
            RenderCount = 0;

            // state
            renderBins = new SortedDictionary<int, List<Drawable>>();
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

        public void UpdateCamera()
        {
            ViewProjectionMatrix = Camera.ViewProjectionMatrix;

            // TODO performance: don't do both Matrix inverse and transpose

            // compute visit order based on view direction
            Matrix viewMatrix = Camera.ViewMatrix;

            /*
            Matrix vt = Matrix.Transpose(viewMatrix);
            Vector3 dir = vt.Forward;
            */
            Vector3 dir = Camera.ViewDirection;
            VisitOrder = VectorUtil.visitOrder(dir);
            //if (Debug) Console.WriteLine(dir + " : " + VisitOrder);

            // frustrum culling
            if (FrustrumCullingEnabled)
            {
                BoundingFrustum = Camera.BoundingFrustum;
            }

            if (DistanceCullingEnabled || ScreenSizeCullingEnabled)
            {
                /*
                Matrix vi = Matrix.Invert(viewMatrix);
                CameraPosition = vi.Translation;
                */
                CameraPosition = Camera.Position;
            }
        }

        public Vector2 ProjectToScreen(ref Vector3 vector)
        {
            Vector3 p = GraphicsDevice.Viewport.Project(vector, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);
            return new Vector2(p.X, p.Y);
        }

        public Vector2 ProjectToScreen2(ref Vector3 vector)
        {
            // http://www.scratchapixel.com/lessons/3d-basic-rendering/perspective-and-orthographic-projection-matrix/projection-matrices-what-you-need-to-know-first

            ref Matrix matrix = ref ViewProjectionMatrix;

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
            }
            if (Debug && list.Contains(drawable))
            {
                throw new Exception("Node already in group " + binId);
            }
            list.Add(drawable);
        }

        public void ClearBins()
        {
            foreach (KeyValuePair<int, List<Drawable>> drawableListKVP in renderBins)
            {
                List<Drawable> drawableList = drawableListKVP.Value;
                drawableList.Clear();
            }
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
                // handle culled nodes
                if (ShowCulledBoundingVolumes)
                {
                    AddToBin((boundingType == BoundingType.Sphere) ? Scene.CULLED_BOUNDING_SPHERE : Scene.CULLED_BOUNDING_BOX, drawable);
                }
            }
        }

    }
}
