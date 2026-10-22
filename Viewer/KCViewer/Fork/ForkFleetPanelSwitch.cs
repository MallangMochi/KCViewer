using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AvalonDock;
using AvalonDock.Layout;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Observer;
using KCViewer.ViewModels;
using KCViewer.Window.Wpf.Fleet;

namespace KCViewer.Fork;

/// <summary>
/// Fork: when a fleet's composition changes (api_req_hensei/change: a ship put in, taken out or swapped, or all but the
/// flagship taken out) or a preset is loaded into it (api_req_hensei/preset_select), its fleet panel comes to the front
/// of its pane, e.g. #2 in front of #1 in the same pane (setting 편성 변경 시 패널 자동 전환, on by default, mockup
/// 20261009-fleet-dock-badge-v5). Only a docked panel: a collapsed (auto-hidden) one, one in a floating window and a closed
/// one stay as they are. Only the tab is selected, the keyboard focus stays in the game.
/// The combined fleet panel (mockup 20261009-combined-fleet-panel-v3, user's decision 2026-10-09 "연합함대 패널로"):
/// forming a combined fleet (api_req_hensei/combined) brings it to the front, and while fleets 1 and 2 are combined their
/// changes go to it; when it is not docked there, fleets 1 and 2 go to their own panels as before.
/// </summary>
public static class ForkFleetPanelSwitch
{
	private static AnchorableViewModel? CombinedPanel { get; set; }

	public static void SetCombinedPanel(AnchorableViewModel panel) => CombinedPanel = panel;

	public static void Attach(DockingManager manager, IReadOnlyList<FleetViewModel> fleets)
	{
		APIObserver observer = APIObserver.Instance;

		observer.ApiReqHensei_Change.RequestReceived += (_, data) =>
		{
			if (data is Dictionary<string, string> request && request.TryGetValue("api_id", out string? id) && int.TryParse(id, out int fleetId))
			{
				SwitchFleet(manager, fleets, fleetId);
			}
		};
		observer.ApiReqHensei_PresetSelect.ResponseReceived += (_, data) =>
		{
			if (data is null || !data.api_id()) return;

			SwitchFleet(manager, fleets, (int)data!.api_id);
		};
		observer.ApiReqHensei_Combined.RequestReceived += (_, data) =>
		{
			// 0 breaks the combined fleet up
			if (data is Dictionary<string, string> request && request.TryGetValue("api_combined_type", out string? type) && type != "0")
			{
				TrySwitch(manager, CombinedPanel);
			}
		};
	}

	private static void SwitchFleet(DockingManager manager, IReadOnlyList<FleetViewModel> fleets, int fleetId)
	{
		bool combined = KCDatabase.Instance.Fleet.CombinedFlag != FleetType.Single && fleetId is 1 or 2;
		if (combined && TrySwitch(manager, CombinedPanel)) return;

		TrySwitch(manager, fleets.FirstOrDefault(fleet => fleet.FleetId == fleetId));
	}

	/// <summary>Brings a docked panel to the front of its pane; false when it is not docked there (or the setting is off).</summary>
	private static bool TrySwitch(DockingManager manager, AnchorableViewModel? panel)
	{
		if (!ForkSettings.Current.FleetPanel.SwitchOnComposition) return false;
		if (panel is not { Visibility: Visibility.Visible }) return false;

		// the layout of now: loading a layout replaces it
		LayoutAnchorable? anchorable = manager.Layout?.Descendents().OfType<LayoutAnchorable>()
			.FirstOrDefault(item => ReferenceEquals(item.Content, panel));

		if (anchorable is not { IsHidden: false, IsAutoHidden: false, IsFloating: false }) return false;
		if (anchorable.Parent is not LayoutAnchorablePane) return false;

		anchorable.IsSelected = true;
		return true;
	}
}
