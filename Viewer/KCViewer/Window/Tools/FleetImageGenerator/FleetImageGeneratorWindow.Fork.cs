using System.Windows;

namespace KCViewer.Window.Tools.FleetImageGenerator;

/// <summary>
/// Fork: the KanColleViewer style window (Fork/KcvViews/KcvFleetImageGeneratorWindow, mockup 20261007-fleet-image-v2)
/// derives from this one to keep its image templates, and has its own buttons for the viewer's copy, load and save.
/// </summary>
public partial class FleetImageGeneratorWindow
{
	protected void ForkCopyImage() => CopyImageToClipboard(this, new RoutedEventArgs());

	protected void ForkLoadImage() => LoadImageFromClipboard(this, new RoutedEventArgs());

	protected void ForkSaveImage() => SaveImage(this, new RoutedEventArgs());
}
