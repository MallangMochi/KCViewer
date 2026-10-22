using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KCViewer.Window.Tools.SortieRecordViewer;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the sortie record in KanColleViewer's style (see the XAML), from the tools menu while the style is on.</summary>
public partial class KcvSortieRecordWindow
{
	private KcvSortieRoutes Routes { get; }
	private KcvConditionSnapshot Conditions { get; }
	private bool RoutesPending { get; set; }

	public KcvSortieRecordWindow() : base(new SortieRecordViewerViewModel())
	{
		InitializeComponent();

		// the CSV export's 추출 필터, 데이터 처리... and failure notice as windows of the style (KcvExportDialogs)
		ViewModel.ContentDialogService = new KcvContentDialogService(this);

		Routes = (KcvSortieRoutes)Resources["Routes"];
		Conditions = new(ViewModel,
			nameof(ViewModel.DateBegin), nameof(ViewModel.TimeBegin), nameof(ViewModel.DateEnd), nameof(ViewModel.TimeEnd),
			nameof(ViewModel.World), nameof(ViewModel.Map), nameof(ViewModel.BossNodeOnly));
		KcvRecordWindows.OnceLoaded(this, Conditions.Capture);

		// the fleet column was as wide as the window let it (star) until 2026-10-05
		KcvRecordWindows.ReplaceOldDefaultWidth(this, FleetColumn, new(1, DataGridLengthUnitType.Star));

		ViewModel.Sorties.CollectionChanged += ScheduleRoutes;
		ViewModel.SelectedSorties.CollectionChanged += ShowSelection;
		ViewModel.PropertyChanged += ShowStatus;
		Closed += (_, _) => Routes.Cancel();
	}

	private void ResetConditions(object sender, RoutedEventArgs e) => Conditions.Restore();

	/// <summary>A search clears the rows and adds them one by one; the routes are read once it is done.</summary>
	private void ScheduleRoutes(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (RoutesPending) return;

		RoutesPending = true;
		Dispatcher.BeginInvoke(() =>
		{
			RoutesPending = false;
			Routes.Load(ViewModel.Sorties.ToList());
		}, DispatcherPriority.Background);
	}

	private void ShowStatus(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(ViewModel.StatusBarText)) return;
		if (KcvRecordWindows.IsSelectionText(ViewModel.StatusBarText, ViewModel.SortieRecordViewer.SelectedItems)) return;

		StatusText.Text = ViewModel.StatusBarText;
	}

	private void ShowSelection(object? sender, NotifyCollectionChangedEventArgs e) =>
		SelectionText.Text = ViewModel.SelectedSorties.Count switch
		{
			0 => "",
			int count => string.Format(ViewModel.SortieRecordViewer.SelectedItems, count, ViewModel.Sorties.Count),
		};
}
