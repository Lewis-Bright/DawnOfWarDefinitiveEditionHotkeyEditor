using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Services
{
    public static class LuaWriter
    {
        public static string EscapeLuaString(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        public static bool TryUpdateBinding(string filePath, string tableName, string actionKey, string newValue)
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);

            foreach (Match m in LuaParser.TableRe.Matches(text))
            {
                if (!string.Equals(m.Groups["name"].Value, tableName, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var bodyIdx = m.Groups["body"].Index;
                var bodyLen = m.Groups["body"].Length;
                var body = text.Substring(bodyIdx, bodyLen);

                var keyPattern = new Regex($@"(\b{Regex.Escape(actionKey)}\s*=\s*)" + "\"(?<val>.*?)\"",
                                           RegexOptions.Multiline);
                if (!keyPattern.IsMatch(body)) return false;

                var replacedBody = keyPattern.Replace(body,
                    mm => mm.Groups[1].Value + "\"" + EscapeLuaString(newValue) + "\"",
                    1);

                var sb = new StringBuilder(text);
                sb.Remove(bodyIdx, bodyLen);
                sb.Insert(bodyIdx, replacedBody);

                File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(false));
                return true;
            }
            return false;
        }

        public static void SetBindingsLocstring(string filePath, string value)
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            var escaped = EscapeLuaString(value);
            var re = new Regex(@"(^|\s)bindings_locstring\s*=\s*""[^""]*""", RegexOptions.Multiline);

            string updated = re.IsMatch(text)
                ? re.Replace(text, m => $"{m.Groups[1].Value}bindings_locstring = \"{escaped}\"", 1)
                : $"bindings_locstring = \"{escaped}\"\n{text}";

            File.WriteAllText(filePath, updated, new UTF8Encoding(false));
        }
    }
}
