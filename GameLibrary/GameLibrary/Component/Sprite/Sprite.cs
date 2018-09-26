using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameLibrary
{
    public class Sprite : DrawableGameComponent
    {
        private Game game;
        private SpriteBatch spriteBatch;

        private Texture2D texture;
        private Vector2 position;
        private float speed = 4.0F;

        public Sprite(Game game, String name)
            : base(game)
        {
            this.game = game;
            position = new Vector2(0, 0);
            texture = game.Content.Load<Texture2D>(name);
            spriteBatch = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));
        }

        protected override void Dispose(bool disposing)
        {
            texture.Dispose();
            base.Dispose(disposing);
        }

        public override void Update(GameTime gameTime)
        {
            // Get the current gamepad state.
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            // Get the current keyboard state.
            KeyboardState keyboardState = Keyboard.GetState();

            updateGamePad(gamePadState);
            updateKeyboard(keyboardState);

            //MouseState MState = Mouse.GetState();
            //Vector2 MouseVector = new Vector2(MState.X, MState.Y);
            //Vector2 IvanToMouseVector = -(_ivanPosition - MouseVector);
            //IvanToMouseVector.Normalize();
            //_ivanPosition += IvanToMouseVector;
            base.Update(gameTime);
        }

        private void updateKeyboard(KeyboardState keyboardState)
        {
            if (keyboardState.IsKeyDown(Keys.Up))
            {
                position.Y -= speed;
            }
            if (keyboardState.IsKeyDown(Keys.Down))
            {
                position.Y += speed;
            }
            if (keyboardState.IsKeyDown(Keys.Left))
            {
                position.X -= speed;
            }
            if (keyboardState.IsKeyDown(Keys.Right))
            {
                position.X += speed;
            }
        }

        private void updateGamePad(GamePadState gamePadState)
        {
            if (gamePadState.IsConnected)
            {
                GamePadDPad dpad = gamePadState.DPad;
                if (dpad.Up == ButtonState.Pressed)
                {
                    position.Y -= speed;
                }
                if (dpad.Down == ButtonState.Pressed)
                {
                    position.Y += speed;
                }
                if (dpad.Left == ButtonState.Pressed)
                {
                    position.X -= speed;
                }
                if (dpad.Right == ButtonState.Pressed)
                {
                    position.X += speed;
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Draw(texture, position, Color.White);
            base.Draw(gameTime);
        }
    }
}