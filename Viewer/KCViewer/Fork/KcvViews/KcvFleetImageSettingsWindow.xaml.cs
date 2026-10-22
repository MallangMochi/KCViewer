using KCViewer.Window.Tools.FleetImageGenerator;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the settings window of <see cref="KcvFleetImageGeneratorWindow"/>.</summary>
public partial class KcvFleetImageSettingsWindow
{
	public KcvFleetImageSettingsWindow(FleetImageGeneratorViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}
