using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: KanColleViewer's 所属艦娘一覧 (see the XAML), from the tools menu while the style is on.</summary>
public partial class KcvShipListWindow
{
	/// <summary>
	/// Columns whose first click sorts from the first: ID (also how a sort is undone, there is no unsorted state),
	/// the ship type (the game's type order) and the name (album order). Numbers sort from the largest first.
	/// </summary>
	private static readonly HashSet<string> AscendingFirst = [nameof(KcvShipRow.Id), nameof(KcvShipRow.TypeId), nameof(KcvShipRow.SortId)];

	public KcvShipListWindow() : base(new KcvShipListViewModel())
	{
		InitializeComponent();

		// A new window starts in ID order, once (Loaded can come again, which must not undo the user's sort)
		Table.Loaded += SortById;
	}

	private void SortById(object sender, System.Windows.RoutedEventArgs e)
	{
		Table.Loaded -= SortById;
		Sort(IdColumn, ListSortDirection.Ascending);
	}

	private void OpenShipEncyclopedia(object sender, MouseButtonEventArgs e)
	{
		if (sender is not DataGridRow { DataContext: KcvShipRow row }) return;

		ViewModel.OpenShipEncyclopediaCommand.Execute(row.ShipId);
	}

	/// <summary>A header click: the other direction on the sorted column, else the column's first direction.</summary>
	private void SortTable(object sender, DataGridSortingEventArgs e)
	{
		e.Handled = true;

		ListSortDirection direction = e.Column.SortDirection switch
		{
			ListSortDirection.Ascending => ListSortDirection.Descending,
			ListSortDirection.Descending => ListSortDirection.Ascending,
			_ => AscendingFirst.Contains(e.Column.SortMemberPath) ? ListSortDirection.Ascending : ListSortDirection.Descending,
		};
		Sort(e.Column, direction);
	}

	/// <summary>Equal values in ID order. The rows are replaced in place when a filter changes, so the order stays.</summary>
	private void Sort(DataGridColumn column, ListSortDirection direction)
	{
		foreach (DataGridColumn other in Table.Columns)
		{
			other.SortDirection = null;
		}
		column.SortDirection = direction;

		SortDescriptionCollection sort = Table.Items.SortDescriptions;
		using (Table.Items.DeferRefresh())
		{
			sort.Clear();
			sort.Add(new SortDescription(column.SortMemberPath, direction));
			if (column.SortMemberPath != nameof(KcvShipRow.Id))
			{
				sort.Add(new SortDescription(nameof(KcvShipRow.Id), ListSortDirection.Ascending));
			}
		}
	}
}
