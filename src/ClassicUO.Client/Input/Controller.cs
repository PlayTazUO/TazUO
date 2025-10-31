using System.Collections.Generic;
using System.Linq;
using SDL3;

namespace ClassicUO.Input
{
    internal static class Controller
    {
        public static bool Button_A { get; private set; }
        public static bool Button_B { get; private set; }
        public static bool Button_X { get; private set; }
        public static bool Button_Y { get; private set; }

        public static bool Button_Left { get; private set; }
        public static bool Button_Right { get; private set; }
        public static bool Button_Up { get; private set; }
        public static bool Button_Down { get; private set; }

        public static bool Button_LeftTrigger { get; private set; }
        public static bool Button_LeftBumper { get; private set; }

        public static bool Button_RightTrigger { get; private set; }
        public static bool Button_RightBumper { get; private set; }

        public static bool Button_LeftStick { get; private set; }
        public static bool Button_RightStick { get; private set; }

        public static Dictionary<SDL.SDL_GamepadButton, bool> ButtonStates = new();

        public static void OnButtonDown(SDL.SDL_GamepadButtonEvent e) => SetButtonState((SDL.SDL_GamepadButton)e.button, true);

        public static void OnButtonUp(SDL.SDL_GamepadButtonEvent e) => SetButtonState((SDL.SDL_GamepadButton)e.button, false);

        private static void SetButtonState(SDL.SDL_GamepadButton button, bool state)
        {
            ButtonStates[button] = state;

            switch (button)
            {
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH:
                    Button_A = state;
                    break;
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST:
                    Button_B = state;
                    break;
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST:
                    Button_X = state;
                    break;
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH:
                    Button_Y = state;
                    break;

                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT:
                    Button_Left = state;
                    break;
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT:
                    Button_Right = state;
                    break;
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP:
                    Button_Up = state;
                    break;
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN:
                    Button_Down = state;
                    break;

                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER:
                    Button_LeftBumper = state;
                    break;
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER:
                    Button_RightBumper = state;
                    break;

                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK:
                    Button_LeftTrigger = state;
                    break;
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE:
                    Button_RightTrigger = state;
                    break;

                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK:
                    Button_LeftStick = state;
                    break;
                case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK:
                    Button_RightStick = state;
                    break;
            }
        }

        public static bool IsButtonPressed(SDL.SDL_GamepadButton button) => ButtonStates.ContainsKey(button) && ButtonStates[button];

        public static bool AreButtonsPressed(int[] buttons, bool exact = true) => AreButtonsPressed(buttons.Select(x => (SDL.SDL_GamepadButton)x).ToArray(), exact);

        /// <summary>
        /// Check is the supplied list of buttons are currently pressed.
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="exact">If true, any other buttons pressed will make this return false</param>
        /// <returns></returns>
        public static bool AreButtonsPressed(SDL.SDL_GamepadButton[] buttons, bool exact = true)
        {
            bool finalstatus = true;

            foreach (SDL.SDL_GamepadButton button in buttons)
            {
                if (!IsButtonPressed(button))
                {
                    finalstatus = false;
                    break;
                }
            }

            if (exact)
            {
                SDL.SDL_GamepadButton[] allPressed = PressedButtons();

                if (allPressed.Length > buttons.Length)
                {
                    finalstatus = false;
                }
            }

            return finalstatus;
        }

        public static SDL.SDL_GamepadButton[] PressedButtons() => ButtonStates.Where(x => x.Value).Select(x => x.Key).ToArray();

        public static string GetButtonNames(SDL.SDL_GamepadButton[] buttons)
        {
            string keys = string.Empty;

            foreach (SDL.SDL_GamepadButton button in buttons)
            {
                switch (button)
                {
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH:
                        keys += "A";
                        break;
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST:
                        keys += "B";
                        break;
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST:
                        keys += "X";
                        break;
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH:
                        keys += "Y";
                        break;

                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT:
                        keys += "Left";
                        break;
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT:
                        keys += "Right";
                        break;
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP:
                        keys += "Up";
                        break;
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN:
                        keys += "Down";
                        break;


                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER:
                        keys += "LB";
                        break;
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER:
                        keys += "RB";
                        break;


                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK:
                        keys += "LT";
                        break;
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE:
                        keys += "RT";
                        break;

                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK:
                        keys += "LS";
                        break;
                    case SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK:
                        keys += "RS";
                        break;
                }

                keys += ", ";
            }

            if (keys.EndsWith(", "))
            {
                keys = keys.Substring(0, keys.Length - 2);
            }

            return keys;
        }
    }
}
