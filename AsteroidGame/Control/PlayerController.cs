using GameLibrary.Control;
using GameLibrary.Physics;
using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace AsteroidGame.Control
{
    public class PlayerController : NodeInputController<GeometryNode>, PhysicsController<GeometryNode>
    {
        private MyIntegrator integrator;

        //public Vector3 Position
        //{
        //    get { return integrator.Position; }
        //    set { integrator.Position = value; }
        //}

        //public Quaternion Orientation
        //{
        //    get { return integrator.Orientation; }
        //    set { integrator.Orientation = value; }
        //}

        //public Vector3 Velocity
        //{
        //    get { return integrator.Velocity; }
        //    set { integrator.Velocity = value; }
        //}

        public PlayerController(GeometryNode node) : base(node)
        {
            integrator = new MyIntegrator();
            integrator.Position = Node.Translation;
            integrator.Orientation = Node.Rotation;
        }

        public void Initialize()
        {
            //base.Initialize();
            //dirty = true;
            //updateWorldMatrix();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            //Console.WriteLine("update: " + gameTime.ElapsedGameTime);

            Vector2 v = GamePadState.ThumbSticks.Left;
            integrator.rotationForce = -v.X * 1.8f;
            float f1 = v.Y * 1.8f;
            float f2 = GamePadState.Triggers.Right * 1.8f;
            integrator.forwardForce = Math.Abs(f1) > Math.Abs(f2) ? f1 : f2;
            /*
            if (IsButtonDown(Buttons.B) || IsKeyDown(Keys.Up))
            {
                integrator.forwardForce = 1.8f;
            }
            if (IsKeyDown(Keys.Down))
            {
                integrator.forwardForce = -1.8f;
            }
            if (IsKeyDown(Keys.Left))
            {
                integrator.rotationForce = 1.8f;
            }
            if (IsKeyDown(Keys.Right))
            {
                integrator.rotationForce = -1.8f;
            }
            */
            integrator.Update(gameTime.ElapsedGameTime);

            Boolean modified = false;
            Vector3 p = integrator.Position;
            Vector3 t = Vector3.Zero;
            if (p.X < -3.0f) { t.X = 6.0f; modified = true; } else if (p.X > 3.0f) { t.X = -6.0f; modified = true; }
            if (p.Y < -3.0f) { t.Y = 6.0f; modified = true; } else if (p.Y > 3.0f) { t.Y = -6.0f; modified = true; }
            if (modified)
            {
                integrator.Translate(t);
            }

            Node.Translation = integrator.Position;
            Node.Rotation = integrator.Orientation;

            Node.Physics.LinearVelocity = integrator.Velocity;
            Node.Physics.AngularVelocity = integrator.AngularVelocity;
        }

        class MyIntegrator : Integrator
        {
            public float forwardForce;
            public float rotationForce;

            public float velocityDamping = 0.9f;
            public float rotationDamping = 0.8f;

            protected override void forces(State state, float t, ref Vector3 force, ref Vector3 torque)
            {
                if (forwardForce != 0)
                {
                    Vector3 forward = Vector3.Transform(Vector3.Right, state.orientation);
                    force = forward * forwardForce;
                    //Console.WriteLine("forwardForce: " + force);
                }

                float velocitySquared = 0;// state.velocity.LengthSquared();
                if (velocitySquared > 0)
                {
                    Vector3 velocityDirection = Vector3.Normalize(state.velocity);
                    //Vector3 friction = frictionForce * velocitySquared * velocityDirection;
                    //Console.WriteLine("velocity: " + state.velocity);
                    //Console.WriteLine("velocitySquared: " + velocitySquared);
                    //Console.WriteLine("velocityDirection: " + velocityDirection);
                    //Console.WriteLine("friction: " + friction);
                    //force -= friction;
                }
                if (velocityDamping != 0)
                {
                    force -= velocityDamping * state.velocity;
                }

                if (rotationForce != 0)
                {
                    torque.Y = rotationForce;
                    //Console.WriteLine("torque: " + torque);
                }

                if (rotationDamping != 0)
                {
                    torque -= rotationDamping * state.angularVelocity;
                }
            }
        }

    }
}
