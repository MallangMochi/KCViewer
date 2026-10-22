using System;
using System.Globalization;
using System.Windows.Data;
using KCViewer.Core.Types;
using KCViewer.Data;

namespace KCViewer.Converters;

public class ShipClassDisplayConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		=> value switch
		{
			ShipClass.Unknown => "*",
			ShipClass id => KCDatabase.Instance.Translation.Ship.GetShipClass(id),
			_ => throw new ArgumentOutOfRangeException()
		};

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
