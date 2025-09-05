using System.Windows;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.ViewModels;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Models;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Services;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Dialogs;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private void EditBinding_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not BindingRow row) return;
            if (_vm.IsBaseFileLoaded) return;

            var dlg = new KeyCaptureDialog(this, $"Set binding for {row.DisplayAction}", row.Binding);
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Result))
            {
                if (LuaWriter.TryUpdateBinding(_vm.SelectedPreset!.FullPath, row.Table, row.Action, dlg.Result))
                    _vm.ReloadCurrentPreset();
            }
        }

        private void CreatePreset_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ProfileNameDialog(this, "New Preset", "Enter a file name (without extension):", "My Profile");
            if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Result))
                return;

            var desiredName = dlg.Result!;
            try
            {
                var createdPath = PresetService.CreatePresetFromKeydefaults(desiredName);

                _vm.RefreshPresets();
                _vm.SelectedPreset = _vm.Presets.FirstOrDefault(p => p.FullPath == createdPath)
                                     ?? _vm.Presets.FirstOrDefault();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this, $"Failed to create preset:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
