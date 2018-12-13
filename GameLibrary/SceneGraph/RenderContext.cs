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
        public int FrustumCullCount { get; set; }

        [Category("Stats")]
        [ReadOnly(true), Browsable(true)]
        public int DistanceCullCount { get; set; }

        [Category("Stats")]
        [ReadOnly(true), Browsable(true)]
        public int ScreenSizeCullCount { get; set; }

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

        public readonly Camera Camera;

        // camera
        public Matrix ViewProjectionMatrix;
        public Vector3 CameraPosition;

        public Vector3 sceneMax;
        public Vector3 sceneMin;

        public readonly SortedDictionary<int, List<Drawable>> renderBins;

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
            Camera = camera;

            RenderTarget = renderTarget;

            frustrumCullingEnabled = true;
            distanceCullingEnabled = false;
            screenSizeCullingEnabled = false;

            frustrumCullingOwner = 0;
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
/*
        public void LightMatrices(Bounding.BoundingBox sceneBoundingBox, out Matrix view, out Matrix projection)
        {
            // Think of light's orthographic frustum as a bounding box that encloses all objects visible by the camera,
            // plus objects not visible but potentially casting shadows. For the simplicity let's disregard the latter.
            // So to find this frustum:
            // - find all objects that are inside the current camera frustum
            // - find minimal aa bounding box that encloses them all
            // - transform corners of that bounding box to the light's space (using light's view matrix)
            // - find aa bounding box in light's space of the transformed (now obb) bounding box
            // - this aa bounding box is your directional light's orthographic frustum.
            //
            // Note that actual translation component in light view matrix doesn't really matter as you'll
            // only get different Z values for the frustum but the boundaries will be the same in world space.
            // For the convenience, when building light view matrix, you can assume the light "position" is at
            // the center of the bounding box enclosing all visible objects.

            Vector3 lightPosition = sceneBoundingBox.Center;
            Vector3 lightDirection = Vector3.Normalize(new Vector3(-1, -1, -1));

            Matrix lightView = Matrix.CreateLookAt(lightPosition, lightPosition + lightDirection, Vector3.Up);

            // transform bounding box
            Matrix matrix = lightView;

            //Vector3 newCenter;
            //Vector3 v = sceneBoundingBox.Center;
            //newCenter.X = (v.X * matrix.M11) + (v.Y * matrix.M21) + (v.Z * matrix.M31) + matrix.M41;
            //newCenter.Y = (v.X * matrix.M12) + (v.Y * matrix.M22) + (v.Z * matrix.M32) + matrix.M42;
            //newCenter.Z = (v.X * matrix.M13) + (v.Y * matrix.M23) + (v.Z * matrix.M33) + matrix.M43;

            Vector3 newHalfSize;
            Vector3 v = sceneBoundingBox.HalfSize;
            newHalfSize.X = (v.X * Math.Abs(matrix.M11)) + (v.Y * Math.Abs(matrix.M21)) + (v.Z * Math.Abs(matrix.M31));
            newHalfSize.Y = (v.X * Math.Abs(matrix.M12)) + (v.Y * Math.Abs(matrix.M22)) + (v.Z * Math.Abs(matrix.M32));
            newHalfSize.Z = (v.X * Math.Abs(matrix.M13)) + (v.Y * Math.Abs(matrix.M23)) + (v.Z * Math.Abs(matrix.M33));

            //Bounding.BoundingBox bb = new Bounding.BoundingBox(newCenter, newHalfSize);

            Matrix lightProjection = Matrix.CreateOrthographic(newHalfSize.X * 2, newHalfSize.Y * 2, -newHalfSize.Z, newHalfSize.Z);

            view = lightView;
            projection = lightProjection;
        }
*/
        public void ResetStats()
        {
            // culli
            FrustumCullCount = 0;
            DistanceCullCount = 0;
            ScreenSizeCullCount = 0;

            // draw
            DrawCount = 0;
            VertexCount = 0;
        }

        public void ShowStats()
        {
            Console.WriteLine(DrawCount + " " + VertexCount);
        }

        public Vector2 ProjectToScreen2(ref Vector3 vector)
        {
            Vector3 p = GraphicsDevice.Viewport.Project(vector, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);
            return new Vector2(p.X, p.Y);
        }

        public Vector2 ProjectToScreen(ref Vector3 vector)
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
