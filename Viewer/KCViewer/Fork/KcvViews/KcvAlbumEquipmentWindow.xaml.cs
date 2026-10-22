using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Window.Tools.DialogAlbumMasterEquipment;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the equipment album in KanColleViewer's style (see the XAML), opened through <see cref="KcvAlbums"/> while
/// the style is on. Its search is the equipment list's filter (<see cref="KcvEquipmentFilterViewModel"/>: the game's
/// categories and the name) over the viewer's table; the viewer's own type filter stays all on.
/// </summary>
public partial class KcvAlbumEquipmentWindow
{
	public KcvEquipmentFilterViewModel Filters { get; } = new();

	public KcvAlbumEquipmentWindow() : base(new DialogAlbumMasterEquipmentViewModel())
	{
		// every category of the master data (the album shows all equipment, the list only what is owned)
		Filters.SetCategories(KCDatabase.Instance.MasterEquipments.Values);

		InitializeComponent();

		ViewModel.DataGridViewModel.FilterValue = Filters.Meets;
		Filters.Changed += Refilter;

		ViewModel.PropertyChanged += ViewModelChanged;
		ViewModel.DataGridViewModel.PropertyChanged += TableChanged;
		ShowCount();

		Closed += (_, _) =>
		{
			Filters.Changed -= Refilter;
			ViewModel.SelectedEquipmentViewModel?.UpgradeViewModel?.UnsubscribeFromApis();
		};
	}

	public KcvAlbumEquipmentWindow(int equipmentId) : this()
	{
		ViewModel.SelectedEquipmentModel = ViewModel.DataGridViewModel.ItemsSource
			.FirstOrDefault(e => e.ID == equipmentId);
	}

	private void Refilter()
	{
		ViewModel.DataGridViewModel.Items.Refresh();
		ShowCount();
		ScrollIntoView();
	}

	private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(ViewModel.SelectedEquipmentViewModel)) ScrollIntoView();
	}

	private void TableChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(ViewModel.DataGridViewModel.Items)) ShowCount();
	}

	private void ShowCount() =>
		Count.Text = ForkStrings.KcvAlbumEquipmentCount(ViewModel.DataGridViewModel.Items.Cast<object>().Count());

	private void TableUpdated(object? sender, DataTransferEventArgs e) => ScrollIntoView();

	private void ScrollIntoView()
	{
		if (ViewModel.SelectedEquipmentModel is null) return;

		Table.ScrollIntoView(ViewModel.SelectedEquipmentModel);
	}

	/// <summary>A right click on a row: that equipment in a new album window, as the viewer.</summary>
	private void OpenRowInNewWindow(object sender, MouseButtonEventArgs e)
	{
		if (sender is not DataGridRow { DataContext: IEquipmentDataMaster equipment }) return;

		KcvAlbums.ShowEquipment(equipment.ID);
	}

	/// <summary>A right click on a ship that comes with the equipment: the ship album.</summary>
	private void OpenInitialShip(object sender, MouseButtonEventArgs e)
	{
		if (sender is not FrameworkElement { DataContext: IShipDataMaster ship }) return;

		KcvAlbums.ShowShip(ship);
	}

	/// <summary>A right click on the equipment of a conversion: its album.</summary>
	private void OpenConvertedEquipment(object sender, MouseButtonEventArgs e)
	{
		if (sender is not FrameworkElement { Tag: IEquipmentDataMaster equipment }) return;

		KcvAlbums.ShowEquipment(equipment.ID);
	}
}
