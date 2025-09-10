using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Dawn_of_War_Definitive_Edition_Hotkey_Editor.Input;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Models
{
    public class CaptureState
    {
        public HashSet<string> Mods = new(StringComparer.OrdinalIgnoreCase);
        public string? Base;
        public bool BaseChosen;
        public bool ModOnlyHold;
        public string? Committed;

        public void ResetAll()
        {
            Mods.Clear();
            Base = null;
            BaseChosen = false;
            ModOnlyHold = false;
            Committed = null;
        }

        public void SetInitial(string s)
        {
            HotkeyCodec.Parse(s, out Mods, out Base);
            BaseChosen = Base != null;
            Committed = s;
        }

        public void HandleKeyDown(Key k)
        {
            if (HotkeyCodec.TryGetToken(k, out var token) && token is { } t)
            {
                if (HotkeyCodec.IsModifierToken(t))
                {
                    if (!BaseChosen) ModOnlyHold = true;
                }
                else
                {
                    Mods = HotkeyCodec.GetCurrentMods();
                    Base = t;
                    BaseChosen = true;
                    ModOnlyHold = false;
                    Committed = HotkeyCodec.Compose(Mods, Base);
                }
            }
        }

        public void HandleKeyUp()
        {
            Mods = HotkeyCodec.GetCurrentMods();
            if (Mods.Count == 0) ModOnlyHold = false;
        }

        public string? LiveBindingOrNull()
        {
            if (Base != null) return HotkeyCodec.Compose(Mods, Base);
            if (Mods.Count == 1) return Mods.First();
            return null;
        }

        public string PreviewText()
        {
            if (ModOnlyHold && Committed == null) return "…";
            return Committed ?? (LiveBindingOrNull() ?? "…");
        }
    }
}
