using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework;
using System;

namespace AsteroidGame.Control
{
    public class RIPController : BaseNodeController<Node>
    {
        private TimeSpan age = TimeSpan.Zero;

        private float lifeTime;

        public TimeSpan Age
        {
            get { return age; }
        }

        public RIPController(Node node, float lifeTime) : base(node)
        {
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
            }
        }
    }
}
