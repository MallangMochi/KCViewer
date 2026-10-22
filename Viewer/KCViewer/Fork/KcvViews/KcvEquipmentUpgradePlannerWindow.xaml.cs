using KCViewer.Window.Tools.EquipmentUpgradePlanner;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the upgrade planner in KanColleViewer's style (see the XAML). Opened instead of the viewer's window while
/// the style is on, with the viewer's view model.
/// </summary>
public partial class KcvEquipmentUpgradePlannerWindow
{
	public KcvEquipmentUpgradePlannerWindow(EquipmentUpgradePlannerViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}
