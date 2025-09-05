using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Services
{
    public static class LuaParser
    {
        public static readonly Regex TableRe = new(@"(?<name>\w+)\s*=\s*\{(?<body>.*?)\}",
            RegexOptions.Singleline | RegexOptions.Compiled);
        public static readonly Regex PairRe = new(@"(?<key>\w+)\s*=\s*""(?<val>.*?)""\s*,?",
            RegexOptions.Compiled);

        public static Dictionary<string, Dictionary<string, string>> Parse(string text)
        {
            var tables = new Dictionary<string, Dictionary<string, string>>(System.StringComparer.OrdinalIgnoreCase);
            foreach (Match m in TableRe.Matches(text))
            {
                var name = m.Groups["name"].Value;
                var body = m.Groups["body"].Value;
                var entries = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
                foreach (Match kv in PairRe.Matches(body))
                    entries[kv.Groups["key"].Value] = kv.Groups["val"].Value;
                tables[name] = entries;
            }
            return tables;
        }
    }
}
