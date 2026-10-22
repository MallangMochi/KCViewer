using KCViewer.ViewModels;
using WpfWindow = System.Windows.Window;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: KanColleViewer's 艦隊詳細 (see the XAML). One window: opening it again selects the fleet and brings it to
/// the front. Opened from the fleet panel's button and from the tools menu while the style is on.
/// </summary>
public partial class KcvFleetDetailWindow
{
	private static KcvFleetDetailWindow? Current { get; set; }

	public KcvFleetDetailWindow(int fleetId) : base(new KcvFleetDetailViewModel(fleetId))
	{
		InitializeComponent();
		Closed += (_, _) => Current = null;
	}

	public static void Open(int fleetId, WpfWindow owner)
	{
		if (Current is { } window)
		{
			window.ViewModel.SelectFleet(fleetId);
			window.Activate();
			return;
		}

		Current = new KcvFleetDetailWindow(fleetId);
		Current.Show(owner);
	}
}
