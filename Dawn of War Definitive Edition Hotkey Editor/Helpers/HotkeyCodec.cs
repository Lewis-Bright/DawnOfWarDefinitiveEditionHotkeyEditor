using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Input
{
    public static class HotkeyCodec
    {
        public static readonly string[] ModifierOrder = ["Control", "Alt", "Shift"];

        public static readonly ISet<string> ModifierTokens =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Control", "Alt", "Shift" };

        public static bool IsModifierToken(string token) => ModifierTokens.Contains(token);

        public static HashSet<string> GetCurrentMods()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mods = Keyboard.Modifiers;
            if ((mods & ModifierKeys.Control) != 0) set.Add("Control");
            if ((mods & ModifierKeys.Alt) != 0) set.Add("Alt");
            if ((mods & ModifierKeys.Shift) != 0) set.Add("Shift");
            return set;
        }

        public static string Compose(ISet<string> mods, string baseKey)
        {
            var ordered = ModifierOrder.Where(mods.Contains);
            return ordered.Any() ? string.Join("+", ordered.Append(baseKey)) : baseKey;
        }

        public static void Parse(string s, out HashSet<string> mods, out string? baseKey)
        {
            mods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            baseKey = null;
            if (string.IsNullOrWhiteSpace(s)) return;

            var parts = s.Split('+')
                         .Select(p => p.Trim())
                         .Where(p => !string.IsNullOrEmpty(p));

            foreach (var p in parts)
            {
                if (IsModifierToken(p)) mods.Add(p);
                else baseKey = p;
            }
        }

        /// <summary>
        /// Maps a WPF Key to a display token (e.g., Key.F5 => "F5").
        /// Returns false if the key isn't representable.
        /// </summary>
        public static bool TryGetToken(Key key, out string? token)
        {
            token = null;

            if (key == Key.System)
            {
                if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                    key = Keyboard.IsKeyDown(Key.LeftAlt) ? Key.LeftAlt : Key.RightAlt;
                else
                    return false;
            }

            if (key is Key.LeftShift or Key.RightShift) { token = "Shift"; return true; }
            if (key is Key.LeftCtrl or Key.RightCtrl) { token = "Control"; return true; }
            if (key is Key.LeftAlt or Key.RightAlt) { token = "Alt"; return true; }

            if (key >= Key.A && key <= Key.Z) { token = ((char)('A' + (key - Key.A))).ToString(); return true; }
            if (key >= Key.D0 && key <= Key.D9) { token = ((char)('0' + (key - Key.D0))).ToString(); return true; }
            if (key >= Key.NumPad0 && key <= Key.NumPad9) { token = "Numpad" + (key - Key.NumPad0); return true; }
            if (key >= Key.F1 && key <= Key.F24) { token = "F" + (key - Key.F1 + 1); return true; }

            if (Special.TryGetValue(key, out var name)) { token = name; return true; }

            return false;
        }


        private static readonly IReadOnlyDictionary<Key, string> Special =
            new Dictionary<Key, string>
            {
                [Key.Space] = "Space",
                [Key.Tab] = "Tab",
                [Key.Enter] = "Enter",
                [Key.Back] = "Backspace",
                [Key.Escape] = "Escape",
                [Key.Up] = "Up",
                [Key.Down] = "Down",
                [Key.Left] = "Left",
                [Key.Right] = "Right",
                [Key.Home] = "Home",
                [Key.End] = "End",
                [Key.PageUp] = "PageUp",
                [Key.PageDown] = "PageDown",
                [Key.Insert] = "Insert",
                [Key.Delete] = "Delete",
                [Key.CapsLock] = "CapsLock",
                [Key.NumLock] = "NumLock",
                [Key.Scroll] = "ScrollLock",
                [Key.PrintScreen] = "PrintScreen",
                [Key.Pause] = "Pause",
                [Key.OemQuotes] = "Apostrophe",
                [Key.OemComma] = "Comma",
                [Key.OemMinus] = "Minus",
                [Key.OemPeriod] = "Period",
                [Key.OemQuestion] = "Slash",
                [Key.OemSemicolon] = "Semicolon",
                [Key.OemPlus] = "Equal",
                [Key.OemOpenBrackets] = "LBracket",
                [Key.OemBackslash] = "Backslash",
                [Key.OemCloseBrackets] = "RBracket",
                [Key.OemTilde] = "Grave",
                [Key.Multiply] = "NumpadMultiply",
                [Key.Add] = "NumpadPlus",
                [Key.Subtract] = "NumpadMinus",
                [Key.Decimal] = "NumpadPeriod",
                [Key.Divide] = "NumpadSlash",
                [Key.Separator] = "NumpadSeparator",
            };
    }
}
