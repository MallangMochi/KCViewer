using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Avalonia.Collections;
using KCViewer.Avalonia.Behaviors.PersistentColumns;
using KCViewer.Avalonia.ShipGroup;
using KCViewer.Utility;
using KCViewer.Window.Wpf.ShipGroupAvalonia;
using AvaloniaLength = Avalonia.Controls.DataGridLength;
using AvaloniaLengthUnit = Avalonia.Controls.DataGridLengthUnitType;

namespace KCViewer.Fork.KcvViews;

/// <summary>The viewer's Avalonia view model of the ship group panel, for the bindings of the columns and menus.</summary>
public sealed class KcvShipGroupProxy : BindingProxy<ShipGroupViewModel>;

/// <summary>
/// Fork: the ship group panel in KanColleViewer's style (see the XAML). The panel's view model is the viewer's
/// ShipGroupAvaloniaViewModel; its Avalonia view model drives this view. Kept as the Avalonia grid keeps them, so the
/// viewer saves them as before (when a group is left and at shutdown):
/// - the group's column settings (ColumnProperties, by position: width, order, visibility, the header for the column
///   selector), as PersistentColumnsBehavior does;
/// - the sorting, copied into the Avalonia collection view, which the viewer saves as the group's sort order.
/// </summary>
public partial class KcvShipGroupView
{
	private ShipGroupViewModel? Model { get; set; }

	/// <summary>While the saved settings are put on the grid, the grid's changes are not written back.</summary>
	private bool Applying { get; set; }

	private Point DragStart { get; set; }
	private ShipGroupItem? DragGroup { get; set; }

