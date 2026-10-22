using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace KCViewer.Dialogs;

public static class Extensions
{
	public static IServiceCollection AddDialogServices(this IServiceCollection services) => services
		.AddSingleton<IViewLocator, ViewLocator>()
		.AddSingleton<IDialogManager, DialogManager>()
		.AddSingleton<IDialogService, DialogService>();
}
