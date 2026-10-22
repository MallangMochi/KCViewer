using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;
using KCViewer.Core.Types.Mocks;
using KCViewer.Data;
using KancolleProgress.Translations;
using KancolleProgress.ViewModels;
using Microsoft.Win32;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: which ships the admiral has, by line (a ship and its remodels count as its base ship), shared by 칸무스 수집
/// 진행도 and the battle panel's dropped ship (mockup 20261006-battle-drop-v2).
/// </summary>
public static class KcvShipCollection
{
	public static int BaseShipId(IShipDataMaster ship) => ship.BaseShip().ShipID;

	/// <summary>No ship of its line is owned, nor among <paramref name="alsoOwned"/> (base ship ids, e.g. dropped since the port).</summary>
	public static bool IsMissing(IShipDataMaster ship, IReadOnlySet<int> alsoOwned)
	{
		int baseShip = BaseShipId(ship);

		return !alsoOwned.Contains(baseShip) && !KCDatabase.Instance.Ships.Values.Any(s => BaseShipId(s.MasterShip) == baseShip);
	}
}

/// <summary>A level's colour in 칸무스 수집 진행도: the viewer's colour filters, the first that matches.</summary>
public enum KcvProgressTier
{
	Max,
	Lv99,
	Lv90,
	Lv80,
	Owned,
	Missing,
}

/// <summary>One ship of the window: its name and the highest level owned (0 when missing, shown without a level).</summary>
public sealed record KcvProgressShip(string Name, int Level)
{
	public KcvProgressTier Tier => KcvKancolleProgressViewModel.TierOf(Level);
	public string LevelText => Level > 0 ? Level.ToString(CultureInfo.InvariantCulture) : "";
}

/// <summary>The ships of one class, a box in the window.</summary>
public sealed record KcvProgressClass(IReadOnlyList<KcvProgressShip> Ships);

/// <summary>A group of ship types (a column): its name, 「owned/all」 and its class boxes.</summary>
public sealed record KcvProgressGroup(string Label, IReadOnlyList<KcvProgressClass> Classes)
{
	public string Count
	{
		get
		{
			List<KcvProgressShip> ships = Classes.SelectMany(c => c.Ships).ToList();
			return $"{ships.Count(s => s.Level > 0)}/{ships.Count}";
		}
	}
}

/// <summary>A chip of the box 「컬러 필터」: the colour, its name and how many ships (base ships) are in it.</summary>
public sealed record KcvProgressFilter(KcvProgressTier Tier, string Label, int Count);

/// <summary>
/// Fork: 칸무스 수집 진행도 in KanColleViewer's style (mockup 20261006-kancolle-progress-v3, approved 2026-10-06 "구현"),
/// computed as the viewer's KancolleProgressViewModel does, with what the mockup changed: a ship type the viewer does
/// not know goes to the group 「그외」 at the end (the viewer's ShipTypeExtensions.ToGroup throws, and opening its window
/// then closes the viewer), the event view's single sonar group stays empty when no sonar is owned (the viewer takes
/// the maximum of no sonar), and each group counts its owned ships. The ships are read once, when the window opens
/// (answer 3 of the second draft), as the viewer's window does.
/// </summary>
public sealed partial class KcvKancolleProgressViewModel : ObservableObject
{
	private IReadOnlyList<IShipDataMaster> AllShips { get; }
	private IReadOnlyList<IShipData> UserShips { get; }
	private IReadOnlyList<IEquipmentData> UserEquipment { get; }

	/// <summary>Every base ship (no remodel before it) with the highest level owned of its line, 0 when none.</summary>
	private IReadOnlyList<ShipDataMock> BaseShips { get; }

	/// <summary>The tab 「이벤트」 (the destroyers for event maps) instead of 「칸무스」.</summary>
	public bool IsEventView { get; set; }

	public bool IsShipsView
	{
		get => !IsEventView;
		set => IsEventView = !value;
	}

	public IReadOnlyList<KcvProgressGroup> Groups { get; private set; }

	public IReadOnlyList<KcvProgressFilter> Filters { get; }

