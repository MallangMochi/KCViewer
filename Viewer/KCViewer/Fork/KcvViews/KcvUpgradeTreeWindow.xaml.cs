using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using KCViewer.Window.Tools.EquipmentUpgradePlanner.UpgradeTree;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the upgrade tree in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/>. At first
/// only the root is open (user's decision 2026-10-09 "루트만 펼치고"); 모두 펼치기 opens every item, 모두 접기 closes them.
/// The status bar counts the plans and the items to develop or get among the shown rows.
/// </summary>
public partial class KcvUpgradeTreeWindow
{
	public KcvUpgradeTreeWindow(UpgradeTreeViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		Title = ForkStrings.KcvUpgradeTreeTitle(viewModel.Items.FirstOrDefault()?.ForkName ?? "");

		Loaded += (_, _) =>
		{
			if (Tree.ItemContainerGenerator.ContainerFromIndex(0) is TreeViewItem root) root.IsExpanded = true;
			UpdateStatusLater();
		};
		Tree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler((_, _) => UpdateStatusLater()));
		Tree.AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler((_, _) => UpdateStatusLater()));
		// the menu's actions change the items under the one clicked
		Tree.AddHandler(ContextMenuClosingEvent, new ContextMenuEventHandler((_, _) => UpdateStatusLater()));
	}

	private void ExpandAll(object sender, RoutedEventArgs e)
	{
		SetExpanded(Tree, true);
		UpdateStatusLater();
	}

	private void CollapseAll(object sender, RoutedEventArgs e)
	{
		SetExpanded(Tree, false);
		UpdateStatusLater();
	}

	/// <summary>Opens or closes every item; an item's children get their rows when it opens.</summary>
	private static void SetExpanded(ItemsControl parent, bool expanded)
	{
		foreach (object item in parent.Items)
		{
			if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem container) continue;

			container.IsExpanded = expanded;
			if (expanded) container.UpdateLayout();
			SetExpanded(container, expanded);
		}
	}

	private void UpdateStatusLater()
	{
		Dispatcher.BeginInvoke(UpdateStatus, DispatcherPriority.Background);
	}

	private void UpdateStatus()
	{
		int plans = 0;
		int crafts = 0;

		void Count(ItemsControl parent)
		{
			foreach (object item in parent.Items)
			{
				if (item is not UpgradeTreeUpgradePlanViewModel node) continue;

				if (node.ForkIsPlan) plans++;
				if (node.ForkIsCraft) crafts++;

				if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem { IsExpanded: true } container)
				{
					Count(container);
				}
			}
		}

		Count(Tree);
		Status.Text = ForkStrings.KcvUpgradeTreeStatus(plans, crafts);
	}
}

/// <summary>A tree item's indent: its depth times 18, as the left margin of its arrow.</summary>
public sealed class KcvTreeIndentConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		int depth = 0;
		ItemsControl? parent = value is TreeViewItem item ? ItemsControl.ItemsControlFromItemContainer(item) : null;

		while (parent is TreeViewItem parentItem)
		{
			depth++;
			parent = ItemsControl.ItemsControlFromItemContainer(parentItem);
		}

		return new Thickness(6 + depth * 18, 0, 0, 0);
	}

	public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
