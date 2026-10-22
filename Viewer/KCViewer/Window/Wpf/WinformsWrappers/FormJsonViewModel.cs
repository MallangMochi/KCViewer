using KCViewer.Resource;
using KCViewer.Window.Wpf.WinformsHost;

namespace KCViewer.Window.Wpf.WinformsWrappers;

public class FormJsonViewModel : WinformsHostViewModel
{
	public FormJsonViewModel() : base("JSON", "Json", IconContent.FormJson)
	{
		// todo remove parameter cause it's never used
		WinformsControl = new FormJson(null!) { TopLevel = false };

		WindowsFormsHost.Child = WinformsControl;
	}
}
