using System.IO;
using System.Text;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Models;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Services
{
    public static class PresetService
    {
        private static string? _profileDirOverride;
        public static void SetProfileDirectory(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                throw new DirectoryNotFoundException(dir);

            // (optional) sanity check: require playercfg.lua to exist
            var cfg = Path.Combine(dir, "playercfg.lua");
            if (!File.Exists(cfg))
                throw new InvalidOperationException("Selected folder doesn't look like a Dawn of War profile (missing playercfg.lua).");

            _profileDirOverride = dir;
        }
        public static string GetProfile1Path()
        {
            if (!string.IsNullOrEmpty(_profileDirOverride)) return _profileDirOverride;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Relic Entertainment", "Dawn of War", "Profiles", "Profile1");
        }

        public static bool IsProtected(string fileName)
        {
            if (fileName.Equals("KEYDEFAULTS.LUA", StringComparison.OrdinalIgnoreCase)) return true;
            if (fileName.Equals("playercfg.lua", StringComparison.OrdinalIgnoreCase)) return true;
            if (fileName.Equals("KEYDEFAULTS_GRID.LUA", StringComparison.OrdinalIgnoreCase)) return true;
            if (fileName.Equals("KEYDEFAULTS_GRID_AZERTY.LUA", StringComparison.OrdinalIgnoreCase)) return true;
            if (fileName.Equals("KEYDEFAULTS_GRID_QWERTZ.LUA", StringComparison.OrdinalIgnoreCase)) return true;
            if (fileName.Equals("KEYDEFAULTS_MODERN.LUA", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static List<PresetItem> LoadPresets()
        {
            var list = new List<PresetItem>();
            var dir = GetProfile1Path();
            if (!Directory.Exists(dir)) return list;

            var files = Directory.EnumerateFiles(dir, "*.lua", SearchOption.TopDirectoryOnly)
                                 .Where(f => !string.Equals(Path.GetFileName(f), "playercfg.lua", StringComparison.OrdinalIgnoreCase))
                                 .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

            foreach (var f in files)
            {
                if (IsPresetEmpty(f)) continue;
                var file = Path.GetFileName(f);
                var display = Path.GetFileNameWithoutExtension(f);
                bool isBase = file.Equals("KEYDEFAULTS.LUA", StringComparison.OrdinalIgnoreCase);
                bool prot = IsProtected(file);
                if (isBase) display += " (Can't be edited)";
                else if (prot) display += " (Default)";
                list.Add(new PresetItem { Name = display, FullPath = f, FileName = file, IsProtected = prot });
            }
            return list;
        }
        public static string CreatePresetFromExisting(string baseFullPath, string desiredNameNoExt)
        {
            if (string.IsNullOrWhiteSpace(baseFullPath))
                throw new ArgumentException("Base path is required.", nameof(baseFullPath));

            var dir = GetProfile1Path();
            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException(dir);

            if (!File.Exists(baseFullPath))
                throw new FileNotFoundException("Base profile not found.", baseFullPath);

            var safe = new string(desiredNameNoExt.Trim()
                .Select(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_')
                .ToArray());

            var target = Path.Combine(dir, safe + ".lua");

            if (File.Exists(target))
            {
                if (IsProtected(Path.GetFileName(target)))
                {
                    target = GetUniquePath(dir, safe);
                }
                else if (IsPresetEmpty(target))
                {
                    File.SetAttributes(target, FileAttributes.Normal);
                    File.Copy(baseFullPath, target, overwrite: true);
                    LuaWriter.SetBindingsLocstring(target, Path.GetFileNameWithoutExtension(target));
                    return target;
                }
                else
                {
                    target = GetUniquePath(dir, safe);
                }
            }
            else
            {
                File.Copy(baseFullPath, target, overwrite: false);
            }

            LuaWriter.SetBindingsLocstring(target, Path.GetFileNameWithoutExtension(target));
            return target;
        }


        public static void DeletePreset(string fullPath)
        {
            if (!File.Exists(fullPath)) return;

            File.SetAttributes(fullPath, FileAttributes.Normal);

            using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Write, FileShare.None);
            fs.SetLength(0);
            fs.Flush(true);
        }

        private static bool IsPresetEmpty(string path)
        {
            try
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(text)) return true;

                var tables = LuaParser.Parse(text);
                var totalPairs = tables.Sum(kv => kv.Value.Count);
                return totalPairs == 0;
            }
            catch
            {
                return false;
            }
        }

        private static string GetUniquePath(string dir, string baseNameNoExt)
        {
            var i = 2;
            var path = Path.Combine(dir, baseNameNoExt + ".lua");
            while (File.Exists(path)) path = Path.Combine(dir, $"{baseNameNoExt}_{i++}.lua");
            return path;
        }
    }
}
