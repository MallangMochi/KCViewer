using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Core;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Resource;
using KCViewer.Utility.Data;
using KCViewer.ViewModels.Translations;
using KCViewer.Window.Control;
using KCViewer.Window.Wpf.Fleet;
using KCViewer.Window.Wpf.Fleet.ViewModels;
using KCViewer.Window.Wpf.FleetOverview;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: one fleet of the KanColleViewer fleet overview (mockup v9, user decisions 2026-10-01) on a single line: the
/// fleet's first state (an expedition by its name), and on the right the first timer among its states (expedition,
/// docking, anchorage repair, fatigue recovery) with the viewer's icon for it. Every state and its details are in the
/// tooltip. Read from the viewer's own fleet state (FleetStateViewModel).
/// </summary>
public sealed class KcvFleetCardViewModel : ObservableObject
{
	private static readonly FleetState[] Timed = [FleetState.Expedition, FleetState.Docking, FleetState.AnchorageRepairing, FleetState.Tired];

	private FormFleetTranslationViewModel FormFleet { get; } = Ioc.Default.GetRequiredService<FormFleetTranslationViewModel>();

	public FleetViewModel Fleet { get; }

	public string? Name { get; private set; }
	public string? NameToolTip { get; private set; }
	public FleetState State { get; private set; } = FleetState.NoShip;
	public string MainText { get; private set; } = "";
	public string? TimeText { get; private set; }
	public IconContent? TimeIcon { get; private set; }
	public bool HasTime => !string.IsNullOrEmpty(TimeText);
	public string? ToolTip { get; private set; }

	public KcvFleetCardViewModel(FleetViewModel fleet)
	{
		Fleet = fleet;
	}

	public void Refresh()
	{
		FleetStatusViewModel control = Fleet.ControlFleet;
		Name = control.Name.Text;
		NameToolTip = control.Name.ToolTip;

		List<StateLabel> labels = control.State.StateLabels.Where(label => label.Enabled).ToList();
		if (labels.Count == 0)
		{
			State = FleetState.NoShip;
			MainText = "";
			TimeText = null;
			TimeIcon = null;
			ToolTip = null;
			return;
		}

		FleetData? fleet = KCDatabase.Instance.Fleet[Fleet.FleetId];
		// The database now hands out IMissionData (upstream "add mission data interface"), which has no Detail;
		// every entry in it is a MissionData
		MissionData? mission = fleet is { ExpeditionState: not ExpeditionState.NotDeployed }
			? KCDatabase.Instance.Mission[fleet.ExpeditionDestination] as MissionData
			: null;

		StateLabel main = labels[0];
		State = main.State;
		MainText = main.State switch
		{
			FleetState.Expedition => mission?.NameEN ?? main.Text,
			FleetState.Docking => FormFleet.OnDock,
			FleetState.AnchorageRepairing => FormFleet.Repairing,
			FleetState.Tired => FormFleet.Fatigued,
			_ => main.Text,
		};

		StateLabel? timed = labels.FirstOrDefault(label => Timed.Contains(label.State));
		TimeText = timed?.ShortenedText;
		TimeIcon = timed?.Label.Icon;

		ToolTip = string.Join(Environment.NewLine + Environment.NewLine, labels.Select(label => Describe(label, mission, fleet)));
	}

	private static string Describe(StateLabel label, MissionData? mission, FleetData? fleet)
	{
		if (label.State == FleetState.Expedition && mission is not null && fleet is not null)
		{
			List<string> lines = [$"[{mission.DisplayID}] {mission.NameEN}"];
			string detail = (mission.Detail ?? "").Replace("<br>", "");
			if (detail.Length > 0) lines.Add(detail);
			lines.Add(ForkStrings.KcvReturnTime(DateTimeHelper.TimeToCSVString(fleet.ExpeditionTime)));
			return string.Join(Environment.NewLine, lines);
		}

		return string.IsNullOrEmpty(label.Label.ToolTip) ? label.Text : label.Text + Environment.NewLine + label.Label.ToolTip;
	}
}

/// <summary>
/// Fork: the combined fleet card, before the 1st fleet while the fleets are combined: the kind of combined fleet, the
/// air power, the line of sight (formula 33; a click cycles Cn 1, 4, 3, 2 as the viewer's fleet panel does) and the
/// transport TP. FleetOverviewViewModel.Fork.cs fills it.
/// </summary>
public sealed class KcvCombinedCardViewModel : ObservableObject
{
	public string? Title { get; set; }
	public FleetState State { get; set; } = FleetState.Ready;
	public string? AirText { get; set; }
	public string? TpText { get; set; }
	public string? ToolTip { get; set; }
	public int Cn { get; private set; } = 1;
	public string? LosText { get; private set; }

	// the values alone, for the combined fleet panel's header (its icons say what they are)
	public int Air { get; set; }
	public int Tp { get; set; }
	public string? LosValue { get; private set; }

	public IRelayCommand CycleCnCommand { get; }

	public KcvCombinedCardViewModel()
	{
		CycleCnCommand = new RelayCommand(() =>
		{
			Cn = Cn <= 1 ? 4 : Cn - 1;
			RefreshLos();
		});
	}

	public void RefreshLos()
	{
		FleetData? fleet1 = KCDatabase.Instance.Fleet[1];
		FleetData? fleet2 = KCDatabase.Instance.Fleet[2];
		if (fleet1 is null || fleet2 is null) return;

		double los = Math.Floor(Calculator.GetSearchingAbility_New33(fleet1, Cn) * 100) / 100
			+ Math.Floor(Calculator.GetSearchingAbility_New33(fleet2, Cn) * 100) / 100;

		LosValue = $"{los:f2}";
		LosText = $"{ForkStrings.KcvLoS} {LosValue}";
	}
}

/// <summary>Fork: the anchorage repair and home port supply timers on one card without a header (the viewer's own items).</summary>
public sealed class KcvTimerCardViewModel(FleetOverviewItemViewModel repair, FleetOverviewItemViewModel supply)
{
	public FleetOverviewItemViewModel Repair { get; } = repair;
	public FleetOverviewItemViewModel Supply { get; } = supply;
}
