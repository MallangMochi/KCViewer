using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the part of <see cref="KcvFleetImageGeneratorWindow"/> above the preview.</summary>
public partial class KcvFleetImageHeader
{
	public KcvFleetImageHeader()
	{
		InitializeComponent();
	}

	private KcvFleetImageGeneratorWindow? HostWindow => System.Windows.Window.GetWindow(this) as KcvFleetImageGeneratorWindow;

	private void CopyToClipboard(object sender, RoutedEventArgs e) => HostWindow?.CopyToClipboard();

	private void LoadFromClipboard(object sender, RoutedEventArgs e) => HostWindow?.LoadFromClipboard();

	private void SaveToFile(object sender, RoutedEventArgs e) => HostWindow?.SaveToFile();

	private void OpenSettings(object sender, RoutedEventArgs e) => HostWindow?.OpenSettings();

	/// <summary>The buttons right of the tabs, or under them on the right when the window is too narrow for both.</summary>
	private void PlaceButtons(object sender, SizeChangedEventArgs e)
	{
		// the panels' own desired sizes are cut to the room they got; their children are measured without a limit
		bool narrow = TopRow.ActualWidth < FullWidth(Tabs) + FullWidth(Buttons);

		Grid.SetRow(Buttons, narrow ? 1 : 0);
		Grid.SetColumn(Buttons, narrow ? 0 : 1);
	}

	private static double FullWidth(StackPanel panel) =>
		panel.Children.OfType<UIElement>().Sum(c => c.DesiredSize.Width) + panel.Margin.Left + panel.Margin.Right;
}
