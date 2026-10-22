using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the record tables pick rows without Ctrl ("컨트롤은 없애도 될듯", 2026-10-05; before, a plain click or drag
/// replaced the selection and a click on an empty part of a cell did nothing). A click puts the row into the selection
/// or takes it out; dragging does the same to the rows passed over (the table scrolls at its top and bottom edges);
/// Shift+click adds the rows from the last clicked one. Esc clears the selection. Only for tables with extended
/// selection: the ship picker's table picks one row.
/// </summary>
public static class KcvRecordSelection
{
	public static readonly DependencyProperty ToggleProperty = DependencyProperty.RegisterAttached(
		"Toggle", typeof(bool), typeof(KcvRecordSelection), new PropertyMetadata(false, OnToggleChanged));

	public static bool GetToggle(DependencyObject element) => (bool)element.GetValue(ToggleProperty);
	public static void SetToggle(DependencyObject element, bool value) => element.SetValue(ToggleProperty, value);

	private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
		"State", typeof(SelectionState), typeof(KcvRecordSelection));

	private sealed class SelectionState
	{
		/// <summary>The last clicked row, where Shift+click starts.</summary>
		public object? Anchor { get; set; }
		public Drag? Drag { get; set; }
	}

	private sealed class Drag(int start, bool select, HashSet<object> before)
	{
		public int Start { get; } = start;
		public bool Select { get; } = select;

		/// <summary>The selection before the drag: rows the drag passes over and leaves again get their state back.</summary>
		public HashSet<object> Before { get; } = before;

		public int Low { get; set; } = start;
		public int High { get; set; } = start;
		public DispatcherTimer? Scroll { get; set; }
		public int ScrollDirection { get; set; }
	}

	private static void OnToggleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not DataGrid grid) return;

		grid.PreviewMouseLeftButtonDown -= Press;
		grid.PreviewMouseMove -= Move;
		grid.PreviewMouseLeftButtonUp -= Release;
		grid.LostMouseCapture -= LostCapture;
		grid.PreviewKeyDown -= ClearOnEscape;

		if (e.NewValue is not true) return;

		grid.SetValue(StateProperty, new SelectionState());
		grid.PreviewMouseLeftButtonDown += Press;
		grid.PreviewMouseMove += Move;
		grid.PreviewMouseLeftButtonUp += Release;
		grid.LostMouseCapture += LostCapture;
		grid.PreviewKeyDown += ClearOnEscape;
	}

	private static SelectionState State(DataGrid grid) => (SelectionState)grid.GetValue(StateProperty);

	private static void Press(object sender, MouseButtonEventArgs e)
	{
		if (sender is not DataGrid { SelectionMode: DataGridSelectionMode.Extended } grid) return;
		if (RowAt(grid, e.OriginalSource as DependencyObject) is not { } row) return;

		object item = row.Item;
		int index = grid.Items.IndexOf(item);
		if (index < 0 || item == CollectionView.NewItemPlaceholder) return;

		e.Handled = true;
		FocusCell(grid, e.OriginalSource as DependencyObject);
		SelectionState state = State(grid);

		if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && state.Anchor is { } anchor && grid.Items.IndexOf(anchor) is >= 0 and int from)
		{
			for (int i = Math.Min(from, index); i <= Math.Max(from, index); i++)
			{
				SetSelected(grid, grid.Items[i], true);
			}

			return;
		}

		state.Anchor = item;
		state.Drag = new(index, !grid.SelectedItems.Contains(item), [.. grid.SelectedItems.Cast<object>()]);
		SetSelected(grid, item, state.Drag.Select);
		grid.CaptureMouse();
	}

	private static void Move(object sender, MouseEventArgs e)
	{
		if (sender is not DataGrid grid || State(grid)?.Drag is not { } drag) return;

		if (e.LeftButton is not MouseButtonState.Pressed)
		{
			EndDrag(grid);
			return;
		}

		Follow(grid, drag);
	}

	private static void Release(object sender, MouseButtonEventArgs e)
	{
		if (sender is DataGrid grid && State(grid)?.Drag is not null) EndDrag(grid);
	}

	private static void LostCapture(object sender, MouseEventArgs e)
	{
		if (sender is DataGrid grid && State(grid)?.Drag is not null) EndDrag(grid);
	}

	private static void ClearOnEscape(object sender, KeyEventArgs e)
	{
		if (e.Key is not Key.Escape || sender is not DataGrid { SelectedItems.Count: > 0 } grid) return;

		grid.UnselectAll();
		e.Handled = true;
	}

	private static void EndDrag(DataGrid grid)
	{
		SelectionState state = State(grid);
		if (state.Drag is not { } drag) return;

		state.Drag = null;
		drag.Scroll?.Stop();
		if (grid.IsMouseCaptured) grid.ReleaseMouseCapture();
	}

	/// <summary>Extends the drag to the row under the mouse; above or below the rows the table scrolls a row at a time.</summary>
	private static void Follow(DataGrid grid, Drag drag)
	{
		if (Rows(grid) is not { } rows) return;

		Point point = Mouse.GetPosition(rows);
		drag.ScrollDirection = point.Y < 0 ? -1 : point.Y > rows.ActualHeight ? 1 : 0;
		if (drag.ScrollDirection is not 0 && drag.Scroll is null)
		{
			drag.Scroll = new(TimeSpan.FromMilliseconds(60), DispatcherPriority.Input, (_, _) => ScrollStep(grid, drag), grid.Dispatcher);
		}

		if (drag.ScrollDirection is 0) drag.Scroll?.Stop();
		else drag.Scroll?.Start();

		Point inside = new(
			Math.Clamp(point.X, 1, Math.Max(1, rows.ActualWidth - 1)),
			Math.Clamp(point.Y, 1, Math.Max(1, rows.ActualHeight - 1)));
		if (RowAt(grid, rows.InputHitTest(inside) as DependencyObject) is not { } row) return;

		int index = grid.Items.IndexOf(row.Item);
		if (index >= 0) Extend(grid, drag, index);
	}

	private static void ScrollStep(DataGrid grid, Drag drag)
	{
		if (State(grid)?.Drag != drag || Scroller(grid) is not { } scroller)
		{
			drag.Scroll?.Stop();
			return;
		}

		if (drag.ScrollDirection < 0) scroller.LineUp();
		else scroller.LineDown();

		grid.UpdateLayout();
		Follow(grid, drag);
	}

	/// <summary>The rows between the start and <paramref name="index"/> get the drag's state; the others their own back.</summary>
	private static void Extend(DataGrid grid, Drag drag, int index)
	{
		int low = Math.Min(drag.Start, index);
		int high = Math.Max(drag.Start, index);

		for (int i = Math.Min(low, drag.Low); i <= Math.Max(high, drag.High); i++)
		{
			object item = grid.Items[i];
			SetSelected(grid, item, i >= low && i <= high ? drag.Select : drag.Before.Contains(item));
		}

		drag.Low = low;
		drag.High = high;
	}

	private static void SetSelected(DataGrid grid, object item, bool selected)
	{
		bool now = grid.SelectedItems.Contains(item);
		if (selected && !now) grid.SelectedItems.Add(item);
		else if (!selected && now) grid.SelectedItems.Remove(item);
	}

	/// <summary>The keyboard starts from the clicked row (arrow keys, Esc) without changing the selection.</summary>
	private static void FocusCell(DataGrid grid, DependencyObject? source)
	{
		if (Ancestor<DataGridCell>(source) is { } cell && cell.Focusable) cell.Focus();
		else grid.Focus();
	}

	private static DataGridRow? RowAt(DataGrid grid, DependencyObject? source)
	{
		// not from the scroll bars, the column headers or a table inside a cell
		if (Ancestor<ScrollBar>(source) is not null || Ancestor<DataGridColumnHeader>(source) is not null) return null;

		return Ancestor<DataGridRow>(source) is { } row && ItemsControl.ItemsControlFromItemContainer(row) == grid ? row : null;
	}

	private static ScrollViewer? Scroller(DataGrid grid) => Descendant<ScrollViewer>(grid);

	/// <summary>The part of the table's scroll viewer that shows the rows (below the column headers).</summary>
	private static FrameworkElement? Rows(DataGrid grid) =>
		Scroller(grid) is { } scroller ? scroller.Template?.FindName("PART_ScrollContentPresenter", scroller) as FrameworkElement : null;

	private static T? Ancestor<T>(DependencyObject? element) where T : DependencyObject
	{
		while (element is not null and not T)
		{
			element = element is Visual or System.Windows.Media.Media3D.Visual3D
				? VisualTreeHelper.GetParent(element)
				: LogicalTreeHelper.GetParent(element);
		}

		return element as T;
	}

	private static T? Descendant<T>(DependencyObject element) where T : DependencyObject
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(element, i);
			if (child is T found) return found;
			if (Descendant<T>(child) is { } deeper) return deeper;
		}

		return null;
	}
}
