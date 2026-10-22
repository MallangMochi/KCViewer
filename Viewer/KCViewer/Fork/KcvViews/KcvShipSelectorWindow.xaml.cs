using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the viewer's ship selector in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/>.</summary>
public partial class KcvShipSelectorWindow
{
	public KcvShipSelectorWindow(KcvShipSelectorViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		Loaded += (_, _) => NameBox.Focus();
		Closed += (_, _) => ViewModel.Close();
	}

	private void Pick(KcvSelectorShip ship)
	{
		ViewModel.Pick(ship);
		DialogResult = true;
	}

	private void PickRow(object sender, MouseButtonEventArgs e)
	{
		if (sender is DataGridRow { Item: KcvSelectorShip ship }) Pick(ship);
	}

	private void PickSelected(object sender, KeyEventArgs e)
	{
		if (e.Key is not Key.Enter || Table.SelectedItem is not KcvSelectorShip ship) return;

		e.Handled = true;
		Pick(ship);
	}
}

/// <summary>
/// A three-state check box that goes 상관없음 (null) → 가능 (true) → 불가 (false), as its hint says; WPF's goes
/// null → false → true.
/// </summary>
public sealed class KcvThreeStateCheckBox : CheckBox
{
	public KcvThreeStateCheckBox()
	{
		IsThreeState = true;
	}

	protected override void OnToggle()
	{
		SetCurrentValue(IsCheckedProperty, IsChecked switch
		{
			null => true,
			true => false,
			false => (bool?)null,
		});
	}
}
