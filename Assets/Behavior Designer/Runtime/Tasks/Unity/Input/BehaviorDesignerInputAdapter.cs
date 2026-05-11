using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace BehaviorDesigner.Runtime.Tasks.Unity.UnityInput
{
    internal static class BehaviorDesignerInputAdapter
    {
        public static Vector3 Acceleration
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.acceleration;
#elif ENABLE_INPUT_SYSTEM
                return Accelerometer.current != null ? Accelerometer.current.acceleration.ReadValue() : Vector3.zero;
#else
                return Vector3.zero;
#endif
            }
        }

        public static Vector3 MousePosition
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    Vector2 position = Mouse.current.position.ReadValue();
                    return new Vector3(position.x, position.y, 0f);
                }

                if (Touchscreen.current != null)
                {
                    Vector2 position = Touchscreen.current.primaryTouch.position.ReadValue();
                    return new Vector3(position.x, position.y, 0f);
                }

                return Vector3.zero;
#else
                return Vector3.zero;
#endif
            }
        }

        public static float GetAxis(string axisName, bool raw)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return raw ? Input.GetAxisRaw(axisName) : Input.GetAxis(axisName);
#elif ENABLE_INPUT_SYSTEM
            if (string.IsNullOrWhiteSpace(axisName))
            {
                return 0f;
            }

            switch (axisName.Trim().ToLowerInvariant())
            {
                case "horizontal":
                    return NormalizeAxis(ReadHorizontalAxis(), raw);
                case "vertical":
                    return NormalizeAxis(ReadVerticalAxis(), raw);
                case "mouse x":
                    return Mouse.current != null ? Mouse.current.delta.x.ReadValue() : 0f;
                case "mouse y":
                    return Mouse.current != null ? Mouse.current.delta.y.ReadValue() : 0f;
                case "mouse scrollwheel":
                    return Mouse.current != null ? Mouse.current.scroll.y.ReadValue() / 120f : 0f;
                default:
                    return 0f;
            }
#else
            return 0f;
#endif
        }

        public static bool GetButton(string buttonName)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetButton(buttonName);
#elif ENABLE_INPUT_SYSTEM
            return ReadButton(buttonName, ButtonReadMode.IsPressed);
#else
            return false;
#endif
        }

        public static bool GetButtonDown(string buttonName)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetButtonDown(buttonName);
#elif ENABLE_INPUT_SYSTEM
            return ReadButton(buttonName, ButtonReadMode.WasPressedThisFrame);
#else
            return false;
#endif
        }

        public static bool GetButtonUp(string buttonName)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetButtonUp(buttonName);
#elif ENABLE_INPUT_SYSTEM
            return ReadButton(buttonName, ButtonReadMode.WasReleasedThisFrame);
#else
            return false;
#endif
        }

        public static bool GetKey(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#elif ENABLE_INPUT_SYSTEM
            ButtonControl control = ResolveKeyControl(key);
            return control != null && control.isPressed;
#else
            return false;
#endif
        }

        public static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#elif ENABLE_INPUT_SYSTEM
            ButtonControl control = ResolveKeyControl(key);
            return control != null && control.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool GetKeyUp(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyUp(key);
#elif ENABLE_INPUT_SYSTEM
            ButtonControl control = ResolveKeyControl(key);
            return control != null && control.wasReleasedThisFrame;
#else
            return false;
#endif
        }

        public static bool GetMouseButton(int buttonIndex)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(buttonIndex);
#elif ENABLE_INPUT_SYSTEM
            ButtonControl control = ResolveMouseButton(buttonIndex);
            return control != null && control.isPressed;
#else
            return false;
#endif
        }

        public static bool GetMouseButtonDown(int buttonIndex)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(buttonIndex);
#elif ENABLE_INPUT_SYSTEM
            ButtonControl control = ResolveMouseButton(buttonIndex);
            return control != null && control.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static bool GetMouseButtonUp(int buttonIndex)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonUp(buttonIndex);
