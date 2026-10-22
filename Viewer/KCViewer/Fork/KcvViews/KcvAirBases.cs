using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Observer;
using KCViewer.Resource;
using KCViewer.Utility.Data;

namespace KCViewer.Fork.KcvViews;

/// <summary>A squadron of a land base: the plane, its proficiency and count, or an empty or relocating slot.</summary>
public sealed record KcvAirSquadron(
	bool Deployed,
	bool Relocating,
	EquipmentIconType Icon,
	string Name,
	int AircraftLevel,
	int Count,
	int Max,
	bool Tired)
{
	public bool Empty => !Deployed && !Relocating;
	public bool Short => Deployed && Count < Max;
	public string MaxText => $"/{Max}";
	public string DetailMaxText => $"/{Max}]";
	public string ProficiencyText => AircraftLevel > 0 ? $" {ForkStrings.KcvProficiency(AircraftLevel)}" : "";
	public string TiredText => Tired ? $" {ForkStrings.KcvAirTiredMark}" : "";
	public string PlaceholderText => Relocating ? ForkStrings.KcvAirRelocatingSlot : ForkStrings.KcvAirEmptySlot;
}

/// <summary>A land base with what the panel and the details window show of it.</summary>
public sealed record KcvAirCorps(
	string Name,
	AirBaseActionKind Action,
	string ActionText,
	int Distance,
	int BaseDistance,
	int BonusDistance,
	int Power,
	string PowerTip,
	string ConditionText,
	Brush ConditionBrush,
	int Planes,
	int PlanesMax,
	string StatusText,
	bool NeedsSupply,
	bool IsTired,
	bool HasEmpty,
	bool HasRelocating,
	List<KcvAirSquadron> Squadrons)
{
	public bool IsDefense => Action == AirBaseActionKind.AirDefense;
	/// <summary>Panel: 7 (6+1); nothing after it without a bonus.</summary>
	public string BonusText => BonusDistance > 0 ? $"({BaseDistance}+{BonusDistance})" : "";
	/// <summary>Details window: (6 + 1).</summary>
	public string DetailBonusText => BonusDistance > 0 ? $"({BaseDistance} + {BonusDistance})" : "";
	public string PowerLabel => IsDefense ? ForkStrings.KcvAirDefenseLabel : ForkStrings.KcvAirPowerLabel;
	public bool PlanesShort => Planes < PlanesMax;
	public string PlanesMaxText => $" / {PlanesMax}{ForkStrings.KcvAirPlanesUnit}";
}

/// <summary>The panel's squadron column headers: 1 → 1중대 / 第1中隊.</summary>
public sealed class KcvAirColumnConverter : System.Windows.Data.IValueConverter
{
	public static KcvAirColumnConverter Instance { get; } = new();

	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
		ForkStrings.KcvAirColumnSquadron(int.TryParse(value?.ToString(), out int n) ? n : 0);

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>A check of the details window's list, bad ones in red.</summary>
public sealed record KcvAirCheck(string Text, bool Ok);

/// <summary>The land bases of one area, with its totals, its state and the checks.</summary>
public sealed record KcvAirArea(
	int AreaId,
	string Name,
	int DefensePower,
	int HighAltitude,
	List<KcvAirCorps> Corps,
	string ActionSummary,
	string StateText,
	Brush StateBrush,
	List<KcvAirCheck> Checks)
{
	public string Title => $"{Name} (#{AreaId})";
	public string DefenseText => ForkStrings.KcvAirDefenseValues(DefensePower, HighAltitude);
}

/// <summary>
/// Fork: the land bases as the KanColleViewer land base panel and the land base details window show them (mockup 5,
/// user's approval 2026-10-02 with one details button). Everything comes from the viewer's database and its
/// calculator, as its own land base panel uses them:
/// - fighter power: <see cref="Calculator.GetAirSuperiority(IBaseAirCorpsData)"/> for the base's action (a base on
///   standby is computed as the viewer computes it); the area's air defense and high altitude values as the viewer's
///   tooltip sums them over the air defense bases.
/// - the air defense values of a sortie base (its fighter power tooltip): the same calculation with the base on air
///   defense, alone (its own interceptors only for the high altitude bonus).
/// - state (interpretation; the game shows none): a sortie base is ready without planes short, fatigue or relocation;
///   an area is ready to sortie when all its sortie bases are, on air defense when it has no sortie base but an air
///   defense one, else idle (its first base's action).
/// </summary>
public static class KcvAirBases
{
	private static readonly SolidColorBrush ConditionSparkle = KcvBrushes.ConditionSparkle;

