using System.Windows.Forms;
using System.Windows.Interop;
using KCViewer.Avalonia.Dialogs.EquipmentSelector;
using KCViewer.Avalonia.Dialogs.ShipSelector;
using KCViewer.Fork.KcvViews;

namespace KCViewer.ViewModels;

public static class DialogExtensions
{
	private static System.Windows.Window MainWindow => App.Current!.MainWindow;

	public static void Show(this System.Windows.Window window, System.Windows.Window mainWindow)
	{
		window.Owner = mainWindow;
		window.Show();
	}

	public static bool? ShowDialog(this System.Windows.Window window, System.Windows.Window mainWindow)
	{
		window.Owner = mainWindow;
		return window.ShowDialog();
	}

	public static bool? ShowDialog(this ShipSelectorViewModel viewModel, System.Windows.Window? owner = null)
	{
		viewModel.SelectedShip = null;

		// Fork: the selector in KanColleViewer's style
		KcvCalcWindows.ShowShipSelector(viewModel);

		return viewModel.SelectedShip is not null;
	}

	public static bool ShowDialog(this EquipmentSelectorViewModel viewModel, System.Windows.Window? owner = null)
	{
		viewModel.SelectedEquipment = null;

		// Fork: the selector in KanColleViewer's style
		KcvCalcWindows.ShowEquipmentSelector(viewModel);

		return viewModel.SelectedEquipment is not null;
	}

	public static void Show(this Form form, System.Windows.Window mainWindow)
	{
		WindowInteropHelper helper = new(mainWindow);
		NativeWindow win32Parent = new();
		win32Parent.AssignHandle(helper.Handle);
		
		form.Show(win32Parent);
	}

	public static DialogResult ShowDialog(this Form form, System.Windows.Window mainWindow)
	{
		WindowInteropHelper helper = new(mainWindow);
		NativeWindow win32Parent = new();
		win32Parent.AssignHandle(helper.Handle);

		form.TopMost = true;

		return form.ShowDialog(win32Parent);
	}

	public static DialogResult ShowDialog(this CommonDialog dialog, System.Windows.Window mainWindow)
	{
		WindowInteropHelper helper = new(mainWindow);
		NativeWindow win32Parent = new();
		win32Parent.AssignHandle(helper.Handle);

		return dialog.ShowDialog(win32Parent);
	}
}
