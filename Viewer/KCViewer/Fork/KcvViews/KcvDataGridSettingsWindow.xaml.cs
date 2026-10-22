using System;
using System.ComponentModel;
using System.Windows.Input;
using KCViewer.Dialogs.DataGridSettings;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: 데이터 그리드 설정 in KanColleViewer's style (see the XAML), opened through
/// <see cref="KcvTableDialogs.TryShowDataGridSettings"/>. The viewer's view model closes it (OK, 취소) and undoes the
/// changes when it is closed otherwise.
/// </summary>
public partial class KcvDataGridSettingsWindow
{
	private DataGridSettingsViewModel ViewModel { get; }

	public KcvDataGridSettingsWindow(DataGridSettingsViewModel viewModel)
	{
		InitializeComponent();

		ViewModel = viewModel;
		DataContext = viewModel;

		ViewModel.RequestClose += CloseRequested;
		Closing += OnClosing;
		// Esc is 취소 (the viewer's command closes the window; a cancel button would close it a second time)
		PreviewKeyDown += (_, e) =>
		{
			if (e.Key is not Key.Escape) return;

			e.Handled = true;
			ViewModel.CancelCommand.Execute(null);
		};
	}

	private void CloseRequested(object? sender, EventArgs e)
	{
		ViewModel.RequestClose -= CloseRequested;
		DialogResult = ViewModel.DialogResult;
	}

	private void OnClosing(object? sender, CancelEventArgs e)
	{
		ViewModel.RequestClose -= CloseRequested;
		ViewModel.ClosingCommand.Execute(null);
	}
}
