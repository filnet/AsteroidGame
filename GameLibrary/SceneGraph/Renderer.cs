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
using StockEffects;

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
            SamplerState = SamplerState.LinearClamp;
        }

        public virtual void Render(RenderContext rc, RenderBin renderBin)
        {
            Render(rc, renderBin.DrawableList);
        }

        internal abstract void Render(RenderContext rc, List<Drawable> drawableList);
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

        internal override void Render(RenderContext rc, List<Drawable> drawableList)
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

        internal override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.RenderCamera.ProjectionMatrix;
                effectMatrices.View = rc.RenderCamera.ViewMatrix;
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

    public class FrustumRenderer : BasicRenderer
    {
        public FrustumRenderer(Effect effect) : base(effect)
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

        internal override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.RenderCamera.ProjectionMatrix;
                effectMatrices.View = rc.RenderCamera.ViewMatrix;
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

        internal override void Render(RenderContext rc, List<Drawable> drawableList)
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
                        ((StockEffects.BasicEffect)effect).Texture = billboard.Texture;
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

    public class BillboardRenderer : EffectRenderer<ShadowMapEffect>
    {
        //private Matrix projectionMatrix;
        //private Matrix viewMatrix;

        public BillboardRenderer(ShadowMapEffect effect) : base(effect)
        {
            RasterizerState = RasterizerState.CullNone;
            //BlendState = BlendState.AlphaBlend;

            DepthStencilState depthState = new DepthStencilState();
            depthState.DepthBufferEnable = false;
            depthState.DepthBufferWriteEnable = false;
            DepthStencilState = depthState;
        }

        internal override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            if (effectMatrices != null)
            {
                // FIXME no need to create each time
                Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0));
                effectMatrices.View = view;

                // FIXME no need to create each time
                int width = rc.GraphicsDevice.Viewport.Width;
                int height = rc.GraphicsDevice.Viewport.Height;
                Matrix projection = Matrix.CreateOrthographicOffCenter(0, width, 0, height, -0.5f, 1);
                effectMatrices.Projection = projection;
            }

            //((StockEffects.ShadowMapEffect)effect).CurrentTechnique = ((StockEffects.ShadowMapEffect)effect).Techniques[1];
            //effect.CurrentTechnique = effect.Techniques[Technique];

            float x = 0;
            float y = 0;
            // FIXME we should not change technique for each drawable...
            foreach (Drawable drawable in drawableList)
            {
                if (!drawable.Enabled || !drawable.Visible)
                {
                    break;
                }
                // HACK
                // HACK
                // HACK
                float aspect = 1.0f;
                float width = 1.0f;
                float height = 1.0f;
                if (drawable is BillboardNode billboard)
                {
                    //Console.WriteLine(billboard.Name);

                    // HACK
                    //((BasicEffect)effect).Texture = billboard.Texture;
                    //Console.WriteLine(billboard.Mode);
                    effect.CurrentTechnique = effect.Techniques[billboard.Mode];
                    effect.Texture = billboard.Texture;

                    aspect = (float)billboard.Texture.Width / (float)billboard.Texture.Height;

                    /*if (billboard.Texture.)
                    {

                    }*/

                }
                if (effectMatrices != null)
                {
                    bool scaleY = false;
                    bool centerY = true;
                    if (scaleY)
                    {
                        float h = height;
                        height /= aspect;
                        if (centerY && (h > height))
                        {
                            y = (h - height) / 2.0f;
                        }
                    }
                    else
                    {
                        width *= aspect;
                        y = 0.0f;
                    }
                    float scale = 256.0f;
                    effectMatrices.World = Matrix.CreateScale(width * scale, height * scale, 0.0f) * Matrix.CreateTranslation(x * scale, y * scale, 0);
                }
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {

                    pass.Apply();

                    drawable.PreDraw(rc.GraphicsDevice);
                    drawable.Draw(rc.GraphicsDevice);
                    drawable.PostDraw(rc.GraphicsDevice);

                    rc.DrawCount++;
                    rc.VertexCount += drawable.VertexCount;
                    x += 1f * aspect;
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
            //shadowSamplerState.Filter = TextureFilter.Point;
            shadowSamplerState.AddressU = TextureAddressMode.Border;
            shadowSamplerState.AddressV = TextureAddressMode.Border;
            shadowSamplerState.ComparisonFunction = CompareFunction.LessEqual;
            shadowSamplerState.FilterMode = TextureFilterMode.Comparison;
            shadowSamplerState.BorderColor = Color.White;
        }

        internal override void Render(RenderContext rc, List<Drawable> drawableList)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;

            // main texture
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            // wireframe textures
            rc.GraphicsDevice.SamplerStates[1] = wireframeSamplerState;

            // shadow map textures
            rc.GraphicsDevice.SamplerStates[2] = shadowSamplerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.RenderCamera.ProjectionMatrix;
                effectMatrices.View = rc.RenderCamera.ViewMatrix;
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

    public class VoxelWaterRenderer : EffectRenderer<VoxelWaterEffect>
    {
        private readonly SamplerState reflectionSamplerState = new SamplerState();

        public VoxelWaterRenderer(VoxelWaterEffect effect) : base(effect)
        {
            RasterizerState = RasterizerState.CullNone;
            BlendState = BlendState.AlphaBlend;
            /*
            BlendState = BlendState.Additive;
            BlendState blendState = new BlendState();
            blendState.ColorBlendFunction = BlendFunction.Add;
            blendState.AlphaSourceBlend = Blend.Zero;
            blendState.AlphaDestinationBlend = Blend.SourceColor;
            BlendState = blendState;
            */
            // TODO there is no need to disable depth write if the transparent is Z sorted
            /*
            DepthStencilState depthState = new DepthStencilState();
            depthState.DepthBufferEnable = true;
            depthState.DepthBufferWriteEnable = false;
            DepthStencilState = depthState;
            */

            // shadow texture sampler
            //reflectionSamplerState.Filter = TextureFilter.Linear;
            //reflectionSamplerState.Filter = TextureFilter.Anisotropic;
            //reflectionSamplerState.AddressU = TextureAddressMode.Border;
            //reflectionSamplerState.AddressV = TextureAddressMode.Border;
            //reflectionSamplerState.ComparisonFunction = CompareFunction.LessEqual;
            //reflectionSamplerState.FilterMode = TextureFilterMode.Comparison;
            //reflectionSamplerState.BorderColor = Color.White;
        }

        public override void Render(RenderContext rc, RenderBin renderBin)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;

            // main texture
            rc.GraphicsDevice.SamplerStates[0] = SamplerState;

            // refraction map textures
            rc.GraphicsDevice.SamplerStates[1] = reflectionSamplerState;

            // reflection map textures
            rc.GraphicsDevice.SamplerStates[2] = reflectionSamplerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.RenderCamera.ProjectionMatrix;
                effectMatrices.View = rc.RenderCamera.ViewMatrix;
                effectMatrices.World = Matrix.Identity;
            }

            List<Drawable> drawableList = renderBin.DrawableList;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                int i = 0;
                foreach (Drawable drawable in drawableList)
                {
                    if (drawable.Enabled && drawable.Visible)
                    {
                        drawable.PreDraw(rc.GraphicsDevice);
                        drawable.Draw(rc.GraphicsDevice);
                        drawable.PostDraw(rc.GraphicsDevice);
                        rc.DrawCount++;
                        rc.VertexCount += drawable.VertexCount;
                    }
                    i++;
                }
            }
        }
    }

    public abstract class AbstractShadowRenderer<E> : EffectRenderer<E> where E : Effect
    {
        internal bool instanced = true;

        public AbstractShadowRenderer(E effect) : base(effect)
        {
            //RasterizerState = RasterizerState.CullNone;
            //RasterizerState = RasterizerState.CullCounterClockwise;
            RasterizerState = new RasterizerState();
            RasterizerState.CullMode = CullMode.CullClockwiseFace;
            // disable depth clipping :
            // occluders outside of near/far planes will have Z clamped to -1.0/1.0 instead of being clipped
            RasterizerState.DepthClipEnable = false;

            //RasterizerState.DepthBias = 0.001f;
            //RasterizerState.SlopeScaleDepthBias = 0.5f;
        }

    }

    public class ShadowRenderer : AbstractShadowRenderer<StockEffects.ShadowEffect>
    {
        public ShadowRenderer(StockEffects.ShadowEffect effect) : base(effect)
        {
            RasterizerState.DepthBias = 0.001f;
            RasterizerState.SlopeScaleDepthBias = 0.5f;
        }

        public override void Render(RenderContext rc, RenderBin renderBin)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.RenderCamera.ProjectionMatrix;
                effectMatrices.View = rc.RenderCamera.ViewMatrix;
            }

            List<Drawable> drawableList = renderBin.DrawableList;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                int i = 0;
                foreach (Drawable drawable in drawableList)
                {
                    if (drawable.Enabled && drawable.Visible)
                    {
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
                    i++;
                }
            }
        }
    }

    public class VoxelShadowRenderer : AbstractShadowRenderer<StockEffects.ShadowEffect>
    {
        public VoxelShadowRenderer(StockEffects.ShadowEffect effect) : base(effect)
        {
            RasterizerState.DepthBias = 0.001f;
            RasterizerState.SlopeScaleDepthBias = 0.5f;
        }

        public override void Render(RenderContext rc, RenderBin renderBin)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.RenderCamera.ProjectionMatrix;
                effectMatrices.View = rc.RenderCamera.ViewMatrix;
                effectMatrices.World = Matrix.Identity;
            }

            List<Drawable> drawableList = renderBin.DrawableList;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                int i = 0;
                foreach (Drawable drawable in drawableList)
                {
                    if (drawable.Enabled && drawable.Visible)
                    {
                        drawable.PreDraw(rc.GraphicsDevice);
                        drawable.Draw(rc.GraphicsDevice);
                        drawable.PostDraw(rc.GraphicsDevice);

                        rc.DrawCount++;
                        rc.VertexCount += drawable.VertexCount;
                    }
                    i++;
                }
            }
        }

    }

    // https://gist.github.com/JSandusky/82cf0022ba78c83e1d436947a6e00926
    public class ShadowCascadeRenderer : AbstractShadowRenderer<StockEffects.ShadowCascadeEffect>
    {
        private readonly VertexBuffer instanceVertexBuffer;

        public ShadowCascadeRenderer(StockEffects.ShadowCascadeEffect effect) : base(effect)
        {
            RasterizerState.ScissorTestEnable = true;

            instanceVertexBuffer = new VertexBuffer(effect.GraphicsDevice, ShadowInstanceVertex.VertexDeclaration, 4, BufferUsage.WriteOnly);
            ShadowInstanceVertex[] instances = new ShadowInstanceVertex[] {
                    new ShadowInstanceVertex(0), new ShadowInstanceVertex(1), new ShadowInstanceVertex(2), new ShadowInstanceVertex(3),
            };
            instanceVertexBuffer.SetData(instances);
        }

        public override void Render(RenderContext rc, RenderBin renderBin)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;

            LightRenderContext lightRenderContext = rc as LightRenderContext;
            LightCamera lightCamera = lightRenderContext.CullCamera as LightCamera;
            rc.GraphicsDevice.ScissorRectangle = lightCamera.ScissorRectangle;

            CascadeRenderBin cascadeRenderBin = renderBin as CascadeRenderBin;
            List<CascadeSplitInfo> cascadeSplitInfoList = cascadeRenderBin.CascadeSplitInfoList;

            List<Drawable> drawableList = renderBin.DrawableList;

            /*if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.RenderCamera.ProjectionMatrix;
                effectMatrices.View = rc.RenderCamera.ViewMatrix;
            }*/
            effect.World = Matrix.Identity;
            effect.ViewProjections = lightRenderContext.viewProjectionMatrices;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                int i = 0;
                foreach (Drawable drawable in drawableList)
                {
                    if (drawable.Enabled && drawable.Visible)
                    {
                        /*if ((effectMatrices != null) && (drawable is Transform transform))
                        {
                            effectMatrices.World = transform.WorldTransform;
                            pass.Apply();
                        }*/
                        if (drawable is Transform transform)
                        {
                            effect.World = transform.WorldTransform;
                            pass.Apply();
                        }
                        if (instanced)
                        {
                            int instanceOffset = cascadeSplitInfoList[i].StartSplit;
                            int instanceCount = cascadeSplitInfoList[i].EndSplit - cascadeSplitInfoList[i].StartSplit + 1;
                            //Console.WriteLine(cascadeSplitInfoList[i].StartSplit + "-" + cascadeSplitInfoList[i].EndSplit);

                            drawable.PreDrawInstanced(rc.GraphicsDevice, instanceVertexBuffer, instanceOffset);
                            drawable.DrawInstanced(rc.GraphicsDevice, instanceCount);
                            drawable.PostDrawInstanced(rc.GraphicsDevice);
                        }
                        else
                        {
                            drawable.PreDraw(rc.GraphicsDevice);
                            drawable.Draw(rc.GraphicsDevice);
                            drawable.PostDraw(rc.GraphicsDevice);
                        }
                        rc.DrawCount++;
                        // TODO should mutliply by instanceCount
                        rc.VertexCount += drawable.VertexCount;
                    }
                    i++;
                }
            }

            // FIXME ugly hack... 
            ((SharpDX.Direct3D11.DeviceContext)rc.GraphicsDevice.ContextHandle).GeometryShader.Set(null);
        }
    }

    public class VoxelShadowCascadeRenderer : AbstractShadowRenderer<StockEffects.ShadowCascadeEffect>
    {
        private readonly VertexBuffer instanceVertexBuffer;

        public VoxelShadowCascadeRenderer(StockEffects.ShadowCascadeEffect effect) : base(effect)
        {
            RasterizerState.ScissorTestEnable = true;

            instanceVertexBuffer = new VertexBuffer(effect.GraphicsDevice, ShadowInstanceVertex.VertexDeclaration, 4, BufferUsage.WriteOnly);
            ShadowInstanceVertex[] instances = new ShadowInstanceVertex[] {
                    new ShadowInstanceVertex(0), new ShadowInstanceVertex(1), new ShadowInstanceVertex(2), new ShadowInstanceVertex(3),
            };
            instanceVertexBuffer.SetData(instances);
        }

        public override void Render(RenderContext rc, RenderBin renderBin)
        {
            rc.GraphicsDevice.BlendState = BlendState;
            rc.GraphicsDevice.DepthStencilState = DepthStencilState;
            rc.GraphicsDevice.RasterizerState = RasterizerState;

            LightRenderContext lightRenderContext = rc as LightRenderContext;
            LightCamera lightCamera = lightRenderContext.CullCamera as LightCamera;
            rc.GraphicsDevice.ScissorRectangle = lightCamera.ScissorRectangle;

            CascadeRenderBin cascadeRenderBin = renderBin as CascadeRenderBin;
            List<CascadeSplitInfo> cascadeSplitInfoList = cascadeRenderBin.CascadeSplitInfoList;

            List<Drawable> drawableList = renderBin.DrawableList;

            /*if (effectMatrices != null)
            {
                effectMatrices.Projection = rc.RenderCamera.ProjectionMatrix;
                effectMatrices.View = rc.RenderCamera.ViewMatrix;
                effectMatrices.World = Matrix.Identity;
            }*/
            effect.World = Matrix.Identity;
            effect.ViewProjections = lightRenderContext.viewProjectionMatrices;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                int i = 0;
                foreach (Drawable drawable in drawableList)
                {
                    if (drawable.Enabled && drawable.Visible)
                    {
                        if (instanced)
                        {
                            int instanceOffset = cascadeSplitInfoList[i].StartSplit;
                            int instanceCount = cascadeSplitInfoList[i].EndSplit - cascadeSplitInfoList[i].StartSplit + 1;
                            //Console.WriteLine(cascadeSplitInfoList[i].StartSplit + "-" + cascadeSplitInfoList[i].EndSplit);

                            drawable.PreDrawInstanced(rc.GraphicsDevice, instanceVertexBuffer, instanceOffset);
                            drawable.DrawInstanced(rc.GraphicsDevice, instanceCount);
                            drawable.PostDrawInstanced(rc.GraphicsDevice);
                        }
                        else
                        {
                            drawable.PreDraw(rc.GraphicsDevice);
                            drawable.Draw(rc.GraphicsDevice);
                            drawable.PostDraw(rc.GraphicsDevice);
                        }
                        rc.DrawCount++;
                        // TODO should mutliply by instanceCount
                        rc.VertexCount += drawable.VertexCount;
                    }
                    i++;
                }
            }

            // FIXME ugly hack... 
            ((SharpDX.Direct3D11.DeviceContext)rc.GraphicsDevice.ContextHandle).GeometryShader.Set(null);
        }

    }

}

