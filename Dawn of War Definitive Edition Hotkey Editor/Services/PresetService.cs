using System.IO;
using System.Text;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Models;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Services
{
    public static class PresetService
    {
        public static string GetProfile1Path()
        {
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

        public static string CreatePresetFromKeydefaults(string desiredNameNoExt)
        {
            var dir = GetProfile1Path();
            if (!Directory.Exists(dir)) throw new DirectoryNotFoundException(dir);

            var safe = new string(desiredNameNoExt.Trim()
                .Select(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_').ToArray());

            var path = Path.Combine(dir, safe + ".lua");
            int i = 2;
            while (File.Exists(path)) path = Path.Combine(dir, $"{safe}_{i++}.lua");

            var keydefaults = Directory.EnumerateFiles(dir, "*.lua", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "KEYDEFAULTS.LUA", StringComparison.OrdinalIgnoreCase));

            if (keydefaults != null) File.Copy(keydefaults, path, overwrite: false);
            else
            {
                var content = "bindings = {\n}\n\ncamera_bindings = {\n}";
                File.WriteAllText(path, content, new UTF8Encoding(false));
            }

            LuaWriter.SetBindingsLocstring(path, Path.GetFileNameWithoutExtension(path));
            return path;
        }

        public static void DeletePreset(string fullPath) => File.Delete(fullPath);
    }
}
