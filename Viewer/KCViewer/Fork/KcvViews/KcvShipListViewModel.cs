using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Common;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Observer;
using KCViewer.ViewModels;

namespace KCViewer.Fork.KcvViews;

/// <summary>A modernizable parameter: its value and what modernization can still add ("MAX" when nothing).</summary>
public sealed record KcvShipStat(int Value, int Remain)
{
	public bool IsMax => Remain <= 0;
	public string Suffix => IsMax ? "MAX" : $"+{Remain}";
}

/// <summary>One ship of the list, taken when the list is built.</summary>
public sealed class KcvShipRow
{
	public required int Id { get; init; }
	public required int ShipId { get; init; }
	public required int TypeId { get; init; }
	public required string TypeName { get; init; }
	public required string Name { get; init; }
	public required int SortId { get; init; }
	public required int Level { get; init; }
	public required int ExpTotal { get; init; }
	public required int ExpNext { get; init; }
	public required int Condition { get; init; }
	public required KcvShipStat Firepower { get; init; }
	public required KcvShipStat Torpedo { get; init; }
	public required KcvShipStat AA { get; init; }
	public required KcvShipStat Armor { get; init; }
	public required KcvShipStat Luck { get; init; }
	public required int Los { get; init; }
	/// <summary>The ordinary slots' equipment; the reinforcement slot's is <see cref="Expansion"/>, after a line.</summary>
	public required List<EquipmentIconType> Equipment { get; init; }
	/// <summary>Equipment in the reinforcement slot, null when there is none (mockup 20261010-ex-slot-separator-v1).</summary>
	public EquipmentIconType? Expansion { get; init; }
	public required string EquipmentToolTip { get; init; }

	public string NextText => $"Next:{ExpNext}";

	// flat sort keys for the table columns
	public int FirepowerValue => Firepower.Value;
	public int TorpedoValue => Torpedo.Value;
	public int AAValue => AA.Value;
	public int ArmorValue => Armor.Value;
	public int LuckValue => Luck.Value;
}

/// <summary>A ship type check box of the filter.</summary>
public sealed class KcvShipTypeFilter(int id, string name) : ObservableObject
{
	public int Id { get; } = id;
	public string Name { get; } = name;
	public bool IsChecked { get; set; } = true;
}

/// <summary>
/// Fork: KanColleViewer's 所属艦娘一覧 (viewer-design/kcv-reference/02-ship-list.png): the ship list with its filters
/// (ship types with presets, level, lock, expedition, speed, remodel, modernization) and the table (ID, type, name,
/// level, condition, firepower / torpedo / AA / armor with MAX, luck, LoS, equipment). The viewer's ship groups stay
/// in their own panel; this is a separate window, opened from the tools menu while the style is on.
/// </summary>
public sealed partial class KcvShipListViewModel : WindowViewModelBase
{
	public string Title => ForkStrings.KcvShipList;

	/// <summary>Kept in the settings (mockup 20261005-list-search-v2, user decision 2026-10-05).</summary>
	public bool FiltersExpanded { get; set; } = ForkSettings.Current.KcvStyle.ShipListFiltersExpanded;

	/// <summary>The name box on the fold line (<see cref="KcvNameSearch"/>); not kept.</summary>
	public string NameFilter { get; set; } = "";

	/// <summary>
	/// While the filters are folded, those not at their default, in the order of the filter area: 「적용 중: 함종, 잠금」.
	/// Empty when unfolded or all at their default.
	/// </summary>
	public string AppliedText
	{
		get
		{
			if (FiltersExpanded) return "";

			List<string> applied = [];
			if (TypeFilters.Any(t => !t.IsChecked)) applied.Add(ForkStrings.KcvShipType);
			if (LevelFilter is not 0) applied.Add(ForkStrings.KcvLevel);
			if (LockFilter is not 0) applied.Add(ForkStrings.KcvLock);
			if (ExpansionSlotFilter is not 0) applied.Add(ForkStrings.KcvExpansionSlot);
			if (ExcludeExpedition) applied.Add(ForkStrings.KcvExpedition);
			if (SpeedFilter is not 0) applied.Add(ForkStrings.KcvSpeed);
			if (RemodelFilter is not 0) applied.Add(ForkStrings.KcvRemodel);
			if (ModernizationFilter is not 0) applied.Add(ForkStrings.KcvModernization);

			// the labels without their colon
			return applied.Count is 0 ? "" : ForkStrings.KcvFiltersApplied(applied.Select(label => label.TrimEnd(':')));
		}
	}

