using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Helpers
{
    sealed class TableActionComparer : IEqualityComparer<(string Table, string Action)>
    {
        public bool Equals((string Table, string Action) x, (string Table, string Action) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.Table, y.Table) &&
            StringComparer.OrdinalIgnoreCase.Equals(x.Action, y.Action);

        public int GetHashCode((string Table, string Action) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Table ?? string.Empty),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Action ?? string.Empty));
    }
}
