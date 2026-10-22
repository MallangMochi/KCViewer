using CommunityToolkit.Mvvm.ComponentModel;

namespace KCViewer.Common.ContentDialogs.ExportProgress;

public class ExportProgressViewModel : ObservableObject
{
	public int Progress { get; set; }
	public int Total { get; set; }
}