	public ObservableCollection<KcvShipTypeFilter> TypeFilters { get; } = [];

	/// <summary>すべて選択: checked when every type is; setting it checks or clears them all.</summary>
	public bool? AllTypes
	{
		get => TypeFilters.All(t => t.IsChecked) ? true : TypeFilters.Any(t => t.IsChecked) ? null : false;
		set => ApplyTypes(value is true ? TypeFilters.Select(t => t.Id) : []);
	}

	// 0: all, then the choices in the order of the radio buttons
	public int LevelFilter { get; set; }
	public int LockFilter { get; set; }
	/// <summary>The reinforcement expansion slot: 1 open (equipped or empty), 2 not open (mockup 20261024-shiplist-exslot-v2).</summary>
	public int ExpansionSlotFilter { get; set; }
	public bool ExcludeExpedition { get; set; }
	public int SpeedFilter { get; set; }
	public int RemodelFilter { get; set; }
	public int ModernizationFilter { get; set; }

	public ObservableCollection<KcvShipRow> Rows { get; } = [];

	private bool Updating { get; set; }

	public KcvShipListViewModel()
	{
		KCDatabase db = KCDatabase.Instance;
		IEnumerable<int> usedTypes = db.MasterShips.Values
			.Where(ship => !ship.IsAbyssalShip)
			.Select(ship => (int)ship.ShipType)
			.Distinct()
			.Order();
		foreach (int id in usedTypes)
		{
			if (db.ShipTypes[id] is not { } type) continue;

			KcvShipTypeFilter filter = new(id, type.NameEN);
			filter.PropertyChanged += (_, _) =>
			{
				if (Updating) return;
				OnPropertyChanged(nameof(AllTypes));
				Update();
			};
			TypeFilters.Add(filter);
		}

		PropertyChanged += (_, args) =>
		{
			if (args.PropertyName is nameof(LevelFilter) or nameof(LockFilter) or nameof(ExpansionSlotFilter) or nameof(ExcludeExpedition)
				or nameof(SpeedFilter) or nameof(RemodelFilter) or nameof(ModernizationFilter) or nameof(NameFilter))
			{
				Update();
			}

			if (args.PropertyName is nameof(FiltersExpanded))
			{
				ForkSettings.Current.KcvStyle.ShipListFiltersExpanded = FiltersExpanded;
				ForkSettings.Current.Save();
				OnPropertyChanged(nameof(AppliedText));
			}
		};

		Update();
	}

	public override void Loaded()
	{
		base.Loaded();

		APIObserver o = APIObserver.Instance;
		o.ApiPort_Port.ResponseReceived += OnApi;
		o.ApiGetMember_Ship2.ResponseReceived += OnApi;
		o.ApiGetMember_Ship3.ResponseReceived += OnApi;
		o.ApiGetMember_ShipDeck.ResponseReceived += OnApi;
		o.ApiReqKaisou_PowerUp.ResponseReceived += OnApi;
		o.ApiReqKousyou_GetShip.ResponseReceived += OnApi;
	}

	public override void Closed()
	{
		base.Closed();

		APIObserver o = APIObserver.Instance;
		o.ApiPort_Port.ResponseReceived -= OnApi;
		o.ApiGetMember_Ship2.ResponseReceived -= OnApi;
		o.ApiGetMember_Ship3.ResponseReceived -= OnApi;
		o.ApiGetMember_ShipDeck.ResponseReceived -= OnApi;
		o.ApiReqKaisou_PowerUp.ResponseReceived -= OnApi;
		o.ApiReqKousyou_GetShip.ResponseReceived -= OnApi;
	}

	private void OnApi(string apiName, dynamic data) => Update();

	/// <summary>Preset buttons: only these ship types (ids separated by commas).</summary>
	[RelayCommand]
	private void ApplyPreset(string ids) => ApplyTypes(ids.Split(',').Select(int.Parse));

	private void ApplyTypes(IEnumerable<int> ids)
	{
		HashSet<int> set = ids.ToHashSet();
		Updating = true;
		foreach (KcvShipTypeFilter filter in TypeFilters) filter.IsChecked = set.Contains(filter.Id);
		Updating = false;

		OnPropertyChanged(nameof(AllTypes));
		Update();
	}

