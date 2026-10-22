using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using KCViewer.Core.Services.Data;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;
using KCViewer.Data;
using KCViewer.Fork;
using KCViewer.Fork.KcvViews;
using KCViewer.Resource;
using KCViewer.Utility;
using KCViewer.Utility.Data;
using KCViewer.Window.Control;

namespace KCViewer.Window.Wpf.FleetOverview;

/// <summary>
/// Fork: the cards of the KanColleViewer fleet overview (Fork/KcvViews/KcvFleetOverviewView.xaml, mockup v9, user
/// decisions 2026-10-01): the combined fleet card first while the fleets are combined, a one-line card per fleet,
/// then the two timers on one card. Laid out side by side (wrapping) or one under another, chosen from the panel's
/// context menu and kept in Settings\Fork.json. Called at the end of the constructor and of <see cref="Updated"/>,
/// and every second.
/// </summary>
public partial class FleetOverviewViewModel
{
	public ObservableCollection<object> ForkCards { get; } = [];

	public bool ForkVertical { get; private set; } = ForkSettings.Current.KcvStyle.FleetOverviewVertical;

	// The two checkable items of the context menu (as the accent items of the View menu): checking one picks it
	public bool ForkLayoutHorizontal { get => !ForkVertical; set => ForkSetVertical(false, value); }
	public bool ForkLayoutVertical { get => ForkVertical; set => ForkSetVertical(true, value); }

	// The two timer items of the context menu (user request 2026-10-05, mockup 20261005-fleet-timers-v2): the viewer's
	// own settings (settings window, fleet tab), so either place changes both. Saved with the viewer's configuration.
	// The fleet panels' header timers do not follow them (user decision).
	public bool ForkShowAnchorageRepairTimer
	{
		get => Configuration.Config.FormFleet.ShowAnchorageRepairingTimer;
		set => ForkSetTimers(value, Configuration.Config.FormFleet.ShowHomePortSupplyTimer);
	}

	public bool ForkShowHomePortSupplyTimer
	{
		get => Configuration.Config.FormFleet.ShowHomePortSupplyTimer;
		set => ForkSetTimers(Configuration.Config.FormFleet.ShowAnchorageRepairingTimer, value);
	}

	private List<KcvFleetCardViewModel> ForkFleetCards { get; set; } = [];
	private KcvCombinedCardViewModel ForkCombined { get; } = new();

	/// <summary>The combined fleet's values, also shown by the combined fleet panel (KcvCombinedFleetViewModel).</summary>
	public KcvCombinedCardViewModel ForkCombinedCard => ForkCombined;
	private KcvTimerCardViewModel? ForkTimers { get; set; }

	private void ForkInitialize()
	{
		ForkFleetCards = Fleets.Select(fleet => new KcvFleetCardViewModel(fleet)).ToList();
		ForkTimers = new(AnchorageRepairingTimer, HomePortSupplyTimer);

		Utility.SystemEvents.UpdateTimerTick += ForkRefresh;
		Configuration.Instance.ConfigurationChanged += ForkTimersChanged;
		ForkRefresh();
	}

	private void ForkSetTimers(bool repair, bool supply)
	{
		Configuration.Config.FormFleet.ShowAnchorageRepairingTimer = repair;
		Configuration.Config.FormFleet.ShowHomePortSupplyTimer = supply;

		// The viewer's own handler (the items' Visible), without the whole configuration change
		ConfigurationChanged();
		ForkTimersChanged();
	}

	// Also after the settings window: the menu's check marks and the timer card
	private void ForkTimersChanged()
	{
		OnPropertyChanged(nameof(ForkShowAnchorageRepairTimer));
		OnPropertyChanged(nameof(ForkShowHomePortSupplyTimer));
		ForkRefresh();
	}

	private void ForkSetVertical(bool vertical, bool value)
	{
		if (value && ForkVertical != vertical)
		{
			ForkVertical = vertical;
			ForkSettings.Current.KcvStyle.FleetOverviewVertical = vertical;
			ForkSettings.Current.Save();
		}

		// A click on the checked item unchecks it in the menu: check it again
		OnPropertyChanged(nameof(ForkLayoutHorizontal));
		OnPropertyChanged(nameof(ForkLayoutVertical));
	}

