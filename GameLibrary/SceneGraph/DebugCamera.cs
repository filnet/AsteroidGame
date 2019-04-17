using GameLibrary.Component.Camera;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.SceneGraph
{
    public class DebugCamera : AbstractCamera
    {
        public override float FovX { get { return fovX; } set => throw new NotImplementedException(); }

        public override float AspectRatio { get { return aspectRatio; } set => throw new NotImplementedException(); }

        public override float ZNear { get { return zNear; } set => throw new NotImplementedException(); }

        public override float ZFar { get { return zFar; } set => throw new NotImplementedException(); }

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

        public override SceneGraph.Bounding.Frustum Frustum { get { return frustum; } }

        public override Bounding.Box BoundingBox { get { return boundingBox; } }
        public override Bounding.Sphere BoundingSphere { get { return boundingSphere; } }

        private readonly float fovX;
        private readonly float aspectRatio;
        private readonly float zNear;
        private readonly float zFar;

        private readonly Vector3 position;
        private readonly Vector3 viewDirection;

        private readonly Matrix viewMatrix;
        private readonly Matrix projectionMatrix;
        private readonly Matrix viewProjectionMatrix;
        private readonly Matrix inverseViewProjectionMatrix;

        private readonly int visitOrder;

        private readonly Bounding.Frustum frustum;
        private readonly Bounding.Box boundingBox;
        private readonly Bounding.Sphere boundingSphere;

        public DebugCamera(Camera camera) : base()
        {
            fovX = camera.FovX;
            aspectRatio = camera.AspectRatio;
            zNear = camera.ZNear;
            zFar = camera.ZFar;

            position = camera.Position;
            viewDirection = camera.ViewDirection;

            viewMatrix = camera.ViewMatrix;
            projectionMatrix = camera.ProjectionMatrix;
            viewProjectionMatrix = camera.ViewProjectionMatrix;
            inverseViewProjectionMatrix = camera.InverseViewProjectionMatrix;

            visitOrder = camera.VisitOrder;

            frustum = camera.Frustum.Clone() as Bounding.Frustum;
            boundingBox = camera.BoundingBox.Clone() as Bounding.Box;
            boundingSphere = camera.BoundingSphere.Clone() as Bounding.Sphere;
        }

    }
}
