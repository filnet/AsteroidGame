#region File Description
//-----------------------------------------------------------------------------
// VoxelEffect.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StockEffects;
using System;
#endregion

namespace StockEffects
{
    public class ShadowCascadeEffect : Effect /*, IEffectMatrices*/
    {
        #region Effect Parameters

        EffectParameter worldParam;
        EffectParameter viewProjectionsParam;

        #endregion

        #region Fields

        /*
        Matrix world = Matrix.Identity;
        Matrix view = Matrix.Identity;
        Matrix projection = Matrix.Identity;

        Matrix worldView;
        */
        Matrix world;
        Matrix[] viewProjections;

        GraphicsDevice device;

        SharpDX.Direct3D11.GeometryShader geometryShader;

        EffectDirtyFlags dirtyFlags = EffectDirtyFlags.All;

        #endregion

        #region Public Properties

        public Matrix[] ViewProjections
        {
            get { return viewProjections; }

            set
            {
                viewProjections = value;
                dirtyFlags |= EffectDirtyFlags.World | EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.Fog;
            }
        }

        public Matrix World
        {
            get { return world; }

            set
            {
                world = value;
                dirtyFlags |= EffectDirtyFlags.World | EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.Fog;
            }
        }

        /// <summary>
        /// Gets or sets the world matrix.
        /// </summary>
        /*public Matrix World
        {
            get { return world; }

            set
            {
                world = value;
                dirtyFlags |= EffectDirtyFlags.World | EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.Fog;
            }
        }


        /// <summary>
        /// Gets or sets the view matrix.
        /// </summary>
        public Matrix View
        {
            get { return view; }

            set
            {
                view = value;
                dirtyFlags |= EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.EyePosition | EffectDirtyFlags.Fog;
            }
        }


        /// <summary>
        /// Gets or sets the projection matrix.
        /// </summary>
        public Matrix Projection
        {
            get { return projection; }

            set
            {
                projection = value;
                dirtyFlags |= EffectDirtyFlags.WorldViewProj;
            }
        }*/

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new ShadowCascadeEffect with default parameter settings.
        /// </summary>
        public ShadowCascadeEffect(GraphicsDevice device)
            : base(AsteroidGame.AsteroidGame.Instance().Content.Load<Effect>("Effects/ShadowCascadeEffect"))
        {
            CacheEffectParameters(null);

            this.device = device;

            //var CompiledGS = SharpDX.D3DCompiler.ShaderBytecode.CompileFromFile(@"Content\Effects\ShadowCascadeGS.hlsl", "MainGS", "gs_4_0");
            //geometryShader = new SharpDX.Direct3D11.GeometryShader((SharpDX.Direct3D11.Device)device.Handle, CompiledGS.Bytecode);
            geometryShader = null;
        }

        /// <summary>
        /// Creates a new ShadowCascadeEffect by cloning parameter settings from an existing instance.
        /// </summary>
        protected ShadowCascadeEffect(ShadowCascadeEffect cloneSource) : base(cloneSource)
        {
            CacheEffectParameters(cloneSource);

            /*world = cloneSource.world;
            view = cloneSource.view;
            projection = cloneSource.projection;*/
            world = cloneSource.world;
            viewProjections = cloneSource.viewProjections;

            // TODO not sure it works...
            geometryShader = cloneSource.geometryShader;

            //CurrentTechnique = Techniques[0];
        }

        /// <summary>
        /// Creates a clone of the current ShadowCascadeEffect instance.
        /// </summary>
        public override Effect Clone()
        {
            return new ShadowCascadeEffect(this);
        }

        /// <summary>
        /// Looks up shortcut references to our effect parameters.
        /// </summary>
        void CacheEffectParameters(ShadowCascadeEffect cloneSource)
        {
            worldParam = Parameters["World"];
            viewProjectionsParam = Parameters["ViewProjections"];
        }

        /// <summary>
        /// Lazily computes derived parameter values immediately before applying the effect.
        /// </summary>
        protected override void OnApply()
        {
            // Recompute the world+view+projection matrix or fog vector?
            //dirtyFlags = EffectHelpers.SetWorldViewProj(dirtyFlags, ref world, ref view, ref projection, ref worldView, worldViewProjParam);
            worldParam.SetValue(world);
            viewProjectionsParam.SetValue(viewProjections);

            // FIXME: could be done only when needed (i.e. when the CBuffers are dirty) ?
            if (geometryShader != null)
            {
                ((SharpDX.Direct3D11.DeviceContext)device.ContextHandle).GeometryShader.Set(geometryShader);
                CopyCBuffers(device);
            }

            //CurrentTechnique = Techniques[shaderIndex];
        }

        private void CopyCBuffers(GraphicsDevice gd)
        {
            var buffers = ((SharpDX.Direct3D11.DeviceContext)gd.ContextHandle).VertexShader.GetConstantBuffers(0, 8);
            if (buffers != null)
            {
                for (int i = 0; i < buffers.Length; ++i)
                {
                    ((SharpDX.Direct3D11.DeviceContext)gd.ContextHandle).GeometryShader.SetConstantBuffer(i, buffers[i]);
                }
            }
        }

        #endregion
    }
}
