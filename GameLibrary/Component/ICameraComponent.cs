using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameLibrary.SceneGraph;
using GameLibrary.Control;

namespace GameLibrary
{
    public interface ICameraComponent : Controller //: IGameComponent, IUpdateable, IDisposable
    {
        Matrix ViewMatrix { get; }
        Matrix ProjectionMatrix { get; }
        Matrix ViewProjectionMatrix { get; }

        BoundingFrustum BoundingFrustum { get; }

        //void LookAt(Vector3 eye, Vector3 target, Vector3 up);

        void Perspective(float FOV, float aspect, float nearPlane, float farPlane);
    }
}