	/// <summary>「보유 16 / 332종 (4.8%)」</summary>
	public string OwnedSummary { get; }

	public KcvKancolleProgressViewModel()
	{
		AllShips = KCDatabase.Instance.MasterShips.Values.Cast<IShipDataMaster>().ToList();
		UserShips = KCDatabase.Instance.Ships.Values.Cast<IShipData>().ToList();
		UserEquipment = KCDatabase.Instance.Equipments.Values.Cast<IEquipmentData>().ToList();

		BaseShips = MakeBaseShips();
		Filters =
		[
			new(KcvProgressTier.Max, KancolleProgressResources.Max, BaseShips.Count(s => s.Level == MaxLevel)),
			new(KcvProgressTier.Lv99, "99+", BaseShips.Count(s => s.Level >= 99)),
			new(KcvProgressTier.Lv90, "90+", BaseShips.Count(s => s.Level >= 90)),
			new(KcvProgressTier.Lv80, "80+", BaseShips.Count(s => s.Level >= 80)),
			new(KcvProgressTier.Owned, KancolleProgressResources.Collection, BaseShips.Count(s => s.Level >= 1)),
			new(KcvProgressTier.Missing, KancolleProgressResources.Missing, BaseShips.Count(s => s.Level == 0)),
		];
		OwnedSummary = ForkStrings.KcvProgressOwned(BaseShips.Count(s => s.Level >= 1), BaseShips.Count);
		Groups = MakeShipGroups();

		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is not nameof(IsEventView)) return;

