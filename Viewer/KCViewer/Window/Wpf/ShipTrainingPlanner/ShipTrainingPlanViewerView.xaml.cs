using System.Windows;
using System.Windows.Controls;

namespace KCViewer.Window.Wpf.ShipTrainingPlanner;
/// <summary>
/// Interaction logic for ShipTrainingPlanViewerView.xaml
/// </summary>
public partial class ShipTrainingPlanViewerView : UserControl
{
	public ShipTrainingPlanViewerViewModel ViewModel
	{
		get => (ShipTrainingPlanViewerViewModel)GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register
	(
		nameof(ViewModel),
		typeof(ShipTrainingPlanViewerViewModel),
		typeof(ShipTrainingPlanViewerView),
		new PropertyMetadata(default(ShipTrainingPlanViewerViewModel))
	);

	public ShipTrainingPlanViewerView()
	{
		InitializeComponent();
	}
}
