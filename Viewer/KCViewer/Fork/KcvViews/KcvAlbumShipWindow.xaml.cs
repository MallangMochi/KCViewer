using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using KCViewer.Core.Types;
using KCViewer.Window.Tools.DialogAlbumMasterShip;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the ship album in KanColleViewer's style (see the XAML), opened through <see cref="KcvAlbums"/> while the style is on.</summary>
public partial class KcvAlbumShipWindow
{
	public KcvAlbumShipWindow() : base(new DialogAlbumMasterShipViewModel { ForkJapaneseNameSearch = true })
	{
		InitializeComponent();

		ViewModel.PropertyChanged += ViewModelChanged;
		ViewModel.DataGridViewModel.PropertyChanged += TableChanged;
		ShowCount();
	}

	public KcvAlbumShipWindow(IShipDataMaster ship) : this()
	{
		ViewModel.ChangeShipCommand.Execute(ship);
	}

	private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ViewModel.SelectedShip):
				ScrollIntoView();
				break;

			// the search makes a new list (and table model items)
			case nameof(ViewModel.DataGridViewModel):
				ViewModel.DataGridViewModel.PropertyChanged += TableChanged;
				ShowCount();
				break;
		}
	}

	private void TableChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(ViewModel.DataGridViewModel.Items)) ShowCount();
	}

	private void ShowCount() =>
		Count.Text = ForkStrings.KcvAlbumShipCount(ViewModel.DataGridViewModel.Items.Cast<object>().Count());

	private void TableUpdated(object? sender, DataTransferEventArgs e) => ScrollIntoView();

	private void ScrollIntoView()
	{
		if (ViewModel.SelectedShip is null) return;

		Table.ScrollIntoView(ViewModel.SelectedShip);
	}

	/// <summary>A right click on a row: that ship in a new album window, as the viewer.</summary>
	private void OpenRowInNewWindow(object sender, MouseButtonEventArgs e)
	{
		if (sender is not DataGridRow { DataContext: ShipDataRecord { Ship: { } ship } }) return;

		KcvAlbums.ShowShip(ship);
	}

	/// <summary>A right click on the ship before or after the remodel: it in a new album window.</summary>
	private void OpenRemodelShip(object sender, MouseButtonEventArgs e)
	{
		if (sender is not FrameworkElement { Tag: IShipDataMaster ship }) return;

		KcvAlbums.ShowShip(ship);
	}

	/// <summary>A right click on an equipment slot: the equipment album.</summary>
	private void OpenSlotEquipment(object sender, MouseButtonEventArgs e)
	{
		if (sender is not FrameworkElement { DataContext: EquipmentSlot { Equipment: { } equipment } }) return;

		KcvAlbums.ShowEquipment(equipment.ID);
	}
}
