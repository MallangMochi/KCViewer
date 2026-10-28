using KCViewer.ViewModels;
using WpfWindow = System.Windows.Window;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the item details window (see the XAML). One window: opening it again brings it to the front. In the tools
/// menu whether the KanColleViewer style is on or not, as MapHP.
/// </summary>
public partial class KcvItemDetailWindow
{
	private static KcvItemDetailWindow? Current { get; set; }

	public KcvItemDetailWindow() : base(new KcvItemDetailViewModel())
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

		Current = new KcvItemDetailWindow();
		Current.Show(owner);
	}
}
