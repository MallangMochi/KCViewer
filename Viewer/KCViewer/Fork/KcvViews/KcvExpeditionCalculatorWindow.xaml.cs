using KCViewer.Avalonia.ExpeditionCalculator;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: 원정 계산기 in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/> while the style is on.</summary>
public partial class KcvExpeditionCalculatorWindow
{
	public KcvExpeditionCalculatorWindow(ExpeditionCalculatorViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}
