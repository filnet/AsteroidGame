#region File Description
//-----------------------------------------------------------------------------
// FpsCounter.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.Util;

#endregion

namespace GameLibrary.Debug
{
    /// <summary>
    /// Component for FPS measure and draw.
    /// </summary>
    public class FpsCounter : DrawableGameComponent
    {
        #region Fields

        private double framesPerSecond;

        private TimeSpan totalElapsedTime = TimeSpan.Zero;
        private TimeSpan lastTime = TimeSpan.Zero;
        private int frameCount;
        private int totalFrameCount;

        // Stopwatch for fps measuring.
        //private Stopwatch stopwatch;

        // stringBuilder for FPS counter draw.
        private StringBuilder stringBuilder = new StringBuilder(64);

        private DebugManager debugManager;

        #endregion

        #region Properties

        /// <summary>
        /// Gets current FPS
        /// </summary>
        public double Fps { get { return framesPerSecond; } }

        /// <summary>
        /// Gets/Sets FPS sample duration.
        /// </summary>
        public TimeSpan SampleSpan { get; set; }

        #endregion

        #region Constructors

        public FpsCounter(Game game)
            : base(game)
        {
            SampleSpan = TimeSpan.FromSeconds(1);
        }

        #endregion

        #region Public methods

        public override void Initialize()
        {
            // Get debug manager from game service.
            debugManager = Game.Services.GetService(typeof(DebugManager)) as DebugManager;

            if (debugManager == null)
                throw new InvalidOperationException("DebugManaer is not registered.");

            // Register 'fps' command if debug command is registered as a service.
            IDebugCommandHost host = Game.Services.GetService(typeof(IDebugCommandHost)) as IDebugCommandHost;

            if (host != null)
            {
                host.RegisterCommand("fps", "FPS Counter", this.CommandExecute);
                Visible = true;
            }

            // Initialize parameters.
            framesPerSecond = 0;
            frameCount = 0;
            totalFrameCount = 0;
            //stopwatch = Stopwatch.StartNew();
            stringBuilder.Length = 0;

            base.Initialize();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// FPS command implementation.
        /// </summary>
        private void CommandExecute(IDebugCommandHost host,
                                    string command, IList<string> arguments)
        {
            if (arguments.Count == 0)
                Visible = !Visible;

            foreach (string arg in arguments)
            {
                switch (arg.ToLower())
                {
                    case "on":
                        Visible = true;
                        break;
                    case "off":
                        Visible = false;
                        break;
                }
            }
        }

        #region Update and Draw

        public override void Update(GameTime gameTime)
        {
            //if (stopwatch.Elapsed > SampleSpan)
            //{
            //    // Update FPS value and start next sampling period.
            //    Fps = (float)sampleFrames / (float)stopwatch.Elapsed.TotalSeconds;

            //    stopwatch.Reset();
            //    stopwatch.Start();
            //    sampleFrames = 0;

            //    // Update draw string.
            //    stringBuilder.Length = 0;
            //    stringBuilder.Append("FPS: ");
            //    stringBuilder.AppendNumber(Fps);
            //}
            base.Update(gameTime);
            UpdateFrameRate(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            IncrementFrameCounter();

            SpriteBatch spriteBatch = debugManager.SpriteBatch;
            SpriteFont font = debugManager.DebugFont;

            //// Compute size of border area.
            //Vector2 size = font.MeasureString("X");
            //Rectangle rc = new Rectangle(0, 0, (int)(size.X * 14f), (int)(size.Y * 1.3f));

            //Layout layout = new Layout(spriteBatch.GraphicsDevice.Viewport);
            //rc = layout.Place(rc, 0.01f, 0.01f, Alignment.TopLeft);

            //// Place FPS string in border area.
            //size = font.MeasureString(stringBuilder);
            //layout.ClientArea = rc;
            //Vector2 pos = layout.Place(size, 0, 0.1f, Alignment.CenterLeft);
            Vector2 pos = new Vector2(1, 2);

            // Draw
            spriteBatch.Begin();//SpriteSortMode.Deferred, BlendState.AlphaBlend);
            //spriteBatch.Draw(debugManager.WhiteTexture, rc, new Color(0, 0, 0, 128));
            spriteBatch.DrawString(font, stringBuilder, pos, Color.Yellow);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        #endregion

        private void UpdateFrameRate(GameTime gameTime)
        {
            totalElapsedTime += gameTime.ElapsedGameTime;

            if (totalElapsedTime - lastTime > SampleSpan)
            {
                TimeSpan elapsed = totalElapsedTime - lastTime;
                framesPerSecond = (double) frameCount / elapsed.TotalSeconds;
                frameCount = 0;
                lastTime = totalElapsedTime;

                stringBuilder.Length = 0;
                stringBuilder.Append(framesPerSecond.ToString("0"));
                stringBuilder.Append(" fps");
                if (gameTime.IsRunningSlowly)
                {
                    stringBuilder.Append(" (Slow!)");
                }
                //text += " (frames: " + totalFrames + " update: " + UpdateStopwatch.ElapsedMilliseconds + " draw: " + DrawStopwatch.ElapsedMilliseconds + ")";
                Console.WriteLine(stringBuilder);
            }
        }

        private void IncrementFrameCounter()
        {
            ++frameCount;
            ++totalFrameCount;
        }

        #endregion
    }

}
