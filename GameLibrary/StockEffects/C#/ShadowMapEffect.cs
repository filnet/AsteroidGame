#region File Description
//-----------------------------------------------------------------------------
// ShadowMapEffect.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
#endregion

namespace StockEffects
{
    /// <summary>
    /// 
    /// </summary>
    public class ShadowMapEffect : Effect, IEffectMatrices
    {
        #region Effect Parameters

        EffectParameter textureParam;
        EffectParameter worldViewProjParam;

        #endregion

        #region Fields

        Matrix world = Matrix.Identity;
        Matrix view = Matrix.Identity;
        Matrix projection = Matrix.Identity;

        Matrix worldView;

        EffectDirtyFlags dirtyFlags = EffectDirtyFlags.All;

        #endregion

        #region Public Properties


        /// <summary>
        /// Gets or sets the world matrix.
        /// </summary>
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
        }


        /// <summary>
        /// Gets or sets the current texture.
        /// </summary>
        public Texture2D Texture
        {
            get { return textureParam.GetValueTexture2D(); }
            set { textureParam.SetValue(value); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new ShadowMapEffect with default parameter settings.
        /// </summary>
        public ShadowMapEffect(GraphicsDevice device)
            : base(AsteroidGame.AsteroidGame.Instance().Content.Load<Effect>("Effects/ShadowMapEffect"))
        {
            CacheEffectParameters(null);
        }

        /// <summary>
        /// Creates a new ShadowMapEffect by cloning parameter settings from an existing instance.
        /// </summary>
        protected ShadowMapEffect(ShadowMapEffect cloneSource)
            : base(cloneSource)
        {
            CacheEffectParameters(cloneSource);

            world = cloneSource.world;
            view = cloneSource.view;
            projection = cloneSource.projection;
        }

        /// <summary>
        /// Creates a clone of the current ShadowMapEffect instance.
        /// </summary>
        public override Effect Clone()
        {
            return new ShadowMapEffect(this);
        }


        /// <summary>
        /// Looks up shortcut references to our effect parameters.
        /// </summary>
        void CacheEffectParameters(ShadowMapEffect cloneSource)
        {
            textureParam = Parameters["Texture"];
            //worldParam = Parameters["World"];
            //worldInverseTransposeParam = Parameters["WorldInverseTranspose"];
            worldViewProjParam = Parameters["WorldViewProj"];
        }


        /// <summary>
        /// Lazily computes derived parameter values immediately before applying the effect.
        /// </summary>
        protected override void OnApply()
        {
            // Recompute the world+view+projection matrix?
            dirtyFlags = EffectHelpers.SetWorldViewProj(dirtyFlags, ref world, ref view, ref projection, ref worldView, worldViewProjParam);
        }


        #endregion
    }
}
