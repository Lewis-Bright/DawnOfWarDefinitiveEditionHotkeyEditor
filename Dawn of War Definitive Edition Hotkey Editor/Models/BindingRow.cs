namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Models
{
    public class BindingRow
    {
        public string Table { get; set; } = "";
        public string CategoryRaw { get; set; } = "";
        public string Category { get; set; } = "";
        public string Action { get; set; } = "";
        public string DisplayAction { get; set; } = "";
        public string Binding { get; set; } = "";
        public bool SecondaryAllowed { get; set; } = false;
    }
}