	[RelayCommand]
	private void OpenShipEncyclopedia(int shipId)
	{
		KcvAlbums.ShowShip(shipId);
	}

	private void Update()
	{
		KCDatabase db = KCDatabase.Instance;
		HashSet<int> types = TypeFilters.Where(t => t.IsChecked).Select(t => t.Id).ToHashSet();
		HashSet<int> onExpedition = db.Fleet.Fleets.Values
			.Where(fleet => (int)fleet.ExpeditionState != 0)
			.SelectMany(fleet => fleet.Members)
			.ToHashSet();

		IEnumerable<ShipData> ships = db.Ships.Values
			.Where(ship => types.Contains((int)ship.MasterShip.ShipType))
			.Where(ship => LevelFilter switch { 1 => ship.Level >= 2, 2 => ship.Level == 1, _ => true })
			.Where(ship => LockFilter switch { 1 => ship.IsLocked, 2 => !ship.IsLocked, _ => true })
			// open: the game's api_slot_ex is not 0 (-1 open and empty, else the equipment), as the ship selector's 보강
			.Where(ship => ExpansionSlotFilter switch { 1 => ship.IsExpansionSlotAvailable, 2 => !ship.IsExpansionSlotAvailable, _ => true })
			.Where(ship => !ExcludeExpedition || !onExpedition.Contains(ship.MasterID))
			.Where(ship => SpeedFilter switch { 1 => ship.Speed >= 10, 2 => ship.Speed < 10, _ => true })
			.Where(ship => RemodelFilter switch { 1 => IsRemodelDone(ship), 2 => !IsRemodelDone(ship), _ => true })
			.Where(ship => ModernizationFilter switch { 1 => IsModernized(ship), 2 => !IsModernized(ship), _ => true })
			.Where(ship => NameFilter is "" || KcvNameSearch.Matches(ship.MasterShip, NameFilter));

		// The collection is kept, so the table keeps its sort order.
		Rows.Clear();
		foreach (ShipData ship in ships.OrderBy(ship => ship.MasterID)) Rows.Add(RowOf(ship));

		// every filter change comes here
		OnPropertyChanged(nameof(AppliedText));
	}

	/// <summary>No later remodel: none, or one that goes back to an earlier form (convertible ships).</summary>
	private static bool IsRemodelDone(ShipData ship) =>
		ship.MasterShip.RemodelAfterShip is not { } after || after.RemodelTier <= ship.MasterShip.RemodelTier;

	private static bool IsModernized(ShipData ship) =>
		ship.FirepowerRemain <= 0 && ship.TorpedoRemain <= 0 && ship.AARemain <= 0 && ship.ArmorRemain <= 0;

	private static KcvShipRow RowOf(ShipData ship)
	{
		List<IEquipmentData> equipment = ship.AllSlotInstance.OfType<IEquipmentData>().ToList();
		// the reinforcement slot's equipment, shown apart from the ordinary slots' (the tooltip lists all)
		IEquipmentData? expansion = ship.ExpansionSlotInstance;

		return new KcvShipRow
		{
			Id = ship.MasterID,
			ShipId = ship.ShipID,
			TypeId = (int)ship.MasterShip.ShipType,
			TypeName = ship.MasterShip.ShipTypeName,
			Name = ship.Name,
			SortId = ship.SortID,
			Level = ship.Level,
			ExpTotal = ship.ExpTotal,
			ExpNext = ship.ExpNext,
			Condition = ship.Condition,
			Firepower = new(ship.FirepowerBase, ship.FirepowerRemain),
			Torpedo = new(ship.TorpedoBase, ship.TorpedoRemain),
			AA = new(ship.AABase, ship.AARemain),
			Armor = new(ship.ArmorBase, ship.ArmorRemain),
			Luck = new(ship.LuckBase, ship.LuckRemain),
			Los = ship.LOSBase,
			Equipment = ship.SlotInstance.OfType<IEquipmentData>().Select(eq => eq.MasterEquipment.IconTypeTyped).ToList(),
			Expansion = expansion?.MasterEquipment.IconTypeTyped,
			EquipmentToolTip = string.Join("\n", equipment.Select(eq => eq.NameWithLevel)),
		};
	}
}
