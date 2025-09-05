using System.Windows;
using System.Windows.Controls;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Dialogs
{
    public sealed class ProfileNameDialog : Window
    {
        private readonly TextBox _input;

        public string? Result { get; private set; }

        public ProfileNameDialog(Window owner, string title, string prompt, string defaultText = "")
        {
            Title = title;
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Width = 420;
            Height = 160;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lbl = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(lbl, 0);

            _input = new TextBox { Text = defaultText };
            Grid.SetRow(_input, 1);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var ok = new Button { Content = "Create", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "Cancel", Width = 80, IsCancel = true };

            ok.Click += (_, __) =>
            {
                Result = _input.Text;
                DialogResult = true;
            };

            panel.Children.Add(ok);
            panel.Children.Add(cancel);
            Grid.SetRow(panel, 2);

            grid.Children.Add(lbl);
            grid.Children.Add(_input);
            grid.Children.Add(panel);

            Content = grid;
        }
    }
}
