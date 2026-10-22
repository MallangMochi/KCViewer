using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using KCViewer.Common;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;
using KCViewer.Data;
using KCViewer.Observer;
using KCViewer.Utility;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the maps of the last api_get_member/mapinfo answer. The game lists the open maps only (a new admiral's first
/// answer has 1-1 alone, recorded 2026-10-01), while the viewer keeps a <see cref="IMapInfoData"/> for every map of the
/// master data, so this is what tells an open map from a locked one. Installed at start (FormMainViewModel.AddForkViews),
/// as the answer may come before the window is opened.
/// </summary>
public static class KcvMapHpTracker
{
	private static HashSet<int> Open { get; set; } = [];

	public static IReadOnlySet<int> OpenMaps => Open;

	/// <summary>After a mapinfo answer (the viewer's database is updated before) and a difficulty choice.</summary>
	public static event Action? Updated;

	public static void Install()
	{
		APIObserver o = APIObserver.Instance;
		o.ApiGetMember_MapInfo.ResponseReceived += OnMapInfo;
		o.ApiReqMap_SelectEventMapRank.RequestReceived += (_, _) => Updated?.Invoke();
	}

	private static void OnMapInfo(string apiName, dynamic data)
	{
		// the older answer was the list itself (as the viewer's mapinfo handler reads it)
		dynamic list = data.api_map_info() ? data.api_map_info : data;

		HashSet<int> open = [];
		foreach (dynamic elem in list)
		{
			open.Add((int)elem.api_id);
		}

		Open = open;
		Updated?.Invoke();
	}
}

/// <summary>A map of the list: 1-6 #1, its area and name, and what is left of its gauge.</summary>
public sealed record KcvMapHpRow(
	string Number,
	string DifficultyText,
	string GaugeText,
	string AreaName,
	string MapName,
	int Current,
	int Max,
	Brush Bar)
{
	public string MaxText => $" / {Max}";
}

/// <summary>
/// Fork: KanColleViewer's MapHP plugin as a window of the tools menu, drawn as the user's reference of it (攻略中マップ),
/// without its remaining count and its boss HP settings. The values are the ones the viewer's information panel shows
/// for the map selection screen (InformationViewModel.GetMapGauge), from the game's answers only: no outside data.
/// Shown: every open map with a gauge left (kills to go, or HP / transport points left) and every open map not cleared
/// yet without a gauge, which needs its boss once (1 / 1). The number is what is left; the gauge number from the first.
/// Kill and HP gauges are red, transport gauges green (user's choice, mockup 3).
/// Fleet transport: the viewer's normal TP of the first fleet (both fleets when combined); A is S × 0.7 rounded down.
/// </summary>
public sealed class KcvMapHpViewModel : WindowViewModelBase
{
	public string Title => ForkStrings.KcvMapHp;
	public string Header => ForkStrings.KcvMapHpHeader;
	public string TransportLabel => ForkStrings.KcvMapHpTransport;
	public string Note => ForkStrings.KcvMapHpNote;

	public List<KcvMapHpRow> Rows { get; private set; } = [];

	public int TransportS { get; private set; }
	public int TransportA { get; private set; }

	public KcvMapHpViewModel()
	{
		UpdateMaps();
		UpdateTransport();
	}

	public override void Loaded()
	{
		base.Loaded();

		KcvMapHpTracker.Updated += UpdateMaps;
		// the fleets change with many answers (composition, equipment, damage); the viewer's 1 s timer covers them all
		SystemEvents.UpdateTimerTick += UpdateTransport;
	}

	public override void Closed()
	{
		base.Closed();

		KcvMapHpTracker.Updated -= UpdateMaps;
		SystemEvents.UpdateTimerTick -= UpdateTransport;
	}

	private void UpdateMaps()
	{
		KCDatabase db = KCDatabase.Instance;
		IReadOnlySet<int> open = KcvMapHpTracker.OpenMaps;

		List<KcvMapHpRow> rows = [];
		foreach (IMapInfoData map in db.MapInfo.Values.OrderBy(m => m.MapID))
		{
			if (!open.Contains(map.MapID)) continue;
			if (GaugeOf(map) is not (int current, int max)) continue;

			rows.Add(new KcvMapHpRow(
				$"{map.MapAreaID}-{map.MapInfoID}",
				map.EventDifficulty > 0 ? $" {Constants.GetDifficulty(map.EventDifficulty)}" : "",
				map.CurrentGaugeIndex > 0 ? $" #{map.CurrentGaugeIndex}" : "",
				db.MapArea[map.MapAreaID]?.NameEN ?? "",
				map.NameEN,
				current,
				max,
				map.GaugeType == 3 ? KcvBrushes.GaugeFull : KcvBrushes.MapGaugeRed));
		}

		Rows = rows;
	}

	/// <summary>What is left of the gauge and its size, or null for a map that is done.</summary>
	private static (int Current, int Max)? GaugeOf(IMapInfoData map)
	{
		// kills (or, on 1-6, arrivals at the goal) to go
		if (map.RequiredDefeatedCount != -1 && map.CurrentDefeatedCount < map.RequiredDefeatedCount)
		{
			return (map.RequiredDefeatedCount - map.CurrentDefeatedCount, map.RequiredDefeatedCount);
		}

		// HP or transport points left (api_eventmap)
		if (map.MapHPMax > 0)
		{
			return map.IsCleared && map.MapHPCurrent <= 0 ? null : (map.MapHPCurrent, map.MapHPMax);
		}

		// no gauge: its boss once, until it is cleared
		return map.IsCleared ? null : (1, 1);
	}

	private void UpdateTransport()
	{
		FleetManager fleets = KCDatabase.Instance.Fleet;
		IEnumerable<IFleetData?> sortie = (int)fleets.CombinedFlag != 0 ? [fleets[1], fleets[2]] : [fleets[1]];

		TransportS = TpGauge.Normal.GetTp(sortie.OfType<IFleetData>().ToList());
		TransportA = (int)(TransportS * 0.7);
	}
}
