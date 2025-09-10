using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Helpers;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Models;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Services;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        private bool _searchMode;
        public bool SearchMode
        {
            get => _searchMode;
            set { _searchMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(LeftColumnWidth)); ApplyFilter(); }
        }

        private string _searchQuery = "";
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                if (ClearSearchCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
            }
        }

        public GridLength LeftColumnWidth => SearchMode ? new GridLength(0) : new GridLength(200);

        public void SubmitSearch()
        {
            SearchMode = !string.IsNullOrWhiteSpace(SearchQuery);
            ApplyFilter();
        }

        public void ClearSearch()
        {
            SearchMode = false;
            SearchQuery = "";
            ApplyFilter();
        }

        public IReadOnlyList<ActionNameIndex> BeautifiedList { get; private set; } = Array.Empty<ActionNameIndex>();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, List<ActionNameIndex>>> DisplayToRawByCategory { get; private set; }
            = new Dictionary<string, IReadOnlyDictionary<string, List<ActionNameIndex>>>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<(string Table, string Action), DisplayInfo> RawToDisplay { get; private set; }
            = new Dictionary<(string, string), DisplayInfo>();

        public ObservableCollection<PresetItem> Presets { get; } = [];
        public ObservableCollection<BindingRow> Rows { get; } = [];
        public ObservableCollection<SectionItem> Sections { get; } =
        [
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
        ];

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

        public ICommand SubmitSearchCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public MainViewModel()
        {
            CreatePresetCommand = new RelayCommand(_ => CreatePreset());
            DeletePresetCommand = new RelayCommand(_ => DeletePreset(), _ => CanDelete);
            EditBindingCommand = new RelayCommand(rowObj => EditBinding(rowObj as BindingRow), _ => !IsBaseFileLoaded);

            RefreshPresets();
            SelectedSection = Sections.FirstOrDefault();
            SubmitSearchCommand = new RelayCommand(_ => SubmitSearch());
            ClearSearchCommand = new RelayCommand(_ => ClearSearch(), _ => !string.IsNullOrWhiteSpace(SearchQuery));
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

            _allRows = rows
                .OrderBy(r => r.DisplayAction, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            ApplyFilter();
            OnPropertyChanged(nameof(IsBaseFileLoaded));
            DeletePresetCommand.RaiseCanExecuteChanged();
            EditBindingCommand.RaiseCanExecuteChanged();

            BeautifiedList = _allRows
                .Select(r => new ActionNameIndex
                {
                    Display = r.DisplayAction,
                    Action = r.Action,
                    Table = r.Table,
                    Category = r.Category,
                    CategoryRaw = r.CategoryRaw
                })
                .OrderBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Display, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            DisplayToRawByCategory = BeautifiedList
                .GroupBy(x => x.CategoryRaw, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyDictionary<string, List<ActionNameIndex>>)g
                            .GroupBy(x => x.Display, StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(
                                gg => gg.Key,
                                gg => gg.ToList(),
                                StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            RawToDisplay = _allRows
                .ToDictionary(
                    r => (r.Table, r.Action),
                    r => new DisplayInfo
                    {
                        Display = r.DisplayAction,
                        Category = r.Category,
                        CategoryRaw = r.CategoryRaw
                    },
                    new TableActionComparer());

            OnPropertyChanged(nameof(BeautifiedList));
            OnPropertyChanged(nameof(DisplayToRawByCategory));
            OnPropertyChanged(nameof(RawToDisplay));
        }

        private void ApplyFilter()
        {
            Rows.Clear();
            if (_allRows == null || _allRows.Length == 0) return;

            var q = _allRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                q = q.Where(r =>
                    r.DisplayAction.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    (r.Binding?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    r.Category.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }
            else if (!SearchMode) // normal mode: filter by selected category
            {
                if (SelectedSection == null || string.IsNullOrEmpty(SelectedSection.Raw)) return;
                q = q.Where(r => r.CategoryRaw == SelectedSection.Raw);
            }

            if (ShowConflictsOnly) q = q.Where(r => r.IsConflict);

            foreach (var r in q) Rows.Add(r);
        }


        private void CreatePreset()
        {
            if (SelectedPreset == null || !File.Exists(SelectedPreset.FullPath))
            {
                MessageBox.Show("Select a base profile first.", "No base selected",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var baseName = Path.GetFileNameWithoutExtension(SelectedPreset.FileName);
            var newName = $"{baseName} Copy";

            var fullPath = PresetService.CreatePresetFromExisting(SelectedPreset.FullPath, newName);

            if(fullPath != null)
            {
                RefreshPresets();
                SelectedPreset = Presets.FirstOrDefault(p => p.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                                 ?? SelectedPreset;
            }
        }


        private void DeletePreset()
        {
            if (SelectedPreset == null) return;
            if (SelectedPreset.IsProtected) return;

            var msg =
                $"Delete “{SelectedPreset.FileName}” permanently?" + Environment.NewLine + Environment.NewLine +
                "Warning: If Steam Cloud is enabled for Dawn of War, Steam may recreate this file when the game starts. " +
                "The file will be cleared but may still exist on the file system after running the game.";

            var res = MessageBox.Show(msg, "Confirm delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

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
            result = Regex.Replace(result, "_+", " ").Trim();
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
