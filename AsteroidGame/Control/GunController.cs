using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameLibrary.SceneGraph;
using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;

namespace AsteroidGame.Control
{
    public class GunController : NodeInputController<GeometryNode>
    {
        private GeometryNode shipNode;

        private Vector3 muzzleTranslation;

        private int triggerCount;

        private TimeSpan coolDown = new TimeSpan(TimeSpan.TicksPerSecond / 6);
        private TimeSpan lastFireTime;

        public GunController(GeometryNode gunNode, GeometryNode shipNode, Vector3 muzzleTranslation)
            : base(gunNode)
        {
            this.muzzleTranslation = muzzleTranslation;
            this.shipNode = shipNode;
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
            if (ButtonJustPressed(Buttons.Y) || IsKeyDown(Keys.LeftShift))
            {
               fireAsteroid(gameTime);
            }
        }

        private void fireGun(GameTime gameTime)
        {
            //Matrix world = shipNode.LocalWorld;
            //Vector3 s, t;
            //Quaternion r;
            //world.Decompose(out s, out r, out t);

            Vector3 position = shipNode.Translation;// +Vector3.Transform(Node.Translation, Matrix.CreateScale(s));// +Vector3.Transform(muzzleTranslation, shipNode.LocalWorld); ;

            AsteroidGame.Instance().AddBullet(gameTime, position, shipNode.Rotation, shipNode.Physics.LinearVelocity);
        }

        private void fireAsteroid(GameTime gameTime)
        {
            Random random = new Random();
            double x = random.NextDouble() * 6 - 3;
            double y = random.NextDouble() * 6 - 3;
            Vector3 position = new Vector3((float) x, (float) y, 0);
            //Vector3 s, t;
            //Quaternion r;
            //world.Decompose(out s, out r, out t);

            AsteroidGame.Instance().AddAsteroid(gameTime, position, shipNode.Rotation, shipNode.Physics.LinearVelocity, shipNode.Physics.AngularVelocity);
        }
    }


}
