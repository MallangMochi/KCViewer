using System;
using System.Globalization;
using System.Windows.Data;
using KCViewer.Data;

namespace KCViewer.Window.Tools.DropRecordViewer;

public class DifficultyToDisplayConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		=> value switch
		{
			int diff => Constants.GetDifficulty(diff),
			_ => value
		};

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
