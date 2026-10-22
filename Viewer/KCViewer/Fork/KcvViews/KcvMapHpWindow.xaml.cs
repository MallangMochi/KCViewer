using KCViewer.ViewModels;
using WpfWindow = System.Windows.Window;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the MapHP window (see the XAML). One window: opening it again brings it to the front. In the tools menu
/// whether the KanColleViewer style is on or not.
/// </summary>
public partial class KcvMapHpWindow
{
	private static KcvMapHpWindow? Current { get; set; }

	public KcvMapHpWindow() : base(new KcvMapHpViewModel())
	{
		InitializeComponent();
		Closed += (_, _) => Current = null;
	}

	public static void Open(WpfWindow owner)
	{
		if (Current is { } window)
		{
			window.Activate();
			return;
		}

		Current = new KcvMapHpWindow();
		Current.Show(owner);
	}
}
