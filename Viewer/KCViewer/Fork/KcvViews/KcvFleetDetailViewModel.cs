using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Common;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;
using KCViewer.Data;
using KCViewer.Observer;
using KCViewer.Utility.Data;

namespace KCViewer.Fork.KcvViews;

/// <summary>A fleet of the list on the left.</summary>
public sealed class KcvFleetTab(int id, string name) : ObservableObject
{
	public int Id { get; } = id;
	public string Name { get; } = name;
	public bool IsSelected { get; set; }
}

/// <summary>A line of the readiness list (出撃準備完了！, 艦隊は遠征中ではありません, ...).</summary>
public sealed record KcvFleetCheck(string Text, bool Ok);

/// <summary>An equipment line: [03 / 03] for planes, icon, name, ★ and proficiency.</summary>
public sealed record KcvDetailSlot(EquipmentIconType Icon, string Name, int Level, int AircraftLevel, string AircraftText)
{
	/// <summary>Equipment in the reinforcement slot: a line above it (mockup 20261010-ex-slot-separator-v1).</summary>
	public bool IsExpansion { get; init; }

	public string LevelText => Level switch
	{
		<= 0 => "",
		>= 10 => "★max",
		_ => $"★+{Level}",
	};

	public string ProficiencyText => AircraftLevel > 0 ? ForkStrings.KcvProficiency(AircraftLevel) : "";
}

/// <summary>A ship of the selected fleet.</summary>
public sealed record KcvDetailShip(
	int ShipId,
	string TypeName,
	string Name,
	int Level,
	int ExpNext,
	int Condition,
	int Hp,
	int HpMax,
	string HealthText,
	bool HasDamageControl,
	int Fuel,
	int FuelMax,
	int Ammo,
	int AmmoMax,
	List<KcvDetailSlot> Slots)
{
	/// <summary>The combined fleet's first ship of fleet 2: a line above it.</summary>
	public bool FleetBreak { get; init; }
}

/// <summary>
/// Fork: KanColleViewer's 艦隊詳細 (viewer-design/kcv-reference/05-fleet-detail.webp). The viewer has no such window.
/// Left: the fleets, the fleet's numbers (total and average level, LoS, fighter power without and with full
/// proficiency, speed) and its state with a readiness list; right: each ship with condition, HP and its state,
/// fuel and ammo, and the equipment with plane counts, improvement and proficiency.
/// Readiness (interpretation, KanColleViewer's rules are not documented): heavily damaged is HP 25% or less, fatigued is
/// condition below 30, resupply is needed when fuel or ammo is below the maximum, an empty fleet is not composed.
/// The combined fleet (fleet id 0, mockup 20261009-combined-fleet-detail-v1, approved 2026-10-09 "구현"): a tab above the
/// fleets while fleets 1 and 2 are combined (「연합함대 · 수상 부대」), the numbers and the readiness of both fleets, and
/// fleet 1's ships, a line, fleet 2's ships. Opened from the combined fleet panel's [함대 상세].
/// </summary>
public sealed partial class KcvFleetDetailViewModel : WindowViewModelBase
{
	public string Title => ForkStrings.KcvFleetDetail;

	public ObservableCollection<KcvFleetTab> Fleets { get; } = [];

	public int FleetId { get; private set; }

	public int TotalLevel { get; private set; }
	public string AverageLevel { get; private set; } = "";
	public string Los { get; private set; } = "";
	public int FighterPowerMin { get; private set; }
	public int FighterPowerMax { get; private set; }
	public string SpeedText { get; private set; } = "";

	public string StateText { get; private set; } = "";
	public Brush StateBrush { get; private set; } = KcvBrushes.NoShipBar;

	public List<KcvFleetCheck> Checks { get; private set; } = [];

	public List<KcvDetailShip> Ships { get; private set; } = [];

	public KcvFleetDetailViewModel(int fleetId)
	{
		FleetId = fleetId;
		Update();
	}

	/// <summary>The requests the viewer's fleet panels update on (FleetViewModel).</summary>
	private static IEnumerable<APIBase> RequestApis(APIObserver o) =>
	[
		o.ApiReqNyukyo_Start, o.ApiReqNyukyo_SpeedChange, o.ApiReqHensei_Change, o.ApiReqKousyou_DestroyShip,
		o.ApiReqMember_UpdateDeckName, o.ApiReqKaisou_Remodeling, o.ApiReqMap_Start, o.ApiReqHensei_Combined,
		o.ApiReqKaisou_OpenExSlot,
	];

