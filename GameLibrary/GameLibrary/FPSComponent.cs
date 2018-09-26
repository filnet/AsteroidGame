using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Diagnostics;

namespace GameLibrary
{
    public class FPSComponent : DrawableGameComponent
    {

        private double framesPerSecond;

        private TimeSpan totalElapsedTime = TimeSpan.Zero;
        private TimeSpan lastTime = TimeSpan.Zero;
        private int frames;
        private int totalFrames;

        public Stopwatch UpdateStopwatch = new Stopwatch();
        public Stopwatch DrawStopwatch = new Stopwatch();

        //(float)StopwatchUpdate.ElapsedTicks/(float)Stopwatch.Frequency*1000.0f


        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;

        private String text = "";

        public FPSComponent(Game game)
            : base(game)
        {
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the sprite font. The sprite font has a 3 pixel outer glow
            // baked into it so we need to decrease the spacing so that the
            // SpriteFont will render correctly.
            spriteFont = Game.Content.Load<SpriteFont>(@"Fonts\DemoFont12pt");
            spriteFont.Spacing = -4.0f;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            UpdateFrameRate(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            Vector2 fontPos = new Vector2(1.0f, 50.0f);


            // save state
            //BlendState blendState = GraphicsDevice.BlendState;
            //DepthStencilState depthStencilState = GraphicsDevice.DepthStencilState;
            //RasterizerState rasterizerState = GraphicsDevice.RasterizerState;
            //SamplerState samplerState = GraphicsDevice.SamplerStates[0];

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawString(spriteFont, text, fontPos, Color.Yellow);
            spriteBatch.End();

            // restore state
            //GraphicsDevice.BlendState = blendState;
            //GraphicsDevice.DepthStencilState = depthStencilState;
            //GraphicsDevice.RasterizerState = rasterizerState;
            //GraphicsDevice.SamplerStates[0] = samplerState;

            IncrementFrameCounter();
        }

        private void UpdateFrameRate(GameTime gameTime)
        {
            totalElapsedTime += gameTime.ElapsedGameTime;

            if (totalElapsedTime - lastTime > TimeSpan.FromSeconds(1))
            {
                TimeSpan elapsed = totalElapsedTime - lastTime;
                framesPerSecond = (double) frames / elapsed.TotalSeconds;
                frames = 0;
                lastTime = totalElapsedTime;
            
                text = framesPerSecond.ToString("0") + " fps";
                if (gameTime.IsRunningSlowly)
                {
                    text += " (Slow!)";
                }
                text += " (frames: " + totalFrames + " update: " + UpdateStopwatch.ElapsedMilliseconds + " draw: " + DrawStopwatch.ElapsedMilliseconds + ")";
                Console.WriteLine(text);
            }
        }

        private void IncrementFrameCounter()
        {
            ++frames;
            ++totalFrames;
        }

    }

}