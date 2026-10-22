using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KCViewer.Window.Tools.ConstructionRecordViewer;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the construction record in KanColleViewer's style (see the XAML), from the tools menu while the style is on.</summary>
public partial class KcvConstructionRecordWindow
{
	private KcvConditionSnapshot Conditions { get; }

	public KcvConstructionRecordWindow() : base(new ConstructionRecordViewerViewModel())
	{
		InitializeComponent();

		// the ship type first: it makes the flagship list again
		Conditions = new(ViewModel,
			nameof(ViewModel.SelectedShipType), nameof(ViewModel.SelectedShip), nameof(ViewModel.SelectedRecipe),
			nameof(ViewModel.SelectedFlagshipType), nameof(ViewModel.SelectedFlagship), nameof(ViewModel.SelectedDevelopmentMaterial),
			nameof(ViewModel.DateBegin), nameof(ViewModel.TimeBegin), nameof(ViewModel.DateEnd), nameof(ViewModel.TimeEnd),
			nameof(ViewModel.IsLargeConstruction), nameof(ViewModel.MergeRows));
		KcvRecordWindows.OnceLoaded(this, Conditions.Capture);

		ViewModel.ForkFollowCategory();
		ViewModel.PropertyChanged += ShowStatus;
		ViewModel.PropertyChanged += SearchAgain;
	}

	private bool SearchPending { get; set; }

	/// <summary>
	/// 합치기, and the ship while merged (it switches between the tables by ship and by recipe), search again at once
	/// ("합치기 시 즉시 화면에서 갱신 필요", 2026-10-05): the viewer only empties the table.
	/// </summary>
	private void SearchAgain(object? sender, PropertyChangedEventArgs e)
	{
		bool again = e.PropertyName switch
		{
			nameof(ViewModel.MergeRows) => true,
			nameof(ViewModel.SelectedShip) => ViewModel.MergeRows,
			_ => false,
		};
		if (!again || SearchPending) return;

		SearchPending = true;
		Dispatcher.BeginInvoke(() =>
		{
			SearchPending = false;
			ViewModel.ForkSearchCommand.Execute(null);
		});
	}

	private void ResetConditions(object sender, RoutedEventArgs e) => Conditions.Restore();

	private void ShowSelection(object sender, SelectionChangedEventArgs e)
	{
		if (sender is not DataGrid table) return;

		if (table.SelectedItems.Count is 0) SelectionText.Text = "";
		ViewModel.SelectedRows = table.SelectedItems.Cast<ConstructionRecordRow>().ToList();
	}

	private void ShowStatus(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(ViewModel.StatusInfoText)) return;

		// the viewer counts the selection with the drop record's text
		if (KcvRecordWindows.IsSelectionText(ViewModel.StatusInfoText, Translations.DropRecordViewerResources.SelectedItems))
		{
			SelectionText.Text = ViewModel.StatusInfoText;
		}
		else
		{
			StatusText.Text = ViewModel.StatusInfoText;
		}
	}
}