	// the marks after a base's name (mockup 5): planes short, fatigued
	public static SolidColorBrush SupplyMark { get; } = Frozen(0xC9, 0xA4, 0x3A);
	public static SolidColorBrush TiredMark { get; } = Frozen(0xFF, 0x9A, 0x20);

	private static SolidColorBrush Frozen(byte r, byte g, byte b)
	{
		SolidColorBrush brush = new(Color.FromRgb(r, g, b));
		brush.Freeze();
		return brush;
	}

	/// <summary>The answers the viewer's land base panel updates on (BaseAirCorpsViewModel).</summary>
	public static IEnumerable<APIBase> Apis(APIObserver o) =>
	[
		o.ApiPort_Port, o.ApiGetMember_MapInfo, o.ApiGetMember_BaseAirCorps, o.ApiReqAirCorps_ChangeDeploymentBase,
		o.ApiReqAirCorps_ChangeName, o.ApiReqAirCorps_SetAction, o.ApiReqAirCorps_SetPlane, o.ApiReqAirCorps_Supply,
		o.ApiReqAirCorps_ExpandBase, o.ApiPort_AirCorpsCondRecoveryWithTimer,
	];

	/// <summary>The areas with land bases, in area order. eventOnly: the viewer's setting of its panel (events only).</summary>
	public static List<KcvAirArea> Areas(bool eventOnly)
	{
		KCDatabase db = KCDatabase.Instance;
		List<BaseAirCorpsData> all = db.BaseAirCorps.Values.Where(c => c != null).ToList();

		if (eventOnly)
		{
			List<BaseAirCorpsData> events = all.Where(c => db.MapArea[c.MapAreaID] is { MapType: 1 }).ToList();
			if (events.Count > 0) all = events;
		}

		return all
			.GroupBy(c => c.MapAreaID)
			.OrderBy(g => g.Key)
			.Select(g => AreaOf(g.Key, g.OrderBy(c => c.AirCorpsID).ToList()))
			.ToList();
	}

	private static KcvAirArea AreaOf(int areaId, List<BaseAirCorpsData> corps)
	{
		KCDatabase db = KCDatabase.Instance;
		List<IBaseAirCorpsData> area = corps.OfType<IBaseAirCorpsData>().ToList();
		List<BaseAirCorpsData> defense = corps.Where(c => c.ActionKind == AirBaseActionKind.AirDefense).ToList();

		int defensePower = defense.Sum(c => Calculator.GetAirSuperiority(c));
		int highAltitude = defense.Sum(c => Calculator.GetHighAltitudeAirSuperiority(c, area, false));

		List<KcvAirCorps> rows = corps.Select(CorpsOf).ToList();

		string summary = string.Join(" · ", corps
			.GroupBy(c => c.ActionKind)
			.OrderBy(g => g.Key switch { AirBaseActionKind.Mission => 0, AirBaseActionKind.AirDefense => 1, _ => 2 + (int)g.Key })
			.Select(g => $"{Constants.GetBaseAirCorpsActionKind(g.Key)} {g.Count()}"));

		List<KcvAirCorps> sortie = rows.Where(r => r.Action == AirBaseActionKind.Mission).ToList();
		(string state, Brush brush) = true switch
		{
			_ when sortie.Count > 0 && sortie.All(Ready) => (ForkStrings.KcvAirStateReady, (Brush)KcvBrushes.ReadyBar),
			_ when sortie.Count > 0 => (ForkStrings.KcvAirStatePreparing, KcvBrushes.WaitingBar),
			_ when rows.Any(r => r.IsDefense) => (ForkStrings.KcvAirStateDefense, KcvBrushes.ExpeditionBar),
			_ => (ForkStrings.KcvAirStateIdle(rows.FirstOrDefault()?.ActionText ?? ""), KcvBrushes.NoShipBar),
		};

		bool supply = rows.Any(r => r.NeedsSupply);
		bool tired = rows.Any(r => r.IsTired);
		bool relocating = rows.Any(r => r.HasRelocating);
		bool empty = rows.Any(r => r.HasEmpty);

		return new KcvAirArea(
			areaId,
			db.MapArea[areaId]?.NameEN ?? "",
			defensePower,
			highAltitude,
			rows,
			summary,
			state,
			brush,
			[
				new(ForkStrings.KcvAirCheckSupply(!supply), !supply),
				new(ForkStrings.KcvAirCheckFatigue(!tired), !tired),
				new(ForkStrings.KcvAirCheckRelocating(!relocating), !relocating),
				new(ForkStrings.KcvAirCheckEmpty(!empty), !empty),
			]);
	}

	private static bool Ready(KcvAirCorps corps) => !corps.NeedsSupply && !corps.IsTired && !corps.HasRelocating;