	private void ForkUpdated()
	{
		if (KCDatabase.Instance.Fleet.CombinedFlag > 0) ForkUpdateCombined();
		ForkRefresh();
	}

	private void ForkRefresh()
	{
		foreach (KcvFleetCardViewModel card in ForkFleetCards) card.Refresh();

		if (CombinedTag.Visible && ForkFleetCards.Count >= 2)
		{
			// The worse state of the 1st and 2nd fleet (a lower FleetState comes first, as the viewer's icon does)
			ForkCombined.State = (FleetState)Math.Min((int)ForkFleetCards[0].State, (int)ForkFleetCards[1].State);
		}

		// The cards in order: the combined fleet, the fleets, the timers
		List<object> cards = [];
		if (CombinedTag.Visible) cards.Add(ForkCombined);
		cards.AddRange(ForkFleetCards);
		if (ForkTimers is not null && (AnchorageRepairingTimer.Visible || HomePortSupplyTimer.Visible)) cards.Add(ForkTimers);

		if (!cards.SequenceEqual(ForkCards))
		{
			ForkCards.Clear();
			foreach (object card in cards) ForkCards.Add(card);
		}
	}

	// The viewer's combined fleet values (Updated: CombinedTag.ToolTip), written as the fork's card and tooltip
	private void ForkUpdateCombined()
	{
		FleetData fleet1 = KCDatabase.Instance.Fleet[1];
		FleetData fleet2 = KCDatabase.Instance.Fleet[2];

		List<IShipData> members = fleet1.MembersWithoutEscaped!
			.Concat(fleet2.MembersWithoutEscaped!)
			.Where(s => s is not null)
			.ToList()!;

		List<int> transport = members.Select(s => s.AllSlotInstanceMaster.Count(eq => eq?.CategoryType == EquipmentTypes.TransportContainer)).ToList();
		List<int> landing = members.Select(s => s.AllSlotInstanceMaster.Count(eq => eq?.CategoryType == EquipmentTypes.LandingCraft || eq?.CategoryType == EquipmentTypes.SpecialAmphibiousTank)).ToList();
		List<int> radar = members.Select(s => s.AllSlotInstanceMaster.Count(eq => eq?.IsSurfaceRadar == true)).ToList();

		int tp = TpGauge.Normal.GetTp([fleet1, fleet2]);
		int air = Calculator.GetAirSuperiority(fleet1) + Calculator.GetAirSuperiority(fleet2);
		double Los(int cn) => Math.Floor(Calculator.GetSearchingAbility_New33(fleet1, cn) * 100) / 100
			+ Math.Floor(Calculator.GetSearchingAbility_New33(fleet2, cn) * 100) / 100;

		StringBuilder tip = new();
		tip.AppendLine(ForkStrings.KcvDrums(transport.Sum(), transport.Count(i => i > 0)));
		tip.AppendLine(ForkStrings.KcvLandingCraft(landing.Sum(), landing.Count(i => i > 0)));
		tip.AppendLine(ForkStrings.KcvSurfaceRadar(radar.Sum(), radar.Count(i => i > 0)));
		tip.AppendLine(ForkStrings.KcvTransportTp(tp, (int)Math.Floor(tp * 0.7)));

		// The viewer's tank TP block starts with "<LandingOperationTooltip>:"; only that name changes
		string tank = GetTankTpTooltip(fleet1, fleet2);
		if (tank.Length > 0)
		{
			string name = FleetOverviewResources.LandingOperationTooltip;
			tip.AppendLine(tank.StartsWith(name, StringComparison.Ordinal) ? ForkStrings.KcvTankTp + tank[name.Length..] : tank);
		}

		tip.AppendLine();
		tip.AppendLine(ForkStrings.KcvAirPowerTotal(air));
		tip.AppendLine(ForkStrings.KcvLosHeader);
		for (int cn = 1; cn <= 4; cn++)
		{
			tip.AppendLine(ForkStrings.KcvLosLine(cn, Los(cn)));
		}

		ForkCombined.Title = CombinedTag.Text;
		ForkCombined.Air = air;
		ForkCombined.Tp = tp;
		ForkCombined.AirText = $"{ForkStrings.KcvAirPower} {air}";
		ForkCombined.TpText = $"TP {tp}";
		ForkCombined.ToolTip = tip.ToString().TrimEnd();
		ForkCombined.RefreshLos();
	}
}
