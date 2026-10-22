using KCViewer.Window.Dialog.UiBlocker;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: UI 차단기 in KanColleViewer's style (see the XAML), opened through <see cref="KcvMenuWindows.TryShowUiBlocker"/>.
/// A plain window with its height following the content (the viewer's window base would keep a size); it calls the
/// view model as the viewer's window does: the settings read when shown, saved and the preview hidden when closed.
/// </summary>
public partial class KcvUiBlockerWindow
{
	public KcvUiBlockerWindow(UiBlockerManagerViewModel viewModel)
	{
		InitializeComponent();

		DataContext = viewModel;
		Loaded += (_, _) => viewModel.Loaded();
		Closed += (_, _) => viewModel.Closed();
	}
}
