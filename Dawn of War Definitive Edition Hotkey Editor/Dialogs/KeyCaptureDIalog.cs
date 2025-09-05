using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Dialogs
{
    public sealed class KeyCaptureDialog : Window
    {
        private readonly TextBlock _preview;
        private readonly Button _ok;
        private HashSet<string> _mods = new(System.StringComparer.OrdinalIgnoreCase);
        private string? _base;
        private bool _baseChosen;

        public string? Result { get; private set; }

        private static readonly string[] CaptureModOrder = new[] { "Control", "Alt", "Shift" };

        public KeyCaptureDialog(Window owner, string title, string initial = "")
        {
            Title = title;
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Width = 420;
            Height = 160;
            ResizeMode = ResizeMode.NoResize;

            var root = new Grid { Margin = new Thickness(12), Focusable = true };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lbl = new TextBlock { Text = "Press a key...", Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(lbl, 0);

            _preview = new TextBlock
            {
                Text = string.IsNullOrEmpty(initial) ? "…" : initial,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(_preview, 1);

            var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            _ok = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsEnabled = false, Focusable = false };
            var cancel = new Button { Content = "Cancel", Width = 80, IsCancel = true, Focusable = false };

            _ok.Click += (_, __) =>
            {
                if (_base != null) { Result = Compose(); DialogResult = true; }
                else if (_mods.Count == 1) { Result = _mods.First(); DialogResult = true; }
            };

            panel.Children.Add(_ok);
            panel.Children.Add(cancel);
            Grid.SetRow(panel, 2);

            root.Children.Add(lbl);
            root.Children.Add(_preview);
            root.Children.Add(panel);
            Content = root;

            AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), handledEventsToo: true);
            AddHandler(Keyboard.PreviewKeyUpEvent, new KeyEventHandler(OnPreviewKeyUp), handledEventsToo: true);

            Loaded += (_, __) =>
            {
                root.Focus();
                Keyboard.Focus(root);
            };
        }

        private static HashSet<string> GetCurrentMods()
        {
            var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var mods = Keyboard.Modifiers;
            if ((mods & ModifierKeys.Control) != 0) set.Add("Control");
            if ((mods & ModifierKeys.Alt) != 0) set.Add("Alt");
            if ((mods & ModifierKeys.Shift) != 0) set.Add("Shift");
            return set;
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            var k = e.Key == Key.System ? e.SystemKey : e.Key;

            if (k == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                return;
            }

            _mods = GetCurrentMods();

            var token = KeyToToken(k);
            if (token != null && !IsModifierToken(token))
            {
                _base = token;      // Enter, A, F1, etc.
                _baseChosen = true;
            }

            UpdatePreview();
            e.Handled = true;       // prevents default button behavior on Enter
        }

        private void OnPreviewKeyUp(object? sender, KeyEventArgs e)
        {
            if (_baseChosen)
            {
                e.Handled = true;   // lock base; don't let key-up clear it
                return;
            }

            _mods = GetCurrentMods();
            UpdatePreview();
            e.Handled = true;
        }

        private void UpdatePreview()
        {
            if (_base != null)
            {
                _preview.Text = Compose();
                _ok.IsEnabled = true;
            }
            else
            {
                _preview.Text = _mods.Count == 1 ? _mods.First() : "…";
                _ok.IsEnabled = _mods.Count == 1; // allow pure modifier binding
            }
        }

        private string Compose()
        {
            var ordered = CaptureModOrder.Where(m => _mods.Contains(m));
            return _base == null ? "" : (ordered.Any() ? string.Join("+", ordered.Append(_base)) : _base);
        }

        // Accept "Control" as a modifier token (matches GetCurrentMods and Compose)
        private static bool IsModifierToken(string t) => t is "Control" or "Alt" or "Shift";

        private static string? KeyToToken(Key k)
        {
            if (k >= Key.A && k <= Key.Z) return ((char)('A' + (k - Key.A))).ToString();
            if (k >= Key.D0 && k <= Key.D9) return ((char)('0' + (k - Key.D0))).ToString();
            if (k >= Key.NumPad0 && k <= Key.NumPad9) return "Numpad" + (k - Key.NumPad0);
            if (k >= Key.F1 && k <= Key.F24) return "F" + (k - Key.F1 + 1);

            return k switch
            {
                Key.Space => "Space",
                Key.Tab => "Tab",
                Key.Enter => "Enter",
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
                Key.PrintScreen => "PrintScreen",
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
    }
}
