using System.Text.Json;
using System.IO;

static class AppStorage
{
    private static string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DOW-Hotkey-Editor");
    private static string File = Path.Combine(Dir, "settings.json");

    public static string? ProfilePath { get; set; }

    public static void Load()
    {
        if (!System.IO.File.Exists(File)) return;
        var s = JsonSerializer.Deserialize<Settings>(System.IO.File.ReadAllText(File));
        ProfilePath = s?.ProfilePath;
    }

    public static void Save()
    {
        Directory.CreateDirectory(Dir);
        var s = new Settings { ProfilePath = ProfilePath };
        System.IO.File.WriteAllText(File, JsonSerializer.Serialize(s));
    }

    private class Settings { public string? ProfilePath { get; set; } }
}
