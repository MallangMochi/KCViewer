using System;
using System.Globalization;
using System.Windows.Data;
using KCViewer.Core.Types.AntiAir;
using KCViewer.Data;
using KCViewer.Window.Wpf;

namespace KCViewer.Converters;

public class AaciDisplayConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		=> value switch
		{
			AntiAirCutIn aaci => aaci.ConditionDisplay(KCDatabase.Instance),
			_ => "???"
		};

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
