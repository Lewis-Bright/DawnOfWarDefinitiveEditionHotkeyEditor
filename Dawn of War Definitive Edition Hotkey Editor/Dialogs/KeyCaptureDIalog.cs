using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfColors = System.Windows.SystemColors;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Input;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Models;

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
        private ActivePanel _active = ActivePanel.Primary;
        private readonly bool _secondaryAllowedForThisBinding;

        private readonly CaptureState _primary = new();
        private readonly CaptureState _secondary = new();

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

        public KeyCaptureDialog(Window owner, string title, string initial, bool secondaryAllowed)
        {
            Title = title;
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            MinWidth = 320;
            Width = 320;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.CanResize;

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
                    _primary.ResetAll();
                    _secondary.ResetAll();
                    PrimaryResult = "";
                    SecondaryResult = null;
                    _explicitEmpty = true;
                }
                else
                {
                    _secondary.ResetAll();
                    SecondaryResult = null;
                    _explicitEmpty = false;
                }
                UpdateAll();
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

                var p = _primary.Committed ?? _primary.LiveBindingOrNull();
                var s = _secondary.Committed ?? _secondary.LiveBindingOrNull();
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

                    if (parts.Length > 0) _primary.SetInitial(parts[0]);
                    if (_secondaryAllowedForThisBinding && parts.Length > 1) _secondary.SetInitial(parts[1]);
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
                _primary.HandleKeyDown(k);
            }
            else
            {
                if (!SecondaryEditable()) { e.Handled = true; return; }
                _secondary.HandleKeyDown(k);
            }

            UpdateAll();
            e.Handled = true;
        }

        private void OnPreviewKeyUp(object? sender, KeyEventArgs e)
        {
            if (_active == ActivePanel.Primary)
            {
                _primary.HandleKeyUp();
            }
            else
            {
                if (!SecondaryEditable()) { e.Handled = true; return; }
                _secondary.HandleKeyUp();
            }

            UpdateAll();
            e.Handled = true;
        }

        private void UpdateAll()
        {
            if (_explicitEmpty)
            {
                _primaryPreview.Text = "…";
                if (_secondaryAllowedForThisBinding) _secondaryPreview.Text = "…";
                _ok.IsEnabled = true;
                if (_secondaryAllowedForThisBinding) _secondaryCard.Opacity = 0.5;
                UpdateActiveVisuals();
                return;
            }

            _primaryPreview.Text = _primary.PreviewText();
            if (_secondaryAllowedForThisBinding)
                _secondaryPreview.Text = _secondary.PreviewText();

            var pLive = _primary.LiveBindingOrNull();
            var primaryReady = (_primary.Committed != null) || (!_primary.ModOnlyHold && pLive != null);
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
                || (_secondaryAllowedForThisBinding && (_secondary.LiveBindingOrNull() != null || !string.IsNullOrEmpty(SecondaryResult)));
        }

        private bool SecondaryEditable()
        {
            if (!_secondaryAllowedForThisBinding) return false;
            return _primary.LiveBindingOrNull() != null || _primary.Committed != null || !string.IsNullOrEmpty(SecondaryResult);
        }

        public string? Result =>
            (PrimaryResult is null && SecondaryResult is null)
                ? null
                : (SecondaryResult is null ? (PrimaryResult ?? "") : $"{PrimaryResult ?? ""}, {SecondaryResult}");
    }
}