			Groups = IsEventView ? MakeEventGroups() : MakeShipGroups();
		};
	}

	/// <summary>The viewer's 칸스토 (its colour filter's level).</summary>
	private const int MaxLevel = 188;

	public static KcvProgressTier TierOf(int level) => level switch
	{
		MaxLevel => KcvProgressTier.Max,
		>= 99 => KcvProgressTier.Lv99,
		>= 90 => KcvProgressTier.Lv90,
		>= 80 => KcvProgressTier.Lv80,
		>= 1 => KcvProgressTier.Owned,
		_ => KcvProgressTier.Missing,
	};

	private List<ShipDataMock> MakeBaseShips()
	{
		Dictionary<int, ShipDataMock> baseShips = AllShips
			.Where(s => !s.IsAbyssalShip && s.RemodelBeforeShipID == 0)
			.Select(s => new ShipDataMock(s) { Level = 0 })
			.ToDictionary(s => s.ShipID);

		foreach (IShipData ship in UserShips)
		{
			if (baseShips.TryGetValue(KcvShipCollection.BaseShipId(ship.MasterShip), out ShipDataMock? baseShip) && baseShip.Level < ship.Level)
			{
				baseShip.Level = ship.Level;
			}
		}

		return [.. baseShips.Values];
	}

	/// <summary>The viewer's group of a ship type, null for one it does not know (「그외」).</summary>
	private static ShipTypeGroup? GroupOf(ShipTypes type)
	{
		try
		{
			return type.ToGroup();
		}
		catch (NotImplementedException)
		{
			return null;
		}
	}

	private List<KcvProgressGroup> MakeShipGroups() => BaseShips
		.OrderBy(s => s.SortID)
		.GroupBy(s => GroupOf(s.MasterShip.ShipType))
		.OrderBy(g => g.Key is null)
		.ThenBy(g => g.Key)
		.Select(g => Group(g.Key?.Display() ?? ForkStrings.KcvProgressOthers, g))
		.ToList();

	private static KcvProgressGroup Group(string label, IEnumerable<IShipData> ships) => new(label, ships
		.GroupBy(s => s.MasterShip.ShipClass)
		.Select(c => new KcvProgressClass(c.Select(s => new KcvProgressShip(s.Name, s.Level)).ToList()))
		.ToList());

	private static DaihatsuGroup DaihatsuGroupOf(IShipData ship)
	{
		bool daihatsu = ship.MasterShip.EquippableCategoriesTyped.Contains(EquipmentTypes.LandingCraft);
		bool tank = ship.MasterShip.EquippableCategoriesTyped.Contains(EquipmentTypes.SpecialAmphibiousTank);

		return (daihatsu, tank) switch
		{
			(true, true) => DaihatsuGroup.DaihatsuAndTank,
			(true, false) => DaihatsuGroup.Daihatsu,
			(false, true) => DaihatsuGroup.Tank,
			_ => DaihatsuGroup.None,
		};
	}

	private static FcfGroup FcfGroupOf(IShipData ship) =>
		ship.MasterShip.EquippableCategoriesTyped.Contains(EquipmentTypes.CommandFacility) ? FcfGroup.Fcf : FcfGroup.None;

	/// <param name="bestSonar">The highest anti-submarine value of the sonars owned, null when none is owned.</param>
	private static AswGroup AswGroupOf(IShipData ship, int? bestSonar) => ship switch
	{
		_ when ship.CanNoSonarOpeningAsw => AswGroup.NoSonar,
		_ when bestSonar is { } asw && ship.ASWBase >= 100 - asw => AswGroup.SingleSonar,
		_ => AswGroup.None,
	};

	/// <summary>The viewer's event check list: owned destroyers (and the latest remodel of those not owned) by what they carry.</summary>
	private List<KcvProgressGroup> MakeEventGroups()
	{
		if (!AllShips.Any() || !UserShips.Any() || !UserEquipment.Any()) return [];

		List<IShipData> allShips = AllShips
			.Where(s => !s.IsAbyssalShip)
			.Select(s => new ShipDataMock(s) { Level = 0 })
			.ToList<IShipData>();

		// as the viewer (its comment): a lower remodel owned hides a higher one that would fit a group
		List<IShipData> destroyers = UserShips
			.Concat(allShips)
			.Where(s => s.MasterShip.ShipType == ShipTypes.Destroyer)
			.OrderBy(s => s.SortID)
			.GroupBy(s => s.MasterShip.BaseShip().ShipID)
			.Select(g => g
				.OrderByDescending(s => s.Level)
				.ThenByDescending(s => s.SortID))
			.SelectMany(g => g.First().Level switch
			{
				0 => g.Take(1),
				_ => g.TakeWhile(s => s.Level > 0),
			})
			.ToList();

		int? bestSonar = UserEquipment
			.Where(e => e.MasterEquipment.IsSonar)
			.Select(e => (int?)e.MasterEquipment.ASW)
			.Max();

		IEnumerable<KcvProgressGroup> ByKind<T>(Func<IShipData, T> groupOf, T none) where T : Enum => destroyers
			.GroupBy(groupOf)
			.Where(g => !g.Key.Equals(none))
			.OrderBy(g => g.Key)
			.Select(g => Group(g.Key.Display(), g));

		return
		[
			.. ByKind(DaihatsuGroupOf, DaihatsuGroup.None),
			.. ByKind(FcfGroupOf, FcfGroup.None),
			.. ByKind(s => AswGroupOf(s, bestSonar), AswGroup.None),
		];
	}

	/// <summary>The viewer's CSV: per group a name and a level column, the groups side by side.</summary>
	[RelayCommand]
	private void ExportAsCsv()
	{
		List<List<KcvProgressShip>> shipLists = Groups
			.Select(g => g.Classes.SelectMany(c => c.Ships).ToList())
			.ToList();

		if (shipLists.Count is 0) return;

		StringBuilder sb = new();

		for (int i = 0; i < shipLists.Max(s => s.Count); i++)
		{
			foreach (List<KcvProgressShip> shipList in shipLists)
			{
				if (shipList.Count > i)
				{
					sb.Append(shipList[i].Name);
				}

				sb.Append(',');

				if (shipList.Count > i)
				{
					sb.Append(shipList[i].Level);
				}

				sb.Append(',');
			}

			sb.AppendLine();
		}

		SaveFileDialog dialog = new()
		{
			FileName = "Ships",
			DefaultExt = ".csv",
			Filter = "CSV UTF-8 (Comma delimited) (*.csv)|*.csv",
		};

		if (dialog.ShowDialog() is true)
		{
			File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
		}
	}
}
