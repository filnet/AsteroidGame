using GameLibrary.Component.Camera;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.SceneGraph
{
    public class DebugCamera : AbstractCamera
    {
        public override Vector3 Position
        {
            get { return position; }
            set => throw new NotImplementedException();
        }

        public override Vector3 ViewDirection { get { return viewDirection; } }

        public override Vector3 YAxis { get { return viewMatrix.Up; } }

        public override Matrix ProjectionMatrix { get { return projectionMatrix; } }

        public override Matrix ViewMatrix { get { return viewMatrix; } }

        public override Matrix ViewProjectionMatrix { get { return viewProjectionMatrix; } }

        public override Matrix InverseViewProjectionMatrix { get { return inverseViewProjectionMatrix; } }

        public override int VisitOrder { get { return visitOrder; } }

        public override SceneGraph.Bounding.Frustum BoundingFrustum { get { return boundingFrustum; } }

        public override Bounding.Box BoundingBox { get { return boundingBox; } }
        public override Bounding.Sphere BoundingSphere { get { return boundingSphere; } }

        private readonly Vector3 position;
        private readonly Vector3 viewDirection;

        private readonly Matrix viewMatrix;
        private readonly Matrix projectionMatrix;
        private readonly Matrix viewProjectionMatrix;
        private readonly Matrix inverseViewProjectionMatrix;

        private readonly int visitOrder;

        private readonly Bounding.Frustum boundingFrustum;
        private readonly Bounding.Box boundingBox;
        private readonly Bounding.Sphere boundingSphere;

        public DebugCamera(Camera camera) : base()
        {
            position = camera.Position;
            viewDirection = camera.ViewDirection;

            viewMatrix = camera.ViewMatrix;
            projectionMatrix = camera.ProjectionMatrix;
            viewProjectionMatrix = camera.ViewProjectionMatrix;
            inverseViewProjectionMatrix = camera.InverseViewProjectionMatrix;

            visitOrder = camera.VisitOrder;

            boundingFrustum = camera.BoundingFrustum.Clone() as Bounding.Frustum;
            boundingBox = camera.BoundingBox.Clone() as Bounding.Box;
            boundingSphere = camera.BoundingSphere.Clone() as Bounding.Sphere;
        }

    }
}
