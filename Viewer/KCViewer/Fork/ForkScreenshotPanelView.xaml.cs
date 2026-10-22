using System.Windows;
using System.Windows.Controls;

namespace KCViewer.Fork;

/// <summary>Fork: the screenshot panel for <see cref="ForkScreenshotPanelViewModel"/> (see the XAML).</summary>
public partial class ForkScreenshotPanelView : UserControl
{
	/// <summary>Narrower than this, the button shows the icon alone (mockup 20261009-screenshot-capture-v2).</summary>
	private const double LabelMinWidth = 90;

	public ForkScreenshotPanelView()
	{
		InitializeComponent();

		SizeChanged += (_, _) => ButtonLabel.Visibility = ActualWidth >= LabelMinWidth ? Visibility.Visible : Visibility.Collapsed;
		TakeButton.IsEnabledChanged += (_, _) => CameraIcon.Opacity = TakeButton.IsEnabled ? 1 : 0.4;
	}
}
