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

        public override Matrix InverseViewProjectionMatrix { get { return inverseViewProjectionMatrix; } }

        public override int VisitOrder { get { return visitOrder; } }

        public override Bounding.Frustum Frustum { get { return boundingFrustum; } }

        public override Bounding.Box BoundingBox { get { return boundingBox; } }

        public Vector3 lightDirection;
        public Vector3 lightPosition;

        public Matrix viewMatrix;
        public Matrix projectionMatrix;
        public Matrix viewProjectionMatrix;

        public Matrix inverseViewProjectionMatrix;

        public int visitOrder;

        public Bounding.Frustum boundingFrustum;
        public Bounding.Region cullRegion;
        public Bounding.Box boundingBox;

        public Rectangle ScissorRectangle;

        public LightCamera(Vector3 direction) : base()
        {
            lightDirection = direction;
            boundingFrustum = new Bounding.Frustum();
            cullRegion = new Bounding.Region();
            boundingBox = new Bounding.Box();
        }

        public LightCamera(LightCamera camera) : base()
        {
            lightDirection = camera.lightDirection;
            boundingFrustum = new Bounding.Frustum();
            cullRegion = new Bounding.Region();
            boundingBox = new Bounding.Box();
        }

    }
}
