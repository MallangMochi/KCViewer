using KCViewer.ViewModels;
using WpfWindow = System.Windows.Window;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the land base details window (see the XAML). One window: opening it again selects the area and brings it
/// to the front. Opened from the land base panel's button and from the tools menu while the style is on.
/// </summary>
public partial class KcvBaseAirCorpsDetailWindow
{
	private static KcvBaseAirCorpsDetailWindow? Current { get; set; }

	public KcvBaseAirCorpsDetailWindow(int areaId) : base(new KcvBaseAirCorpsDetailViewModel(areaId))
	{
		InitializeComponent();
		Closed += (_, _) => Current = null;
	}

	/// <summary>areaId 0: the first area.</summary>
	public static void Open(int areaId, WpfWindow owner)
	{
		if (Current is { } window)
		{
			if (areaId > 0) window.ViewModel.SelectArea(areaId);
			window.Activate();
			return;
		}

		Current = new KcvBaseAirCorpsDetailWindow(areaId);
		Current.Show(owner);
	}
}
