using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameLibrary
{
    public class Integrator
    {

        #region Fields

        // constant state
        private float size; // length of the cube sides in meters.
        private float mass; // mass of the cube in kilograms.
        private float inverseMass; // inverse of the mass used to convert momentum to velocity.
        private float inertiaTensor; // inertia tensor of the cube (i have simplified it to a single value due to the mass properties a cube).
        private float inverseInertiaTensor; // inverse inertia tensor used to convert angular momentum to angular velocity.

        // secondary state
        //public Matrix World; // body to world coordinates matrix.
        //public Matrix worldToBody; // world to body coordinates matrix.

        private State previous; // previous physics state.
        private State current; // current physics state.

        private State state;

        private TimeSpan dt = new TimeSpan(TimeSpan.TicksPerSecond / 100);

        private TimeSpan accumulator;
        private TimeSpan t;

        private TimeSpan maxElapsed = new TimeSpan(TimeSpan.TicksPerSecond / 4);

        #endregion

        #region Properties
        
        public Vector3 Position
        {
            get { return state.position; }
            set { Translate(value - state.position); }
        }

        public Quaternion Orientation
        {
            get { return state.orientation; }
            // TODO current / previous should be rotated by offset
            set { state.orientation = current.orientation = previous.orientation = value; }
        }

        public Vector3 Velocity
        {
            get { return state.velocity; }
            // TODO current / previous should be translated by offset
            set { state.momentum = current.momentum = previous.momentum = value * mass; state.velocity = current.velocity = previous.velocity = value; }
        }

        public Vector3 AngularVelocity
        {
            get { return state.angularVelocity; }
            // TODO current / previous should be translated by offset
            set { state.angularMomentum = current.angularMomentum = previous.angularMomentum = value * inertiaTensor; state.angularVelocity = current.angularVelocity = previous.angularVelocity = value; }
        }

        #endregion

        #region Constructors

        public Integrator()
        {
            // initialize state constants
            size = 1;
            mass = 1;
            inverseMass = 1.0f / mass;
            inertiaTensor = mass * size * size * 1.0f / 6.0f;
            inverseInertiaTensor = 1.0f / inertiaTensor;

            // initialize state
            current.outer = this;
            current.position = new Vector3(2, 0, 0);
            current.momentum = new Vector3(0, 0, 0);
            current.orientation = Quaternion.Identity;
            current.angularMomentum = new Vector3(0, 0, 0);
            current.recalculate();

            previous = current;
            state = current;
        }

        #endregion

        #region Public Methods

        public void Translate(Vector3 translation)
        {
            Vector3.Add(ref state.position, ref translation, out state.position);
            Vector3.Add(ref current.position, ref translation, out current.position);
            Vector3.Add(ref previous.position, ref translation, out previous.position);
        }

        public void Update(TimeSpan elapsed)
        {
            if (elapsed > maxElapsed)
            {
                elapsed = maxElapsed;
            }

            accumulator += elapsed;
            while (accumulator >= dt)
            {
                previous = current;
                integrate(ref current, (float) t.TotalSeconds, (float) dt.TotalSeconds);
                accumulator -= dt;
                t += dt;
            }

            if (accumulator > TimeSpan.Zero)
            {
                // interpolate state with alpha for smooth animation
                state = interpolate(previous, current, (float) (accumulator.TotalSeconds / dt.TotalSeconds));
            }
            else
            {
                state = current;
            }

            //World = Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
            //worldToBody = Matrix.Invert(World);
        }

        #endregion

        #region Private
        
        protected struct State
        {
            // Outer class...
            public Integrator outer;

            // primary physics state
            public Vector3 position; // the position of the cube center of mass in world coordinates (meters).
            public Vector3 momentum; // the momentum of the cube in kilogram meters per second.
            public Quaternion orientation; // the orientation of the cube represented by a unit quaternion.
            public Vector3 angularMomentum; // angular momentum vector.

            // secondary state
            public Vector3 velocity; // velocity in meters per second (calculated from momentum).
            public Quaternion spin; // quaternion rate of change in orientation.
            public Vector3 angularVelocity; // angular velocity (calculated from angularMomentum).

            // Recalculate secondary state values from primary values.
            public void recalculate()
            {
                velocity = momentum * outer.inverseMass;
                angularVelocity = angularMomentum * outer.inverseInertiaTensor;
                orientation.Normalize();
                spin = /*0.5 */ new Quaternion(0, angularVelocity.X, angularVelocity.Y, angularVelocity.Z) * orientation * 0.5f;
            }
        }

        // Derivative values for primary state.
        // This structure stores all derivative values for primary state in VoxelMap::State.
        // For example velocity is the derivative of position, force is the derivative
        // of momentum etc. Storing all derivatives in this structure makes it easy
        // to implement the RK4 integrator cleanly because it needs to calculate the
        // and store derivative values at several points each timestep.
        private struct Derivative
        {
            public Vector3 velocity; // velocity is the derivative of position.
            public Vector3 force; // force in the derivative of momentum.
            public Quaternion spin; // spin is the derivative of the orientation quaternion.
            public Vector3 torque; // torque is the derivative of angular momentum.
        }

        // Evaluate all derivative values for the physics state at time t.
        // @param state the physics state of the cube.
        private Derivative evaluate(State state, float t)
        {
            Derivative output = new Derivative();
            output.velocity = state.velocity;
            output.spin = state.spin;
            forces(state, t, ref output.force, ref output.torque);
            return output;
        }

        // Evaluate derivative values for the physics state at future time t+dt 
        // using the specified set of derivatives to advance dt seconds from the 
        // specified physics state.
        // WARNING : State is passed as a value!
        private Derivative evaluate(State state, float t, float dt, Derivative derivative)
        {
            state.position += derivative.velocity * dt;
            state.momentum += derivative.force * dt;
            state.orientation += derivative.spin * dt;
            state.angularMomentum += derivative.torque * dt;
            state.recalculate();

            Derivative output = new Derivative();
            output.velocity = state.velocity;
            output.spin = state.spin;
            forces(state, t + dt, ref output.force, ref output.torque);
            return output;
        }

        // Integrate physics state forward by dt seconds.
        // Uses an RK4 integrator to numerically integrate with error O(5).
        private void integrate(ref State state, float t, float dt)
        {
            Derivative a = evaluate(state, t);
            Derivative b = evaluate(state, t, dt * 0.5f, a);
            Derivative c = evaluate(state, t, dt * 0.5f, b);
            Derivative d = evaluate(state, t, dt, c);

            state.position += 1.0f / 6.0f * dt * (a.velocity + 2.0f * (b.velocity + c.velocity) + d.velocity);
            state.momentum += 1.0f / 6.0f * dt * (a.force + 2.0f * (b.force + c.force) + d.force);
            state.orientation += /*1.0f / 6.0f * dt **/ (a.spin + /*2.0f **/ (b.spin + c.spin) * 2.0f + d.spin) * (1.0f / 6.0f * dt);
            state.angularMomentum += 1.0f / 6.0f * dt * (a.torque + 2.0f * (b.torque + c.torque) + d.torque);

            state.recalculate();
        }

        // Calculate force and torque for physics state at time t.
        // Due to the way that the RK4 integrator works we need to calculate
        // force implicitly from state rather than explictly applying forces
        // to the rigid body once per update. This is because the RK4 achieves
        // its accuracy by detecting curvature in derivative values over the 
        // timestep so we need our force values to supply the curvature.
        protected virtual void forces(State state, float t, ref Vector3 force, ref Vector3 torque)
        {
            // attract towards origin
            force.X = -5.0f * state.position.X;
            force.Y = -5.0f * state.position.Y;
            force.Z = -5.0f * state.position.Z;

            //// sine force to add some randomness to the motion
            //force.X += 10 * (float) Math.Sin(t * 0.9 + 0.5);
            //force.Y += 11 * (float) Math.Sin(t * 0.5 + 0.4);
            //force.Z += 12 * (float) Math.Sin(t * 0.7 + 0.9);

            //// sine torque to get some spinning action
            //torque.X = 1.0f * (float) Math.Sin(t * 0.9 + 0.5);
            //torque.Y = 1.1f * (float) Math.Sin(t * 0.5 + 0.4);
            //torque.Z = 1.2f * (float) Math.Sin(t * 0.7 + 0.9);

            //// damping torque so we dont spin too fast
            //torque.X -= 0.2f * state.angularVelocity.X;
            //torque.Y -= 0.2f * state.angularVelocity.Y;
            //torque.Z -= 0.2f * state.angularVelocity.Z;
        }

        private State interpolate(State a, State b, float alpha)
        {
            State state = b;
            state.position = a.position * (1 - alpha) + b.position * alpha;
            //state.momentum = a.momentum * (1 - alpha) + b.momentum * alpha;
            state.orientation = Quaternion.Slerp(a.orientation, b.orientation, alpha);
            //state.angularMomentum = a.angularMomentum * (1 - alpha) + b.angularMomentum * alpha;
            //state.recalculate();
            return state;
        }

        #endregion

    }

}