	/// <summary>The responses the viewer's fleet panels update on (FleetViewModel).</summary>
	private static IEnumerable<APIBase> ResponseApis(APIObserver o) =>
	[
		o.ApiPort_Port, o.ApiGetMember_Ship2, o.ApiGetMember_NDock, o.ApiReqHokyu_Charge, o.ApiReqKousyou_DestroyShip,
		o.ApiGetMember_Ship3, o.ApiReqKaisou_PowerUp, o.ApiGetMember_Deck, o.ApiGetMember_SlotItem, o.ApiReqMap_Start,
		o.ApiReqMap_Next, o.ApiGetMember_ShipDeck, o.ApiReqHensei_PresetSelect, o.ApiReqKaisou_SlotExchangeIndex,
		o.ApiGetMember_RequireInfo, o.ApiReqKaisou_SlotDeprive, o.ApiReqKaisou_Marriage, o.ApiReqMap_AnchorageRepair,
	];

	public override void Loaded()
	{
		base.Loaded();

		APIObserver o = APIObserver.Instance;
		foreach (APIBase api in RequestApis(o)) api.RequestReceived += OnApi;
		foreach (APIBase api in ResponseApis(o)) api.ResponseReceived += OnApi;
	}

	public override void Closed()
	{
		base.Closed();

		APIObserver o = APIObserver.Instance;
		foreach (APIBase api in RequestApis(o)) api.RequestReceived -= OnApi;
		foreach (APIBase api in ResponseApis(o)) api.ResponseReceived -= OnApi;
	}

	private void OnApi(string apiName, dynamic data) => Update();

	[RelayCommand]
	public void SelectFleet(int fleetId)
	{
		FleetId = fleetId;
		Update();
	}

	private void Update()
	{
		KCDatabase db = KCDatabase.Instance;

		// the fleets, with the combined fleet above them while fleets 1 and 2 are combined
		bool combined = db.Fleet.CombinedFlag != FleetType.Single && db.Fleet[1] is not null && db.Fleet[2] is not null;
		List<(int Id, string Name)> tabs = db.Fleet.Fleets.Values.OrderBy(fleet => fleet.FleetID).Select(fleet => (fleet.FleetID, fleet.Name)).ToList();
		if (combined) tabs.Insert(0, (0, ForkStrings.KcvCombinedFleetTab(Constants.GetCombinedFleet(db.Fleet.CombinedFlag))));
		if (!tabs.Select(t => t.Id).SequenceEqual(Fleets.Select(t => t.Id))
			|| tabs.Zip(Fleets).Any(pair => pair.First.Name != pair.Second.Name))
		{
			Fleets.Clear();
			foreach ((int id, string name) in tabs) Fleets.Add(new KcvFleetTab(id, name));
		}

		// the combined fleet broken up: its first fleet
		if (FleetId == 0 && !combined) FleetId = 1;

		foreach (KcvFleetTab tab in Fleets) tab.IsSelected = tab.Id == FleetId;

		List<FleetData> chosen = FleetId == 0
			? [db.Fleet[1], db.Fleet[2]]
			: db.Fleet.Fleets[FleetId] is FleetData one ? [one] : [];
		if (chosen.Count == 0)
		{
			Ships = [];
			Checks = [];
			return;
		}

		// each ship with its own fleet (escape and the line before fleet 2)
		List<(IShipData Ship, FleetData Fleet)> members = chosen
			.SelectMany(fleet => (fleet.MembersInstance?.OfType<IShipData>() ?? Enumerable.Empty<IShipData>()).Select(ship => (ship, fleet)))
			.ToList();

		TotalLevel = members.Sum(member => member.Ship.Level);
		AverageLevel = members.Count > 0 ? ((double)TotalLevel / members.Count).ToString("0.00") : "0";
		// the combined fleet's 색적 as the fleet overview's combined card: each fleet's, cut to 2 decimals, added
		Los = chosen.Count == 1
			? chosen[0].GetSearchingAbilityString(1)
			: chosen.Sum(fleet => Math.Floor(Calculator.GetSearchingAbility_New33(fleet, 1) * 100) / 100).ToString("0.00");
		FighterPowerMin = chosen.Sum(fleet => Calculator.GetAirSuperiority(fleet, false));
		FighterPowerMax = chosen.Sum(fleet => Calculator.GetAirSuperiority(fleet, true));
		SpeedText = ForkStrings.KcvFleetSpeed(Constants.GetSpeed(members.Select(member => member.Ship.Speed).DefaultIfEmpty(20).Min()));

		bool inSortie = chosen.Any(fleet => fleet.IsInSortie);
		bool onExpedition = chosen.Any(fleet => (int)fleet.ExpeditionState != 0);
		bool damaged = members.Any(member => !member.Ship.IsEscaped(member.Fleet) && member.Ship.HPRate <= 0.25);
		bool docked = members.Any(member => member.Ship.RepairingDockID != -1);
		bool needsSupply = members.Any(member => member.Ship.Fuel < member.Ship.FuelMax || member.Ship.Ammo < member.Ship.AmmoMax);
		bool fatigued = members.Any(member => member.Ship.Condition < 30);
		bool empty = members.Count == 0;
		bool ready = !inSortie && !onExpedition && !damaged && !docked && !needsSupply && !fatigued && !empty;

		(StateText, StateBrush) = (inSortie, onExpedition, ready) switch
		{
			(true, _, _) => (ForkStrings.KcvStateSortie, (Brush)KcvBrushes.SortieBar),
			(_, true, _) => (ForkStrings.KcvStateExpedition, KcvBrushes.ExpeditionBar),
			(_, _, true) => (ForkStrings.KcvStateReady, KcvBrushes.ReadyBar),
			_ => (ForkStrings.KcvStateNotReady, KcvBrushes.WaitingBar),
		};

		Checks =
		[
			new(ForkStrings.KcvCheckReady(ready), ready),
			new(ForkStrings.KcvCheckExpedition(!onExpedition), !onExpedition),
			new(ForkStrings.KcvCheckDamage(!damaged), !damaged),
			new(ForkStrings.KcvCheckDock(!docked), !docked),
			new(ForkStrings.KcvCheckSupply(!needsSupply), !needsSupply),
			new(ForkStrings.KcvCheckFatigue(!fatigued), !fatigued),
			new(ForkStrings.KcvCheckComposition(!empty), !empty),
		];

		Ships = members
			.Select((member, i) => ShipOf(member.Ship, member.Fleet) with
			{
				FleetBreak = chosen.Count > 1 && i > 0 && member.Fleet != members[i - 1].Fleet,
			})
			.ToList();
	}

