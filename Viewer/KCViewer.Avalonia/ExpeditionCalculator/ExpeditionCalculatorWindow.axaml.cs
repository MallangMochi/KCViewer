using Avalonia.Controls;

namespace KCViewer.Avalonia.ExpeditionCalculator;

public partial class ExpeditionCalculatorWindow : Window
{
	public ExpeditionCalculatorWindow(ExpeditionCalculatorViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
	}
}
