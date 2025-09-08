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
            DataContext = _vm;

            var saved = Properties.Settings.Default.ProfilePath;
            if (!string.IsNullOrWhiteSpace(saved) && Directory.Exists(saved))
            {
                try { PresetService.SetProfileDirectory(saved); } catch { /* ignore invalid saved path */ }
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
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Choose your Dawn of War profile folder (e.g. ...\\Relic Entertainment\\Dawn of War\\Profiles\\Profile1)",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
                !string.IsNullOrWhiteSpace(dlg.SelectedPath))
            {
                try
                {
                    PresetService.SetProfileDirectory(dlg.SelectedPath);

                    Properties.Settings.Default.ProfilePath = dlg.SelectedPath;
                    Properties.Settings.Default.Save();

                    _vm.RefreshPresets();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Invalid folder:\n{ex.Message}", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
