using System;
using KCViewer.Core;
using KCViewer.Core.Services;
using KCViewer.Data;
using KCViewer.Resource;
using KCViewer.Window.Wpf.FleetOverview;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the anchorage repair and home port supply timers in the fleet panel's header, left of the air power (mockup
/// 20261005-fleet-timers-v2, user decisions 2026-10-05). The anchorage repair timer while the flagship is a repair ship
/// (FleetData.IsFlagshipRepairShip), the home port supply timer while 野埼 is the flagship or the 2nd ship
/// (HomePortSupplyService.IsHomePortSupplyFleet), both one after the other. Shown whatever the fleet overview's context
/// menu says (user decision). The same single timers, icons and tooltips as the fleet overview's items.
/// KcvFleetView refreshes it every second while it is loaded.
/// </summary>
public sealed class KcvFleetTimersViewModel
{
	public FleetOverviewItemViewModel Repair { get; } = new()
	{
		Text = "-",
		Icon = IconContent.FleetAnchorageRepairing,
	};

	public FleetOverviewItemViewModel Supply { get; } = new()
	{
		Text = "-",
		Icon = IconContent.ConditionSparkle,
	};

	public void Refresh(int fleetId)
	{
		FleetData? fleet = KCDatabase.Instance.Fleet[fleetId];

		Repair.Visible = fleet?.IsFlagshipRepairShip == true;
		Supply.Visible = fleet is not null && HomePortSupplyService.IsHomePortSupplyFleet(fleet);

		if (Repair.Visible)
		{
			Set(Repair, KCDatabase.Instance.Fleet.AnchorageRepairingTimer, 20, FleetOverviewResources.AnchorageRepairToolTip);
		}

		if (Supply.Visible)
		{
			Set(Supply, KCDatabase.Instance.HomePortSupplyService.HomePortSupplyTimer, 15, FleetOverviewResources.HomePortSupplyToolTip);
		}
	}

	// As FleetOverviewViewModel.Updated: the elapsed time, and the start and recovery times in the tooltip
	private static void Set(FleetOverviewItemViewModel item, DateTime start, int minutes, string title)
	{
		if (start <= DateTime.MinValue)
		{
			item.Text = "-";
			item.ToolTip = title;
			return;
		}

		item.Text = DateTimeHelper.ToTimeElapsedString(start);
		item.ToolTip = string.Join(Environment.NewLine,
			title,
			$"{FleetOverviewResources.TimerStart}：{DateTimeHelper.TimeToCSVString(start)}",
			$"{FleetOverviewResources.Recovery}：{DateTimeHelper.TimeToCSVString(start.AddMinutes(minutes))}");
	}
}
