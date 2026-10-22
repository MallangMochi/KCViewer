using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Jot;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: what the record windows in KanColleViewer's style share in code (KcvRecordStyles.xaml has the looks).</summary>
public static class KcvRecordWindows
{
	/// <summary>Their column settings, kept apart from the viewer's windows (the columns differ).</summary>
	public static void ConfigureTracker(Tracker tracker)
	{
		tracker
			.Configure<KcvSortieRecordWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<KcvExpeditionRecordWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<KcvDropRecordWindow>()
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.SortDescriptions)
			.Property(w => w.ViewModel.DataGridMergedRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridMergedRowsViewModel.SortDescriptions);

		tracker
			.Configure<KcvDevelopmentRecordWindow>()
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.SortDescriptions)
			.Property(w => w.ViewModel.DataGridMergedRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridMergedRowsViewModel.SortDescriptions);

		tracker
			.Configure<KcvConstructionRecordWindow>()
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.SortDescriptions)
			.Property(w => w.ViewModel.DataGridMergedRowsAllViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridMergedRowsAllViewModel.SortDescriptions)
			.Property(w => w.ViewModel.DataGridMergedRowsFilteredByShipViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridMergedRowsFilteredByShipViewModel.SortDescriptions);

		tracker
			.Configure<KcvSenkaViewerWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		// the albums (KcvAlbums.cs)
		tracker
			.Configure<KcvAlbumShipWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<KcvAlbumEquipmentWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		// the calculators (KcvCalcWindows.cs)
		tracker
			.Configure<KcvAirDefenseWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<KcvExpCheckerWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		// the event lock planner keeps its view menu's checks as the viewer's window does
		tracker
			.Configure<KcvEventLockPlannerWindow>()
			.Property(w => w.ViewModel.ShowFinishedPhases)
			.Property(w => w.ViewModel.DisplayToolTips)
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);
	}

	/// <summary>
	/// The viewer's windows write the selection count into their one status text; these windows show it on the right
	/// of the status bar and keep the search state on the left. A text that starts like the selection format is the
	/// selection's.
	/// </summary>
	public static bool IsSelectionText(string? text, string selectionFormat)
	{
		if (string.IsNullOrEmpty(text)) return false;

		int placeholder = selectionFormat.IndexOf('{');
		string start = placeholder > 0 ? selectionFormat[..placeholder] : selectionFormat;
		return start.Length > 0 && text.StartsWith(start, StringComparison.Ordinal);
	}

	/// <summary>
	/// For a column whose default width changed. The column settings keep every column's width from the first time the
	/// window was opened and put it back when the table loads, so a width still at <paramref name="oldDefault"/> gets
	/// the column's new default from the XAML instead. A width the user dragged stays.
	/// </summary>
	public static void ReplaceOldDefaultWidth(FrameworkElement window, DataGridColumn column, DataGridLength oldDefault)
	{
		DataGridLength newDefault = column.Width;

		OnceLoaded(window, () =>
		{
			if (column.Width.UnitType == oldDefault.UnitType && column.Width.Value.Equals(oldDefault.Value))
			{
				column.Width = newDefault;
			}
		});
	}

	/// <summary>Runs once the window's view model has set its first values (WindowBase calls its Loaded first).</summary>
	public static void OnceLoaded(FrameworkElement window, Action action)
	{
		void Run(object sender, RoutedEventArgs e)
		{
			window.Loaded -= Run;
			window.Dispatcher.BeginInvoke(action, DispatcherPriority.Loaded);
		}

		window.Loaded += Run;
	}
}
