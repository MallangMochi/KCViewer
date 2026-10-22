using System.Windows.Controls;
using System.Windows.Input;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the viewer's equipment selector in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/>.</summary>
public partial class KcvEquipmentSelectorWindow
{
	public KcvEquipmentSelectorWindow(KcvEquipmentSelectorViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		Loaded += (_, _) => NameBox.Focus();
		Closed += (_, _) => ViewModel.Close();
	}

	private void Pick(KcvSelectorEquipment row)
	{
		ViewModel.Pick(row);
		DialogResult = true;
	}

	private void PickRow(object sender, MouseButtonEventArgs e)
	{
		if (sender is DataGridRow { Item: KcvSelectorEquipment row }) Pick(row);
	}

	private void PickSelected(object sender, KeyEventArgs e)
	{
		if (e.Key is not Key.Enter || Table.SelectedItem is not KcvSelectorEquipment row) return;

		e.Handled = true;
		Pick(row);
	}
}
