using System.Collections.Generic;
using KCViewer.Window.Wpf.Fleet.ViewModels;

namespace KCViewer.Window.Wpf.FleetOverview;

public class CombinedFleetOverviewItemViewModel : FleetOverviewItemViewModel
{
	public List<TotalRate> SmokeGeneratorRates { get; set; } = [];
}
