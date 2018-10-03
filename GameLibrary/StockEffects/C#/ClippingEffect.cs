#region File Description
//-----------------------------------------------------------------------------
// BasicEffect.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using AsteroidGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace StockEffects
{
    /// <summary>
    /// Built-in effect that supports ...
    /// </summary>
    public class ClippingEffect : Effect, IEffectMatrices
    {
        #region Effect Parameters

        EffectParameter worldParam;
        EffectParameter worldViewProjParam;
        EffectParameter clippingPlane1Param;
        EffectParameter clippingPlane2Param;
        EffectParameter clippingPlane3Param;
        EffectParameter clippingPlane4Param;
        EffectParameter colorParam;

        #endregion

        #region Fields

        Matrix world = Matrix.Identity;
        Matrix view = Matrix.Identity;
        Matrix projection = Matrix.Identity;

        Vector4 clippingPlane1;
        Vector4 clippingPlane2;
        Vector4 clippingPlane3;
        Vector4 clippingPlane4;

        Color color;

        //bool clippingPlane1;
        //bool clippingPlane2;
        //Vector4 clippingPlane3;
        //Vector4 clippingPlane4;

        EffectDirtyFlags dirtyFlags = EffectDirtyFlags.All;

        private Matrix worldView;

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
                dirtyFlags |= EffectDirtyFlags.World | EffectDirtyFlags.WorldViewProj;
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
                dirtyFlags |= EffectDirtyFlags.WorldViewProj;
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

        public Vector4 ClippingPlane1
        {
            get { return clippingPlane1; }
            set
            {
                clippingPlane1 = value;
                dirtyFlags |= EffectDirtyFlags.ClippingPlane;
            }
        }

        public Vector4 ClippingPlane2
        {
            get { return clippingPlane2; }
            set
            {
                clippingPlane2 = value;
                dirtyFlags |= EffectDirtyFlags.ClippingPlane;
            }
        }

        public Vector4 ClippingPlane3
        {
            get { return clippingPlane3; }
            set
            {
                clippingPlane3 = value;
                dirtyFlags |= EffectDirtyFlags.ClippingPlane;
            }
        }

        public Vector4 ClippingPlane4
        {
            get { return clippingPlane4; }
            set
            {
                clippingPlane4 = value;
                dirtyFlags |= EffectDirtyFlags.ClippingPlane;
            }
        }

        public Color Color
        {
            get { return color; }
            set
            {
                color = value;
                dirtyFlags |= EffectDirtyFlags.MaterialColor;
            }
        }

        #region Methods


        /// <summary>
        /// Creates a new BasicEffect with default parameter settings.
        /// </summary>
        public ClippingEffect(GraphicsDevice device)
            : base(AsteroidGame.AsteroidGame.Instance().Content.Load<Effect>("ClippingEffect"))
        {
            // TODO fix use of AsteroidGame.AsteroidGame.Instance(). above
            CacheEffectParameters(null);
        }


        /// <summary>
        /// Creates a new BasicEffect by cloning parameter settings from an existing instance.
        /// </summary>
        protected ClippingEffect(ClippingEffect cloneSource)
            : base(cloneSource)
        {
            CacheEffectParameters(cloneSource);

            world = cloneSource.world;
            view = cloneSource.view;
            projection = cloneSource.projection;
            clippingPlane1 = cloneSource.clippingPlane1;
            clippingPlane2 = cloneSource.clippingPlane2;
            clippingPlane3 = cloneSource.clippingPlane3;
            clippingPlane4 = cloneSource.clippingPlane4;
            color = cloneSource.color;
        }


        /// <summary>
        /// Creates a clone of the current BasicEffect instance.
        /// </summary>
        public override Effect Clone()
        {
            return new ClippingEffect(this);
        }


        /// <summary>
        /// Looks up shortcut references to our effect parameters.
        /// </summary>
        void CacheEffectParameters(ClippingEffect cloneSource)
        {
            worldParam = Parameters["World"];
            worldViewProjParam = Parameters["WorldViewProj"];
            clippingPlane1Param = Parameters["ClippingPlane1"];
            clippingPlane2Param = Parameters["ClippingPlane2"];
            clippingPlane3Param = Parameters["ClippingPlane3"];
            clippingPlane4Param = Parameters["ClippingPlane4"];
            colorParam = Parameters["Color"];
        }


        /// <summary>
        /// Lazily computes derived parameter values immediately before applying the effect.
        /// </summary>
        protected override void OnApply()
        {
            // Recompute the world+view+projection matrix or fog vector?
            dirtyFlags = EffectHelpers.SetWorldViewProj(dirtyFlags, ref world, ref view, ref projection, ref worldView, worldViewProjParam, worldParam);
            if ((dirtyFlags & EffectDirtyFlags.ClippingPlane) != 0)
            {
                clippingPlane1Param.SetValue(clippingPlane1);
                clippingPlane2Param.SetValue(clippingPlane2);
                clippingPlane3Param.SetValue(clippingPlane3);
                clippingPlane4Param.SetValue(clippingPlane4);
                dirtyFlags &= ~EffectDirtyFlags.ClippingPlane;
            }
            if ((dirtyFlags & EffectDirtyFlags.MaterialColor) != 0)
            {
                colorParam.SetValue(color.ToVector4());
                dirtyFlags &= ~EffectDirtyFlags.MaterialColor;
            }
        }


        #endregion
    }
}
