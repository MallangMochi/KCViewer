using KCViewer.Common.ContentDialogs;

namespace KCViewer.Window.Tools.SortieRecordViewer;

/// <summary>
/// Interaction logic for SortieRecordViewerWindow.xaml
/// </summary>
public partial class SortieRecordViewerWindow
{
	public SortieRecordViewerWindow() : base(new())
	{
		InitializeComponent();

		ViewModel.ContentDialogService = new ContentDialogService
		{
			ExportFilterContentDialog = ExportFilterContentDialog,
			ExportProgressContentDialog = ExportProgressContentDialog,
			NotificationContentDialog = NotificationContentDialog,
		};
	}
}
