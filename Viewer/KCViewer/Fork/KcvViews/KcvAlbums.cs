using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.ViewModels;
using KCViewer.Window.Tools.DialogAlbumMasterEquipment;
using KCViewer.Window.Tools.DialogAlbumMasterShip;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: opens the ship and equipment albums. While the KanColleViewer style is on they open in its style
/// (KcvAlbumShipWindow, KcvAlbumEquipmentWindow, mockup v5 approved 2026-10-05), otherwise the viewer's own windows.
/// </summary>
public static class KcvAlbums
{
	public static void ShowShip(int shipId) => ShowShip(KCDatabase.Instance.MasterShips[shipId]);

	public static void ShowShip(IShipDataMaster? ship)
	{
		if (ship is null) return;

		if (KcvViewState.Current.Enabled)
		{
			new KcvAlbumShipWindow(ship).Show(App.Current!.MainWindow!);
		}
		else
		{
			new DialogAlbumMasterShipWpf(ship).Show(App.Current!.MainWindow!);
		}
	}

	public static void ShowEquipment(int equipmentId)
	{
		if (KcvViewState.Current.Enabled)
		{
			new KcvAlbumEquipmentWindow(equipmentId).Show(App.Current!.MainWindow!);
		}
		else
		{
			new DialogAlbumMasterEquipmentWpf(equipmentId).Show(App.Current!.MainWindow!);
		}
	}
}

/// <summary>
/// The rarity names and colours of the albums (user decisions 2026-10-05, mockup v4/v5). The names are the viewer's
/// translations (Data/ConstantsRes.ko-KR.resx: ships 커먼 (C) … 사쿠라 홀로 (SSH), equipment the game client's
/// SLOTITEM_RARITY_NAMES); the colours are those of the background the game client picks for the value
/// (img/common/ship_bg: ships ≤1 c1, 2 c2, 3 c3, 4 r1, 5 r2, 6 sr1, 7 sr2, ≥8 sr3; equipment 0 item_c1, 1 item_r1,
/// 2–4 sr1, ≥5 sr2). Ships have no rarity 0 (the name is 「-」 and there is no colour).
/// </summary>
public static class KcvRarity
{
	public static string ShipName(int rarity) => Constants.GetShipRarity(Math.Min(rarity, 8));

	public static string EquipmentName(int rarity) => Constants.GetEquipmentRarity(Math.Clamp(rarity, 0, 7));

	public static Brush? ShipBrush(int rarity) => rarity switch
	{
		<= 0 => null,
		1 => Frozen(0x3D6FD8),
		2 => Frozen(0x5A9AE0),
		3 => Frozen(0x5CC4CC),
		4 => Frozen(0xA8A8B0),
		5 => Frozen(0xC8A83A),
		6 => Rainbow,
		7 => ShiningRainbow,
		_ => Frozen(0xF0B0C0),
	};

	public static Brush EquipmentBrush(int rarity) => rarity switch
	{
		<= 0 => Frozen(0xB8B4AE),
		1 => Frozen(0x8AA8B0),
		<= 4 => Rainbow,
		_ => ShiningRainbow,
	};

	private static Brush Rainbow { get; } = Frozen(0xC080E0);
	private static Brush ShiningRainbow { get; } = Frozen(0xE8A0C8);

	private static Brush Frozen(int rgb)
	{
		SolidColorBrush brush = new(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
		brush.Freeze();
		return brush;
	}
}

/// <summary>A rarity → its name, or with the parameter "Brush" its colour; the converter parameter "Ship" or "Equipment" first.</summary>
public sealed class KcvRarityConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not int rarity) return null;

		string[] parts = (parameter as string ?? "").Split(' ');
		bool ship = parts[0] == "Ship";
		bool brush = parts.Length > 1 && parts[1] == "Brush";

		return (ship, brush) switch
		{
			(true, true) => KcvRarity.ShipBrush(rarity),
			(true, false) => KcvRarity.ShipName(rarity),
			(false, true) => KcvRarity.EquipmentBrush(rarity),
			(false, false) => KcvRarity.EquipmentName(rarity),
		};
	}
}

/// <summary>Shown while the value is there (the remodel level and cost of a ship that has a remodel).</summary>
public sealed class KcvNotNullVisibilityConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is null ? Visibility.Collapsed : Visibility.Visible;
}

/// <summary>An improvement level range of the album's upgrade table 「0~5」 (the viewer writes 「0 ～ 5」).</summary>
public sealed class KcvUpgradeRangeConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is string text ? text.Replace(" ～ ", "~") : value;
}
