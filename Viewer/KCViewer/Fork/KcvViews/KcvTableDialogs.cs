using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Common.Datagrid;
using KCViewer.Dialogs.DataGridSettings;
using KCViewer.Dialogs.TextInput;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the table dialogs in KanColleViewer's style (mockup 20261017-table-dialogs-v2, approved 2026-10-17 "목록에 넣고
/// 구현"): 열 표시 설정 from the tables' menus and the ship group's, 데이터 그리드 설정 and the ship group's name (add,
/// copy, rename, a new group of the selected ships). Each is modal over the window in front; Enter is OK and Esc is
/// 취소. With the style off the viewer's dialogs open.
/// </summary>
public static class KcvTableDialogs
{
	/// <summary>열 표시 설정; null while the style is off (the caller opens the viewer's), else whether OK was pressed.</summary>
	public static bool? TryShowColumnSelector(ColumnSelectorViewModel selector)
	{
		if (!KcvViewState.Current.Enabled) return null;

		return ShowModal(new KcvColumnSelectorWindow(new KcvColumnSelectorViewModel(selector)));
	}

	/// <summary>데이터 그리드 설정; false while the style is off. The view model applies and undoes the changes itself.</summary>
	public static bool TryShowDataGridSettings(DataGridSettingsViewModel settings)
	{
		if (!KcvViewState.Current.Enabled) return false;

		ShowModal(new KcvDataGridSettingsWindow(settings));
		return true;
	}

	/// <summary>The group's name; null while the style is off, else whether OK was pressed (closing the window is 취소).</summary>
	public static bool? TryShowTextInput(TextInputViewModel input)
	{
		if (!KcvViewState.Current.Enabled) return null;

		ShowModal(new KcvTextInputWindow(input));
		return input.DialogResult is true;
	}

	private static bool ShowModal(System.Windows.Window window)
	{
		if (KcvCalcWindows.FrontWindow() is { } owner) window.Owner = owner;

		return window.ShowDialog() is true;
	}
}

/// <summary>
/// 열 표시 설정's model: the viewer's (its columns' 표시 and the ship group's 열 고정) with 「전체 선택」 and the layout of
/// the checks: one column up to 12, else three, filled from the top down.
/// </summary>
public sealed class KcvColumnSelectorViewModel : ObservableObject
{
	private const int SingleColumnLimit = 12;
	private const int Columns = 3;
	public const double CheckHeight = 24;

	public ColumnSelectorViewModel Selector { get; }
	public IReadOnlyList<ColumnViewModel> Items => Selector.Columns;

	public bool HasFrozen => Selector.FrozenColumns is not null;
	public int FrozenMaximum => Items.Count;

	public int Frozen
	{
		get => Selector.FrozenColumns ?? 0;
		set => Selector.FrozenColumns = value;
	}

	/// <summary>
	/// Every column shown (true), none (false) or some (null, the box's third state). A click hides all when all are
	/// shown and shows all otherwise (the check box itself would go from some to none).
	/// </summary>
	public bool? AllVisible
	{
		get => Items.All(c => c.IsVisible) ? true : Items.Any(c => c.IsVisible) ? null : false;
		set
		{
			bool visible = value is true || AllVisible is null;

			foreach (ColumnViewModel column in Items) column.IsVisible = visible;
		}
	}

	public double ListHeight => CheckHeight * (Items.Count <= SingleColumnLimit ? Items.Count : (int)Math.Ceiling(Items.Count / (double)Columns));
	public double ItemWidth => Items.Count <= SingleColumnLimit ? double.NaN : 150;

	/// <summary>The window's width for one or three columns (its height follows the content: with the width too, the window's frame left a blank band).</summary>
	public double WindowWidth => Items.Count <= SingleColumnLimit ? 260 : Columns * 150 + 70;

	public KcvColumnSelectorViewModel(ColumnSelectorViewModel selector)
	{
		Selector = selector;

		foreach (ColumnViewModel column in Items) column.PropertyChanged += ColumnChanged;
	}

	private void ColumnChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(ColumnViewModel.IsVisible)) OnPropertyChanged(nameof(AllVisible));
	}
}

/// <summary>True for a name with something other than spaces (the group's name may not be empty).</summary>
public sealed class KcvHasTextConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is string text && !string.IsNullOrWhiteSpace(text);
}
