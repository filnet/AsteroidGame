using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace AsteroidGame.Voxel.Control
{
    public class GunController : NodeInputController<Node>
    {
        private Vector3 muzzleTranslation;

        private int triggerCount;

        private TimeSpan coolDown = new TimeSpan(TimeSpan.TicksPerSecond / 6);
        private TimeSpan lastFireTime;

        public GunController(Node node) : base(node)
        {
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            //Console.WriteLine("update: " + gameTime.ElapsedGameTime);

            if (ButtonJustPressed(Buttons.A))
            {
                triggerCount++;
            }
            if (triggerCount > 0 || IsButtonDown(Buttons.A) || IsKeyDown(Keys.Space))
            {
                if (lastFireTime + coolDown <= gameTime.TotalGameTime)
                {
                    triggerCount--;
                    lastFireTime = gameTime.TotalGameTime;
                    fireGun(gameTime);
                }
            }
        }

        private void fireGun(GameTime gameTime)
        {
            //Matrix world = shipNode.LocalWorld;
            //Vector3 s, t;
            //Quaternion r;
            //world.Decompose(out s, out r, out t);

            //Vector3 position = shipNode.Translation;// +Vector3.Transform(Node.Translation, Matrix.CreateScale(s));// +Vector3.Transform(muzzleTranslation, shipNode.LocalWorld); ;

            AsteroidGame.Instance().FireBullet(gameTime);
        }

    }


}
