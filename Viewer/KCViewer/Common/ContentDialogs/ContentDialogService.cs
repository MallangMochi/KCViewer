using System;
using System.Threading.Tasks;
using KCViewer.Common.ContentDialogs.ExportFilter;
using KCViewer.Common.ContentDialogs.ExportProgress;
using KCViewer.Common.ContentDialogs.Notification;
using ModernWpf.Controls;

namespace KCViewer.Common.ContentDialogs;

/// <summary>
/// This service is used to avoid coupling ViewModels with wpf.
/// Fork: the methods are virtual, for the windows in KanColleViewer's style (KcvExportDialogs).
/// </summary>
public class ContentDialogService
{
	public ExportFilterContentDialog? ExportFilterContentDialog { get; init; }
	public ExportProgressContentDialog? ExportProgressContentDialog { get; init; }
	public NotificationContentDialog? NotificationContentDialog { get; init; }

	public virtual async Task<ExportFilterViewModel?> ShowExportFilterAsync(ExportFilterViewModel exportFilter)
	{
		ArgumentNullException.ThrowIfNull(ExportFilterContentDialog);

		ExportFilterContentDialog.ExportFilter = exportFilter;

		_ = await ExportFilterContentDialog.ShowAsync(ContentDialogPlacement.InPlace);

		return exportFilter;
	}

	public virtual async Task<bool> ShowExportProgressAsync(ExportProgressViewModel exportProgress)
	{
		ArgumentNullException.ThrowIfNull(ExportProgressContentDialog);

		ExportProgressContentDialog.ExportProgress = exportProgress;

		bool result = await ExportProgressContentDialog.ShowAsync(ContentDialogPlacement.InPlace) switch
		{
			ContentDialogResult.None => true,
			_ => false,
		};

		ExportProgressContentDialog.ExportProgress = null;

		return result;
	}

	public virtual async Task<bool> ShowNotificationAsync(string? text, string? title = null)
	{
		ArgumentNullException.ThrowIfNull(NotificationContentDialog);

		NotificationContentDialog.Notification = text;
		NotificationContentDialog.NotificationTitle = title;

		await NotificationContentDialog.ShowAsync(ContentDialogPlacement.InPlace);

		return true;
	}
}
