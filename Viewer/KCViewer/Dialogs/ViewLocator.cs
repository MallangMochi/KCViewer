using KCViewer.Dialogs.DataGridSettings;
using KCViewer.Dialogs.TextInput;
using KCViewer.ViewModels;
using HanumanInstitute.MvvmDialogs.Wpf;

namespace KCViewer.Dialogs;

public class ViewLocator : StrongViewLocator
{
	public ViewLocator()
	{
		Register<TextInputViewModel, TextInputDialog>();
		Register<DataGridSettingsViewModel, DataGridSettingsDialog>();

		Register<FormMainViewModel, FormMainWpf>();
	}
}
