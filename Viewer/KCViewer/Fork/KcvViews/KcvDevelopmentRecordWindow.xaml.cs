using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KCViewer.Window.Tools.DevelopmentRecordViewer;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the development record in KanColleViewer's style (see the XAML), from the tools menu while the style is on.</summary>
public partial class KcvDevelopmentRecordWindow
{
	private KcvConditionSnapshot Conditions { get; }

	public KcvDevelopmentRecordWindow() : base(new DevelopmentRecordViewerViewModel())
	{
		InitializeComponent();

		// the ship type first: it makes the flagship list again
		Conditions = new(ViewModel,
			nameof(ViewModel.SelectedCategory), nameof(ViewModel.SelectedEquipment), nameof(ViewModel.SelectedRecipe),
			nameof(ViewModel.SelectedFlagshipType), nameof(ViewModel.SelectedFlagship),
			nameof(ViewModel.DateBegin), nameof(ViewModel.TimeBegin), nameof(ViewModel.DateEnd), nameof(ViewModel.TimeEnd),
			nameof(ViewModel.MergeRows));
		KcvRecordWindows.OnceLoaded(this, Conditions.Capture);

		// 함종 별 횟수, or 레시피 별 횟수 once a flagship is chosen (the viewer's header)
		SummaryColumn.Header = ViewModel.SummaryHeader;
		ViewModel.ForkFollowCategory();
		ViewModel.PropertyChanged += ShowChanges;
	}

	private bool SearchPending { get; set; }

	/// <summary>합치기 searches again at once ("합치기 시 즉시 화면에서 갱신 필요", 2026-10-05); the viewer only empties the table.</summary>
	private void SearchAgain()
	{
		if (SearchPending) return;

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
		ViewModel.SelectedRows = table.SelectedItems.Cast<DevelopmentRecordRow>().ToList();
	}

	private void ShowChanges(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ViewModel.SummaryHeader):
				SummaryColumn.Header = ViewModel.SummaryHeader;
				break;

			case nameof(ViewModel.MergeRows):
				SearchAgain();
				break;

			case nameof(ViewModel.StatusInfoText) when KcvRecordWindows.IsSelectionText(ViewModel.StatusInfoText, ViewModel.DialogDevelopmentRecordViewer.SelectedItems):
				SelectionText.Text = ViewModel.StatusInfoText;
				break;

			case nameof(ViewModel.StatusInfoText):
				StatusText.Text = ViewModel.StatusInfoText;
				break;
		}
	}
}
