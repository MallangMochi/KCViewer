using KCViewer.ViewModels;
using HanumanInstitute.MvvmDialogs.Wpf;

namespace KCViewer.Dialogs;

public class ViewLocator : StrongViewLocator
{
	public ViewLocator()
	{
		// Fork: the text input and data grid settings dialogs open in KanColleViewer's style (Fork/KcvViews/KcvTableDialogs.cs)
		Register<FormMainViewModel, FormMainWpf>();
	}
}
