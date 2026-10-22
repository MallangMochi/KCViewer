using System;
using System.Windows.Input;
using KCViewer.Dialogs.TextInput;
using ShipGroupResources = KCViewer.Avalonia.ShipGroup.ShipGroupResources;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the ship group's name in KanColleViewer's style (see the XAML), opened through
/// <see cref="KcvTableDialogs.TryShowTextInput"/>. The viewer's view model closes it (OK, 취소); Esc is 취소.
/// </summary>
public partial class KcvTextInputWindow
{
	private TextInputViewModel ViewModel { get; }

	public KcvTextInputWindow(TextInputViewModel viewModel)
	{
		InitializeComponent();

		ViewModel = viewModel;
		DataContext = viewModel;

		// the copy's title as the menu's 「그룹 복사」
		Title = viewModel.Title == ShipGroupResources.DialogGroupCopyTitle ? ForkStrings.GroupCopyTitle : viewModel.Title;

		ViewModel.RequestClose += CloseRequested;
		Closed += (_, _) => ViewModel.RequestClose -= CloseRequested;
		PreviewKeyDown += (_, e) =>
		{
			if (e.Key is not Key.Escape) return;

			e.Handled = true;
			ViewModel.CancelCommand.Execute(null);
		};

		// the name to type, or the name now selected (rename)
		Loaded += (_, _) =>
		{
			NameBox.Focus();
			NameBox.SelectAll();
		};
	}

	private void CloseRequested(object? sender, EventArgs e) => DialogResult = ViewModel.DialogResult;
}