	private static KcvAirCorps CorpsOf(BaseAirCorpsData corps)
	{
		List<IBaseAirCorpsSquadron?> squadrons = corps.Squadrons.Values.Cast<IBaseAirCorpsSquadron?>().ToList();
		List<IBaseAirCorpsSquadron> deployed = squadrons.OfType<IBaseAirCorpsSquadron>().Where(s => s.State == 1).ToList();

		int power = Calculator.GetAirSuperiority(corps);
		string tip = corps.ActionKind switch
		{
			AirBaseActionKind.AirDefense => DefenseValues(corps),
			_ => $"{ForkStrings.KcvAirStates((int)(power / 3.0), (int)(power / 1.5), Math.Max((int)(power * 1.5 - 1), 0), Math.Max((int)(power * 3.0 - 1), 0))}\n{DefenseValues(new OnAirDefense(corps))}",
		};

		AirBaseCondition worst = deployed.Count > 0 ? deployed.Max(s => s.Condition) : AirBaseCondition.Normal;
		(string conditionText, Brush conditionBrush) = worst switch
		{
			AirBaseCondition.Sparkled => (GeneralRes.MaximumMorale, ConditionSparkle),
			AirBaseCondition.Tired => (GeneralRes.Tired, KcvBrushes.ConditionTired),
			AirBaseCondition.VeryTired => (GeneralRes.VeryTired, KcvBrushes.ConditionVeryTired),
			_ => (ForkStrings.KcvAirConditionNormal, (Brush)KcvBrushes.ConditionNormal),
		};

		bool needsSupply = deployed.Any(s => s.AircraftCurrent < s.AircraftMax);
		bool tired = deployed.Any(s => s.Condition >= AirBaseCondition.Tired);
		bool relocating = squadrons.Any(s => s is { State: 2 });
		bool empty = squadrons.Any(s => s is null or { State: 0 });

		string status = true switch
		{
			_ when relocating => ForkStrings.KcvAirStatusRelocating,
			_ when needsSupply => ForkStrings.KcvAirStatusSupply,
			_ when tired => ForkStrings.KcvAirStatusTired,
			_ => ForkStrings.KcvAirStatusReady,
		};

		return new KcvAirCorps(
			corps.Name,
			corps.ActionKind,
			Constants.GetBaseAirCorpsActionKind(corps.ActionKind),
			corps.Distance,
			corps.BaseDistance,
			corps.BonusDistance,
			power,
			tip,
			conditionText,
			conditionBrush,
			deployed.Sum(s => s.AircraftCurrent),
			deployed.Sum(s => s.AircraftMax),
			status,
			needsSupply,
			tired,
			empty,
			relocating,
			squadrons.Select(SquadronOf).ToList());
	}

	private static KcvAirSquadron SquadronOf(IBaseAirCorpsSquadron? squadron)
	{
		if (squadron is not { State: 1, EquipmentInstance: { } eq })
		{
			return new KcvAirSquadron(false, squadron is { State: 2 }, EquipmentIconType.Nothing, "", 0, 0, 0, false);
		}

		return new KcvAirSquadron(
			true,
			false,
			eq.MasterEquipment.IconTypeTyped,
			eq.Name,
			eq.AircraftLevel,
			squadron.AircraftCurrent,
			squadron.AircraftMax,
			squadron.Condition >= AirBaseCondition.Tired);
	}

	// The base alone: its own interceptors for the high altitude bonus.
	private static string DefenseValues(IBaseAirCorpsData corps) => ForkStrings.KcvAirDefenseValues(
		Calculator.GetAirSuperiority(corps),
		Calculator.GetHighAltitudeAirSuperiority(corps, [corps], false));

	/// <summary>A base as if it were on air defense, for the calculator, which picks its formulas by the action.</summary>
	private sealed class OnAirDefense(IBaseAirCorpsData corps) : IBaseAirCorpsData
	{
		public int MapAreaID => corps.MapAreaID;
		public int AirCorpsID => corps.AirCorpsID;
		public string Name => corps.Name;
		public int Distance => corps.Distance;
		public int BonusDistance => corps.BonusDistance;
		public int BaseDistance => corps.BaseDistance;
		public AirBaseActionKind ActionKind => AirBaseActionKind.AirDefense;
		public List<int> StrikePoints => corps.StrikePoints;
		public IDictionary<int, IBaseAirCorpsSquadron> Squadrons => corps.Squadrons;
		public int ID => corps.ID;
		public bool IsAvailable => corps.IsAvailable;
		public int HPCurrent => corps.HPCurrent;
		public int HPMax => corps.HPMax;
	}
}
