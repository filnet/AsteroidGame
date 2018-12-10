using GameLibrary.Component.Camera;
using GameLibrary.Control;
using Microsoft.Xna.Framework;
using System;

namespace GameLibrary.Component
{
    public interface CameraComponent : Camera.Camera, IGameComponent, IUpdateable, IDisposable
    {
    }
}