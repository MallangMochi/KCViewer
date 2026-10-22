using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KCViewer.Window.Tools.SenkaViewer;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the senka viewer in KanColleViewer's style (see the XAML), from the tools menu while the style is on.</summary>
public partial class KcvSenkaViewerWindow
{
	private KcvConditionSnapshot Conditions { get; }

	public KcvSenkaViewerWindow() : base(new SenkaViewerViewModel())
	{
		InitializeComponent();

		Conditions = new(ViewModel,
			nameof(ViewModel.DateBegin), nameof(ViewModel.TimeBegin), nameof(ViewModel.DateEnd), nameof(ViewModel.TimeEnd));
		KcvRecordWindows.OnceLoaded(this, Conditions.Capture);
		KcvRecordWindows.ReplaceOldDefaultWidth(this, TimeColumn, new(200));

		ViewModel.PropertyChanged += ShowCounts;
		ShowCounts(null, new PropertyChangedEventArgs(nameof(ViewModel.ForkSessionCount)));
	}

	private void ResetConditions(object sender, RoutedEventArgs e) => Conditions.Restore();

	private void ShowSelection(object sender, SelectionChangedEventArgs e)
	{
		if (sender is not DataGrid table) return;

		ViewModel.SelectedSenkaRecords = table.SelectedItems.Cast<SenkaRecord>().ToList();
		ViewModel.ForkRefreshTotals();
	}

	/// <summary>Whose sum the cards show, and the number of sessions on the right of the status bar.</summary>
	private void ShowCounts(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(ViewModel.ForkSessionCount)) return;

		int sessions = ViewModel.ForkSessionCount;
		int selected = ViewModel.SelectedSenkaRecords.Count;

		TotalsNote.Text = selected > 0 ? ForkStrings.KcvSenkaTotalOfSelected(selected) : ForkStrings.KcvSenkaTotalOfAll(sessions);
		SessionsText.Text = ForkStrings.KcvSenkaSessions(sessions);
	}
}
