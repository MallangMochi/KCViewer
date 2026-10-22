using System;
using KCViewer.Common.ContentDialogs.ExportProgress;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the sortie record's 데이터 처리... in KanColleViewer's style (see the XAML): closes itself when the viewer's
/// progress reaches its total, or on 취소 (the caller then stops the work, as the viewer's).
/// </summary>
public partial class KcvExportProgressWindow
{
	private readonly KcvExportProgressViewModel _viewModel;
	private bool _closed;

	public KcvExportProgressWindow(ExportProgressViewModel progress)
	{
		InitializeComponent();

		_viewModel = new KcvExportProgressViewModel(progress);
		DataContext = _viewModel;

		_viewModel.Done += WorkDone;
		Closed += (_, _) =>
		{
			_closed = true;
			_viewModel.Done -= WorkDone;
			_viewModel.Detach();
		};
	}

	private void WorkDone(object? sender, EventArgs e)
	{
		if (!_closed) Close();
	}
}
