using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using KCViewer.Core.Types;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the game's own equipment icons (kcs2/img/common/common_icon_weapon.png with its frame list, read from the
/// game's files by <see cref="ForkGameAssets"/>, not kept in the build) for the KanColleViewer designs. The viewer's
/// icons are 16 px and turn blurry when enlarged; these are drawn at 54 px. The frame number is the icon number of the
/// master data (api_type[3]), the same as <see cref="EquipmentIconType"/>. Some frames are 45 px; like in the game they
/// sit in the middle of a 54 px square, so every icon has the same scale. A number the sprite does not have, and every
/// icon while the sprite is still being fetched on a first start, is the viewer's icon.
/// </summary>
public static class KcvGameIcons
{
	private const string Sprite = @"kcs2\img\common\common_icon_weapon";

	private const double Cell = 54;

	/// <summary>
	/// The part of the 54 px square any icon draws in (measured over all frames: x 6-47, y 5-46). Only this part is
	/// shown, so the icons come out larger at the same size while small and large guns keep their relative size.
	/// </summary>
	private static readonly Rect Visible = new(5, 4.5, 43, 43);

	private static Dictionary<EquipmentIconType, ImageSource?> Icons { get; } = new();

	public static ImageSource? Get(EquipmentIconType type)
	{
		if (type is EquipmentIconType.Nothing) return null;

		return KcvSpriteImages.OnUiThread(() =>
		{
			if (Icons.TryGetValue(type, out ImageSource? icon)) return icon;

			ImageSource? viewer = Application.Current.TryFindResource($"EquipmentIconType.{type}") as ImageSource;

			// the sprite is there but has no such frame: the viewer's icon
			icon = KcvSpriteImages.Image(Sprite, sprite => Draw(sprite, (int)type), Meanwhile(viewer)) ?? viewer;
			Icons[type] = icon;
			return icon;
		});
	}

	private static Drawing? Draw(KcvSprite sprite, int id)
	{
		// common_icon_weapon_id_12
		if (sprite.Crop($"common_icon_weapon_id_{id}") is not { } bitmap) return null;

		// The frame is centred in the 54 px square; every icon is cut to the same visible part, so all have the same
		// bounds and scale.
		DrawingGroup drawing = new() { ClipGeometry = new RectangleGeometry(Visible) };
		drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(Visible)));
		drawing.Children.Add(new ImageDrawing(bitmap,
			new Rect((Cell - bitmap.PixelWidth) / 2, (Cell - bitmap.PixelHeight) / 2, bitmap.PixelWidth, bitmap.PixelHeight)));
		drawing.Freeze();
		return drawing;
	}

	/// <summary>The viewer's icon in the same bounds, while the sprite is being fetched.</summary>
	private static Drawing? Meanwhile(ImageSource? viewer)
	{
		if (viewer is null) return null;

		DrawingGroup drawing = new();
		drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(Visible)));
		drawing.Children.Add(new ImageDrawing(viewer, Visible));
		if (drawing.CanFreeze) drawing.Freeze();
		return drawing;
	}
}

/// <summary>Equipment icon type → the game's icon (<see cref="KcvGameIcons"/>).</summary>
public sealed class KcvGameIconConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
		value is EquipmentIconType type ? KcvGameIcons.Get(type) : null;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
