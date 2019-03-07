using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLibrary.SceneGraph
{
    class EffectFactory
    {
        public static Effect CreateClippingEffect(GraphicsDevice gd)
        {
            return CreateClippingEffect(gd, Color.White);
        }

        public static Effect CreateBulletEffect(GraphicsDevice gd)
        {
            return CreateClippingEffect(gd, Color.Yellow);
        }

        public static Effect CreateClippingEffect(GraphicsDevice gd, Color color)
        {
            StockEffects.ClippingEffect effect = new StockEffects.ClippingEffect(gd);

            effect.ClippingPlane1 = VectorUtil.CreatePlane(Vector3.Right, 3.0f);
            effect.ClippingPlane2 = VectorUtil.CreatePlane(Vector3.Left, 3.0f);
            effect.ClippingPlane3 = VectorUtil.CreatePlane(Vector3.Up, 3.0f);
            effect.ClippingPlane4 = VectorUtil.CreatePlane(Vector3.Down, 3.0f);
            effect.Color = color;

            return effect;
        }

        public static Effect CreateFrustumEffect(GraphicsDevice gd)
        {
            Color c = new Color(Color.White, 255);
            return CreateBasicEffect3(gd, c);
        }

        public static Effect CreateVectorEffect(GraphicsDevice gd, bool clipping)
        {
            if (clipping)
            {
                return CreateClippingEffect(gd);
            }
            return CreateBasicEffect3(gd);
        }

        public static Effect CreateVectorEffect(GraphicsDevice gd, bool clipping, Color color)
        {
            if (clipping)
            {
                return CreateClippingEffect(gd, color);
            }
            return CreateBasicEffect3(gd, color);
        }

        public static Effect CreateBillboardEffect(GraphicsDevice gd)
        {
            //BasicEffect effect = new BasicEffect(gd);
            StockEffects.ShadowMapEffect effect = new StockEffects.ShadowMapEffect(gd);

            // primitive color
            //effect.VertexColorEnabled = true;
            //effect.AmbientLightColor = new Vector3(1f, 1f, 1f);
            //effect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            //effect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            //effect.SpecularPower = 5.0f;
            //effect.Alpha = 1.0f;

            //effect.TextureEnabled = true;
            //effect.Texture = createTileTextureArray(gd, getTiles());

            return effect;
        }

        // 3 lights (x red, y green, z blue)
        public static Effect CreateBasicEffect1(GraphicsDevice gd)
        {
            BasicEffect effect = new BasicEffect(gd);

            // primitive color
            effect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            effect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            effect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            effect.SpecularPower = 5.0f;
            effect.Alpha = 1.0f;

            effect.LightingEnabled = true;
            if (effect.LightingEnabled)
            {
                // enable each light individually
                effect.DirectionalLight0.Enabled = true;
                if (effect.DirectionalLight0.Enabled)
                {
                    // x direction is red
                    effect.DirectionalLight0.DiffuseColor = new Vector3(1, 0, 0); // range is 0 to 1
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, 0, 0));
                    // points from the light to the origin of the scene
                    effect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                effect.DirectionalLight1.Enabled = true;
                if (effect.DirectionalLight1.Enabled)
                {
                    // y direction is green
                    effect.DirectionalLight1.DiffuseColor = new Vector3(0, 1, 0);
                    effect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0, -1, 0));
                    effect.DirectionalLight1.SpecularColor = Vector3.One;
                }

                effect.DirectionalLight2.Enabled = true;
                if (effect.DirectionalLight2.Enabled)
                {
                    // z direction is blue
                    effect.DirectionalLight2.DiffuseColor = new Vector3(0, 0, 1);
                    effect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0, 0, -1));
                    effect.DirectionalLight2.SpecularColor = Vector3.One;
                }
            }

            return effect;
        }

        public static Effect CreateBasicEffect2(GraphicsDevice gd)
        {
            BasicEffect effect = new BasicEffect(gd);

            // primitive color
            effect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            effect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            effect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            effect.SpecularPower = 5.0f;
            effect.Alpha = 1.0f;

            effect.LightingEnabled = true;
            if (effect.LightingEnabled)
            {
                // enable each light individually
                effect.DirectionalLight0.Enabled = true;
                if (effect.DirectionalLight0.Enabled)
                {
                    // x direction is red
                    effect.DirectionalLight0.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f); // range is 0 to 1
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
                    // points from the light to the origin of the scene
                    //effect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                effect.DirectionalLight1.Enabled = false;
                if (effect.DirectionalLight1.Enabled)
                {
                    // y direction is green
                    effect.DirectionalLight1.DiffuseColor = new Vector3(0, 1, 0);
                    effect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0, -1, 0));
                    effect.DirectionalLight1.SpecularColor = Vector3.One;
                }

                effect.DirectionalLight2.Enabled = false;
                if (effect.DirectionalLight2.Enabled)
                {
                    // z direction is blue
                    effect.DirectionalLight2.DiffuseColor = new Vector3(0, 0, 1);
                    effect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0, 0, -1));
                    effect.DirectionalLight2.SpecularColor = Vector3.One;
                }
            }

            return effect;
        }

        public static Effect CreateBasicEffect3(GraphicsDevice gd)
        {
            BasicEffect effect = new BasicEffect(gd);

            // primitive color
            effect.AmbientLightColor = new Vector3(1f, 1f, 1f);
            effect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            effect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            //effect.SpecularPower = 5.0f;
            //effect.Alpha = 1.0f;

            return effect;
        }

        public static Effect CreateBasicEffect3(GraphicsDevice gd, Color color)
        {
            BasicEffect effect = new BasicEffect(gd);

            // primitive color
            effect.VertexColorEnabled = true;
            effect.AmbientLightColor = new Vector3(1f, 1f, 1f);
            effect.DiffuseColor = color.ToVector3();
            //effect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            //effect.SpecularPower = 5.0f;
            effect.Alpha = color.A / 255.0f;

            return effect;
        }

        public static Effect CreateInstancedEffect(GraphicsDevice gd)
        {
            StockEffects.InstancedEffect effect = new StockEffects.InstancedEffect(gd);

            return effect;
        }

        public static StockEffects.ShadowEffect CreateShadowEffect(GraphicsDevice gd)
        {
            StockEffects.ShadowEffect effect = new StockEffects.ShadowEffect(gd);
            return effect;
        }

        public static StockEffects.ShadowCascadeEffect CreateShadowCascadeEffect(GraphicsDevice gd)
        {
            StockEffects.ShadowCascadeEffect effect = new StockEffects.ShadowCascadeEffect(gd);
            return effect;
        }

    }

}
