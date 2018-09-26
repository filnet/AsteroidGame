using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary.SceneGraph;

namespace GameLibrary.Control
{
    public class InputController : Controller
    {
        private KeyboardState currentKeyboardState;
        private KeyboardState previousKeyboardState;

        private GamePadState currentGamePadState;
        private GamePadState previousGamePadState;

        public KeyboardState KeyboardState
        {
            get { return currentKeyboardState; }
        }

        public GamePadState GamePadState
        {
            get { return currentGamePadState; }
        }

        public InputController()
            : base()
        {
            // Setup the initial input states.
            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = GamePad.GetState(PlayerIndex.One);
        }

        public virtual void Update(GameTime gameTime)
        {
            //base.Update(gameTime);

            previousKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            previousGamePadState = currentGamePadState;
            currentGamePadState = GamePad.GetState(PlayerIndex.One);
         }

        public bool IsKeyDown(Keys key)
        {
            return currentKeyboardState.IsKeyDown(key);
        }

        public bool KeyJustPressed(Keys key)
        {
            return previousKeyboardState.IsKeyUp(key) && currentKeyboardState.IsKeyDown(key);
        }

        public bool IsButtonDown(Buttons buttons)
        {
            return currentGamePadState.IsButtonDown(buttons);
        }

        public bool ButtonJustPressed(Buttons buttons)
        {
            return !previousGamePadState.IsButtonDown(buttons) && currentGamePadState.IsButtonDown(buttons);
        }

        public bool checkExitKey()
        {
            // Check to see whether ESC was pressed on the keyboard 
            // or BACK was pressed on the controller.
            if (IsKeyDown(Keys.Escape) || IsButtonDown(Buttons.Back))
            {
                return true;
            }
            return false;
        }
    }

}
