using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using KCViewer.Core.Types;
using KCViewer.Resource;
using KCViewer.Window.Wpf;

namespace KCViewer.Converters;

public class EquipmentToImageConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not IEquipmentDataMaster equipment) return null;

		try
		{
			string? imageUri = KCResourceHelper.GetEquipmentImagePath(equipment.ID, KCResourceHelper.ResourceTypeEquipmentCard) ??
							   KCResourceHelper.GetEquipmentImagePath(equipment.ID, KCResourceHelper.ResourceTypeEquipmentCardSmall);

			return imageUri switch
			{
				// Fork: in no folder yet: the card is fetched from the official server and shown when it arrives
				// (Fork/KcvViews/KcvGameImages.cs)
				null => Fork.KcvViews.KcvGameImages.EquipmentImage(equipment.ID, KCResourceHelper.ResourceTypeEquipmentCard, 390, 390),
				_ => new BitmapImage(new Uri(imageUri, UriKind.RelativeOrAbsolute).ToAbsolute())
			};
		}
		catch
		{
			return null;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
