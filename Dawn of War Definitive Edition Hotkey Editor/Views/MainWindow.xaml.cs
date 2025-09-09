using System.IO;
using System.Linq;
using System.Windows;
using WinForms = System.Windows.Forms;
using System.Windows.Input;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Dialogs;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Models;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Services;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.ViewModels;


namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            AppStorage.Load();
            if (!string.IsNullOrWhiteSpace(AppStorage.ProfilePath) &&
                Directory.Exists(AppStorage.ProfilePath))
            {
                try { PresetService.SetProfileDirectory(AppStorage.ProfilePath); } catch { }
            }

            _vm = new MainViewModel();
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
            var basePreset = _vm.SelectedPreset;
            if (basePreset == null)
            {
                MessageBox.Show(this, "Select a profile to copy from first.", "No base selected",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new ProfileNameDialog(this, "New Profile", "Enter a profile name:", "My Profile");
            if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Result)) return;

            try
            {
                var createdPath = PresetService.CreatePresetFromExisting(basePreset.FullPath, dlg.Result!.Trim());
                _vm.RefreshPresets();
                _vm.SelectedPreset = _vm.Presets.FirstOrDefault(p =>
                    p.FullPath.Equals(createdPath, StringComparison.OrdinalIgnoreCase))
                    ?? _vm.Presets.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to create preset:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectProfileFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var dlg = new WinForms.FolderBrowserDialog
                {
                    Description = "Choose your Dawn of War profile folder (…\\Relic Entertainment\\Dawn of War\\Profiles\\Profile1)",
                    ShowNewFolderButton = false
                };

                var r = dlg.ShowDialog();
                if (r != WinForms.DialogResult.OK) return;

                var path = dlg.SelectedPath;

                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                    throw new DirectoryNotFoundException("Folder not found.");

                var cfg = Path.Combine(path, "playercfg.lua");
                if (!File.Exists(cfg))
                    throw new InvalidOperationException("This doesn't look like a Dawn of War profile (missing playercfg.lua).");

                PresetService.SetProfileDirectory(path);
                AppStorage.ProfilePath = path;
                AppStorage.Save();

                _vm.RefreshPresets();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Can't use that folder:\n{ex.Message}",
                                "Invalid profile folder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
