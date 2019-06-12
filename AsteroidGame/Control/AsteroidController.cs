using GameLibrary.Control;
using GameLibrary.Physics;
using GameLibrary.SceneGraph.Common;
using Microsoft.Xna.Framework;
using System;

namespace AsteroidGame.Control
{
    public class AsteroidController : BaseNodeController<GeometryNode>
    {
        private MyIntegrator integrator;

        public AsteroidController(GeometryNode node, Vector3 velocity, Vector3 angularVelocity) : base(node)
        {
            integrator = new MyIntegrator();
            integrator.Position = Node.Translation;
            integrator.Orientation = Node.Rotation;
            integrator.Velocity = velocity;
            integrator.AngularVelocity = angularVelocity;
        }

        public void Initialize()
        {
            //base.Initialize();
            //dirty = true;
            //updateWorldMatrix();
            //// Setup the initial input states.
            //currentKeyboardState = Keyboard.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            //integrator.Position = Node.Translation;
            //integrator.Orientation = Node.Rotation;
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
        }

        class MyIntegrator : Integrator
        {
            public float forwardForce = 0.0f;
            public float rotationForce = 0.0f;

            public float velocityDamping = 0.0f;
            public float rotationDamping = 0.0f;

            protected override void forces(State state, float t, ref Vector3 force, ref Vector3 torque)
            {
                if (forwardForce != 0)
                {
                    Vector3 forward = Vector3.Transform(Vector3.Right, state.orientation);
                    force = forward * forwardForce;
                    //Console.WriteLine("forwardForce: " + force);
                }
                // attract towards origin
                //float f = 0.5f;
                //force.X = -f * state.position.X;
                //force.Y = -f * state.position.Y;
                //force.Z = -f * state.position.Z;

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

                // damping torque
                if (rotationDamping != 0)
                {
                    torque -= rotationDamping * state.angularVelocity;
                }

            }
        }
    }
}
