using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Resource;
using KCViewer.Utility;
using KCViewer.ViewModels;
using KCViewer.Window.Control;
using KCViewer.Window.Wpf.Fleet;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the combined fleet panel (mockup 20261009-combined-fleet-panel-v3, approved 2026-10-09 "구현"): fleets 1 and 2
/// side by side in a fleet panel's size under one header (연합함대, the kind, 제공 · 색적 · TP · 속력, [함대 상세]) and
/// the worse fleet's status bar. The rows are the fleet panels' own (FleetViewModel.ControlMember), so their tooltips,
/// menus, heavy damage and docking follow; the values are the fleet overview's combined card's (same calculation, same
/// tooltip, the 색적 click cycles both). Closed at first; a KanColleViewer style panel only: closed when the style
/// is turned off, and its menu item shows only while the style is on.
/// </summary>
public sealed partial class KcvCombinedFleetViewModel : AnchorableViewModel
{
	public FleetViewModel Fleet1 { get; }
	public FleetViewModel Fleet2 { get; }

	/// <summary>The fleet overview's combined fleet card: 제공, TP, 색적 and its tooltip.</summary>
	public KcvCombinedCardViewModel Card { get; }

	public bool IsCombined { get; private set; }
	public string KindText { get; private set; } = "";
	public string SpeedText { get; private set; } = "";
	public string SpeedToolTip { get; private set; } = "";

	/// <summary>The fleet whose state is worse (a lower FleetState comes first), for the status bar and the mark.</summary>
	public FleetViewModel WorseFleet { get; private set; }

	public string NotFormedText => ForkStrings.KcvCombinedNotFormed;

	public KcvCombinedFleetViewModel(FleetViewModel fleet1, FleetViewModel fleet2, KcvCombinedCardViewModel card)
		: base(ForkStrings.KcvCombinedFleet, "CombinedFleet", null)
	{
		Fleet1 = fleet1;
		Fleet2 = fleet2;
		Card = card;
		WorseFleet = fleet1;

		SystemEvents.UpdateTimerTick += Refresh;
		Configuration.Instance.ConfigurationChanged += FollowStyle;
		Refresh();
	}

	/// <summary>The fleet detail window on its combined fleet screen.</summary>
	[RelayCommand]
	private void OpenDetail()
	{
		if (System.Windows.Application.Current?.MainWindow is { } owner) KcvFleetDetailWindow.Open(0, owner);
	}

	private void Refresh()
	{
		// also when the saved layout opens it while the style is off
		FollowStyle();

		KCDatabase db = KCDatabase.Instance;
		FleetType kind = db.Fleet.CombinedFlag;

		IsCombined = kind != FleetType.Single && db.Fleet[1] is not null && db.Fleet[2] is not null;
		KindText = IsCombined ? Constants.GetCombinedFleet(kind) : "";

		if (!IsCombined)
		{
			Icon = null;
			return;
		}

		FleetState State(FleetViewModel fleet) => fleet.ControlFleet.State.StateLabels.FirstOrDefault()?.State ?? FleetState.NoShip;
		WorseFleet = (int)State(Fleet2) < (int)State(Fleet1) ? Fleet2 : Fleet1;
		Icon = WorseFleet.Icon;

		// the slowest ship of both fleets, as a fleet panel's 속력
		List<IShipData> ships = new[] { db.Fleet[1], db.Fleet[2] }
			.SelectMany(fleet => fleet.MembersInstance?.OfType<IShipData>() ?? Enumerable.Empty<IShipData>())
			.ToList();
		SpeedText = Constants.GetSpeed(ships.Select(ship => ship.Speed).DefaultIfEmpty(20).Min());
		SpeedToolTip = string.Join("\r\n", ships.Select(ship => $"{ship.Name}：{Constants.GetSpeed(ship.Speed)}"));
	}

	// A KanColleViewer style panel: turning the style off closes it (its menu item hides then).
	private void FollowStyle()
	{
		if (!ForkSettings.Current.KcvStyle.Enabled && Visibility == Visibility.Visible) Visibility = Visibility.Collapsed;
	}
}