	private static KcvDetailShip ShipOf(IShipData ship, IFleetData fleet)
	{
		List<KcvDetailSlot> slots = [];
		List<IEquipmentData?> equipment = ship.AllSlotInstance.ToList();
		for (int i = 0; i < equipment.Count; i++)
		{
			if (equipment[i] is not { } eq) continue;

			// plane counts only for plane slots (not the reinforcement slot)
			bool plane = i < ship.MasterShip.Aircraft.Count && eq.MasterEquipment.IsAircraft && ship.MasterShip.Aircraft[i] > 0;
			string aircraft = plane ? $"[{ship.Aircraft[i]:00} / {ship.MasterShip.Aircraft[i]:00}]" : "";

			// the reinforcement slot is the last one (ShipData.AllSlot: the ordinary slots, then ExpansionSlot)
			slots.Add(new KcvDetailSlot(eq.MasterEquipment.IconTypeTyped, eq.Name, eq.Level, eq.AircraftLevel, aircraft)
			{
				IsExpansion = i == equipment.Count - 1,
			});
		}

		bool damageControl = equipment.Any(eq => eq?.MasterEquipment.IconTypeTyped is EquipmentIconType.DamageControl);

		string health = (ship.IsEscaped(fleet), ship.RepairingDockID != -1, ship.HPRate) switch
		{
			(true, _, _) => ForkStrings.KcvEscaped,
			(_, true, _) => ForkStrings.KcvInDock,
			(_, _, > 0.75) => ForkStrings.KcvHealthy,
			(_, _, > 0.5) => ForkStrings.KcvLightDamage,
			(_, _, > 0.25) => ForkStrings.KcvModerateDamage,
			(_, _, > 0) => ForkStrings.KcvHeavyDamage,
			_ => ForkStrings.KcvSunk,
		};
		if (damageControl) health += $"・{ForkStrings.KcvDamageControl}";

		return new KcvDetailShip(
			ship.ShipID,
			ship.MasterShip.ShipTypeName,
			ship.Name,
			ship.Level,
			ship.ExpNext,
			ship.Condition,
			ship.HPCurrent,
			ship.HPMax,
			health,
			damageControl,
			ship.Fuel,
			ship.FuelMax,
			ship.Ammo,
			ship.AmmoMax,
			slots);
	}
}
