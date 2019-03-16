using GameLibrary.Component.Camera;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.SceneGraph
{
    public class LightCamera : AbstractCamera
    {
        public override Vector3 Position { get { return lightPosition; } set => throw new NotImplementedException(); }

        public override Vector3 ViewDirection { get { return lightDirection; } }

        public override Matrix ProjectionMatrix { get { return projectionMatrix; } }

        public override Matrix ViewMatrix { get { return viewMatrix; } }

        public override Matrix ViewProjectionMatrix { get { return viewProjectionMatrix; } }

        public override Matrix InverseViewProjectionMatrix { get { return invViewProjectionMatrix; } }

        public override int VisitOrder { get { return visitOrder; } }

        public override SceneGraph.Bounding.Frustum BoundingFrustum { get { return boundingFrustum; } }

        public Vector3 lightPosition;
        public Vector3 lightDirection;

        public Matrix viewMatrix;
        public Matrix projectionMatrix;
        public Matrix viewProjectionMatrix;

        public Matrix invViewProjectionMatrix;

        public int visitOrder;

        public SceneGraph.Bounding.Frustum boundingFrustum;

        public Rectangle ScissorRectangle;

        public LightCamera() : base()
        {
            boundingFrustum = new SceneGraph.Bounding.Frustum();
        }

    }
}
