using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.Component.Camera
{
    public abstract class AbstractCamera : Camera
    {
        public Quaternion Orientation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual Vector3 Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual Vector3 ViewDirection => throw new NotImplementedException();

        public virtual Matrix ProjectionMatrix => throw new NotImplementedException();

        public virtual Matrix ViewMatrix => throw new NotImplementedException();

        public virtual Matrix ViewProjectionMatrix => throw new NotImplementedException();

        public virtual Matrix InverseViewProjectionMatrix => throw new NotImplementedException();

        public virtual Vector3 XAxis => throw new NotImplementedException();

        public virtual Vector3 YAxis => throw new NotImplementedException();

        public virtual Vector3 ZAxis => throw new NotImplementedException();

        public virtual int VisitOrder => throw new NotImplementedException();

        public virtual BoundingFrustum BoundingFrustum => throw new NotImplementedException();

        public virtual SceneGraph.Bounding.BoundingBox BoundingBox => throw new NotImplementedException();

        public virtual SceneGraph.Bounding.BoundingSphere BoundingSphere => throw new NotImplementedException();

        public virtual float AspectRatio { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual float ZFar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public AbstractCamera()
        {
        }

        public void LookAt(Vector3 target)
        {
            throw new NotImplementedException();
        }

        public void LookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            throw new NotImplementedException();
        }

        public void Move(float dx, float dy, float dz)
        {
            throw new NotImplementedException();
        }

        public void Move(Vector3 direction, Vector3 distance)
        {
            throw new NotImplementedException();
        }

        public void Perspective(float fovx, float aspect, float znear, float zfar)
        {
            throw new NotImplementedException();
        }

        public void Rotate(float headingDegrees, float pitchDegrees, float rollDegrees)
        {
            throw new NotImplementedException();
        }

        public void SetAspect(float aspect)
        {
            throw new NotImplementedException();
        }

        public void SetZFar(float zfar)
        {
            throw new NotImplementedException();
        }

        public void Zoom(float zoom, float minZoom, float maxZoom)
        {
            throw new NotImplementedException();
        }
    }
}
