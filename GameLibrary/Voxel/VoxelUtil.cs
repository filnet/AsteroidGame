using GameLibrary.Component.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Voxel;

namespace GameLibrary.Voxel
{
    class VoxelUtil
    {
        public static VoxelEffect CreateVoxelEffect(GraphicsDevice gd)
        {
            VoxelEffect effect = new VoxelEffect(gd);

            // primitive color
            effect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            effect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            effect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            effect.SpecularPower = 5.0f;
            effect.Alpha = 1.0f;

            //effect.VertexColorEnabled = true;
            //effect.PreferPerPixelLighting = true;

            effect.LightingEnabled = true;
            if (effect.LightingEnabled)
            {
                effect.DirectionalLight0.Enabled = true;
                if (effect.DirectionalLight0.Enabled)
                {
                    effect.DirectionalLight0.DiffuseColor = new Vector3(0.6f, 0.6f, 0.6f); // range is 0 to 1
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
                    // points from the light to the origin of the scene
                    //effect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                effect.DirectionalLight1.Enabled = false;
                effect.DirectionalLight2.Enabled = false;
            }

            effect.TextureEnabled = true;
            effect.Texture = createTileTextureArray(gd, getTiles());
            effect.WireframeTexture = createWireframeTexture(gd, 1.0f);
            return effect;
        }

        public static VoxelSimpleEffect CreateVoxelTransparentEffect(GraphicsDevice gd)
        {
            VoxelSimpleEffect effect = new VoxelSimpleEffect(gd);

            // primitive color
            effect.SpecularColor = new Vector3(1.0f, 1.0f, 1.0f); ; // new Vector3(0.25f, 0.25f, 0.25f);
            effect.SpecularPower = 5.0f;
            effect.Alpha = 0.5f;

            //effect.VertexColorEnabled = true;
            effect.PreferPerPixelLighting = true;

            effect.LightingEnabled = true;
            if (effect.LightingEnabled)
            {
                effect.DirectionalLight0.Enabled = true;
                if (effect.DirectionalLight0.Enabled)
                {
                    effect.DirectionalLight0.DiffuseColor = new Vector3(0.6f, 0.6f, 0.6f); // range is 0 to 1
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
                    effect.DirectionalLight0.SpecularColor = Vector3.One;

                    // points from the light to the origin of the scene
                    //effect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                effect.DirectionalLight1.Enabled = false;
                effect.DirectionalLight2.Enabled = false;
            }

            effect.TextureEnabled = true;
            effect.Texture = createTileTextureArray(gd, getTiles());

            return effect;
        }

        public static VoxelWaterEffect CreateVoxelWaterEffect(GraphicsDevice gd)
        {
            VoxelWaterEffect effect = new VoxelWaterEffect(gd);

            // primitive color
            effect.SpecularColor = new Vector3(1.0f, 1.0f, 1.0f); ; // new Vector3(0.25f, 0.25f, 0.25f);
            effect.SpecularPower = 5.0f;
            effect.Alpha = 0.5f;

            //effect.VertexColorEnabled = true;
            effect.PreferPerPixelLighting = true;

            effect.LightingEnabled = true;
            if (effect.LightingEnabled)
            {
                effect.DirectionalLight0.Enabled = true;
                if (effect.DirectionalLight0.Enabled)
                {
                    effect.DirectionalLight0.DiffuseColor = new Vector3(0.6f, 0.6f, 0.6f); // range is 0 to 1
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
                    effect.DirectionalLight0.SpecularColor = Vector3.One;

                    // points from the light to the origin of the scene
                    //effect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                effect.DirectionalLight1.Enabled = false;
                effect.DirectionalLight2.Enabled = false;
            }

            effect.TextureEnabled = true;
            effect.Texture = createTileTextureArray(gd, getTiles());
            //effect.WireframeTexture = createWireframeTexture(gd, 1.0f);

            return effect;
        }

        public static VoxelShadowEffect CreateVoxelShadowEffect(GraphicsDevice gd)
        {
            VoxelShadowEffect effect = new VoxelShadowEffect(gd);
            return effect;
        }

        public static Effect CreateVoxelEffect1(GraphicsDevice gd)
        {
            VoxelEffect effect = new VoxelEffect(gd);

            // primitive color
            effect.AmbientLightColor = new Vector3(0.3f, 0.3f, 0.3f);
            effect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            effect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            effect.SpecularPower = 5.0f;
            effect.Alpha = 1.0f;

            effect.VertexColorEnabled = true;
            //effect.PreferPerPixelLighting = true;

            effect.LightingEnabled = true;
            if (effect.LightingEnabled)
            {
                effect.DirectionalLight0.Enabled = true;
                if (effect.DirectionalLight0.Enabled)
                {
                    effect.DirectionalLight0.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f); // range is 0 to 1
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
                    // points from the light to the origin of the scene
                    //effect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                effect.DirectionalLight1.Enabled = false;
                effect.DirectionalLight2.Enabled = false;
            }

            effect.TextureEnabled = true;
            effect.Texture = createTileTextureArray(gd, getTiles());
            return effect;
        }

        private static List<string> getTiles()
        {
            // grass 
            // earth + grass top
            // earth
            // rock
            // rock + snow top
            // snow

            FaceType[] types = (FaceType[])Enum.GetValues(typeof(FaceType));
            List<string> tiles = new List<string>(types.Length - 1);
            foreach (FaceType faceType in types)
            {
                if (faceType != FaceType.None)
                {
                    tiles.Add(faceType.ToString().ToLower());
                }
            }


            /*
            String[] tiles = new String[]
            {
                "earth", "grass", "rock", "snow", "water", "glass", "test", "test_left", "test_right", "test_bottom", "test_top", "test_back", "test_front"
            };
            */
            return tiles;
        }

        // https://blogs.msdn.microsoft.com/shawnhar/2009/09/14/texture-filtering-mipmaps/
        // NOTE mimpap generation is set in pipeline tool
        private static Texture2D createTileTextureArray(GraphicsDevice gd, List<string> tiles)
        {
            Texture2D textureArray = new Texture2D(gd, 32, 32, true, SurfaceFormat.Color, tiles.Count);
            for (int slice = 0; slice < tiles.Count; slice++)
            {
                String name = tiles[slice];
                Texture2D texture = AsteroidGame.AsteroidGame.Instance().Content.Load<Texture2D>(name);
                int width = texture.Width;
                int height = texture.Height;
                for (var level = 0; level < texture.LevelCount; level++)
                {
                    var size = width * height;
                    var data = new Color[size];
                    texture.GetData(level, 0, null, data, 0, data.Length);

                    textureArray.SetData(level, slice, null, data, 0, data.Length);

                    width /= 2;
                    height /= 2;
                }
            }
            return textureArray;
        }

        private static Texture2D createWireframeTexture(GraphicsDevice gd, float thickness)
        {
            Color c = Color.Black;
            int width = 4096;
            Texture2D texture = new Texture2D(gd, width, 1, true, SurfaceFormat.Color);
            int levelCount = texture.LevelCount;
            for (var level = 0; level < levelCount; level++)
            {
                // we draw only half the line...
                float t = thickness / 2f;
                // desaturate
                if (true)
                // t -> t/3 in levelCount steps
                {
                    t = MathUtil.Lerp(t, t / 3.0f, (float)level / (float)(levelCount - 1));
                }
                else
                {
                    // experimental... not great...
                    if (level > levelCount / 2)
                    {
                        t /= 3;
                    }
                }
                var data = new Color[width];
                for (int i = width - 1; i >= 0; i--)
                {
                    float alpha = t >= 1.0f ? 1.0f : t;
                    t = Math.Max(t - 1.0f, 0);
                    data[i] = new Color(c, alpha);
                }
                texture.SetData(level, 0, null, data, 0, data.Length);
                width /= 2;
            }
            return texture;
        }

    }
}

