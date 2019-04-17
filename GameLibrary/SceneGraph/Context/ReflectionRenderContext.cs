using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GameLibrary.Component.Camera;

namespace GameLibrary.SceneGraph
{
    public sealed class ReflectionRenderContext : AbstractMapRenderContext
    {
        #region Properties

        #endregion

        public ReflectionRenderContext(GraphicsDevice graphicsDevice, Camera camera) : base("WATER", graphicsDevice, camera)
        {
        }

        public void Update(Camera camera)
        {
            // FIXME cache clipPlane
            float waterZ = 0.33f;
            float offset = 0.01f;// 0.01f;
            Plane clipPlane = new Plane(Vector3.Up, waterZ /*+ offset*/);

            // TODO use clipPlane for better culling 
            cullCamera = new SimpleCamera(camera, ref clipPlane);

            // FIXME performance!
            SimpleCamera obliqueCamera = new SimpleCamera(camera, ref clipPlane);

            clipPlane.D += offset;
            Matrix m = obliqueCamera.ViewMatrix;
            Plane.Transform(ref clipPlane, ref m, out clipPlane);

            obliqueCamera.MakeOblique(ref clipPlane);

            renderCamera = obliqueCamera;
        }

    }
}
