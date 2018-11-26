using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;
using GameLibrary.SceneGraph.Bounding;
using GameLibrary.Geometry.Common;
using GameLibrary.Geometry;
using GameLibrary.Voxel;
using System.ComponentModel;

namespace GameLibrary.SceneGraph
{

    public sealed class RenderContext
    {
        // camera
        public Matrix ViewProjectionMatrix;
        public Vector3 CameraPosition;

        #region Properties

        // frustrum culling
        [Category("Culling")]
        public bool FrustrumCullingEnabled { get; set; }

        [Category("Culling")]
        public bool DistanceCullingEnabled { get; set; }
        [Category("Culling")]
        public float CullDistance
        {
            get { return cullDistance; }
            set { cullDistance = value; cullDistanceSquared = cullDistance * cullDistance; }
        }

        [Category("Culling")]
        public bool ScreenSizeCullingEnabled { get; set; }

        // flags
        [Category("Flags")]
        public bool Debug { get; set; }
        [Category("Flags")]
        public bool AddBoundingGeometry { get; set; }
        [Category("Flags")]
        public bool ShowBoundingVolumes { get; set; }
        [Category("Flags")]
        public bool ShowCulledBoundingVolumes { get; set; }
        [Category("Flags")]
        public bool ShowCollisionVolumes { get; set; }

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

        public BoundingFrustum BoundingFrustum;
        public ulong FrustrumCullingOwner;

        public float cullDistance;
        public float cullDistanceSquared;
        public ulong DistanceCullingOwner;

        //public bool dirty;

        public readonly GraphicsDevice GraphicsDevice;

        public readonly ICameraComponent CameraComponent;

        public readonly SortedDictionary<int, List<Drawable>> renderBins;

        public RenderContext(GraphicsDevice graphicsDevice, ICameraComponent cameraComponent)
        {
            GraphicsDevice = graphicsDevice;
            CameraComponent = cameraComponent;

            FrustrumCullingEnabled = true;
            DistanceCullingEnabled = false;
            ScreenSizeCullingEnabled = false;

            FrustrumCullingOwner = 0;
            DistanceCullingOwner = 0;

            // distance culling
            cullDistance = 4 * 512;
            cullDistanceSquared = cullDistance * cullDistance;

            // flags
            Debug = false;
            AddBoundingGeometry = false;
            ShowBoundingVolumes = false;
            ShowCulledBoundingVolumes = false;
            ShowCollisionVolumes = false;

            // stats
            DistanceCullCount = 0;
            FrustumCullCount = 0;
            RenderCount = 0;

            // state
            renderBins = new SortedDictionary<int, List<Drawable>>();
        }

        public void UpdateCamera()
        {
            ViewProjectionMatrix = CameraComponent.ViewProjectionMatrix;

            // TODO performance: don't do both Matrix inverse and transpose

            // compute visit order based on view direction
            Matrix viewMatrix = CameraComponent.ViewMatrix;

            Matrix vt = Matrix.Transpose(viewMatrix);
            Vector3 dir = vt.Forward;
            VisitOrder = VectorUtil.visitOrder(dir);
            //if (Debug) Console.WriteLine(dir + " : " + VisitOrder);

            // frustrum culling
            if (FrustrumCullingEnabled)
            {
                BoundingFrustum = CameraComponent.BoundingFrustum;
            }

            if (DistanceCullingEnabled || ScreenSizeCullingEnabled)
            {
                Matrix vi = Matrix.Invert(viewMatrix);
                CameraPosition = vi.Translation;
            }
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
