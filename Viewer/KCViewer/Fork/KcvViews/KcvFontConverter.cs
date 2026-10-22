using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: KanColleViewer's font (Kcv.FontFamily) while its style is on, else the viewer's own font, for a viewer panel
/// that keeps its own layout (the information panel, user decision 2026-10-01). Values: the viewer's font (the
/// window's FontFamily) and <see cref="KcvViewState.Enabled"/>.
/// </summary>
public sealed class KcvFontConverter : IMultiValueConverter
{
	public static KcvFontConverter Instance { get; } = new();

	public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		object? viewerFont = values.Length > 0 ? values[0] : null;
		bool enabled = values.Length > 1 && values[1] is true;

		if (enabled && Application.Current?.TryFindResource("Kcv.FontFamily") is FontFamily kcvFont) return kcvFont;

		return viewerFont is FontFamily ? viewerFont : DependencyProperty.UnsetValue;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
