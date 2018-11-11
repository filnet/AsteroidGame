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
    public class GraphicsContext
    {
        public GraphicsDevice GraphicsDevice;
        public ICameraComponent Camera;
        public GameTime GameTime;
    }

    public abstract class Renderer
    {
        protected BlendState BlendState;
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

        public abstract void Render(GraphicsContext gc, List<GeometryNode> nodeList);
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

        public override void Render(GraphicsContext gc, List<GeometryNode> nodeList)
        {
            if (!Enabled)
            {
                renderer.Render(gc, nodeList);
            }
            double currentTime = gc.GameTime.TotalGameTime.TotalMilliseconds;
            if (lastTime == -1 || currentTime - lastTime >= 100)
            {
                lastTime = currentTime;
                index++;
            }
            index %= nodeList.Count;
            if (index != 0)
            {
                renderer.Render(gc, nodeList.GetRange(0, index));
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

        public override void Render(GraphicsContext gc, List<GeometryNode> nodeList)
        {
            gc.GraphicsDevice.BlendState = BlendState;
            gc.GraphicsDevice.DepthStencilState = DepthStencilState;
            gc.GraphicsDevice.RasterizerState = RasterizerState;
            gc.GraphicsDevice.SamplerStates[0] = SamplerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = gc.Camera.ProjectionMatrix;
                effectMatrices.View = gc.Camera.ViewMatrix;
            }

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                foreach (GeometryNode node in nodeList)
                {
                    if (!node.Enabled || !node.Visible)
                    {
                        break;
                    }
                    if (effectMatrices != null)
                    {
                        effectMatrices.World = node.WorldTransform;
                    }
                    pass.Apply();
                    node.preDraw(gc.GraphicsDevice);
                    node.Draw(gc.GraphicsDevice);
                    node.postDraw(gc.GraphicsDevice);
                }
            }
        }

        private void renderOld(GraphicsContext gc, List<GeometryNode> nodeList)
        {
            foreach (GeometryNode node in nodeList)
            {
                if (effectMatrices != null)
                {
                    effectMatrices.World = node.WorldTransform;
                }
                node.preDraw(gc.GraphicsDevice);
                // TODO in case of multiple passes, it might be more efficient
                // to loop over passes then over geometries and not the other
                // way around as is currently done
                //Console.Out.WriteLine(Effect.CurrentTechnique.Passes.Count);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    //dc.pass = pass;
                    node.Draw(gc.GraphicsDevice);
                }
                node.postDraw(gc.GraphicsDevice);
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

    public class BoundRenderer : EffectRenderer
    {
        private readonly GeometryNode boundingGeometry;

        public BoundRenderer(Effect effect, GeometryNode boundingGeometry) : base(effect)
        {
            this.boundingGeometry = boundingGeometry;

            BlendState = BlendState.AlphaBlend;
        }

        public override void Render(GraphicsContext gc, List<GeometryNode> nodeList)
        {
            gc.GraphicsDevice.BlendState = BlendState;
            gc.GraphicsDevice.DepthStencilState = DepthStencilState;
            gc.GraphicsDevice.RasterizerState = RasterizerState;
            gc.GraphicsDevice.SamplerStates[0] = SamplerState;

            if (effectMatrices != null)
            {
                effectMatrices.Projection = gc.Camera.ProjectionMatrix;
                effectMatrices.View = gc.Camera.ViewMatrix;
            }

            boundingGeometry.preDraw(gc.GraphicsDevice);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                foreach (GeometryNode node in nodeList)
                {
                    if (effectMatrices != null)
                    {
                        effectMatrices.World = node.WorldBoundingVolume.WorldMatrix;
                    }
                    pass.Apply();
                    boundingGeometry.Draw(gc.GraphicsDevice);
                }
            }
            boundingGeometry.postDraw(gc.GraphicsDevice);
        }
    }

}
