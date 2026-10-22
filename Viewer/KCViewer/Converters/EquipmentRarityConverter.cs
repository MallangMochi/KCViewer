using System;
using System.Globalization;
using System.Windows.Data;
using KCViewer.Data;

namespace KCViewer.Converters;

public class EquipmentRarityConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		=> value switch
		{
			int i => Constants.GetEquipmentRarity(i),
			_ => null
		};

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
