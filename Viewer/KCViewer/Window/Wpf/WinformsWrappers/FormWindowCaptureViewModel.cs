using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Resource;
using KCViewer.ViewModels;
using KCViewer.ViewModels.Translations;
using KCViewer.Window.Wpf.WinformsHost;

namespace KCViewer.Window.Wpf.WinformsWrappers;

public class FormWindowCaptureViewModel : WinformsHostViewModel
{
	private FormWindowCaptureTranslationViewModel FormWindowCapture { get; }

	public FormWindowCaptureViewModel(FormMainViewModel parent) 
		: base("Window Capture", "WindowCapture", IconContent.FormWindowCapture) // Fork: the window capture icon, not JSON's (user's choice 2026-10-02)
	{
		FormWindowCapture = Ioc.Default.GetService<FormWindowCaptureTranslationViewModel>()!;

		Title = FormWindowCapture.Title;
		FormWindowCapture.PropertyChanged += (_, _) => Title = FormWindowCapture.Title;

		WinformsControl = new FormWindowCapture(parent) { TopLevel = false };

		WindowsFormsHost.Child = WinformsControl;
	}
}
