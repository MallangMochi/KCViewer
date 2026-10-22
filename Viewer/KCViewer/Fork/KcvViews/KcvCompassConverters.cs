using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Window.Wpf.Compass.ViewModels;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: a compass ship's equipment as game icons: what the battle shows, or for a recorded fleet (no equipment kept)
/// the abyssal ship's default equipment, as the viewer's tooltip does.
/// </summary>
public sealed class KcvCompassEquipmentConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not MasterShipViewModel ship) return null;

		IEnumerable<int>? slots = ship.Slot ?? ship.Ship?.DefaultSlot;

		return (slots ?? [])
			.Where(id => id > 0)
			.Select(id => KCDatabase.Instance.MasterEquipments[id])
			.Where(equipment => equipment is not null)
			.Select(equipment => KcvGameIcons.Get(equipment!.IconTypeTyped))
			.Where(icon => icon is not null)
			.Cast<ImageSource>()
			.ToList();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>Fork: the name of a recorded enemy fleet (translated as the viewer does); "-" for a previewed one without a record.</summary>
public sealed class KcvEnemyFleetNameConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		string? name = (value as EnemyFleetElementViewModel)?.EnemyFleetCandidate?.FleetName;
		return string.IsNullOrEmpty(name) ? "-" : KCDatabase.Instance.Translation.Operation.FleetName(name);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>Fork: the ships of a recorded fleet without its empty places (unknown ships of a preview stay as ???).</summary>
public sealed class KcvCompassMembersConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
		(value as IEnumerable<MasterShipViewModel>)?.Where(ship => ship.ShipId > 0 || ship.IsUnknown).ToList();

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
