using System;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Common;
using KCViewer.ViewModels.Translations;

namespace KCViewer.Window.Dialog.ResourceChartWPF;

public partial class ResourceChartViewModel : WindowViewModelBase
{
	public ResourceChartViewModel()
	{
		DialogResourceChart = Ioc.Default.GetService<DialogResourceChartTranslationViewModel>()!;
	}

	public DateTime DateBegin { get; set; }
	public DateTime DateEnd { get; set; }
	public DateTime MinDate { get; set; }
	public DateTime MaxDate { get; set; }
	public DialogResourceChartTranslationViewModel DialogResourceChart { get; }
}
