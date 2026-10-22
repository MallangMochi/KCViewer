using System;
using System.Globalization;
using System.Windows.Data;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: true when the first value has reached the second (a training plan's current value and its target).</summary>
public sealed class KcvAtLeastConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values.Length >= 2 && values[0] is int current && values[1] is int target && current >= target;

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>Fork: how far a ship's experience is towards the target level, 0 to 1 (its experience, the experience left).</summary>
public sealed class KcvExpProgressConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values.Length >= 2 && values[0] is int exp && values[1] is int left && exp + left > 0
			? (double)exp / (exp + left)
			: 1.0;

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
