using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using KCViewer.Data;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the ship group's 필터 설정 in KanColleViewer's style (see the XAML and <see cref="KcvShipGroupFilterViewModel"/>),
/// opened through <see cref="TryEdit"/>, modal over the window in front.
/// </summary>
public partial class KcvShipGroupFilterWindow
{
	private KcvShipGroupFilterViewModel ViewModel { get; }

	/// <summary>
	/// The group's filter edited in a copy: false while the style is off and the group has no blocks (the caller opens
	/// the viewer's window), else true with the edited copy, or null for 취소. A group with blocks opens here with the
	/// style off too: the viewer's window cannot edit them.
	/// </summary>
	public static bool TryEdit(ShipGroupData group, out ShipGroupData? edited)
	{
		edited = null;

		if (!KcvViewState.Current.Enabled && group.Expressions?.ForkBlocks is null) return false;

		ShipGroupData copy = group.Clone();
		KcvShipGroupFilterWindow window = new(new KcvShipGroupFilterViewModel(copy));
		if (KcvCalcWindows.FrontWindow() is { } owner) window.Owner = owner;

		if (window.ShowDialog() is true) edited = window.ViewModel.WriteTo(copy);

		return true;
	}

	private KcvShipGroupFilterWindow(KcvShipGroupFilterViewModel viewModel)
	{
		InitializeComponent();

		ViewModel = viewModel;
		DataContext = viewModel;

		viewModel.Confirm = (text, caption, warning) => MessageBox.Show(this, text, caption, MessageBoxButton.YesNo,
			warning ? MessageBoxImage.Exclamation : MessageBoxImage.Question,
			warning ? MessageBoxResult.No : MessageBoxResult.Yes) == MessageBoxResult.Yes;
		viewModel.Inform = (text, caption, image) => MessageBox.Show(this, text, caption, MessageBoxButton.OK, image);

		// the viewer's menu items with their access keys
		ImportItem.Header = ShipGroupFilterResources.SubMenu_ImportFilter.Replace("&", "_");
		ExportItem.Header = ShipGroupFilterResources.SubMenu_ExportFilter.Replace("&", "_");
	}

	private void MenuButton_Click(object sender, RoutedEventArgs e)
	{
		ContextMenu menu = MenuButton.ContextMenu;
		menu.PlacementTarget = MenuButton;
		menu.Placement = PlacementMode.Bottom;
		menu.IsOpen = true;
	}

	private void ImportItem_Click(object sender, RoutedEventArgs e) => ViewModel.ImportCommand.Execute(null);

	private void ExportItem_Click(object sender, RoutedEventArgs e) => ViewModel.ExportCommand.Execute(null);

	private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}

/// <summary>A comparison's symbol in the box (=, ≤ …) and 「symbol name」 in its list (≤ 이하 …).</summary>
public sealed class KcvFilterOperatorTemplateSelector : DataTemplateSelector
{
	public DataTemplate? Short { get; set; }
	public DataTemplate? Long { get; set; }

	public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
		container is FrameworkElement { TemplatedParent: ComboBoxItem } ? Long : Short;
}

/// <summary>
/// The lines, brackets and ships of the filter window: UI Automation sees what is drawn (the elements inside), as for a
/// panel. With the items control's own items, the lines after the first two had nothing inside once they were made again.
/// </summary>
public sealed class KcvFilterItemsControl : ItemsControl
{
	protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}
