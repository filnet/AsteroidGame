using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GameLibrary.Component.Camera;

namespace GameLibrary.SceneGraph
{
    public sealed class RefractionRenderContext : AbstractMapRenderContext
    {
        #region Properties

        #endregion

        public RefractionRenderContext(GraphicsDevice graphicsDevice, Camera camera) : base("WATER", graphicsDevice, camera)
        {            
        }

        public void Update(Camera camera)
        {
            // FIXME performance!
            SimpleCamera obliqueCamera = new SimpleCamera(camera);

            // FIXME cache clipPlane
            Plane clipPlane = new Plane(-Vector3.Up, -0.01f);

            Matrix m = obliqueCamera.ViewMatrix;
            Plane.Transform(ref clipPlane, ref m, out clipPlane);

            obliqueCamera.MakeOblique(ref clipPlane);

            renderCamera = obliqueCamera;
            // TODO
            cullCamera = new SimpleCamera(camera);
        }

    }
}
