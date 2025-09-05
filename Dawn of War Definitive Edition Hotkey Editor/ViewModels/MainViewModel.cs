using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Models;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Services;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Helpers;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PresetItem> Presets { get; } = new();
        public ObservableCollection<BindingRow> Rows { get; } = new();
        public ObservableCollection<SectionItem> Sections { get; } = new()
        {
            new() { Raw="marine", Display="Space Marine" },
            new() { Raw="chaos", Display="Chaos" },
            new() { Raw="tau", Display="Tau" },
            new() { Raw="ork", Display="Ork" },
            new() { Raw="guard", Display="Imperial Guard" },
            new() { Raw="eldar", Display="Eldar" },
            new() { Raw="necron", Display="Necron" },
            new() { Raw="dark_eldar", Display="Dark Eldar" },
            new() { Raw="sisters", Display="Sisters of Battle" },
            new() { Raw="other", Display="Other" },
        };

        private PresetItem? _selectedPreset;
        public PresetItem? SelectedPreset
        {
            get => _selectedPreset;
            set { _selectedPreset = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanDelete)); OnPropertyChanged(nameof(IsBaseFileLoaded)); LoadSelectedPreset(); }
        }

        private SectionItem? _selectedSection;
        public SectionItem? SelectedSection
        {
            get => _selectedSection;
            set { _selectedSection = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private bool _showConflictsOnly;
        public bool ShowConflictsOnly
        {
            get => _showConflictsOnly;
            set { _showConflictsOnly = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public bool IsBaseFileLoaded => _selectedPreset?.FileName.Equals("KEYDEFAULTS.LUA", System.StringComparison.OrdinalIgnoreCase) == true;
        public bool CanDelete => _selectedPreset != null && !_selectedPreset.IsProtected;

        private string? _currentFilePath;
        private BindingRow[] _allRows = [];

        public RelayCommand CreatePresetCommand { get; }
        public RelayCommand DeletePresetCommand { get; }
        public RelayCommand EditBindingCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            CreatePresetCommand = new RelayCommand(_ => CreatePreset());
            DeletePresetCommand = new RelayCommand(_ => DeletePreset(), _ => CanDelete);
            EditBindingCommand = new RelayCommand(rowObj => EditBinding(rowObj as BindingRow), _ => !IsBaseFileLoaded);

            RefreshPresets();
        }

        public void RefreshPresets()
        {
            var keepPath = SelectedPreset?.FullPath;

            Presets.Clear();
            foreach (var p in PresetService.LoadPresets()) Presets.Add(p);

            if (keepPath != null)
                SelectedPreset = Presets.FirstOrDefault(p => p.FullPath.Equals(keepPath, StringComparison.OrdinalIgnoreCase))
                                 ?? Presets.FirstOrDefault();
            else
                SelectedPreset = Presets.FirstOrDefault();
        }


        private void LoadSelectedPreset()
        {
            Rows.Clear();
            _allRows = [];
            if (SelectedPreset == null) return;

            _currentFilePath = SelectedPreset.FullPath;
            var text = File.ReadAllText(_currentFilePath!, Encoding.UTF8);
            var tables = LuaParser.Parse(text);

            var used = new Dictionary<string, List<(string section, string action)>>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var (section, entries) in tables)
                foreach (var (action, binding) in entries)
                {
                    foreach (var combo in binding.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
                    {
                        if (!used.TryGetValue(combo, out var list)) used[combo] = list = new();
                        list.Add((section, action));
                    }
                }
            var conflicts = used.Where(kv => kv.Value.Count > 1)
                                .ToDictionary(kv => kv.Key, kv => kv.Value, System.StringComparer.OrdinalIgnoreCase);
            var conflictSet = new HashSet<(string section, string action)>(conflicts.SelectMany(kv => kv.Value));

            var rows = new List<BindingRow>();
            foreach (var section in tables.Keys.OrderBy(k => k, System.StringComparer.OrdinalIgnoreCase))
                foreach (var action in tables[section].Keys.OrderBy(k => k, System.StringComparer.OrdinalIgnoreCase))
                {
                    var binding = tables[section][action];
                    var catRaw = ComputeCategoryRaw(action);
                    rows.Add(new BindingRow
                    {
                        Table = section,
                        CategoryRaw = catRaw,
                        Category = DisplayForRaw(catRaw),
                        Action = action,
                        DisplayAction = BeautifyAction(action, catRaw),
                        Binding = binding,
                        IsConflict = conflictSet.Contains((section, action))
                    });
                }

            _allRows = rows.ToArray();
            ApplyFilter();
            OnPropertyChanged(nameof(IsBaseFileLoaded));
            DeletePresetCommand.RaiseCanExecuteChanged();
            EditBindingCommand.RaiseCanExecuteChanged();
        }

        private void ApplyFilter()
        {
            var q = _allRows.AsEnumerable();
            if (ShowConflictsOnly) q = q.Where(r => r.IsConflict);
            if (SelectedSection != null && !string.IsNullOrEmpty(SelectedSection.Raw))
                q = q.Where(r => r.CategoryRaw == SelectedSection.Raw);

            Rows.Clear();
            foreach (var r in q) Rows.Add(r);
        }

        private void CreatePreset()
        {
            var newName = "My Profile";
            var fullPath = PresetService.CreatePresetFromKeydefaults(newName);
            RefreshPresets();
            SelectedPreset = Presets.FirstOrDefault(p => p.FullPath == fullPath) ?? Presets.FirstOrDefault();
        }

        private void DeletePreset()
        {
            if (SelectedPreset == null) return;
            if (SelectedPreset.IsProtected) return;

            var res = MessageBox.Show($"Delete “{SelectedPreset.FileName}” permanently?",
                "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (res != MessageBoxResult.Yes) return;

            PresetService.DeletePreset(SelectedPreset.FullPath);
            RefreshPresets();
        }

        private void EditBinding(BindingRow? row)
        {
            if (row == null || _currentFilePath == null) return;
            if (IsBaseFileLoaded) return;
        }

        private static readonly List<SectionItem> SectionDefs = new()
        {
            new() { Raw="marine", Display="Space Marine" }, new() { Raw="chaos", Display="Chaos" },
            new() { Raw="tau", Display="Tau" }, new() { Raw="ork", Display="Ork" },
            new() { Raw="guard", Display="Imperial Guard" }, new() { Raw="eldar", Display="Eldar" },
            new() { Raw="necron", Display="Necron" }, new() { Raw="dark_eldar", Display="Dark Eldar" },
            new() { Raw="sisters", Display="Sisters of Battle" }, new() { Raw="other", Display="Other" }
        };

        private static readonly Dictionary<string, string> Overrides = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["cannibalism"] = "tau",
            ["melee_dance"] = "eldar",
            ["webway_gate_healing_research"] = "eldar",
            ["relocate"] = "eldar",
            ["earthshaker_round"] = "guard",
            ["entrench"] = "guard",
            ["possess_enemy"] = "necron",
            ["direct_spawn"] = "necron",
            ["lightning_field"] = "necron",
            ["harvest"] = "necron",
            ["possess"] = "chaos"
        };

        private static string ComputeCategoryRaw(string action)
        {
            if (Overrides.TryGetValue(action, out var forced)) return forced;
            foreach (var s in SectionDefs)
                if (s.Raw != "other" &&
                    (action.StartsWith(s.Raw + "_", System.StringComparison.OrdinalIgnoreCase) ||
                     action.StartsWith("addon_" + s.Raw + "_", System.StringComparison.OrdinalIgnoreCase)))
                    return s.Raw;
            return "other";
        }
        private static string DisplayForRaw(string raw) =>
            SectionDefs.FirstOrDefault(x => string.Equals(x.Raw, raw, System.StringComparison.OrdinalIgnoreCase))?.Display ?? raw;

        private static string BeautifyAction(string rawAction, string catRaw)
        {
            var result = rawAction;
            if (result.StartsWith(catRaw + "_", System.StringComparison.OrdinalIgnoreCase))
                result = result[(catRaw.Length + 1)..];
            else if (result.StartsWith("addon_" + catRaw + "_", System.StringComparison.OrdinalIgnoreCase))
                result = result[("addon_" + catRaw + "_").Length..];
            result = result.Replace('_', ' ');
            return result.Length == 0 ? result : char.ToUpper(result[0]) + (result.Length > 1 ? result[1..].ToLower() : "");
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void ReloadCurrentPreset()
        {
            if (SelectedPreset != null)
            {
                var current = SelectedPreset;
                var tmp = current; 
                LoadSelectedPreset();       
                SelectedPreset = tmp; 
            }
        }

    }
}
