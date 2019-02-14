#region File Description
//-----------------------------------------------------------------------------
// Effect.cs
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
    public class ShadowEffect : Effect, IEffectMatrices
    {
        #region Effect Parameters

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

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new Effect with default parameter settings.
        /// </summary>
        public ShadowEffect(GraphicsDevice device)
            : base(AsteroidGame.AsteroidGame.Instance().Content.Load<Effect>("Effects/ShadowEffect"))
        {
            CacheEffectParameters(null);
        }

        /// <summary>
        /// Creates a new Effect by cloning parameter settings from an existing instance.
        /// </summary>
        protected ShadowEffect(ShadowEffect cloneSource) : base(cloneSource)
        {
            CacheEffectParameters(cloneSource);

            world = cloneSource.world;
            view = cloneSource.view;
            projection = cloneSource.projection;

            //CurrentTechnique = Techniques[0];
        }

        /// <summary>
        /// Creates a clone of the current Effect instance.
        /// </summary>
        public override Effect Clone()
        {
            return new ShadowEffect(this);
        }

        /// <summary>
        /// Looks up shortcut references to our effect parameters.
        /// </summary>
        void CacheEffectParameters(ShadowEffect cloneSource)
        {
            worldViewProjParam = Parameters["WorldViewProj"];
        }

        /// <summary>
        /// Lazily computes derived parameter values immediately before applying the effect.
        /// </summary>
        protected override void OnApply()
        {
            // Recompute the world+view+projection matrix or fog vector?
            dirtyFlags = EffectHelpers.SetWorldViewProj(dirtyFlags, ref world, ref view, ref projection, ref worldView, worldViewProjParam);

            //CurrentTechnique = Techniques[shaderIndex];
        }

        #endregion
    }
}
