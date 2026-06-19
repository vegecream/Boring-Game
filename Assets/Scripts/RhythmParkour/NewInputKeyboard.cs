using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RhythmParkour
{
    public static class NewInputKeyboard
    {
        public static bool IsPressed(KeyCode keyCode)
        {
            var key = GetKeyControl(keyCode);
            return key != null && key.isPressed;
        }

        public static bool WasPressedThisFrame(KeyCode keyCode)
        {
            var key = GetKeyControl(keyCode);
            return key != null && key.wasPressedThisFrame;
        }

        static KeyControl GetKeyControl(KeyCode keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return null;

            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
                return keyboard[(Key)((int)Key.A + keyCode - KeyCode.A)];

            if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
                return keyboard[(Key)((int)Key.Numpad0 + keyCode - KeyCode.Keypad0)];

            if (keyCode >= KeyCode.F1 && keyCode <= KeyCode.F12)
                return keyboard[(Key)((int)Key.F1 + keyCode - KeyCode.F1)];

            switch (keyCode)
            {
                case KeyCode.Alpha0:
                    return keyboard.digit0Key;
                case KeyCode.Alpha1:
                    return keyboard.digit1Key;
                case KeyCode.Alpha2:
                    return keyboard.digit2Key;
                case KeyCode.Alpha3:
                    return keyboard.digit3Key;
                case KeyCode.Alpha4:
                    return keyboard.digit4Key;
                case KeyCode.Alpha5:
                    return keyboard.digit5Key;
                case KeyCode.Alpha6:
                    return keyboard.digit6Key;
                case KeyCode.Alpha7:
                    return keyboard.digit7Key;
                case KeyCode.Alpha8:
                    return keyboard.digit8Key;
                case KeyCode.Alpha9:
                    return keyboard.digit9Key;
                case KeyCode.Space:
                    return keyboard.spaceKey;
                case KeyCode.Return:
                    return keyboard.enterKey;
                case KeyCode.KeypadEnter:
                    return keyboard.numpadEnterKey;
                case KeyCode.Escape:
                    return keyboard.escapeKey;
                case KeyCode.Tab:
                    return keyboard.tabKey;
                case KeyCode.Backspace:
                    return keyboard.backspaceKey;
                case KeyCode.Delete:
                    return keyboard.deleteKey;
                case KeyCode.Insert:
                    return keyboard.insertKey;
                case KeyCode.Home:
                    return keyboard.homeKey;
                case KeyCode.End:
                    return keyboard.endKey;
                case KeyCode.PageUp:
                    return keyboard.pageUpKey;
                case KeyCode.PageDown:
                    return keyboard.pageDownKey;
                case KeyCode.UpArrow:
                    return keyboard.upArrowKey;
                case KeyCode.DownArrow:
                    return keyboard.downArrowKey;
                case KeyCode.LeftArrow:
                    return keyboard.leftArrowKey;
                case KeyCode.RightArrow:
                    return keyboard.rightArrowKey;
                case KeyCode.LeftShift:
                    return keyboard.leftShiftKey;
                case KeyCode.RightShift:
                    return keyboard.rightShiftKey;
                case KeyCode.LeftControl:
                    return keyboard.leftCtrlKey;
                case KeyCode.RightControl:
                    return keyboard.rightCtrlKey;
                case KeyCode.LeftAlt:
                    return keyboard.leftAltKey;
                case KeyCode.RightAlt:
                    return keyboard.rightAltKey;
                case KeyCode.Minus:
                    return keyboard.minusKey;
                case KeyCode.Equals:
                    return keyboard.equalsKey;
                case KeyCode.LeftBracket:
                    return keyboard.leftBracketKey;
                case KeyCode.RightBracket:
                    return keyboard.rightBracketKey;
                case KeyCode.Backslash:
                    return keyboard.backslashKey;
                case KeyCode.Semicolon:
                    return keyboard.semicolonKey;
                case KeyCode.Quote:
                    return keyboard.quoteKey;
                case KeyCode.Comma:
                    return keyboard.commaKey;
                case KeyCode.Period:
                    return keyboard.periodKey;
                case KeyCode.Slash:
                    return keyboard.slashKey;
                case KeyCode.BackQuote:
                    return keyboard.backquoteKey;
                default:
                    return null;
            }
        }
    }
}
