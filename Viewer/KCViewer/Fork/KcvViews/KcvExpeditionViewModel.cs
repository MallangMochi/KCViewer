using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Resource;
using KCViewer.Utility;
using KCViewer.ViewModels;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: a docking panel with the expeditions of fleets 2 to 4, drawn as the dock and arsenal tables
/// (KcvExpeditionView, mockup v2 approved 2026-10-02): fleet number, the expedition in progress (待機中 or 艦隊未開放
/// otherwise) and the time left, with the return time as its tooltip. The context menu lays the fleets out one under
/// another or side by side (KcvPanelLayout) and shows or hides the expedition names (mockup 20261006-timer-panels-v2).
/// Added by FormMainViewModel.AddForkViews and shown by the template in Fork/ForkViews.xaml.
/// </summary>
public sealed class KcvExpeditionViewModel : AnchorableViewModel
{
	public IReadOnlyList<KcvExpeditionLine> Lines { get; } = [new(2), new(3), new(4)];

	/// <summary>The context menu's 원정명을 표시: on, 「[02] 장거리연습항해」 (default); off, 「[02]」 (Settings\Fork.json).</summary>
	public bool ShowName
	{
		get => ForkSettings.Current.KcvStyle.ExpeditionShowName;
		set
		{
			if (ForkSettings.Current.KcvStyle.ExpeditionShowName != value)
			{
				ForkSettings.Current.KcvStyle.ExpeditionShowName = value;
				ForkSettings.Current.Save();
				Update();
			}

			OnPropertyChanged();
		}
	}

	public KcvExpeditionViewModel() : base(ForkStrings.KcvExpeditionPanel, "Fork.Expedition", IconContent.FleetExpedition)
	{
		SystemEvents.UpdateTimerTick += Update;
		Update();
	}

	private void Update()
	{
		foreach (KcvExpeditionLine line in Lines)
		{
			line.Update();
		}
	}
}

public enum KcvExpeditionLineState
{
	/// <summary>No fleet data yet (before the first port).</summary>
	Unknown,
	Locked,
	Idle,
	Expedition,
}

public sealed class KcvExpeditionLine(int fleetId) : ObservableObject
{
	public int FleetId { get; } = fleetId;

	public KcvExpeditionLineState State { get; private set; }

	/// <summary>The expedition's number and name, or 待機中 / 艦隊未開放.</summary>
	public string Name { get; private set; } = "";

	/// <summary>The expedition's number and name in full, always (the name may be trimmed in a narrow pane).</summary>
	public string? NameToolTip { get; private set; }

	public string TimeText { get; private set; } = "";

	public string? TimeToolTip { get; private set; }

	public void Update()
	{
		FleetManager fleets = KCDatabase.Instance.Fleet;
		FleetData? fleet = fleets[FleetId];

		if (fleet is null)
		{
			// only the open fleets are in the list, once a port answer came
			State = fleets.Fleets.Count > 0 ? KcvExpeditionLineState.Locked : KcvExpeditionLineState.Unknown;
			Name = State is KcvExpeditionLineState.Locked ? ForkStrings.KcvFleetLocked : "";
			Clear();
			return;
		}

		if (fleet.ExpeditionState is ExpeditionState.NotDeployed)
		{
			State = KcvExpeditionLineState.Idle;
			Name = ForkStrings.KcvFleetIdle;
			Clear();
			return;
		}

		IMissionData? mission = KCDatabase.Instance.Mission[fleet.ExpeditionDestination];
		State = KcvExpeditionLineState.Expedition;
		// with the expedition number as the game shows it ([02] 長距離練習航海, user's choice 2026-10-02); 원정명을 표시 off:
		// the number only, the name in the tooltip (mockup 20261006-timer-panels-v2)
		NameToolTip = mission is null ? null : $"[{mission.DisplayID}] {mission.NameEN}";
		Name = mission is null ? "" : ForkSettings.Current.KcvStyle.ExpeditionShowName ? NameToolTip! : $"[{mission.DisplayID}]";
		TimeText = TimeLeft(fleet.ExpeditionTime);
		TimeToolTip = ForkStrings.KcvReturnTime(DateTimeHelper.TimeToCSVString(fleet.ExpeditionTime));
	}

	private void Clear()
	{
		NameToolTip = null;
		TimeText = "";
		TimeToolTip = null;
	}

	/// <summary>Hours past 24 as they are (a long expedition reads 25:10:00); 00:00:00 once it is back.</summary>
	private static string TimeLeft(DateTime end)
	{
		TimeSpan left = end - DateTime.Now;
		if (left < TimeSpan.Zero) left = TimeSpan.Zero;
		return $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
	}
}