	public KcvShipGroupView()
	{
		InitializeComponent();

		DependencyPropertyDescriptor width = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));
		foreach (DataGridColumn column in Grid.Columns) width.AddValueChanged(column, (_, _) => StoreColumns());

		DataContextChanged += (_, _) => Attach();
		Loaded += (_, _) => Attach();
		Unloaded += (_, _) => Detach();
	}

	private void Attach()
	{
		ShipGroupViewModel? model = (DataContext as ShipGroupAvaloniaViewModel)?.WpfAvaloniaHost.Content is global::Avalonia.Controls.Control { DataContext: ShipGroupViewModel viewModel }
			? viewModel
			: null;
		if (model == Model) return;

		Detach();
		Model = model;
		((KcvShipGroupProxy)Resources["Proxy"]).DataContext = model!;
		Root.DataContext = model;
		if (model is null) return;

		model.PropertyChanged += OnModelPropertyChanged;
		ApplyColumns();
		ApplySort();
	}

	private void Detach()
	{
		if (Model is not null) Model.PropertyChanged -= OnModelPropertyChanged;
		Model = null;
	}

	private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ShipGroupViewModel.ColumnProperties):
				ApplyColumns();
				break;

			// a group was selected or refreshed: its sorting once the grid has the new items
			case nameof(ShipGroupViewModel.Items):
				Dispatcher.BeginInvoke(DispatcherPriority.DataBind, ApplySort);
				break;
		}
	}

	/// <summary>Puts the group's saved column settings on the grid (PersistentColumnsBehavior.ColumnPropertiesChanged).</summary>
	private void ApplyColumns()
	{
		if (Model is null) return;

		ObservableCollection<ColumnModel> models = Model.ColumnProperties;
		Applying = true;

		try
		{
			// a group saved with fewer columns gets the others, shown (AddMissingColumnData)
			for (int i = models.Count; i < Grid.Columns.Count; i++)
			{
				string header = HeaderOf(Grid.Columns[i]);
				models.Add(new ColumnModel { Name = header, Header = header, Width = AvaloniaLength.Auto, DisplayIndex = i, IsVisible = true });
			}

			// a grid never shown has no order yet (WasGridInitialized)
			if (models.Take(Grid.Columns.Count).All(m => m.DisplayIndex >= 0))
			{
				int last = Grid.Columns.Count - 1;
				foreach ((DataGridColumn column, ColumnModel model) in Grid.Columns.Zip(models).OrderBy(t => t.Second.DisplayIndex))
				{
					column.DisplayIndex = Math.Clamp(model.DisplayIndex, 0, last);
				}

				foreach ((DataGridColumn column, ColumnModel model) in Grid.Columns.Zip(models))
				{
					column.Width = ToWpf(model.Width);
					column.Visibility = model.IsVisible ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}
		finally
		{
			Applying = false;
		}

		StoreColumns();
	}

	/// <summary>Writes the grid's columns back into the group's settings (PersistentColumnsBehavior.UpdateColumnInfo).</summary>
	private void StoreColumns()
	{
		if (Model is null || Applying) return;

		foreach ((DataGridColumn column, ColumnModel model) in Grid.Columns.Zip(Model.ColumnProperties))
		{
			model.Width = ToAvalonia(column.Width);
			model.DisplayIndex = column.DisplayIndex >= 0 ? column.DisplayIndex : Grid.Columns.IndexOf(column);
			model.IsVisible = column.Visibility == Visibility.Visible;
			model.Header = HeaderOf(column);
		}
	}

	private void OnColumnReordered(object? sender, DataGridColumnEventArgs e) => StoreColumns();

	private static string HeaderOf(DataGridColumn column) => column.Header?.ToString() ?? "";

	private static DataGridLength ToWpf(AvaloniaLength width) => width.UnitType switch
	{
		// a width the viewer could not read back is 0 (ShipGroupAvaloniaViewModel.ProcessWidth): sized again
		AvaloniaLengthUnit.Pixel when width.Value >= 8 => new DataGridLength(width.Value),
		AvaloniaLengthUnit.Star => new DataGridLength(width.Value, DataGridLengthUnitType.Star),
		AvaloniaLengthUnit.SizeToCells => DataGridLength.SizeToCells,
		AvaloniaLengthUnit.SizeToHeader => DataGridLength.SizeToHeader,
		_ => DataGridLength.Auto,
	};

	private static AvaloniaLength ToAvalonia(DataGridLength width) => width.UnitType switch
	{
		DataGridLengthUnitType.Pixel => new AvaloniaLength(width.Value, AvaloniaLengthUnit.Pixel, width.Value, width.Value),
		DataGridLengthUnitType.Star => new AvaloniaLength(width.Value, AvaloniaLengthUnit.Star),
		DataGridLengthUnitType.SizeToCells => AvaloniaLength.SizeToCells,
		DataGridLengthUnitType.SizeToHeader => AvaloniaLength.SizeToHeader,
		_ => AvaloniaLength.Auto,
	};

	/// <summary>The group's saved sorting on the grid, with the header arrows.</summary>
	private void ApplySort()
	{
		if (Model is null) return;

		Grid.Items.SortDescriptions.Clear();
		foreach (DataGridColumn column in Grid.Columns) column.SortDirection = null;

		foreach (DataGridSortDescription sort in Model.SortDescriptions)
		{
			Grid.Items.SortDescriptions.Add(new SortDescription(sort.PropertyPath, sort.Direction));
			if (Grid.Columns.FirstOrDefault(c => c.SortMemberPath == sort.PropertyPath) is { } column) column.SortDirection = sort.Direction;
		}
	}

	// After the grid sorted (a header click): the same sorting into the Avalonia view, which the viewer saves.
	private void OnSorting(object? sender, DataGridSortingEventArgs e) => Dispatcher.BeginInvoke(DispatcherPriority.DataBind, () =>
	{
		if (Model is null) return;

		Model.CollectionView.SortDescriptions.Clear();
		foreach (SortDescription sort in Grid.Items.SortDescriptions)
		{
			Model.CollectionView.SortDescriptions.Add(DataGridSortDescription.FromPath(sort.PropertyName, sort.Direction));
		}
	});

	private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => Model?.SelectionChangedCommand.Execute(Grid.SelectedItems);

	// The press itself selects the group (the tab's command); it may become a drag
	private void OnGroupMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is not FrameworkElement { Tag: ShipGroupItem group }) return;

		DragStart = e.GetPosition(this);
		DragGroup = group;
	}

	private void OnGroupMouseMove(object sender, MouseEventArgs e)
	{
		if (DragGroup is not { } group || e.LeftButton != MouseButtonState.Pressed || sender is not DependencyObject tab) return;

		Vector moved = e.GetPosition(this) - DragStart;
		if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance) return;

		DragGroup = null;
		DragDrop.DoDragDrop(tab, new DataObject(typeof(ShipGroupItem), group), DragDropEffects.Move);
	}

	// A tab dropped onto another takes its place (ShipGroupDropHandler)
	private void OnGroupDrop(object sender, DragEventArgs e)
	{
		if (Model is null || sender is not FrameworkElement { Tag: ShipGroupItem target }) return;
		if (e.Data.GetData(typeof(ShipGroupItem)) is not ShipGroupItem source) return;

		int from = Model.Groups.IndexOf(source);
		int to = Model.Groups.IndexOf(target);
		if (from >= 0 && to >= 0 && from != to) Model.Groups.Move(from, to);
	}

	private void OnGroupAreaMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount == 2) Model?.AddGroupCommand.Execute(null);
	}
}
