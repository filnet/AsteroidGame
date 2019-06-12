using BepuPhysics;
using GameLibrary.Control;
using GameLibrary.Physics.Bepu;
using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework;

namespace AsteroidGame.Voxel.Control
{
    public class BulletController : BaseNodeController<GeometryNode>
    {
        private readonly BodyReference bodyReference;

        public BulletController(GeometryNode node, BodyReference bodyReference) : base(node)
        {
            this.bodyReference = bodyReference;
        }

        public void Initialize()
        {
        }

        public override void Update(GameTime gameTime)
        {
            RigidPose pose = bodyReference.Pose;
            if (bodyReference.IsActive)
            {
                Vector3 position;
                pose.Position.ToMonogame(out position);

                //Quaternion orientation = Quaternion.Identity;
                //pose.Orientation.ToMonogame(out orientation);

                Node.Translation = position;
                //Node.Rotation = orientation;
            }
            else
            {
                //Console.WriteLine("INACTIVE");
            }
        }
    }
}
