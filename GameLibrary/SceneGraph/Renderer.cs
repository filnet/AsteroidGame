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
using Voxel;

namespace GameLibrary.SceneGraph
{

    public abstract class Renderer
    {
        public BlendState BlendState;
        public DepthStencilState DepthStencilState;
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

    public class EffectRenderer<E> : Renderer where E : Effect
    {
        public readonly E effect;

        protected readonly IEffectMatrices effectMatrices;

        public EffectRenderer(E effect)
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

    public class BasicRenderer : EffectRenderer<Effect>
    {
        public BasicRenderer(Effect effect) : base(effect)
        {
        }
    }

    public class WireFrameRenderer : BasicRenderer
    {
        public WireFrameRenderer(Effect effect) : base(effect)
        {
            RasterizerState = WireFrameRasterizer;
        }
    }

    public class FrustrumRenderer : BasicRenderer
    {
        public FrustrumRenderer(Effect effect) : base(effect)
        {
            //DepthStencilState = new DepthStencilState();
            //DepthStencilState.DepthBufferEnable = false;
            BlendState = BlendState.AlphaBlend;
        }
    }

    public class BoundRenderer : BasicRenderer
    {
        private readonly GeometryNode boundingGeometry;

        public BoundRenderer(Effect effect, GeometryNode boundingGeometry) : base(effect)
        {
            this.boundingGeometry = boundingGeometry;

            DepthStencilState = new DepthStencilState();
            //DepthStencilState.DepthBufferEnable = false;
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

            Matrix worldMatrix;

            boundingGeometry.PreDraw(rc.GraphicsDevice);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (Drawable drawable in drawableList)
                {
                    if (effectMatrices != null)
                    {
                        drawable.WorldBoundingVolume.WorldMatrix(out worldMatrix);
                        effectMatrices.World = worldMatrix;
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

    public class HortographicRenderer : BasicRenderer
    {
        //private Matrix projectionMatrix;
        //private Matrix viewMatrix;

        public HortographicRenderer(Effect effect) : base(effect)
        {
            RasterizerState = RasterizerState.CullNone;
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
                    // HACK
                    // HACK
                    // HACK
                    if (drawable is BillboardNode billboard)
                    {
                        // HACK
                        ((BasicEffect)effect).Texture = billboard.Texture;
                        //pass.Apply();
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

    public class BillboardRenderer : BasicRenderer
    {
        //private Matrix projectionMatrix;
        //private Matrix viewMatrix;

        public BillboardRenderer(Effect effect) : base(effect)
        {
            RasterizerState = RasterizerState.CullNone;
            //BlendState = BlendState.AlphaBlend;

            DepthStencilState depthState = new DepthStencilState();
            depthState.DepthBufferEnable = false;
            depthState.DepthBufferWriteEnable = false;
            DepthStencilState = depthState;
        }

        public override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            int width = rc.GraphicsDevice.Viewport.Width;
            int height = rc.GraphicsDevice.Viewport.Height;
            if (effectMatrices != null)
            {
                Vector2 eye;
                eye.X = 0;// width / 2;
                eye.Y = 0;// height / 2;

                Matrix view = Matrix.CreateLookAt(new Vector3(eye.X, eye.Y, 0), new Vector3(eye.X, eye.Y, -1), new Vector3(0, 1, 0));
                //Matrix projection = Matrix.CreateOrthographic(width, height, -0.5f, 1);
                //Matrix projection = Matrix.CreateOrthographicOffCenter(-10, width, -10, height, -0.5f, 1);
                Matrix projection = Matrix.CreateOrthographicOffCenter(0, width, 0, height, -0.5f, 1);

                // FIXME...
                effectMatrices.Projection = projection;
                effectMatrices.View = view;

                effectMatrices.World = Matrix.CreateScale(256, 256, 0);
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
                    // HACK
                    // HACK
                    // HACK
                    if (drawable is BillboardNode billboard)
                    {
                        // HACK
                        ((BasicEffect)effect).Texture = billboard.Texture;
                        //pass.Apply();
                        /*
                        if ((effectMatrices != null) && (drawable is Transform transform))
                        {
                            effectMatrices.World = transform.WorldTransform;
                            //effectMatrices.World = Matrix.CreateTranslation(new Vector3(0, height - billboard.Texture.Height, 0));
                            pass.Apply();
                        }
                        */
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

    public class VoxelRenderer : EffectRenderer<VoxelEffect>
    {
        private readonly SamplerState wireframeSamplerState = new SamplerState();

        private readonly SamplerState shadowSamplerState = new SamplerState();

        public VoxelRenderer(VoxelEffect effect) : base(effect)
        {
            //RasterizerState = RasterizerState.CullNone;
            //RasterizerState = WireFrameRasterizer;

            // wireframe texture sampler
            //wireframeSamplerState.Filter = TextureFilter.MinLinearMagPointMipLinear;
            //wireframeSamplerState.Filter = TextureFilter.LinearMipPoint;
            wireframeSamplerState.AddressU = TextureAddressMode.Mirror;

            // shadow texture sampler
            shadowSamplerState.Filter = TextureFilter.Linear;
            shadowSamplerState.AddressU = TextureAddressMode.Clamp;
            shadowSamplerState.AddressV = TextureAddressMode.Clamp;
            shadowSamplerState.AddressW = TextureAddressMode.Clamp;
            shadowSamplerState.ComparisonFunction = CompareFunction.LessEqual;
            shadowSamplerState.FilterMode = TextureFilterMode.Comparison;
            //shadowSamplerState.BorderColor = new Color(1f, 1f, 1f, 1f);
        }

        public override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;

            // main texture
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            // wireframe textures
            rc.GraphicsDevice.SamplerStates[1] = wireframeSamplerState;
            rc.GraphicsDevice.SamplerStates[2] = wireframeSamplerState;

            // shadow map textures
            rc.GraphicsDevice.SamplerStates[3] = shadowSamplerState;

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

    public class VoxelWaterRenderer : VoxelRenderer
    {
        public VoxelWaterRenderer(VoxelEffect effect) : base(effect)
        {
            RasterizerState = RasterizerState.CullNone;
            BlendState = BlendState.AlphaBlend;
            // TODO there is no need to disable depth write if the transparent is Z sorted
            //DepthStencilState depthState = new DepthStencilState();
            //depthState.DepthBufferEnable = true;
            //depthState.DepthBufferWriteEnable = false;
            //DepthStencilState = depthState;
        }
    }

    public class VoxelShadowRenderer : EffectRenderer<VoxelShadowEffect>
    {
        public VoxelShadowRenderer(VoxelShadowEffect effect) : base(effect)
        {
            //RasterizerState = RasterizerState.CullNone;
            //RasterizerState = RasterizerState.CullCounterClockwise;
        }

        public override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;

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
