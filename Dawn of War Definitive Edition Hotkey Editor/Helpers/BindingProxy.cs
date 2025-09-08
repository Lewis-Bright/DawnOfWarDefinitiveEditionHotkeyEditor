using System.Windows;

namespace Dawn_of_War_Definitive_Edition_Hotkey_Editor.Helpers
{
	public class BindingProxy : Freezable
	{
		protected override Freezable CreateInstanceCore() => new BindingProxy();

		public object? Data
		{
			get => GetValue(DataProperty);
			set => SetValue(DataProperty, value);
		}

		public static readonly DependencyProperty DataProperty =
			DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy));
	}
}
