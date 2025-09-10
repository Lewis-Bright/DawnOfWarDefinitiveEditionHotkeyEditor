using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfColors = System.Windows.SystemColors;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Input;


namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Dialogs
{
    public sealed class KeyCaptureDialog : Window
    {
        private enum ActivePanel { Primary, Secondary }

        private readonly Border _primaryCard;
        private readonly Border _secondaryCard;
        private readonly TextBlock _primaryLabel;
        private readonly TextBlock _secondaryLabel;
        private readonly TextBlock _primaryPreview;
        private readonly TextBlock _secondaryPreview;
        private readonly Button _ok;
        private readonly Button _clear;
        private bool _explicitEmpty;

        private HashSet<string> _modsPrimary = new(System.StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _modsSecondary = new(System.StringComparer.OrdinalIgnoreCase);
        private string? _basePrimary;
        private string? _baseSecondary;
        private bool _baseChosenPrimary;
        private bool _baseChosenSecondary;
        private bool _modOnlyHoldPrimary;
        private bool _modOnlyHoldSecondary;
        private string? _committedPrimary;
        private string? _committedSecondary;
        private ActivePanel _active = ActivePanel.Primary;
        private readonly bool _secondaryAllowedForThisBinding;

        public string? PrimaryResult { get; private set; }
        public string? SecondaryResult { get; private set; }


        private static Border MakeCaptureCard(string title, Thickness margin, out TextBlock label, out TextBlock preview)
        {
            var card = new Border { BorderBrush = WpfColors.ActiveBorderBrush, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(6), Padding = new Thickness(12), Margin = margin, Background = System.Windows.Media.Brushes.Transparent };
            var stack = new StackPanel();
            label = new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) };
            preview = new TextBlock { Text = "…", FontSize = 16, FontWeight = FontWeights.SemiBold };
            stack.Children.Add(label);
            stack.Children.Add(preview);
            card.Child = stack;
            return card;
        }
        private static bool IsModifierKey(Key k) =>
             k is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt;


        public KeyCaptureDialog(Window owner, string title, string initial, bool secondaryAllowed)
        {
            Title = title;
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Width = 640;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;

            _secondaryAllowedForThisBinding = secondaryAllowed;

            var root = new Grid { Margin = new Thickness(12), Focusable = true };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lbl = new TextBlock { Text = "Press keys to set bindings", Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(lbl, 0);

            var cards = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            cards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(cards, 1);

            _primaryCard = MakeCaptureCard("Primary", new Thickness(0, 0, 8, 0), out _primaryLabel, out _primaryPreview);
            _secondaryCard = MakeCaptureCard("Secondary", new Thickness(8, 0, 0, 0), out _secondaryLabel, out _secondaryPreview);

            cards.Children.Add(_primaryCard);
            cards.Children.Add(_secondaryCard);
            Grid.SetColumn(_primaryCard, 0);
            Grid.SetColumn(_secondaryCard, 1);
            ApplySecondaryVisibility(cards);

            var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            _clear = new Button { Content = "Clear", Width = 100, Margin = new Thickness(0, 0, 8, 0), IsEnabled = true, Focusable = false };
            _ok = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsEnabled = false, Focusable = false };
            var cancel = new Button { Content = "Cancel", Width = 80, IsCancel = true, Focusable = false };

            panel.Children.Add(_clear);
            panel.Children.Add(_ok);
            panel.Children.Add(cancel);
            Grid.SetRow(panel, 2);

            root.Children.Add(lbl);
            root.Children.Add(cards);
            root.Children.Add(panel);
            Content = root;

            AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), handledEventsToo: true);
            AddHandler(Keyboard.PreviewKeyUpEvent, new KeyEventHandler(OnPreviewKeyUp), handledEventsToo: true);

            _primaryCard.MouseLeftButtonDown += (_, __) => { _active = ActivePanel.Primary; UpdateActiveVisuals(); };
            _secondaryCard.MouseLeftButtonDown += (_, __) =>
            {
                if (_secondaryAllowedForThisBinding && SecondaryEditable())
                {
                    _active = ActivePanel.Secondary;
                    UpdateActiveVisuals();
                }
            };
            _clear.Click += (_, __) =>
            {
                if (_active == ActivePanel.Primary)
                {
                    _modsPrimary.Clear(); _modsSecondary.Clear();
                    _basePrimary = null; _baseSecondary = null;
                    _baseChosenPrimary = false; _baseChosenSecondary = false;
                    _committedPrimary = null; _committedSecondary = null;
                    PrimaryResult = ""; SecondaryResult = null;
                    _explicitEmpty = true;
                    UpdateAll();
                }
                else
                {
                    _modsSecondary.Clear();
                    _baseSecondary = null;
                    _baseChosenSecondary = false;
                    _committedSecondary = null;
                    SecondaryResult = null;
                    _explicitEmpty = false;
                    UpdateAll();
                }
            };


            _ok.Click += (_, __) =>
            {
                if (_explicitEmpty)
                {
                    PrimaryResult = "";
                    SecondaryResult = null;
                    DialogResult = true;
                    return;
                }

                var p = _committedPrimary ?? CurrentBindingOrNullPrimary();
                var s = _committedSecondary ?? CurrentBindingOrNullSecondary();
                if (p != null) PrimaryResult = p;
                if (_secondaryAllowedForThisBinding && s != null) SecondaryResult = s;
                DialogResult = true;
            };



            Loaded += (_, __) =>
            {
                root.Focus();
                Keyboard.Focus(root);

                if (!string.IsNullOrWhiteSpace(initial))
                {
                    var parts = initial.Split(',')
                                       .Select(x => x.Trim())
                                       .Where(x => !string.IsNullOrEmpty(x))
                                       .ToArray();

                    if (parts.Length > 0) SetInitialPrimary(parts[0]);
                    if (_secondaryAllowedForThisBinding && parts.Length > 1) SetInitialSecondary(parts[1]);
                }

                UpdateAll();
            };

        }

        private void ApplySecondaryVisibility(Grid cardsGrid)
        {
            if (_secondaryAllowedForThisBinding)
            {
                if (cardsGrid.ColumnDefinitions.Count == 1)
                {
                    cardsGrid.ColumnDefinitions.Clear();
                    cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(_primaryCard, 0);
                    Grid.SetColumn(_secondaryCard, 1);
                }
                _secondaryCard.Visibility = Visibility.Visible;
            }
            else
            {
                _secondaryCard.Visibility = Visibility.Collapsed;
                if (cardsGrid.ColumnDefinitions.Count != 1)
                {
                    cardsGrid.ColumnDefinitions.Clear();
                    cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(_primaryCard, 0);
                }
                _primaryLabel.Text = "Key Binding";
            }
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            _explicitEmpty = false;
            var k = e.Key == Key.System ? e.SystemKey : e.Key;

            if (k == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                return;
            }

            if (_active == ActivePanel.Primary)
            {
                if (IsModifierKey(k))
                {
                    _modOnlyHoldPrimary = true;
                }
                else
                {
                    _modsPrimary = HotkeyCodec.GetCurrentMods();
                    if (HotkeyCodec.TryGetToken(k, out var token) && token is { } t)
                    {
                        _basePrimary = t;
                        _baseChosenPrimary = true;
                        _modOnlyHoldPrimary = false;
                        _committedPrimary = HotkeyCodec.Compose(_modsPrimary, _basePrimary);
                    }
                }
            }
            else
            {
                if (!SecondaryEditable()) { e.Handled = true; return; }

                if (IsModifierKey(k))
                {
                    _modOnlyHoldSecondary = true;
                }
                else
                {
                    _modsSecondary = HotkeyCodec.GetCurrentMods();
                    if (HotkeyCodec.TryGetToken(k, out var token) && token is { } t)
                    {
                        _baseSecondary = t;
                        _baseChosenSecondary = true;
                        _modOnlyHoldSecondary = false;
                        _committedSecondary = HotkeyCodec.Compose(_modsSecondary, _baseSecondary);
                    }
                }
            }

            UpdateAll();
            e.Handled = true;
        }

        private void OnPreviewKeyUp(object? sender, KeyEventArgs e)
        {
            if (_active == ActivePanel.Primary)
            {
                _modsPrimary = HotkeyCodec.GetCurrentMods();
                if (_modsPrimary.Count == 0) _modOnlyHoldPrimary = false;
            }
            else
            {
                if (!SecondaryEditable()) { e.Handled = true; return; }
                _modsSecondary = HotkeyCodec.GetCurrentMods();
                if (_modsSecondary.Count == 0) _modOnlyHoldSecondary = false;
            }

            UpdateAll();
            e.Handled = true;
        }

        private void UpdateAll()
        {
            var pLive = CurrentBindingOrNullPrimary();
            var sLive = CurrentBindingOrNullSecondary();

            if (_explicitEmpty)
            {
                _primaryPreview.Text = "…";
                if (_secondaryAllowedForThisBinding) _secondaryPreview.Text = "…";
                _ok.IsEnabled = true;
                if (_secondaryAllowedForThisBinding) _secondaryCard.Opacity = 0.5;
                UpdateActiveVisuals();
                return;
            }

            _primaryPreview.Text = (_modOnlyHoldPrimary && _committedPrimary != null) ? "…" : (_committedPrimary ?? (pLive ?? "…"));

            if (_secondaryAllowedForThisBinding)
                _secondaryPreview.Text = (_modOnlyHoldSecondary && _committedSecondary != null) ? "…" : (_committedSecondary ?? (sLive ?? "…"));

            var primaryReady = (_committedPrimary != null) || (!_modOnlyHoldPrimary && pLive != null);
            _ok.IsEnabled = primaryReady || PrimaryResult == "";

            if (_secondaryAllowedForThisBinding)
                _secondaryCard.Opacity = SecondaryEditable() ? 1.0 : 0.5;

            UpdateActiveVisuals();
        }

        private void UpdateActiveVisuals()
        {
            _primaryCard.BorderBrush = _active == ActivePanel.Primary ? WpfColors.HighlightBrush : WpfColors.ActiveBorderBrush;
            if (_secondaryAllowedForThisBinding)
            {
                _secondaryCard.BorderBrush = _active == ActivePanel.Secondary ? WpfColors.HighlightBrush : WpfColors.ActiveBorderBrush;
                _secondaryLabel.Foreground = _active == ActivePanel.Secondary ? WpfColors.HighlightBrush : WpfColors.ControlTextBrush;
            }

            _primaryLabel.Foreground = _active == ActivePanel.Primary ? WpfColors.HighlightBrush : WpfColors.ControlTextBrush;

            _clear.IsEnabled = _active == ActivePanel.Primary
                || (_secondaryAllowedForThisBinding && (CurrentBindingOrNullSecondary() != null || !string.IsNullOrEmpty(SecondaryResult)));
        }


        private string? CurrentBindingOrNullPrimary()
        {
            if (_basePrimary != null) return HotkeyCodec.Compose(_modsPrimary, _basePrimary);
            if (_modsPrimary.Count == 1) return _modsPrimary.First();
            return null;
        }

        private string? CurrentBindingOrNullSecondary()
        {
            if (!SecondaryEditable() && string.IsNullOrEmpty(SecondaryResult)) return null;
            if (_baseSecondary != null) return HotkeyCodec.Compose(_modsSecondary, _baseSecondary);
            if (_modsSecondary.Count == 1) return _modsSecondary.First();
            return null;
        }

        private bool SecondaryEditable()
        {
            if (!_secondaryAllowedForThisBinding) return false;
            return CurrentBindingOrNullPrimary() != null || !string.IsNullOrEmpty(SecondaryResult);
        }

        private void SetInitialPrimary(string s)
        {
            HotkeyCodec.Parse(s, out _modsPrimary, out _basePrimary);
            _baseChosenPrimary = _basePrimary != null;
            _committedPrimary = s;
        }

        private void SetInitialSecondary(string s)
        {
            HotkeyCodec.Parse(s, out _modsSecondary, out _baseSecondary);
            _baseChosenSecondary = _baseSecondary != null;
            _committedSecondary = s;
            SecondaryResult = s;
        }


        public string? Result =>
            (PrimaryResult is null && SecondaryResult is null)
                ? null
                : (SecondaryResult is null ? (PrimaryResult ?? "") : $"{PrimaryResult ?? ""}, {SecondaryResult}");

    }
}
