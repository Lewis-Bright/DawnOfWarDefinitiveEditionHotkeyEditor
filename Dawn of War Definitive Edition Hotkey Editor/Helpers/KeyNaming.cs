using System.Windows.Input;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Helpers
{
    public static class KeyNaming
    {
        public static readonly string[] ModOrder = { "Control", "Alt", "Shift" };
        public static bool IsModifier(string t) => t is "Control" or "Alt" or "Shift";

        public static HashSet<string> GetCurrentMods()
        {
            var set = new HashSet<string>();
            var mods = Keyboard.Modifiers;
            if ((mods & ModifierKeys.Control) != 0) set.Add("Control");
            if ((mods & ModifierKeys.Alt) != 0) set.Add("Alt");
            if ((mods & ModifierKeys.Shift) != 0) set.Add("Shift");
            return set;
        }

        public static string? KeyToToken(Key k)
        {
            if (k >= Key.A && k <= Key.Z) return ((char)('A' + (k - Key.A))).ToString();
            if (k >= Key.D0 && k <= Key.D9) return ((char)('0' + (k - Key.D0))).ToString();
            if (k >= Key.NumPad0 && k <= Key.NumPad9) return "Numpad" + (k - Key.NumPad0);
            if (k >= Key.F1 && k <= Key.F24) return "F" + (k - Key.F1 + 1);
            return k switch
            {
                Key.Space => "Space",
                Key.Tab => "Tab",
                Key.Enter or Key.Return => "Enter",
                Key.Back => "Backspace",
                Key.Escape => "Escape",
                Key.Up => "Up",
                Key.Down => "Down",
                Key.Left => "Left",
                Key.Right => "Right",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PageUp",
                Key.PageDown => "PageDown",
                Key.Insert => "Insert",
                Key.Delete => "Delete",
                Key.CapsLock => "CapsLock",
                Key.NumLock => "NumLock",
                Key.Scroll => "ScrollLock",
                Key.PrintScreen or Key.Snapshot => "PrintScreen",
                Key.Pause => "Pause",
                Key.OemQuotes => "Apostrophe",
                Key.OemComma => "Comma",
                Key.OemMinus => "Minus",
                Key.OemPeriod => "Period",
                Key.OemQuestion => "Slash",
                Key.OemSemicolon => "Semicolon",
                Key.OemPlus => "Equal",
                Key.OemOpenBrackets => "LBracket",
                Key.OemBackslash => "Backslash",
                Key.OemCloseBrackets => "RBracket",
                Key.OemTilde => "Grave",
                Key.Multiply => "NumpadMultiply",
                Key.Add => "NumpadPlus",
                Key.Subtract => "NumpadMinus",
                Key.Decimal => "NumpadPeriod",
                Key.Divide => "NumpadSlash",
                Key.Separator => "NumpadSeparator",
                _ => null
            };
        }

        public static string Compose(HashSet<string> mods, string? @base)
        {
            if (@base == null) return "";
            var ordered = ModOrder.Where(m => mods.Contains(m));
            return ordered.Any() ? string.Join("+", ordered.Append(@base)) : @base;
        }
    }
}
