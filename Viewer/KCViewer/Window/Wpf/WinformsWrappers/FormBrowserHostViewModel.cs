using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Resource;
using KCViewer.ViewModels.Translations;
using KCViewer.Window.Wpf.WinformsHost;

namespace KCViewer.Window.Wpf.WinformsWrappers;

// prefix with Form so it doesn't clash with the wpf versions
public class FormBrowserHostViewModel : WinformsHostViewModel
{
	public static string BrowserContentId => "Browser";
	public FormBrowserHostTranslationViewModel FormBrowserHost { get; }

	public FormBrowserHostViewModel() : base("Browser", BrowserContentId, IconContent.FormBrowser)
	{
		FormBrowserHost = Ioc.Default.GetService<FormBrowserHostTranslationViewModel>()!;

		Title = FormBrowserHost.Title;
		FormBrowserHost.PropertyChanged += (_, _) => Title = FormBrowserHost.Title;

		// todo remove parameter cause it's never used
		WinformsControl = new FormBrowserHost(null!) { TopLevel = false };

		WindowsFormsHost.Child = WinformsControl;
	}
}
