using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor
{
    public partial class MainWindow : Window
    {
        // ==== View model-ish bits ====
        public class BindingRow
        {
            public string Section { get; set; } = "";
            public string Action { get; set; } = "";
            public string Binding { get; set; } = "";
            public bool IsConflict { get; set; }
        }

        private readonly ObservableCollection<BindingRow> _rows = new();
        private bool _showConflictsOnly = false;

        public MainWindow()
        {
            InitializeComponent();
            Grid.ItemsSource = _rows;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string defaultPath = System.IO.Path.Combine(
                appData,
                "Relic Entertainment",
                "Dawn of War",
                "Profiles",
                "Profile1",
                "keydefaults.lua"
            );

            if (File.Exists(defaultPath))
            {
                LoadFile(defaultPath);
            }
            else
            {
                StatusText.Text = "No default keydefaults.lua found in Profile1";
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Lua files (*.lua)|*.lua|All files (*.*)|*.*",
                Title = "Open keydefaults .lua"
            };
            if (dlg.ShowDialog() == true)
                LoadFile(dlg.FileName);
        }

        private void OpenDefaults_Click(object sender, RoutedEventArgs e)
        {
            var candidates = GuessCandidatePaths();
            if (candidates.Count == 0)
            {
                MessageBox.Show(this,
                    "Couldn't find default key files. Use File → Open… instead.",
                    "No files found",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // simple picker dialog
            var pick = new Window
            {
                Title = "Choose a default key file",
                Owner = this,
                Width = 800,
                Height = 360,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var list = new System.Windows.Controls.ListBox { Margin = new Thickness(10) };
            foreach (var p in candidates) list.Items.Add(p);
            var openBtn = new System.Windows.Controls.Button
            {
                Content = "Open",
                Width = 90,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            openBtn.Click += (_, __) =>
            {
                if (list.SelectedItem is string path)
                {
                    pick.Tag = path;
                    pick.DialogResult = true;
                }
            };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            System.Windows.Controls.Grid.SetRow(list, 0);
            System.Windows.Controls.Grid.SetRow(openBtn, 1);
            grid.Children.Add(list);
            grid.Children.Add(openBtn);
            pick.Content = grid;

            if (pick.ShowDialog() == true && pick.Tag is string chosen)
                LoadFile(chosen);
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void ConflictsOnly_Checked(object sender, RoutedEventArgs e)
        {
            _showConflictsOnly = !_showConflictsOnly; // toggled by menu item
            RefreshGrid();
        }

        // ==== Core loading/display ====
        private void LoadFile(string path)
        {
            try
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                var tables = ParseLuaBindings(text);                 // section -> (action -> binding)
                var conflicts = FindConflicts(tables);                // combo -> [(section, action)]

                // Build a quick lookup for conflicting (section, action)
                var conflictSet = new HashSet<(string section, string action)>(
                    conflicts.SelectMany(kv => kv.Value));

                _rows.Clear();
                foreach (var section in tables.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    foreach (var action in tables[section].Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                    {
                        var binding = tables[section][action];
                        _rows.Add(new BindingRow
                        {
                            Section = section,
                            Action = action,
                            Binding = binding,
                            IsConflict = conflictSet.Contains((section, action))
                        });
                    }
                }

                StatusText.Text = $"Loaded: {path}  |  Conflicts: {conflicts.Count}"; // TextBlock named StatusText
                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to read file:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshGrid()
        {
            if (!_showConflictsOnly)
            {
                Grid.ItemsSource = _rows;
            }
            else
            {
                Grid.ItemsSource = _rows.Where(r => r.IsConflict).ToList();
            }
        }

        // ==== File discovery (Windows) ====
        private static readonly string[] CommonFilenames = new[]
        {
            "keydefaults.lua",
            "keydefaults_grid.lua",
            "keydefaults_grid_azerty.lua",
            "keydefaults_grid_qwertz.lua",
            "keydefaults_modern.lua",
            "KEYDEFAULTS.lua"
        };

        private static List<string> GuessCandidatePaths()
        {
            var paths = new List<string>();

            // Relic AppData (Definitive Edition)
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var relicBase = System.IO.Path.Combine(appdata, "Relic Entertainment", "Dawn of War", "Profiles");
            if (Directory.Exists(relicBase))
            {
                foreach (var prof in Directory.EnumerateDirectories(relicBase, "Profile*"))
                {
                    foreach (var name in CommonFilenames)
                    {
                        var p = System.IO.Path.Combine(prof, name);
                        if (File.Exists(p)) paths.Add(p);
                    }
                }
            }

            // Steam legacy installs (default library)
            var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var steamCommon = System.IO.Path.Combine(pf86, "Steam", "steamapps", "common");
            var guesses = new[]
            {
                System.IO.Path.Combine(steamCommon, "Dawn of War Gold", "Profiles", "Profile1"),
                System.IO.Path.Combine(steamCommon, "Dawn of War Soulstorm", "Profiles", "Profile1"),
            };
            foreach (var g in guesses)
            {
                foreach (var sub in new[] { "w40k", "wxp", "dxp2", string.Empty })
                {
                    foreach (var name in CommonFilenames)
                    {
                        var p = string.IsNullOrEmpty(sub)
                            ? System.IO.Path.Combine(g, name)
                            : System.IO.Path.Combine(g, sub, name);
                        if (File.Exists(p)) paths.Add(p);
                    }
                }
            }

            return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        // ==== Minimal parser + conflict detection ====
        // Parses lua tables like:
        // bindings = { action_name = "KeyCombo", ... }
        // camera_bindings = { ... }
        private static readonly Regex TableRe = new(@"(?<name>\w+)\s*=\s*\{(?<body>.*?)\}",
            RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex PairRe = new(@"(?<key>\w+)\s*=\s*""(?<val>.*?)""\s*,?",
            RegexOptions.Compiled);

        private static Dictionary<string, Dictionary<string, string>> ParseLuaBindings(string text)
        {
            var tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in TableRe.Matches(text))
            {
                var name = m.Groups["name"].Value;
                var body = m.Groups["body"].Value;
                var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match kv in PairRe.Matches(body))
                    entries[kv.Groups["key"].Value] = kv.Groups["val"].Value;
                tables[name] = entries;
            }
            return tables;
        }

        // Normalize/compare combos so "shift+a" == "Shift+A" and mod order is consistent
        private static readonly string[] ModOrder = new[] { "Ctrl", "Alt", "Shift" };
        private static readonly Dictionary<string, string> ModAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["control"] = "Ctrl",
                ["ctrl"] = "Ctrl",
                ["alt"] = "Alt",
                ["shift"] = "Shift",
                ["lctrl"] = "Ctrl",
                ["rctrl"] = "Ctrl",
                ["lalt"] = "Alt",
                ["ralt"] = "Alt",
                ["lshift"] = "Shift",
                ["rshift"] = "Shift",
            };

        private static string NormalizeToken(string tok)
        {
            var t = tok.Trim();
            if (t.Length == 1) return t.ToUpperInvariant();
            if (ModAliases.TryGetValue(t, out var alias)) return alias;

            var low = t.ToLowerInvariant();
            return low switch
            {
                "escape" => "Esc",
                "pagedown" => "PgDn",
                "pageup" => "PgUp",
                "return" => "Enter",
                "spacebar" => "Space",
                _ => (low.StartsWith("f") && int.TryParse(t.AsSpan(1), out _))
                        ? t.ToUpperInvariant()
                        : char.ToUpperInvariant(t[0]) + (t.Length > 1 ? t[1..] : "")
            };
        }

        private static string NormalizeCombo(string combo)
        {
            var tokens = combo.Split('+').Select(NormalizeToken).ToList();
            var mods = tokens.Where(t => ModOrder.Contains(t)).ToList();
            var @base = tokens.Where(t => !ModOrder.Contains(t)).ToList();
            string basePart = @base.Count == 1 ? @base[0] : string.Join('+', @base);
            var orderedMods = ModOrder.Where(m => mods.Contains(m));
            return string.IsNullOrEmpty(basePart)
                ? string.Join('+', tokens)
                : (orderedMods.Any() ? string.Join('+', orderedMods.Append(basePart)) : basePart);
        }

        private static IEnumerable<string> SplitAlternatives(string binding) =>
            binding.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0);

        private static Dictionary<string, List<(string section, string action)>> FindConflicts(
            Dictionary<string, Dictionary<string, string>> tables)
        {
            var used = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (section, entries) in tables)
            {
                foreach (var (action, binding) in entries)
                {
                    foreach (var combo in SplitAlternatives(binding).Select(NormalizeCombo))
                    {
                        if (combo.Length == 0) continue;
                        if (!used.TryGetValue(combo, out var list))
                            used[combo] = list = new();
                        list.Add((section, action));
                    }
                }
            }
            // keep only duplicates
            return used.Where(kv => kv.Value.Count > 1)
                       .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