#elif ENABLE_INPUT_SYSTEM
            ButtonControl control = ResolveMouseButton(buttonIndex);
            return control != null && control.wasReleasedThisFrame;
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private enum ButtonReadMode
        {
            IsPressed,
            WasPressedThisFrame,
            WasReleasedThisFrame
        }

        private static float ReadHorizontalAxis()
        {
            float keyboardValue = 0f;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyboardValue += IsPressed(keyboard.dKey) || IsPressed(keyboard.rightArrowKey) ? 1f : 0f;
                keyboardValue -= IsPressed(keyboard.aKey) || IsPressed(keyboard.leftArrowKey) ? 1f : 0f;
            }

            float gamepadValue = 0f;
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                gamepadValue = gamepad.leftStick.x.ReadValue();
                if (Mathf.Approximately(gamepadValue, 0f))
                {
                    gamepadValue += IsPressed(gamepad.dpad.right) ? 1f : 0f;
                    gamepadValue -= IsPressed(gamepad.dpad.left) ? 1f : 0f;
                }
            }

            return Mathf.Clamp(keyboardValue + gamepadValue, -1f, 1f);
        }

        private static float ReadVerticalAxis()
        {
            float keyboardValue = 0f;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                keyboardValue += IsPressed(keyboard.wKey) || IsPressed(keyboard.upArrowKey) ? 1f : 0f;
                keyboardValue -= IsPressed(keyboard.sKey) || IsPressed(keyboard.downArrowKey) ? 1f : 0f;
            }

            float gamepadValue = 0f;
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                gamepadValue = gamepad.leftStick.y.ReadValue();
                if (Mathf.Approximately(gamepadValue, 0f))
                {
                    gamepadValue += IsPressed(gamepad.dpad.up) ? 1f : 0f;
                    gamepadValue -= IsPressed(gamepad.dpad.down) ? 1f : 0f;
                }
            }

            return Mathf.Clamp(keyboardValue + gamepadValue, -1f, 1f);
        }

        private static float NormalizeAxis(float value, bool raw)
        {
            if (!raw)
            {
                return value;
            }

            if (value > 0f)
            {
                return 1f;
            }

            return value < 0f ? -1f : 0f;
        }

        private static bool ReadButton(string buttonName, ButtonReadMode mode)
        {
            foreach (ButtonControl control in EnumerateButtonControls(buttonName))
            {
                if (ReadButtonControl(control, mode))
                {
                    return true;
                }
            }

            return false;
        }

        private static System.Collections.Generic.IEnumerable<ButtonControl> EnumerateButtonControls(string buttonName)
        {
            if (string.IsNullOrWhiteSpace(buttonName))
            {
                yield break;
            }

            string normalized = buttonName.Trim().ToLowerInvariant();
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;

            switch (normalized)
            {
                case "fire1":
                    if (mouse != null) yield return mouse.leftButton;
                    if (gamepad != null) yield return gamepad.rightTrigger;
                    break;
                case "fire2":
                    if (mouse != null) yield return mouse.rightButton;
                    if (gamepad != null) yield return gamepad.leftTrigger;
                    break;
                case "fire3":
                    if (mouse != null) yield return mouse.middleButton;
                    if (gamepad != null) yield return gamepad.rightShoulder;
                    break;
                case "jump":
                    if (keyboard != null) yield return keyboard.spaceKey;
                    if (gamepad != null) yield return gamepad.buttonSouth;
                    break;
                case "submit":
                    if (keyboard != null) yield return keyboard.enterKey;
                    if (keyboard != null) yield return keyboard.numpadEnterKey;
                    if (gamepad != null) yield return gamepad.buttonSouth;
                    break;
                case "cancel":
                    if (keyboard != null) yield return keyboard.escapeKey;
                    if (gamepad != null) yield return gamepad.buttonEast;
                    break;
            }
        }

        private static bool ReadButtonControl(ButtonControl control, ButtonReadMode mode)
        {
            if (control == null)
            {
                return false;
            }

            switch (mode)
            {
                case ButtonReadMode.WasPressedThisFrame:
                    return control.wasPressedThisFrame;
                case ButtonReadMode.WasReleasedThisFrame:
                    return control.wasReleasedThisFrame;
                default:
                    return control.isPressed;
            }
        }

        private static ButtonControl ResolveMouseButton(int buttonIndex)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return null;
            }

            switch (buttonIndex)
            {
                case 0:
                    return mouse.leftButton;
                case 1:
                    return mouse.rightButton;
                case 2:
                    return mouse.middleButton;
                case 3:
                    return mouse.forwardButton;
                case 4:
                    return mouse.backButton;
                default:
                    return null;
            }
        }

        private static ButtonControl ResolveKeyControl(KeyCode key)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || key == KeyCode.None)
            {
                return null;
            }

            switch (key)
            {
                case KeyCode.Backspace: return keyboard.backspaceKey;
                case KeyCode.Tab: return keyboard.tabKey;
                case KeyCode.Return: return keyboard.enterKey;
                case KeyCode.Escape: return keyboard.escapeKey;
                case KeyCode.Space: return keyboard.spaceKey;
                case KeyCode.LeftShift: return keyboard.leftShiftKey;
                case KeyCode.RightShift: return keyboard.rightShiftKey;
                case KeyCode.LeftControl: return keyboard.leftCtrlKey;
                case KeyCode.RightControl: return keyboard.rightCtrlKey;
                case KeyCode.LeftAlt: return keyboard.leftAltKey;
                case KeyCode.RightAlt: return keyboard.rightAltKey;
                case KeyCode.UpArrow: return keyboard.upArrowKey;
                case KeyCode.DownArrow: return keyboard.downArrowKey;
                case KeyCode.LeftArrow: return keyboard.leftArrowKey;
                case KeyCode.RightArrow: return keyboard.rightArrowKey;
                case KeyCode.Insert: return keyboard.insertKey;
                case KeyCode.Delete: return keyboard.deleteKey;
                case KeyCode.Home: return keyboard.homeKey;
                case KeyCode.End: return keyboard.endKey;
                case KeyCode.PageUp: return keyboard.pageUpKey;
                case KeyCode.PageDown: return keyboard.pageDownKey;
                case KeyCode.CapsLock: return keyboard.capsLockKey;
                case KeyCode.Numlock: return keyboard.numLockKey;
                case KeyCode.ScrollLock: return keyboard.scrollLockKey;
                case KeyCode.Print: return keyboard.printScreenKey;
                case KeyCode.Pause: return keyboard.pauseKey;
                case KeyCode.Menu: return keyboard.contextMenuKey;
                case KeyCode.KeypadEnter: return keyboard.numpadEnterKey;
                case KeyCode.KeypadPlus: return keyboard.numpadPlusKey;
                case KeyCode.KeypadMinus: return keyboard.numpadMinusKey;
                case KeyCode.KeypadMultiply: return keyboard.numpadMultiplyKey;
                case KeyCode.KeypadDivide: return keyboard.numpadDivideKey;
                case KeyCode.KeypadPeriod: return keyboard.numpadPeriodKey;
                case KeyCode.Alpha0: return keyboard.digit0Key;
                case KeyCode.Alpha1: return keyboard.digit1Key;
                case KeyCode.Alpha2: return keyboard.digit2Key;
                case KeyCode.Alpha3: return keyboard.digit3Key;
                case KeyCode.Alpha4: return keyboard.digit4Key;
                case KeyCode.Alpha5: return keyboard.digit5Key;
                case KeyCode.Alpha6: return keyboard.digit6Key;
                case KeyCode.Alpha7: return keyboard.digit7Key;
                case KeyCode.Alpha8: return keyboard.digit8Key;
                case KeyCode.Alpha9: return keyboard.digit9Key;
                case KeyCode.Keypad0: return keyboard.numpad0Key;
                case KeyCode.Keypad1: return keyboard.numpad1Key;
                case KeyCode.Keypad2: return keyboard.numpad2Key;
                case KeyCode.Keypad3: return keyboard.numpad3Key;
                case KeyCode.Keypad4: return keyboard.numpad4Key;
                case KeyCode.Keypad5: return keyboard.numpad5Key;
                case KeyCode.Keypad6: return keyboard.numpad6Key;
                case KeyCode.Keypad7: return keyboard.numpad7Key;
                case KeyCode.Keypad8: return keyboard.numpad8Key;
                case KeyCode.Keypad9: return keyboard.numpad9Key;
                case KeyCode.A: return keyboard.aKey;
                case KeyCode.B: return keyboard.bKey;
                case KeyCode.C: return keyboard.cKey;
                case KeyCode.D: return keyboard.dKey;
                case KeyCode.E: return keyboard.eKey;
                case KeyCode.F: return keyboard.fKey;
                case KeyCode.G: return keyboard.gKey;
                case KeyCode.H: return keyboard.hKey;
                case KeyCode.I: return keyboard.iKey;
                case KeyCode.J: return keyboard.jKey;
                case KeyCode.K: return keyboard.kKey;
                case KeyCode.L: return keyboard.lKey;
                case KeyCode.M: return keyboard.mKey;
                case KeyCode.N: return keyboard.nKey;
                case KeyCode.O: return keyboard.oKey;
                case KeyCode.P: return keyboard.pKey;
                case KeyCode.Q: return keyboard.qKey;
                case KeyCode.R: return keyboard.rKey;
                case KeyCode.S: return keyboard.sKey;
                case KeyCode.T: return keyboard.tKey;
                case KeyCode.U: return keyboard.uKey;
                case KeyCode.V: return keyboard.vKey;
                case KeyCode.W: return keyboard.wKey;
                case KeyCode.X: return keyboard.xKey;
                case KeyCode.Y: return keyboard.yKey;
                case KeyCode.Z: return keyboard.zKey;
                case KeyCode.F1: return keyboard.f1Key;
                case KeyCode.F2: return keyboard.f2Key;
                case KeyCode.F3: return keyboard.f3Key;
                case KeyCode.F4: return keyboard.f4Key;
                case KeyCode.F5: return keyboard.f5Key;
                case KeyCode.F6: return keyboard.f6Key;
                case KeyCode.F7: return keyboard.f7Key;
                case KeyCode.F8: return keyboard.f8Key;
                case KeyCode.F9: return keyboard.f9Key;
                case KeyCode.F10: return keyboard.f10Key;
                case KeyCode.F11: return keyboard.f11Key;
                case KeyCode.F12: return keyboard.f12Key;
                default:
                    return null;
            }
        }

        private static bool IsPressed(ButtonControl control)
        {
            return control != null && control.isPressed;
        }
#endif
    }
}
