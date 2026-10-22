using KCViewer.Window.Tools.AirDefense;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: 대공 포화 상세 in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/> while the style is on.</summary>
public partial class KcvAirDefenseWindow
{
	public KcvAirDefenseWindow(AirDefenseViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}
