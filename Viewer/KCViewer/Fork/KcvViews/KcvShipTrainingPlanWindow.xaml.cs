using System.Windows;
using KCViewer.Window.Wpf.ShipTrainingPlanner;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the ship training plan editor in KanColleViewer's style (see the XAML), opened through
/// <see cref="KcvCalcWindows.TryShowShipTrainingPlan"/>: 「계획 추가」 after the ship picker, 「계획 편집」 from the panel.
/// </summary>
public partial class KcvShipTrainingPlanWindow
{
	public KcvShipTrainingPlanWindow(ShipTrainingPlanViewModel viewModel, bool adding) : base(viewModel)
	{
		InitializeComponent();

		Title = adding ? ForkStrings.KcvTrainingPlanAdd : ForkStrings.KcvTrainingPlanEdit;
	}

	private void OnConfirmClick(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		DialogResult = false;
	}
}
