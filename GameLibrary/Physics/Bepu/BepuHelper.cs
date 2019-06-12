using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using System;
using System.Numerics;

namespace GameLibrary.Physics.Bepu
{
    public static class BepuHelper
    {
        public static Sphere bulletSphere;
        public static BodyDescription bulletDescription;

        public static Simulation CreateSimulation(BufferPool bufferPool)
        {
            Simulation simulation = Simulation.Create(bufferPool, new NarrowPhaseCallbacks(), new PoseIntegratorCallbacks(new Vector3(0, -10, 0), 0.05f));

            simulation.Statics.Add(new StaticDescription(new Vector3(0, -0.5f, 0), new CollidableDescription(simulation.Shapes.Add(new Box(500, 1, 500)), 0.1f)));

            bulletSphere = new Sphere(0.1f);
            bulletSphere.ComputeInertia(0.1f, out var bulletInertia);
            CollidableDescription bulletCollidableDescription = new CollidableDescription(simulation.Shapes.Add(bulletSphere), 10);
            BodyActivityDescription bulletBodyActivityDescription = new BodyActivityDescription(0.01f);
            bulletDescription = BodyDescription.CreateDynamic(new Vector3(), bulletInertia, bulletCollidableDescription, bulletBodyActivityDescription);

            return simulation;
        }

        public static int AddBullet(Simulation simulation, Microsoft.Xna.Framework.Vector3 position, Microsoft.Xna.Framework.Vector3 velocity)
        {
            bulletDescription.Pose.Position = new Vector3(position.X, position.Y, position.Z);
            bulletDescription.Velocity.Linear = new Vector3(velocity.X, velocity.Y, velocity.Z);
            // camera.GetRayDirection(input.MouseLocked, window.GetNormalizedMousePosition(input.MousePosition)) * 400;
            int handle = simulation.Bodies.Add(bulletDescription);
            return handle;
        }

        public static void RemoveBullet(Simulation simulation, int bulletHandle)
        {
            simulation.Bodies.Remove(bulletHandle);
            Console.WriteLine(simulation.Bodies.ActiveSet.Count);
        }
    }
}
