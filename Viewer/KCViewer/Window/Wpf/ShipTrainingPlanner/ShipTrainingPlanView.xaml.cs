using System.Windows.Threading;
using KCViewer.Common;

namespace KCViewer.Window.Wpf.ShipTrainingPlanner;
/// <summary>
/// Interaction logic for ShipTrainingPlanView.xaml
/// </summary>
public partial class ShipTrainingPlanView : WindowBase<ShipTrainingPlanViewModel>
{
	public ShipTrainingPlanView(ShipTrainingPlanViewModel vm) : base(vm)
	{
		// https://github.com/Kinnara/ModernWpf/issues/378
		SourceInitialized += (s, a) =>
		{
			Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Input);
		};

		InitializeComponent();
	}

	private void OnConfirmClick(object sender, System.Windows.RoutedEventArgs e)
	{
		DialogResult = true;
	}

	private void OnCancelClick(object sender, System.Windows.RoutedEventArgs e)
	{
		DialogResult = false;
	}
}
