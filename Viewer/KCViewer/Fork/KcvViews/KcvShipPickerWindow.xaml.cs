using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the drop record's ship picker in KanColleViewer's style (see the XAML).</summary>
public partial class KcvShipPickerWindow
{
	public KcvShipPickerWindow(KcvShipPickerViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		Loaded += (_, _) => NameBox.Focus();
	}

	private void Pick(object? result)
	{
		if (result is null) return;

		ViewModel.Result = result;
		DialogResult = true;
	}

	private void PickOption(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: DropRecordOptionChoice choice }) Pick(choice.Option);
	}

	private void PickRow(object sender, MouseButtonEventArgs e)
	{
		if (sender is DataGridRow { Item: KcvPickerShip ship }) Pick(ship.Ship);
	}

	private void PickSelected(object sender, KeyEventArgs e)
	{
		if (e.Key is not Key.Enter || Table.SelectedItem is not KcvPickerShip ship) return;

		e.Handled = true;
		Pick(ship.Ship);
	}
}
