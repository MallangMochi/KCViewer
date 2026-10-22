using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KCViewer.Window.Tools.DropRecordViewer;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the drop record in KanColleViewer's style (see the XAML), from the tools menu while the style is on.</summary>
public partial class KcvDropRecordWindow
{
	private KcvConditionSnapshot Conditions { get; }

	public KcvDropRecordWindow() : base(new DropRecordViewerViewModel())
	{
		InitializeComponent();

		// the sea area and map reset the node, so the node comes after them
		Conditions = new(ViewModel,
			nameof(ViewModel.ShipSearchOption), nameof(ViewModel.ItemSearchOption), nameof(ViewModel.ShipTypeSearchOption),
			nameof(ViewModel.DateBegin), nameof(ViewModel.TimeBegin), nameof(ViewModel.DateEnd), nameof(ViewModel.TimeEnd),
			nameof(ViewModel.MapAreaID), nameof(ViewModel.MapInfoID), nameof(ViewModel.MapCellID), nameof(ViewModel.MapDifficulty),
			nameof(ViewModel.IsBossOnly), nameof(ViewModel.RankS), nameof(ViewModel.RankA), nameof(ViewModel.RankB), nameof(ViewModel.RankX),
			$"{nameof(ViewModel.DropRecordFilter)}.{nameof(DropRecordFilterViewModel.IgnoreCommonDrops)}", nameof(ViewModel.MergeRows));
		KcvRecordWindows.OnceLoaded(this, Conditions.Capture);

		ViewModel.PropertyChanged += ShowStatus;
		ViewModel.PropertyChanged += SearchAgain;
	}

	private bool SearchPending { get; set; }

	/// <summary>합치기 searches again at once ("합치기 시 즉시 화면에서 갱신 필요", 2026-10-05); the viewer only empties the table.</summary>
	private void SearchAgain(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(ViewModel.MergeRows) || SearchPending) return;

		SearchPending = true;
		Dispatcher.BeginInvoke(() =>
		{
			SearchPending = false;
			if (!ViewModel.SearchCommand.IsRunning) ViewModel.SearchCommand.Execute(null);
		});
	}

	/// <summary>The picker keeps its conditions while the record window is open, as the viewer's picker.</summary>
	private KcvShipPickerViewModel? ShipPicker { get; set; }

	/// <summary>The ship button opens the picker in this style (mockup v5 ③-1) instead of the viewer's.</summary>
	private void OpenShipPicker(object sender, RoutedEventArgs e)
	{
		ShipPicker ??= new();
		ShipPicker.Result = null;

		if (new KcvShipPickerWindow(ShipPicker) { Owner = this }.ShowDialog() is not true) return;

		ViewModel.ShipSearchOption = ShipPicker.Result ?? DropRecordOption.All;
	}

	private void ResetConditions(object sender, RoutedEventArgs e) => Conditions.Restore();

	private void ShowSelection(object sender, SelectionChangedEventArgs e)
	{
		if (sender is not DataGrid table) return;

		if (table.SelectedItems.Count is 0) SelectionText.Text = "";
		ViewModel.SelectedRows = table.SelectedItems.Cast<DropRecordRowBase>().ToList();
	}

	private void ShowStatus(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(ViewModel.StatusInfoText)) return;

		if (KcvRecordWindows.IsSelectionText(ViewModel.StatusInfoText, ViewModel.DialogDropRecordViewer.SelectedItems))
		{
			SelectionText.Text = ViewModel.StatusInfoText;
		}
		else
		{
			StatusText.Text = ViewModel.StatusInfoText;
		}
	}
}
