using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using KCViewer.Avalonia.ShipGroup;
using KCViewer.Data;
using KCViewer.Utility.Data;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the ship group panel's texts as the viewer's Avalonia view writes them (Converters.cs there).</summary>
public sealed class KcvShipGroupTextConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (parameter as string, value) switch
	{
		// remaining modernization: MAX when there is none left
		("Remain", int remain) => remain == 0 ? "MAX" : remain.ToString(culture),
		// a rate as the viewer's tooltips write it (P2); a binding's StringFormat does not reach an object property like ToolTip
		("Rate", double rate) => rate.ToString("P2", culture),
		("Time", TimeSpan time) => $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}",
		("Speed", int speed) => Constants.GetSpeed(speed),
		("Range", int range) => Constants.GetRange(range),
		_ => value?.ToString(),
	};

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// Fork: the ship group panel's colour rules as categories for the cells' triggers (the colours are set in the XAML),
/// with the viewer's thresholds: fuel, ammo and planes short (Short) or planes all lost (Empty), the repair time under
/// an hour (Short), under six hours (Middle) or longer (Long), none (None).
/// </summary>
public sealed class KcvShipGroupStateConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		Fraction { Max: > 0, Current: 0 } when parameter is "Planes" => "Empty",
		Fraction { Max: > 0, Rate: < 1 } => "Short",
		TimeSpan { TotalMilliseconds: < 1 } => "None",
		TimeSpan { TotalHours: < 1 } => "Short",
		TimeSpan { TotalHours: < 6 } => "Middle",
		TimeSpan => "Long",
		_ => "",
	};

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// Fork: whether a parameter is fully modernized, as the ship list colours it (remaining 0 and a total above 0): the
/// remaining value, then the total.
/// </summary>
public sealed class KcvShipGroupMaxConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values is [int remain, int total, ..] && remain == 0 && total > 0;

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>Fork: the lock column, ♥ for a locked ship and ■ for one locked by its equipment (the viewer's marks).</summary>
public sealed class KcvShipGroupLockConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		ShipGroupItemViewModel { IsLocked: true } => parameter is "Brush" ? KcvShipGroupBrushes.HeartLock : "♥",
		ShipGroupItemViewModel { IsLockedByEquipment: true } => parameter is "Brush" ? KcvShipGroupBrushes.EquipmentLock : "■",
		_ => parameter is "Brush" ? DependencyProperty.UnsetValue : "",
	};

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>Fork: text colours of the ship group panel that read on both themes.</summary>
public static class KcvShipGroupBrushes
{
	public static System.Windows.Media.SolidColorBrush Short { get; } = Frozen(0xC9A43A);
	public static System.Windows.Media.SolidColorBrush Middle { get; } = Frozen(0xFF8C1A);
	public static System.Windows.Media.SolidColorBrush Long { get; } = Frozen(0xE53030);
	public static System.Windows.Media.SolidColorBrush HeartLock { get; } = Frozen(0xCD5C5C);
	public static System.Windows.Media.SolidColorBrush EquipmentLock { get; } = Frozen(0x8C8C90);

	private static System.Windows.Media.SolidColorBrush Frozen(int rgb)
	{
		System.Windows.Media.SolidColorBrush brush = new(KcvPanels.Rgb(rgb));
		brush.Freeze();
		return brush;
	}
}
