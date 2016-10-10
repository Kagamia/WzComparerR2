using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Reflection;

namespace WzComparerR2.MapRender
{
    public class InputState
    {
        public InputState()
        {

        }

        public InputState(Game game)
        {
            _gameWindow = game.Window;
        }

        private GameWindow _gameWindow;

        private KeyboardState CurrentKeyboardState;
        private KeyboardState LastKeyboardState;

        private MouseState CurrentMouseState;
        private MouseState LastMouseState;

        /// <summary>
        /// 更新当前输入设备的信息，这个方法应该在游戏执行Update时调用。
        /// </summary>
        public void Update(GameTime gameTime)
        {
            LastKeyboardState = CurrentKeyboardState;
            CurrentKeyboardState = Keyboard.GetState();

            LastMouseState = CurrentMouseState;
            CurrentMouseState = _gameWindow != null ? Mouse.GetState(_gameWindow) : Mouse.GetState();
        }

        /// <summary>
        /// 检查最后一次更新前，指定的key是否被按下。
        /// </summary>
        /// <param name="key">要检查的key。</param>
        /// <returns></returns>
        public bool IsKeyDown(Keys key)
        {
            return CurrentKeyboardState.IsKeyDown(key) && LastKeyboardState.IsKeyUp(key);
        }

        /// <summary>
        /// 检查最后一次更新前，指定的key是否弹起。
        /// </summary>
        /// <param name="key">要检查的key。</param>
        /// <returns></returns>
        public bool IsKeyUp(Keys key)
        {
            return CurrentKeyboardState.IsKeyUp(key) && LastKeyboardState.IsKeyDown(key);
        }

        /// <summary>
        /// 检查当前指定的key是否正在被按下的状态。
        /// </summary>
        /// <param name="key">要检查的key。</param>
        /// <returns></returns>
        public bool IsKeyPressing(Keys key)
        {
            return CurrentKeyboardState.IsKeyDown(key);
        }

        public bool IsCtrlPressing
        {
            get
            {
                return this.IsKeyPressing(Keys.LeftControl) || this.IsKeyPressing(Keys.RightControl);
            }
        }

        public bool IsAltPressing
        {
            get
            {
                return this.IsKeyPressing(Keys.LeftAlt) || this.IsKeyPressing(Keys.RightAlt);
            }
        }

        public bool IsShiftPressing
        {
            get
            {
                return this.IsKeyPressing(Keys.LeftShift) || this.IsKeyPressing(Keys.RightShift);
            }
        }

        /// <summary>
        /// 获取或设置当前的鼠标指针位置。
        /// </summary>
        public Point MousePosition
        {
            get { return new Point(CurrentMouseState.X, CurrentMouseState.Y); }
            set { Mouse.SetPosition(value.X, value.Y); CurrentMouseState = Mouse.GetState(); }
        }

        public Point MousePositionLast
        {
            get { return new Point(LastMouseState.X, LastMouseState.Y); }
        }

        /// <summary>
        /// 检查最后一次更新前，指定的鼠标按键是否被按下。
        /// </summary>
        /// <param name="button">要检查的鼠标按键的组合。</param>
        /// <returns></returns>
        public bool IsMouseButtonDown(MouseButton button)
        {
            MouseButton[] baseButtons = (MouseButton[])Enum.GetValues(typeof(MouseButton));
            bool isBtnDown = false;
            foreach (MouseButton baseBtn in baseButtons)
            {
                if ((int)(button & baseBtn) != 0)
                    isBtnDown |= IsSingleMouseButtonDown(baseBtn);
                if (isBtnDown)
                    break;
            }
            return isBtnDown;
        }

        private bool IsSingleMouseButtonDown(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.LeftButton:
                    return CurrentMouseState.LeftButton == ButtonState.Pressed &&
                        LastMouseState.LeftButton == ButtonState.Released;
                case MouseButton.MiddleButton:
                    return CurrentMouseState.MiddleButton == ButtonState.Pressed &&
                        LastMouseState.MiddleButton == ButtonState.Released;
                case MouseButton.RightButton:
                    return CurrentMouseState.RightButton == ButtonState.Pressed &&
                        LastMouseState.RightButton == ButtonState.Released;
                case MouseButton.XButton1:
                    return CurrentMouseState.XButton1 == ButtonState.Pressed &&
                        LastMouseState.XButton1 == ButtonState.Released;
                case MouseButton.XButton2:
                    return CurrentMouseState.XButton2 == ButtonState.Pressed &&
                        LastMouseState.XButton2 == ButtonState.Released;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 检查最后一次更新前，指定的鼠标按键是否弹起.
        /// </summary>
        /// <param name="button">要检查的鼠标按键的组合。</param>
        /// <returns></returns>
        public bool IsMouseButtonUp(MouseButton button)
        {
            MouseButton[] baseButtons = (MouseButton[])Enum.GetValues(typeof(MouseButton));
            bool isBtnUp = false;
            foreach (MouseButton baseBtn in baseButtons)
            {
                if ((int)(button & baseBtn) != 0)
                    isBtnUp |= IsSingleMouseButtonUp(baseBtn);
                if (isBtnUp)
                    break;
            }
            return isBtnUp;
        }

        private bool IsSingleMouseButtonUp(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.LeftButton:
                    return CurrentMouseState.LeftButton == ButtonState.Released &&
                        LastMouseState.LeftButton == ButtonState.Pressed;
                case MouseButton.MiddleButton:
                    return CurrentMouseState.MiddleButton == ButtonState.Released &&
                        LastMouseState.MiddleButton == ButtonState.Pressed;
                case MouseButton.RightButton:
                    return CurrentMouseState.RightButton == ButtonState.Released &&
                        LastMouseState.RightButton == ButtonState.Pressed;
                case MouseButton.XButton1:
                    return CurrentMouseState.XButton1 == ButtonState.Released &&
                        LastMouseState.XButton1 == ButtonState.Pressed;
                case MouseButton.XButton2:
                    return CurrentMouseState.XButton2 == ButtonState.Released &&
                        LastMouseState.XButton2 == ButtonState.Pressed;
                default:
                    return false;
            }
        }

        public bool IsMouseButtonPressing(MouseButton button)
        {
            MouseButton[] baseButtons = (MouseButton[])Enum.GetValues(typeof(MouseButton));
            bool isBtnPressing = false;
            foreach (MouseButton baseBtn in baseButtons)
            {
                if ((int)(button & baseBtn) != 0)
                    isBtnPressing |= IsSingleMouseButtonPressing(baseBtn);
                if (isBtnPressing)
                    break;
            }
            return isBtnPressing;
        }

        private bool IsSingleMouseButtonPressing(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.LeftButton:
                    return CurrentMouseState.LeftButton == ButtonState.Pressed;
                case MouseButton.MiddleButton:
                    return CurrentMouseState.MiddleButton == ButtonState.Pressed;
                case MouseButton.RightButton:
                    return CurrentMouseState.RightButton == ButtonState.Pressed;
                case MouseButton.XButton1:
                    return CurrentMouseState.XButton1 == ButtonState.Pressed;
                case MouseButton.XButton2:
                    return CurrentMouseState.XButton2 == ButtonState.Pressed;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 获取最后一次更新前，鼠标滚轮滚动变化值。
        /// </summary>
        /// <returns></returns>
        public int GetMouseWheelScrolledValue()
        {
            return CurrentMouseState.ScrollWheelValue - LastMouseState.ScrollWheelValue;
        }
    }
}
