using System.Windows;
using System.Windows.Controls;
using KCViewer.Window.Tools.ExpeditionRecordViewer;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the expedition record in KanColleViewer's style (see the XAML), from the tools menu while the style is on.</summary>
public partial class KcvExpeditionRecordWindow
{
	private KcvConditionSnapshot Conditions { get; }

	public KcvExpeditionRecordWindow() : base(new ExpeditionRecordViewerViewModel())
	{
		InitializeComponent();

		// the sea area first: it makes the expedition list again
		Conditions = new(ViewModel,
			nameof(ViewModel.DateBegin), nameof(ViewModel.TimeBegin), nameof(ViewModel.DateEnd), nameof(ViewModel.TimeEnd),
			nameof(ViewModel.World), nameof(ViewModel.Mission));
		KcvRecordWindows.OnceLoaded(this, Conditions.Capture);

		// the fleet column was as wide as the window let it (star) until 2026-10-05
		KcvRecordWindows.ReplaceOldDefaultWidth(this, FleetColumn, new(1, DataGridLengthUnitType.Star));
	}

	private void ResetConditions(object sender, RoutedEventArgs e) => Conditions.Restore();
}
