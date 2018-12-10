using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Control;
using GameLibrary.SceneGraph.Common;
using GameLibrary.SceneGraph.Bounding;
using GameLibrary.Geometry.Common;
using GameLibrary.Geometry;

namespace GameLibrary.SceneGraph
{

    public abstract class Renderer
    {
        public BlendState BlendState;
        protected DepthStencilState DepthStencilState;
        public RasterizerState RasterizerState;
        public SamplerState SamplerState;

        public static RasterizerState WireFrameRasterizer = new RasterizerState()
        {
            CullMode = CullMode.None,
            FillMode = FillMode.WireFrame,
        };

        public Renderer()
        {
            BlendState = BlendState.Opaque;
            DepthStencilState = DepthStencilState.Default;
            RasterizerState = RasterizerState.CullClockwise;
            SamplerState = SamplerState.LinearWrap;
        }

        public abstract void Render(RenderContext rc, List<Drawable> drawableList);
    }

    public class ShowTimeRenderer : Renderer
    {
        protected readonly Renderer renderer;

        private int index;
        private double lastTime;

        public bool Enabled;

        public ShowTimeRenderer(Renderer renderer)
        {
            this.renderer = renderer;
            index = 0;
            lastTime = -1;
            //Enabled = true;
        }

        public override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            if (!Enabled)
            {
                renderer.Render(rc, drawableList);
                return;
            }
            double currentTime = rc.GameTime.TotalGameTime.TotalMilliseconds;
            if (lastTime == -1 || currentTime - lastTime >= 100)
            {
                lastTime = currentTime;
                index++;
            }
            index %= drawableList.Count;
            if (index != 0)
            {
                renderer.Render(rc, drawableList.GetRange(0, index));
            }
        }
    }

    public class EffectRenderer : Renderer
    {
        protected readonly Effect effect;

        protected readonly IEffectMatrices effectMatrices;

        public EffectRenderer(Effect effect)
        {
            this.effect = effect;
            effectMatrices = effect as IEffectMatrices;
        }

        public override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.Camera.ProjectionMatrix;
                effectMatrices.View = rc.Camera.ViewMatrix;
            }

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (Drawable drawable in drawableList)
                {
                    if (!drawable.Enabled || !drawable.Visible)
                    {
                        break;
                    }
                    if ((effectMatrices != null) && (drawable is Transform transform))
                    {
                        effectMatrices.World = transform.WorldTransform;
                        pass.Apply();
                    }
                    drawable.PreDraw(rc.GraphicsDevice);
                    drawable.Draw(rc.GraphicsDevice);
                    drawable.PostDraw(rc.GraphicsDevice);

                    rc.DrawCount++;
                    rc.VertexCount += drawable.VertexCount;
                }
            }
        }

    }

    public class WireFrameRenderer : EffectRenderer
    {
        public WireFrameRenderer(Effect effect) : base(effect)
        {
            RasterizerState = WireFrameRasterizer;
        }
    }

    public class FrustrumRenderer : EffectRenderer
    {
        public FrustrumRenderer(Effect effect) : base(effect)
        {
            //DepthStencilState = new DepthStencilState();
            //DepthStencilState.DepthBufferEnable = false;
            BlendState = BlendState.AlphaBlend;
        }
    }

    public class BoundRenderer : EffectRenderer
    {
        private readonly GeometryNode boundingGeometry;

        public BoundRenderer(Effect effect, GeometryNode boundingGeometry) : base(effect)
        {
            this.boundingGeometry = boundingGeometry;

            DepthStencilState = new DepthStencilState();
            DepthStencilState.DepthBufferEnable = false;
            BlendState = BlendState.AlphaBlend;
        }

        public override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.Camera.ProjectionMatrix;
                effectMatrices.View = rc.Camera.ViewMatrix;
            }

            boundingGeometry.PreDraw(rc.GraphicsDevice);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (Drawable drawable in drawableList)
                {
                    if (effectMatrices != null)
                    {
                        effectMatrices.World = drawable.WorldBoundingVolume.WorldMatrix;
                        pass.Apply();
                    }
                    boundingGeometry.Draw(rc.GraphicsDevice);

                    rc.DrawCount++;
                    rc.VertexCount += boundingGeometry.VertexCount;
                }
            }
            boundingGeometry.PostDraw(rc.GraphicsDevice);
        }
    }

    public class HortographicRenderer : EffectRenderer
    {
        //private Matrix projectionMatrix;
        //private Matrix viewMatrix;

        public HortographicRenderer(Effect effect) : base(effect)
        {
            //BlendState = BlendState.AlphaBlend;        
        }

        public override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            if (effectMatrices != null)
            {
                Vector2 center;
                center.X = rc.GraphicsDevice.Viewport.Width * 0.5f;
                center.Y = rc.GraphicsDevice.Viewport.Height * 0.5f;

                Matrix view = Matrix.CreateLookAt(new Vector3(center, 0), new Vector3(center, 1), new Vector3(0, -1, 0));
                Matrix projection = Matrix.CreateOrthographic(center.X * 2, center.Y * 2, -0.5f, 1);

                // FIXME...
                effectMatrices.Projection = projection;
                effectMatrices.View = view;
            }

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (Drawable drawable in drawableList)
                {
                    if (!drawable.Enabled || !drawable.Visible)
                    {
                        break;
                    }
                    if ((effectMatrices != null) && (drawable is Transform transform))
                    {
                        effectMatrices.World = transform.WorldTransform;
                        pass.Apply();
                    }
                    drawable.PreDraw(rc.GraphicsDevice);
                    drawable.Draw(rc.GraphicsDevice);
                    drawable.PostDraw(rc.GraphicsDevice);

                    rc.DrawCount++;
                    rc.VertexCount += drawable.VertexCount;
                }
            }
        }
    }

    public class VoxelRenderer : Renderer
    {
        protected readonly Effect effect;

        protected readonly IEffectMatrices effectMatrices;

        private SamplerState wireframeSamplerState = new SamplerState();


        public VoxelRenderer(Effect effect)
        {
            this.effect = effect;
            effectMatrices = effect as IEffectMatrices;

            //RasterizerState = RasterizerState.CullNone;
            //RasterizerState = Renderer.WireFrameRasterizer;

            //wireframeSamplerState.Filter = TextureFilter.MinLinearMagPointMipLinear;
            //wireframeSamplerState.Filter = TextureFilter.LinearMipPoint;
            wireframeSamplerState.AddressU = TextureAddressMode.Mirror;
        }

        public override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;
            rc.GraphicsDevice.SamplerStates[1] = wireframeSamplerState;
            rc.GraphicsDevice.SamplerStates[2] = wireframeSamplerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.Camera.ProjectionMatrix;
                effectMatrices.View = rc.Camera.ViewMatrix;
                effectMatrices.World = Matrix.Identity;
            }

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (Drawable drawable in drawableList)
                {
                    if (!drawable.Enabled || !drawable.Visible)
                    {
                        break;
                    }
                    drawable.PreDraw(rc.GraphicsDevice);
                    drawable.Draw(rc.GraphicsDevice);
                    drawable.PostDraw(rc.GraphicsDevice);

                    rc.DrawCount++;
                    rc.VertexCount += drawable.VertexCount;
                }
            }
        }

    }
}
