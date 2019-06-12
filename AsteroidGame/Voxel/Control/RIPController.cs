using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework;
using System;

namespace AsteroidGame.Voxel.Control
{
    public class RIPController : BaseNodeController<Node>
    {
        private readonly int bulletHandle;
        private readonly float lifeTime;

        private TimeSpan age = TimeSpan.Zero;

        public TimeSpan Age
        {
            get { return age; }
        }

        public RIPController(Node node, int bulletHandle, float lifeTime) : base(node)
        {
            this.bulletHandle = bulletHandle;
            this.lifeTime = lifeTime;
        }

        public override void Update(GameTime gameTime)
        {
            age += gameTime.ElapsedGameTime;
            if (age.TotalSeconds > lifeTime)
            {
                Node.Enabled = false;
                Node.Visible = false;
                Node.Remove();
                AsteroidGame.Instance().RemoveBullet(bulletHandle);
            }
        }
    }
}
